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
    private readonly Mock<IDriverRepository> _driverRepo = new();
    private readonly Mock<IRouteRepository> _routeRepo = new();
    private readonly Mock<IDeliveryRepository> _deliveryRepo = new();
    private readonly Mock<ITripRepository> _tripRepo = new();
    private readonly Mock<IFleetDocumentRepository> _documentRepo = new();
    private readonly FleetDashboardService _sut;

    public FleetDashboardServiceTests()
    {
        _sut = new FleetDashboardService(
            _vehicleRepo.Object, _driverRepo.Object, _routeRepo.Object,
            _deliveryRepo.Object, _tripRepo.Object, _documentRepo.Object);
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

        _vehicleRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(vehicles);
        _driverRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Driver>());
        _routeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Route>());
        _deliveryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Delivery>());
        _tripRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Trip>());
        _documentRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<FleetDocument>());

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

        _vehicleRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Vehicle>());
        _driverRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(drivers);
        _routeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Route>());
        _deliveryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Delivery>());
        _tripRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Trip>());
        _documentRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<FleetDocument>());

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

        _vehicleRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Vehicle>());
        _driverRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Driver>());
        _routeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(routes);
        _deliveryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Delivery>());
        _tripRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Trip>());
        _documentRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<FleetDocument>());

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

        _vehicleRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Vehicle>());
        _driverRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Driver>());
        _routeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Route>());
        _deliveryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(deliveries);
        _tripRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Trip>());
        _documentRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<FleetDocument>());

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

        _vehicleRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Vehicle>());
        _driverRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Driver>());
        _routeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Route>());
        _deliveryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Delivery>());
        _tripRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Trip>());
        _documentRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(documents);

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

        _vehicleRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(vehicles);
        _driverRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Driver>());
        _routeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Route>());
        _deliveryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Delivery>());
        _tripRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Trip>());
        _documentRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<FleetDocument>());

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

        _vehicleRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(vehicles);
        _driverRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Driver>());
        _routeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Route>());
        _deliveryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Delivery>());
        _tripRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Trip>());
        _documentRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<FleetDocument>());

        var result = await _sut.GetDashboardAsync();

        result.Alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardAsync_CallsAllReposExactlyOnce()
    {
        _vehicleRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Vehicle>());
        _driverRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<Driver>());
        _routeRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Route>());
        _deliveryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Delivery>());
        _tripRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Trip>());
        _documentRepo.Setup(r => r.GetAllAsync(It.IsAny<Guid>())).ReturnsAsync(new List<FleetDocument>());

        await _sut.GetDashboardAsync();

        _vehicleRepo.Verify(r => r.GetAllAsync(It.IsAny<Guid>()), Times.Once);
        _driverRepo.Verify(r => r.GetAllAsync(It.IsAny<Guid>()), Times.Once);
        _routeRepo.Verify(r => r.GetAllAsync(), Times.Once);
        _deliveryRepo.Verify(r => r.GetAllAsync(), Times.Once);
        _tripRepo.Verify(r => r.GetAllAsync(), Times.Once);
        _documentRepo.Verify(r => r.GetAllAsync(It.IsAny<Guid>()), Times.Once);
    }
}
