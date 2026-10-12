using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

/// <summary>
/// Matriz de visibilidad por sucursal de EmployeeRepository (Employee ya tiene BranchId).
/// Semántica: null/Guid.Empty = todas las sucursales; GUID concreto = solo esa.
/// </summary>
public sealed class EmployeeRepositoryBranchScopeTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly EmployeeRepository _repo;
    private readonly string _tenantId;
    private readonly Guid _branchA = Guid.NewGuid();
    private readonly Guid _branchB = Guid.NewGuid();
    private readonly Guid _companyId = Guid.NewGuid();

    public EmployeeRepositoryBranchScopeTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _repo = new EmployeeRepository(_db);

        Seed();
    }

    private void Seed()
    {
        foreach (var branch in new[] { _branchA, _branchB })
        {
            _db.Branches.Add(new Branch
            {
                Id = branch,
                TenantId = _tenantId,
                CompanyId = _companyId,
                Name = $"Sede {branch.ToString()[..4]}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            });
        }

        var collaboratorId = Guid.NewGuid();
        _db.Collaborators.Add(new Collaborator
        {
            Id = collaboratorId,
            TenantId = _tenantId,
            CompanyId = _companyId,
            CollaboratorCode = "C-001",
            FirstName = "Colaborador",
            LastName = "Base",
            Email = "base@test.com",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        // 2 empleados en sucursal A, 1 en B, 1 sin sucursal (company-wide).
        foreach (var (branch, name) in new[] { (_branchA, "Ana"), (_branchA, "Bruno"), (_branchB, "Carla") })
        {
            _db.Employees.Add(new Employee
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                CompanyId = _companyId,
                CollaboratorId = collaboratorId,
                BranchId = branch,
                FirstName = name,
                LastName = "Prueba",
                Email = $"{name.ToLower()}@test.com",
                HireDate = new DateOnly(2024, 1, 1),
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            });
        }

        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _companyId,
            CollaboratorId = collaboratorId,
            BranchId = null,
            FirstName = "Sin",
            LastName = "Sucursal",
            Email = "sinsucursal@test.com",
            HireDate = new DateOnly(2024, 1, 1),
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        _db.SaveChanges();
    }

    [Fact]
    public async Task GetFilteredAsync_ConBranchNull_DevuelveTodasLasSucursales()
    {
        var result = await _repo.GetFilteredAsync(null, null, null, 1, 100, null);

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task GetFilteredAsync_ConBranchVacio_DevuelveTodasLasSucursales()
    {
        var result = await _repo.GetFilteredAsync(null, null, null, 1, 100, Guid.Empty);

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task GetFilteredAsync_ConBranchConcreto_FiltraSoloEsaSucursal()
    {
        var result = await _repo.GetFilteredAsync(null, null, null, 1, 100, _branchA);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(_branchA, e.BranchId));
    }

    [Fact]
    public async Task GetFilteredAsync_ConBranchSinDatos_DevuelveVacio()
    {
        var result = await _repo.GetFilteredAsync(null, null, null, 1, 100, Guid.NewGuid());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFilteredCountAsync_ConBranchConcreto_CoincideConElFiltro()
    {
        var total = await _repo.GetFilteredCountAsync(null, null, null, _branchB);

        Assert.Equal(1, total);
    }

    [Fact]
    public async Task GetFilteredCountAsync_ConBranchNull_CuentaTodasLasSucursales()
    {
        var total = await _repo.GetFilteredCountAsync(null, null, null, null);

        Assert.Equal(4, total);
    }
}
