using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class LeadRepository : ILeadRepository
{
    private readonly ZorvianDbContext _db;

    public LeadRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Lead?> GetByIdAsync(Guid id) =>
        await _db.Leads
            .Include(l => l.Activities)
                .ThenInclude(a => a.CreatedByUser)
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<List<Lead>> GetFilteredAsync(string? search, string? status, Guid companyId, int page, int pageSize)
    {
        var query = _db.Leads.Where(l => l.CompanyId == companyId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(l =>
                l.FirstName.ToLower().Contains(s) ||
                l.LastName.ToLower().Contains(s) ||
                (l.CompanyName != null && l.CompanyName.ToLower().Contains(s)) ||
                (l.Email != null && l.Email.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status.ToString().ToLower() == status.ToLower());
        }

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(string? search, string? status, Guid companyId)
    {
        var query = _db.Leads.Where(l => l.CompanyId == companyId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(l =>
                l.FirstName.ToLower().Contains(s) ||
                l.LastName.ToLower().Contains(s) ||
                (l.CompanyName != null && l.CompanyName.ToLower().Contains(s)) ||
                (l.Email != null && l.Email.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status.ToString().ToLower() == status.ToLower());
        }

        return await query.CountAsync();
    }

    public async Task AddAsync(Lead lead) =>
        await _db.Leads.AddAsync(lead);

    public Task UpdateAsync(Lead lead)
    {
        _db.Leads.Update(lead);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Lead lead)
    {
        _db.Leads.Remove(lead);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
