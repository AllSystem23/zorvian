using FluentAssertions;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;
using Zorvian.Web.Services;
using Xunit;

namespace Zorvian.Tests.Services;

public sealed class CombinedNotificationServiceTests
{
    private readonly Mock<ISignalRNotificationService> _signalRMock = new();
    private readonly Mock<IFCMNotificationService> _fcmMock = new();
    private readonly Mock<ITenantContext> _tenantMock = new();
    private readonly CombinedNotificationService _sut;

    public CombinedNotificationServiceTests()
    {
        _sut = new CombinedNotificationService(
            _signalRMock.Object,
            _fcmMock.Object,
            _tenantMock.Object
        );
    }

    [Fact]
    public async Task NotifyTenantAsync_CallsAllServices()
    {
        // Arrange
        var tenantId = "tenant1";
        var title = "Test";
        var message = "Msg";

        // Act
        await _sut.NotifyTenantAsync(tenantId, title, message);

        // Assert
        _signalRMock.Verify(s => s.NotifyTenantAsync(tenantId, title, message, null, null), Times.Once);
        _fcmMock.Verify(f => f.SendToTenantAsync(tenantId, title, message, null), Times.Once);
        // WhatsApp is not called in NotifyTenantAsync in the current implementation, check code.
    }
}
