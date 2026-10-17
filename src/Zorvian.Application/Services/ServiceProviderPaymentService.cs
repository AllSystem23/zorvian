using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.ServiceProviderPayment;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

/// <summary>
/// Pago a Prestadores de Servicios (colaborador NO de planilla): pago quincenal por
/// recaudo en efectivo con comisión porcentual por línea, incentivo fijo por meta
/// cumplida (evaluada por total, abierta a modo individual) y deducciones. La
/// referencia/fecha del acuerdo es OPCIONAL (no se exige); se conserva para el
/// comprobante cuando el usuario la registra. Edición manual en borrador;
/// historial por agente y período. Máquina de estados: draft → approved → paid;
/// cancelled desde draft/approved.
/// </summary>
public sealed class ServiceProviderPaymentService
{
    /// <summary>Tipos de deducción válidos. La referencia del acuerdo previo es
    /// OPCIONAL para todos los tipos (el usuario puede registrarla para el comprobante,
    /// pero el pago no se bloquea si está vacía).</summary>
    private static readonly HashSet<string> ValidDeductionTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "advance_first_half", "store_credit", "salary_advance",
            "cash_loan", "inss", "other",
        };

    private static readonly string[] MonthNames =
        { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

    private readonly IServiceProviderPaymentRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ITenantContext _tenant;
    private readonly IWebhookService _webhook;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly ILogger<ServiceProviderPaymentService> _logger;

    public ServiceProviderPaymentService(
        IServiceProviderPaymentRepository repo,
        IEmployeeRepository employeeRepo,
        ITenantContext tenant,
        IWebhookService webhook,
        IAutoAccountingService autoAccounting,
        ILogger<ServiceProviderPaymentService> logger)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _tenant = tenant;
        _webhook = webhook;
        _autoAccounting = autoAccounting;
        _logger = logger;
    }

    public async Task<ServiceProviderPaymentResponse> CreateAsync(CreateServiceProviderPaymentRequest request)
    {
        var employee = await GetServiceProviderAsync(request.EmployeeId);
        ValidatePeriod(request.PeriodYear, request.PeriodMonth, request.PeriodHalf);
        await EnsureNoDuplicateAsync(request.EmployeeId, request.PeriodYear, request.PeriodMonth, request.PeriodHalf);
        ValidateLines(request.Lines);
        ValidateDeductions(request.Deductions);

        var companyId = RequireCompanyId();

        var entity = new ServiceProviderPayment
        {
            PaymentNumber = await _repo.GeneratePaymentNumberAsync(companyId),
            EmployeeId = employee.Id,
            PeriodYear = request.PeriodYear,
            PeriodMonth = request.PeriodMonth,
            PeriodHalf = request.PeriodHalf,
            PaymentDate = request.PaymentDate,
            GoalEvaluationMode = NormalizeGoalMode(request.GoalEvaluationMode),
            GoalBonusAmount = request.GoalBonusAmount,
            OtherIncome = request.OtherIncome,
            Notes = request.Notes,
            Status = "draft",
            Lines = BuildLines(request.Lines),
            Deductions = BuildDeductions(request.Deductions),
        };

        Recompute(entity);

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(entity.TenantId, "service_provider_payment.created",
            new { PaymentId = entity.Id, PaymentNumber = entity.PaymentNumber, EmployeeId = entity.EmployeeId });

        return MapToResponse(entity, employee);
    }

    public async Task<ServiceProviderPaymentResponse> UpdateAsync(Guid id, UpdateServiceProviderPaymentRequest request)
    {
        var (entity, employee) = await GetRequiredWithEmployeeAsync(id);
        if (entity.Status != "draft")
            throw new InvalidOperationException(
                $"Solo se puede editar un pago en borrador (estado actual: {entity.Status}).");

        ValidateLines(request.Lines);
        ValidateDeductions(request.Deductions);

        entity.PaymentDate = request.PaymentDate;
        entity.GoalEvaluationMode = NormalizeGoalMode(request.GoalEvaluationMode);
        entity.GoalBonusAmount = request.GoalBonusAmount;
        entity.OtherIncome = request.OtherIncome;
        entity.Notes = request.Notes;

        // Edición manual completa: se reconstruyen líneas y deducciones.
        entity.Lines = BuildLines(request.Lines);
        entity.Deductions = BuildDeductions(request.Deductions);
        Recompute(entity);

        await _repo.SaveChangesAsync();
        return MapToResponse(entity, employee);
    }

    public async Task<ServiceProviderPaymentResponse> ApproveAsync(Guid id)
    {
        var (entity, employee) = await GetRequiredWithEmployeeAsync(id);
        if (entity.Status != "draft")
            throw new InvalidOperationException(
                $"Solo se puede aprobar un pago en borrador (estado actual: {entity.Status}).");

        entity.Status = "approved";
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(entity.TenantId, "service_provider_payment.approved",
            new { PaymentId = entity.Id, PaymentNumber = entity.PaymentNumber });

        return MapToResponse(entity, employee);
    }

    public async Task<ServiceProviderPaymentResponse> MarkPaidAsync(Guid id, MarkServiceProviderPaymentPaidRequest request)
    {
        var (entity, employee) = await GetRequiredWithEmployeeAsync(id);
        if (entity.Status != "approved")
            throw new InvalidOperationException(
                $"Solo se puede marcar como pagado un pago aprobado (estado actual: {entity.Status}).");

        entity.Status = "paid";
        entity.PaidAt = DateTime.UtcNow;
        entity.PaymentReference = request.PaymentReference;
        await _repo.SaveChangesAsync();

        // Asiento contable al pagar (Débito Gasto de Servicios / Crédito Efectivo,
        // con abono de deducciones a Adelantos o INSS por pagar). Best-effort: si faltan
        // enlaces de cuentas o el período está cerrado, NO se bloquea el pago — se omite
        // el asiento y se registra la advertencia para que contabilidad lo configure.
        if (entity.TotalAccumulated > 0)
        {
            try
            {
                // La sucursal del asiento = sucursal DEL PRESTADOR (la transacción le
                // pertenece a esa sucursal para la reportería financiera por branch);
                // fallback al contexto si el prestador no tiene sucursal asignada.
                await _autoAccounting.GenerateServiceProviderPaymentEntryAsync(
                    entity, employee.BranchId ?? _tenant.ResolveBranchId());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    "Pago {PaymentNumber} marcado como pagado SIN asiento contable: {Reason}. Configure los enlaces de cuentas (ServiceProviderPayment/ServicesExpense, /Cash, /Advances, /WithholdingPayable).",
                    entity.PaymentNumber, ex.Message);
            }
        }

        await _webhook.PublishAsync(entity.TenantId, "service_provider_payment.paid",
            new { PaymentId = entity.Id, PaymentNumber = entity.PaymentNumber, entity.NetAmount });

        return MapToResponse(entity, employee);
    }

    public async Task<ServiceProviderPaymentResponse> CancelAsync(Guid id)
    {
        var (entity, employee) = await GetRequiredWithEmployeeAsync(id);
        if (entity.Status is not ("draft" or "approved"))
            throw new InvalidOperationException(
                $"No se puede cancelar un pago en estado {entity.Status}.");

        entity.Status = "cancelled";
        await _repo.SaveChangesAsync();

        await _webhook.PublishAsync(entity.TenantId, "service_provider_payment.cancelled",
            new { PaymentId = entity.Id, PaymentNumber = entity.PaymentNumber });

        return MapToResponse(entity, employee);
    }

    public async Task<ServiceProviderPaymentResponse?> GetByIdAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        var employee = entity.Employee ?? await _employeeRepo.GetByIdAsync(entity.EmployeeId);
        return employee is null ? null : MapToResponse(entity, employee);
    }

    public async Task<(List<ServiceProviderPaymentResponse> Items, int Total)> GetFilteredAsync(
        string? search, Guid? employeeId, int? year, int? month, int? half,
        string? status, Guid? branchId, int page, int pageSize)
    {
        // Reportería por sucursal: el filtro EXPLÍCITO del caller gana; si no viene,
        // se usa la sucursal del contexto (header X-Branch-Id). Vacío = todas.
        var scopeBranch = branchId ?? _tenant.ResolveBranchId() ?? Guid.Empty;
        var (items, total) = await _repo.GetFilteredAsync(
            search, employeeId, year, month, half, status, scopeBranch, page, pageSize);
        return ((items ?? new List<ServiceProviderPayment>())
            .Select(e => MapToResponse(e, e.Employee ?? new Employee())).ToList(), total);
    }

    // ── Reglas de negocio ──

    private static void ValidatePeriod(int year, int month, int half)
    {
        if (year < 2000 || year > 2100)
            throw new InvalidOperationException($"Año fuera de rango: {year}.");
        if (month is < 1 or > 12)
            throw new InvalidOperationException($"Mes inválido: {month}. Debe ser 1-12.");
        if (half is not (1 or 2))
            throw new InvalidOperationException($"Quincena inválida: {half}. Debe ser 1 (primera) o 2 (segunda).");
    }

    private static void ValidateLines(List<ServiceProviderPaymentLineDto> lines)
    {
        if (lines is null || lines.Count == 0)
            throw new InvalidOperationException("Debe indicar al menos una línea de recaudo.");

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.LineName))
                throw new InvalidOperationException("Cada línea de recaudo requiere un nombre (ej. OTROS, TELÉFONO).");
            if (line.CollectedAmount < 0)
                throw new InvalidOperationException($"El recaudo de la línea '{line.LineName}' no puede ser negativo.");
            if (line.ContractedAmount < 0)
                throw new InvalidOperationException($"El monto en contrato de la línea '{line.LineName}' no puede ser negativo.");
            if (line.CommissionRate is < 0 or > 100)
                throw new InvalidOperationException($"La comisión de la línea '{line.LineName}' debe estar entre 0 y 100%.");
        }
    }

    private static void ValidateDeductions(List<ServiceProviderPaymentDeductionDto> deductions)
    {
        if (deductions is null) return;

        foreach (var d in deductions)
        {
            if (!ValidDeductionTypes.Contains(d.DeductionType ?? string.Empty))
                throw new InvalidOperationException(
                    $"Tipo de deducción desconocido: '{d.DeductionType}'. " +
                    "Use advance_first_half, store_credit, salary_advance, cash_loan, inss u other.");
            if (d.Amount <= 0)
                throw new InvalidOperationException(
                    $"La deducción '{d.Description ?? d.DeductionType}' debe ser mayor a cero.");
            // La referencia/fecha del acuerdo es OPCIONAL: no se exige para ningún tipo
            // (si se registra, se muestra en el comprobante de pago).
        }
    }

    private static string NormalizeGoalMode(string? mode) =>
        string.Equals(mode, "individual", StringComparison.OrdinalIgnoreCase) ? "individual" : "total";

    private static List<ServiceProviderPaymentLine> BuildLines(List<ServiceProviderPaymentLineDto> lines) =>
        lines.Select(l => new ServiceProviderPaymentLine
        {
            LineName = l.LineName.Trim(),
            CollectedAmount = l.CollectedAmount,
            ContractedAmount = l.ContractedAmount,
            CommissionRate = l.CommissionRate,
            // Monto de comisión calculado SIEMPRE por el servidor (no se confía en el cliente).
            CommissionAmount = Math.Round(l.CollectedAmount * l.CommissionRate / 100m, 2),
        }).ToList();

    private static List<ServiceProviderPaymentDeduction> BuildDeductions(List<ServiceProviderPaymentDeductionDto> deductions) =>
        deductions.Select(d => new ServiceProviderPaymentDeduction
        {
            DeductionType = d.DeductionType.ToLowerInvariant(),
            Description = d.Description,
            Amount = d.Amount,
            AgreementReference = d.AgreementReference,
            AgreementDate = d.AgreementDate,
        }).ToList();

    /// <summary>Recalcula metas por línea, totales y monto neto. Fuente única de verdad.</summary>
    private static void Recompute(ServiceProviderPayment payment)
    {
        foreach (var line in payment.Lines)
        {
            line.LineGoalMet = line.ContractedAmount > 0 && line.CollectedAmount >= line.ContractedAmount;
        }

        payment.TotalCollected = payment.Lines.Sum(l => l.CollectedAmount);
        payment.TotalContracted = payment.Lines.Sum(l => l.ContractedAmount);
        payment.TotalCommission = payment.Lines.Sum(l => l.CommissionAmount);

        payment.GoalMet = payment.GoalEvaluationMode == "individual"
            // Individual: todas las líneas con meta deben cumplirla (y debe haber al menos una).
            ? payment.Lines.Any(l => l.ContractedAmount > 0) &&
              payment.Lines.Where(l => l.ContractedAmount > 0).All(l => l.LineGoalMet)
            // Total: el recaudo total alcanza el contrato total.
            : payment.TotalContracted > 0 && payment.TotalCollected >= payment.TotalContracted;

        // Bono fijo solo si la meta se cumple (si no, no paga nada de meta).
        var goalBonus = payment.GoalMet ? payment.GoalBonusAmount : 0m;
        payment.TotalAccumulated = goalBonus + payment.TotalCommission + payment.OtherIncome;
        payment.TotalDeductions = payment.Deductions.Sum(d => d.Amount);
        payment.NetAmount = payment.TotalAccumulated - payment.TotalDeductions;
    }

    // ── Helpers de carga ──

    private async Task<Employee> GetServiceProviderAsync(Guid employeeId)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId)
            ?? throw new InvalidOperationException($"Colaborador no encontrado: {employeeId}.");

        // El pago a prestador es SOLO para colaboradores de tipo service_provider.
        // Un empleado de planilla se paga vía PayrollRun, no por este flujo.
        if (!string.Equals(employee.CollaboratorType, "service_provider", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Este flujo es solo para prestadores de servicios (CollaboratorType = service_provider). " +
                "El colaborador seleccionado está contratado en planilla: use el módulo de Nómina.");

        return employee;
    }

    private async Task EnsureNoDuplicateAsync(Guid employeeId, int year, int month, int half)
    {
        var existing = await _repo.GetByPeriodAsync(employeeId, year, month, half);
        if (existing is not null)
            throw new InvalidOperationException(
                $"Ya existe un pago ({existing.PaymentNumber}) para este prestador en " +
                $"{PeriodLabel(year, month, half)} con estado {existing.Status}. Cancele el existente o edítelo.");
    }

    private async Task<(ServiceProviderPayment Entity, Employee Employee)> GetRequiredWithEmployeeAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Pago no encontrado: {id}.");
        var employee = entity.Employee ?? await _employeeRepo.GetByIdAsync(entity.EmployeeId)
            ?? throw new InvalidOperationException($"Colaborador no encontrado: {entity.EmployeeId}.");
        return (entity, employee);
    }

    private Guid RequireCompanyId()
    {
        if (!Guid.TryParse(_tenant.TenantId?.ToString(), out var companyId))
            throw new InvalidOperationException("Tenant not configured");
        return companyId;
    }

    // ── Mapeo ──

    public static string PeriodLabel(int year, int month, int half) =>
        $"Quincena {half} · {MonthNames[Math.Clamp(month, 1, 12) - 1]} {year}";

    private static ServiceProviderPaymentResponse MapToResponse(ServiceProviderPayment entity, Employee employee) =>
        new(
            entity.Id,
            entity.PaymentNumber,
            entity.EmployeeId,
            $"{employee.FirstName} {employee.LastName}".Trim(),
            employee.EmployeeCode,
            employee.BranchId,
            employee.Branch?.Name,
            entity.PeriodYear,
            entity.PeriodMonth,
            entity.PeriodHalf,
            PeriodLabel(entity.PeriodYear, entity.PeriodMonth, entity.PeriodHalf),
            entity.PaymentDate,
            entity.Status,
            entity.GoalEvaluationMode,
            entity.GoalMet,
            entity.GoalBonusAmount,
            entity.OtherIncome,
            entity.TotalCollected,
            entity.TotalContracted,
            entity.TotalCommission,
            entity.TotalAccumulated,
            entity.TotalDeductions,
            entity.NetAmount,
            entity.Notes,
            entity.CreatedAt,
            entity.PaidAt,
            entity.PaymentReference,
            entity.Lines.Select(l => new ServiceProviderPaymentLineResponse(
                l.Id, l.LineName, l.CollectedAmount, l.ContractedAmount,
                l.CommissionRate, l.CommissionAmount, l.LineGoalMet)).ToList(),
            entity.Deductions.Select(d => new ServiceProviderPaymentDeductionResponse(
                d.Id, d.DeductionType, d.Description, d.Amount,
                d.AgreementReference, d.AgreementDate)).ToList());
}
