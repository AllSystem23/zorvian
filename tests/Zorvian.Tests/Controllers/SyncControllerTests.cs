using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Web.Controllers;

namespace Zorvian.Tests.Controllers;

public sealed class SyncControllerTests
{
    private readonly Mock<ISyncService> _syncService = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly SyncController _sut;
    private readonly string _tenantId;

    public SyncControllerTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        _tenant.Setup(t => t.TenantId).Returns(new TenantId(Guid.Parse(_tenantId)));
        _sut = new SyncController(_syncService.Object, _tenant.Object);
    }

    [Fact]
    public async Task Pull_WithValidEntity_ShouldReturnOk()
    {
        var changes = new List<SyncChangeDto>
        {
            new("Product", "p1", "created", "{}", DateTime.UtcNow),
        };
        _syncService
            .Setup(s => s.PullAsync(It.IsAny<SyncPullRequest>(), _tenantId))
            .ReturnsAsync(new SyncPullResponse(changes, DateTime.UtcNow));

        var result = await _sut.Pull("Product", null, 500);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeAssignableTo<SyncPullResponse>().Subject;
        response.Changes.Should().HaveCount(1);
    }

    [Fact]
    public async Task Pull_WithEmptyEntity_ShouldReturnBadRequest()
    {
        var result = await _sut.Pull("", null, 500);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Pull_WithNullEntity_ShouldReturnBadRequest()
    {
        var result = await _sut.Pull(null!, null, 500);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Pull_WithSince_ShouldPassSinceToService()
    {
        var since = DateTime.UtcNow.AddHours(-1);
        _syncService
            .Setup(s => s.PullAsync(It.Is<SyncPullRequest>(r => r.Since == since), _tenantId))
            .ReturnsAsync(new SyncPullResponse([], DateTime.UtcNow));

        var result = await _sut.Pull("Product", since, 500);

        result.Should().BeOfType<OkObjectResult>();
        _syncService.Verify(s => s.PullAsync(It.Is<SyncPullRequest>(r => r.Since == since), _tenantId), Times.Once);
    }

    [Fact]
    public async Task Pull_ShouldPassTakeToService()
    {
        _syncService
            .Setup(s => s.PullAsync(It.Is<SyncPullRequest>(r => r.Take == 100), _tenantId))
            .ReturnsAsync(new SyncPullResponse([], DateTime.UtcNow));

        var result = await _sut.Pull("Product", null, 100);

        result.Should().BeOfType<OkObjectResult>();
        _syncService.Verify(s => s.PullAsync(It.Is<SyncPullRequest>(r => r.Take == 100), _tenantId), Times.Once);
    }

    [Fact]
    public async Task Push_WithValidMutations_ShouldReturnOk()
    {
        var mutations = new List<SyncPushRequest>
        {
            new("Product", "p1", "created", "{}", "c1"),
        };
        _syncService
            .Setup(s => s.PushAsync(mutations, _tenantId))
            .ReturnsAsync(new SyncPushResponse([], DateTime.UtcNow));

        var result = await _sut.Push(mutations);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeAssignableTo<SyncPushResponse>().Subject;
        response.Conflicts.Should().BeEmpty();
    }

    [Fact]
    public async Task Push_WithEmptyList_ShouldReturnBadRequest()
    {
        var result = await _sut.Push([]);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Push_WithNullList_ShouldReturnBadRequest()
    {
        var result = await _sut.Push(null!);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Status_ShouldReturnOkWithServerTime()
    {
        var result = _sut.Status();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }
}
