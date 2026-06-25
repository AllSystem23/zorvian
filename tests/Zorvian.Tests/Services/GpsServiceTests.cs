using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class GpsServiceTests
{
    private readonly Mock<IGpsPositionRepository> _gpsRepo = new();
    private readonly Mock<IVehicleRepository> _vehicleRepo = new();
    private readonly Mock<IGeofenceRepository> _geofenceRepo = new();
    private readonly Mock<IGeofenceStateRepository> _geofenceStateRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<INotificationService> _notification = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GpsService _sut;
    private readonly Guid _companyId = Guid.NewGuid();

    public GpsServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _sut = new GpsService(
            _gpsRepo.Object, _vehicleRepo.Object, _geofenceRepo.Object,
            _geofenceStateRepo.Object, _tenant.Object, _notification.Object, _mapper.Object);
    }

    [Fact]
    public async Task ReceivePositionAsync_WithValidVehicleId_SavesAndReturns()
    {
        var vehicleId = Guid.NewGuid();
        var request = new ReceiveGpsPositionRequest(
            "DEV-001", vehicleId, 12.11, -86.23, null, 65.5, 90,
            DateTime.UtcNow, true, 150000m, 75m, 32m, 85m, 5, 12);

        _mapper.Setup(m => m.Map<GpsPositionResponse>(It.IsAny<GpsPosition>()))
            .Returns(new GpsPositionResponse(
                Guid.NewGuid(), vehicleId, "ABC-123", 12.11, -86.23, null, 65.5, 90,
                request.GpsTimestamp, true, 150000m, 75m, 32m, 85m, 5, 12));

        var result = await _sut.ReceivePositionAsync(request);

        result.Should().NotBeNull();
        _gpsRepo.Verify(r => r.AddAsync(It.IsAny<GpsPosition>()), Times.Once);
        _gpsRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReceivePositionAsync_WithoutVehicleId_ResolvesByDeviceId()
    {
        var vehicleId = Guid.NewGuid();
        var vehicles = new List<Vehicle>
        {
            new() { Id = vehicleId, GpsDeviceId = "DEV-001" }
        };

        _vehicleRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(vehicles);
        _mapper.Setup(m => m.Map<GpsPositionResponse>(It.IsAny<GpsPosition>()))
            .Returns(new GpsPositionResponse(
                Guid.NewGuid(), vehicleId, "ABC-123", 12.11, -86.23, null, null, null,
                DateTime.UtcNow, null, null, null, null, null, null, null));

        var request = new ReceiveGpsPositionRequest(
            "DEV-001", null, 12.11, -86.23, null, null, null,
            DateTime.UtcNow, null, null, null, null, null, null, null);

        var result = await _sut.ReceivePositionAsync(request);

        result.Should().NotBeNull();
        _gpsRepo.Verify(r => r.AddAsync(It.IsAny<GpsPosition>()), Times.Once);
    }

    [Fact]
    public async Task ReceivePositionAsync_WithUnknownDevice_ReturnsNull()
    {
        _vehicleRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(new List<Vehicle>());

        var request = new ReceiveGpsPositionRequest(
            "UNKNOWN-DEVICE", null, 12.11, -86.23, null, null, null,
            DateTime.UtcNow, null, null, null, null, null, null, null);

        var result = await _sut.ReceivePositionAsync(request);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ReceiveBulkAsync_ResolvesMultipleVehicles()
    {
        var v1Id = Guid.NewGuid();
        var v2Id = Guid.NewGuid();
        var vehicles = new List<Vehicle>
        {
            new() { Id = v1Id, GpsDeviceId = "DEV-001" },
            new() { Id = v2Id, GpsDeviceId = "DEV-002" },
        };

        _vehicleRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(vehicles);

        var request = new BulkReceiveGpsRequest(new List<ReceiveGpsPositionRequest>
        {
            new("DEV-001", null, 12.11, -86.23, null, null, null, DateTime.UtcNow, null, null, null, null, null, null, null),
            new("DEV-002", null, 12.15, -86.25, null, null, null, DateTime.UtcNow, null, null, null, null, null, null, null),
        });

        var count = await _sut.ReceiveBulkAsync(request);

        count.Should().Be(2);
        _gpsRepo.Verify(r => r.AddRangeAsync(It.IsAny<List<GpsPosition>>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveBulkAsync_SkipsUnknownDevices()
    {
        _vehicleRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(new List<Vehicle>());

        var request = new BulkReceiveGpsRequest(new List<ReceiveGpsPositionRequest>
        {
            new("UNKNOWN", null, 12.11, -86.23, null, null, null, DateTime.UtcNow, null, null, null, null, null, null, null),
        });

        var count = await _sut.ReceiveBulkAsync(request);

        count.Should().Be(0);
        _gpsRepo.Verify(r => r.AddRangeAsync(It.IsAny<List<GpsPosition>>()), Times.Never);
    }

    [Fact]
    public async Task GetFleetPositionsAsync_ReturnsVehicleSummaries()
    {
        var vehicleId = Guid.NewGuid();
        var brand = new VehicleBrand { Name = "Toyota" };
        var vehicle = new Vehicle { Id = vehicleId, Plate = "ABC-123", Model = "Hilux", Brand = brand };
        var positions = new List<GpsPosition>
        {
            new()
            {
                Id = Guid.NewGuid(), VehicleId = vehicleId,
                Latitude = 12.11, Longitude = -86.23, Speed = 65,
                Heading = 90, GpsTimestamp = DateTime.UtcNow,
                IgnitionOn = true, FuelLevel = 75m, Vehicle = vehicle
            }
        };

        _gpsRepo.Setup(r => r.GetLatestPerVehicleAsync(_companyId)).ReturnsAsync(positions);

        var result = await _sut.GetFleetPositionsAsync();

        result.Should().HaveCount(1);
        result[0].VehiclePlate.Should().Be("ABC-123");
        result[0].Latitude.Should().Be(12.11);
    }

    [Fact]
    public async Task GetVehicleHistoryAsync_WhenNoPositions_ReturnsNull()
    {
        _gpsRepo.Setup(r => r.GetByVehicleAndDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<GpsPosition>());

        var result = await _sut.GetVehicleHistoryAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetVehicleHistoryAsync_WhenPositionsExist_ReturnsHistory()
    {
        var vehicleId = Guid.NewGuid();
        var brand = new VehicleBrand { Name = "Toyota" };
        var vehicle = new Vehicle { Id = vehicleId, Plate = "ABC-123", Brand = brand };

        var positions = new List<GpsPosition>
        {
            new()
            {
                Id = Guid.NewGuid(), VehicleId = vehicleId,
                Latitude = 12.11, Longitude = -86.23, Speed = 60,
                GpsTimestamp = DateTime.UtcNow.AddHours(-1), Vehicle = vehicle
            },
            new()
            {
                Id = Guid.NewGuid(), VehicleId = vehicleId,
                Latitude = 12.12, Longitude = -86.24, Speed = 70,
                GpsTimestamp = DateTime.UtcNow, Vehicle = vehicle
            }
        };

        _gpsRepo.Setup(r => r.GetByVehicleAndDateRangeAsync(vehicleId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(positions);
        _mapper.Setup(m => m.Map<GpsPositionResponse>(It.IsAny<GpsPosition>()))
            .Returns(new GpsPositionResponse(
                Guid.NewGuid(), vehicleId, "ABC-123", 12.11, -86.23, null, 60, null,
                DateTime.UtcNow, null, null, null, null, null, null, null));

        var result = await _sut.GetVehicleHistoryAsync(vehicleId, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow);

        result.Should().NotBeNull();
        result!.Positions.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLatestPositionAsync_WhenFound_ReturnsPosition()
    {
        var vehicleId = Guid.NewGuid();
        var brand = new VehicleBrand { Name = "Toyota" };
        var vehicle = new Vehicle { Id = vehicleId, Plate = "ABC-123", Brand = brand };

        var position = new GpsPosition
        {
            Id = Guid.NewGuid(), VehicleId = vehicleId,
            Latitude = 12.11, Longitude = -86.23, Vehicle = vehicle
        };

        _gpsRepo.Setup(r => r.GetLatestByVehicleAsync(vehicleId)).ReturnsAsync(position);
        _mapper.Setup(m => m.Map<GpsPositionResponse>(position))
            .Returns(new GpsPositionResponse(
                position.Id, vehicleId, "ABC-123", 12.11, -86.23, null, null, null,
                DateTime.UtcNow, null, null, null, null, null, null, null));

        var result = await _sut.GetLatestPositionAsync(vehicleId);

        result.Should().NotBeNull();
        result!.VehiclePlate.Should().Be("ABC-123");
    }

    [Fact]
    public async Task GetLatestPositionAsync_WhenNotFound_ReturnsNull()
    {
        _gpsRepo.Setup(r => r.GetLatestByVehicleAsync(It.IsAny<Guid>())).ReturnsAsync((GpsPosition?)null);

        var result = await _sut.GetLatestPositionAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckPointInGeofenceAsync_InsideCircleGeofence_ReturnsInside()
    {
        var geofences = new List<Geofence>
        {
            new()
            {
                Id = Guid.NewGuid(), Name = "Bodega Central",
                Type = "Circle", Active = true, Radius = 5.0,
                CoordinatesJson = "[[12.11, -86.23]]"
            }
        };

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(geofences);

        var result = await _sut.CheckPointInGeofenceAsync(12.11, -86.23);

        result.IsInside.Should().BeTrue();
        result.GeofenceName.Should().Be("Bodega Central");
    }

    [Fact]
    public async Task CheckPointInGeofenceAsync_OutsideGeofence_ReturnsNotInside()
    {
        var geofences = new List<Geofence>
        {
            new()
            {
                Id = Guid.NewGuid(), Name = "Bodega Central",
                Type = "Circle", Active = true, Radius = 1.0,
                CoordinatesJson = "[[12.11, -86.23]]"
            }
        };

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(geofences);

        // Point far away (~200km from Managua)
        var result = await _sut.CheckPointInGeofenceAsync(13.5, -87.5);

        result.IsInside.Should().BeFalse();
        result.GeofenceName.Should().BeNull();
    }

    [Fact]
    public async Task CheckPointInGeofenceAsync_NoGeofences_ReturnsNotInside()
    {
        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Geofence>());

        var result = await _sut.CheckPointInGeofenceAsync(12.11, -86.23);

        result.IsInside.Should().BeFalse();
    }

    [Fact]
    public async Task CleanupOldPositionsAsync_ReturnsDeletedCount()
    {
        _gpsRepo.Setup(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>())).ReturnsAsync(42);

        var result = await _sut.CleanupOldPositionsAsync(90);

        result.Should().Be(42);
    }

    // ── Geofence Entry/Exit Transition Tests ──

    [Fact]
    public async Task ReceivePositionAsync_EntersGeofence_CreatesState()
    {
        var vehicleId = Guid.NewGuid();
        var geofenceId = Guid.NewGuid();
        var geofence = new Geofence
        {
            Id = geofenceId, Name = "Bodega Central",
            Type = "Circle", Active = true, Radius = 5.0,
            CoordinatesJson = "[[12.11, -86.23]]"
        };

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Geofence> { geofence });
        _geofenceStateRepo.Setup(r => r.GetActiveByVehicleAsync(vehicleId))
            .ReturnsAsync(new List<VehicleGeofenceState>());

        var request = new ReceiveGpsPositionRequest(
            "DEV-001", vehicleId, 12.11, -86.23, null, 65.5, 90,
            DateTime.UtcNow, true, 150000m, 75m, 32m, 85m, 5, 12);

        _mapper.Setup(m => m.Map<GpsPositionResponse>(It.IsAny<GpsPosition>()))
            .Returns(new GpsPositionResponse(
                Guid.NewGuid(), vehicleId, "ABC-123", 12.11, -86.23, null, 65.5, 90,
                request.GpsTimestamp, true, 150000m, 75m, 32m, 85m, 5, 12));

        await _sut.ReceivePositionAsync(request);

        _geofenceStateRepo.Verify(r => r.AddAsync(
            It.Is<VehicleGeofenceState>(s =>
                s.VehicleId == vehicleId &&
                s.GeofenceId == geofenceId &&
                s.IsInside == true)), Times.Once);
        _geofenceStateRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReceivePositionAsync_ExitsGeofence_MarksExit()
    {
        var vehicleId = Guid.NewGuid();
        var geofenceId = Guid.NewGuid();
        var geofence = new Geofence
        {
            Id = geofenceId, Name = "Bodega Central",
            Type = "Circle", Active = true, Radius = 1.0,
            CoordinatesJson = "[[12.11, -86.23]]"
        };

        var existingState = new VehicleGeofenceState
        {
            Id = Guid.NewGuid(), VehicleId = vehicleId, GeofenceId = geofenceId,
            IsInside = true, EnteredAt = DateTime.UtcNow.AddHours(-1)
        };

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Geofence> { geofence });
        _geofenceStateRepo.Setup(r => r.GetActiveByVehicleAsync(vehicleId))
            .ReturnsAsync(new List<VehicleGeofenceState> { existingState });

        // Position far from geofence center (~200km away)
        var request = new ReceiveGpsPositionRequest(
            "DEV-001", vehicleId, 13.5, -87.5, null, 65.5, 90,
            DateTime.UtcNow, true, 150000m, 75m, 32m, 85m, 5, 12);

        _mapper.Setup(m => m.Map<GpsPositionResponse>(It.IsAny<GpsPosition>()))
            .Returns(new GpsPositionResponse(
                Guid.NewGuid(), vehicleId, "ABC-123", 13.5, -87.5, null, 65.5, 90,
                request.GpsTimestamp, true, 150000m, 75m, 32m, 85m, 5, 12));

        await _sut.ReceivePositionAsync(request);

        existingState.IsInside.Should().BeFalse();
        existingState.ExitedAt.Should().NotBeNull();
        _geofenceStateRepo.Verify(r => r.UpdateAsync(existingState), Times.Once);
        _geofenceStateRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReceivePositionAsync_OutsideAllGeofences_NoStateChanges()
    {
        var vehicleId = Guid.NewGuid();
        var geofence = new Geofence
        {
            Id = Guid.NewGuid(), Name = "Bodega",
            Type = "Circle", Active = true, Radius = 1.0,
            CoordinatesJson = "[[12.11, -86.23]]"
        };

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Geofence> { geofence });
        _geofenceStateRepo.Setup(r => r.GetActiveByVehicleAsync(vehicleId))
            .ReturnsAsync(new List<VehicleGeofenceState>());

        // Position far from geofence
        var request = new ReceiveGpsPositionRequest(
            "DEV-001", vehicleId, 13.5, -87.5, null, 65.5, 90,
            DateTime.UtcNow, true, 150000m, 75m, 32m, 85m, 5, 12);

        _mapper.Setup(m => m.Map<GpsPositionResponse>(It.IsAny<GpsPosition>()))
            .Returns(new GpsPositionResponse(
                Guid.NewGuid(), vehicleId, "ABC-123", 13.5, -87.5, null, 65.5, 90,
                request.GpsTimestamp, true, 150000m, 75m, 32m, 85m, 5, 12));

        await _sut.ReceivePositionAsync(request);

        _geofenceStateRepo.Verify(r => r.AddAsync(It.IsAny<VehicleGeofenceState>()), Times.Never);
        _geofenceStateRepo.Verify(r => r.UpdateAsync(It.IsAny<VehicleGeofenceState>()), Times.Never);
    }

    [Fact]
    public async Task ReceivePositionAsync_NoActiveGeofences_NoStateChanges()
    {
        var vehicleId = Guid.NewGuid();

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Geofence>());
        _geofenceStateRepo.Setup(r => r.GetActiveByVehicleAsync(vehicleId))
            .ReturnsAsync(new List<VehicleGeofenceState>());

        var request = new ReceiveGpsPositionRequest(
            "DEV-001", vehicleId, 12.11, -86.23, null, 65.5, 90,
            DateTime.UtcNow, true, 150000m, 75m, 32m, 85m, 5, 12);

        _mapper.Setup(m => m.Map<GpsPositionResponse>(It.IsAny<GpsPosition>()))
            .Returns(new GpsPositionResponse(
                Guid.NewGuid(), vehicleId, "ABC-123", 12.11, -86.23, null, 65.5, 90,
                request.GpsTimestamp, true, 150000m, 75m, 32m, 85m, 5, 12));

        await _sut.ReceivePositionAsync(request);

        _geofenceStateRepo.Verify(r => r.AddAsync(It.IsAny<VehicleGeofenceState>()), Times.Never);
        _geofenceStateRepo.Verify(r => r.UpdateAsync(It.IsAny<VehicleGeofenceState>()), Times.Never);
    }

    [Fact]
    public async Task ReceivePositionAsync_StillInsideGeofence_NoStateChanges()
    {
        var vehicleId = Guid.NewGuid();
        var geofenceId = Guid.NewGuid();
        var geofence = new Geofence
        {
            Id = geofenceId, Name = "Bodega Central",
            Type = "Circle", Active = true, Radius = 5.0,
            CoordinatesJson = "[[12.11, -86.23]]"
        };

        var existingState = new VehicleGeofenceState
        {
            Id = Guid.NewGuid(), VehicleId = vehicleId, GeofenceId = geofenceId,
            IsInside = true, EnteredAt = DateTime.UtcNow.AddHours(-1)
        };

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Geofence> { geofence });
        _geofenceStateRepo.Setup(r => r.GetActiveByVehicleAsync(vehicleId))
            .ReturnsAsync(new List<VehicleGeofenceState> { existingState });

        // Position still inside geofence
        var request = new ReceiveGpsPositionRequest(
            "DEV-001", vehicleId, 12.11, -86.23, null, 65.5, 90,
            DateTime.UtcNow, true, 150000m, 75m, 32m, 85m, 5, 12);

        _mapper.Setup(m => m.Map<GpsPositionResponse>(It.IsAny<GpsPosition>()))
            .Returns(new GpsPositionResponse(
                Guid.NewGuid(), vehicleId, "ABC-123", 12.11, -86.23, null, 65.5, 90,
                request.GpsTimestamp, true, 150000m, 75m, 32m, 85m, 5, 12));

        await _sut.ReceivePositionAsync(request);

        _geofenceStateRepo.Verify(r => r.AddAsync(It.IsAny<VehicleGeofenceState>()), Times.Never);
        _geofenceStateRepo.Verify(r => r.UpdateAsync(It.IsAny<VehicleGeofenceState>()), Times.Never);
    }

    [Fact]
    public async Task ReceivePositionAsync_GeofenceProcessingFails_PositionStillSaved()
    {
        var vehicleId = Guid.NewGuid();

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ThrowsAsync(new Exception("DB down"));

        var request = new ReceiveGpsPositionRequest(
            "DEV-001", vehicleId, 12.11, -86.23, null, 65.5, 90,
            DateTime.UtcNow, true, 150000m, 75m, 32m, 85m, 5, 12);

        _mapper.Setup(m => m.Map<GpsPositionResponse>(It.IsAny<GpsPosition>()))
            .Returns(new GpsPositionResponse(
                Guid.NewGuid(), vehicleId, "ABC-123", 12.11, -86.23, null, 65.5, 90,
                request.GpsTimestamp, true, 150000m, 75m, 32m, 85m, 5, 12));

        var result = await _sut.ReceivePositionAsync(request);

        result.Should().NotBeNull();
        _gpsRepo.Verify(r => r.AddAsync(It.IsAny<GpsPosition>()), Times.Once);
        _gpsRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── Geofence Notification Tests ──

    [Fact]
    public async Task ReceivePositionAsync_EntersGeofence_SendsNotification()
    {
        var vehicleId = Guid.NewGuid();
        var geofence = new Geofence
        {
            Id = Guid.NewGuid(), Name = "Bodega Central",
            Type = "Circle", Active = true, Radius = 5.0,
            CoordinatesJson = "[[12.11, -86.23]]"
        };

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Geofence> { geofence });
        _geofenceStateRepo.Setup(r => r.GetActiveByVehicleAsync(vehicleId))
            .ReturnsAsync(new List<VehicleGeofenceState>());

        var request = new ReceiveGpsPositionRequest(
            "DEV-001", vehicleId, 12.11, -86.23, null, 65.5, 90,
            DateTime.UtcNow, true, 150000m, 75m, 32m, 85m, 5, 12);

        _mapper.Setup(m => m.Map<GpsPositionResponse>(It.IsAny<GpsPosition>()))
            .Returns(new GpsPositionResponse(
                Guid.NewGuid(), vehicleId, "ABC-123", 12.11, -86.23, null, 65.5, 90,
                request.GpsTimestamp, true, 150000m, 75m, 32m, 85m, 5, 12));

        await _sut.ReceivePositionAsync(request);

        _notification.Verify(n => n.NotifyTenantAsync(
            _companyId.ToString(),
            "Vehículo entró a geocerca",
            It.Is<string>(m => m.Contains("entró") && m.Contains("Bodega Central")),
            "fleet_geofence",
            vehicleId.ToString()), Times.Once);
    }

    [Fact]
    public async Task ReceivePositionAsync_ExitsGeofence_SendsNotification()
    {
        var vehicleId = Guid.NewGuid();
        var geofenceId = Guid.NewGuid();
        var geofence = new Geofence
        {
            Id = geofenceId, Name = "Bodega Central",
            Type = "Circle", Active = true, Radius = 1.0,
            CoordinatesJson = "[[12.11, -86.23]]"
        };

        var existingState = new VehicleGeofenceState
        {
            Id = Guid.NewGuid(), VehicleId = vehicleId, GeofenceId = geofenceId,
            IsInside = true, EnteredAt = DateTime.UtcNow.AddHours(-1)
        };

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Geofence> { geofence });
        _geofenceStateRepo.Setup(r => r.GetActiveByVehicleAsync(vehicleId))
            .ReturnsAsync(new List<VehicleGeofenceState> { existingState });

        var request = new ReceiveGpsPositionRequest(
            "DEV-001", vehicleId, 13.5, -87.5, null, 65.5, 90,
            DateTime.UtcNow, true, 150000m, 75m, 32m, 85m, 5, 12);

        _mapper.Setup(m => m.Map<GpsPositionResponse>(It.IsAny<GpsPosition>()))
            .Returns(new GpsPositionResponse(
                Guid.NewGuid(), vehicleId, "ABC-123", 13.5, -87.5, null, 65.5, 90,
                request.GpsTimestamp, true, 150000m, 75m, 32m, 85m, 5, 12));

        await _sut.ReceivePositionAsync(request);

        _notification.Verify(n => n.NotifyTenantAsync(
            _companyId.ToString(),
            "Vehículo salió de geocerca",
            It.Is<string>(m => m.Contains("salió") && m.Contains("Bodega Central")),
            "fleet_geofence",
            vehicleId.ToString()), Times.Once);
    }

    // ── Dirty Flag Tests ──

    [Fact]
    public async Task ReceivePositionAsync_OutsideAllGeofences_NoStateSaveChanges()
    {
        var vehicleId = Guid.NewGuid();
        var geofence = new Geofence
        {
            Id = Guid.NewGuid(), Name = "Bodega",
            Type = "Circle", Active = true, Radius = 1.0,
            CoordinatesJson = "[[12.11, -86.23]]"
        };

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Geofence> { geofence });
        _geofenceStateRepo.Setup(r => r.GetActiveByVehicleAsync(vehicleId))
            .ReturnsAsync(new List<VehicleGeofenceState>());

        var request = new ReceiveGpsPositionRequest(
            "DEV-001", vehicleId, 13.5, -87.5, null, 65.5, 90,
            DateTime.UtcNow, true, 150000m, 75m, 32m, 85m, 5, 12);

        _mapper.Setup(m => m.Map<GpsPositionResponse>(It.IsAny<GpsPosition>()))
            .Returns(new GpsPositionResponse(
                Guid.NewGuid(), vehicleId, "ABC-123", 13.5, -87.5, null, 65.5, 90,
                request.GpsTimestamp, true, 150000m, 75m, 32m, 85m, 5, 12));

        await _sut.ReceivePositionAsync(request);

        _geofenceStateRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    // ══════════════════════════════════════════════════════════════
    //  Null Safety Tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetFleetPositionsAsync_WithNullBrand_DoesNotThrow()
    {
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId, Plate = "ABC-123", Model = "Hilux",
            Brand = null!, Driver = null
        };

        var positions = new List<GpsPosition>
        {
            new()
            {
                Id = Guid.NewGuid(), VehicleId = vehicleId,
                Latitude = 12.11, Longitude = -86.23, Speed = 65,
                Heading = 90, GpsTimestamp = DateTime.UtcNow,
                IgnitionOn = true, FuelLevel = 75m, Vehicle = vehicle
            }
        };

        _gpsRepo.Setup(r => r.GetLatestPerVehicleAsync(_companyId)).ReturnsAsync(positions);

        var result = await _sut.GetFleetPositionsAsync();

        result.Should().HaveCount(1);
        result[0].VehiclePlate.Should().Be("ABC-123");
        result[0].VehicleBrandModel.Should().Be("Hilux");
        result[0].DriverName.Should().BeNull();
    }

    [Fact]
    public async Task GetFleetPositionsAsync_WithNullDriver_DoesNotThrow()
    {
        var vehicleId = Guid.NewGuid();
        var brand = new VehicleBrand { Name = "Toyota" };
        var vehicle = new Vehicle
        {
            Id = vehicleId, Plate = "DEF-456", Model = "Hilux",
            Brand = brand, Driver = null
        };

        var positions = new List<GpsPosition>
        {
            new()
            {
                Id = Guid.NewGuid(), VehicleId = vehicleId,
                Latitude = 12.11, Longitude = -86.23, Speed = 65,
                Heading = 90, GpsTimestamp = DateTime.UtcNow,
                IgnitionOn = true, FuelLevel = 75m, Vehicle = vehicle
            }
        };

        _gpsRepo.Setup(r => r.GetLatestPerVehicleAsync(_companyId)).ReturnsAsync(positions);

        var result = await _sut.GetFleetPositionsAsync();

        result.Should().HaveCount(1);
        result[0].DriverName.Should().BeNull();
        result[0].VehicleBrandModel.Should().Be("Toyota Hilux");
    }

    [Fact]
    public async Task GetFleetPositionsAsync_WithNullVehicle_DoesNotThrow()
    {
        var positions = new List<GpsPosition>
        {
            new()
            {
                Id = Guid.NewGuid(), VehicleId = Guid.NewGuid(),
                Latitude = 12.11, Longitude = -86.23, Speed = 65,
                Heading = 90, GpsTimestamp = DateTime.UtcNow,
                Vehicle = null!
            }
        };

        _gpsRepo.Setup(r => r.GetLatestPerVehicleAsync(_companyId)).ReturnsAsync(positions);

        var result = await _sut.GetFleetPositionsAsync();

        result.Should().HaveCount(1);
        result[0].VehiclePlate.Should().BeEmpty();
        result[0].VehicleBrandModel.Should().BeEmpty();
        result[0].DriverName.Should().BeNull();
    }

    [Fact]
    public async Task GetFleetPositionsAsync_EmptyList_ReturnsEmpty()
    {
        _gpsRepo.Setup(r => r.GetLatestPerVehicleAsync(_companyId)).ReturnsAsync(new List<GpsPosition>());

        var result = await _sut.GetFleetPositionsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetVehicleHistoryAsync_WithNullVehicleOnPosition_ReturnsNull()
    {
        var vehicleId = Guid.NewGuid();
        var positions = new List<GpsPosition>
        {
            new()
            {
                Id = Guid.NewGuid(), VehicleId = vehicleId,
                Latitude = 12.11, Longitude = -86.23, Speed = 60,
                GpsTimestamp = DateTime.UtcNow, Vehicle = null!
            }
        };

        _gpsRepo.Setup(r => r.GetByVehicleAndDateRangeAsync(vehicleId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(positions);

        var result = await _sut.GetVehicleHistoryAsync(vehicleId, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ReceiveBulkAsync_WithEmptyPositions_ReturnsZero()
    {
        _vehicleRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(new List<Vehicle>());

        var request = new BulkReceiveGpsRequest(new List<ReceiveGpsPositionRequest>());

        var count = await _sut.ReceiveBulkAsync(request);

        count.Should().Be(0);
        _gpsRepo.Verify(r => r.AddRangeAsync(It.IsAny<List<GpsPosition>>()), Times.Never);
    }

    [Fact]
    public async Task CheckPointInGeofenceAsync_WithNullCoordinatesJson_DoesNotThrow()
    {
        var geofence = new Geofence
        {
            Id = Guid.NewGuid(), Name = "Test",
            Type = "Circle", Active = true, Radius = 5.0,
            CoordinatesJson = null!
        };

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Geofence> { geofence });

        var result = await _sut.CheckPointInGeofenceAsync(12.11, -86.23);

        result.IsInside.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPointInGeofenceAsync_WithInvalidJson_DoesNotThrow()
    {
        var geofence = new Geofence
        {
            Id = Guid.NewGuid(), Name = "Test",
            Type = "Circle", Active = true, Radius = 5.0,
            CoordinatesJson = "not-json"
        };

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Geofence> { geofence });

        var result = await _sut.CheckPointInGeofenceAsync(12.11, -86.23);

        result.IsInside.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════
    //  Error Isolation Tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ReceiveBulkAsync_GeofenceStateRepoThrows_PositionsStillSaved()
    {
        var vehicleId = Guid.NewGuid();
        var vehicles = new List<Vehicle>
        {
            new() { Id = vehicleId, GpsDeviceId = "DEV-001" }
        };

        _vehicleRepo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(vehicles);
        _geofenceStateRepo.Setup(r => r.GetActiveByVehicleAsync(vehicleId))
            .ThrowsAsync(new Exception("State repo down"));

        var request = new BulkReceiveGpsRequest(new List<ReceiveGpsPositionRequest>
        {
            new("DEV-001", vehicleId, 12.11, -86.23, null, null, null, DateTime.UtcNow, null, null, null, null, null, null, null),
        });

        var count = await _sut.ReceiveBulkAsync(request);

        count.Should().Be(1);
        _gpsRepo.Verify(r => r.AddRangeAsync(It.IsAny<List<GpsPosition>>()), Times.Once);
        _gpsRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReceivePositionAsync_NotificationFails_PositionStillSaved()
    {
        var vehicleId = Guid.NewGuid();
        var geofence = new Geofence
        {
            Id = Guid.NewGuid(), Name = "Bodega Central",
            Type = "Circle", Active = true, Radius = 5.0,
            CoordinatesJson = "[[12.11, -86.23]]"
        };

        _geofenceRepo.Setup(r => r.GetActiveAsync()).ReturnsAsync(new List<Geofence> { geofence });
        _geofenceStateRepo.Setup(r => r.GetActiveByVehicleAsync(vehicleId))
            .ReturnsAsync(new List<VehicleGeofenceState>());
        _notification.Setup(n => n.NotifyTenantAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Notification service down"));

        var request = new ReceiveGpsPositionRequest(
            "DEV-001", vehicleId, 12.11, -86.23, null, 65.5, 90,
            DateTime.UtcNow, true, 150000m, 75m, 32m, 85m, 5, 12);

        _mapper.Setup(m => m.Map<GpsPositionResponse>(It.IsAny<GpsPosition>()))
            .Returns(new GpsPositionResponse(
                Guid.NewGuid(), vehicleId, "ABC-123", 12.11, -86.23, null, 65.5, 90,
                request.GpsTimestamp, true, 150000m, 75m, 32m, 85m, 5, 12));

        var result = await _sut.ReceivePositionAsync(request);

        result.Should().NotBeNull();
        _gpsRepo.Verify(r => r.AddAsync(It.IsAny<GpsPosition>()), Times.Once);
        _gpsRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetFleetPositionsAsync_GpsRepoThrows_PropagatesException()
    {
        _gpsRepo.Setup(r => r.GetLatestPerVehicleAsync(_companyId))
            .ThrowsAsync(new Exception("DB connection lost"));

        var act = () => _sut.GetFleetPositionsAsync();

        await act.Should().ThrowAsync<Exception>().WithMessage("DB connection lost");
    }
}
