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

public sealed class WarrantyProviderServiceTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly WarrantyProviderRepository _repo;
    private readonly WarrantyProviderService _sut;
    private readonly string _tenantId;

    public WarrantyProviderServiceTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        var db = new ZorvianDbContext(options, _tenant.Object);
        _repo = new WarrantyProviderRepository(db);
        _sut = new WarrantyProviderService(_repo, _tenant.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var companyId = Guid.Parse(_tenantId);
        var provider = new WarrantyProvider
        {
            Id = Guid.NewGuid(),
            Code = "PV001",
            Name = "Test Provider",
            Type = "manufacturer",
            CompanyId = companyId,
            TenantId = _tenantId,
            CreatedBy = "test"
        };
        await _repo.AddAsync(provider);
        await _repo.SaveChangesAsync();

        var expected = new WarrantyProviderResponse(
            provider.Id, provider.Code, provider.Name,
            null, null, "manufacturer", null, null, null,
            null, null, null, null, 96, true, null, 0);
        _mapper.Setup(m => m.Map<List<WarrantyProviderResponse>>(It.IsAny<List<WarrantyProvider>>()))
            .Returns([expected]);

        var results = await _sut.GetAllAsync();

        Assert.Single(results);
    }

    [Fact]
    public async Task CreateAsync_MapsAndPersists()
    {
        var request = new CreateWarrantyProviderRequest(
            "PV002", "New Provider", null, null, "distributor",
            null, null, null, null, null, null, null);

        var provider = new WarrantyProvider { Name = "New Provider" };
        _mapper.Setup(m => m.Map<WarrantyProvider>(It.IsAny<CreateWarrantyProviderRequest>()))
            .Returns(provider);
        _mapper.Setup(m => m.Map<WarrantyProviderResponse>(It.IsAny<WarrantyProvider>()))
            .Returns(new WarrantyProviderResponse(
                Guid.Empty, "", "New Provider",
                null, null, "distributor", null, null, null,
                null, null, null, null, 96, true, null, 0));

        var result = await _sut.CreateAsync(request);

        Assert.Equal("New Provider", result.Name);
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
