using AutoMapper;
using MassTransit;
using Moq;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class EmployeeServiceMaxEmployeesTests
{
    private readonly Mock<IEmployeeRepository> _repo = new();
    private readonly Mock<IProviderRepository> _providerRepo = new();
    private readonly Mock<ICollaboratorRepository> _collaboratorRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IAuthRepository> _authRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();

    private readonly string _tenantId = Guid.NewGuid().ToString();

    private EmployeeService BuildSut()
    {
        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        return new EmployeeService(
            _repo.Object, _providerRepo.Object, _collaboratorRepo.Object,
            _mapper.Object, _publishEndpoint.Object, _authRepo.Object, _tenant.Object);
    }

    private static CreateEmployeeRequest Request() => new(
        FirstName: "Ana", LastName: "López", Email: "ana@test.com",
        Phone: null, EmployeeCode: null, CollaboratorType: "employee",
        DateOfBirth: null, Gender: null, IdentificationType: null,
        IdentificationNumber: null, DepartmentId: null, Position: "Analista",
        HireDate: DateOnly.FromDateTime(DateTime.UtcNow), Salary: 1000m,
        SalaryType: "monthly", BankName: null, BankAccountNumber: null,
        BankAccountType: null, ContractId: null);

    [Fact]
    public async Task CreateAsync_Throws_WhenEmployeeLimitReached()
    {
        _authRepo.Setup(a => a.GetCompanyByTenantIdAsync(_tenantId))
            .ReturnsAsync(new Company { MaxEmployees = 5 });
        _repo.Setup(r => r.GetFilteredCountAsync(null, null, null, It.IsAny<Guid?>())).ReturnsAsync(5);

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(Request()));
        Assert.Contains("Límite de empleados", ex.Message);
        _collaboratorRepo.Verify(c => c.AddAsync(It.IsAny<Collaborator>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Allows_WhenUnderLimit()
    {
        _authRepo.Setup(a => a.GetCompanyByTenantIdAsync(_tenantId))
            .ReturnsAsync(new Company { MaxEmployees = 5 });
        _repo.Setup(r => r.GetFilteredCountAsync(null, null, null, It.IsAny<Guid?>())).ReturnsAsync(4);

        _mapper.Setup(m => m.Map<Employee>(It.IsAny<CreateEmployeeRequest>()))
            .Returns(new Employee { FirstName = "Ana", LastName = "López", Status = "active" });
        _mapper.Setup(m => m.Map<EmployeeResponse>(It.IsAny<Employee>()))
            .Returns((Employee e) => new EmployeeResponse(
                e.Id, e.EmployeeCode ?? "", e.FirstName, e.LastName, e.Email, e.Phone ?? "",
                e.DateOfBirth, e.Gender ?? "", e.IdentificationType ?? "", e.IdentificationNumber ?? "",
                e.DepartmentId, "", e.Position ?? "", e.HireDate, e.Status, e.Salary,
                e.SalaryType ?? "monthly", e.BankName, e.BankAccountNumber, e.BankAccountType,
                e.CollaboratorType, null));

        var sut = BuildSut();
        var result = await sut.CreateAsync(Request());

        Assert.NotNull(result);
        _collaboratorRepo.Verify(c => c.AddAsync(It.IsAny<Collaborator>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Allows_WhenCompanyMissing()
    {
        _authRepo.Setup(a => a.GetCompanyByTenantIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Company?)null);

        _mapper.Setup(m => m.Map<Employee>(It.IsAny<CreateEmployeeRequest>()))
            .Returns(new Employee { FirstName = "Ana", LastName = "López", Status = "active" });
        _mapper.Setup(m => m.Map<EmployeeResponse>(It.IsAny<Employee>()))
            .Returns((Employee e) => new EmployeeResponse(
                e.Id, e.EmployeeCode ?? "", e.FirstName, e.LastName, e.Email, e.Phone ?? "",
                e.DateOfBirth, e.Gender ?? "", e.IdentificationType ?? "", e.IdentificationNumber ?? "",
                e.DepartmentId, "", e.Position ?? "", e.HireDate, e.Status, e.Salary,
                e.SalaryType ?? "monthly", e.BankName, e.BankAccountNumber, e.BankAccountType,
                e.CollaboratorType, null));

        var sut = BuildSut();
        var result = await sut.CreateAsync(Request());

        Assert.NotNull(result);
        _repo.Verify(r => r.GetFilteredCountAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()), Times.Never);
    }
}
