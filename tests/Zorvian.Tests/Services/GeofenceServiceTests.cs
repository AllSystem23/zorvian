using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Tests.Services;

public sealed class GeofenceServiceTests
{
    private readonly Mock<IGeofenceRepository> _repository = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GeofenceService _sut;

    public GeofenceServiceTests()
    {
        _sut = new GeofenceService(_repository.Object, _mapper.Object);
    }

    // ══════════════════════════════════════════════════════════════
    //  Happy Path Tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllAsync_ReturnsMappedList()
    {
        var geofences = new List<Geofence>
        {
            new() { Id = Guid.NewGuid(), Name = "Bodega Central", Type = "Circle", Active = true, Radius = 5.0 },
            new() { Id = Guid.NewGuid(), Name = "Zona Norte", Type = "Polygon", Active = true }
        };

        var responses = new List<GeofenceResponse>
        {
            new(Guid.NewGuid(), "Bodega Central", "Circle", "[]", 5.0, true, DateTime.UtcNow),
            new(Guid.NewGuid(), "Zona Norte", "Polygon", "[]", null, true, DateTime.UtcNow)
        };

        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(geofences);
        _mapper.Setup(m => m.Map<List<GeofenceResponse>>(geofences)).Returns(responses);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Bodega Central");
        result[1].Name.Should().Be("Zona Norte");
    }

    [Fact]
    public async Task GetAllAsync_EmptyList_ReturnsEmpty()
    {
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Geofence>());
        _mapper.Setup(m => m.Map<List<GeofenceResponse>>(It.IsAny<List<Geofence>>())).Returns(new List<GeofenceResponse>());

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMapped()
    {
        var id = Guid.NewGuid();
        var geofence = new Geofence { Id = id, Name = "Bodega", Type = "Circle", Active = true, Radius = 2.5 };
        var response = new GeofenceResponse(id, "Bodega", "Circle", "[]", 2.5, true, DateTime.UtcNow);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(geofence);
        _mapper.Setup(m => m.Map<GeofenceResponse?>(geofence)).Returns(response);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Bodega");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Geofence?)null);
        _mapper.Setup(m => m.Map<GeofenceResponse?>(null)).Returns((GeofenceResponse?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveMapped()
    {
        var active = new List<Geofence>
        {
            new() { Id = Guid.NewGuid(), Name = "Active Zone", Type = "Circle", Active = true, Radius = 5.0 }
        };

        var responses = new List<GeofenceResponse>
        {
            new(Guid.NewGuid(), "Active Zone", "Circle", "[]", 5.0, true, DateTime.UtcNow)
        };

        _repository.Setup(r => r.GetActiveAsync()).ReturnsAsync(active);
        _mapper.Setup(m => m.Map<List<GeofenceResponse>>(active)).Returns(responses);

        var result = await _sut.GetActiveAsync();

        result.Should().HaveCount(1);
        result[0].Active.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_SavesAndReturnsMapped()
    {
        var request = new CreateGeofenceRequest("Nueva Zona", "Circle", "[[12.11,-86.23]]", 5.0);

        _repository.Setup(r => r.AddAsync(It.IsAny<Geofence>())).Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mapper.Setup(m => m.Map<GeofenceResponse>(It.IsAny<Geofence>()))
            .Returns(new GeofenceResponse(Guid.NewGuid(), "Nueva Zona", "Circle", "[[12.11,-86.23]]", 5.0, true, DateTime.UtcNow));

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Nueva Zona");
        result.Active.Should().BeTrue();
        _repository.Verify(r => r.AddAsync(It.Is<Geofence>(g =>
            g.Name == "Nueva Zona" && g.Type == "Circle" && g.Active == true)), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesAndReturns()
    {
        var id = Guid.NewGuid();
        var geofence = new Geofence { Id = id, Name = "Old Name", Type = "Circle", Active = true };
        var request = new UpdateGeofenceRequest("New Name", null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(geofence);
        _repository.Setup(r => r.UpdateAsync(geofence)).Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mapper.Setup(m => m.Map<GeofenceResponse>(geofence))
            .Returns(new GeofenceResponse(id, "New Name", "Circle", "[]", null, true, DateTime.UtcNow));

        var result = await _sut.UpdateAsync(id, request);

        result.Should().NotBeNull();
        geofence.Name.Should().Be("New Name");
        _repository.Verify(r => r.UpdateAsync(geofence), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Geofence?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateGeofenceRequest("Test", null, null, null, null));

        result.Should().BeNull();
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Geofence>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var geofence = new Geofence { Id = id, Name = "ToDelete" };

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(geofence);
        _repository.Setup(r => r.DeleteAsync(geofence)).Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync(id);

        result.Should().BeTrue();
        _repository.Verify(r => r.DeleteAsync(geofence), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Geofence?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
        _repository.Verify(r => r.DeleteAsync(It.IsAny<Geofence>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    // ══════════════════════════════════════════════════════════════
    //  Null Safety Tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateAsync_WithNullName_KeepsExistingName()
    {
        var id = Guid.NewGuid();
        var geofence = new Geofence { Id = id, Name = "Original", Type = "Circle", Active = true };
        var request = new UpdateGeofenceRequest(null, null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(geofence);
        _repository.Setup(r => r.UpdateAsync(geofence)).Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mapper.Setup(m => m.Map<GeofenceResponse>(geofence))
            .Returns(new GeofenceResponse(id, "Original", "Circle", "[]", null, true, DateTime.UtcNow));

        var result = await _sut.UpdateAsync(id, request);

        result.Should().NotBeNull();
        geofence.Name.Should().Be("Original");
    }

    [Fact]
    public async Task UpdateAsync_WithNullType_KeepsExistingType()
    {
        var id = Guid.NewGuid();
        var geofence = new Geofence { Id = id, Name = "Zone", Type = "Polygon", Active = true };
        var request = new UpdateGeofenceRequest(null, null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(geofence);
        _repository.Setup(r => r.UpdateAsync(geofence)).Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mapper.Setup(m => m.Map<GeofenceResponse>(geofence))
            .Returns(new GeofenceResponse(id, "Zone", "Polygon", "[]", null, true, DateTime.UtcNow));

        var result = await _sut.UpdateAsync(id, request);

        geofence.Type.Should().Be("Polygon");
    }

    [Fact]
    public async Task UpdateAsync_WithNullActive_KeepsExistingActive()
    {
        var id = Guid.NewGuid();
        var geofence = new Geofence { Id = id, Name = "Zone", Type = "Circle", Active = true };
        var request = new UpdateGeofenceRequest(null, null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(geofence);
        _repository.Setup(r => r.UpdateAsync(geofence)).Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mapper.Setup(m => m.Map<GeofenceResponse>(geofence))
            .Returns(new GeofenceResponse(id, "Zone", "Circle", "[]", null, true, DateTime.UtcNow));

        var result = await _sut.UpdateAsync(id, request);

        geofence.Active.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithNullCoordinatesJson_KeepsExisting()
    {
        var id = Guid.NewGuid();
        var geofence = new Geofence { Id = id, Name = "Zone", Type = "Circle", Active = true, CoordinatesJson = "[[12,-86]]" };
        var request = new UpdateGeofenceRequest(null, null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(geofence);
        _repository.Setup(r => r.UpdateAsync(geofence)).Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mapper.Setup(m => m.Map<GeofenceResponse>(geofence))
            .Returns(new GeofenceResponse(id, "Zone", "Circle", "[[12,-86]]", null, true, DateTime.UtcNow));

        var result = await _sut.UpdateAsync(id, request);

        geofence.CoordinatesJson.Should().Be("[[12,-86]]");
    }

    [Fact]
    public async Task UpdateAsync_WithNullRadius_KeepsExistingRadius()
    {
        var id = Guid.NewGuid();
        var geofence = new Geofence { Id = id, Name = "Zone", Type = "Circle", Active = true, Radius = 10.0 };
        var request = new UpdateGeofenceRequest(null, null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(geofence);
        _repository.Setup(r => r.UpdateAsync(geofence)).Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mapper.Setup(m => m.Map<GeofenceResponse>(geofence))
            .Returns(new GeofenceResponse(id, "Zone", "Circle", "[]", 10.0, true, DateTime.UtcNow));

        var result = await _sut.UpdateAsync(id, request);

        geofence.Radius.Should().Be(10.0);
    }

    // ══════════════════════════════════════════════════════════════
    //  Error Isolation Tests
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_PropagatesException()
    {
        _repository.Setup(r => r.GetAllAsync())
            .ThrowsAsync(new Exception("DB connection lost"));

        var act = () => _sut.GetAllAsync();

        await act.Should().ThrowAsync<Exception>().WithMessage("DB connection lost");
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_PropagatesException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("DB connection lost"));

        var act = () => _sut.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<Exception>().WithMessage("DB connection lost");
    }

    [Fact]
    public async Task CreateAsync_AddThrows_PropagatesException()
    {
        var request = new CreateGeofenceRequest("Test", "Circle", "[]", 5.0);

        _repository.Setup(r => r.AddAsync(It.IsAny<Geofence>()))
            .ThrowsAsync(new Exception("Insert failed"));

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<Exception>().WithMessage("Insert failed");
        _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SaveThrows_PropagatesException()
    {
        var request = new CreateGeofenceRequest("Test", "Circle", "[]", 5.0);

        _repository.Setup(r => r.AddAsync(It.IsAny<Geofence>())).Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new Exception("Save failed"));

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<Exception>().WithMessage("Save failed");
    }

    [Fact]
    public async Task DeleteAsync_DeleteThrows_PropagatesException()
    {
        var id = Guid.NewGuid();
        var geofence = new Geofence { Id = id, Name = "ToDelete" };

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(geofence);
        _repository.Setup(r => r.DeleteAsync(geofence))
            .ThrowsAsync(new Exception("Delete failed"));

        var act = () => _sut.DeleteAsync(id);

        await act.Should().ThrowAsync<Exception>().WithMessage("Delete failed");
        _repository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_SaveThrows_PropagatesException()
    {
        var id = Guid.NewGuid();
        var geofence = new Geofence { Id = id, Name = "Zone", Type = "Circle", Active = true };
        var request = new UpdateGeofenceRequest("New", null, null, null, null);

        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(geofence);
        _repository.Setup(r => r.UpdateAsync(geofence)).Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new Exception("Save failed"));

        var act = () => _sut.UpdateAsync(id, request);

        await act.Should().ThrowAsync<Exception>().WithMessage("Save failed");
    }
}
