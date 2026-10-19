using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;

namespace Zorvian.Tests.Services;

/// <summary>
/// Oferta por módulos editable por plan: la UI de SuperAdmin define qué
/// módulos incluye cada plan y el ModuleEnforcementFilter los consume.
/// </summary>
public sealed class SubscriptionPlanModulesTests
{
    private readonly Mock<ISubscriptionPlanRepository> _repo = new();
    private readonly SubscriptionPlanService _sut;
    private readonly SubscriptionPlan _plan = new()
    {
        Id = Guid.NewGuid(),
        PlanId = "starter",
        Name = "Starter",
        Price = 0,
        Period = "/mes",
        MaxEmployees = 10,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
    };

    public SubscriptionPlanModulesTests()
    {
        _sut = new SubscriptionPlanService(_repo.Object);
        _repo.Setup(r => r.GetPlanByIdAsync(_plan.Id)).ReturnsAsync(_plan);
    }

    [Fact]
    public async Task UpdatePlanModules_ReplacesSet_AndReturnsModules()
    {
        List<ModuleType>? replaced = null;
        _repo.Setup(r => r.ReplaceModulesAsync(_plan.PlanId, It.IsAny<IReadOnlyCollection<ModuleType>>()))
            .Callback<string, IReadOnlyCollection<ModuleType>>((_, m) => replaced = m.ToList())
            .Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var response = await _sut.UpdatePlanModulesAsync(_plan.Id, new[] { "Sales", "Hr", "sales" });

        Assert.NotNull(response);
        // "sales" duplicado con distinta casededuplicado.
        Assert.Equal(new[] { "Hr", "Sales" }, response!.Modules!.OrderBy(m => m));
        Assert.NotNull(replaced);
        // Ojo: el sort de enums es por valor subyacente (Sales=0 < Hr=6).
        Assert.Equal(new[] { ModuleType.Sales, ModuleType.Hr }, replaced!.OrderBy(m => m));
    }

    [Fact]
    public async Task UpdatePlanModules_UnknownModule_Throws()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdatePlanModulesAsync(_plan.Id, new[] { "Sales", "NoExiste" }));

        Assert.Contains("NoExiste", ex.Message);
        _repo.Verify(r => r.ReplaceModulesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<ModuleType>>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePlanModules_UnknownPlan_ReturnsNull()
    {
        _repo.Setup(r => r.GetPlanByIdAsync(It.IsAny<Guid>())).ReturnsAsync((SubscriptionPlan?)null);

        var response = await _sut.UpdatePlanModulesAsync(Guid.NewGuid(), new[] { "Sales" });

        Assert.Null(response);
    }

    [Fact]
    public async Task GetPlanById_ReturnsEnabledModules()
    {
        _repo.Setup(r => r.GetModulesByPlanIdAsync(_plan.PlanId)).ReturnsAsync(new List<PlanModule>
        {
            new() { Id = Guid.NewGuid(), PlanId = _plan.PlanId, Module = ModuleType.Sales, IsEnabled = true },
            new() { Id = Guid.NewGuid(), PlanId = _plan.PlanId, Module = ModuleType.Fleet, IsEnabled = false },
        });

        var response = await _sut.GetPlanByIdAsync(_plan.Id);

        Assert.NotNull(response);
        // El set de la UI son las filas del catálogo (habilitadas por defecto al reemplazar).
        Assert.Equal(new[] { "Fleet", "Sales" }, response!.Modules!.OrderBy(m => m));
    }
}
