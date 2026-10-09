using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Department;
using Zorvian.Core.Entities;
using Zorvian.Web.Extensions;

namespace Zorvian.Tests.Mapping;

/// <summary>
/// Regresión: GET /departments devuelve 500 al mapear departamentos reales
/// (el fallo solo aparecía con la lista no vacía, nunca se detectó porque
/// todas las listas estaban vacías antes del seed manual).
/// </summary>
public sealed class DepartmentMappingTests
{
    private static IMapper CreateProductionMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddZorvianAutoMapper();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMapper>();
    }

    private static Department MakePopulatedDepartment() => new()
    {
        Id = Guid.NewGuid(),
        Code = "RH",
        Name = "Recursos Humanos",
        Description = "RRHH",
        IsActive = true,
        Manager = null,
        Employees = [],
        ParentDepartmentId = null,
        TenantId = Guid.NewGuid().ToString(),
    };

    [Fact]
    public void DepartmentToResponse_Maps_WithProductionMapper()
    {
        var mapper = CreateProductionMapper();
        var dept = MakePopulatedDepartment();

        var response = mapper.Map<DepartmentResponse>(dept);

        response.Should().NotBeNull();
        response.Id.Should().Be(dept.Id);
        response.Name.Should().Be("Recursos Humanos");
        response.Code.Should().Be("RH");
        response.ManagerName.Should().Be(string.Empty);
        response.EmployeeCount.Should().Be(0);
        response.IsActive.Should().BeTrue();
    }

    [Fact]
    public void DepartmentListToResponseList_Maps_WithProductionMapper()
    {
        var mapper = CreateProductionMapper();
        var departments = new List<Department>
        {
            MakePopulatedDepartment(),
            new Department { Id = Guid.NewGuid(), Code = "DIR", Name = "Dirección General", IsActive = true, Employees = [] },
        };

        var responses = mapper.Map<List<DepartmentResponse>>(departments);

        responses.Should().HaveCount(2);
        responses[1].Code.Should().Be("DIR");
    }
}
