using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Tests.Services;

public sealed class RouteServiceTests
{
    private readonly Mock<IRouteRepository> _repo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly RouteService _sut;

    public RouteServiceTests()
    {
        _sut = new RouteService(_repo.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedRoutes()
    {
        var routes = new List<Route>
        {
            new() { Id = Guid.NewGuid(), Name = "Ruta Managua-León", Status = "Planned" },
            new() { Id = Guid.NewGuid(), Name = "Ruta Masaya-Granada", Status = "InProgress" },
        };

        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(routes);
        _mapper.Setup(m => m.Map<List<RouteResponse>>(routes)).Returns(
            routes.Select(r => new RouteResponse(
                r.Id, "RT-001", r.Name, "Urban",
                DateOnly.FromDateTime(DateTime.UtcNow),
                new TimeOnly(8, 0), new TimeOnly(17, 0),
                "Managua", "León", 90.5m, 120,
                Guid.NewGuid(), "ABC-123",
                Guid.NewGuid(), "Carlos López",
                null, null,
                r.Status, 5000m, null,
                Guid.NewGuid(), "Sede Central",
                [], DateTime.UtcNow
            )).ToList());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        _repo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsRoute()
    {
        var id = Guid.NewGuid();
        var route = new Route { Id = id, Name = "Ruta Managua-León" };
        var response = new RouteResponse(
            id, "RT-001", "Ruta Managua-León", "Interurban",
            DateOnly.FromDateTime(DateTime.UtcNow),
            new TimeOnly(6, 0), new TimeOnly(20, 0),
            "Managua", "León", 120.5m, 180,
            Guid.NewGuid(), "ABC-123",
            Guid.NewGuid(), "Carlos López",
            null, null,
            "InProgress", 8000m, "Ruta principal",
            Guid.NewGuid(), "Sede Central",
            [], DateTime.UtcNow);

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(route);
        _mapper.Setup(m => m.Map<RouteResponse>(route)).Returns(response);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Ruta Managua-León");
        result.Type.Should().Be("Interurban");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Route?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_SetsStatusToPlanned()
    {
        var branchId = Guid.NewGuid();
        var request = new CreateRouteRequest(
            "RT-002", "Ruta Nueva", "Urban",
            DateOnly.FromDateTime(DateTime.UtcNow),
            new TimeOnly(8, 0), new TimeOnly(17, 0),
            "Masaya", "Granada", 45m, 60,
            null, null, null, 3000m, null, branchId, null);

        var route = new Route();
        _mapper.Setup(m => m.Map<Route>(request)).Returns(route);
        _mapper.Setup(m => m.Map<RouteResponse>(It.IsAny<Route>()))
            .Returns(new RouteResponse(
                Guid.NewGuid(), "RT-002", "Ruta Nueva", "Urban",
                DateOnly.FromDateTime(DateTime.UtcNow),
                new TimeOnly(8, 0), new TimeOnly(17, 0),
                "Masaya", "Granada", 45m, 60,
                null, null, null, null, null, null,
                "Planned", 3000m, null,
                branchId, "Sede Central",
                [], DateTime.UtcNow));

        await _sut.CreateAsync(request);

        route.Status.Should().Be("Planned");
        _repo.Verify(r => r.AddAsync(route), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Route?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateRouteRequest(
            null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null));

        result.Should().BeNull();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_MapsAndSaves()
    {
        var id = Guid.NewGuid();
        var route = new Route { Id = id, Name = "Ruta Vieja" };
        var response = new RouteResponse(
            id, "RT-001", "Ruta Actualizada", "Urban",
            DateOnly.FromDateTime(DateTime.UtcNow),
            new TimeOnly(8, 0), new TimeOnly(17, 0),
            "Managua", null, 50m, 60,
            null, null, null, null, null, null,
            "Planned", 3000m, null,
            Guid.NewGuid(), "Sede Central",
            [], DateTime.UtcNow);
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(route);
        _mapper.Setup(m => m.Map<RouteResponse>(route)).Returns(response);

        var result = await _sut.UpdateAsync(id, new UpdateRouteRequest(
            null, "Ruta Actualizada", null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null));

        result.Should().NotBeNull();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Route?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        var route = new Route { Id = id };
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(route);

        var result = await _sut.DeleteAsync(id);

        result.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(route), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
