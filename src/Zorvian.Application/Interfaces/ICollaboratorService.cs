using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICollaboratorService
{
    Task<Collaborator> CreateCollaboratorAsync(Collaborator collaborator, string typeDataJson);
    Task<Collaborator?> GetByIdAsync(Guid id);
    Task<List<Collaborator>> ListByTypeAsync(string type);
}
