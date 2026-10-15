using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Mapping;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Xunit;

namespace Zorvian.Tests.Services;

/// <summary>
/// Regresión del bug latente: el perfil mapeaba <c>Quote.Status</c> desde el literal
/// <c>"draft"</c> (string → enum), lo que hacía que AutoMapper lanzara
/// AutoMapperMappingException al crear una cotización. Además no existía mapa para
/// UpdateQuoteRequest (el PUT fallaba) y los enums viajaban como enteros en el JSON,
/// mientras el frontend los consume como strings en minúsculas.
/// </summary>
public sealed class QuoteMappingTests
{
    private static IMapper CreateMapper()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);
        return cfg.CreateMapper();
    }

    [Fact]
    public void CreateQuoteRequest_MapsToQuote_WithoutThrowing_AndStatusPending()
    {
        var mapper = CreateMapper();
        var request = new CreateQuoteRequest(
            Guid.NewGuid(), null, null, 0m, null, Guid.NewGuid(), null, null,
            "proforma",
            new List<QuoteDetailItem>());

        var quote = mapper.Map<Quote>(request);

        Assert.Equal(QuoteStatus.Pending, quote.Status);
        Assert.Equal(Guid.Empty, quote.Id);
    }

    [Fact]
    public void UpdateQuoteRequest_MapsOntoExistingQuote_WithoutThrowing()
    {
        var mapper = CreateMapper();
        var quote = new Quote { Id = Guid.NewGuid(), Status = QuoteStatus.Accepted, QuoteType = QuoteType.Proforma };
        var clientId = Guid.NewGuid();
        var request = new UpdateQuoteRequest(
            clientId, null, null, 25m, "nota", Guid.NewGuid(), null,
            new List<QuoteDetailItem>());

        mapper.Map(request, quote);

        Assert.Equal(clientId, quote.ClientId);
        Assert.Equal(25m, quote.Discount);
        Assert.Equal("nota", quote.Notes);
        // No debe pisar estado ni tipo.
        Assert.Equal(QuoteStatus.Accepted, quote.Status);
        Assert.Equal(QuoteType.Proforma, quote.QuoteType);
    }

    [Fact]
    public void Quote_MapsToResponse_ExposingLowercaseStrings()
    {
        var mapper = CreateMapper();
        var quote = new Quote
        {
            Id = Guid.NewGuid(),
            QuoteNumber = "PRO-20261015-0001",
            Status = QuoteStatus.Converted,
            QuoteType = QuoteType.Proforma,
            Client = new Client { FirstName = "Ana", LastName = "Lopez" },
            Employee = new Employee { FirstName = "Luis", LastName = "Perez" },
        };

        var response = mapper.Map<QuoteResponse>(quote);

        Assert.Equal("converted", response.Status);
        Assert.Equal("proforma", response.QuoteType);
        Assert.Equal("Ana Lopez", response.ClientName);
        Assert.Equal("Luis Perez", response.EmployeeName);
    }

    [Theory]
    [InlineData(QuoteStatus.Pending, "pending")]
    [InlineData(QuoteStatus.Sent, "sent")]
    [InlineData(QuoteStatus.Accepted, "accepted")]
    [InlineData(QuoteStatus.Rejected, "rejected")]
    [InlineData(QuoteStatus.Expired, "expired")]
    [InlineData(QuoteStatus.Converted, "converted")]
    public void QuoteStatus_ToDbValue_MatchesApiVocabulary(QuoteStatus status, string expected) =>
        Assert.Equal(expected, status.ToDbValue());

    [Theory]
    [InlineData(null, QuoteType.Cotizacion)]
    [InlineData("", QuoteType.Cotizacion)]
    [InlineData("cotizacion", QuoteType.Cotizacion)]
    [InlineData("Proforma", QuoteType.Proforma)]
    [InlineData("  PROFORMA ", QuoteType.Proforma)]
    public void QuoteType_Parse_IsCaseInsensitiveWithDefault(string? input, QuoteType expected) =>
        Assert.Equal(expected, QuoteTypeExtensions.Parse(input));
}
