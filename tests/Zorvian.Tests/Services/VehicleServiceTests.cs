using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class VehicleServiceTests
{
    private readonly Mock<IVehicleRepository> _repo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly VehicleService _sut;
    private readonly Guid _companyId = Guid.NewGuid();

    public VehicleServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _sut = new VehicleService(_repo.Object, _tenant.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetAllAsync_WithValidTenant_ReturnsMappedVehicles()
    {
        var vehicles = new List<Vehicle>
        {
            new() { Id = Guid.NewGuid(), Plate = "ABC-123", Status = "Active" },
            new() { Id = Guid.NewGuid(), Plate = "DEF-456", Status = "Maintenance" },
        };

        _repo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(vehicles);
        _mapper.Setup(m => m.Map<List<VehicleResponse>>(vehicles)).Returns(
            vehicles.Select(v => new VehicleResponse(
                v.Id, "V-001", v.Plate, Guid.NewGuid(), "Toyota", "Hilux", 2024,
                null, null, null, "Blanco", Guid.NewGuid(), "Camioneta",
                Guid.NewGuid(), "Diesel", Guid.NewGuid(), "Sede Central",
                50000, 1500, null, null, v.Status, null, null, null, null, null, null, DateTime.UtcNow
            )).ToList());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        _repo.Verify(r => r.GetAllAsync(_companyId), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidTenant_ReturnsEmpty()
    {
        _tenant.Setup(t => t.TenantId).Returns("not-a-guid");

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
        _repo.Verify(r => r.GetAllAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedResponse()
    {
        var id = Guid.NewGuid();
        var vehicle = new Vehicle { Id = id, Plate = "ABC-123", Status = "Active" };
        var response = new VehicleResponse(
            id, "V-001", "ABC-123", Guid.NewGuid(), "Toyota", "Hilux", 2024,
            null, null, null, "Blanco", Guid.NewGuid(), "Camioneta",
            Guid.NewGuid(), "Diesel", Guid.NewGuid(), "Sede Central",
            50000, 1500, null, null, "Active", null, null, null, null, null, null, DateTime.UtcNow);

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(vehicle);
        _mapper.Setup(m => m.Map<VehicleResponse>(vehicle)).Returns(response);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Plate.Should().Be("ABC-123");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Vehicle?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_SetsStatusToActive()
    {
        var request = new CreateVehicleRequest(
            "V-001", "ABC-123", Guid.NewGuid(), "Hilux", 2024,
            null, null, null, "Blanco", Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), 0, 1500, null, null, null, null, null, null);

        var vehicle = new Vehicle();
        _mapper.Setup(m => m.Map<Vehicle>(request)).Returns(vehicle);
        _mapper.Setup(m => m.Map<VehicleResponse>(It.IsAny<Vehicle>()))
            .Returns(new VehicleResponse(
                Guid.NewGuid(), "V-001", "ABC-123", Guid.NewGuid(), "Toyota", "Hilux", 2024,
                null, null, null, "Blanco", Guid.NewGuid(), "Camioneta",
                Guid.NewGuid(), "Diesel", Guid.NewGuid(), "Sede Central",
                0, 1500, null, null, "Active", null, null, null, null, null, null, DateTime.UtcNow));

        await _sut.CreateAsync(request);

        vehicle.Status.Should().Be("Active");
        _repo.Verify(r => r.AddAsync(vehicle), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Vehicle?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateVehicleRequest(
            null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null));

        result.Should().BeNull();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_MapsAndSaves()
    {
        var id = Guid.NewGuid();
        var vehicle = new Vehicle { Id = id, Plate = "OLD-001" };
        var response = new VehicleResponse(
            id, "V-001", "NEW-001", Guid.NewGuid(), "Toyota", "Hilux", 2024,
            null, null, null, "Blanco", Guid.NewGuid(), "Camioneta",
            Guid.NewGuid(), "Diesel", Guid.NewGuid(), "Sede Central",
            50000, 1500, null, null, "Active", null, null, null, null, null, null, DateTime.UtcNow);
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(vehicle);
        _mapper.Setup(m => m.Map(It.IsAny<UpdateVehicleRequest>(), vehicle));
        _mapper.Setup(m => m.Map<VehicleResponse>(vehicle)).Returns(response);

        var result = await _sut.UpdateAsync(id, new UpdateVehicleRequest(
            null, "NEW-001", null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, null));

        result.Should().NotBeNull();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Vehicle?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        var vehicle = new Vehicle { Id = id };
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(vehicle);
        _repo.Setup(r => r.DeleteAsync(vehicle));

        var result = await _sut.DeleteAsync(id);

        result.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(vehicle), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
