using AutoMapper;
using Zorvian.Application.DTOs.Department;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class DepartmentService
{
    private readonly IDepartmentRepository _repo;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenantContext;

    public DepartmentService(IDepartmentRepository repo, IMapper mapper, ITenantContext tenantContext)
    {
        _repo = repo;
        _mapper = mapper;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Siembra los departamentos por defecto para el tenant actual. Idempotente: no crea duplicados si el código/nombre ya existe.
    /// </summary>
    public async Task<List<DepartmentResponse>> SeedDefaultAsync()
    {
        var tenantId = _tenantContext.TenantId.ToString();
        var existing = await _repo.GetAllAsync();

        var defaults = new[]
        {
            ("DIR", "Dirección General", "Dirección"),
            ("RH", "Recursos Humanos", "RRHH"),
            ("TI", "Tecnología e Innovación", "Tecnología"),
            ("CONT", "Contabilidad", "Contabilidad"),
            ("VENT", "Ventas y Marketing", "Ventas"),
            ("OPER", "Operaciones", "Operaciones"),
        };

        var created = 0;
        foreach (var (code, name, desc) in defaults)
        {
            if (existing.Any(d =>
                    string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase)))
                continue;

            await _repo.AddAsync(new Core.Entities.Department
            {
                Code = code,
                Name = name,
                Description = desc,
                IsActive = true,
                TenantId = tenantId,
            });
            created++;
        }

        if (created > 0)
            await _repo.SaveChangesAsync();

        return await GetAllAsync();
    }

    public async Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request)
    {
        var department = _mapper.Map<Core.Entities.Department>(request);

        await _repo.AddAsync(department);
        await _repo.SaveChangesAsync();

        return _mapper.Map<DepartmentResponse>(department);
    }

    public async Task<DepartmentResponse?> UpdateAsync(Guid id, UpdateDepartmentRequest request)
    {
        var department = await _repo.GetByIdAsync(id);
        if (department is null) return null;

        _mapper.Map(request, department);
        await _repo.SaveChangesAsync();

        return _mapper.Map<DepartmentResponse>(department);
    }

    public async Task<List<DepartmentResponse>> GetAllAsync()
    {
        var departments = await _repo.GetAllAsync();
        return _mapper.Map<List<DepartmentResponse>>(departments);
    }

    public async Task<DepartmentResponse?> GetByIdAsync(Guid id)
    {
        var dept = await _repo.GetByIdAsync(id);
        return dept is null ? null : _mapper.Map<DepartmentResponse>(dept);
    }

    public async Task<(bool success, string? error)> DeleteAsync(Guid id)
    {
        var department = await _repo.GetByIdAsync(id);
        if (department is null)
            return (false, "Department not found");

        if (await _repo.HasEmployeesAsync(id))
            return (false, "Cannot delete department with active employees");

        await _repo.DeleteAsync(department);
        await _repo.SaveChangesAsync();
        return (true, null);
    }
}
