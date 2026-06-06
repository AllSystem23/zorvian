using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Services;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;
using FluentAssertions;

namespace Zorvian.Tests.Services;

public sealed class WarrantyPartRequestServiceTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IInventoryMovementService> _inventoryService = new();
    private readonly WarrantyPartRequestRepository _repo;
    private readonly WarrantyPartRequestService _sut;
    private readonly string _tenantId;

    public WarrantyPartRequestServiceTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        var db = new ZorvianDbContext(options, _tenant.Object);
        _repo = new WarrantyPartRequestRepository(db);
        _sut = new WarrantyPartRequestService(_repo, _inventoryService.Object, _mapper.Object);
    }

    [Fact]
    public async Task CreateAsync_MapsAndPersists()
    {
        var warrantyId = Guid.NewGuid();
        var claimId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var request = new CreateWarrantyPartRequestRequest(
            WarrantyId: warrantyId,
            ClaimId: claimId,
            ProviderId: providerId,
            ProductId: productId,
            QuantityRequested: 3,
            UnitPrice: 25m
        );

        var partRequest = new WarrantyPartRequest
        {
            Id = Guid.NewGuid(),
            WarrantyId = warrantyId,
            ClaimId = claimId,
            ProviderId = providerId,
            ProductId = productId,
            QuantityRequested = 3,
            UnitPrice = 25m,
            RequestNumber = "REQ-001",
            Status = "requested"
        };

        _mapper.Setup(m => m.Map<WarrantyPartRequest>(request)).Returns(partRequest);
        _mapper.Setup(m => m.Map<WarrantyPartRequestResponse>(partRequest))
            .Returns(new WarrantyPartRequestResponse(
                partRequest.Id, warrantyId, claimId, providerId, productId, 3, 0, 25m,
                "REQ-001", It.IsAny<DateTime>(), null, null, "requested", null));

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.QuantityRequested.Should().Be(3);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_ReturnsNull_WhenNotFound()
    {
        var request = new UpdateWarrantyPartRequestStatusRequest("received");
        var result = await _sut.UpdateStatusAsync(Guid.NewGuid(), request);
        result.Should().BeNull();
    }
}
