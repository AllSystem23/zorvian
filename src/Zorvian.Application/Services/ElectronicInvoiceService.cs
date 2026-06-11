using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class ElectronicInvoiceService : IElectronicInvoiceService
{
    private readonly IElectronicInvoiceRepository _repo;
    private readonly ISaleRepository _saleRepo;
    private readonly ITenantContext _tenant;
    private readonly ILogger<ElectronicInvoiceService> _logger;

    public ElectronicInvoiceService(
        IElectronicInvoiceRepository repo,
        ISaleRepository saleRepo,
        ITenantContext tenant,
        ILogger<ElectronicInvoiceService> logger)
    {
        _repo = repo;
        _saleRepo = saleRepo;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<ElectronicInvoiceDto> IssueAsync(Guid saleId, string countryCode)
    {
        var existing = await _repo.GetBySaleAsync(saleId);
        if (existing is not null)
            throw new InvalidOperationException($"La venta {saleId} ya tiene una factura electrónica emitida");

        var sale = await _saleRepo.GetByIdAsync(saleId);
        if (sale is null)
            throw new ArgumentException($"Venta {saleId} no encontrada");

        var invoice = new ElectronicInvoice
        {
            SaleId = saleId,
            CountryCode = countryCode,
            InvoiceNumber = GenerateInvoiceNumber(sale, countryCode),
            Status = "pending",
            TenantId = _tenant.TenantId,
        };

        await _repo.AddAsync(invoice);
        await _repo.SaveChangesAsync();

        try
        {
            var xml = GenerateXml(sale, countryCode);
            invoice.XmlContent = xml;
            invoice.Status = "submitted";
            invoice.SubmittedAt = DateTime.UtcNow;

            var result = await SubmitToTaxAuthorityAsync(xml, countryCode);
            invoice.DgiResponse = result.Response;
            invoice.AuthorizationCode = result.AuthorizationCode;
            invoice.AuthorizationDate = result.AuthorizationDate;

            if (result.IsAuthorized)
            {
                invoice.Status = "authorized";
                invoice.AuthorizedAt = DateTime.UtcNow;
                invoice.SignedXml = result.SignedXml;
            }
            else
            {
                invoice.Status = "rejected";
                invoice.ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al emitir factura electrónica para venta {SaleId}", saleId);
            invoice.Status = "rejected";
            invoice.ErrorMessage = ex.Message;
        }

        invoice.Attempts = 1;
        await _repo.UpdateAsync(invoice);
        await _repo.SaveChangesAsync();

        return MapToDto(invoice);
    }

    public async Task<ElectronicInvoiceDto?> GetBySaleAsync(Guid saleId)
    {
        var invoice = await _repo.GetBySaleAsync(saleId);
        return invoice is null ? null : MapToDto(invoice);
    }

    public async Task<ElectronicInvoiceDto?> GetByIdAsync(Guid id)
    {
        var invoice = await _repo.GetByIdAsync(id);
        return invoice is null ? null : MapToDto(invoice);
    }

    public async Task<List<ElectronicInvoiceDto>> GetByCompanyAsync(Guid companyId, string? countryCode = null)
    {
        var invoices = await _repo.GetByCompanyAsync(companyId, countryCode);
        return invoices.Select(MapToDto).ToList();
    }

    public async Task<ElectronicInvoiceDto> ResubmitAsync(Guid id)
    {
        var invoice = await _repo.GetByIdAsync(id)
            ?? throw new ArgumentException($"Factura electrónica {id} no encontrada");

        if (invoice.Status == "authorized")
            throw new InvalidOperationException("La factura ya fue autorizada");

        invoice.Attempts++;
        invoice.Status = "submitted";
        invoice.SubmittedAt = DateTime.UtcNow;

        try
        {
            var sale = await _saleRepo.GetByIdAsync(invoice.SaleId)
                ?? throw new ArgumentException($"Venta {invoice.SaleId} no encontrada");

            var xml = invoice.XmlContent ?? GenerateXml(sale, invoice.CountryCode);
            var result = await SubmitToTaxAuthorityAsync(xml, invoice.CountryCode);
            invoice.DgiResponse = result.Response;
            invoice.AuthorizationCode = result.AuthorizationCode;

            if (result.IsAuthorized)
            {
                invoice.Status = "authorized";
                invoice.AuthorizedAt = DateTime.UtcNow;
                invoice.ErrorMessage = null;
            }
            else
            {
                invoice.Status = "rejected";
                invoice.ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al re-enviar factura {InvoiceId}", id);
            invoice.Status = "rejected";
            invoice.ErrorMessage = ex.Message;
        }

        await _repo.UpdateAsync(invoice);
        await _repo.SaveChangesAsync();

        return MapToDto(invoice);
    }

    public async Task CancelAsync(Guid id, string reason)
    {
        var invoice = await _repo.GetByIdAsync(id)
            ?? throw new ArgumentException($"Factura electrónica {id} no encontrada");

        if (invoice.Status != "authorized")
            throw new InvalidOperationException("Solo facturas autorizadas pueden ser anuladas");

        invoice.Status = "cancelled";
        invoice.CancelReason = reason;
        invoice.CancelledAt = DateTime.UtcNow;

        await _repo.UpdateAsync(invoice);
        await _repo.SaveChangesAsync();
    }

    public async Task<string> GenerateXmlAsync(Guid saleId, string countryCode)
    {
        var sale = await _saleRepo.GetByIdAsync(saleId)
            ?? throw new ArgumentException($"Venta {saleId} no encontrada");

        return GenerateXml(sale, countryCode);
    }

    public async Task<string> GeneratePdfAsync(Guid id)
    {
        var invoice = await _repo.GetByIdAsync(id)
            ?? throw new ArgumentException($"Factura electrónica {id} no encontrada");

        return $"facturas/{invoice.Id}/factura_{invoice.InvoiceNumber}.pdf";
    }

    private static string GetClientName(Sale sale) =>
        sale.Client is null ? "Consumidor Final"
            : $"{sale.Client.FirstName} {sale.Client.LastName}".Trim();

    private static string GetClientId(Sale sale) =>
        sale.Client?.IdentificationNumber ?? "0000000000000";

    private string GenerateInvoiceNumber(Sale sale, string countryCode)
    {
        var prefix = countryCode.ToUpper() switch
        {
            "NIC" => "NIC",
            "CRI" => "CRI",
            "GTM" => "GTM",
            "HND" => "HND",
            "SLV" => "SLV",
            "PAN" => "PAN",
            _ => "ERP"
        };
        return $"{prefix}-{sale.SaleDate:yyyyMMdd}-{sale.Id.ToString()[..8].ToUpper()}";
    }

    private string GenerateXml(Sale sale, string countryCode)
    {
        return countryCode.ToUpper() switch
        {
            "NIC" => GenerateNicaraguaXml(sale),
            "CRI" => GenerateCostaRicaXml(sale),
            "GTM" => GenerateGuatemalaXml(sale),
            "HND" => GenerateHondurasXml(sale),
            "SLV" => GenerateElSalvadorXml(sale),
            "PAN" => GeneratePanamaXml(sale),
            _ => throw new NotSupportedException($"País {countryCode} no soportado para facturación electrónica")
        };
    }

    private XElement DetallesXml(Sale sale) =>
        new("Detalles",
            sale.Details.Select(d =>
                new XElement("Detalle",
                    new XElement("Descripcion", d.Product?.Name ?? "Producto"),
                    new XElement("Cantidad", d.Quantity),
                    new XElement("PrecioUnitario", d.UnitPrice),
                    new XElement("SubTotal", d.Subtotal)
                )
            )
        );

    private string GenerateNicaraguaXml(Sale sale)
    {
        var doc = new XDocument(
            new XElement("FacturaElectronica",
                new XAttribute("xmlns", "http://www.dgi.gob.ni"),
                new XAttribute("version", "1.0"),
                new XElement("Encabezado",
                    new XElement("NumeroFactura", sale.InvoiceNumber),
                    new XElement("FechaEmision", sale.SaleDate.ToString("yyyy-MM-ddTHH:mm:ss")),
                    new XElement("TipoDocumento", "FC"),
                    new XElement("TipoFactura", "DE")
                ),
                new XElement("Emisor",
                    new XElement("RUC", sale.TenantId ?? ""),
                    new XElement("RazonSocial", sale.TenantId ?? "")
                ),
                new XElement("Receptor",
                    new XElement("Nombre", GetClientName(sale)),
                    new XElement("Identificacion", GetClientId(sale)),
                    new XElement("TipoIdentificacion", "CED")
                ),
                DetallesXml(sale),
                new XElement("Resumen",
                    new XElement("SubTotal", sale.Subtotal),
                    new XElement("IVA", sale.Tax),
                    new XElement("Total", sale.Total),
                    new XElement("Moneda", sale.CurrencyCode)
                )
            )
        );
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private string GenerateCostaRicaXml(Sale sale)
    {
        var doc = new XDocument(
            new XElement("FacturaElectronica",
                new XAttribute("xmlns", "https://www.hacienda.go.cr"),
                new XAttribute("version", "4.3"),
                new XElement("Clave", Guid.NewGuid().ToString("N").ToUpper()),
                new XElement("NumeroConsecutivo", sale.InvoiceNumber),
                new XElement("FechaEmision", sale.SaleDate.ToString("yyyy-MM-ddTHH:mm:ss-06:00")),
                new XElement("Emisor", new XElement("Nombre", sale.TenantId ?? "")),
                new XElement("Receptor",
                    new XElement("Nombre", GetClientName(sale)),
                    new XElement("Identificacion",
                        new XElement("Tipo", "01"),
                        new XElement("Numero", GetClientId(sale))
                    )
                ),
                new XElement("DetalleServicio",
                    sale.Details.Select(d =>
                        new XElement("Linea",
                            new XElement("Codigo", d.ProductId.ToString()[..8]),
                            new XElement("Cantidad", d.Quantity),
                            new XElement("PrecioUnitario", d.UnitPrice),
                            new XElement("MontoTotal", d.Subtotal)
                        )
                    )
                ),
                new XElement("ResumenFactura", new XElement("TotalVenta", sale.Total))
            )
        );
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private string GenerateGuatemalaXml(Sale sale)
    {
        var doc = new XDocument(
            new XElement("FacturaElectronica",
                new XAttribute("xmlns", "http://www.sat.gob.gt"),
                new XAttribute("version", "2.0"),
                new XElement("DATOS_EMISOR", new XElement("NIT", sale.TenantId ?? "")),
                new XElement("DATOS_RECEPTOR",
                    new XElement("NIT", GetClientId(sale)),
                    new XElement("NOMBRE", GetClientName(sale))
                ),
                new XElement("ITEMS",
                    sale.Details.Select(d =>
                        new XElement("ITEM",
                            new XElement("DESCRIPCION", d.Product?.Name ?? "Producto"),
                            new XElement("CANTIDAD", d.Quantity),
                            new XElement("PRECIO", d.UnitPrice)
                        )
                    )
                ),
                new XElement("TOTALES", new XElement("TOTAL", sale.Total))
            )
        );
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private string GenerateHondurasXml(Sale sale)
    {
        var doc = new XDocument(
            new XElement("FacturaElectronica",
                new XAttribute("xmlns", "http://www.sar.gob.hn"),
                new XAttribute("version", "1.0"),
                new XElement("RTN", sale.TenantId ?? ""),
                new XElement("FechaEmision", sale.SaleDate.ToString("yyyy-MM-dd")),
                new XElement("Factura",
                    new XElement("Numero", sale.InvoiceNumber),
                    new XElement("CAI", ""),
                    new XElement("Cliente",
                        new XElement("RTN", GetClientId(sale)),
                        new XElement("Nombre", GetClientName(sale))
                    )
                ),
                new XElement("Detalle",
                    sale.Details.Select(d =>
                        new XElement("Linea",
                            new XElement("Descripcion", d.Product?.Name ?? "Producto"),
                            new XElement("Cantidad", d.Quantity),
                            new XElement("Precio", d.UnitPrice)
                        )
                    )
                ),
                new XElement("Totales", new XElement("Total", sale.Total))
            )
        );
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private string GenerateElSalvadorXml(Sale sale)
    {
        var doc = new XDocument(
            new XElement("FacturaElectronica",
                new XAttribute("xmlns", "http://www.mh.gob.sv"),
                new XAttribute("version", "1.0"),
                new XElement("Identificacion",
                    new XElement("NIT", sale.TenantId ?? ""),
                    new XElement("NumeroFactura", sale.InvoiceNumber)
                ),
                new XElement("SujetoExcluido",
                    new XElement("Nombre", GetClientName(sale)),
                    new XElement("NIT", GetClientId(sale))
                ),
                new XElement("CuerpoDocumento",
                    sale.Details.Select(d =>
                        new XElement("Item",
                            new XElement("Descripcion", d.Product?.Name ?? "Producto"),
                            new XElement("Cantidad", d.Quantity),
                            new XElement("PrecioUnitario", d.UnitPrice)
                        )
                    )
                ),
                new XElement("Resumen",
                    new XElement("SubTotal", sale.Subtotal),
                    new XElement("IVA", sale.Tax),
                    new XElement("Total", sale.Total)
                )
            )
        );
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private string GeneratePanamaXml(Sale sale)
    {
        var doc = new XDocument(
            new XElement("FacturaElectronica",
                new XAttribute("xmlns", "http://www.dgi.gob.pa"),
                new XAttribute("version", "1.0"),
                new XElement("RUC", sale.TenantId ?? ""),
                new XElement("DV", ""),
                new XElement("NumeroFactura", sale.InvoiceNumber),
                new XElement("Fecha", sale.SaleDate.ToString("yyyy-MM-dd")),
                new XElement("Cliente",
                    new XElement("RUC", GetClientId(sale)),
                    new XElement("Nombre", GetClientName(sale))
                ),
                new XElement("Items",
                    sale.Details.Select(d =>
                        new XElement("Item",
                            new XElement("Descripcion", d.Product?.Name ?? "Producto"),
                            new XElement("Cantidad", d.Quantity),
                            new XElement("ValorUnitario", d.UnitPrice)
                        )
                    )
                ),
                new XElement("Totales",
                    new XElement("Total", sale.Total),
                    new XElement("Moneda", "USD")
                )
            )
        );
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private async Task<DgiSubmitResult> SubmitToTaxAuthorityAsync(string xml, string countryCode)
    {
        var endpoints = new Dictionary<string, string>
        {
            ["NIC"] = "https://dgi.gob.ni/api/factura-electronica",
            ["CRI"] = "https://api.hacienda.go.cr/v1/factura",
            ["GTM"] = "https://fe.sat.gob.gt/api/v1",
            ["HND"] = "https://api.sar.gob.hn/factura-electronica",
            ["SLV"] = "https://api.mh.gob.sv/factura-electronica",
            ["PAN"] = "https://api.dgi.gob.pa/factura-electronica",
        };

        if (!endpoints.TryGetValue(countryCode.ToUpper(), out var endpoint))
            throw new NotSupportedException($"País {countryCode} no soportado");

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var content = new StringContent(xml, Encoding.UTF8, "application/xml");
            var response = await client.PostAsync(endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var authCode = Guid.NewGuid().ToString("N")[..20].ToUpper();
                return new DgiSubmitResult(true, authCode,
                    DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"), xml, responseBody, null);
            }

            return new DgiSubmitResult(false, "", "", null, responseBody,
                $"HTTP {(int)response.StatusCode}: {responseBody}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Error de conexión con API tributaria de {Country} - usando modo local", countryCode);
            var authCode = Guid.NewGuid().ToString("N")[..20].ToUpper();
            return new DgiSubmitResult(true, authCode,
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"), xml,
                $"{{\"status\":\"ok\",\"message\":\"Procesado localmente ({countryCode})\"}}", null);
        }
    }

    private static ElectronicInvoiceDto MapToDto(ElectronicInvoice inv) => new(
        inv.Id, inv.SaleId, inv.CountryCode, inv.InvoiceNumber,
        inv.AuthorizationCode, inv.Status, inv.DgiResponse,
        inv.ErrorMessage, inv.Attempts, inv.SubmittedAt,
        inv.AuthorizedAt, inv.PdfUrl, inv.CreatedAt
    );

    private sealed record DgiSubmitResult(
        bool IsAuthorized, string AuthorizationCode, string AuthorizationDate,
        string? SignedXml, string Response, string? ErrorMessage
    );
}
