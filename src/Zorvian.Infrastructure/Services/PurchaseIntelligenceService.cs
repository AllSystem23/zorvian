using System.Text.Json;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Interfaces;

namespace Zorvian.Infrastructure.Services;

public sealed class PurchaseIntelligenceService : IPurchaseIntelligenceService
{
    private readonly IAiDocumentService _aiService;

    public PurchaseIntelligenceService(IAiDocumentService aiService)
    {
        _aiService = aiService;
    }

    public async Task<CreatePurchaseRequest> AnalyzeInvoiceAsync(Stream fileStream)
    {
        var prompt = @"Analiza esta factura. Extrae: Proveedor (Nombre), Fecha, Fecha Vencimiento, Referencia de Factura, Subtotal, Impuestos, Total, Moneda (Código ISO: USD, NIO, etc.) y los Ítems (Producto, Cantidad, Precio Unitario).
        
Devuelve SOLO un JSON estrictamente con esta estructura:
{
  ""SupplierName"": ""string"",
  ""PurchaseDate"": ""yyyy-MM-dd"",
  ""DueDate"": ""yyyy-MM-dd"",
  ""InvoiceReference"": ""string"",
  ""Subtotal"": decimal,
  ""Tax"": decimal,
  ""Total"": decimal,
  ""CurrencyCode"": ""string"",
  ""Details"": [
    {
      ""ProductName"": ""string"",
      ""Quantity"": int,
      ""UnitCost"": decimal
    }
  ]
}";

        var jsonResponse = await _aiService.AnalyzeFileAsync(fileStream, prompt, "image/jpeg");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rawData = JsonSerializer.Deserialize<InvoiceAnalysisDto>(jsonResponse, options) 
                      ?? throw new InvalidOperationException("Failed to analyze invoice");

        // Map DTO to CreatePurchaseRequest
        return new CreatePurchaseRequest(
            SupplierId: Guid.Empty,
            PurchaseDate: DateTime.TryParse(rawData.PurchaseDate, out var pd) ? pd : DateTime.UtcNow,
            DueDate: DateOnly.TryParse(rawData.DueDate, out var dd) ? dd : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            InvoiceReference: rawData.InvoiceReference,
            WithholdingType: null,
            WithholdingRate: null,
            Discount: 0,
            Notes: $"Analizado por IA. Proveedor original: {rawData.SupplierName}",
            BranchId: Guid.Empty,
            CurrencyCode: rawData.CurrencyCode ?? "NIO", // Fallback a NIO si la IA no detecta moneda
            ExchangeRateToReporting: 1.0m,
            CountryCode: null,
            Details: rawData.Details.Select(d => new PurchaseDetailItem(
                Guid.Empty, d.ProductName, d.Quantity, d.UnitCost, 0, d.Quantity * d.UnitCost
            )).ToList()
        );
    }
}

internal record InvoiceAnalysisDto(
    string SupplierName,
    string PurchaseDate,
    string DueDate,
    string InvoiceReference,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    string CurrencyCode,
    List<InvoiceDetailDto> Details
);

internal record InvoiceDetailDto(
    string ProductName,
    int Quantity,
    decimal UnitCost
);
