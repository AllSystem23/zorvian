using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Xunit;

namespace Zorvian.Tests.Services;

public class CashRegisterServiceTests
{
    private readonly Mock<ICashRegisterRepository> _mockRegisterRepo = new();
    private readonly Mock<ICashMovementRepository> _mockMovementRepo = new();
    private readonly Mock<ITenantContext> _mockTenant = new();
    private readonly Mock<AutoMapper.IMapper> _mockMapper = new();
    private readonly Mock<IAutoAccountingService> _mockAccountingService = new();
    
    private readonly CashRegisterService _sut;

    public CashRegisterServiceTests()
    {
        _sut = new CashRegisterService(
            _mockRegisterRepo.Object,
            _mockMovementRepo.Object,
            _mockTenant.Object,
            _mockMapper.Object,
            _mockAccountingService.Object);
    }

    [Fact]
    public async Task ApproveMovementAsync_ShouldUpdateStatusAndTriggerAccounting()
    {
        // ARRANGE
        var movementId = Guid.NewGuid();
        var movement = new CashMovement { Id = movementId, ApprovalStatus = "draft" };
        
        _mockMovementRepo.Setup(r => r.GetByIdAsync(movementId)).ReturnsAsync(movement);

        // ACT
        var result = await _sut.ApproveMovementAsync(movementId);

        // ASSERT
        Assert.True(result);
        Assert.Equal("approved", movement.ApprovalStatus);
        _mockMovementRepo.Verify(r => r.UpdateAsync(movement), Times.Once);
        _mockAccountingService.Verify(a => a.GenerateCashMovementEntryAsync(movementId), Times.Once);
    }
}
