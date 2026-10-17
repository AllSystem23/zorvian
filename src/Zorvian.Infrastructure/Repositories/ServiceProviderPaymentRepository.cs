using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

/// <summary>
/// Pagos a Prestadores de Servicios. El pago NO tiene BranchId propio: la sucursal
/// se HEREDA del prestador (Employee.BranchId) — no se crea una migración para esto.
/// El filtro de sucursal es opt-in por el caller (servicio resuelve filtro explícito
/// o contexto del header); Guid.Empty/null = todas las sucursales.
/// </summary>
public sealed class ServiceProviderPaymentRepository : IServiceProviderPaymentRepository
{
    private readonly ZorvianDbContext _db;

    public ServiceProviderPaymentRepository(ZorvianDbContext db) => _db = db;

    public async Task<ServiceProviderPayment?> GetByIdAsync(Guid id) =>
        await _db.ServiceProviderPayments
            .Include(p => p.Employee).ThenInclude(e => e.Branch)
            .Include(p => p.Lines)
            .Include(p => p.Deductions)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<(List<ServiceProviderPayment> Items, int Total)> GetFilteredAsync(
        string? search, Guid? employeeId, int? year, int? month, int? half,
        string? status, Guid? branchId, int page, int pageSize)
    {
        var query = _db.ServiceProviderPayments
            .Include(p => p.Employee).ThenInclude(e => e.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.PaymentNumber.Contains(search) ||
                (p.Employee.FirstName + " " + p.Employee.LastName).Contains(search));

        if (employeeId.HasValue)
            query = query.Where(p => p.EmployeeId == employeeId.Value);

        if (year.HasValue)
            query = query.Where(p => p.PeriodYear == year.Value);

        if (month.HasValue)
            query = query.Where(p => p.PeriodMonth == month.Value);

        if (half.HasValue)
            query = query.Where(p => p.PeriodHalf == half.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        // Sucursal: se filtra por la sucursal del PRESTADOR (Employee.BranchId).
        // Vacío/Empty = todas las sucursales (semántica canónica del proyecto).
        if (branchId.HasValue && branchId.Value != Guid.Empty)
            query = query.Where(p => p.Employee.BranchId == branchId.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.PeriodYear)
            .ThenByDescending(p => p.PeriodMonth)
            .ThenByDescending(p => p.PeriodHalf)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<ServiceProviderPayment?> GetByPeriodAsync(Guid employeeId, int year, int month, int half) =>
        await _db.ServiceProviderPayments
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p =>
                p.EmployeeId == employeeId &&
                p.PeriodYear == year &&
                p.PeriodMonth == month &&
                p.PeriodHalf == half &&
                p.Status != "cancelled");

    public async Task<string> GeneratePaymentNumberAsync(Guid companyId)
    {
        var now = DateTime.UtcNow;

        if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var prefix = $"PP-{now:yyyyMMdd}-";
            var last = await _db.ServiceProviderPayments
                .Where(p => p.PaymentNumber.StartsWith(prefix))
                .OrderByDescending(p => p.PaymentNumber)
                .FirstOrDefaultAsync();
            int next = 1;
            if (last != null && int.TryParse(last.PaymentNumber[^4..], out var lastNum))
                next = lastNum + 1;
            return $"{prefix}{next:D4}";
        }

        // PostgreSQL sequence: generación atómica (mismo patrón que SC-/TR-).
        var raw = await _db.Database.SqlQueryRaw<int>(
            "SELECT nextval('seq_service_provider_payment_number')::int").FirstOrDefaultAsync();
        return $"PP-{now:yyyyMMdd}-{raw:D4}";
    }

    public async Task AddAsync(ServiceProviderPayment payment) =>
        await _db.ServiceProviderPayments.AddAsync(payment);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
