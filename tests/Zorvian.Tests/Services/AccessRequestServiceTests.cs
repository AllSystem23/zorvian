using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Application.DTOs.Auth;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class AccessRequestServiceTests
{
    private readonly Mock<IAccessRequestRepository> _requests = new();
    private readonly Mock<IInvitationRepository> _invitations = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<ILogger<AccessRequestService>> _logger = new();
    private readonly AccessRequestService _sut;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private const string CompanyTenant = "11111111-1111-1111-1111-111111111111";

    public AccessRequestServiceTests()
    {
        _tenant.Setup(t => t.CurrentUserId).Returns(Guid.NewGuid());
        _sut = new AccessRequestService(
            _requests.Object,
            _invitations.Object,
            _companies.Object,
            _email.Object,
            _tenant.Object,
            _logger.Object);
    }

    private static RequestAccessRequest ValidRequest(string email = "nuevo@empresa.com") =>
        new("Nuevo Usuario", email, "Empresa Nueva", "5555-1234", "Quiero acceso");

    [Fact]
    public async Task RequestAsync_WithoutName_ReturnsError()
    {
        var (success, error) = await _sut.RequestAsync(new RequestAccessRequest("  ", "a@b.com", null, null, null));

        Assert.False(success);
        Assert.NotNull(error);
        _requests.Verify(r => r.AddAsync(It.IsAny<AccessRequest>()), Times.Never);
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("a@b")]
    [InlineData("")]
    public async Task RequestAsync_WithInvalidEmail_ReturnsError(string email)
    {
        var (success, error) = await _sut.RequestAsync(new RequestAccessRequest("Nombre", email, null, null, null));

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task RequestAsync_CreatesPendingRequest()
    {
        AccessRequest? stored = null;
        _requests.Setup(r => r.AddAsync(It.IsAny<AccessRequest>()))
            .Callback<AccessRequest>(r => stored = r)
            .Returns(Task.CompletedTask);

        var (success, error) = await _sut.RequestAsync(ValidRequest());

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(stored);
        Assert.Equal(AccessRequestStatus.Pending, stored!.Status);
        Assert.Equal("Nuevo Usuario", stored.FullName);
        Assert.Equal("Empresa Nueva", stored.Company);
    }

    [Fact]
    public async Task RequestAsync_DuplicatePending_DoesNotCreateAnother()
    {
        _requests.Setup(r => r.ExistsPendingByEmailAsync(It.IsAny<string>())).ReturnsAsync(true);

        var (success, _) = await _sut.RequestAsync(ValidRequest());

        Assert.True(success);
        _requests.Verify(r => r.AddAsync(It.IsAny<AccessRequest>()), Times.Never);
    }

    [Fact]
    public async Task RequestAsync_EmailFailure_StillSucceeds()
    {
        _email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var (success, _) = await _sut.RequestAsync(ValidRequest());

        Assert.True(success);
    }

    [Fact]
    public async Task ApproveAsync_CreatesInvitationForTheCompany_AndMarksApproved()
    {
        var request = new AccessRequest
        {
            Id = Guid.NewGuid(),
            FullName = "Nuevo Usuario",
            Email = "nuevo@empresa.com",
            Company = "Empresa Nueva",
            Status = AccessRequestStatus.Pending
        };

        _requests.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);
        _companies.Setup(c => c.GetByIdForPlatformAsync(CompanyId))
            .ReturnsAsync(new Company { Id = CompanyId, Name = "Empresa Nueva", TenantId = CompanyTenant });

        Invitation? invitation = null;
        _invitations.Setup(i => i.CreateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()))
            .Callback<string, string?, string>((email, role, tenant) =>
            {
                invitation = new Invitation { Email = email, Role = role ?? "Employee", TenantId = tenant, Code = "ABCD1234" };
            })
            .ReturnsAsync(() => invitation!);

        var (success, error) = await _sut.ApproveAsync(request.Id, CompanyId, "Rrhh");

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(AccessRequestStatus.Approved, request.Status);
        Assert.Equal("ABCD1234", request.InvitationCode);
        Assert.NotNull(request.ReviewedAt);
        _invitations.Verify(i => i.CreateAsync("nuevo@empresa.com", "Rrhh", CompanyTenant), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_UnknownCompany_ReturnsError()
    {
        var request = new AccessRequest { Id = Guid.NewGuid(), Email = "a@b.com", Status = AccessRequestStatus.Pending };
        _requests.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);
        _companies.Setup(c => c.GetByIdForPlatformAsync(It.IsAny<Guid>())).ReturnsAsync((Company?)null);

        var (success, error) = await _sut.ApproveAsync(request.Id, CompanyId, null);

        Assert.False(success);
        Assert.NotNull(error);
        _invitations.Verify(i => i.CreateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ApproveAsync_AlreadyReviewed_ReturnsError()
    {
        var request = new AccessRequest { Id = Guid.NewGuid(), Email = "a@b.com", Status = AccessRequestStatus.Approved };
        _requests.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);

        var (success, error) = await _sut.ApproveAsync(request.Id, CompanyId, null);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task ApproveAsync_UnknownRequest_ReturnsError()
    {
        _requests.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((AccessRequest?)null);

        var (success, error) = await _sut.ApproveAsync(Guid.NewGuid(), CompanyId, null);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task RejectAsync_MarksRejectedWithReason()
    {
        var request = new AccessRequest { Id = Guid.NewGuid(), Email = "a@b.com", Status = AccessRequestStatus.Pending };
        _requests.Setup(r => r.GetByIdAsync(request.Id)).ReturnsAsync(request);

        var (success, _) = await _sut.RejectAsync(request.Id, "No pertenece a la organización");

        Assert.True(success);
        Assert.Equal(AccessRequestStatus.Rejected, request.Status);
        Assert.Equal("No pertenece a la organización", request.RejectionReason);
        Assert.NotNull(request.ReviewedAt);
    }

    [Fact]
    public async Task ListAsync_InvalidStatus_ReturnsEmpty()
    {
        var items = await _sut.ListAsync("basura");

        Assert.Empty(items);
        _requests.Verify(r => r.GetAllAsync(It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task ListAsync_Pending_FiltersByStatus()
    {
        _requests.Setup(r => r.GetAllAsync("pending")).ReturnsAsync(new List<AccessRequest>());

        var items = await _sut.ListAsync("Pending");

        Assert.Empty(items);
        _requests.Verify(r => r.GetAllAsync("pending"), Times.Once);
    }
}
