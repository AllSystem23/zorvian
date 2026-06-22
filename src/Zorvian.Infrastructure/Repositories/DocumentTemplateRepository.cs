using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class DocumentTemplateRepository : IDocumentTemplateRepository
{
    private readonly ZorvianDbContext _db;

    public DocumentTemplateRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<DocumentTemplate?> GetByIdAsync(Guid id) =>
        await _db.DocumentTemplates.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<List<DocumentTemplate>> GetAllAsync() =>
        await _db.DocumentTemplates.Where(t => t.IsActive).ToListAsync();

    public async Task<(List<DocumentTemplate> Items, int Total)> GetFilteredAsync(
        string? category, string? countryCode, string? search, string? module,
        bool? isActive, int page, int pageSize)
    {
        var query = _db.DocumentTemplates.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category == category);
        if (!string.IsNullOrWhiteSpace(countryCode))
            query = query.Where(t => t.CountryCode == countryCode || t.CountryCode == "ALL");
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.Contains(search));
        if (!string.IsNullOrWhiteSpace(module))
            query = query.Where(t => t.Module == module);
        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);
        else
            query = query.Where(t => t.IsActive);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<DocumentTemplate>> GetByCategoryAsync(string category) =>
        await _db.DocumentTemplates
            .Where(t => t.Category == category && t.IsActive)
            .ToListAsync();

    public async Task<List<DocumentTemplate>> GetByCountryAsync(string countryCode) =>
        await _db.DocumentTemplates
            .Where(t => (t.CountryCode == countryCode || t.CountryCode == "ALL") && t.IsActive)
            .ToListAsync();

    public async Task AddAsync(DocumentTemplate template) =>
        await _db.DocumentTemplates.AddAsync(template);

    public Task UpdateAsync(DocumentTemplate template)
    {
        _db.DocumentTemplates.Update(template);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DocumentTemplate template)
    {
        _db.DocumentTemplates.Remove(template);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
