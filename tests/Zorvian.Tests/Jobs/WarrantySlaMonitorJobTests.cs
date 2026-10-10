using FluentAssertions;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Jobs;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Xunit;

namespace Zorvian.Tests.Jobs;

public sealed class WarrantySlaMonitorJobTests
{
    private readonly Mock<IWarrantyRepository> _warrantyRepo = new();
    private readonly Mock<INotificationService> _notifier = new();
    private readonly Mock<IAuthRepository> _authRepo = new();
    private readonly Mock<ITenantContextWriter> _tenantWriter = new();
    private readonly WarrantySlaMonitorJob _sut;

    public WarrantySlaMonitorJobTests()
    {
        _authRepo.Setup(r => r.GetAllCompaniesAsync())
            .ReturnsAsync(new List<Company>
            {
                new() { Id = Guid.NewGuid(), TenantId = "company-tenant-1", Name = "Test Co" }
            });

        _sut = new WarrantySlaMonitorJob(
            _warrantyRepo.Object, _notifier.Object, _authRepo.Object, _tenantWriter.Object);
    }

    [Fact]
    public async Task RunAsync_DetectsBreachedSla_AndTriggersNotification()
    {
        // Arrange
        var breachedWarranty = new Warranty 
        { 
            Id = Guid.NewGuid(), 
            WarrantyNumber = "GAR-001",
            SlaDueAt = DateTime.UtcNow.AddHours(-1) // Breached 1 hour ago
        };
        
        _warrantyRepo.Setup(r => r.GetAtRiskWarrantiesAsync())
            .ReturnsAsync(new List<Warranty> { breachedWarranty });

        // Act
        await _sut.RunAsync();

        // Assert
        breachedWarranty.SlaBreachedAt.Should().NotBeNull();
        _notifier.Verify(n => n.NotifyTenantAsync(
            It.IsAny<string>(), 
            "SLA Excedido", 
            It.Is<string>(msg => msg.Contains("GAR-001")), 
            "SLA_BREACHED", 
            breachedWarranty.Id.ToString()), 
            Times.Once);
        _warrantyRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
