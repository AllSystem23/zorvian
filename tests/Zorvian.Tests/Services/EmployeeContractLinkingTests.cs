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
    private readonly Mock<IEncryptionService> _encryption = new();
    private readonly Mock<IProviderRepository> _providerRepo = new();
    private readonly Mock<IMapper> _mapper = new();
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

        _encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        _encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);

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
        _sut = new EmployeeService(employeeRepo, _providerRepo.Object, _mapper.Object, _encryption.Object);
    }

    [Fact]
    public async Task CreateAsync_WithContractorAndContractId_LinksContractToWorker()
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
            Phone: "555-0100", EmployeeCode: null, CollaboratorType: "contractor",
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
        Assert.Equal("contractor", result.CollaboratorType);
        Assert.Equal(contractId, result.ContractId);
        _providerRepo.Verify(r => r.UpdateProviderAsync(
            It.Is<ServiceProvider>(p => p.EmployeeId == result.Id)), Times.Once);
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
    public async Task CreateAsync_WithContractorButNoContractId_DoesNotLink()
    {
        var request = new CreateEmployeeRequest(
            FirstName: "Pedro", LastName: "Garcia", Email: "pedro@test.com",
            Phone: null, EmployeeCode: null, CollaboratorType: "contractor",
            DateOfBirth: null, Gender: null, IdentificationType: null,
            IdentificationNumber: null, DepartmentId: null, Position: "Consultor",
            HireDate: null, Salary: 3000m, SalaryType: "hourly",
            BankName: null, BankAccountNumber: null, BankAccountType: null,
            ContractId: null);

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("contractor", result.CollaboratorType);
        Assert.Null(result.ContractId);
        _providerRepo.Verify(r => r.GetContractByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithContractorAndNonexistentContract_DoesNotThrow()
    {
        var fakeContractId = Guid.NewGuid();
        _providerRepo.Setup(r => r.GetContractByIdAsync(fakeContractId))
            .ReturnsAsync((ServiceContract?)null);

        var request = new CreateEmployeeRequest(
            FirstName: "Ana", LastName: "Torres", Email: "ana@test.com",
            Phone: null, EmployeeCode: null, CollaboratorType: "contractor",
            DateOfBirth: null, Gender: null, IdentificationType: null,
            IdentificationNumber: null, DepartmentId: null, Position: "Diseñadora",
            HireDate: null, Salary: 4000m, SalaryType: "monthly",
            BankName: null, BankAccountNumber: null, BankAccountType: null,
            ContractId: fakeContractId);

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("contractor", result.CollaboratorType);
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
