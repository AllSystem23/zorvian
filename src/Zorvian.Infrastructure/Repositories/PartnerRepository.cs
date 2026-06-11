using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class PartnerRepository : IPartnerRepository
{
    private readonly ZorvianDbContext _db;

    public PartnerRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Partner?> GetByIdAsync(Guid id) =>
        await _db.Set<Partner>().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Partner?> GetByCodeAsync(string code) =>
        await _db.Set<Partner>().FirstOrDefaultAsync(p => p.Code == code);

    public async Task<List<Partner>> GetFilteredAsync(string? search, string? status, string? countryCode, string? partnerType, int page, int pageSize)
    {
        var query = _db.Set<Partner>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                p.Code.ToLower().Contains(searchLower) ||
                p.TaxId.ToLower().Contains(searchLower));
        }
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(countryCode))
            query = query.Where(p => p.CountryCode == countryCode);
        if (!string.IsNullOrWhiteSpace(partnerType))
            query = query.Where(p => p.PartnerType == partnerType);

        return await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(string? search, string? status, string? countryCode, string? partnerType)
    {
        var query = _db.Set<Partner>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                p.Code.ToLower().Contains(searchLower) ||
                p.TaxId.ToLower().Contains(searchLower));
        }
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(countryCode))
            query = query.Where(p => p.CountryCode == countryCode);
        if (!string.IsNullOrWhiteSpace(partnerType))
            query = query.Where(p => p.PartnerType == partnerType);

        return await query.CountAsync();
    }

    public async Task AddAsync(Partner partner) =>
        await _db.Set<Partner>().AddAsync(partner);

    public Task UpdateAsync(Partner partner)
    {
        _db.Set<Partner>().Update(partner);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();

    public async Task<bool> ExistsByTaxIdAsync(string taxId) =>
        await _db.Set<Partner>().AnyAsync(p => p.TaxId == taxId);

    public async Task<List<Partner>> GetActiveByCountryAsync(string countryCode) =>
        await _db.Set<Partner>()
            .Where(p => p.Status == "active" && p.CountryCode == countryCode)
            .OrderBy(p => p.Name)
            .ToListAsync();
}
