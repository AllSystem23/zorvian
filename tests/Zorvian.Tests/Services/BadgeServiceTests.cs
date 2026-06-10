using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;

namespace Zorvian.Tests.Services;

public sealed class BadgeServiceTests
{
    private readonly Mock<IBadgeRepository> _repo = new();
    private readonly BadgeService _sut;

    public BadgeServiceTests()
    {
        _sut = new BadgeService(_repo.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCounts()
    {
        _repo.Setup(r => r.GetCreditsPendingCountAsync()).ReturnsAsync(12);
        _repo.Setup(r => r.GetOverdueCreditsCountAsync()).ReturnsAsync(3);
        _repo.Setup(r => r.GetWarrantiesPendingCountAsync()).ReturnsAsync(5);
        _repo.Setup(r => r.GetApprovalsPendingCountAsync()).ReturnsAsync(8);

        var result = await _sut.GetAllAsync();

        Assert.Equal(4, result.Count);
        Assert.Equal(12, result["credits-pending"]);
        Assert.Equal(3, result["overdue-credits"]);
        Assert.Equal(5, result["warranties-pending"]);
        Assert.Equal(8, result["approvals-pending"]);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnZero_WhenNothingPending()
    {
        _repo.Setup(r => r.GetCreditsPendingCountAsync()).ReturnsAsync(0);
        _repo.Setup(r => r.GetOverdueCreditsCountAsync()).ReturnsAsync(0);
        _repo.Setup(r => r.GetWarrantiesPendingCountAsync()).ReturnsAsync(0);
        _repo.Setup(r => r.GetApprovalsPendingCountAsync()).ReturnsAsync(0);

        var result = await _sut.GetAllAsync();

        Assert.All(result.Values, v => Assert.Equal(0, v));
    }

    [Fact]
    public async Task GetAllAsync_ShouldThrow_WhenRepoThrows()
    {
        _repo.Setup(r => r.GetCreditsPendingCountAsync()).ThrowsAsync(new InvalidOperationException("DB error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetAllAsync());
    }

    [Fact]
    public async Task GetAllAsync_ShouldHandlePartialCounts()
    {
        _repo.Setup(r => r.GetCreditsPendingCountAsync()).ReturnsAsync(7);
        _repo.Setup(r => r.GetOverdueCreditsCountAsync()).ReturnsAsync(0);
        _repo.Setup(r => r.GetWarrantiesPendingCountAsync()).ReturnsAsync(0);
        _repo.Setup(r => r.GetApprovalsPendingCountAsync()).ReturnsAsync(2);

        var result = await _sut.GetAllAsync();

        Assert.Equal(7, result["credits-pending"]);
        Assert.Equal(0, result["overdue-credits"]);
        Assert.Equal(0, result["warranties-pending"]);
        Assert.Equal(2, result["approvals-pending"]);
    }
}
