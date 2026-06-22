using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zorvian.Core.Entities;

namespace Zorvian.Infrastructure.Data;

/// <summary>
/// Seeds 6 professional document templates when the table is empty.
/// </summary>
public static class DocumentTemplateSeeder
{
    public static async Task SeedAsync(ZorvianDbContext db, ILogger logger)
    {
        if (await db.DocumentTemplates.IgnoreQueryFilters().AnyAsync())
            return;

        logger.LogInformation("Seeding document templates...");

        var now = DateTime.UtcNow;
        var tenantId = "SYSTEM";

        var templates = new List<DocumentTemplate>();

        // ── HR: Contrato de Trabajo ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Contrato de Trabajo a Termino Indefinido",
            Category = "HR",
            Module = "Employee",
            CountryCode = "NI",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"employee_name\",\"label\":\"Nombre del Empleado\",\"type\":\"text\",\"required\":true},{\"key\":\"employee_id\",\"label\":\"No. Identificacion\",\"type\":\"text\",\"required\":true},{\"key\":\"position\",\"label\":\"Cargo\",\"type\":\"text\",\"required\":true},{\"key\":\"salary\",\"label\":\"Salario Mensual\",\"type\":\"number\",\"required\":true},{\"key\":\"start_date\",\"label\":\"Fecha de Ingreso\",\"type\":\"date\",\"required\":true},{\"key\":\"department\",\"label\":\"Departamento\",\"type\":\"text\",\"required\":false}]",
            Content = BuildContractHtml()
        });

        // ── HR: Carta de Oferta ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Carta de Oferta Laboral",
            Category = "HR",
            Module = "Employee",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"candidate_name\",\"label\":\"Nombre del Candidato\",\"type\":\"text\",\"required\":true},{\"key\":\"position\",\"label\":\"Cargo Ofertado\",\"type\":\"text\",\"required\":true},{\"key\":\"salary\",\"label\":\"Salario Propuesto\",\"type\":\"number\",\"required\":true},{\"key\":\"start_date\",\"label\":\"Fecha de Inicio\",\"type\":\"date\",\"required\":true},{\"key\":\"benefits\",\"label\":\"Beneficios Adicionales\",\"type\":\"textarea\",\"required\":false}]",
            Content = BuildOfferLetterHtml()
        });

        // ── HR: Constancia de Empleo ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Constancia de Empleo",
            Category = "HR",
            Module = "Employee",
            CountryCode = "NI",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"employee_name\",\"label\":\"Nombre del Empleado\",\"type\":\"text\",\"required\":true},{\"key\":\"employee_id\",\"label\":\"No. Identificacion\",\"type\":\"text\",\"required\":true},{\"key\":\"position\",\"label\":\"Cargo\",\"type\":\"text\",\"required\":true},{\"key\":\"salary\",\"label\":\"Salario Mensual\",\"type\":\"number\",\"required\":true},{\"key\":\"start_date\",\"label\":\"Fecha de Ingreso\",\"type\":\"date\",\"required\":true},{\"key\":\"purpose\",\"label\":\"Proposito de la Constancia\",\"type\":\"text\",\"required\":false}]",
            Content = BuildEmploymentCertHtml()
        });

        // ── Sales: Cotizacion ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Cotizacion Comercial",
            Category = "Sales",
            Module = "Sale",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"client_name\",\"label\":\"Nombre del Cliente\",\"type\":\"text\",\"required\":true},{\"key\":\"quote_number\",\"label\":\"No. Cotizacion\",\"type\":\"text\",\"required\":true},{\"key\":\"quote_date\",\"label\":\"Fecha\",\"type\":\"date\",\"required\":true},{\"key\":\"validity_days\",\"label\":\"Dias de Validez\",\"type\":\"number\",\"required\":false},{\"key\":\"payment_terms\",\"label\":\"Condiciones de Pago\",\"type\":\"text\",\"required\":false},{\"key\":\"notes\",\"label\":\"Observaciones\",\"type\":\"textarea\",\"required\":false}]",
            Content = BuildQuoteHtml()
        });

        // ── Sales: Nota de Entrega ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Nota de Entrega",
            Category = "Sales",
            Module = "Sale",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"client_name\",\"label\":\"Nombre del Cliente\",\"type\":\"text\",\"required\":true},{\"key\":\"delivery_date\",\"label\":\"Fecha de Entrega\",\"type\":\"date\",\"required\":true},{\"key\":\"invoice_ref\",\"label\":\"Ref. Factura\",\"type\":\"text\",\"required\":false},{\"key\":\"carrier\",\"label\":\"Transportista\",\"type\":\"text\",\"required\":false},{\"key\":\"received_by\",\"label\":\"Recibido Por\",\"type\":\"text\",\"required\":false}]",
            Content = BuildDeliveryNoteHtml()
        });

        // ── Legal: Poder Notarial ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Poder Notarial General",
            Category = "Legal",
            Module = "General",
            CountryCode = "NI",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"grantor_name\",\"label\":\"Nombre del Otorgante\",\"type\":\"text\",\"required\":true},{\"key\":\"grantor_id\",\"label\":\"No. Identificacion\",\"type\":\"text\",\"required\":true},{\"key\":\"attorney_name\",\"label\":\"Nombre del Apoderado\",\"type\":\"text\",\"required\":true},{\"key\":\"attorney_id\",\"label\":\"No. Identificacion Apoderado\",\"type\":\"text\",\"required\":true},{\"key\":\"powers\",\"label\":\"Facultades Otorgadas\",\"type\":\"textarea\",\"required\":true},{\"key\":\"scope\",\"label\":\"Alcance del Poder\",\"type\":\"text\",\"required\":false}]",
            Content = BuildPowerOfAttorneyHtml()
        });

        await db.DocumentTemplates.AddRangeAsync(templates);
        await db.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} document templates successfully.", templates.Count);
    }

    // ═══════════════════════════════════════════════
    // HTML builders for each template
    // ═══════════════════════════════════════════════

    private static string SharedStyles() => @"
  <style>
    body { font-family: 'Segoe UI', Arial, sans-serif; margin: 40px; color: #1a1a2e; line-height: 1.7; }
    .header { text-align: center; border-bottom: 3px solid #1A0A3E; padding-bottom: 20px; margin-bottom: 30px; }
    .company-name { font-size: 28px; font-weight: 700; color: #1A0A3E; letter-spacing: 1px; }
    .doc-title { font-size: 20px; color: #7C4DFF; margin-top: 10px; font-weight: 600; }
    .field { font-weight: 600; color: #1A0A3E; }
    .section { margin: 20px 0; }
    .article { margin: 12px 0; padding-left: 10px; border-left: 3px solid #00E5FF; }
    .signature-area { margin-top: 80px; display: flex; justify-content: space-between; }
    .sig-block { text-align: center; width: 40%; }
    .sig-line { border-top: 1px solid #333; margin-top: 60px; padding-top: 8px; }
    .footer { margin-top: 40px; text-align: center; font-size: 11px; color: #888; }
    table.items { width: 100%; border-collapse: collapse; margin: 20px 0; }
    table.items th { background: #1A0A3E; color: white; padding: 10px 12px; text-align: left; font-size: 13px; }
    table.items td { padding: 10px 12px; border-bottom: 1px solid #eee; font-size: 13px; }
  </style>";

    private static string BuildContractHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>{SharedStyles()}</head>
<body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.Name }}}}</div>
    <div class='doc-title'>CONTRATO DE TRABAJO A TERMINO INDEFINIDO</div>
  </div>
  <p>En la ciudad de Managua, Republica de Nicaragua, a <strong>{{{{ Company.Date }}}}</strong>, entre
  <span class='field'>{{{{ Company.Name }}}}</span>, en adelante denominado <em>&quot;EL EMPLEADOR&quot;</em>,
  y <span class='field'>{{{{ employee_name }}}}</span>, identificado con No.
  <span class='field'>{{{{ employee_id }}}}</span>, en adelante denominado <em>&quot;EL EMPLEADO&quot;</em>,
  se celebra el presente contrato de trabajo, el cual se regira por las siguientes clausulas:</p>

  <div class='section'>
    <div class='article'>
      <strong>PRIMERA - OBJETO:</strong> EL EMPLEADO sera contratado para desempenar el cargo de
      <span class='field'>{{{{ position }}}}</span>, en el departamento de
      <span class='field'>{{{{ department | default: 'General' }}}}</span>.
    </div>
    <div class='article'>
      <strong>SEGUNDA - DURACION:</strong> El presente contrato tendra una duracion indefinida a partir del
      <span class='field'>{{{{ start_date }}}}</span>, sujeto al periodo de prueba de tres (3) meses.
    </div>
    <div class='article'>
      <strong>TERCERA - REMUNERACION:</strong> EL EMPLEADOR pagara a EL EMPLEADO un salario mensual de
      <span class='field'>{{{{ salary }}}}</span> Cordobas, sujeto a las deducciones de ley.
    </div>
    <div class='article'>
      <strong>CUARTA - JORNADA:</strong> La jornada laboral sera de lunes a viernes, conforme al horario establecido.
    </div>
    <div class='article'>
      <strong>QUINTA - CONFIDENCIALIDAD:</strong> EL EMPLEADO se compromete a mantener estricta confidencialidad
      sobre la informacion privilegiada de la empresa.
    </div>
    <div class='article'>
      <strong>SEXTA - TERMINACION:</strong> El presente contrato podra terminar por mutuo acuerdo, por justa causa
      conforme al Codigo del Trabajo, o por decision de cualquiera de las partes.
    </div>
  </div>
  <p>En senal de conformidad, las partes firman el presente contrato.</p>
  <div class='signature-area'>
    <div class='sig-block'><div class='sig-line'>{{{{ Company.Name }}}}</div><small>EL EMPLEADOR</small></div>
    <div class='sig-block'><div class='sig-line'>{{{{ employee_name }}}}</div><small>EL EMPLEADO</small></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente</div>
</body></html>";

    private static string BuildOfferLetterHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>{SharedStyles()}</head>
<body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.Name }}}}</div>
    <div class='doc-title'>CARTA DE OFERTA LABORAL</div>
  </div>
  <p>Managua, <span class='field'>{{{{ Company.Date }}}}</span></p>
  <p><strong>A:</strong> <span class='field'>{{{{ candidate_name }}}}</span></p>
  <p><em>Presente.</em></p>
  <p>Estimado(a) <span class='field'>{{{{ candidate_name }}}}</span>,</p>
  <p>Tenemos el agrado de comunicarle que ha sido seleccionado(a) para ocupar el cargo de:</p>
  <div style='background:#f0e6ff; padding:16px 20px; border-radius:8px; margin:16px 0; border-left:4px solid #7C4DFF;'>
    <p style='margin:0; font-size:18px;'><strong>Cargo:</strong> <span class='field'>{{{{ position }}}}</span></p>
  </div>
  <div class='section'>
    <p>Los terminos de nuestra oferta son los siguientes:</p>
    <ul>
      <li><strong>Salario mensual:</strong> <span class='field'>{{{{ salary }}}}</span> Cordobas</li>
      <li><strong>Fecha de inicio:</strong> <span class='field'>{{{{ start_date }}}}</span></li>
      <li><strong>Lugar de trabajo:</strong> Oficinas de {{{{ Company.Name }}}}</li>
      <li><strong>Periodo de prueba:</strong> 3 meses</li>
      {{% if benefits != blank %}}<li><strong>Beneficios adicionales:</strong> {{{{ benefits }}}}</li>{{% endif %}}
    </ul>
  </div>
  <p>Su ingreso estara sujeto a la presentacion de: identificacion personal, certificado de antecedentes y examen medico admissional.</p>
  <p>Le invitamos a confirmar su aceptacion respondiendo a este documento.</p>
  <p>Quedamos a su disposicion para cualquier consulta.</p>
  <p>Atentamente,</p>
  <div class='signature-area' style='justify-content:flex-end;'>
    <div class='sig-block' style='width:50%;'><div class='sig-line'>{{{{ Company.Name }}}}</div><small>Departamento de Recursos Humanos</small></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente</div>
</body></html>";

    private static string BuildEmploymentCertHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>{SharedStyles()}</head>
<body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.Name }}}}</div>
    <div class='doc-title'>CONSTANCIA DE EMPLEO</div>
  </div>
  <p style='text-align:justify;'>Managua, <span class='field'>{{{{ Company.Date }}}}</span></p>
  <p style='text-align:justify;'>Quien suscribe, REPRESENTANTE LEGAL de <span class='field'>{{{{ Company.Name }}}}</span>,
  por medio de la presente CERTIFICA que:</p>
  <p style='text-align:justify;'>El(la) senor(a) <span class='field'>{{{{ employee_name }}}}</span>, identificado(a) con cedula No.
  <span class='field'>{{{{ employee_id }}}}</span>, labora en esta empresa desde el dia
  <span class='field'>{{{{ start_date }}}}</span>, desempenandose en el cargo de
  <span class='field'>{{{{ position }}}}</span>, devengando un salario mensual de
  <span class='field'>{{{{ salary }}}}</span> Cordobas.</p>
  <p style='text-align:justify;'>La presente constancia se expide a solicitud del interesado(a)
  {{% if purpose != blank %}}para los fines de <span class='field'>{{{{ purpose }}}}</span>{{% else %}}para los fines que el(la) interesado(a) considere convenientes{{% endif %}},
  sin que por ello se genere responsabilidad alguna para la empresa.</p>
  <div style='margin-top:80px; text-align:center;'>
    <div style='display:inline-block; width:250px; border-top: 1px solid #333; padding-top: 8px;'>
      <strong>{{{{ Company.Name }}}}</strong><br><small>Representante Legal</small>
    </div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente</div>
</body></html>";

    private static string BuildQuoteHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #7C4DFF; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .meta-table {{ width: 100%; margin: 15px 0; }} .meta-table td {{ padding: 4px 0; }}
    .meta-label {{ color: #666; width: 120px; }} .meta-value {{ font-weight: 600; color: #1A0A3E; }}
    .totals {{ text-align: right; margin-top: 10px; }}
    .totals-grand {{ font-size: 18px; font-weight: 700; color: #7C4DFF; border-top: 2px solid #1A0A3E; padding-top: 8px; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.Name }}}}</div>
    <div class='doc-badge'>COTIZACION</div>
  </div>
  <table class='meta-table'>
    <tr><td class='meta-label'>No.:</td><td class='meta-value'>{{{{ quote_number }}}}</td></tr>
    <tr><td class='meta-label'>Fecha:</td><td class='meta-value'>{{{{ quote_date }}}}</td></tr>
    <tr><td class='meta-label'>Cliente:</td><td class='meta-value'>{{{{ client_name }}}}</td></tr>
    {{% if payment_terms != blank %}}<tr><td class='meta-label'>Pago:</td><td class='meta-value'>{{{{ payment_terms }}}}</td></tr>{{% endif %}}
  </table>
  <table class='items'>
    <thead><tr><th>Descripcion</th><th style='text-align:center;'>Cant.</th><th style='text-align:right;'>Precio Unit.</th><th style='text-align:right;'>Subtotal</th></tr></thead>
    <tbody><tr><td colspan='4' style='text-align:center; color:#888; padding:20px;'><em>Los items se cargan dinamicamente desde la cotizacion del sistema.</em></td></tr></tbody>
  </table>
  <div class='totals'>
    <div>Subtotal: <strong>{{{{ sale.Subtotal | default: '0.00' }}}}</strong></div>
    <div>ISV (15%): <strong>{{{{ sale.Tax | default: '0.00' }}}}</strong></div>
    <div>Descuento: <strong>{{{{ sale.Discount | default: '0.00' }}}}</strong></div>
    <div class='totals-grand'>TOTAL: <strong>{{{{ sale.Total | default: '0.00' }}}}</strong></div>
  </div>
  {{% if notes != blank %}}<p style='margin-top:20px; padding:12px; background:#f0f0f0; border-radius:6px;'><strong>Observaciones:</strong> {{{{ notes }}}}</p>{{% endif %}}
  <p style='margin-top:20px; font-size:13px;'>{{% if validity_days != blank %}}Vigencia: {{{{ validity_days }}}} dias.{{% else %}}Vigencia: 15 dias.{{% endif %}}</p>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente</div>
</body></html>";

    private static string BuildDeliveryNoteHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .header {{ border-bottom-color: #2EE59D; }}
    .doc-title {{ color: #2EE59D; }}
    .meta-box {{ display: flex; gap: 20px; margin: 15px 0; }}
    .meta-card {{ flex: 1; background: #f0fdf4; padding: 12px 16px; border-radius: 8px; border-left: 3px solid #2EE59D; }}
    .meta-card small {{ color: #666; display: block; margin-bottom: 4px; }}
    .meta-card strong {{ color: #1A0A3E; }}
    .sig-area {{ display: flex; justify-content: space-between; margin-top: 80px; }}
    .sig-block {{ text-align: center; width: 30%; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.Name }}}}</div>
    <div class='doc-title'>NOTA DE ENTREGA</div>
  </div>
  <div class='meta-box'>
    <div class='meta-card'><small>Fecha de Entrega</small><strong>{{{{ delivery_date }}}}</strong></div>
    <div class='meta-card'><small>Cliente</small><strong>{{{{ client_name }}}}</strong></div>
    {{% if invoice_ref != blank %}}<div class='meta-card'><small>Ref. Factura</small><strong>{{{{ invoice_ref }}}}</strong></div>{{% endif %}}
  </div>
  <table class='items'>
    <thead><tr><th>Descripcion</th><th style='text-align:center;'>Cantidad</th><th style='text-align:left;'>Observaciones</th></tr></thead>
    <tbody><tr><td colspan='3' style='text-align:center; color:#888; padding:20px;'><em>Los items se cargan desde la orden de entrega del sistema.</em></td></tr></tbody>
  </table>
  {{% if carrier != blank %}}<p><strong>Transportista:</strong> {{{{ carrier }}}}</p>{{% endif %}}
  <p><strong>Recibido por:</strong> {{{{ received_by | default: '_________________________' }}}}</p>
  <p style='margin-top:20px; font-size:13px;'>Al firmar esta nota, el receptor confirma la correcta recepcion de la mercancia descrita.</p>
  <div class='sig-area'>
    <div class='sig-block'><div class='sig-line'>Entregado por</div></div>
    <div class='sig-block'><div class='sig-line'>Recibido por</div></div>
    <div class='sig-block'><div class='sig-line'>Vo.Bo. Admin.</div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente</div>
</body></html>";

    private static string BuildPowerOfAttorneyHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  <style>
    body {{ font-family: 'Georgia', 'Times New Roman', serif; margin: 40px; color: #1a1a2e; line-height: 2; font-size: 15px; }}
    .header {{ text-align: center; border-bottom: 2px double #1A0A3E; padding-bottom: 20px; margin-bottom: 30px; }}
    .company-name {{ font-size: 18px; color: #666; letter-spacing: 2px; text-transform: uppercase; }}
    .doc-title {{ font-size: 22px; font-weight: 700; color: #1A0A3E; margin-top: 10px; }}
    .body-text {{ text-align: justify; text-indent: 40px; margin: 12px 0; }}
    .field {{ font-weight: 700; color: #1A0A3E; }}
    .article {{ margin: 16px 0; }}
    .closing {{ text-align: center; margin-top: 30px; font-style: italic; }}
    .footer {{ margin-top: 40px; text-align: center; font-size: 10px; color: #aaa; border-top: 1px solid #ddd; padding-top: 10px; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.Name }}}}</div>
    <div class='doc-title'>PODER NOTARIAL GENERAL</div>
  </div>
  <p class='body-text'>En la ciudad de Managua, Republica de Nicaragua, comparece ante mi:</p>
  <p class='body-text'>El(la) senor(a) <span class='field'>{{{{ grantor_name }}}}</span>, identificado(a) con cedula No. <span class='field'>{{{{ grantor_id }}}}</span>,
  en adelante denominado(a) <em>&quot;EL OTORGANTE&quot;</em>, quien por su libre y espontanea voluntad, otorga el presente PODER GENERAL a favor de:</p>
  <p class='body-text'>El(la) senor(a) <span class='field'>{{{{ attorney_name }}}}</span>, identificado(a) con cedula No. <span class='field'>{{{{ attorney_id }}}}</span>,
  en adelante denominado(a) <em>&quot;EL APODERADO&quot;</em>, a quien EL OTORGANTE designa como su representante legal con las siguientes facultades:</p>
  <div class='article'>
    <p><strong>PRIMERA - FACULTADES:</strong> EL APODERADO queda facultado para:</p>
    <p class='body-text'>{{{{ powers }}}}</p>
  </div>
  {{% if scope != blank %}}<div class='article'><p><strong>SEGUNDA - ALCANCE:</strong> {{{{ scope }}}}</p></div>{{% endif %}}
  <div class='article'>
    <p><strong>{{% if scope != blank %}}TERCERA{{% else %}}SEGUNDA{{% endif %}} - REVOCACION:</strong> El presente podra ser revocado en cualquier momento por EL OTORGANTE.</p>
  </div>
  <div class='article'>
    <p><strong>{{% if scope != blank %}}CUARTA{{% else %}}TERCERA{{% endif %}} - VIGENCIA:</strong> El presente entrara en vigor a partir de su firma y tendra validez mientras no sea expresamente revocado.</p>
  </div>
  <p class='closing'>En senal de conformidad, se firma el presente documento.</p>
  <div style='margin-top:100px; text-align:center;'>
    <div style='display:inline-block; text-align:center; margin: 0 40px;'>
      <div style='width:250px; border-top: 1px solid #333; padding-top: 8px;'><strong>EL OTORGANTE</strong><br><small>{{{{ grantor_name }}}}</small></div>
    </div>
    <div style='display:inline-block; text-align:center; margin: 0 40px;'>
      <div style='width:250px; border-top: 1px solid #333; padding-top: 8px;'><strong>EL APODERADO</strong><br><small>{{{{ attorney_name }}}}</small></div>
    </div>
  </div>
  <p style='margin-top:20px; font-size:12px; color:#888;'>Firma autorizada: ____________________________ &nbsp;&nbsp;&nbsp; Sello:</p>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente</div>
</body></html>";
}
