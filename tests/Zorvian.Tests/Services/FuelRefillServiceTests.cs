using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class FuelRefillServiceTests
{
    private readonly Mock<IFuelRefillRepository> _repo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly FuelRefillService _sut;
    private readonly Guid _companyId = Guid.NewGuid();

    public FuelRefillServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _sut = new FuelRefillService(_repo.Object, _tenant.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetAllAsync_WithValidTenant_ReturnsMappedFuelRefills()
    {
        var refills = new List<FuelRefill>
        {
            new() { Id = Guid.NewGuid(), Liters = 45.5m, TotalCost = 2500m },
        };

        _repo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(refills);
        _mapper.Setup(m => m.Map<List<FuelRefillResponse>>(refills)).Returns(
            refills.Select(f => new FuelRefillResponse(
                f.Id, DateTime.UtcNow, Guid.NewGuid(), "ABC-123", "Toyota Hilux",
                Guid.NewGuid(), "Carlos López", Guid.NewGuid(), "Diesel",
                f.Liters, 55m, f.TotalCost, 100000, null, null, null,
                "Station", "Cash", null, null, true, false, null, DateTime.UtcNow
            )).ToList());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].Liters.Should().Be(45.5m);
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
    public async Task GetByIdAsync_WhenFound_ReturnsFuelRefill()
    {
        var id = Guid.NewGuid();
        var refill = new FuelRefill { Id = id, Liters = 50m };
        var response = new FuelRefillResponse(
            id, DateTime.UtcNow, Guid.NewGuid(), "ABC-123", "Toyota Hilux",
            Guid.NewGuid(), "Carlos López", Guid.NewGuid(), "Diesel",
            50m, 55m, 2750m, 100000, null, null, null,
            "Station", "Cash", null, null, true, false, null, DateTime.UtcNow);

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(refill);
        _mapper.Setup(m => m.Map<FuelRefillResponse>(refill)).Returns(response);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Liters.Should().Be(50m);
        result.TotalCost.Should().Be(2750m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FuelRefill?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_MapsAndSaves()
    {
        var vehicleId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var fuelTypeId = Guid.NewGuid();

        var request = new CreateFuelRefillRequest(
            DateTime.UtcNow, vehicleId, driverId, fuelTypeId,
            45.5m, 55m, 2502.5m, 100000, null, null,
            "Station", "Cash", null, null, true);

        var entity = new FuelRefill();
        _mapper.Setup(m => m.Map<FuelRefill>(request)).Returns(entity);
        _mapper.Setup(m => m.Map<FuelRefillResponse>(It.IsAny<FuelRefill>()))
            .Returns(new FuelRefillResponse(
                Guid.NewGuid(), DateTime.UtcNow, vehicleId, "ABC-123", "Toyota Hilux",
                driverId, "Carlos López", fuelTypeId, "Diesel",
                45.5m, 55m, 2502.5m, 100000, null, null, null,
                "Station", "Cash", null, null, true, false, null, DateTime.UtcNow));

        await _sut.CreateAsync(request);

        _repo.Verify(r => r.AddAsync(entity), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FuelRefill?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateFuelRefillRequest(
            null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null));

        result.Should().BeNull();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_MapsAndSaves()
    {
        var id = Guid.NewGuid();
        var refill = new FuelRefill { Id = id };
        var response = new FuelRefillResponse(
            id, DateTime.UtcNow, Guid.NewGuid(), "ABC-123", "Toyota Hilux",
            Guid.NewGuid(), "Carlos López", Guid.NewGuid(), "Diesel",
            60m, 55m, 3300m, 100000, null, null, null,
            "Station", "Cash", null, null, true, false, null, DateTime.UtcNow);
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(refill);
        _mapper.Setup(m => m.Map<FuelRefillResponse>(refill)).Returns(response);

        var result = await _sut.UpdateAsync(id, new UpdateFuelRefillRequest(
            null, null, null, null, 60m, null, null, null, null, null,
            null, null, null, null, null, null, null));

        result.Should().NotBeNull();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FuelRefill?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        var refill = new FuelRefill { Id = id };
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(refill);

        var result = await _sut.DeleteAsync(id);

        result.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(refill), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
