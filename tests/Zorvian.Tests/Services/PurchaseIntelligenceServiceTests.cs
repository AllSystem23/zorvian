using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

public sealed class PurchaseIntelligenceServiceTests
{
    private readonly Mock<IAiDocumentService> _aiService = new();
    private readonly PurchaseIntelligenceService _sut;

    public PurchaseIntelligenceServiceTests()
    {
        _sut = new PurchaseIntelligenceService(_aiService.Object);
    }

    [Fact]
    public async Task AnalyzeInvoiceAsync_Should_Return_Mapped_PurchaseRequest_When_AiReturnsValidJson()
    {
        var jsonResponse = @"
        {
          ""SupplierName"": ""Proveedor Demo"",
          ""PurchaseDate"": ""2026-06-12"",
          ""DueDate"": ""2026-07-12"",
          ""InvoiceReference"": ""INV-001"",
          ""Subtotal"": 100.0,
          ""Tax"": 15.0,
          ""Total"": 115.0,
          ""CurrencyCode"": ""USD"",
          ""Details"": [
            {
              ""ProductName"": ""Producto A"",
              ""Quantity"": 2,
              ""UnitCost"": 50.0
            }
          ]
        }";

        _aiService.Setup(a => a.AnalyzeFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(jsonResponse);

        using var stream = new MemoryStream();
        var result = await _sut.AnalyzeInvoiceAsync(stream);

        Assert.Equal("INV-001", result.InvoiceReference);
        Assert.Equal("USD", result.CurrencyCode);
        Assert.Single(result.Details);
        Assert.Equal("Producto A", result.Details[0].ProductName);
        Assert.Equal(2, result.Details[0].Quantity);
        Assert.Equal(50.0m, result.Details[0].UnitCost);
    }
}
