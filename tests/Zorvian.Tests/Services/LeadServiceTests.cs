using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class LeadServiceTests
{
    private readonly Mock<ILeadRepository> _leadRepo = new();
    private readonly Mock<ITenantContext> _tenantContext = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IFCMNotificationService> _fcmService = new();
    private readonly LeadService _sut;

    public LeadServiceTests()
    {
        var tenantGuid = Guid.NewGuid();
        _tenantContext.Setup(t => t.TenantId).Returns(TenantId.FromGuid(tenantGuid));
        _sut = new LeadService(
            _leadRepo.Object, 
            _tenantContext.Object, 
            _emailService.Object, 
            _fcmService.Object);
    }

    [Fact]
    public async Task CreateLeadAsync_SendsEmail_WhenLeadHasEmail()
    {
        // Arrange
        var lead = new Lead 
        { 
            FirstName = "Juan", 
            LastName = "Perez", 
            Email = "juan@example.com" 
        };

        // Act
        await _sut.CreateLeadAsync(lead);

        // Assert
        _emailService.Verify(e => e.SendWelcomeEmailAsync(lead.Email, lead.FirstName), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateLeadAsync_SendsPushNotification_ToTenant()
    {
        // Arrange
        var tenantId = _tenantContext.Object.TenantId.Value;
        var lead = new Lead 
        { 
            FirstName = "Maria", 
            LastName = "Lopez", 
            CompanyName = "Tienda ABC" 
        };

        // Act
        await _sut.CreateLeadAsync(lead);

        // Assert
        // Se agrega It.IsAny<string>() para el parámetro opcional 'type'
        _fcmService.Verify(f => f.SendToTenantAsync(
            tenantId.ToString(), 
            It.IsAny<string>(), 
            It.Is<string>(s => s.Contains("Maria Lopez")),
            It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateLeadAsync_PersistsLead_InRepository()
    {
        // Arrange
        var lead = new Lead { FirstName = "Carlos" };

        // Act
        await _sut.CreateLeadAsync(lead);

        // Assert
        _leadRepo.Verify(r => r.AddAsync(lead), Times.Once);
        _leadRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
