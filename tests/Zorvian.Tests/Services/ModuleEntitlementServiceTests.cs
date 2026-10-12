using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

public sealed class ModuleEntitlementServiceTests : IDisposable
{
    private readonly ZorvianDbContext _db;
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ModuleEntitlementService _sut;

    public ModuleEntitlementServiceTests()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new ModuleEntitlementService(_db);

        _db.PlanModules.AddRange(
            new PlanModule { PlanId = "starter", Module = ModuleType.Sales, IsEnabled = true },
            new PlanModule { PlanId = "starter", Module = ModuleType.Inventory, IsEnabled = true },
            new PlanModule { PlanId = "starter", Module = ModuleType.Fleet, IsEnabled = false },
            new PlanModule { PlanId = "professional", Module = ModuleType.Fleet, IsEnabled = true },
            new PlanModule { PlanId = "professional", Module = ModuleType.Finance, IsEnabled = true });
        _db.SaveChanges();
    }

    [Fact]
    public async Task IsModuleEnabledAsync_ReturnsTrue_WhenPlanIncludesModule()
    {
        var result = await _sut.IsModuleEnabledAsync("starter", ModuleType.Sales);
        Assert.True(result);
    }

    [Fact]
    public async Task IsModuleEnabledAsync_ReturnsFalse_WhenPlanExcludesModule()
    {
        var result = await _sut.IsModuleEnabledAsync("starter", ModuleType.Finance);
        Assert.False(result);
    }

    [Fact]
    public async Task IsModuleEnabledAsync_ReturnsFalse_WhenMappingDisabled()
    {
        var result = await _sut.IsModuleEnabledAsync("starter", ModuleType.Fleet);
        Assert.False(result);
    }

    [Fact]
    public async Task IsModuleEnabledAsync_ReturnsTrue_WhenAnotherPlanEnablesModule()
    {
        var result = await _sut.IsModuleEnabledAsync("professional", ModuleType.Fleet);
        Assert.True(result);
    }

    [Fact]
    public async Task GetEnabledModulesAsync_ReturnsOnlyEnabledModules()
    {
        var modules = await _sut.GetEnabledModulesAsync("starter");
        Assert.Contains(ModuleType.Sales, modules);
        Assert.Contains(ModuleType.Inventory, modules);
        Assert.DoesNotContain(ModuleType.Fleet, modules);
        Assert.DoesNotContain(ModuleType.Finance, modules);
    }

    public void Dispose() => _db.Dispose();
}
