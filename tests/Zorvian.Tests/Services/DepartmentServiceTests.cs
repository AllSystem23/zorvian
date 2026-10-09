using AutoMapper;
using Moq;
using FluentAssertions;
using Zorvian.Application.DTOs.Department;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class DepartmentServiceTests
{
    private readonly Mock<IDepartmentRepository> _repo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly DepartmentService _sut;
    private readonly Guid _companyId = Guid.NewGuid();

    public DepartmentServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _sut = new DepartmentService(_repo.Object, _mapper.Object, _tenant.Object);
    }

    private static Department MakeDepartment(string code = "RH", string name = "Recursos Humanos", bool isActive = true)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = "RRHH",
            IsActive = isActive,
            Employees = [],
        };

    private static DepartmentResponse MakeResponse(Department d)
        => new(d.Id, d.Name, d.Code ?? string.Empty, d.Description ?? string.Empty, string.Empty, d.ParentDepartmentId, d.IsActive, d.Employees.Count);

    // ── CreateAsync ──

    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsAndSavesDepartment()
    {
        var request = new CreateDepartmentRequest("Tecnología", "TI", "Innovación", null, null);
        var entity = new Department { Id = Guid.NewGuid(), Name = "Tecnología" };
        _mapper.Setup(m => m.Map<Department>(request)).Returns(entity);
        _mapper.Setup(m => m.Map<DepartmentResponse>(entity)).Returns(MakeResponse(entity));

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Tecnología");
        _repo.Verify(r => r.AddAsync(entity), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MapsAllFieldsCorrectly()
    {
        var request = new CreateDepartmentRequest("Ventas", "VENT", "Ventas y Marketing", null, null);
        _mapper.Setup(m => m.Map<Department>(It.IsAny<CreateDepartmentRequest>()))
               .Returns((CreateDepartmentRequest r) => new Department { Name = r.Name, Code = r.Code, Description = r.Description });
        _mapper.Setup(m => m.Map<DepartmentResponse>(It.IsAny<Department>()))
               .Returns((Department d) => MakeResponse(d));

        await _sut.CreateAsync(request);

        _repo.Verify(r => r.AddAsync(It.Is<Department>(d =>
            d.Name == "Ventas" && d.Code == "VENT" && d.Description == "Ventas y Marketing")), Times.Once);
    }

    // ── SeedDefaultAsync ──

    /// <summary>Repo mock con persistencia: GetAllAsync devuelve la lista viva y AddAsync agrega a ella.</summary>
    private void SetupPersistentRepo(List<Department> existing)
    {
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);
        _repo.Setup(r => r.AddAsync(It.IsAny<Department>()))
             .Callback<Department>(existing.Add)
             .Returns(Task.CompletedTask);
        _mapper.Setup(m => m.Map<List<DepartmentResponse>>(It.IsAny<List<Department>>()))
               .Returns((List<Department> ds) => ds.Select(MakeResponse).ToList());
    }

    [Fact]
    public async Task SeedDefaultAsync_WithEmptyRepo_CreatesSixDefaultDepartments()
    {
        SetupPersistentRepo([]);

        var result = await _sut.SeedDefaultAsync();

        result.Should().HaveCount(6);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SeedDefaultAsync_UsesExpectedDefaultSet()
    {
        var all = new List<Department>();
        SetupPersistentRepo(all);

        var result = await _sut.SeedDefaultAsync();

        all.Select(d => d.Code).Should().BeEquivalentTo(["DIR", "RH", "TI", "CONT", "VENT", "OPER"]);
        all.Select(d => d.Name).Should().BeEquivalentTo([
            "Dirección General", "Recursos Humanos", "Tecnología e Innovación",
            "Contabilidad", "Ventas y Marketing", "Operaciones",
        ]);
        all.Should().OnlyContain(d => d.IsActive);
        result.Should().HaveCount(6);
    }

    [Fact]
    public async Task SeedDefaultAsync_SetsTenantIdFromTenantContext()
    {
        var all = new List<Department>();
        SetupPersistentRepo(all);

        await _sut.SeedDefaultAsync();

        all.Should().OnlyContain(d => d.TenantId == _companyId.ToString());
    }

    [Fact]
    public async Task SeedDefaultAsync_WhenCodeAlreadyExists_SkipsThatDepartment()
    {
        var existing = new List<Department> { MakeDepartment(code: "RH", name: "Recursos Humanos") };
        SetupPersistentRepo(existing);

        var result = await _sut.SeedDefaultAsync();

        existing.Count(d => string.Equals(d.Code, "RH", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
        existing.Should().HaveCount(6); // 1 existente + 5 nuevos
        result.Should().HaveCount(6); // 1 existente + 5 nuevos
        result.Should().HaveCount(6); // 1 existente + 5 nuevos
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SeedDefaultAsync_WhenNameAlreadyExists_SkipsEvenWithDifferentCode()
    {
        var existing = new List<Department> { MakeDepartment(code: "X1", name: "Contabilidad") };
        SetupPersistentRepo(existing);

        var result = await _sut.SeedDefaultAsync();

        existing.Count(d => string.Equals(d.Name, "Contabilidad", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
        existing.Should().HaveCount(6);
        result.Should().HaveCount(6);
    }

    [Fact]
    public async Task SeedDefaultAsync_IsCaseInsensitiveForExistingMatches()
    {
        var existing = new List<Department> { MakeDepartment(code: "rh", name: "recursos humanos") };
        SetupPersistentRepo(existing);

        await _sut.SeedDefaultAsync();

        existing.Count(d => string.Equals(d.Code, "RH", StringComparison.OrdinalIgnoreCase)).Should().Be(1);
        existing.Should().HaveCount(6);
    }

    [Fact]
    public async Task SeedDefaultAsync_WhenAllDefaultsExist_DoesNotAddOrSave()
    {
        var existing = new List<Department>
        {
            MakeDepartment("DIR", "Dirección General"),
            MakeDepartment("RH", "Recursos Humanos"),
            MakeDepartment("TI", "Tecnología e Innovación"),
            MakeDepartment("CONT", "Contabilidad"),
            MakeDepartment("VENT", "Ventas y Marketing"),
            MakeDepartment("OPER", "Operaciones"),
        };
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);
        _mapper.Setup(m => m.Map<List<DepartmentResponse>>(It.IsAny<List<Department>>()))
               .Returns((List<Department> ds) => ds.Select(MakeResponse).ToList());

        var result = await _sut.SeedDefaultAsync();

        _repo.Verify(r => r.AddAsync(It.IsAny<Department>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
        result.Should().HaveCount(6);
    }

    // ── UpdateAsync ──

    [Fact]
    public async Task UpdateAsync_WithValidId_UpdatesAndReturnsDepartment()
    {
        var dept = MakeDepartment();
        _repo.Setup(r => r.GetByIdAsync(dept.Id)).ReturnsAsync(dept);
        var request = new UpdateDepartmentRequest("Nuevo Nombre", "NN", "Desc nueva", null, null, true);
        var response = MakeResponse(dept);
        _mapper.Setup(m => m.Map(request, dept));
        _mapper.Setup(m => m.Map<DepartmentResponse>(dept)).Returns(response);

        var result = await _sut.UpdateAsync(dept.Id, request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Recursos Humanos");
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Department?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateDepartmentRequest(null, null, null, null, null, null));

        result.Should().BeNull();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    // ── GetByIdAsync ──

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsDepartment()
    {
        var dept = MakeDepartment();
        _repo.Setup(r => r.GetByIdAsync(dept.Id)).ReturnsAsync(dept);
        _mapper.Setup(m => m.Map<DepartmentResponse>(dept)).Returns(MakeResponse(dept));

        var result = await _sut.GetByIdAsync(dept.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Recursos Humanos");
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Department?)null);
        // GetByIdAsync devuelve null sin tocar el mapper cuando no existe.

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── DeleteAsync ──

    [Fact]
    public async Task DeleteAsync_WithValidIdWithoutEmployees_DeletesDepartment()
    {
        var dept = MakeDepartment();
        _repo.Setup(r => r.GetByIdAsync(dept.Id)).ReturnsAsync(dept);
        _repo.Setup(r => r.HasEmployeesAsync(dept.Id)).ReturnsAsync(false);

        var (success, error) = await _sut.DeleteAsync(dept.Id);

        success.Should().BeTrue();
        error.Should().BeNull();
        _repo.Verify(r => r.DeleteAsync(dept), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithEmployees_ReturnsConflictError()
    {
        var dept = MakeDepartment();
        _repo.Setup(r => r.GetByIdAsync(dept.Id)).ReturnsAsync(dept);
        _repo.Setup(r => r.HasEmployeesAsync(dept.Id)).ReturnsAsync(true);

        var (success, error) = await _sut.DeleteAsync(dept.Id);

        success.Should().BeFalse();
        error.Should().Be("Cannot delete department with active employees");
        _repo.Verify(r => r.DeleteAsync(It.IsAny<Department>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsNotFoundError()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Department?)null);

        var (success, error) = await _sut.DeleteAsync(Guid.NewGuid());

        success.Should().BeFalse();
        error.Should().Be("Department not found");
    }
}
