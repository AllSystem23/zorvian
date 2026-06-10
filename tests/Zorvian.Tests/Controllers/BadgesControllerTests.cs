using Microsoft.AspNetCore.Mvc;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Web.Controllers;

namespace Zorvian.Tests.Controllers;

public sealed class BadgesControllerTests
{
    private readonly Mock<IBadgeService> _service = new();
    private readonly BadgesController _sut;

    public BadgesControllerTests()
    {
        _sut = new BadgesController(_service.Object);
    }

    [Fact]
    public async Task GetBadges_ShouldReturnOkWithCounts()
    {
        var counts = new Dictionary<string, int>
        {
            { "credits-pending", 12 },
            { "overdue-credits", 3 },
            { "warranties-pending", 5 },
            { "approvals-pending", 8 },
        };
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(counts);

        var result = await _sut.GetBadges();

        var ok = Assert.IsType<OkObjectResult>(result);
        var dict = Assert.IsType<Dictionary<string, int>>(ok.Value);
        Assert.Equal(4, dict.Count);
        Assert.Equal(12, dict["credits-pending"]);
    }

    [Fact]
    public async Task GetBadges_ShouldReturnOkWithEmptyDict_WhenNoBadges()
    {
        var empty = new Dictionary<string, int>();
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(empty);

        var result = await _sut.GetBadges();

        var ok = Assert.IsType<OkObjectResult>(result);
        var dict = Assert.IsType<Dictionary<string, int>>(ok.Value);
        Assert.Empty(dict);
    }

    [Fact]
    public async Task GetBadges_ShouldReturnError_WhenServiceThrows()
    {
        _service.Setup(s => s.GetAllAsync()).ThrowsAsync(new Exception("Unexpected error"));

        await Assert.ThrowsAsync<Exception>(() => _sut.GetBadges());
    }
}
