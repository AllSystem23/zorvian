using AutoMapper;
using Moq;
using Xunit;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Tests.Services;

public sealed class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ISyncService> _sync = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        _sut = new ProductService(_productRepo.Object, _movementRepo.Object, _tenant.Object, _mapper.Object, _sync.Object);
    }

    [Fact]
    public async Task GetById_Found_ReturnsMappedResponse()
    {
        var id = Guid.NewGuid();
        var product = new Product { Id = id, Code = "PROD-001", Name = "Test Product" };
        _productRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(product);
        var expected = new ProductResponse(id, "PROD-001", "Test Product", null, null, null, null, null,
            null, null, null, null, 0, 0, "unit", 0, 0, 0, null, null, true, Guid.Empty);
        _mapper.Setup(m => m.Map<ProductResponse>(product)).Returns(expected);

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("PROD-001", result!.Code);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
