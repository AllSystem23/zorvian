using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class FleetAlertServiceTests
{
    private readonly Mock<IFleetDocumentRepository> _documentRepo = new();
    private readonly Mock<IDriverRepository> _driverRepo = new();
    private readonly Mock<IVehicleRepository> _vehicleRepo = new();
    private readonly Mock<IFuelRefillRepository> _fuelRepo = new();
    private readonly Mock<IWorkOrderRepository> _workOrderRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<INotificationService> _notification = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ILogger<FleetAlertService>> _logger = new();
    private readonly FleetAlertService _sut;
    private readonly Guid _companyId = Guid.NewGuid();

    public FleetAlertServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _sut = new FleetAlertService(
            _documentRepo.Object, _driverRepo.Object, _vehicleRepo.Object,
            _fuelRepo.Object, _workOrderRepo.Object, _tenant.Object,
            _notification.Object, _mapper.Object, _logger.Object);

        // Default: all repos return empty
        _documentRepo.Setup(r => r.GetExpiringAsync(It.IsAny<int>(), It.IsAny<Guid>())).ReturnsAsync(new List<FleetDocument>());
        _driverRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Driver>());
        _vehicleRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Vehicle>());
        _fuelRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<FuelRefill>());
        _workOrderRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<WorkOrder>());
    }

    [Fact]
    public async Task GetActiveAlertsAsync_WhenNoIssues_ReturnsEmpty()
    {
        var result = await _sut.GetActiveAlertsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAlertSummaryAsync_WhenNoIssues_ReturnsZeroCounts()
    {
        var result = await _sut.GetAlertSummaryAsync();

        result.ActiveAlerts.Should().Be(0);
        result.CriticalAlerts.Should().Be(0);
        result.WarningAlerts.Should().Be(0);
        result.InfoAlerts.Should().Be(0);
    }

    [Fact]
    public async Task GetActiveAlertsAsync_WithExpiringDocuments_GeneratesAlerts()
    {
        var expiringDoc = new FleetDocument
        {
            Id = Guid.NewGuid(),
            DocumentNumber = "SOAT-2026",
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            EntityType = "Vehicle",
            EntityId = Guid.NewGuid(),
            DocumentType = new DocumentType { Name = "SOAT" },
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        _documentRepo.Setup(r => r.GetExpiringAsync(30, It.IsAny<Guid>())).ReturnsAsync(new List<FleetDocument> { expiringDoc });

        var result = await _sut.GetActiveAlertsAsync();

        result.Should().NotBeEmpty();
        result.Should().Contain(a => a.Category == "Document" && a.Severity == "critical");
    }

    [Fact]
    public async Task GetActiveAlertsAsync_WithExpiredDocument_GeneratesCriticalAlert()
    {
        var expiredDoc = new FleetDocument
        {
            Id = Guid.NewGuid(),
            DocumentNumber = "REV-2024",
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            EntityType = "Vehicle",
            EntityId = Guid.NewGuid(),
            DocumentType = new DocumentType { Name = "Revisión Técnica" },
            CreatedAt = DateTime.UtcNow.AddMonths(-12)
        };

        _documentRepo.Setup(r => r.GetExpiringAsync(30, It.IsAny<Guid>())).ReturnsAsync(new List<FleetDocument> { expiredDoc });

        var result = await _sut.GetActiveAlertsAsync();

        result.Should().Contain(a => a.Severity == "critical" && a.Title.Contains("vencido"));
    }

    [Fact]
    public async Task GetActiveAlertsAsync_WithDriverLicenseExpiring_GeneratesLicenseAlert()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var drivers = new List<Driver>
        {
            new()
            {
                Id = Guid.NewGuid(), FirstName = "Carlos", LastName = "López",
                Status = "Active",
                LicenseExpiryDate = today.AddDays(10),
                LicenseNumber = "LIC-001",
                CreatedAt = DateTime.UtcNow
            }
        };

        _driverRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(drivers);

        var result = await _sut.GetActiveAlertsAsync();

        result.Should().Contain(a => a.Category == "License" && a.Severity == "warning");
    }

    [Fact]
    public async Task GetActiveAlertsAsync_WithVehicleInMaintenance_GeneratesInfoAlert()
    {
        var vehicles = new List<Vehicle>
        {
            new()
            {
                Id = Guid.NewGuid(), Plate = "ABC-123", Status = "Maintenance",
                Model = "Hilux",
                Brand = new VehicleBrand { Name = "Toyota" },
                CreatedAt = DateTime.UtcNow
            }
        };

        _vehicleRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(vehicles);

        var result = await _sut.GetActiveAlertsAsync();

        result.Should().Contain(a => a.Category == "Maintenance" && a.Severity == "info");
    }

    [Fact]
    public async Task GetActiveAlertsAsync_WithAnomalousFuel_GeneratesWarning()
    {
        var fuelRefills = new List<FuelRefill>
        {
            new()
            {
                Id = Guid.NewGuid(), AnomalyFlag = true, Liters = 100m, TotalCost = 8000m,
                RefillDateTime = DateTime.UtcNow.AddDays(-2),
                VehicleId = Guid.NewGuid(),
                Vehicle = new Vehicle { Plate = "XYZ-789" },
                CreatedAt = DateTime.UtcNow
            }
        };

        _fuelRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(fuelRefills);

        var result = await _sut.GetActiveAlertsAsync();

        result.Should().Contain(a => a.Category == "Fuel" && a.Severity == "warning");
    }

    [Fact]
    public async Task BlockDriverAsync_WhenFound_SetsStatusAndReturnsResponse()
    {
        var driverId = Guid.NewGuid();
        var driver = new Driver
        {
            Id = driverId, FirstName = "Pedro", LastName = "García",
            Status = "Active"
        };

        _driverRepo.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);

        var result = await _sut.BlockDriverAsync(driverId, new BlockDriverRequest("Accidente de tránsito"));

        result.Should().NotBeNull();
        result!.IsBlocked.Should().BeTrue();
        result.BlockReason.Should().Be("Accidente de tránsito");
        driver.Status.Should().Be("Suspended");
        _notification.Verify(n => n.NotifyTenantAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task BlockDriverAsync_WhenNotFound_ReturnsNull()
    {
        _driverRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Driver?)null);

        var result = await _sut.BlockDriverAsync(Guid.NewGuid(), new BlockDriverRequest("Test"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task UnblockDriverAsync_WhenFound_SetsActiveAndReturnsResponse()
    {
        var driverId = Guid.NewGuid();
        var driver = new Driver
        {
            Id = driverId, FirstName = "Pedro", LastName = "García",
            Status = "Suspended"
        };

        _driverRepo.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);

        var result = await _sut.UnblockDriverAsync(driverId);

        result.Should().NotBeNull();
        result!.IsBlocked.Should().BeFalse();
        driver.Status.Should().Be("Active");
    }

    [Fact]
    public async Task UnblockDriverAsync_WhenNotFound_ReturnsNull()
    {
        _driverRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Driver?)null);

        var result = await _sut.UnblockDriverAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDriverBlockStatusAsync_SuspendedDriver_ReturnsBlocked()
    {
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, FirstName = "Ana", LastName = "Martínez", Status = "Suspended", UpdatedAt = DateTime.UtcNow };

        _driverRepo.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);

        var result = await _sut.GetDriverBlockStatusAsync(driverId);

        result.Should().NotBeNull();
        result!.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task GetDriverBlockStatusAsync_ActiveDriver_ReturnsNotBlocked()
    {
        var driverId = Guid.NewGuid();
        var driver = new Driver { Id = driverId, FirstName = "Ana", LastName = "Martínez", Status = "Active" };

        _driverRepo.Setup(r => r.GetByIdAsync(driverId)).ReturnsAsync(driver);

        var result = await _sut.GetDriverBlockStatusAsync(driverId);

        result.Should().NotBeNull();
        result!.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task GetDriverBlockStatusAsync_WhenNotFound_ReturnsNull()
    {
        _driverRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Driver?)null);

        var result = await _sut.GetDriverBlockStatusAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAlertAsync_DispatchesNotificationAndReturnsAlert()
    {
        var request = new CreateFleetAlertRequest(
            "Maintenance", "warning", "Vehicle", Guid.NewGuid(),
            "Mantenimiento pendiente", "El vehículo ABC-123 necesita mantenimiento");

        var result = await _sut.CreateAlertAsync(request);

        result.Should().NotBeNull();
        result.Category.Should().Be("Maintenance");
        result.Severity.Should().Be("warning");
        result.Status.Should().Be("Active");
        result.Title.Should().Be("Mantenimiento pendiente");
        _notification.Verify(n => n.NotifyTenantAsync(
            It.IsAny<string>(), "Mantenimiento pendiente",
            It.IsAny<string>(), "fleet_maintenance_warning",
            It.IsAny<string?>()), Times.Once);
    }

    // ══════════════════════════════════════════════════════════════
    //  Null Safety Tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetActiveAlertsAsync_WithNullBrand_DoesNotThrow()
    {
        var vehicles = new List<Vehicle>
        {
            new()
            {
                Id = Guid.NewGuid(), Plate = "DEF-456", Status = "Maintenance",
                Model = "Navara",
                Brand = null!,
                CreatedAt = DateTime.UtcNow
            }
        };

        _vehicleRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(vehicles);

        var result = await _sut.GetActiveAlertsAsync();

        result.Should().ContainSingle(a => a.Category == "Maintenance" && a.Severity == "info");
        result.First(a => a.Category == "Maintenance").Message.Should().Contain("N/A");
    }

    [Fact]
    public async Task GetActiveAlertsAsync_WithNullBrandOutOfService_DoesNotThrow()
    {
        var vehicles = new List<Vehicle>
        {
            new()
            {
                Id = Guid.NewGuid(), Plate = "GHI-789", Status = "OutOfService",
                Model = "Canter",
                Brand = null!,
                CreatedAt = DateTime.UtcNow
            }
        };

        _vehicleRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(vehicles);

        var result = await _sut.GetActiveAlertsAsync();

        result.Should().ContainSingle(a => a.Category == "Maintenance" && a.Severity == "critical");
        result.First(a => a.Category == "Maintenance").Message.Should().Contain("N/A");
    }

    [Fact]
    public async Task GetActiveAlertsAsync_WithNullFuelVehicle_DoesNotThrow()
    {
        var fuelRefills = new List<FuelRefill>
        {
            new()
            {
                Id = Guid.NewGuid(), AnomalyFlag = true, Liters = 50m, TotalCost = 4000m,
                RefillDateTime = DateTime.UtcNow.AddDays(-1),
                VehicleId = Guid.NewGuid(),
                Vehicle = null!,
                CreatedAt = DateTime.UtcNow
            }
        };

        _fuelRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(fuelRefills);

        var result = await _sut.GetActiveAlertsAsync();

        result.Should().ContainSingle(a => a.Category == "Fuel" && a.Severity == "warning");
        result.First(a => a.Category == "Fuel").Title.Should().Contain("N/A");
    }

    [Fact]
    public async Task GetActiveAlertsAsync_WithNullWorkOrderVehicle_DoesNotThrow()
    {
        var workOrders = new List<WorkOrder>
        {
            new()
            {
                Id = Guid.NewGuid(), Number = "OT-001", Priority = "Urgent", Status = "Reported",
                Vehicle = null!,
                CreatedAt = DateTime.UtcNow
            }
        };

        _workOrderRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(workOrders);

        var result = await _sut.GetActiveAlertsAsync();

        result.Should().ContainSingle(a => a.Category == "Maintenance" && a.Severity == "critical");
        result.First(a => a.Category == "Maintenance").Title.Should().Contain("N/A");
    }

    [Fact]
    public async Task GetActiveAlertsAsync_WithNullDocumentType_UsesFallbackName()
    {
        var expiringDoc = new FleetDocument
        {
            Id = Guid.NewGuid(),
            DocumentNumber = "DOC-001",
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            EntityType = "Driver",
            EntityId = Guid.NewGuid(),
            DocumentType = null,
            CreatedAt = DateTime.UtcNow
        };

        _documentRepo.Setup(r => r.GetExpiringAsync(30, It.IsAny<Guid>())).ReturnsAsync(new List<FleetDocument> { expiringDoc });

        var result = await _sut.GetActiveAlertsAsync();

        result.Should().ContainSingle(a => a.Category == "Document");
        result.First(a => a.Category == "Document").Title.Should().Contain("DOC-001");
    }

    // ══════════════════════════════════════════════════════════════
    //  Error Isolation Tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetActiveAlertsAsync_DocumentCheckThrows_OtherChecksStillRun()
    {
        // Document repo throws
        _documentRepo.Setup(r => r.GetExpiringAsync(It.IsAny<int>(), It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("DB connection lost"));

        // But driver repo works fine
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var drivers = new List<Driver>
        {
            new()
            {
                Id = Guid.NewGuid(), FirstName = "Luis", LastName = "Hernández",
                Status = "Active",
                LicenseExpiryDate = today.AddDays(5),
                LicenseNumber = "LIC-002",
                CreatedAt = DateTime.UtcNow
            }
        };
        _driverRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(drivers);

        var result = await _sut.GetActiveAlertsAsync();

        // Driver alert should still be returned despite document check failure
        result.Should().Contain(a => a.Category == "License");
        _logger.Verify(l => l.Log(
            LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveAlertsAsync_VehicleCheckThrows_OtherChecksStillRun()
    {
        // Vehicle repo throws
        _vehicleRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("Table missing column"));

        // But fuel repo works
        var fuelRefills = new List<FuelRefill>
        {
            new()
            {
                Id = Guid.NewGuid(), AnomalyFlag = true, Liters = 100m, TotalCost = 8000m,
                RefillDateTime = DateTime.UtcNow.AddDays(-2),
                VehicleId = Guid.NewGuid(),
                Vehicle = new Vehicle { Plate = "XYZ-789" },
                CreatedAt = DateTime.UtcNow
            }
        };
        _fuelRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(fuelRefills);

        var result = await _sut.GetActiveAlertsAsync();

        // Fuel alert should still be returned
        result.Should().Contain(a => a.Category == "Fuel");
        _logger.Verify(l => l.Log(
            LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveAlertsAsync_AllChecksThrow_ReturnsEmptyWithAllLogged()
    {
        _documentRepo.Setup(r => r.GetExpiringAsync(It.IsAny<int>(), It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("doc error"));
        _driverRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("driver error"));
        _vehicleRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("vehicle error"));
        _fuelRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("fuel error"));
        _workOrderRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("wo error"));

        var result = await _sut.GetActiveAlertsAsync();

        result.Should().BeEmpty();
        _logger.Verify(l => l.Log(
            LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(5));
    }

    [Fact]
    public async Task GetActiveAlertsAsync_MixedSuccessAndFailure_ReturnsPartialResults()
    {
        // Document check succeeds
        var expiringDoc = new FleetDocument
        {
            Id = Guid.NewGuid(),
            DocumentNumber = "PERMISO-2026",
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EntityType = "Vehicle",
            EntityId = Guid.NewGuid(),
            DocumentType = new DocumentType { Name = "Permiso" },
            CreatedAt = DateTime.UtcNow.AddMonths(-3)
        };
        _documentRepo.Setup(r => r.GetExpiringAsync(30, It.IsAny<Guid>())).ReturnsAsync(new List<FleetDocument> { expiringDoc });

        // Driver check fails
        _driverRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("driver error"));

        // Vehicle check succeeds
        var vehicles = new List<Vehicle>
        {
            new()
            {
                Id = Guid.NewGuid(), Plate = "MIX-001", Status = "OutOfService",
                Model = "Fortuner",
                Brand = new VehicleBrand { Name = "Toyota" },
                CreatedAt = DateTime.UtcNow
            }
        };
        _vehicleRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(vehicles);

        // Fuel check fails
        _fuelRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("fuel error"));

        // Work order check succeeds
        var workOrders = new List<WorkOrder>
        {
            new()
            {
                Id = Guid.NewGuid(), Number = "OT-100", Priority = "High", Status = "InProgress",
                Vehicle = new Vehicle { Plate = "MIX-002" },
                CreatedAt = DateTime.UtcNow
            }
        };
        _workOrderRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(workOrders);

        var result = await _sut.GetActiveAlertsAsync();

        // Should have results from document + vehicle + work order checks
        result.Should().Contain(a => a.Category == "Document");
        result.Should().Contain(a => a.Category == "Maintenance" && a.EntityType == "Vehicle");
        result.Should().Contain(a => a.Category == "Maintenance" && a.EntityType == "WorkOrder");
        // 2 failed checks logged
        _logger.Verify(l => l.Log(
            LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetAlertSummaryAsync_WithNullNavigationProperties_StillReturnsSummary()
    {
        var vehicles = new List<Vehicle>
        {
            new()
            {
                Id = Guid.NewGuid(), Plate = "NAV-001", Status = "Maintenance",
                Model = "Defender",
                Brand = null!,
                CreatedAt = DateTime.UtcNow
            }
        };
        _vehicleRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(vehicles);

        var result = await _sut.GetAlertSummaryAsync();

        result.ActiveAlerts.Should().Be(1);
        result.InfoAlerts.Should().Be(1);
        result.RecentAlerts.Should().HaveCount(1);
    }
}
