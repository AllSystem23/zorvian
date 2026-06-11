using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CollaboratorRepository : ICollaboratorRepository
{
    private readonly ZorvianDbContext _db;

    public CollaboratorRepository(ZorvianDbContext db) => _db = db;

    public async Task<Collaborator?> GetByIdAsync(Guid id) => 
        await _db.Collaborators.FindAsync(id);

    public async Task AddAsync(Collaborator collaborator) => 
        await _db.Collaborators.AddAsync(collaborator);

    public Task UpdateAsync(Collaborator collaborator)
    {
        _db.Collaborators.Update(collaborator);
        return Task.CompletedTask;
    }

    public async Task<List<Collaborator>> ListByTypeAsync(string type) =>
        await _db.Collaborators.Where(c => c.CollaboratorType == type).ToListAsync();
}
