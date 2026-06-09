using Microsoft.Extensions.Logging;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

/// <summary>
/// Service for managing inventory alerts (low stock, overstock, expiring)
/// </summary>
public interface IInventoryAlertService
{
    Task<List<InventoryAlert>> GetLowStockProductsAsync();
    Task<List<InventoryAlert>> GetExpiringProductsAsync(int daysAhead = 30);
    Task ProcessStockAlertsAsync();
}

public class InventoryAlert
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
    public string AlertType { get; set; } = string.Empty; // "low", "out", "overstock"
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Stock alert processing service (P3.6 - Stock mínimo/máximo)
/// </summary>
public class InventoryAlertService : IInventoryAlertService
{
    private readonly ILogger<InventoryAlertService> _logger;

    public InventoryAlertService(ILogger<InventoryAlertService> logger)
    {
        _logger = logger;
    }

    public async Task<List<InventoryAlert>> GetLowStockProductsAsync()
    {
        // In production: query products with current stock below MinStock
        // For now, return a sample query structure
        return await Task.FromResult(new List<InventoryAlert>
        {
            new InventoryAlert
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Producto ejemplo",
                ProductCode = "P001",
                CurrentStock = 5,
                MinStock = 10,
                MaxStock = 100,
                AlertType = "low"
            }
        });
    }

    public async Task<List<InventoryAlert>> GetExpiringProductsAsync(int daysAhead = 30)
    {
        return await Task.FromResult(new List<InventoryAlert>());
    }

    public async Task ProcessStockAlertsAsync()
    {
        try
        {
            var lowStock = await GetLowStockProductsAsync();
            if (lowStock.Any())
            {
                _logger.LogWarning("[STOCK-ALERT] {Count} products below minimum stock", lowStock.Count);
                // In production: send notifications via FCM/email
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STOCK-ALERT] Error processing stock alerts");
        }
    }
}

/// <summary>
/// Physical inventory count service (P3.5 - Inventario físico)
/// </summary>
public interface IPhysicalInventoryService
{
    Task<PhysicalCount> CreatePhysicalCountAsync(string name, string? notes = null);
    Task<PhysicalCountItem> AddCountItemAsync(Guid countId, Guid productId, int systemQty, int countedQty);
    Task<PhysicalCount> FinalizeCountAsync(Guid countId, Guid userId);
    Task<List<PhysicalCountAdjustment>> GetAdjustmentsAsync(Guid countId);
}

public class PhysicalCount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = "in_progress"; // in_progress, completed, cancelled
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public List<PhysicalCountItem> Items { get; set; } = new();
}

public class PhysicalCountItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CountId { get; set; }
    public Guid ProductId { get; set; }
    public int SystemQuantity { get; set; }
    public int CountedQuantity { get; set; }
    public int Variance { get; set; }
    public string? Notes { get; set; }
}

public class PhysicalCountAdjustment
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int SystemQty { get; set; }
    public int CountedQty { get; set; }
    public int Adjustment { get; set; }
    public string Type { get; set; } = string.Empty; // "increase" or "decrease"
}

public class PhysicalInventoryService : IPhysicalInventoryService
{
    private readonly ILogger<PhysicalInventoryService> _logger;

    public PhysicalInventoryService(ILogger<PhysicalInventoryService> logger)
    {
        _logger = logger;
    }

    public async Task<PhysicalCount> CreatePhysicalCountAsync(string name, string? notes = null)
    {
        var count = new PhysicalCount { Name = name, Notes = notes };
        _logger.LogInformation("[PHYSICAL-COUNT] Created physical count: {Name}", name);
        await Task.CompletedTask;
        return count;
    }

    public async Task<PhysicalCountItem> AddCountItemAsync(Guid countId, Guid productId, int systemQty, int countedQty)
    {
        var item = new PhysicalCountItem
        {
            CountId = countId,
            ProductId = productId,
            SystemQuantity = systemQty,
            CountedQuantity = countedQty,
            Variance = countedQty - systemQty
        };
        _logger.LogInformation("[PHYSICAL-COUNT] Added item: Product={ProductId} Variance={Variance}", productId, item.Variance);
        await Task.CompletedTask;
        return item;
    }

    public async Task<PhysicalCount> FinalizeCountAsync(Guid countId, Guid userId)
    {
        _logger.LogInformation("[PHYSICAL-COUNT] Finalized count {CountId} by user {UserId}", countId, userId);
        // In production: apply inventory movements for all variances
        await Task.CompletedTask;
        return new PhysicalCount { Id = countId, Status = "completed", CompletedAt = DateTime.UtcNow };
    }

    public async Task<List<PhysicalCountAdjustment>> GetAdjustmentsAsync(Guid countId)
    {
        return await Task.FromResult(new List<PhysicalCountAdjustment>());
    }
}
