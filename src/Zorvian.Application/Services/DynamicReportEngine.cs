using System.Text.Json;
using Zorvian.Application.DTOs.Report;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public sealed class DynamicReportEngine
{
    private readonly ISaleRepository _saleRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IProductRepository _productRepo;
    private readonly IAccountingEntryRepository _entryRepo;
    private readonly IClientRepository _clientRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IEmployeeRepository _employeeRepo;

    public DynamicReportEngine(
        ISaleRepository saleRepo,
        IPurchaseRepository purchaseRepo,
        IProductRepository productRepo,
        IAccountingEntryRepository entryRepo,
        IClientRepository clientRepo,
        ISupplierRepository supplierRepo,
        IEmployeeRepository employeeRepo)
    {
        _saleRepo = saleRepo;
        _purchaseRepo = purchaseRepo;
        _productRepo = productRepo;
        _entryRepo = entryRepo;
        _clientRepo = clientRepo;
        _supplierRepo = supplierRepo;
        _employeeRepo = employeeRepo;
    }

    public async Task<ReportResult> ExecuteAsync(
        string module,
        List<ReportField> fields,
        List<ReportFilter> filters,
        string? groupByField,
        string? sortByField,
        string sortOrder,
        Guid companyId)
    {
        var rows = module.ToLowerInvariant() switch
        {
            "sales" => await LoadSalesAsync(companyId),
            "purchases" => await LoadPurchasesAsync(companyId),
            "products" => await LoadProductsAsync(companyId),
            "clients" => await LoadClientsAsync(companyId),
            "suppliers" => await LoadSuppliersAsync(companyId),
            "employees" => await LoadEmployeesAsync(companyId),
            _ => throw new ArgumentException($"Unknown module: {module}"),
        };

        rows = ApplyFilters(rows, filters);

        if (!string.IsNullOrEmpty(groupByField))
            rows = ApplyGrouping(rows, groupByField, fields);

        rows = ApplySorting(rows, sortByField, sortOrder);

        var selected = ProjectFields(rows, fields);

        var columns = fields.Where(f => f.Visible).OrderBy(f => f.Order).Select(f => f.Name).ToList();
        return new ReportResult(columns, selected, selected.Count);
    }

    // ── Module data loaders ──

    private async Task<List<Dictionary<string, object?>>> LoadSalesAsync(Guid companyId)
    {
        var branchId = Guid.Empty;
        var sales = await _saleRepo.GetFilteredAsync(null, null, null, null, null, null, branchId, 1, int.MaxValue);
        return sales.Select(s => new Dictionary<string, object?>
        {
            ["id"] = s.Id,
            ["invoiceNumber"] = s.InvoiceNumber,
            ["saleDate"] = s.SaleDate,
            ["saleType"] = s.SaleType,
            ["subtotal"] = s.Subtotal,
            ["tax"] = s.Tax,
            ["discount"] = s.Discount,
            ["total"] = s.Total,
            ["paidAmount"] = s.PaidAmount,
            ["balance"] = s.Balance,
            ["status"] = s.Status,
            ["currencyCode"] = s.CurrencyCode,
            ["clientName"] = s.Client != null ? $"{s.Client.FirstName} {s.Client.LastName}" : null,
            ["clientIdentification"] = s.Client?.IdentificationNumber,
            ["clientPhone"] = s.Client?.Phone,
            ["clientCity"] = s.Client?.City,
            ["employeeName"] = s.Employee != null ? $"{s.Employee.FirstName} {s.Employee.LastName}" : null,
            ["createdAt"] = s.CreatedAt,
            ["branchId"] = s.BranchId,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> LoadPurchasesAsync(Guid companyId)
    {
        var branchId = Guid.Empty;
        var purchases = await _purchaseRepo.GetFilteredAsync(null, null, null, null, null, branchId, 1, int.MaxValue);
        return purchases.Select(p => new Dictionary<string, object?>
        {
            ["id"] = p.Id,
            ["purchaseNumber"] = p.PurchaseNumber,
            ["purchaseDate"] = p.PurchaseDate,
            ["dueDate"] = p.DueDate,
            ["status"] = p.Status,
            ["subtotal"] = p.Subtotal,
            ["tax"] = p.Tax,
            ["discount"] = p.Discount,
            ["total"] = p.Total,
            ["paidAmount"] = p.PaidAmount,
            ["balance"] = p.Balance,
            ["currencyCode"] = p.CurrencyCode,
            ["supplierName"] = p.Supplier?.Name,
            ["supplierContact"] = p.Supplier?.ContactName,
            ["supplierTaxId"] = p.Supplier?.TaxId,
            ["createdAt"] = p.CreatedAt,
            ["branchId"] = p.BranchId,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> LoadProductsAsync(Guid companyId)
    {
        var products = await _productRepo.GetFilteredAsync(null, null, null, null, null, Guid.Empty, 1, int.MaxValue);
        return products.Select(p => new Dictionary<string, object?>
        {
            ["id"] = p.Id,
            ["code"] = p.Code,
            ["name"] = p.Name,
            ["category"] = p.Category?.Name,
            ["brand"] = p.Brand?.Name,
            ["supplierName"] = p.Supplier?.Name,
            ["costPrice"] = p.CostPrice,
            ["sellingPrice"] = p.SellingPrice,
            ["stock"] = p.Stock,
            ["minStock"] = p.MinStock,
            ["maxStock"] = p.MaxStock,
            ["unitOfMeasure"] = p.UnitOfMeasure,
            ["location"] = p.Location,
            ["isActive"] = p.IsActive,
            ["barcode"] = p.Barcode,
            ["createdAt"] = p.CreatedAt,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> LoadClientsAsync(Guid companyId)
    {
        var branchId = Guid.Empty;
        var clients = await _clientRepo.GetFilteredAsync(null, null, branchId, 1, int.MaxValue);
        return clients.Select(c => new Dictionary<string, object?>
        {
            ["id"] = c.Id,
            ["code"] = c.Code,
            ["firstName"] = c.FirstName,
            ["lastName"] = c.LastName,
            ["fullName"] = $"{c.FirstName} {c.LastName}",
            ["identificationNumber"] = c.IdentificationNumber,
            ["phone"] = c.Phone,
            ["city"] = c.City,
            ["state"] = c.State,
            ["status"] = c.Status,
            ["creditLimit"] = c.CreditLimit,
            ["createdAt"] = c.CreatedAt,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> LoadSuppliersAsync(Guid companyId)
    {
        var suppliers = await _supplierRepo.GetFilteredAsync(null, companyId, 1, int.MaxValue);
        return suppliers.Select(s => new Dictionary<string, object?>
        {
            ["id"] = s.Id,
            ["code"] = s.Code,
            ["name"] = s.Name,
            ["contactName"] = s.ContactName,
            ["phone"] = s.Phone,
            ["email"] = s.Email,
            ["taxId"] = s.TaxId,
            ["isActive"] = s.IsActive,
            ["createdAt"] = s.CreatedAt,
        }).ToList();
    }

    private async Task<List<Dictionary<string, object?>>> LoadEmployeesAsync(Guid companyId)
    {
        var employees = await _employeeRepo.GetFilteredAsync(null, null, null, 1, int.MaxValue);
        return employees.Select(e => new Dictionary<string, object?>
        {
            ["id"] = e.Id,
            ["employeeCode"] = e.EmployeeCode,
            ["firstName"] = e.FirstName,
            ["lastName"] = e.LastName,
            ["fullName"] = $"{e.FirstName} {e.LastName}",
            ["email"] = e.Email,
            ["phone"] = e.Phone,
            ["department"] = e.Department?.Name,
            ["position"] = e.Position,
            ["hireDate"] = e.HireDate,
            ["salary"] = e.Salary,
            ["salaryType"] = e.SalaryType,
            ["status"] = e.Status,
            ["createdAt"] = e.CreatedAt,
        }).ToList();
    }

    // ── Filter engine ──

    private static List<Dictionary<string, object?>> ApplyFilters(
        List<Dictionary<string, object?>> rows, List<ReportFilter> filters)
    {
        if (filters.Count == 0) return rows;

        return rows.Where(row => filters.All(f => EvaluateFilter(row, f))).ToList();
    }

    private static bool EvaluateFilter(Dictionary<string, object?> row, ReportFilter filter)
    {
        if (!row.TryGetValue(filter.Field, out var value)) return true;
        var strVal = value?.ToString() ?? "";

        return filter.Operator.ToLowerInvariant() switch
        {
            "eq" => string.Equals(strVal, filter.Value, StringComparison.OrdinalIgnoreCase),
            "neq" => !string.Equals(strVal, filter.Value, StringComparison.OrdinalIgnoreCase),
            "contains" => strVal.Contains(filter.Value, StringComparison.OrdinalIgnoreCase),
            "gt" => CompareNumeric(value, filter.Value) > 0,
            "gte" => CompareNumeric(value, filter.Value) >= 0,
            "lt" => CompareNumeric(value, filter.Value) < 0,
            "lte" => CompareNumeric(value, filter.Value) <= 0,
            "isnull" => value is null,
            "isnotnull" => value is not null,
            _ => true,
        };
    }

    private static int CompareNumeric(object? value, string compareTo)
    {
        if (value is null) return -1;
        if (!decimal.TryParse(compareTo, out var target)) return 0;
        return (value switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            double db => (decimal)db,
            _ => 0m,
        }).CompareTo(target);
    }

    // ── Grouping ──

    private static List<Dictionary<string, object?>> ApplyGrouping(
        List<Dictionary<string, object?>> rows, string groupByField, List<ReportField> fields)
    {
        var groups = rows.GroupBy(r => r.GetValueOrDefault(groupByField)?.ToString() ?? "(null)");
        var aggregates = fields.Where(f => !string.IsNullOrEmpty(f.Aggregate)).ToList();

        return groups.Select(g =>
        {
            var row = new Dictionary<string, object?> { [groupByField] = g.Key };
            foreach (var field in aggregates)
            {
                var values = g.Select(r => r.GetValueOrDefault(field.Source)).Where(v => v is not null).ToList();
                if (values.Count == 0) continue;

                row[field.Name] = field.Aggregate!.ToLowerInvariant() switch
                {
                    "sum" => values.Sum(v => Convert.ToDecimal(v)),
                    "avg" => values.Average(v => Convert.ToDecimal(v)),
                    "count" => values.Count,
                    "min" => values.Min(),
                    "max" => values.Max(),
                    _ => values.Count,
                };
            }
            return row;
        }).ToList();
    }

    // ── Sorting ──

    private static List<Dictionary<string, object?>> ApplySorting(
        List<Dictionary<string, object?>> rows, string? sortBy, string sortOrder)
    {
        if (string.IsNullOrEmpty(sortBy)) return rows;

        var descending = sortOrder.ToLowerInvariant() == "desc";
        return descending
            ? rows.OrderByDescending(r => r.GetValueOrDefault(sortBy)?.ToString() ?? "").ToList()
            : rows.OrderBy(r => r.GetValueOrDefault(sortBy)?.ToString() ?? "").ToList();
    }

    // ── Field projection ──

    private static List<Dictionary<string, object?>> ProjectFields(
        List<Dictionary<string, object?>> rows, List<ReportField> fields)
    {
        var visible = fields.Where(f => f.Visible).OrderBy(f => f.Order).ToList();
        if (visible.Count == 0) return rows;

        return rows.Select(row =>
        {
            var projected = new Dictionary<string, object?>();
            foreach (var field in visible)
            {
                projected[field.Name] = row.GetValueOrDefault(field.Source);
            }
            return projected;
        }).ToList();
    }
}
