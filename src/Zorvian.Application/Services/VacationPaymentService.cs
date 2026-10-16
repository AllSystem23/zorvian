using Microsoft.Extensions.Logging;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class VacationPaymentService : IVacationPaymentService
{
    private readonly IVacationRepository _vacationRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ICountryTaxConfigRepository _taxConfigRepo;
    private readonly IPayrollRepository _payrollRepo;
    private readonly ITenantContext _tenant;
    private readonly ILogger<VacationPaymentService> _logger;

    public VacationPaymentService(
        IVacationRepository vacationRepo,
        IEmployeeRepository employeeRepo,
        ICountryTaxConfigRepository taxConfigRepo,
        IPayrollRepository payrollRepo,
        ITenantContext tenant,
        ILogger<VacationPaymentService> logger)
    {
        _vacationRepo = vacationRepo;
        _employeeRepo = employeeRepo;
        _taxConfigRepo = taxConfigRepo;
        _payrollRepo = payrollRepo;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<VacationPaymentReceipt> PayVacationAsync(
        Guid vacationRequestId, DateOnly? paymentDate = null, string? observations = null, string? paidBy = null)
    {
        // 1. Load the vacation request
        var request = await _vacationRepo.GetByIdAsync(vacationRequestId)
            ?? throw new KeyNotFoundException("Solicitud de vacaciones no encontrada");

        if (request.Status != "approved")
            throw new InvalidOperationException("La solicitud debe estar aprobada para procesar el pago");

        // 2. Check if already paid
        var existingReceipt = await _vacationRepo.GetReceiptByRequestIdAsync(vacationRequestId);
        if (existingReceipt != null)
            throw new InvalidOperationException($"Esta solicitud ya fue pagada con el recibo {existingReceipt.ReceiptNumber}");

        // 2.5. Anti doble pago: si una corrida de nómina NO cancelada cubre el período de estas
        // vacaciones, la corrida ya les incluye vacationPay al aprobarse — pagar por recibo RV
        // además duplicaría el pago. Se debe cancelar la corrida (revierte las vacaciones) o
        // terminar el ciclo de nómina primero.
        var periods = await _payrollRepo.GetPeriodsAsync(null) ?? new List<PayrollPeriod>();
        foreach (var period in periods.Where(p => request.StartDate <= p.EndDate && request.EndDate >= p.StartDate))
        {
            var runs = await _payrollRepo.GetRunsAsync(period.Id) ?? new List<PayrollRun>();
            if (runs.Any(r => r.Status != "cancelled"))
                throw new InvalidOperationException(
                    $"El período '{period.Name}' tiene una corrida de nómina activa que incluye estas vacaciones. " +
                    "Cancele la corrida (revierte el consumo de saldo) o termine el ciclo de nómina antes de pagar por recibo.");
        }

        // 3. Load employee
        var employee = await _employeeRepo.GetByIdAsync(request.EmployeeId)
            ?? throw new KeyNotFoundException("Empleado no encontrado");

        // 4. Load country tax config for INSS rate
        var taxConfig = await _taxConfigRepo.GetByCountryCodeAsync(employee.CountryCode);

        // 5. Calculate salary
        var monthlySalary = employee.Salary ?? 0;
        var dailySalary = Math.Round(monthlySalary / 30m, 2);
        var businessDays = request.BusinessDays > 0 ? request.BusinessDays : request.TotalDays;
        var vacationPay = Math.Round(businessDays * dailySalary, 2);

        // 6. Calculate INSS employee deduction (7% NIC, varies by country)
        var inssRate = taxConfig?.InssEmployeeRate ?? 0.07m;
        var inssAmount = Math.Round(vacationPay * inssRate, 2);

        // 7. Determine payment date (default: end of vacation period or today)
        var payDate = paymentDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var returnDate = request.EndDate.AddDays(1);

        // 8. Generate sequential receipt number
        var receiptNumber = await GenerateReceiptNumberAsync();

        // 9. Create the receipt
        var receipt = new VacationPaymentReceipt
        {
            Id = Guid.NewGuid(),
            VacationRequestId = vacationRequestId,
            EmployeeId = employee.Id,
            ReceiptNumber = receiptNumber,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ReturnDate = returnDate,
            BusinessDays = businessDays,
            DailySalary = dailySalary,
            MonthlySalary = monthlySalary,
            VacationPay = vacationPay,
            InssEmployeeAmount = inssAmount,
            InssEmployeeRate = inssRate,
            CountryCode = employee.CountryCode,
            Currency = taxConfig?.Currency ?? "NIO",
            PaymentDate = payDate,
            Observations = observations ?? $"Pago correspondiente a {businessDays:0.##} días de vacaciones",
            PaidBy = paidBy,
            Status = "paid",
            TenantId = _tenant.TenantId.ToString(),
        };

        // 10. Mark vacation request as taken
        request.Status = "taken";

        // 11. Recalcula el espejo de LeaveBalances (idempotente — ver LeaveBalanceSync).
        // Best-effort: un fallo aquí no debe invalidar el pago ya emitido.
        try
        {
            await LeaveBalanceSync.SyncAsync(_vacationRepo, _tenant, employee.Id, request.StartDate.Year);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync LeaveBalances for employee {EmployeeId} year {Year}", employee.Id, request.StartDate.Year);
        }

        // 12. Save
        await _vacationRepo.AddReceiptAsync(receipt);
        await _vacationRepo.SaveChangesAsync();

        _logger.LogInformation(
            "Vacation payment processed: Receipt {ReceiptNumber} for employee {EmployeeId}, {Days} days, Net {Net}",
            receiptNumber, employee.Id, businessDays, receipt.NetPay);

        return receipt;
    }

    public async Task<VacationPaymentReceipt?> GetReceiptByIdAsync(Guid id) =>
        await _vacationRepo.GetReceiptByIdAsync(id);

    public async Task<VacationPaymentReceipt?> GetReceiptByRequestIdAsync(Guid vacationRequestId) =>
        await _vacationRepo.GetReceiptByRequestIdAsync(vacationRequestId);

    public async Task<List<VacationPaymentReceipt>> GetReceiptsAsync(
        Guid? employeeId, int? year, int page, int pageSize) =>
        await _vacationRepo.GetReceiptsFilteredAsync(employeeId, year, page, pageSize);

    private async Task<string> GenerateReceiptNumberAsync()
    {
        var count = await _vacationRepo.GetReceiptCountAsync();
        var now = DateTime.UtcNow;
        return $"RV-{now:yyyyMMdd}-{(count + 1):D4}";
    }
}
