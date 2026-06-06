using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;
using Zorvian.Application.Services;
using Zorvian.Application.DTOs.Warranty;
using AutoMapper;

namespace Zorvian.Tests.Services;

public sealed class WorkshopServiceTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly ServiceWorkshopRepository _repo;
    private readonly WorkshopService _sut;
    private readonly string _tenantId;

    public WorkshopServiceTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        var db = new ZorvianDbContext(options, _tenant.Object);
        _repo = new ServiceWorkshopRepository(db);
        _sut = new WorkshopService(_repo, _tenant.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var companyId = Guid.Parse(_tenantId);
        var workshop = new ServiceWorkshop
        {
            Id = Guid.NewGuid(),
            Code = "WS001",
            Name = "Test Workshop",
            CompanyId = companyId,
            TenantId = _tenantId,
            BranchId = Guid.NewGuid(),
            CreatedBy = "test"
        };
        await _repo.AddAsync(workshop);
        await _repo.SaveChangesAsync();

        var expected = new ServiceWorkshopResponse(
            workshop.Id, workshop.BranchId, workshop.Code, workshop.Name,
            null, null, null, null, null, null, null, null,
            48, 72, 0m, true, null, 0);
        _mapper.Setup(m => m.Map<List<ServiceWorkshopResponse>>(It.IsAny<List<ServiceWorkshop>>()))
            .Returns([expected]);

        var results = await _sut.GetAllAsync();

        Assert.Single(results);
    }

    [Fact]
    public async Task CreateAsync_MapsAndPersists()
    {
        var request = new CreateServiceWorkshopRequest(
            Guid.NewGuid(), "WS002", "New Workshop",
            null, null, null, null, null, null, null, null);

        var workshop = new ServiceWorkshop { Name = "New Workshop" };
        _mapper.Setup(m => m.Map<ServiceWorkshop>(It.IsAny<CreateServiceWorkshopRequest>()))
            .Returns(workshop);
        _mapper.Setup(m => m.Map<ServiceWorkshopResponse>(It.IsAny<ServiceWorkshop>()))
            .Returns(new ServiceWorkshopResponse(
                Guid.Empty, Guid.Empty, "", "New Workshop",
                null, null, null, null, null, null, null, null,
                48, 72, 0m, true, null, 0));

        var result = await _sut.CreateAsync(request);

        Assert.Equal("New Workshop", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        var deleted = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }
}
