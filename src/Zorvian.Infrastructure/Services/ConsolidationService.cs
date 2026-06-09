using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.Consolidation;
using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class ConsolidationService : IConsolidationService
{
    private readonly ZorvianDbContext _context;

    public ConsolidationService(ZorvianDbContext context)
    {
        _context = context;
    }

    public async Task<ConsolidatedFinancialReportDto> GetConsolidatedFinancialReportAsync(IEnumerable<Guid> companyIds, DateTime startDate, DateTime endDate)
    {
        // Esta es una implementación simplificada de consolidación
        // En un entorno de producción, esto requeriría una lógica contable compleja
        // de eliminación de saldos intercompañía.
        
        var lines = await _context.AccountingEntries
            .Where(e => companyIds.Contains(Guid.Parse(e.TenantId)) && e.EntryDate >= startDate && e.EntryDate <= endDate)
            .SelectMany(e => e.Details)
            .GroupBy(d => d.Account.Name)
            .Select(g => new ConsolidatedLineItemDto(g.Key, g.Sum(d => d.DebitAmount - d.CreditAmount)))
            .ToListAsync();

        return new ConsolidatedFinancialReportDto(
            0, 0, 0, 0, // Placeholder valores contables
            lines
        );
    }

    public async Task<IEnumerable<IntercompanyTransactionDto>> GetPendingIntercompanyTransactionsAsync(Guid companyId)
    {
        return await _context.IntercompanyTransactions
            .Where(t => (t.FromCompanyId == companyId || t.ToCompanyId == companyId) && t.Status == "Pending")
            .Select(t => new IntercompanyTransactionDto(
                t.Id, t.FromCompanyId, t.ToCompanyId, t.Amount, t.Currency, t.Description, t.Status
            ))
            .ToListAsync();
    }
}
