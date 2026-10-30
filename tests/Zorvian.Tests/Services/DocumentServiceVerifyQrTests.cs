using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.Documentary;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// El QR del bloque de auditoría debe (1) codificar la URL de verificación del
/// MISMO documento que se está generando y (2) generarse LOCALMENTE: antes se
/// pedía a api.qrserver.com y el PDF dependía de un servicio de terceros.
/// </summary>
public sealed class DocumentServiceVerifyQrTests
{
    private readonly Mock<IDocumentTemplateRepository> _templateRepo = new();
    private readonly Mock<IGeneratedDocumentRepository> _docRepo = new();
    private readonly Mock<IAiDocumentService> _aiService = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly Mock<IAppLinks> _appLinks = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IDocumentStorageService> _storage = new();
    private readonly Mock<IDocumentVersionContentService> _versionContent = new();
    private readonly Mock<ILogger<DocumentService>> _logger = new();

    private const string ApiBase = "https://api.example.com/zorvian/v1";

    private DocumentVersion? _addedVersion;
    private DocumentService _sut = null!;

    private static readonly Regex DataUriPattern =
        new("data:image/png;base64,([A-Za-z0-9+/=]+)", RegexOptions.Compiled);

    private DocumentService CreateSut(string publicApiBaseUrl)
    {
        _tenant.Setup(t => t.TenantId).Returns("tenant-1");
        _companyRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<string>())).ReturnsAsync(new Company
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-1",
            Name = "Comercial Zorvian",
            TaxId = "J0310000000000",
            Country = "Nicaragua",
            Address = "Central, Managua",
        });
        _appLinks.Setup(a => a.BuildDocumentVerifyUrl(It.IsAny<Guid>()))
            .Returns((Guid id) => publicApiBaseUrl.Length == 0
                ? string.Empty
                : $"{publicApiBaseUrl}/documents/verify/{id}");

        _templateRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Liquidación Final",
            Category = "HR",
            Module = "Employee",
            CountryCode = "ALL",
            Content = "<html><body><p>Documento</p></body></html>",
        });

        _addedVersion = null;
        _docRepo.Setup(r => r.AddVersionAsync(It.IsAny<DocumentVersion>()))
            .Callback<DocumentVersion>(v => _addedVersion = v)
            .Returns(Task.CompletedTask);

        _sut = new DocumentService(
            _templateRepo.Object,
            _docRepo.Object,
            _aiService.Object,
            _companyRepo.Object,
            _appLinks.Object,
            _tenant.Object,
            _storage.Object,
            _versionContent.Object,
            _logger.Object);
        return _sut;
    }

    private static string ExtractDataUri(string html)
    {
        var match = DataUriPattern.Match(html);
        return match.Success ? match.Value : string.Empty;
    }

    [Fact]
    public async Task Qr_IsGeneratedLocally_AndEncodesTheVerifyUrlOfThisDocument()
    {
        CreateSut(ApiBase);

        var doc = await _sut.GenerateProfessionalDocumentAsync(
            Guid.NewGuid(), Guid.NewGuid(), new Dictionary<string, string>());

        var rendered = _addedVersion!.Content;
        var expectedUrl = $"{ApiBase}/documents/verify/{doc.Id}";

        // (1) Ya no se le la imagen a un tercero.
        rendered.Should().NotContain("api.qrserver.com");
        // (2) El QR va incrustado como data URI...
        var dataUri = ExtractDataUri(rendered);
        dataUri.Should().NotBeEmpty();
        // ...y codifica EXACTAMENTE la URL de verificación de este documento.
        dataUri.Should().Be(DocumentQrImage.TryGetDataUri(expectedUrl));
        // (3) Enlace legible para quien reciba el documento en digital.
        rendered.Should().Contain($"href='{expectedUrl}'");
    }

    [Fact]
    public async Task Qr_WithoutPublicApiBaseUrl_OmitsQrInsteadOfPrintingAFakeOne()
    {
        CreateSut(string.Empty);

        await _sut.GenerateProfessionalDocumentAsync(
            Guid.NewGuid(), Guid.NewGuid(), new Dictionary<string, string>());

        var rendered = _addedVersion!.Content;

        rendered.Should().NotContain("api.qrserver.com");
        rendered.Should().NotContain("data:image/png;base64,");
        rendered.Should().NotContain("documents/verify/");
        // El texto del bloque de auditoría sigue intacto.
        rendered.Should().Contain("Bloque de auditoría");
    }

    [Fact]
    public async Task Preview_HasNoQr_BecauseThereIsNoDocumentToVerifyYet()
    {
        CreateSut(ApiBase);

        var html = await _sut.RenderLiquidPreviewAsync(
            "<html><body><p>Vista previa</p></body></html>",
            new Dictionary<string, string>());

        html.Should().NotContain("api.qrserver.com");
        html.Should().NotContain("data:image/png;base64,");
        html.Should().NotContain("documents/verify/");
        html.Should().Contain("Bloque de auditoría");
    }
}
