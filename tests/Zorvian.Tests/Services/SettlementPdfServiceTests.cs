using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Models;
using Zorvian.Core.Enums;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

public sealed class SettlementPdfServiceTests
{
    [Fact]
    public async Task GenerateSettlementPdfAsync_Should_Return_ByteArray()
    {
        // Arrange
        var service = new SettlementPdfService();
        var context = new PayrollContext(1, false, new TerminationContext(TerminationType.Resignation, DateTime.Now.AddYears(-1), DateTime.Now, 1000m));
        
        // Act
        var result = await service.GenerateSettlementPdfAsync(Guid.NewGuid(), Guid.NewGuid(), context);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }
}
