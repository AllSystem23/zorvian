using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICollaboratorRepository
{
    Task<Collaborator?> GetByIdAsync(Guid id);
    Task AddAsync(Collaborator collaborator);
    Task UpdateAsync(Collaborator collaborator);
    Task<List<Collaborator>> ListByTypeAsync(string type);
}
