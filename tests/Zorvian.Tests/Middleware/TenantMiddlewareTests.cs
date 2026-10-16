using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories.Fleet;
using Zorvian.Web.Middleware;

namespace Zorvian.Tests.Middleware;

/// <summary>
/// E2E del filtro de sucursal: header X-Branch-Id → TenantMiddleware →
/// ITenantContext.CurrentBranchId → (servicio resuelve) → filtro EF (helper de sucursal).
/// Honra el invariante: sin sucursal válida → contexto null → "todas las sucursales".
/// </summary>
public sealed class TenantMiddlewareTests
{
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _branchA = Guid.NewGuid();
    private readonly Guid _branchB = Guid.NewGuid();
    private readonly string _tenantId;

    public TenantMiddlewareTests()
    {
        _tenantId = Guid.NewGuid().ToString();
    }

    private sealed record Harness(
        Guid? BranchSeenByNext,
        Guid? WarehouseSeenByNext,
        Mock<IBranchRepository> Branches,
        Mock<IWarehouseRepository> Warehouses);

    /// <summary>
    /// NOTA AsyncLocal: la escritura de <c>CurrentBranchId</c>/<c>CurrentWarehouseId</c> ocurre en el
    /// contexto de ejecución del middleware (después de su primer await) y NO fluye de vuelta al
    /// contexto que hizo el <c>await InvokeAsync(...)</c>. En producción esto es correcto: los
    /// consumidores del contexto (servicios/repos) corren EN LA MISMA CADENA que el middleware (vía
    /// <c>_next</c>), donde el valor sí es visible. Por eso el harness captura el estado dentro de <c>_next</c>.
    /// </summary>
    private async Task<Harness> InvokeAsync(
        Action<HttpContext, TenantContext>? configure = null,
        Func<HttpContext, IServiceProvider, Task>? inChain = null,
        params Warehouse[] seededWarehouses)
    {
        var tenant = new TenantContext();
        var authRepo = new Mock<IAuthRepository>();
        var branchRepo = new Mock<IBranchRepository>();
        var warehouseRepo = new Mock<IWarehouseRepository>();

        var branchList = new List<Branch>
        {
            new()
            {
                Id = _branchA,
                TenantId = _tenantId,
                CompanyId = _companyId,
                Name = "Sede A",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            },
            new()
            {
                Id = _branchB,
                TenantId = _tenantId,
                CompanyId = _companyId,
                Name = "Sede B",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            }
        };
        branchRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(branchList);
        authRepo.Setup(r => r.GetAllCompaniesAsync()).ReturnsAsync(new List<Company>());
        foreach (var warehouse in seededWarehouses)
        {
            warehouseRepo.Setup(r => r.GetByIdAsync(warehouse.Id)).ReturnsAsync(warehouse);
        }

        var services = new ServiceCollection();
        services.AddSingleton(tenant);
        services.AddSingleton<ITenantContext>(_ => tenant);
        services.AddSingleton<ITenantContextWriter>(_ => tenant);
        services.AddSingleton(branchRepo.Object);
        services.AddSingleton(warehouseRepo.Object);
        services.AddSingleton(authRepo.Object);
        using var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        configure?.Invoke(httpContext, tenant);

        Guid? seenByNext = null;
        Guid? warehouseSeenByNext = null;
        var logger = new Mock<ILogger<TenantMiddleware>>();
        var middleware = new TenantMiddleware(ctx =>
        {
            seenByNext = tenant.CurrentBranchId;
            warehouseSeenByNext = tenant.CurrentWarehouseId;
            return inChain is null ? Task.CompletedTask : inChain(ctx, provider);
        });
        await middleware.InvokeAsync(httpContext, tenant, tenant, logger.Object);

        return new Harness(seenByNext, warehouseSeenByNext, branchRepo, warehouseRepo);
    }

    private static void Compania(HttpContext ctx, TenantContext tenant, Guid companyId) =>
        tenant.SetTenantId(TenantId.FromGuid(companyId));

    [Fact]
    public async Task SinHeaderBranch_DejaSucursalNull()
    {
        var (seenByNext, _, branches, _) = await InvokeAsync((ctx, t) => Compania(ctx, t, _companyId));

        Assert.Null(seenByNext);
        branches.Verify(r => r.GetAllAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task HeaderBranchInvalido_DejaSucursalNull()
    {
        var (seenByNext, _, _, _) = await InvokeAsync((ctx, t) =>
        {
            Compania(ctx, t, _companyId);
            ctx.Request.Headers["X-Branch-Id"] = "not-a-guid";
        });

        Assert.Null(seenByNext);
    }

    [Fact]
    public async Task HeaderBranchValido_PropagaAlContexto()
    {
        var (seenByNext, _, branches, _) = await InvokeAsync((ctx, t) =>
        {
            Compania(ctx, t, _companyId);
            ctx.Request.Headers["X-Branch-Id"] = _branchA.ToString();
        });

        Assert.Equal(_branchA, seenByNext);
        branches.Verify(r => r.GetAllAsync(_companyId), Times.Once);
    }

    [Fact]
    public async Task HeaderBranchFueraDeCompania_SeIgnora()
    {
        var (seenByNext, _, _, _) = await InvokeAsync((ctx, t) =>
        {
            Compania(ctx, t, _companyId);
            ctx.Request.Headers["X-Branch-Id"] = new Guid("11111111-1111-1111-1111-111111111111").ToString();
        });

        Assert.Null(seenByNext);
    }

    [Fact]
    public async Task HeaderBranchSinCompaniaSeleccionada_SeIgnora()
    {
        var (seenByNext, _, _, _) = await InvokeAsync((ctx, t) =>
        {
            t.SetTenantId(TenantId.FromString("legacy-no-company"));
            ctx.Request.Headers["X-Branch-Id"] = _branchA.ToString();
        });

        Assert.Null(seenByNext);
    }

    // ── X-Warehouse-Id (bodegas, 2026-10-16) ─────────────────────────────

    /// <summary>Configura header de bodega y, si aplica, el de sucursal activa.</summary>
    private void Bodega(HttpContext ctx, Guid warehouseId, Guid? branchId = null)
    {
        ctx.Request.Headers["X-Warehouse-Id"] = warehouseId.ToString();
        if (branchId is { } b) ctx.Request.Headers["X-Branch-Id"] = b.ToString();
    }

    [Fact]
    public async Task SinHeaderWarehouse_DejaBodegaNull()
    {
        var (_, warehouseSeen, _, warehouses) = await InvokeAsync((ctx, t) => Compania(ctx, t, _companyId));

        Assert.Null(warehouseSeen);
        warehouses.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task HeaderWarehouseValido_DeSucursalActiva_PropagaAlContexto()
    {
        var warehouseId = Guid.NewGuid();
        var warehouse = SeedWarehouse(warehouseId, _branchA, _companyId, _tenantId);

        var (_, warehouseSeen, _, warehouses) = await InvokeAsync(
            (ctx, t) =>
            {
                Compania(ctx, t, _companyId);
                Bodega(ctx, warehouseId, _branchA);
            },
            null,
            warehouse);

        Assert.Equal(warehouseId, warehouseSeen);
        warehouses.Verify(r => r.GetByIdAsync(warehouseId), Times.Once);
    }

    [Fact]
    public async Task HeaderWarehouseInvalido_DejaBodegaNull()
    {
        var (_, warehouseSeen, _, _) = await InvokeAsync((ctx, t) =>
        {
            Compania(ctx, t, _companyId);
            ctx.Request.Headers["X-Warehouse-Id"] = "not-a-guid";
        });

        Assert.Null(warehouseSeen);
    }

    [Fact]
    public async Task HeaderWarehouseInexistente_SeIgnora()
    {
        var warehouseId = Guid.NewGuid();
        var (_, warehouseSeen, _, warehouses) = await InvokeAsync((ctx, t) =>
        {
            Compania(ctx, t, _companyId);
            Bodega(ctx, warehouseId);
        });

        Assert.Null(warehouseSeen);
        warehouses.Verify(r => r.GetByIdAsync(warehouseId), Times.Once);
    }

    [Fact]
    public async Task HeaderWarehouseFueraDeCompania_SeIgnora()
    {
        var warehouseId = Guid.NewGuid();
        var otherCompany = Guid.NewGuid();
        var foreign = SeedWarehouse(warehouseId, _branchA, otherCompany, otherCompany.ToString());

        var (_, warehouseSeen, _, _) = await InvokeAsync(
            (ctx, t) =>
            {
                Compania(ctx, t, _companyId);
                Bodega(ctx, warehouseId);
            },
            null,
            foreign);

        Assert.True(warehouseSeen is null, "una bodega de otra compañía no puede fijarse en el contexto");
    }

    [Fact]
    public async Task HeaderWarehouseDeOtraSucursal_ActivaSeIgnora()
    {
        // Bodega válida en la compañía pero de la sucursal B, con la sucursal activa A.
        var warehouseId = Guid.NewGuid();
        var nordWarehouse = SeedWarehouse(warehouseId, _branchB, _companyId, _tenantId);

        var (_, warehouseSeen, _, _) = await InvokeAsync(
            (ctx, t) =>
            {
                Compania(ctx, t, _companyId);
                Bodega(ctx, warehouseId, _branchA);
            },
            null,
            nordWarehouse);

        Assert.True(warehouseSeen is null, "la bodega debe pertenecer a la sucursal activa del header");
    }

    [Fact]
    public async Task HeaderWarehouseSinCompaniaSeleccionada_SeIgnora()
    {
        var warehouseId = Guid.NewGuid();
        var warehouse = SeedWarehouse(warehouseId, _branchA, _companyId, _tenantId);

        var (_, warehouseSeen, _, _) = await InvokeAsync(
            (ctx, t) =>
            {
                t.SetTenantId(TenantId.FromString("legacy-no-company"));
                Bodega(ctx, warehouseId);
            },
            null,
            warehouse);

        Assert.Null(warehouseSeen);
    }

    private static Warehouse SeedWarehouse(Guid id, Guid branchId, Guid companyId, string tenantId) => new()
    {
        Id = id,
        BranchId = branchId,
        CompanyId = companyId,
        TenantId = tenantId,
        Name = "Bodega Test",
        IsActive = true,
        IsDefault = true,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test",
    };

    [Fact]
    public async Task E2e_HeaderBranch_ConduceAlFiltroEfDeSucursal()
    {
        // 1) Middleware: header X-Branch-Id válido → contexto de sucursal (visible en la cadena).
        // El E2E corre DENTRO de la cadena (_next) y en la MISMA DbContext compartida, replicando
        // producción: el servicio/resolver ve el branch del contexto y el repo aplica la semántica
        // canónica del helper.
        Guid? resolvedInChain = null;
        List<Route>? routesInChain = null;

        await InvokeAsync(
            (ctx, t) =>
            {
                Compania(ctx, t, _companyId);
                ctx.Request.Headers["X-Branch-Id"] = _branchA.ToString();
            },
            async (_, provider) =>
            {
                var options = new DbContextOptionsBuilder<ZorvianDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;
                var chainContext = provider.GetRequiredService<ITenantContext>();
                await using var db = new ZorvianDbContext(options, chainContext);
                SeedGraph(db);

                resolvedInChain = chainContext.ResolveBranchId();

                var repo = new RouteRepository(db);
                routesInChain = await repo.GetAllAsync(resolvedInChain);
            });

        Assert.Equal(_branchA, resolvedInChain);
        Assert.Equal(2, routesInChain!.Count);
        Assert.All(routesInChain, r => Assert.Equal(_branchA, r.BranchId));
    }

    private void SeedGraph(ZorvianDbContext db)
    {
        // El query filter EF compara contra TenantId del contexto = _companyId (el middleware
        // resolvió la compañía vía TenantId.FromGuid(_companyId)).
        var tenantId = _companyId.ToString();
        foreach (var branch in new[] { _branchA, _branchB })
        {
            db.Branches.Add(new Branch
            {
                Id = branch,
                TenantId = tenantId,
                CompanyId = _companyId,
                Name = $"Sede {branch.ToString()[..4]}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            });
        }
        foreach (var (branch, code) in new[] { (_branchA, "R-A1"), (_branchA, "R-A2"), (_branchB, "R-B1") })
        {
            db.Set<Route>().Add(new Route
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CompanyId = _companyId,
                BranchId = branch,
                Code = code,
                Name = $"Ruta {code}",
                Type = "Urban",
                ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EstimatedDeparture = new TimeOnly(8, 0),
                EstimatedReturn = new TimeOnly(17, 0),
                OriginAddress = "Origen",
                DistanceEstKm = 10,
                DurationEstMinutes = 60,
                Status = "Planned"
            });
        }
        db.SaveChanges();
    }
}