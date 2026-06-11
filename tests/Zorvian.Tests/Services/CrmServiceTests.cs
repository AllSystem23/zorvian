using Moq;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;

namespace Zorvian.Tests.Services;

public sealed class CrmServiceTests
{
    private readonly Mock<ILogger<CrmService>> _logger = new();
    private readonly Mock<IGoalIntegrationService> _goalIntegration = new();
    private readonly CrmService _sut;

    public CrmServiceTests()
    {
        _sut = new CrmService(_logger.Object, _goalIntegration.Object);
    }

    [Fact]
    public async Task CreateOpportunityAsync_DispatchesGoalIntegration()
    {
        var clientId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        await _sut.CreateOpportunityAsync("Test Opp", clientId, 1000m, "prospecting", ownerId);

        _goalIntegration.Verify(g => g.HandleNewClientAsync(ownerId, clientId), Times.Once);
    }
}
