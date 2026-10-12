using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories.Fleet;

namespace Zorvian.Tests.Services;

/// <summary>
/// Matriz de visibilidad por sucursal de los repos Fleet con BranchId
/// (Vehicle, Driver, Route — las únicas entidades Fleet que lo tienen).
/// Semántica: null/Guid.Empty = todas las sucursales; GUID concreto = solo esa.
/// </summary>
public sealed class FleetRepositoryBranchScopeTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly VehicleRepository _vehicles;
    private readonly DriverRepository _drivers;
    private readonly RouteRepository _routes;
    private readonly TripRepository _trips;
    private readonly DeliveryRepository _deliveries;
    private readonly FuelRefillRepository _refills;
    private readonly WorkOrderRepository _workOrders;
    private readonly FleetExpenseRepository _expenses;
    private readonly string _tenantId;
    private readonly Guid _branchA = Guid.NewGuid();
    private readonly Guid _branchB = Guid.NewGuid();
    private readonly Guid _companyId = Guid.NewGuid();

    public FleetRepositoryBranchScopeTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _vehicles = new VehicleRepository(_db);
        _drivers = new DriverRepository(_db);
        _routes = new RouteRepository(_db);
        _trips = new TripRepository(_db);
        _deliveries = new DeliveryRepository(_db);
        _refills = new FuelRefillRepository(_db);
        _workOrders = new WorkOrderRepository(_db);
        _expenses = new FleetExpenseRepository(_db);

        SeedCatalogs();
        SeedVehicles();
        SeedDrivers();
        SeedRoutes();
        SeedTransitive();
    }

    private void SeedCatalogs()
    {
        foreach (var branch in new[] { _branchA, _branchB })
        {
            _db.Branches.Add(new Branch
            {
                Id = branch,
                TenantId = _tenantId,
                CompanyId = _companyId,
                Name = $"Sede {branch.ToString()[..4]}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            });
        }

        _db.Set<VehicleBrand>().Add(new VehicleBrand
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, CompanyId = _companyId,
            Name = "Toyota", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });
        _db.Set<VehicleType>().Add(new VehicleType
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, CompanyId = _companyId,
            Name = "Camioneta", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });
        _db.Set<FuelType>().Add(new FuelType
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, CompanyId = _companyId,
            Name = "Diesel", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });
        _db.Set<DriverLicenseCategory>().Add(new DriverLicenseCategory
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, CompanyId = _companyId,
            Name = "A", CountryCode = "NIC", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });
        _db.SaveChanges();
    }

    private void SeedVehicles()
    {
        var brandId = _db.Set<VehicleBrand>().First().Id;
        var typeId = _db.Set<VehicleType>().First().Id;
        var fuelId = _db.Set<FuelType>().First().Id;

        foreach (var (branch, plate) in new[] { (_branchA, "A-001"), (_branchA, "A-002"), (_branchB, "B-001") })
        {
            _db.Set<Vehicle>().Add(new Vehicle
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                CompanyId = _companyId,
                BranchId = branch,
                Code = $"V-{plate}",
                Plate = plate,
                BrandId = brandId,
                Model = "Hilux",
                Year = 2024,
                VehicleTypeId = typeId,
                FuelTypeId = fuelId,
                Status = "Active"
            });
        }
        _db.SaveChanges();
    }

    private void SeedDrivers()
    {
        var licenseCategoryId = _db.Set<DriverLicenseCategory>().First().Id;

        foreach (var (branch, doc) in new[] { (_branchA, "001"), (_branchA, "002"), (_branchB, "003") })
        {
            _db.Set<Driver>().Add(new Driver
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                CompanyId = _companyId,
                BranchId = branch,
                FirstName = "Chofer",
                LastName = doc,
                IdDocument = doc,
                BirthDate = new DateOnly(1990, 1, 1),
                Phone = "88888888",
                Email = $"driver{doc}@test.com",
                LicenseCategoryId = licenseCategoryId,
                LicenseNumber = $"L-{doc}",
                LicenseIssueDate = new DateOnly(2020, 1, 1),
                LicenseExpiryDate = new DateOnly(2030, 1, 1),
                HireDate = new DateOnly(2022, 1, 1)
            });
        }
        _db.SaveChanges();
    }

    private void SeedRoutes()
    {
        foreach (var (branch, code) in new[] { (_branchA, "R-A1"), (_branchA, "R-A2"), (_branchB, "R-B1") })
        {
            _db.Set<Route>().Add(new Route
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
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
        _db.SaveChanges();
    }

    private void SeedTransitive()
    {
        var vehA1 = _db.Set<Vehicle>().First(v => v.BranchId == _branchA);
        var vehA2 = _db.Set<Vehicle>().First(v => v.BranchId == _branchA && v.Plate == "A-002");
        var vehB1 = _db.Set<Vehicle>().First(v => v.BranchId == _branchB);
        var driverA = _db.Set<Driver>().First(d => d.BranchId == _branchA);
        var driverB = _db.Set<Driver>().First(d => d.BranchId == _branchB);
        var routeA1 = _db.Set<Route>().First(r => r.BranchId == _branchA);
        var routeB1 = _db.Set<Route>().First(r => r.BranchId == _branchB);
        var fuelTypeId = _db.Set<FuelType>().First().Id;

        var category = new ExpenseCategory
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _companyId,
            Name = "Mantenimiento",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _db.Set<ExpenseCategory>().Add(category);
        var categoryId = category.Id;

        // Trips: 2 en sucursal A, 1 en B (vía Vehicle).
        Trip? tripA1 = null;
        foreach (var (veh, driver, code) in new[]
                 { (vehA1, driverA, "T-A1"), (vehA2, driverA, "T-A2"), (vehB1, driverB, "T-B1") })
        {
            var trip = new Trip
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                CompanyId = _companyId,
                Code = code,
                VehicleId = veh.Id,
                Vehicle = veh,
                DriverId = driver.Id,
                Driver = driver,
                StartDateTime = DateTime.UtcNow.AddHours(-1),
                Origin = "A",
                Destination = "B",
                StartKm = 0,
                Status = "Planned"
            };
            if (code == "T-A1") tripA1 = trip;
            _db.Set<Trip>().Add(trip);
        }

        // FuelRefills: 2 en A, 1 en B.
        foreach (var (veh, driver, liters) in new[]
                 { (vehA1, driverA, 10m), (vehA2, driverA, 20m), (vehB1, driverB, 30m) })
        {
            _db.Set<FuelRefill>().Add(new FuelRefill
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                CompanyId = _companyId,
                RefillDateTime = DateTime.UtcNow,
                VehicleId = veh.Id,
                Vehicle = veh,
                DriverId = driver.Id,
                Driver = driver,
                FuelTypeId = fuelTypeId,
                Liters = liters,
                PricePerLiter = 1m,
                TotalCost = liters,
                CurrentKm = 100,
                RefillType = "Full",
                PaymentMethod = "Cash",
                ValidForCalculation = true
            });
        }

        // WorkOrders: 2 en A, 1 en B.
        foreach (var (veh, number) in new[] { (vehA1, "OT-A1"), (vehA2, "OT-A2"), (vehB1, "OT-B1") })
        {
            _db.Set<WorkOrder>().Add(new WorkOrder
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                CompanyId = _companyId,
                Number = number,
                VehicleId = veh.Id,
                Vehicle = veh,
                ReportDateTime = DateTime.UtcNow,
                Priority = "High",
                Status = "Reported"
            });
        }

        // Deliveries: 1 vía vehículo A, 1 vía vehículo B, 1 vía ruta A (sin vehículo), 1 sin vínculo.
        _db.Set<Delivery>().Add(new Delivery
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _companyId,
            Code = "D-A1",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DeliveryAddress = "A",
            VehicleId = vehA1.Id,
            Vehicle = vehA1,
            Status = "Pending"
        });
        _db.Set<Delivery>().Add(new Delivery
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _companyId,
            Code = "D-B1",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DeliveryAddress = "B",
            VehicleId = vehB1.Id,
            Vehicle = vehB1,
            Status = "Pending"
        });
        _db.Set<Delivery>().Add(new Delivery
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _companyId,
            Code = "D-A-route",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DeliveryAddress = "A",
            RouteId = routeA1.Id,
            Route = routeA1,
            Status = "Pending"
        });
        _db.Set<Delivery>().Add(new Delivery
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _companyId,
            Code = "D-unlinked",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DeliveryAddress = "X",
            Status = "Pending"
        });

        // FleetExpenses: 1 vía vehículo A, 1 vía vehículo B, 1 vía Trip (de sucursal A), 1 sin vínculo.
        _db.Set<FleetExpense>().Add(new FleetExpense
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _companyId,
            ExpenseDate = DateTime.UtcNow,
            CategoryId = categoryId,
            VehicleId = vehA1.Id,
            Vehicle = vehA1,
            Description = "Gasto A",
            Amount = 100m,
            Currency = "NIO",
            ExchangeRate = 1m,
            AmountBaseCurrency = 100m,
            PaymentMethod = "Cash"
        });
        _db.Set<FleetExpense>().Add(new FleetExpense
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _companyId,
            ExpenseDate = DateTime.UtcNow,
            CategoryId = categoryId,
            VehicleId = vehB1.Id,
            Vehicle = vehB1,
            Description = "Gasto B",
            Amount = 100m,
            Currency = "NIO",
            ExchangeRate = 1m,
            AmountBaseCurrency = 100m,
            PaymentMethod = "Cash"
        });
        _db.Set<FleetExpense>().Add(new FleetExpense
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _companyId,
            ExpenseDate = DateTime.UtcNow,
            CategoryId = categoryId,
            TripId = tripA1!.Id,
            Trip = tripA1,
            Description = "Gasto trip A",
            Amount = 100m,
            Currency = "NIO",
            ExchangeRate = 1m,
            AmountBaseCurrency = 100m,
            PaymentMethod = "Cash"
        });
        _db.Set<FleetExpense>().Add(new FleetExpense
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _companyId,
            ExpenseDate = DateTime.UtcNow,
            CategoryId = categoryId,
            Description = "Gasto sin vínculo",
            Amount = 100m,
            Currency = "NIO",
            ExchangeRate = 1m,
            AmountBaseCurrency = 100m,
            PaymentMethod = "Cash"
        });

        _db.SaveChanges();
    }

    // ── Vehicles ──

    [Fact]
    public async Task Vehicles_ConBranchNull_DevuelveTodasLasSucursales()
    {
        var result = await _vehicles.GetAllAsync(_companyId, null);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task Vehicles_ConBranchVacio_DevuelveTodasLasSucursales()
    {
        var result = await _vehicles.GetAllAsync(_companyId, Guid.Empty);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task Vehicles_ConBranchConcreto_FiltraSoloEsaSucursal()
    {
        var result = await _vehicles.GetAllAsync(_companyId, _branchA);

        Assert.Equal(2, result.Count);
        Assert.All(result, v => Assert.Equal(_branchA, v.BranchId));
    }

    [Fact]
    public async Task Vehicles_ConBranchSinDatos_DevuelveVacio()
    {
        var result = await _vehicles.GetAllAsync(_companyId, Guid.NewGuid());

        Assert.Empty(result);
    }

    // ── Drivers ──

    [Fact]
    public async Task Drivers_ConBranchNull_DevuelveTodasLasSucursales()
    {
        var result = await _drivers.GetAllAsync(_companyId, null);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task Drivers_ConBranchVacio_DevuelveTodasLasSucursales()
    {
        var result = await _drivers.GetAllAsync(_companyId, Guid.Empty);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task Drivers_ConBranchConcreto_FiltraSoloEsaSucursal()
    {
        var result = await _drivers.GetAllAsync(_companyId, _branchB);

        Assert.Single(result);
        Assert.Equal(_branchB, result[0].BranchId);
    }

    [Fact]
    public async Task Drivers_ConBranchSinDatos_DevuelveVacio()
    {
        var result = await _drivers.GetAllAsync(_companyId, Guid.NewGuid());

        Assert.Empty(result);
    }

    // ── Routes ──

    [Fact]
    public async Task Routes_ConBranchNull_DevuelveTodasLasSucursales()
    {
        var result = await _routes.GetAllAsync(null);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task Routes_ConBranchVacio_DevuelveTodasLasSucursales()
    {
        var result = await _routes.GetAllAsync(Guid.Empty);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task Routes_ConBranchConcreto_FiltraSoloEsaSucursal()
    {
        var result = await _routes.GetAllAsync(_branchA);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(_branchA, r.BranchId));
    }

    [Fact]
    public async Task Routes_ConBranchSinDatos_DevuelveVacio()
    {
        var result = await _routes.GetAllAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    // ── Trips (transitivo vía Vehicle) ──

    [Fact]
    public async Task Trips_ConBranchNull_DevuelveTodasLasSucursales()
    {
        var result = await _trips.GetAllAsync(null);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task Trips_ConBranchConcreto_FiltraPorVehiculoDeLaSucursal()
    {
        var result = await _trips.GetAllAsync(_branchA);

        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(_branchA, t.Vehicle.BranchId));
    }

    [Fact]
    public async Task Trips_ConBranchSinDatos_DevuelveVacio()
    {
        var result = await _trips.GetAllAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    // ── FuelRefills (transitivo vía Vehicle) ──

    [Fact]
    public async Task FuelRefills_ConBranchConcreto_FiltraPorVehiculoDeLaSucursal()
    {
        var result = await _refills.GetAllAsync(_companyId, _branchB);

        Assert.Single(result);
        Assert.Equal(_branchB, result[0].Vehicle.BranchId);
    }

    [Fact]
    public async Task FuelRefills_ConBranchSinDatos_DevuelveVacio()
    {
        var result = await _refills.GetAllAsync(_companyId, Guid.NewGuid());

        Assert.Empty(result);
    }

    // ── WorkOrders (transitivo vía Vehicle) ──

    [Fact]
    public async Task WorkOrders_ConBranchConcreto_FiltraPorVehiculoDeLaSucursal()
    {
        var result = await _workOrders.GetAllAsync(_companyId, _branchA);

        Assert.Equal(2, result.Count);
        Assert.All(result, w => Assert.Equal(_branchA, w.Vehicle.BranchId));
    }

    [Fact]
    public async Task WorkOrders_ConBranchSinDatos_DevuelveVacio()
    {
        var result = await _workOrders.GetAllAsync(_companyId, Guid.NewGuid());

        Assert.Empty(result);
    }

    // ── Deliveries (transitivo vía Vehicle/Route + sin vínculo siempre visible) ──

    [Fact]
    public async Task Deliveries_ConBranchConcreto_IncluyeVinculadasYSinVinculo()
    {
        var result = await _deliveries.GetAllAsync(_branchA);

        // D-A1 (vehículo A) + D-A-route (ruta A) + D-unlinked (sin vínculo, company-wide)
        Assert.Equal(3, result.Count);
        Assert.Contains(result, d => d.Code == "D-unlinked");
    }

    [Fact]
    public async Task Deliveries_ConBranchConcreto_DeOtraSucursalIncluyeSoloSinVinculo()
    {
        var result = await _deliveries.GetAllAsync(Guid.NewGuid());

        Assert.Single(result);
        Assert.Equal("D-unlinked", result[0].Code);
    }

    [Fact]
    public async Task Deliveries_ConBranchNull_DevuelveTodasLasSucursales()
    {
        var result = await _deliveries.GetAllAsync(null);

        Assert.Equal(4, result.Count);
    }

    // ── FleetExpenses (transitivo vía Vehicle/Trip/Route + sin vínculo siempre visible) ──

    [Fact]
    public async Task Expenses_ConBranchConcreto_FiltraPorVehiculoYPorTrip()
    {
        var result = await _expenses.GetAllAsync(_companyId, _branchA);

        // E-A1 (vehículo A) + E-trip A (trip de sucursal A) + E-unlinked
        Assert.Equal(3, result.Count);
        Assert.Contains(result, e => e.Description == "Gasto trip A");
        Assert.Contains(result, e => e.Description == "Gasto sin vínculo");
    }

    [Fact]
    public async Task Expenses_ConBranchConcreto_OtraSucursalSoloIncluyeSinVinculo()
    {
        var result = await _expenses.GetAllAsync(_companyId, Guid.NewGuid());

        Assert.Single(result);
        Assert.Equal("Gasto sin vínculo", result[0].Description);
    }

    [Fact]
    public async Task Expenses_ConBranchNull_DevuelveTodasLasSucursales()
    {
        var result = await _expenses.GetAllAsync(_companyId, null);

        Assert.Equal(4, result.Count);
    }
}
