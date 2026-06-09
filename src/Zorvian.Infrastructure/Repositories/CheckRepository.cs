using System;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories
{
    public sealed class CheckRepository : ICheckRepository
    {
        private readonly ZorvianDbContext _db;

        public CheckRepository(ZorvianDbContext db) => _db = db;

        public async Task<Check?> GetByIdAsync(Guid id) => await _db.Checks.FindAsync(id);

        public async Task<List<Check>> GetAllAsync() => await _db.Checks.ToListAsync();

        public async Task AddAsync(Check check) => await _db.Checks.AddAsync(check);

        public Task UpdateAsync(Check check)
        {
            _db.Checks.Update(check);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _db.Checks.FindAsync(id);
            if (entity != null) _db.Checks.Remove(entity);
        }

        public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
    }
}
