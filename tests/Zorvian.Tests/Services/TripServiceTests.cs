using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Tests.Services;

public sealed class TripServiceTests
{
    private readonly Mock<ITripRepository> _repo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly TripService _sut;

    public TripServiceTests()
    {
        _sut = new TripService(_repo.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedTrips()
    {
        var trips = new List<Trip>
        {
            new() { Id = Guid.NewGuid(), Code = "TRIP-001", Status = "Planned" },
            new() { Id = Guid.NewGuid(), Code = "TRIP-002", Status = "InProgress" },
        };

        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(trips);
        _mapper.Setup(m => m.Map<List<TripResponse>>(trips)).Returns(
            trips.Select(t => new TripResponse(
                t.Id, t.Code, Guid.NewGuid(), "ABC-123", "Toyota Hilux",
                Guid.NewGuid(), "Carlos López", null, null,
                DateTime.UtcNow, null, "Managua", "León", 0, null, null, null,
                t.Status, null, DateTime.UtcNow
            )).ToList());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsTrip()
    {
        var id = Guid.NewGuid();
        var trip = new Trip { Id = id, Code = "TRIP-001" };
        var response = new TripResponse(
            id, "TRIP-001", Guid.NewGuid(), "ABC-123", "Toyota Hilux",
            Guid.NewGuid(), "Carlos López", null, null,
            DateTime.UtcNow, null, "Managua", "León", 100000, 100050, 50, 120,
            "Completed", "Entrega a tiempo", DateTime.UtcNow);

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(trip);
        _mapper.Setup(m => m.Map<TripResponse>(trip)).Returns(response);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Code.Should().Be("TRIP-001");
        result.TotalKm.Should().Be(50);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Trip?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_SetsStatusToPlanned()
    {
        var request = new CreateTripRequest(
            "TRIP-003", Guid.NewGuid(), Guid.NewGuid(), null,
            DateTime.UtcNow, null, "Managua", "Masaya", 100000, null, null, null, null);

        var trip = new Trip();
        _mapper.Setup(m => m.Map<Trip>(request)).Returns(trip);
        _mapper.Setup(m => m.Map<TripResponse>(It.IsAny<Trip>()))
            .Returns(new TripResponse(
                Guid.NewGuid(), "TRIP-003", Guid.NewGuid(), "ABC-123", "Toyota Hilux",
                Guid.NewGuid(), "Carlos López", null, null,
                DateTime.UtcNow, null, "Managua", "Masaya", 100000, null, null, null,
                "Planned", null, DateTime.UtcNow));

        await _sut.CreateAsync(request);

        trip.Status.Should().Be("Planned");
        _repo.Verify(r => r.AddAsync(trip), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Trip?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateTripRequest(
            null, null, null, null, null, null, null, null, null, null, null, null, null, null));

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_MapsAndSaves()
    {
        var id = Guid.NewGuid();
        var trip = new Trip { Id = id, Status = "Planned" };
        var response = new TripResponse(
            id, "TRIP-001", Guid.NewGuid(), "ABC-123", "Toyota Hilux",
            Guid.NewGuid(), "Carlos López", null, null,
            DateTime.UtcNow, null, "Managua", "León", 100000, 100050, 50, 120,
            "InProgress", null, DateTime.UtcNow);
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(trip);
        _mapper.Setup(m => m.Map<TripResponse>(trip)).Returns(response);

        var result = await _sut.UpdateAsync(id, new UpdateTripRequest(
            null, null, null, null, null, null, null, null, null, null, null, null, "InProgress", null));

        result.Should().NotBeNull();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Trip?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        var trip = new Trip { Id = id };
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(trip);

        var result = await _sut.DeleteAsync(id);

        result.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(trip), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
