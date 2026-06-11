using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class ElectronicInvoiceRepository : IElectronicInvoiceRepository
{
    private readonly ZorvianDbContext _context;

    public ElectronicInvoiceRepository(ZorvianDbContext context)
    {
        _context = context;
    }

    public async Task<ElectronicInvoice?> GetByIdAsync(Guid id)
    {
        return await _context.ElectronicInvoices
            .Include(e => e.Sale)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
    }

    public async Task<ElectronicInvoice?> GetBySaleAsync(Guid saleId)
    {
        return await _context.ElectronicInvoices
            .FirstOrDefaultAsync(e => e.SaleId == saleId && !e.IsDeleted);
    }

    public async Task<List<ElectronicInvoice>> GetByCompanyAsync(Guid companyId, string? countryCode = null)
    {
        var query = _context.ElectronicInvoices
            .Where(e => e.TenantId == companyId.ToString() && !e.IsDeleted);

        if (!string.IsNullOrEmpty(countryCode))
            query = query.Where(e => e.CountryCode == countryCode);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(ElectronicInvoice invoice)
    {
        await _context.ElectronicInvoices.AddAsync(invoice);
    }

    public Task UpdateAsync(ElectronicInvoice invoice)
    {
        _context.ElectronicInvoices.Update(invoice);
        return Task.CompletedTask;
    }

    public async Task AddXmlAsync(ElectronicInvoiceXml xml)
    {
        await _context.ElectronicInvoiceXmls.AddAsync(xml);
    }

    public async Task<List<ElectronicInvoiceXml>> GetXmlsAsync(Guid invoiceId)
    {
        return await _context.ElectronicInvoiceXmls
            .Where(x => x.ElectronicInvoiceId == invoiceId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<ElectronicInvoiceSummaryDto> GetSummaryAsync(Guid companyId, string? countryCode = null)
    {
        var query = _context.ElectronicInvoices
            .Where(e => e.TenantId == companyId.ToString() && !e.IsDeleted);

        if (!string.IsNullOrEmpty(countryCode))
            query = query.Where(e => e.CountryCode == countryCode);

        var total = await query.CountAsync();
        var authorized = await query.CountAsync(e => e.Status == "authorized");
        var pending = await query.CountAsync(e => e.Status == "pending" || e.Status == "submitted");
        var rejected = await query.CountAsync(e => e.Status == "rejected");
        var cancelled = await query.CountAsync(e => e.Status == "cancelled");

        return new ElectronicInvoiceSummaryDto(total, authorized, pending, rejected, cancelled);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
