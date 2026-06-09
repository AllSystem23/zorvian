using System.Text.Json;
using AutoMapper;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class ProductService
{
    private readonly IProductRepository _repo;
    private readonly IInventoryMovementRepository _movementRepo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;
    private readonly ISyncService _sync;

    public ProductService(IProductRepository repo, IInventoryMovementRepository movementRepo, ITenantContext tenant, IMapper mapper, ISyncService sync)
    {
        _repo = repo;
        _movementRepo = movementRepo;
        _tenant = tenant;
        _mapper = mapper;
        _sync = sync;
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        var product = _mapper.Map<Product>(request);
        product.CompanyId = Guid.Parse(_tenant.TenantId);

        await _repo.AddAsync(product);

        if (request.Stock > 0)
        {
            var movement = _mapper.Map<InventoryMovement>(request);
            movement.ProductId = product.Id;
            movement.MovementType = "initial";
            movement.StockBefore = 0;
            movement.StockAfter = request.Stock;
            movement.Notes = "Initial stock entry";
            movement.CompanyId = product.CompanyId;
            movement.BranchId = product.BranchId;

            await _movementRepo.AddAsync(movement);
        }

        await _repo.SaveChangesAsync();

        await _sync.JournalAsync("Product", product.Id.ToString(), "created",
            JsonSerializer.Serialize(_mapper.Map<ProductResponse>(product)), _tenant.TenantId.ToString());

        return await GetByIdAsync(product.Id) ?? throw new InvalidOperationException("Failed to create product");
    }

    public async Task<ProductResponse?> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product is null) return null;

        _mapper.Map(request, product);
        await _repo.SaveChangesAsync();

        var response = _mapper.Map<ProductResponse>(product);
        await _sync.JournalAsync("Product", product.Id.ToString(), "updated",
            JsonSerializer.Serialize(response), _tenant.TenantId.ToString());

        return response;
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id)
    {
        var product = await _repo.GetByIdAsync(id);
        return product is null ? null : _mapper.Map<ProductResponse>(product);
    }

    public async Task<PagedResult<ProductListResponse>> GetFilteredAsync(ProductFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var items = await _repo.GetFilteredAsync(filter.Search, filter.CategoryId, filter.BrandId, filter.LowStock, filter.IsActive, Guid.Empty, page, pageSize);
        var total = await _repo.GetFilteredCountAsync(filter.Search, filter.CategoryId, filter.BrandId, filter.LowStock, filter.IsActive, Guid.Empty);

        return new PagedResult<ProductListResponse>(
            _mapper.Map<List<ProductListResponse>>(items),
            total, page, pageSize
        );
    }

    public async Task<List<ProductListResponse>> GetLowStockAsync()
    {
        var items = await _repo.GetLowStockAsync(Guid.Empty);
        return _mapper.Map<List<ProductListResponse>>(items);
    }

    public async Task<PagedResult<InventoryMovementResponse>> GetMovementsAsync(InventoryMovementFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var items = await _movementRepo.GetFilteredAsync(filter.ProductId, filter.MovementType, filter.FromDate, filter.ToDate, null, Guid.Empty, page, pageSize);
        var total = await _movementRepo.GetFilteredCountAsync(filter.ProductId, filter.MovementType, filter.FromDate, filter.ToDate, null, Guid.Empty);

        return new PagedResult<InventoryMovementResponse>(
            _mapper.Map<List<InventoryMovementResponse>>(items),
            total, page, pageSize
        );
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _repo.GetByIdAsync(id);
        if (product is null) return false;

        await _repo.DeleteAsync(product);
        await _repo.SaveChangesAsync();

        await _sync.JournalAsync("Product", id.ToString(), "deleted",
            null, _tenant.TenantId.ToString());

        return true;
    }
}
