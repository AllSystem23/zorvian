using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Core.Entities;
using Zorvian.Web.Extensions;

namespace Zorvian.Tests.Mapping;

/// <summary>
/// Regresión: POST /employees (CreateAsync) mapea Employee → EmployeeResponse al
/// final del flujo. Si ese mapeo lanza, el error queda enmascarado por AUD-004
/// (el AuditAttribute intenta anotar la falla sobre un AuditLog ya persistido,
/// lo cual la inmutabilidad rechaza con HTTP 400).
/// </summary>
public sealed class EmployeeMappingTests
{
    private static IMapper CreateProductionMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddZorvianAutoMapper();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }

    [Fact]
    public void EmployeeToResponse_Maps_WithProductionMapper()
    {
        var mapper = CreateProductionMapper();
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = "EMP-001",
            FirstName = "Ana",
            LastName = "Pérez",
            Email = "ana@ejemplo.com",
            Phone = "555-0100",
            Gender = "female",
            IdentificationType = "CC",
            IdentificationNumber = "001-000000-0000A",
            DepartmentId = Guid.NewGuid(),
            Department = null,
            Position = "Contadora",
            HireDate = new DateOnly(2026, 1, 15),
            Status = "active",
            Salary = 25000m,
            SalaryType = "monthly",
            CollaboratorType = "employee",
        };

        var response = mapper.Map<EmployeeResponse>(employee);

        response.Should().NotBeNull();
        response.Id.Should().Be(employee.Id);
        response.EmployeeCode.Should().Be("EMP-001");
        response.FirstName.Should().Be("Ana");
        response.Email.Should().Be("ana@ejemplo.com");
        response.Position.Should().Be("Contadora");
        // Department no está cargado → ConstructUsing produce "" (no null).
        response.DepartmentName.Should().Be(string.Empty);
        response.CollaboratorType.Should().Be("employee");
        response.ContractId.Should().BeNull();
    }

    [Fact]
    public void EmployeeListToResponseList_Maps_WithProductionMapper()
    {
        var mapper = CreateProductionMapper();
        var employees = new List<Employee>
        {
            new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeCode = "EMP-001",
                FirstName = "Ana",
                LastName = "Pérez",
                Email = "ana@ejemplo.com",
                DepartmentId = null,
                Department = null,
                Position = "Contadora",
                HireDate = new DateOnly(2026, 1, 15),
                Status = "active",
            },
        };

        var responses = mapper.Map<List<EmployeeListResponse>>(employees);

        responses.Should().HaveCount(1);
        responses[0].FullName.Should().Be("Ana Pérez");
        responses[0].DepartmentName.Should().Be(string.Empty);
    }
}
