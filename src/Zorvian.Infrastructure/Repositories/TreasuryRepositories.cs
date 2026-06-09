using System;
using System.Threading.Tasks;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories
{
    public sealed class CheckbookRepository : ICheckbookRepository
    {
        private readonly ZorvianDbContext _db;
        public CheckbookRepository(ZorvianDbContext db) => _db = db;
        public async Task<Checkbook?> GetByIdAsync(Guid id) => await _db.Checkbooks.FindAsync(id);
        public async Task AddAsync(Checkbook checkbook) => await _db.Checkbooks.AddAsync(checkbook);
        public Task UpdateAsync(Checkbook checkbook) { _db.Checkbooks.Update(checkbook); return Task.CompletedTask; }
        public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
    }

    public sealed class CheckPrintTemplateRepository : ICheckPrintTemplateRepository
    {
        private readonly ZorvianDbContext _db;
        public CheckPrintTemplateRepository(ZorvianDbContext db) => _db = db;
        public async Task<CheckPrintTemplate?> GetByIdAsync(Guid id) => await _db.CheckPrintTemplates.FindAsync(id);
        public async Task AddAsync(CheckPrintTemplate template) => await _db.CheckPrintTemplates.AddAsync(template);
    }

    public sealed class CheckAuditTrailRepository : ICheckAuditTrailRepository
    {
        private readonly ZorvianDbContext _db;
        public CheckAuditTrailRepository(ZorvianDbContext db) => _db = db;
        public async Task AddAsync(CheckAuditTrail auditTrail) => await _db.CheckAuditTrails.AddAsync(auditTrail);
    }
}
