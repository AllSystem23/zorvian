using Zorvian.Application.DTOs.PurchaseRequest;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

/// <summary>
/// Solicitud de Cotización (RFQ interno): un área pide a Compras cotizar productos.
/// Máquina de estados: draft → submitted → approved/rejected; cancelled desde
/// draft o submitted. No consume presupuesto (es pre-compra), por lo que NO pasa
/// por <see cref="IApprovalEngine"/>: la aprobación es un paso explícito del flujo.
/// </summary>
public sealed class PurchaseRequestService
{
    private static readonly HashSet<string> ValidPriorities =
        new(StringComparer.OrdinalIgnoreCase) { "low", "normal", "high", "urgent" };

    private readonly IPurchaseRequestRepository _repo;
    private readonly IProductRepository _productRepo;
    private readonly IBranchValidator _branchValidator;
    private readonly IWebhookService _webhook;
    private readonly ITenantContext _tenant;

    public PurchaseRequestService(
        IPurchaseRequestRepository repo,
        IProductRepository productRepo,
        IBranchValidator branchValidator,
        IWebhookService webhook,
        ITenantContext tenant)
    {
        _repo = repo;
        _productRepo = productRepo;
        _branchValidator = branchValidator;
        _webhook = webhook;
        _tenant = tenant;
    }

    public async Task<PurchaseRequestResponse> CreateAsync(CreatePurchaseRequestRequest request)
    {
        ValidateLines(request.Title, request.Details);

        // Sucursal obligatoria: el Include(Branch) del repositorio es INNER JOIN
        // (FK requerida), un BranchId vacío volvería invisible la solicitud.
        var branchId = await _branchValidator.ResolveForWriteAsync(request.BranchId)
            ?? throw new InvalidOperationException("Debe indicar la sucursal de la solicitud.");
        var companyId = RequireCompanyId();

        decimal estimatedTotal = 0;
        var details = new List<PurchaseRequestDetail>(request.Details.Count);
        foreach (var line in request.Details)
        {
            var product = await _productRepo.GetByIdAsync(line.ProductId)
                ?? throw new InvalidOperationException($"Producto no encontrado: {line.ProductId}");

            var subtotal = line.Quantity * line.EstimatedUnitPrice;
            estimatedTotal += subtotal;
            details.Add(new PurchaseRequestDetail
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                EstimatedUnitPrice = line.EstimatedUnitPrice,
                Subtotal = subtotal,
                Notes = line.Notes,
                BranchId = branchId,
            });
        }

        var entity = new PurchaseRequest
        {
            RequestNumber = await _repo.GenerateRequestNumberAsync(companyId),
            Title = request.Title.Trim(),
            Priority = NormalizePriority(request.Priority),
            NeededByDate = request.NeededByDate,
            Status = "draft",
            EstimatedTotal = estimatedTotal,
            Notes = request.Notes,
            BranchId = branchId,
            Details = details,
        };

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(entity.TenantId, "purchase_request.created",
            new { PurchaseRequestId = entity.Id, RequestNumber = entity.RequestNumber, Title = entity.Title });

        return MapToResponse(entity);
    }

    public async Task<PurchaseRequestResponse> SubmitAsync(Guid id)
    {
        var entity = await GetRequiredAsync(id);
        if (entity.Status != "draft")
            throw new InvalidOperationException(
                $"Solo se puede enviar una solicitud en borrador (estado actual: {entity.Status}).");

        entity.Status = "submitted";
        await _repo.SaveChangesAsync();
        return MapToResponse(entity);
    }

    public async Task<PurchaseRequestResponse> ApproveAsync(Guid id)
    {
        var entity = await GetRequiredAsync(id);
        if (entity.Status != "submitted")
            throw new InvalidOperationException(
                $"Solo se puede aprobar una solicitud enviada (estado actual: {entity.Status}).");

        entity.Status = "approved";
        entity.RejectionReason = null;
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(entity.TenantId, "purchase_request.approved",
            new { PurchaseRequestId = entity.Id, RequestNumber = entity.RequestNumber });

        return MapToResponse(entity);
    }

    public async Task<PurchaseRequestResponse> RejectAsync(Guid id, RejectPurchaseRequestRequest request)
    {
        var entity = await GetRequiredAsync(id);
        if (entity.Status != "submitted")
            throw new InvalidOperationException(
                $"Solo se puede rechazar una solicitud enviada (estado actual: {entity.Status}).");

        entity.Status = "rejected";
        entity.RejectionReason = request.Reason;
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(entity.TenantId, "purchase_request.rejected",
            new { PurchaseRequestId = entity.Id, RequestNumber = entity.RequestNumber, Reason = request.Reason });

        return MapToResponse(entity);
    }

    public async Task<PurchaseRequestResponse> CancelAsync(Guid id)
    {
        var entity = await GetRequiredAsync(id);
        if (entity.Status is not ("draft" or "submitted"))
            throw new InvalidOperationException(
                $"No se puede cancelar una solicitud con estado '{entity.Status}'.");

        entity.Status = "cancelled";
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(entity.TenantId, "purchase_request.cancelled",
            new { PurchaseRequestId = entity.Id, RequestNumber = entity.RequestNumber });

        return MapToResponse(entity);
    }

    public async Task<PurchaseRequestResponse?> GetByIdAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        return entity is null ? null : MapToResponse(entity);
    }

    public async Task<(List<PurchaseRequestResponse> Items, int Total)> GetFilteredAsync(
        string? search, string? status, string? priority,
        Guid? branchId, int page, int pageSize)
    {
        var effectiveBranchId = branchId ?? _tenant.ResolveBranchId() ?? Guid.Empty;
        var (items, total) = await _repo.GetFilteredAsync(
            search, status, priority, effectiveBranchId, page, pageSize);
        return (items.Select(MapToResponse).ToList(), total);
    }

    private async Task<PurchaseRequest> GetRequiredAsync(Guid id) =>
        await _repo.GetByIdAsync(id)
        ?? throw new KeyNotFoundException("Solicitud de cotización no encontrada");

    private static void ValidateLines(string? title, List<PurchaseRequestLineDto>? details)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("El título de la solicitud es obligatorio.");
        if (details is null || details.Count == 0)
            throw new InvalidOperationException("La solicitud debe incluir al menos un producto.");
        if (details.Any(d => d.Quantity <= 0))
            throw new InvalidOperationException("Todas las cantidades deben ser mayores a cero.");
    }

    private static string NormalizePriority(string? priority) =>
        !string.IsNullOrWhiteSpace(priority) && ValidPriorities.Contains(priority.Trim())
            ? priority.Trim().ToLowerInvariant()
            : "normal";

    private Guid RequireCompanyId()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId) || companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant inválido");
        return companyId;
    }

    private static PurchaseRequestResponse MapToResponse(PurchaseRequest r) => new(
        r.Id, r.RequestNumber, r.Title, r.Priority, r.NeededByDate, r.Status,
        r.RejectionReason, r.EstimatedTotal, r.Notes,
        r.BranchId, r.Branch?.Name, r.CreatedBy, r.CreatedAt,
        r.Details.Select(d => new PurchaseRequestDetailResponse(
            d.ProductId, d.Product?.Name, d.Product?.Code,
            d.Quantity, d.EstimatedUnitPrice, d.Subtotal, d.Notes)).ToList());
}
