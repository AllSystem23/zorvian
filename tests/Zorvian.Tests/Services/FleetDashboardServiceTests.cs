using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Tests.Services;

public sealed class FleetDashboardServiceTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepo = new();
    private readonly FleetDashboardService _sut;

    public FleetDashboardServiceTests()
    {
        _sut = new FleetDashboardService(_vehicleRepo.Object);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsCorrectVehicleCounts()
    {
        var vehicles = new List<Vehicle>
        {
            new() { Id = Guid.NewGuid(), Status = "Active" },
            new() { Id = Guid.NewGuid(), Status = "Active" },
            new() { Id = Guid.NewGuid(), Status = "Maintenance" },
            new() { Id = Guid.NewGuid(), Status = "Inactive" },
        };

        _vehicleRepo.Setup(r => r.GetDashboardScalarsRawAsync())
            .ReturnsAsync(new FleetDashboardScalars { TotalVehicles = 4, ActiveVehicles = 2, InMaintenance = 1, AvailableDrivers = 0, ActiveRoutes = 0, PendingDeliveries = 0, TripsToday = 0, ExpiringDocuments = 0, ExpiringSoon = 0 });

        var result = await _sut.GetDashboardAsync();

        result.TotalVehicles.Should().Be(4);
        result.ActiveVehicles.Should().Be(2);
        result.InMaintenance.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboardAsync_CountsAvailableDrivers()
    {
        var drivers = new List<Driver>
        {
            new() { Id = Guid.NewGuid(), Status = "Available" },
            new() { Id = Guid.NewGuid(), Status = "Available" },
            new() { Id = Guid.NewGuid(), Status = "OnTrip" },
        };

        _vehicleRepo.Setup(r => r.GetDashboardScalarsRawAsync())
            .ReturnsAsync(new FleetDashboardScalars { TotalVehicles = 0, ActiveVehicles = 0, InMaintenance = 0, AvailableDrivers = 2, ActiveRoutes = 0, PendingDeliveries = 0, TripsToday = 0, ExpiringDocuments = 0, ExpiringSoon = 0 });

        var result = await _sut.GetDashboardAsync();

        result.AvailableDrivers.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardAsync_CountsActiveRoutes()
    {
        var routes = new List<Route>
        {
            new() { Id = Guid.NewGuid(), Status = "InProgress" },
            new() { Id = Guid.NewGuid(), Status = "Planned" },
            new() { Id = Guid.NewGuid(), Status = "Completed" },
        };

        _vehicleRepo.Setup(r => r.GetDashboardScalarsRawAsync())
            .ReturnsAsync(new FleetDashboardScalars { TotalVehicles = 0, ActiveVehicles = 0, InMaintenance = 0, AvailableDrivers = 0, ActiveRoutes = 2, PendingDeliveries = 0, TripsToday = 0, ExpiringDocuments = 0, ExpiringSoon = 0 });

        var result = await _sut.GetDashboardAsync();

        result.ActiveRoutes.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardAsync_CountsPendingDeliveries()
    {
        var deliveries = new List<Delivery>
        {
            new() { Id = Guid.NewGuid(), Status = "Pending" },
            new() { Id = Guid.NewGuid(), Status = "InRoute" },
            new() { Id = Guid.NewGuid(), Status = "Delivered" },
        };

        _vehicleRepo.Setup(r => r.GetDashboardScalarsRawAsync())
            .ReturnsAsync(new FleetDashboardScalars { TotalVehicles = 0, ActiveVehicles = 0, InMaintenance = 0, AvailableDrivers = 0, ActiveRoutes = 0, PendingDeliveries = 2, TripsToday = 0, ExpiringDocuments = 0, ExpiringSoon = 0 });

        var result = await _sut.GetDashboardAsync();

        result.PendingDeliveries.Should().Be(2);
    }

    [Fact]
    public async Task GetDashboardAsync_GeneratesAlertForExpiringDocuments()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var documents = new List<FleetDocument>
        {
            new() { Id = Guid.NewGuid(), ExpiryDate = today.AddDays(15), Status = "Valid" },
            new() { Id = Guid.NewGuid(), ExpiryDate = today.AddDays(60), Status = "Valid" },
            new() { Id = Guid.NewGuid(), ExpiryDate = today.AddDays(-5), Status = "Valid" },
        };

        _vehicleRepo.Setup(r => r.GetDashboardScalarsRawAsync())
            .ReturnsAsync(new FleetDashboardScalars { TotalVehicles = 0, ActiveVehicles = 0, InMaintenance = 0, AvailableDrivers = 0, ActiveRoutes = 0, PendingDeliveries = 0, TripsToday = 0, ExpiringDocuments = 0, ExpiringSoon = 1 });

        var result = await _sut.GetDashboardAsync();

        result.Alerts.Should().Contain(a => a.Type == "Document");
        result.Alerts.Should().Contain(a => a.Type == "Document" && a.Severity == "warning");
    }

    [Fact]
    public async Task GetDashboardAsync_GeneratesAlertForMaintenance()
    {
        var vehicles = new List<Vehicle>
        {
            new() { Id = Guid.NewGuid(), Status = "Maintenance" },
        };

        _vehicleRepo.Setup(r => r.GetDashboardScalarsRawAsync())
            .ReturnsAsync(new FleetDashboardScalars { TotalVehicles = 1, ActiveVehicles = 0, InMaintenance = 1, AvailableDrivers = 0, ActiveRoutes = 0, PendingDeliveries = 0, TripsToday = 0, ExpiringDocuments = 0, ExpiringSoon = 0 });

        var result = await _sut.GetDashboardAsync();

        result.Alerts.Should().Contain(a => a.Type == "Maintenance" && a.Severity == "info");
    }

    [Fact]
    public async Task GetDashboardAsync_NoAlertsWhenEverythingNormal()
    {
        var vehicles = new List<Vehicle>
        {
            new() { Id = Guid.NewGuid(), Status = "Active" },
        };

        _vehicleRepo.Setup(r => r.GetDashboardScalarsRawAsync())
            .ReturnsAsync(new FleetDashboardScalars { TotalVehicles = 1, ActiveVehicles = 1, InMaintenance = 0, AvailableDrivers = 0, ActiveRoutes = 0, PendingDeliveries = 0, TripsToday = 0, ExpiringDocuments = 0, ExpiringSoon = 0 });

        var result = await _sut.GetDashboardAsync();

        result.Alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardAsync_CallsAllReposExactlyOnce()
    {
        _vehicleRepo.Setup(r => r.GetDashboardScalarsRawAsync())
            .ReturnsAsync(new FleetDashboardScalars { TotalVehicles = 0, ActiveVehicles = 0, InMaintenance = 0, AvailableDrivers = 0, ActiveRoutes = 0, PendingDeliveries = 0, TripsToday = 0, ExpiringDocuments = 0, ExpiringSoon = 0 });

        await _sut.GetDashboardAsync();

        _vehicleRepo.Verify(r => r.GetDashboardScalarsRawAsync(), Times.Once);
    }
}
