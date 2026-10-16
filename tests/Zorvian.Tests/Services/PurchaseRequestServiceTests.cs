using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.PurchaseRequest;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Máquina de estados de la Solicitud de Cotización (RFQ interno):
/// draft → submitted → approved/rejected, cancel desde draft/submitted.
/// Fija también totales estimados, normalización de prioridad, webhook y
/// el alcance de sucursal del listado (filtro explícito vs contexto).
/// </summary>
public sealed class PurchaseRequestServiceTests
{
    private readonly Mock<IPurchaseRequestRepository> _repo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IBranchValidator> _branchValidator = new();
    private readonly Mock<IWebhookService> _webhook = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly PurchaseRequestService _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _branch = Guid.NewGuid();
    private readonly Guid _product = Guid.NewGuid();

    public PurchaseRequestServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _repo.Setup(r => r.GenerateRequestNumberAsync(It.IsAny<Guid>()))
            .ReturnsAsync("SC-20261016-0001");
        _branchValidator
            .Setup(v => v.ResolveForWriteAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(_branch);
        _productRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new Product { Id = id, Name = "Producto", Code = "PRD-1" });

        _sut = new PurchaseRequestService(
            _repo.Object, _productRepo.Object, _branchValidator.Object,
            _webhook.Object, _tenant.Object);
    }

    private static PurchaseRequestLineDto Line(int qty = 2, decimal price = 100m) =>
        new(Guid.NewGuid(), qty, price, null);

    private static CreatePurchaseRequestRequest Request(params PurchaseRequestLineDto[] lines) =>
        new("Reposición de laptops", "high", new DateOnly(2026, 10, 30), null, "Urgente", lines.ToList());

    private static PurchaseRequest Stored(string status) => new()
    {
        Id = Guid.NewGuid(),
        RequestNumber = "SC-20261016-0007",
        Title = "Reposición de laptops",
        Priority = "normal",
        Status = status,
        BranchId = Guid.NewGuid(),
    };

    [Fact]
    public async Task Create_ComputesEstimatedTotalAndStampsDraftStatus()
    {
        var result = await _sut.CreateAsync(Request(Line(2, 100m), Line(4, 50m)));

        result.Status.Should().Be("draft");
        result.RequestNumber.Should().Be("SC-20261016-0001");
        result.Priority.Should().Be("high");
        result.EstimatedTotal.Should().Be(400m, "2×100 + 4×50");
        result.BranchId.Should().Be(_branch, "la sucursal pasa por IBranchValidator");
        result.Details.Should().HaveCount(2);
        result.Details.Sum(d => d.Subtotal).Should().Be(400m);

        _repo.Verify(r => r.AddAsync(It.Is<PurchaseRequest>(e =>
            e.Status == "draft" &&
            e.EstimatedTotal == 400m &&
            e.RequestNumber == "SC-20261016-0001")), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _webhook.Invocations.Should().ContainSingle(i =>
            i.Method.Name == nameof(IWebhookService.PublishAsync) &&
            (string)i.Arguments[1] == "purchase_request.created");
    }

    [Fact]
    public async Task Create_UnknownProduct_Throws()
    {
        _productRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Product?)null);

        var act = async () => await _sut.CreateAsync(Request(Line()));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Producto no encontrado*");
        _repo.Verify(r => r.AddAsync(It.IsAny<PurchaseRequest>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithoutLines_Throws()
    {
        var act = async () => await _sut.CreateAsync(Request());

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*al menos un producto*");
    }

    [Fact]
    public async Task Create_UnknownPriority_FallsBackToNormal()
    {
        var request = new CreatePurchaseRequestRequest(
            "Título", "critica-ya", null, null, null, new List<PurchaseRequestLineDto> { Line() });

        var result = await _sut.CreateAsync(request);

        result.Priority.Should().Be("normal", "prioridades fuera de low/normal/high/urgent se normalizan");
    }

    [Fact]
    public async Task Create_MissingBranch_Throws()
    {
        _branchValidator.Setup(v => v.ResolveForWriteAsync(It.IsAny<Guid?>()))
            .ReturnsAsync((Guid?)null);

        var act = async () => await _sut.CreateAsync(Request(Line()));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*sucursal*");
        _repo.Verify(r => r.AddAsync(It.IsAny<PurchaseRequest>()), Times.Never);
    }

    [Fact]
    public async Task Create_InvalidBranch_PropagatesValidatorError()
    {
        _branchValidator.Setup(v => v.ResolveForWriteAsync(It.IsAny<Guid?>()))
            .ThrowsAsync(new InvalidOperationException("La sucursal no pertenece a la compañía operativa"));

        var act = async () => await _sut.CreateAsync(Request(Line()));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*no pertenece a la compañía*");
    }

    [Fact]
    public async Task Submit_FromDraft_MovesToSubmitted()
    {
        var entity = Stored("draft");
        _repo.Setup(r => r.GetByIdAsync(entity.Id)).ReturnsAsync(entity);

        var result = await _sut.SubmitAsync(entity.Id);

        result.Status.Should().Be("submitted");
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Submit_FromApproved_Throws()
    {
        var entity = Stored("approved");
        _repo.Setup(r => r.GetByIdAsync(entity.Id)).ReturnsAsync(entity);

        var act = async () => await _sut.SubmitAsync(entity.Id);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*borrador*");
    }

    [Fact]
    public async Task Approve_FromSubmitted_MovesToApproved()
    {
        var entity = Stored("submitted");
        _repo.Setup(r => r.GetByIdAsync(entity.Id)).ReturnsAsync(entity);

        var result = await _sut.ApproveAsync(entity.Id);

        result.Status.Should().Be("approved");
        _webhook.Invocations.Should().ContainSingle(i =>
            i.Method.Name == nameof(IWebhookService.PublishAsync) &&
            (string)i.Arguments[1] == "purchase_request.approved");
    }

    [Fact]
    public async Task Approve_FromDraft_Throws()
    {
        var entity = Stored("draft");
        _repo.Setup(r => r.GetByIdAsync(entity.Id)).ReturnsAsync(entity);

        var act = async () => await _sut.ApproveAsync(entity.Id);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*enviada*");
    }

    [Fact]
    public async Task Reject_FromSubmitted_SetsReasonAndPublishes()
    {
        var entity = Stored("submitted");
        _repo.Setup(r => r.GetByIdAsync(entity.Id)).ReturnsAsync(entity);

        var result = await _sut.RejectAsync(entity.Id, new RejectPurchaseRequestRequest("Sin presupuesto"));

        result.Status.Should().Be("rejected");
        result.RejectionReason.Should().Be("Sin presupuesto");
        _webhook.Invocations.Should().ContainSingle(i =>
            i.Method.Name == nameof(IWebhookService.PublishAsync) &&
            (string)i.Arguments[1] == "purchase_request.rejected");
    }

    [Fact]
    public async Task Reject_FromApproved_Throws()
    {
        var entity = Stored("approved");
        _repo.Setup(r => r.GetByIdAsync(entity.Id)).ReturnsAsync(entity);

        var act = async () => await _sut.RejectAsync(entity.Id, new RejectPurchaseRequestRequest(null));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*enviada*");
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("submitted")]
    public async Task Cancel_FromDraftOrSubmitted_Allows(string status)
    {
        var entity = Stored(status);
        _repo.Setup(r => r.GetByIdAsync(entity.Id)).ReturnsAsync(entity);

        var result = await _sut.CancelAsync(entity.Id);

        result.Status.Should().Be("cancelled");
    }

    [Fact]
    public async Task Cancel_FromApproved_Throws()
    {
        var entity = Stored("approved");
        _repo.Setup(r => r.GetByIdAsync(entity.Id)).ReturnsAsync(entity);

        var act = async () => await _sut.CancelAsync(entity.Id);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*estado 'approved'*");
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PurchaseRequest?)null);

        (await _sut.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task GetFiltered_WithoutExplicitBranch_UsesContextBranch()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns(_branch);
        _repo.Setup(r => r.GetFilteredAsync(null, null, null, It.IsAny<Guid>(), 1, 20))
            .ReturnsAsync((new List<PurchaseRequest>(), 0));

        await _sut.GetFilteredAsync(null, null, null, null, 1, 20);

        _repo.Verify(r => r.GetFilteredAsync(null, null, null, _branch, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetFiltered_ExplicitBranch_WinsOverContext()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns(Guid.NewGuid());
        _repo.Setup(r => r.GetFilteredAsync(null, null, null, It.IsAny<Guid>(), 1, 20))
            .ReturnsAsync((new List<PurchaseRequest>(), 0));

        await _sut.GetFilteredAsync(null, null, null, _branch, 1, 20);

        _repo.Verify(r => r.GetFilteredAsync(null, null, null, _branch, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetFiltered_NoContextBranch_FallsBackToEmpty_AllBranches()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns((Guid?)null);
        _repo.Setup(r => r.GetFilteredAsync(null, null, null, It.IsAny<Guid>(), 1, 20))
            .ReturnsAsync((new List<PurchaseRequest>(), 0));

        await _sut.GetFilteredAsync(null, null, null, null, 1, 20);

        _repo.Verify(r => r.GetFilteredAsync(null, null, null, Guid.Empty, 1, 20), Times.Once);
    }
}
