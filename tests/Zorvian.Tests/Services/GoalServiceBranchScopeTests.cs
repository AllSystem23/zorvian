using AutoMapper;
using FluentAssertions;
using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Fase A: el dashboard de metas se acota por la sucursal del EMPLEADO asignado
/// (las metas no tienen columna propia — misma herencia que los pagos a prestadores).
/// Sin sucursal en el contexto → Guid.Empty = todas.
/// </summary>
public sealed class GoalServiceBranchScopeTests
{
    private readonly Mock<IGoalRepository> _repo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly GoalService _sut;

    public GoalServiceBranchScopeTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        _sut = new GoalService(_repo.Object, Mock.Of<IMapper>(), _tenant.Object);
    }

    private static Employee Emp(Guid branch) => new() { BranchId = branch };

    private static GoalAssignment Assign(GoalDefinition def, Employee emp) => new()
    {
        GoalDefinitionId = def.Id,
        GoalDefinition = def,
        EmployeeId = emp.Id,
        Employee = emp,
        TargetValue = 100,
        Status = "active",
    };

    [Fact]
    public async Task GetDashboardAsync_WithBranch_FiltersAssignmentsByEmployeeBranch()
    {
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(branchA);

        var def = new GoalDefinition { Name = "Ventas", GoalType = "sales" };
        var a1 = Assign(def, Emp(branchA));
        var a2 = Assign(def, Emp(branchB));

        _repo.Setup(r => r.GetGoalDefinitionsAsync()).ReturnsAsync(new List<GoalDefinition> { def });
        _repo.Setup(r => r.GetGoalAssignmentsAsync()).ReturnsAsync(new List<GoalAssignment> { a1, a2 });
        _repo.Setup(r => r.GetAllProgressAsync()).ReturnsAsync(new List<GoalProgress>());
        _repo.Setup(r => r.GetAllIncentivePaymentsAsync()).ReturnsAsync(new List<IncentivePayment>());

        var result = await _sut.GetDashboardAsync();

        result.TotalAssignments.Should().Be(1, "solo los assignments de la sucursal activa cuentan");
        result.GoalStats.Should().ContainSingle().Which.Participants.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboardAsync_WithoutBranch_IncludesAllEmployeeBranches()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns((Guid?)null);

        var def = new GoalDefinition { Name = "Ventas", GoalType = "sales" };
        var a1 = Assign(def, Emp(Guid.NewGuid()));
        var a2 = Assign(def, Emp(Guid.NewGuid()));

        _repo.Setup(r => r.GetGoalDefinitionsAsync()).ReturnsAsync(new List<GoalDefinition> { def });
        _repo.Setup(r => r.GetGoalAssignmentsAsync()).ReturnsAsync(new List<GoalAssignment> { a1, a2 });
        _repo.Setup(r => r.GetAllProgressAsync()).ReturnsAsync(new List<GoalProgress>());
        _repo.Setup(r => r.GetAllIncentivePaymentsAsync()).ReturnsAsync(new List<IncentivePayment>());

        var result = await _sut.GetDashboardAsync();

        result.TotalAssignments.Should().Be(2, "sin sucursal el dashboard es company-wide");
    }

    [Fact]
    public async Task GetDashboardAsync_WithBranch_ScopesProgressAndIncentivesByEmployeeBranch()
    {
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(branchA);

        var def = new GoalDefinition { Name = "Ventas", GoalType = "sales" };
        var empA = Emp(branchA);
        var empB = Emp(branchB);
        var a1 = Assign(def, empA);
        var a2 = Assign(def, empB);

        var p1 = new GoalProgress { GoalAssignmentId = a1.Id, GoalAssignment = a1, CurrentValue = 80, CompliancePercentage = 80 };
        var p2 = new GoalProgress { GoalAssignmentId = a2.Id, GoalAssignment = a2, CurrentValue = 40, CompliancePercentage = 40 };

        var payA = new IncentivePayment { Employee = empA, Status = "approved", FinalAmount = 500 };
        var payB = new IncentivePayment { Employee = empB, Status = "approved", FinalAmount = 300 };

        _repo.Setup(r => r.GetGoalDefinitionsAsync()).ReturnsAsync(new List<GoalDefinition> { def });
        _repo.Setup(r => r.GetGoalAssignmentsAsync()).ReturnsAsync(new List<GoalAssignment> { a1, a2 });
        _repo.Setup(r => r.GetAllProgressAsync()).ReturnsAsync(new List<GoalProgress> { p1, p2 });
        _repo.Setup(r => r.GetAllIncentivePaymentsAsync()).ReturnsAsync(new List<IncentivePayment> { payA, payB });

        var result = await _sut.GetDashboardAsync();

        result.GlobalCompliance.Should().Be(80m, "solo el progreso de la sucursal activa promedia");
        result.IncentiveBudget.Should().Be(500m, "el presupuesto de incentivos también se acota por sucursal");
    }
}
