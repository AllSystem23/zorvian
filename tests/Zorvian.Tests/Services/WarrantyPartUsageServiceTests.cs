using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Xunit;

namespace Zorvian.Tests.Services;

public sealed class WarrantyPartUsageServiceTests
{
    private readonly Mock<IWarrantyPartUsageRepository> _repo = new();
    private readonly Mock<IInventoryMovementService> _inventoryService = new();
    private readonly Mock<IWarrantyCostService> _costService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly WarrantyPartUsageService _sut;

    public WarrantyPartUsageServiceTests()
    {
        _sut = new WarrantyPartUsageService(_repo.Object, _inventoryService.Object, _costService.Object, _mapper.Object);
    }

    [Fact]
    public async Task RecordUsageAsync_CreatesMovement_AndPersistsUsage()
    {
        // Arrange
        var request = new CreateWarrantyPartUsageRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "Test usage");
        var branchId = Guid.NewGuid();
        var warrantyId = Guid.NewGuid();

        var movement = new InventoryMovementResponse(
            Guid.NewGuid(), request.ProductId, "Part", "P001", "exit", request.QuantityUsed, 
            10, 9, 100m, "REF", "Test", "User", DateTime.UtcNow);

        _inventoryService.Setup(s => s.CreateAsync(It.IsAny<CreateInventoryMovementRequest>()))
            .ReturnsAsync(movement);
        
        _mapper.Setup(m => m.Map<WarrantyPartUsage>(request)).Returns(new WarrantyPartUsage());
        _mapper.Setup(m => m.Map<WarrantyPartUsageResponse>(It.IsAny<WarrantyPartUsage>()))
            .Returns(new WarrantyPartUsageResponse(Guid.NewGuid(), request.ClaimId, request.PartReceiptId, request.ProductId, "Part", request.QuantityUsed, 100m, 100m, DateTime.UtcNow, request.Notes));

        // Act
        var result = await _sut.RecordUsageAsync(request, branchId, warrantyId);

        // Assert
        result.Should().NotBeNull();
        _repo.Verify(r => r.AddAsync(It.IsAny<WarrantyPartUsage>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _costService.Verify(c => c.CreateAsync(It.IsAny<CreateWarrantyCostRequest>()), Times.Once);
    }
}
