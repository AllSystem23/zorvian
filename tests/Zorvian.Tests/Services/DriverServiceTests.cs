using AutoMapper;
using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class DriverServiceTests
{
    private readonly Mock<IDriverRepository> _repo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly DriverService _sut;
    private readonly Guid _companyId = Guid.NewGuid();

    public DriverServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _sut = new DriverService(_repo.Object, _tenant.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetAllAsync_WithValidTenant_ReturnsMappedDrivers()
    {
        var drivers = new List<Driver>
        {
            new() { Id = Guid.NewGuid(), FirstName = "Carlos", LastName = "López" },
        };

        _repo.Setup(r => r.GetAllAsync(_companyId)).ReturnsAsync(drivers);
        _mapper.Setup(m => m.Map<List<DriverResponse>>(drivers)).Returns(
            drivers.Select(d => new DriverResponse(
                d.Id, null, d.FirstName, d.LastName, $"{d.FirstName} {d.LastName}",
                "001-050590-0001X", DateOnly.FromDateTime(DateTime.Now.AddYears(-30)),
                "8888-1234", "carlos@test.com", null, Guid.NewGuid(), "Clase A",
                "LIC-001", DateOnly.FromDateTime(DateTime.Now.AddYears(-2)),
                DateOnly.FromDateTime(DateTime.Now.AddYears(3)), null,
                DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
                "Available", Guid.NewGuid(), "Sede Central", null, DateTime.UtcNow
            )).ToList());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].FirstName.Should().Be("Carlos");
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidTenant_ReturnsEmpty()
    {
        _tenant.Setup(t => t.TenantId).Returns("bad-guid");

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsDriver()
    {
        var id = Guid.NewGuid();
        var driver = new Driver { Id = id, FirstName = "Ana" };
        var response = new DriverResponse(
            id, null, "Ana", "Martínez", "Ana Martínez", "001-050590-0001X",
            DateOnly.FromDateTime(DateTime.Now.AddYears(-28)),
            "7777-5678", "ana@test.com", null, Guid.NewGuid(), "Clase A",
            "LIC-002", DateOnly.FromDateTime(DateTime.Now.AddYears(-1)),
            DateOnly.FromDateTime(DateTime.Now.AddYears(4)), null,
            DateOnly.FromDateTime(DateTime.Now.AddYears(-3)),
            "Available", Guid.NewGuid(), "Sede Norte", null, DateTime.UtcNow);

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(driver);
        _mapper.Setup(m => m.Map<DriverResponse>(driver)).Returns(response);

        var result = await _sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Ana");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Driver?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_SetsStatusToActive()
    {
        var request = new CreateDriverRequest(
            null, "Pedro", "García", "001-010185-0001X",
            DateOnly.FromDateTime(DateTime.Now.AddYears(-40)),
            "5555-4321", "pedro@test.com", null,
            Guid.NewGuid(), "LIC-003",
            DateOnly.FromDateTime(DateTime.Now.AddYears(-3)),
            DateOnly.FromDateTime(DateTime.Now.AddYears(2)),
            null, DateOnly.FromDateTime(DateTime.Now.AddYears(-10)),
            Guid.NewGuid(), null);

        var driver = new Driver();
        _mapper.Setup(m => m.Map<Driver>(request)).Returns(driver);
        _mapper.Setup(m => m.Map<DriverResponse>(It.IsAny<Driver>()))
            .Returns(new DriverResponse(
                Guid.NewGuid(), null, "Pedro", "García", "Pedro García", "001-010185-0001X",
                DateOnly.FromDateTime(DateTime.Now.AddYears(-40)),
                "5555-4321", "pedro@test.com", null, Guid.NewGuid(), "Clase A",
                "LIC-003", DateOnly.FromDateTime(DateTime.Now.AddYears(-3)),
                DateOnly.FromDateTime(DateTime.Now.AddYears(2)), null,
                DateOnly.FromDateTime(DateTime.Now.AddYears(-10)),
                "Active", Guid.NewGuid(), "Sede Central", null, DateTime.UtcNow));

        await _sut.CreateAsync(request);

        driver.Status.Should().Be("Active");
        _repo.Verify(r => r.AddAsync(driver), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Driver?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateDriverRequest(
            null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null));

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_MapsAndSaves()
    {
        var id = Guid.NewGuid();
        var driver = new Driver { Id = id };
        var response = new DriverResponse(
            id, null, "Nuevo Nombre", "García", "Nuevo Nombre García", "001-050590-0001X",
            DateOnly.FromDateTime(DateTime.Now.AddYears(-30)),
            "8888-1234", "test@test.com", null, Guid.NewGuid(), "Clase A",
            "LIC-001", DateOnly.FromDateTime(DateTime.Now.AddYears(-2)),
            DateOnly.FromDateTime(DateTime.Now.AddYears(3)), null,
            DateOnly.FromDateTime(DateTime.Now.AddYears(-5)),
            "Available", Guid.NewGuid(), "Sede Central", null, DateTime.UtcNow);
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(driver);
        _mapper.Setup(m => m.Map<DriverResponse>(driver)).Returns(response);

        var result = await _sut.UpdateAsync(id, new UpdateDriverRequest(
            null, "Nuevo Nombre", null, null, null, null, null, null, null, null, null, null,
            null, null, null, null));

        result.Should().NotBeNull();
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Driver?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_DeletesAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        var driver = new Driver { Id = id };
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(driver);

        var result = await _sut.DeleteAsync(id);

        result.Should().BeTrue();
        _repo.Verify(r => r.DeleteAsync(driver), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
