using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using AutoMapper;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Services;

public sealed class EmployeeContractLinkingTests : IDisposable
{
    private readonly ZorvianDbContext _db;
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IProviderRepository> _providerRepo = new();
    private readonly Mock<ICollaboratorRepository> _collaboratorRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly EmployeeService _sut;
    private readonly string _tenantId;
    private readonly Guid _companyId = Guid.NewGuid();

    public EmployeeContractLinkingTests()
    {
        _tenantId = _companyId.ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);

        _mapper.Setup(m => m.Map<Employee>(It.IsAny<CreateEmployeeRequest>()))
            .Returns<CreateEmployeeRequest>(r => new Employee
            {
                FirstName = r.FirstName,
                LastName = r.LastName,
                Email = r.Email,
                Phone = r.Phone,
                Position = r.Position,
                DepartmentId = r.DepartmentId,
                Salary = r.Salary,
                CollaboratorType = r.CollaboratorType ?? "employee",
                HireDate = r.HireDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "active",
                SalaryType = r.SalaryType ?? "monthly",
            });

        _mapper.Setup(m => m.Map<EmployeeResponse>(It.IsAny<Employee>()))
            .Returns<Employee>(e => new EmployeeResponse(
                e.Id, e.EmployeeCode ?? "", e.FirstName, e.LastName,
                e.Email, e.Phone ?? "", e.DateOfBirth, e.Gender ?? "",
                e.IdentificationType ?? "", e.IdentificationNumber ?? "",
                e.DepartmentId, e.Department?.Name ?? "", e.Position ?? "",
                e.HireDate, e.Status, e.Salary, e.SalaryType ?? "monthly",
                e.BankName, e.BankAccountNumber, e.BankAccountType,
                e.CollaboratorType,
                e.ServiceProviderDetails != null &&
                e.ServiceProviderDetails.Contracts != null &&
                e.ServiceProviderDetails.Contracts.Any()
                    ? e.ServiceProviderDetails.Contracts.First().Id
                    : (Guid?)null));

        var employeeRepo = new EmployeeRepo(_db);
        _sut = new EmployeeService(employeeRepo, _providerRepo.Object, _collaboratorRepo.Object, _mapper.Object, _publishEndpoint.Object);
    }

    [Fact]
    public async Task CreateAsync_WithServiceProviderAndContractId_LinksContractToWorker()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        var provider = new ServiceProvider
        {
            Id = providerId,
            BusinessName = "Proveedor Test",
            ServiceCategory = "IT",
            Status = "active",
            TenantId = _tenantId,
            EmployeeId = Guid.Empty,
        };
        _db.Set<ServiceProvider>().Add(provider);

        var contract = new ServiceContract
        {
            Id = contractId,
            ServiceProviderId = providerId,
            ContractNumber = "CONT-001",
            ContractName = "Servicios de desarrollo",
            TotalContractAmount = 50000m,
            Currency = "NIO",
            Status = "active",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TenantId = _tenantId,
        };
        _db.Set<ServiceContract>().Add(contract);
        await _db.SaveChangesAsync();

        _providerRepo.Setup(r => r.GetContractByIdAsync(contractId))
            .ReturnsAsync(() => _db.Set<ServiceContract>()
                .Include(c => c.ServiceProvider)
                .FirstOrDefault(c => c.Id == contractId));

        _providerRepo.Setup(r => r.UpdateProviderAsync(It.IsAny<ServiceProvider>()))
            .Callback<ServiceProvider>(p => _db.Set<ServiceProvider>().Update(p))
            .Returns(Task.CompletedTask);

        var request = new CreateEmployeeRequest(
            FirstName: "Carlos", LastName: "Mendoza", Email: "carlos@test.com",
            Phone: "555-0100", EmployeeCode: null, CollaboratorType: "service_provider",
            DateOfBirth: null, Gender: null, IdentificationType: null,
            IdentificationNumber: null, DepartmentId: null, Position: "Desarrollador",
            HireDate: DateOnly.FromDateTime(DateTime.UtcNow), Salary: 5000m,
            SalaryType: "monthly", BankName: null, BankAccountNumber: null,
            BankAccountType: null, ContractId: contractId);

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Carlos", result.FirstName);
        Assert.Equal("Mendoza", result.LastName);
        Assert.Equal("service_provider", result.CollaboratorType);
        Assert.Equal(contractId, result.ContractId);
        _providerRepo.Verify(r => r.UpdateProviderAsync(
            It.Is<ServiceProvider>(p => p.EmployeeId == result.Id)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithContractorAndContractId_DoesNotLinkContract()
    {
        // Regresión: "contractor" (Contratista) es figura distinta del Prestador de
        // Servicio; el contrato solo se vincula para "service_provider".
        var contractId = Guid.NewGuid();
        var request = new CreateEmployeeRequest(
            FirstName: "Jorge", LastName: "Ruiz", Email: "jorge@test.com",
            Phone: null, EmployeeCode: null, CollaboratorType: "contractor",
            DateOfBirth: null, Gender: null, IdentificationType: null,
            IdentificationNumber: null, DepartmentId: null, Position: "Consultor",
            HireDate: null, Salary: 5000m, SalaryType: "monthly",
            BankName: null, BankAccountNumber: null, BankAccountType: null,
            ContractId: contractId);

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("contractor", result.CollaboratorType);
        Assert.Null(result.ContractId);
        _providerRepo.Verify(r => r.GetContractByIdAsync(It.IsAny<Guid>()), Times.Never);
        _providerRepo.Verify(r => r.UpdateProviderAsync(It.IsAny<ServiceProvider>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithEmployeeType_DoesNotLinkContract()
    {
        var request = new CreateEmployeeRequest(
            FirstName: "Maria", LastName: "Lopez", Email: "maria@test.com",
            Phone: null, EmployeeCode: null, CollaboratorType: "employee",
            DateOfBirth: null, Gender: null, IdentificationType: null,
            IdentificationNumber: null, DepartmentId: null, Position: "Contadora",
            HireDate: null, Salary: 8000m, SalaryType: "monthly",
            BankName: null, BankAccountNumber: null, BankAccountType: null,
            ContractId: null);

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("employee", result.CollaboratorType);
        Assert.Null(result.ContractId);
        _providerRepo.Verify(r => r.GetContractByIdAsync(It.IsAny<Guid>()), Times.Never);
        _providerRepo.Verify(r => r.UpdateProviderAsync(It.IsAny<ServiceProvider>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithServiceProviderButNoContractId_DoesNotLink()
    {
        var request = new CreateEmployeeRequest(
            FirstName: "Pedro", LastName: "Garcia", Email: "pedro@test.com",
            Phone: null, EmployeeCode: null, CollaboratorType: "service_provider",
            DateOfBirth: null, Gender: null, IdentificationType: null,
            IdentificationNumber: null, DepartmentId: null, Position: "Consultor",
            HireDate: null, Salary: 3000m, SalaryType: "hourly",
            BankName: null, BankAccountNumber: null, BankAccountType: null,
            ContractId: null);

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("service_provider", result.CollaboratorType);
        Assert.Null(result.ContractId);
        _providerRepo.Verify(r => r.GetContractByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithServiceProviderAndNonexistentContract_DoesNotThrow()
    {
        var fakeContractId = Guid.NewGuid();
        _providerRepo.Setup(r => r.GetContractByIdAsync(fakeContractId))
            .ReturnsAsync((ServiceContract?)null);

        var request = new CreateEmployeeRequest(
            FirstName: "Ana", LastName: "Torres", Email: "ana@test.com",
            Phone: null, EmployeeCode: null, CollaboratorType: "service_provider",
            DateOfBirth: null, Gender: null, IdentificationType: null,
            IdentificationNumber: null, DepartmentId: null, Position: "Diseñadora",
            HireDate: null, Salary: 4000m, SalaryType: "monthly",
            BankName: null, BankAccountNumber: null, BankAccountType: null,
            ContractId: fakeContractId);

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("service_provider", result.CollaboratorType);
    }

    [Fact]
    public async Task UpdateAsync_WithServiceProviderAndContractId_LinksContract()
    {
        // Arrange: empleado service_provider existente sin contrato
        var employee = new Employee
        {
            FirstName = "Maria", LastName = "Castillo", Email = "maria.c@test.com",
            CollaboratorType = "service_provider", Position = "Supervisora",
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "active",
            SalaryType = "monthly", TenantId = _tenantId,
        };
        _db.Set<Employee>().Add(employee);
        await _db.SaveChangesAsync();

        var contractId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var provider = new ServiceProvider
        {
            Id = providerId, BusinessName = "Proveedor Update", ServiceCategory = "Limpieza",
            Status = "active", TenantId = _tenantId, EmployeeId = Guid.Empty,
        };
        _db.Set<ServiceProvider>().Add(provider);
        _db.Set<ServiceContract>().Add(new ServiceContract
        {
            Id = contractId, ServiceProviderId = providerId, ContractNumber = "CONT-U1",
            ContractName = "Servicios generales", TotalContractAmount = 30000m,
            Currency = "NIO", Status = "active",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow), TenantId = _tenantId,
        });
        await _db.SaveChangesAsync();

        _providerRepo.Setup(r => r.GetContractByIdAsync(contractId))
            .ReturnsAsync(() => _db.Set<ServiceContract>()
                .Include(c => c.ServiceProvider)
                .FirstOrDefault(c => c.Id == contractId));
        _providerRepo.Setup(r => r.UpdateProviderAsync(It.IsAny<ServiceProvider>()))
            .Callback<ServiceProvider>(p => _db.Set<ServiceProvider>().Update(p))
            .Returns(Task.CompletedTask);

        var request = new UpdateEmployeeRequest(
            FirstName: null, LastName: null, Email: null, Phone: null,
            EmployeeCode: null, CollaboratorType: "service_provider",
            DateOfBirth: null, Gender: null, IdentificationType: null,
            IdentificationNumber: null, DepartmentId: null, Position: "Supervisora",
            HireDate: null, Salary: null, SalaryType: null, Status: null,
            BankName: null, BankAccountNumber: null, BankAccountType: null,
            ContractId: contractId);

        // Act
        var result = await _sut.UpdateAsync(employee.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contractId, result.ContractId);
        _providerRepo.Verify(r => r.UpdateProviderAsync(
            It.Is<ServiceProvider>(p => p.EmployeeId == employee.Id)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_AlreadyLinkedSameProvider_DoesNotRelink()
    {
        // Arrange: empleado ya vinculado al proveedor del contrato
        var employeeId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var contractId = Guid.NewGuid();

        var provider = new ServiceProvider
        {
            Id = providerId, BusinessName = "Proveedor Ya", ServiceCategory = "IT",
            Status = "active", TenantId = _tenantId, EmployeeId = employeeId,
        };
        _db.Set<ServiceProvider>().Add(provider);
        _db.Set<ServiceContract>().Add(new ServiceContract
        {
            Id = contractId, ServiceProviderId = providerId, ContractNumber = "CONT-Y1",
            ContractName = "Servicios IT", TotalContractAmount = 10000m,
            Currency = "NIO", Status = "active",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow), TenantId = _tenantId,
        });
        _db.Set<Employee>().Add(new Employee
        {
            Id = employeeId, FirstName = "Pedro", LastName = "Ruiz", Email = "pedro.r@test.com",
            CollaboratorType = "service_provider", Position = "DevOps",
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "active",
            SalaryType = "monthly", TenantId = _tenantId,
        });
        await _db.SaveChangesAsync();

        _providerRepo.Setup(r => r.GetContractByIdAsync(contractId))
            .ReturnsAsync(() => _db.Set<ServiceContract>()
                .Include(c => c.ServiceProvider)
                .FirstOrDefault(c => c.Id == contractId));
        _providerRepo.Setup(r => r.UpdateProviderAsync(It.IsAny<ServiceProvider>()))
            .Returns(Task.CompletedTask);

        var request = new UpdateEmployeeRequest(
            FirstName: null, LastName: null, Email: null, Phone: null,
            EmployeeCode: null, CollaboratorType: "service_provider",
            DateOfBirth: null, Gender: null, IdentificationType: null,
            IdentificationNumber: null, DepartmentId: null, Position: "DevOps Sr",
            HireDate: null, Salary: null, SalaryType: null, Status: null,
            BankName: null, BankAccountNumber: null, BankAccountType: null,
            ContractId: contractId);

        // Act
        var result = await _sut.UpdateAsync(employeeId, request);

        // Assert: idempotente — ya estaba vinculado, no vuelve a hacer Update
        Assert.NotNull(result);
        _providerRepo.Verify(r => r.UpdateProviderAsync(It.IsAny<ServiceProvider>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithEmployeeTypeAndContractId_DoesNotLink()
    {
        // Arrange: empleado interno (no service_provider) con ContractId enviado
        var employee = new Employee
        {
            FirstName = "Ana", LastName = "Lopez", Email = "ana.l@test.com",
            CollaboratorType = "employee", Position = "Contadora",
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "active",
            SalaryType = "monthly", TenantId = _tenantId,
        };
        _db.Set<Employee>().Add(employee);
        await _db.SaveChangesAsync();

        _providerRepo.Setup(r => r.GetContractByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ServiceContract?)null);

        var request = new UpdateEmployeeRequest(
            FirstName: null, LastName: null, Email: null, Phone: null,
            EmployeeCode: null, CollaboratorType: "employee",
            DateOfBirth: null, Gender: null, IdentificationType: null,
            IdentificationNumber: null, DepartmentId: null, Position: "Contadora Sr",
            HireDate: null, Salary: null, SalaryType: null, Status: null,
            BankName: null, BankAccountNumber: null, BankAccountType: null,
            ContractId: Guid.NewGuid());

        // Act
        var result = await _sut.UpdateAsync(employee.Id, request);

        // Assert
        Assert.NotNull(result);
        _providerRepo.Verify(r => r.GetContractByIdAsync(It.IsAny<Guid>()), Times.Never);
        _providerRepo.Verify(r => r.UpdateProviderAsync(It.IsAny<ServiceProvider>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_GeneratesEmployeeCodeAutomatically()
    {
        var request = new CreateEmployeeRequest(
            FirstName: "Luis", LastName: "Hernandez", Email: "luis@test.com",
            Phone: null, EmployeeCode: null, CollaboratorType: "employee",
            DateOfBirth: null, Gender: null, IdentificationType: null,
            IdentificationNumber: null, DepartmentId: null, Position: "Analista",
            HireDate: null, Salary: 6000m, SalaryType: "monthly",
            BankName: null, BankAccountNumber: null, BankAccountType: null,
            ContractId: null);

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(result.EmployeeCode);
        Assert.StartsWith("EMP-", result.EmployeeCode);
    }

    [Fact]
    public async Task CreateAsync_SetsDefaultCollaboratorTypeToEmployee()
    {
        var request = new CreateEmployeeRequest(
            FirstName: "Rosa", LastName: "Martinez", Email: "rosa@test.com",
            Phone: null, EmployeeCode: null, CollaboratorType: null,
            DateOfBirth: null, Gender: null, IdentificationType: null,
            IdentificationNumber: null, DepartmentId: null, Position: "Secretaria",
            HireDate: null, Salary: 3500m, SalaryType: "monthly",
            BankName: null, BankAccountNumber: null, BankAccountType: null,
            ContractId: null);

        var result = await _sut.CreateAsync(request);

        Assert.Equal("employee", result.CollaboratorType);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private sealed class EmployeeRepo(ZorvianDbContext db) : IEmployeeRepository
    {
        public async Task<Employee?> GetByIdAsync(Guid id) =>
            await db.Set<Employee>()
                .Include(e => e.Department)
                .Include(e => e.ServiceProviderDetails)
                    .ThenInclude(sp => sp!.Contracts)
                .FirstOrDefaultAsync(e => e.Id == id);

        public Task<Employee?> GetByEmployeeCodeAsync(string code) =>
            Task.FromResult<Employee?>(null);

        public Task<List<Employee>> SearchByCodeAsync(string partialCode, int maxResults) =>
            Task.FromResult(new List<Employee>());

        public Task<List<Employee>> GetFilteredAsync(string? search, string? status, Guid? departmentId, int page, int pageSize) =>
            Task.FromResult(new List<Employee>());

        public Task<int> GetFilteredCountAsync(string? search, string? status, Guid? departmentId) =>
            Task.FromResult(0);

        public Task<List<EmployeeSupervisor>> GetSupervisorsAsync(Guid employeeId) =>
            Task.FromResult(new List<EmployeeSupervisor>());

        public async Task AddAsync(Employee employee) =>
            await db.Set<Employee>().AddAsync(employee);

        public Task UpdateAsync(Employee employee)
        {
            db.Set<Employee>().Update(employee);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Employee employee)
        {
            db.Set<Employee>().Remove(employee);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() => await db.SaveChangesAsync();

        public Task<List<AttendanceRecord>> GetAttendanceInRangeAsync(Guid employeeId, DateOnly start, DateOnly end) =>
            Task.FromResult(new List<AttendanceRecord>());

        public Task<List<VacationRequest>> GetVacationsInRangeAsync(Guid employeeId, DateOnly start, DateOnly end) =>
            Task.FromResult(new List<VacationRequest>());

        public Task<List<EmployeeBankAccount>> GetBankAccountsAsync(Guid employeeId) =>
            Task.FromResult(new List<EmployeeBankAccount>());
    }
}
