using System.Text.Json;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public sealed class CollaboratorService : ICollaboratorService
{
    private readonly ICollaboratorRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IProviderRepository _providerRepo;
    private readonly IUnitOfWork _uow;

    public CollaboratorService(
        ICollaboratorRepository repo, 
        IEmployeeRepository employeeRepo,
        IProviderRepository providerRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _providerRepo = providerRepo;
        _uow = uow;
    }

    public async Task<Collaborator> CreateCollaboratorAsync(Collaborator collaborator, string typeDataJson)
    {
        await _repo.AddAsync(collaborator);

        if (collaborator.CollaboratorType == "employee")
        {
            var employee = JsonSerializer.Deserialize<Employee>(typeDataJson) ?? new Employee();
            employee.CollaboratorId = collaborator.Id;
            await _employeeRepo.AddAsync(employee);
        }
        else if (collaborator.CollaboratorType == "service_provider")
        {
            var provider = JsonSerializer.Deserialize<ServiceProvider>(typeDataJson) ?? new ServiceProvider();
            provider.CollaboratorId = collaborator.Id;
            await _providerRepo.AddProviderAsync(provider);
        }
        
        await _uow.SaveChangesAsync();
        return collaborator;
    }

    public async Task<Collaborator?> GetByIdAsync(Guid id) => await _repo.GetByIdAsync(id);

    public async Task<List<Collaborator>> ListByTypeAsync(string type) => await _repo.ListByTypeAsync(type);
}
