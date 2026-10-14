using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zorvian.Core.Entities;

namespace Zorvian.Infrastructure.Data;

/// <summary>
/// Seeds 7 professional document templates when the table is empty.
/// </summary>
public static class DocumentTemplateSeeder
{
    public static async Task SeedAsync(ZorvianDbContext db, ILogger logger)
    {
        await UpgradeSeededTemplatesAsync(db, logger);

        if (await db.DocumentTemplates.IgnoreQueryFilters().AnyAsync())
            return;

        logger.LogInformation("Seeding document templates...");

        var templates = BuildTemplates(DateTime.UtcNow);
        await db.DocumentTemplates.AddRangeAsync(templates);
        await db.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} document templates successfully.", templates.Count);
    }

    private static List<DocumentTemplate> BuildTemplates(DateTime now)
    {
        var tenantId = DocumentTemplate.SystemTenantId;
        var templates = new List<DocumentTemplate>();

        // ── HR: Contrato de Trabajo (multi-país: NIC/CRI/GTM/HND/SLV/PAN) ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Contrato de Trabajo a Término Indefinido",
            Category = "HR",
            Module = "Employee",
            CountryCode = "ALL",
            IsActive = true,
            Version = "2.1",
            Variables = "[{\"key\":\"employee_name\",\"label\":\"Nombre del Trabajador\",\"type\":\"text\",\"required\":true},{\"key\":\"employee_id\",\"label\":\"No. Identificación\",\"type\":\"text\",\"required\":true},{\"key\":\"position\",\"label\":\"Cargo\",\"type\":\"text\",\"required\":true},{\"key\":\"salary\",\"label\":\"Salario Mensual\",\"type\":\"number\",\"required\":true},{\"key\":\"start_date\",\"label\":\"Fecha de Ingreso\",\"type\":\"date\",\"required\":true},{\"key\":\"department\",\"label\":\"Departamento\",\"type\":\"text\",\"required\":false}]",
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
            Version = "2.1",
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
            CountryCode = "ALL",
            IsActive = true,
            Version = "2.1",
            Variables = "[{\"key\":\"employee_name\",\"label\":\"Nombre del Trabajador\",\"type\":\"text\",\"required\":true},{\"key\":\"employee_id\",\"label\":\"No. Identificación\",\"type\":\"text\",\"required\":true},{\"key\":\"position\",\"label\":\"Cargo\",\"type\":\"text\",\"required\":true},{\"key\":\"salary\",\"label\":\"Salario Mensual\",\"type\":\"number\",\"required\":true},{\"key\":\"start_date\",\"label\":\"Fecha de Ingreso\",\"type\":\"date\",\"required\":true},{\"key\":\"purpose\",\"label\":\"Propósito de la Constancia\",\"type\":\"text\",\"required\":false}]",
            Content = BuildEmploymentCertHtml()
        });

        // ── Sales: Cotizacion ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Cotización Comercial",
            Category = "Sales",
            Module = "Sale",
            CountryCode = "ALL",
            IsActive = true,
            Version = "2.1",
            Variables = "[{\"key\":\"client_name\",\"label\":\"Nombre del Cliente\",\"type\":\"text\",\"required\":true},{\"key\":\"quote_number\",\"label\":\"No. Cotización\",\"type\":\"text\",\"required\":true},{\"key\":\"quote_date\",\"label\":\"Fecha\",\"type\":\"date\",\"required\":true},{\"key\":\"validity_days\",\"label\":\"Días de Validez\",\"type\":\"number\",\"required\":false},{\"key\":\"payment_terms\",\"label\":\"Condiciones de Pago\",\"type\":\"text\",\"required\":false},{\"key\":\"notes\",\"label\":\"Observaciones\",\"type\":\"textarea\",\"required\":false}]",
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
            Version = "2.1",
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
            CountryCode = "ALL",
            IsActive = true,
            Version = "2.1",
            Variables = "[{\"key\":\"grantor_name\",\"label\":\"Nombre del Otorgante\",\"type\":\"text\",\"required\":true},{\"key\":\"grantor_id\",\"label\":\"No. Identificación\",\"type\":\"text\",\"required\":true},{\"key\":\"attorney_name\",\"label\":\"Nombre del Apoderado\",\"type\":\"text\",\"required\":true},{\"key\":\"attorney_id\",\"label\":\"No. Identificación Apoderado\",\"type\":\"text\",\"required\":true},{\"key\":\"powers\",\"label\":\"Facultades Otorgadas\",\"type\":\"textarea\",\"required\":true},{\"key\":\"scope\",\"label\":\"Alcance del Poder\",\"type\":\"text\",\"required\":false}]",
            Content = BuildPowerOfAttorneyHtml()
        });

        // ── HR: Liquidación Final (específica de Nicaragua: CT art. 93/78/45, Ley 822, INSS, INATEC) ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Liquidación Final (Nicaragua)",
            Category = "HR",
            Module = "Employee",
            CountryCode = "NIC",
            IsActive = true,
            Version = "1.1",
            Variables = "[{\"key\":\"employee_name\",\"label\":\"Nombre del Trabajador\",\"type\":\"text\",\"required\":true},{\"key\":\"employee_id\",\"label\":\"Cédula de Identidad\",\"type\":\"text\",\"required\":true},{\"key\":\"retiro_reason\",\"label\":\"Motivo de Retiro\",\"type\":\"text\",\"required\":false},{\"key\":\"contract_type\",\"label\":\"Tipo de Contrato\",\"type\":\"text\",\"required\":false},{\"key\":\"seniority\",\"label\":\"Antigüedad\",\"type\":\"text\",\"required\":false},{\"key\":\"position\",\"label\":\"Cargo Desempeñado\",\"type\":\"text\",\"required\":false},{\"key\":\"fecha_elaboracion\",\"label\":\"Fecha de Elaboración\",\"type\":\"date\",\"required\":false},{\"key\":\"fecha_ingreso\",\"label\":\"Fecha de Ingreso\",\"type\":\"date\",\"required\":true},{\"key\":\"fecha_salida\",\"label\":\"Fecha de Salida\",\"type\":\"date\",\"required\":true},{\"key\":\"salario_mensual\",\"label\":\"Salario Ordinario Mensual\",\"type\":\"number\",\"required\":true},{\"key\":\"salario_promedio\",\"label\":\"Salario Promedio (6 meses)\",\"type\":\"number\",\"required\":false},{\"key\":\"salario_mas_alto\",\"label\":\"Salario Más Alto (6 meses)\",\"type\":\"number\",\"required\":false},{\"key\":\"sueldo_diario\",\"label\":\"Sueldo Diario\",\"type\":\"number\",\"required\":false},{\"key\":\"sueldo_por_hora\",\"label\":\"Sueldo por Hora\",\"type\":\"number\",\"required\":false},{\"key\":\"hora_extra_valor\",\"label\":\"Valor de una Hora Extra\",\"type\":\"number\",\"required\":false},{\"key\":\"sueldo_pendiente\",\"label\":\"Sueldo Pendiente de Pago\",\"type\":\"number\",\"required\":false},{\"key\":\"horas_extras\",\"label\":\"Monto de Horas Extras\",\"type\":\"number\",\"required\":false},{\"key\":\"otros_conceptos\",\"label\":\"Otros Conceptos\",\"type\":\"text\",\"required\":false},{\"key\":\"otros_monto\",\"label\":\"Monto Otros Conceptos\",\"type\":\"number\",\"required\":false},{\"key\":\"periodo_aguinaldo\",\"label\":\"Periodo Aguinaldo (DEL - AL)\",\"type\":\"text\",\"required\":false},{\"key\":\"aguinaldo_dias\",\"label\":\"Días de Aguinaldo\",\"type\":\"number\",\"required\":false},{\"key\":\"aguinaldo_monto\",\"label\":\"Monto Aguinaldo\",\"type\":\"number\",\"required\":false},{\"key\":\"periodo_vacaciones\",\"label\":\"Periodo Vacaciones (DEL - AL)\",\"type\":\"text\",\"required\":false},{\"key\":\"vacaciones_dias\",\"label\":\"Días de Vacaciones a Pagar\",\"type\":\"number\",\"required\":false},{\"key\":\"vac_deducciones\",\"label\":\"Deducción de Vacaciones (días)\",\"type\":\"number\",\"required\":false},{\"key\":\"vacaciones_monto\",\"label\":\"Monto Vacaciones\",\"type\":\"number\",\"required\":false},{\"key\":\"periodo_indem\",\"label\":\"Periodo Indemnización (DEL - AL)\",\"type\":\"text\",\"required\":false},{\"key\":\"indemnizacion_monto\",\"label\":\"Monto Indemnización por Antigüedad\",\"type\":\"number\",\"required\":false},{\"key\":\"inss_laboral\",\"label\":\"INSS Laboral (7%)\",\"type\":\"number\",\"required\":false},{\"key\":\"ir_ord\",\"label\":\"IR - Salario Ordinario\",\"type\":\"number\",\"required\":false},{\"key\":\"ir_ocasional\",\"label\":\"IR - Pagos Ocasionales\",\"type\":\"number\",\"required\":false},{\"key\":\"ir_indemnizacion\",\"label\":\"IR - Retención Indemnización\",\"type\":\"number\",\"required\":false},{\"key\":\"prestamo\",\"label\":\"Saldo de Préstamo\",\"type\":\"number\",\"required\":false},{\"key\":\"aguinaldo_pagado\",\"label\":\"Aguinaldo Pagado\",\"type\":\"number\",\"required\":false},{\"key\":\"inss_patronal\",\"label\":\"INSS Patronal (21.5%)\",\"type\":\"number\",\"required\":false},{\"key\":\"inatec\",\"label\":\"INATEC (2%)\",\"type\":\"number\",\"required\":false},{\"key\":\"total_ingresos\",\"label\":\"Total Ingresos\",\"type\":\"number\",\"required\":false},{\"key\":\"total_deducciones\",\"label\":\"Total Deducciones\",\"type\":\"number\",\"required\":false},{\"key\":\"neto_a_recibir\",\"label\":\"Neto a Recibir\",\"type\":\"number\",\"required\":true},{\"key\":\"neto_en_letras\",\"label\":\"Cantidad en Letras\",\"type\":\"textarea\",\"required\":true},{\"key\":\"elaborado_por\",\"label\":\"Elaborado Por\",\"type\":\"text\",\"required\":false},{\"key\":\"revisado_por\",\"label\":\"Revisado Por\",\"type\":\"text\",\"required\":false},{\"key\":\"autorizado_por\",\"label\":\"Autorizado Por\",\"type\":\"text\",\"required\":false}]",
            Content = BuildLiquidationFinalHtml()
        });

        return templates;
    }

    /// <summary>
    /// Upgrade idempotente: refresca las plantillas sembradas con versiones anteriores
    /// (1.0 hardcodeadas a Nicaragua / 2.0 sin jornada multi-país) al contenido actual.
    /// La "Liquidación Final (Nicaragua)" se siembra en 1.1 → queda fuera del filtro
    /// para no pisar ediciones de usuario en cada arranque.
    /// Se empareja por nombre normalizado (sin acentos, minúsculas) para que renombres
    /// como "Cotización Comercial" sigan matcheando "Cotizacion Comercial".
    /// </summary>
    private static async Task UpgradeSeededTemplatesAsync(ZorvianDbContext db, ILogger logger)
    {
        var freshByKey = BuildTemplates(DateTime.UtcNow)
            .ToDictionary(t => NormalizeTemplateName(t.Name), t => t);

        var outdated = await db.DocumentTemplates.IgnoreQueryFilters()
            .Where(t => t.Version == "1.0" || t.Version == "2.0")
            .ToListAsync();

        var updated = 0;
        foreach (var row in outdated)
        {
            if (!freshByKey.TryGetValue(NormalizeTemplateName(row.Name), out var fresh))
                continue;

            row.Name = fresh.Name;
            row.Category = fresh.Category;
            row.Module = fresh.Module;
            row.CountryCode = fresh.CountryCode;
            row.Version = fresh.Version;
            row.Variables = fresh.Variables;
            row.Content = fresh.Content;
            row.UpdatedAt = DateTime.UtcNow;
            row.UpdatedBy = "system";
            updated++;
        }

        if (updated == 0)
            return;

        await db.SaveChangesAsync();
        logger.LogInformation(
            "Refreshed {Count} seeded document template(s) to the current multi-country version.",
            updated);
    }

    private static string NormalizeTemplateName(string name) =>
        string.Concat(name
                .Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                    != System.Globalization.UnicodeCategory.NonSpacingMark))
            .ToLowerInvariant();

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
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-title'>CONTRATO INDIVIDUAL DE TRABAJO A TÉRMINO INDEFINIDO</div>
    <div style='font-size:13px;color:#555;margin-top:6px;'>{{{{ Country.Name }}}} &middot; {{{{ Branch.Name | default: 'Sede principal' }}}}</div>
  </div>

  <p><strong>PARTES CONTRATANTES</strong></p>
  <p>En {{{{ Contract.City | default: '____________' }}}}, a los {{{{ Company.Date }}}}, comparecen de una parte
  <span class='field'>{{{{ Company.LegalName }}}}</span>, identificada con {{{{ Company.TaxIdLabel }}}} No.
  <span class='field'>{{{{ Company.TaxId | default: '____________' }}}}</span>, con domicilio en
  <span class='field'>{{{{ Company.Address | default: '____________' }}}}</span>, en adelante
  <strong>EL EMPLEADOR</strong>; y de otra parte <span class='field'>{{{{ employee_name }}}}</span>, mayor de edad,
  identificado(a) con {{{{ Country.IdDocument }}}} No.
  <span class='field'>{{{{ employee_id | default: '____________' }}}}</span>, con domicilio en
  <span class='field'>{{{{ Employee.Address | default: '____________' }}}}</span>, en adelante
  <strong>EL TRABAJADOR</strong>. Ambas partes, en plena capacidad legal y en ejercicio de sus derechos, celebran el
  presente Contrato Individual de Trabajo a Término Indefinido, que se regirá por las siguientes cláusulas:</p>

  <div class='section'>
    <div class='article'>
      <strong>CLÁUSULA PRIMERA — OBJETO:</strong> EL TRABAJADOR se obliga a prestar sus servicios personales e
      indelegables como <span class='field'>{{{{ position | default: '____________' }}}}</span>
      {{% if department != blank %}}, en el departamento de <span class='field'>{{{{ department }}}}</span>{{% endif %}},
      reportando a sus superiores jerárquicos y desempeñando las tareas propias de su cargo y las que sean
      compatibles con su formación profesional.
    </div>

    <div class='article'>
      <strong>CLÁUSULA SEGUNDA — PLAZO Y PERÍODO DE PRUEBA:</strong> El presente contrato es a término indefinido e
      inicia el <span class='field'>{{{{ start_date }}}}</span>. Las partes acuerdan un periodo de prueba de
      <span class='field'>{{{{ Country.ProbationMonths }}}} meses</span>, conforme a lo dispuesto en el
      {{{{ Country.LaborLaw }}}}. Vencido el periodo de prueba, el contrato continuará produciendo todos sus
      efectos legales.
    </div>

    <div class='article'>
      <strong>CLÁUSULA TERCERA — JORNADA DE TRABAJO:</strong> La jornada ordinaria
      <span class='field'>{{{{ Country.WeeklySchedule }}}}</span>, por un total de
      <span class='field'>{{{{ Country.WeeklyHours }}}} horas semanales</span>, conforme al horario que
      defina EL EMPLEADOR dentro de los límites legales. Las horas extraordinarias se compensarán o pagarán
      conforme a la legislación vigente de {{{{ Country.Name }}}}.
    </div>

    <div class='article'>
      <strong>CLÁUSULA CUARTA — REMUNERACIÓN:</strong> EL EMPLEADOR pagará a EL TRABAJADOR la cantidad de
      <span class='field'>{{{{ salary }}}} {{{{ Country.Currency }}}}</span> ({{{{ Country.CurrencyName }}}})
      mensuales, pagaderos en la periodicidad que establezca la ley y sujetos a las deducciones autorizadas,
      incluyendo los aportes al {{{{ Country.SocialSecurity }}}}.
    </div>

    <div class='article'>
      <strong>CLÁUSULA QUINTA — LUGAR DE TRABAJO:</strong> Los servicios se prestarán en la sede
      <span class='field'>{{{{ Branch.Name | default: 'principal' }}}}</span>, ubicada en
      <span class='field'>{{{{ Branch.Address | default: Company.Address | default: '____________' }}}}</span>.
      EL EMPLEADOR podrá cambiar el lugar de prestación por razones operativas, con aviso razonable al trabajador.
    </div>

    <div class='article'>
      <strong>CLÁUSULA SEXTA — OBLIGACIONES DEL EMPLEADOR:</strong> Pagar puntualmente la remuneración pactada;
      afiliar y reportar a EL TRABAJADOR ante el {{{{ Country.SocialSecurity }}}}; proporcionar condiciones de
      trabajo seguras y saludables; respetar la dignidad y derechos del trabajador; reconocer los beneficios
      sociales de ley, incluyendo {{{{ Country.ThirteenthMonth }}}} y vacaciones de
      <span class='field'>{{{{ Country.VacationDays }}}} días</span> anuales remuneradas; y cumplir el
      {{{{ Country.LaborLaw }}}}.
    </div>

    <div class='article'>
      <strong>CLÁUSULA SÉPTIMA — OBLIGACIONES DEL TRABAJADOR:</strong> Prestar el servicio con lealtad, dedicación
      y buen obrar; cumplir las instrucciones, normas y reglamentos internos; asistir con puntualidad; cuidar los
      bienes, equipos e insumos a su cargo; guardar reserva de la información de la empresa; y notificar de
      inmediato cualquier cambio que afecte la relación laboral.
    </div>

    <div class='article'>
      <strong>CLÁUSULA OCTAVA — CONFIDENCIALIDAD Y PROPIEDAD INTELECTUAL:</strong> EL TRABAJADOR se obliga a
      conservar estricta reserva sobre toda la información técnica, comercial, financiera o estratégica a la que
      acceda con ocasión del trabajo. Los resultados, desarrollos e invenciones realizados en el marco de esta
      relación laboral pertenecen en plena propiedad a EL EMPLEADOR.
    </div>

    <div class='article'>
      <strong>CLÁUSULA NOVENA — PROTECCIÓN DE DATOS PERSONALES:</strong> EL EMPLEADOR tratará los datos
      personales de EL TRABAJADOR conforme a la normativa de protección de datos vigente en {{{{ Country.Name }}}},
      con la finalidad de gestionar la relación laboral, la nómina y el cumplimiento de obligaciones legales.
    </div>

    <div class='article'>
      <strong>CLÁUSULA DÉCIMA — TERMINACIÓN:</strong> El contrato podrá terminar por mutuo acuerdo, renuncia,
      despido con o sin causa justa, muerte del trabajador u otras causas previstas en el {{{{ Country.LaborLaw }}}},
      respetando los preavisos, indemnizaciones y derechos adquiridos que la ley establezca.
    </div>

    <div class='article'>
      <strong>CLÁUSULA DÉCIMA PRIMERA — LEY APLICABLE Y JURISDICCIÓN:</strong> El presente contrato se rige por
      el {{{{ Country.LaborLaw }}}}. Para cualquier controversia derivada del mismo, las partes se someten a los
      tribunales competentes de {{{{ Contract.City | default: '____________' }}}}, {{{{ Country.Name }}}}.
    </div>
  </div>

  <p>En señal de conformidad, las partes firman el presente contrato en el lugar y fecha indicados.</p>

  <div class='signature-area'>
    <div class='sig-block'>
      <div class='sig-line'>{{{{ Company.LegalName }}}}</div>
      <small>EL EMPLEADOR</small><br />
      <small>{{{{ Company.TaxIdLabel }}}}: {{{{ Company.TaxId | default: '____________' }}}}</small>
    </div>
    <div class='sig-block'>
      <div class='sig-line'>{{{{ employee_name }}}}</div>
      <small>EL TRABAJADOR</small><br />
      <small>{{{{ Country.IdDocument }}}}: {{{{ employee_id | default: '____________' }}}}</small>
    </div>
  </div>
  <div class='footer'>Generado por Zorvian ERP · Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildOfferLetterHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>{SharedStyles()}</head>
<body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-title'>CARTA DE OFERTA LABORAL</div>
  </div>
  <p>{{{{ Contract.City }}}}, <span class='field'>{{{{ Company.Date }}}}</span></p>
  <p><strong>A:</strong> <span class='field'>{{{{ candidate_name }}}}</span></p>
  <p><em>Presente.</em></p>
  <p>Estimado(a) <span class='field'>{{{{ candidate_name }}}}</span>,</p>
  <p>Tenemos el agrado de comunicarle que ha sido seleccionado(a) para ocupar el cargo de:</p>
  <div style='background:#f0e6ff; padding:16px 20px; border-radius:8px; margin:16px 0; border-left:4px solid #7C4DFF;'>
    <p style='margin:0; font-size:18px;'><strong>Cargo:</strong> <span class='field'>{{{{ position }}}}</span></p>
  </div>
  <div class='section'>
    <p>Los términos de nuestra oferta son los siguientes:</p>
    <ul>
      <li><strong>Salario mensual:</strong> <span class='field'>{{{{ salary }}}}</span> {{{{ Country.Currency }}}} ({{{{ Country.CurrencyName }}}})</li>
      <li><strong>Fecha de inicio:</strong> <span class='field'>{{{{ start_date }}}}</span></li>
      <li><strong>Lugar de trabajo:</strong> Oficinas de {{{{ Company.LegalName }}}}</li>
      <li><strong>Periodo de prueba:</strong> {{{{ Country.ProbationMonths }}}} meses</li>
      {{% if benefits != blank %}}<li><strong>Beneficios adicionales:</strong> {{{{ benefits }}}}</li>{{% endif %}}
    </ul>
  </div>
  <p>Su ingreso estará sujeto a la presentación de: identificación personal ({{{{ Country.IdDocument }}}}), certificado de antecedentes y examen médico admissional.</p>
  <p>Le invitamos a confirmar su aceptación respondiendo a este documento.</p>
  <p>Quedamos a su disposicion para cualquier consulta.</p>
  <p>Atentamente,</p>
  <div class='signature-area' style='justify-content:flex-end;'>
    <div class='sig-block' style='width:50%;'><div class='sig-line'>{{{{ Company.LegalName }}}}</div><small>Departamento de Recursos Humanos</small></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente</div>
</body></html>";

    private static string BuildEmploymentCertHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>{SharedStyles()}</head>
<body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-title'>CONSTANCIA DE EMPLEO</div>
  </div>
  <p style='text-align:justify;'>{{{{ Contract.City }}}}, <span class='field'>{{{{ Company.Date }}}}</span></p>
  <p style='text-align:justify;'>Quien suscribe, REPRESENTANTE LEGAL de <span class='field'>{{{{ Company.LegalName }}}}</span>,
  por medio de la presente CERTIFICA que:</p>
  <p style='text-align:justify;'>El(la) señor(a) <span class='field'>{{{{ employee_name }}}}</span>, identificado(a) con {{{{ Country.IdDocument }}}} No.
  <span class='field'>{{{{ employee_id }}}}</span>, labora en esta empresa desde el día
  <span class='field'>{{{{ start_date }}}}</span>, desempeñándose en el cargo de
  <span class='field'>{{{{ position }}}}</span>, devengando un salario mensual de
  <span class='field'>{{{{ salary }}}}</span> {{{{ Country.Currency }}}} ({{{{ Country.CurrencyName }}}}).</p>
  <p style='text-align:justify;'>La presente constancia se expide a solicitud del interesado(a)
  {{% if purpose != blank %}}para los fines de <span class='field'>{{{{ purpose }}}}</span>{{% else %}}para los fines que el(la) interesado(a) considere convenientes{{% endif %}},
  sin que por ello se genere responsabilidad alguna para la empresa.</p>
  <div style='margin-top:80px; text-align:center;'>
    <div style='display:inline-block; width:250px; border-top: 1px solid #333; padding-top: 8px;'>
      <strong>{{{{ Company.LegalName }}}}</strong><br><small>Representante Legal</small>
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
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-badge'>COTIZACIÓN</div>
  </div>
  <table class='meta-table'>
    <tr><td class='meta-label'>No.:</td><td class='meta-value'>{{{{ quote_number }}}}</td></tr>
    <tr><td class='meta-label'>Fecha:</td><td class='meta-value'>{{{{ quote_date }}}}</td></tr>
    <tr><td class='meta-label'>Cliente:</td><td class='meta-value'>{{{{ client_name }}}}</td></tr>
    {{% if payment_terms != blank %}}<tr><td class='meta-label'>Pago:</td><td class='meta-value'>{{{{ payment_terms }}}}</td></tr>{{% endif %}}
  </table>
  <table class='items'>
    <thead><tr><th>Descripción</th><th style='text-align:center;'>Cant.</th><th style='text-align:right;'>Precio Unit.</th><th style='text-align:right;'>Subtotal</th></tr></thead>
    <tbody><tr><td colspan='4' style='text-align:center; color:#888; padding:20px;'><em>Los items se cargan dinámicamente desde la cotización del sistema.</em></td></tr></tbody>
  </table>
  <div class='totals'>
    <div>Subtotal: <strong>{{{{ Sale.Subtotal | default: '0.00' }}}}</strong></div>
    <div>{{{{ Country.VatLabel | default: 'Impuesto' }}}}: <strong>{{{{ Sale.Tax | default: '0.00' }}}}</strong></div>
    <div>Descuento: <strong>{{{{ Sale.Discount | default: '0.00' }}}}</strong></div>
    <div class='totals-grand'>TOTAL: <strong>{{{{ Sale.Total | default: '0.00' }}}}</strong></div>
  </div>
  {{% if notes != blank %}}<p style='margin-top:20px; padding:12px; background:#f0f0f0; border-radius:6px;'><strong>Observaciones:</strong> {{{{ notes }}}}</p>{{% endif %}}
  <p style='margin-top:20px; font-size:13px;'>{{% if validity_days != blank %}}Vigencia: {{{{ validity_days }}}} días.{{% else %}}Vigencia: 15 días.{{% endif %}}</p>
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
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-title'>NOTA DE ENTREGA</div>
  </div>
  <div class='meta-box'>
    <div class='meta-card'><small>Fecha de Entrega</small><strong>{{{{ delivery_date }}}}</strong></div>
    <div class='meta-card'><small>Cliente</small><strong>{{{{ client_name }}}}</strong></div>
    {{% if invoice_ref != blank %}}<div class='meta-card'><small>Ref. Factura</small><strong>{{{{ invoice_ref }}}}</strong></div>{{% endif %}}
  </div>
  <table class='items'>
    <thead><tr><th>Descripción</th><th style='text-align:center;'>Cantidad</th><th style='text-align:left;'>Observaciones</th></tr></thead>
    <tbody><tr><td colspan='3' style='text-align:center; color:#888; padding:20px;'><em>Los items se cargan desde la orden de entrega del sistema.</em></td></tr></tbody>
  </table>
  {{% if carrier != blank %}}<p><strong>Transportista:</strong> {{{{ carrier }}}}</p>{{% endif %}}
  <p><strong>Recibido por:</strong> {{{{ received_by | default: '_________________________' }}}}</p>
  <p style='margin-top:20px; font-size:13px;'>Al firmar esta nota, el receptor confirma la correcta recepción de la mercancía descrita.</p>
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
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-title'>PODER NOTARIAL GENERAL</div>
  </div>
  <p class='body-text'>En {{{{ Contract.City }}}}, {{{{ Country.Name }}}}, comparece ante mi:</p>
  <p class='body-text'>El(la) señor(a) <span class='field'>{{{{ grantor_name }}}}</span>, identificado(a) con {{{{ Country.IdDocument }}}} No. <span class='field'>{{{{ grantor_id }}}}</span>,
  en adelante denominado(a) <em>&quot;EL OTORGANTE&quot;</em>, quien por su libre y espontánea voluntad, otorga el presente PODER GENERAL a favor de:</p>
  <p class='body-text'>El(la) señor(a) <span class='field'>{{{{ attorney_name }}}}</span>, identificado(a) con {{{{ Country.IdDocument }}}} No. <span class='field'>{{{{ attorney_id }}}}</span>,
  en adelante denominado(a) <em>&quot;EL APODERADO&quot;</em>, a quien EL OTORGANTE designa como su representante legal con las siguientes facultades:</p>
  <div class='article'>
    <p><strong>PRIMERA - FACULTADES:</strong> EL APODERADO queda facultado para:</p>
    <p class='body-text'>{{{{ powers }}}}</p>
  </div>
  {{% if scope != blank %}}<div class='article'><p><strong>SEGUNDA - ALCANCE:</strong> {{{{ scope }}}}</p></div>{{% endif %}}
  <div class='article'>
    <p><strong>{{% if scope != blank %}}TERCERA{{% else %}}SEGUNDA{{% endif %}} - REVOCACIÓN:</strong> El presente podrá ser revocado en cualquier momento por EL OTORGANTE.</p>
  </div>
  <div class='article'>
    <p><strong>{{% if scope != blank %}}CUARTA{{% else %}}TERCERA{{% endif %}} - VIGENCIA:</strong> El presente entrará en vigor a partir de su firma y tendrá validez mientras no sea expresamente revocado.</p>
  </div>
  <p class='closing'>En señal de conformidad, se firma el presente documento.</p>
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

    /// <summary>
    /// Liquidación final específica de Nicaragua (Código del Trabajo arts. 93/78/45,
    /// Ley 822 -IR-, INSS 7%/21.5%, INATEC 2%). Los montos los calcula quien
    /// elabora el documento (nómina) y se pasan como variables; la plantilla
    /// presenta y estructura profesionalmente el finiquito.
    /// </summary>
    private static string BuildLiquidationFinalHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .liq-grid {{ width: 100%; margin-bottom: 12px; }}
    .liq-grid td {{ padding: 3px 8px 3px 0; font-size: 12px; vertical-align: top; }}
    .liq-label {{ color: #666; width: 150px; }}
    .liq-value {{ font-weight: 600; color: #1A0A3E; }}
    .salary-box {{ background: #f5f3ff; border-left: 3px solid #7C4DFF; border-radius: 6px; padding: 10px 14px; margin: 12px 0; }}
    .salary-box table {{ width: 100%; }}
    .salary-box td {{ padding: 2px 0; font-size: 12px; }}
    .section-title {{ background: #1A0A3E; color: #fff; padding: 5px 12px; font-weight: 700; font-size: 12px; letter-spacing: 1px; margin-top: 16px; text-transform: uppercase; }}
    .totals-row {{ text-align: right; font-weight: 700; font-size: 13px; margin-top: 8px; }}
    .net-box {{ text-align: right; font-size: 16px; font-weight: 700; color: #1A0A3E; border: 2px solid #7C4DFF; border-radius: 6px; padding: 8px 16px; margin-top: 14px; }}
    .amount-words {{ margin-top: 8px; font-size: 12px; }}
    .legal-text {{ text-align: justify; font-size: 11.5px; margin-top: 16px; line-height: 1.7; }}
    .sig-grid {{ display: flex; justify-content: space-between; margin-top: 70px; gap: 12px; }}
    .sig-block {{ text-align: center; width: 23%; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-title'>LIQUIDACIÓN FINAL</div>
    <div style='font-size:11px; color:#666;'>{{{{ Contract.City }}}}, {{{{ Company.Date }}}} · Código del Trabajo de {{{{ Country.Name }}}} (Arts. 93, 78 y 45) · Ley 822 (IR) · INSS · INATEC</div>
  </div>

  <table class='liq-grid'>
    <tr>
      <td class='liq-label'>Fecha de Elaboración</td>
      <td class='liq-value'>{{% if fecha_elaboracion != blank %}}{{{{ fecha_elaboracion }}}}{{% else %}}{{{{ Company.Date }}}}{{% endif %}}</td>
      <td class='liq-label'>Fecha de Ingreso</td>
      <td class='liq-value'>{{{{ fecha_ingreso }}}}</td>
    </tr>
    <tr>
      <td class='liq-label'>Nombre del Trabajador</td>
      <td class='liq-value'>{{{{ employee_name }}}}</td>
      <td class='liq-label'>Fecha de Salida</td>
      <td class='liq-value'>{{{{ fecha_salida }}}}</td>
    </tr>
    <tr>
      <td class='liq-label'>Cédula de Identidad</td>
      <td class='liq-value'>{{{{ employee_id }}}}</td>
      <td class='liq-label'>Motivo de Retiro</td>
      <td class='liq-value'>{{{{ retiro_reason | default: '____________' }}}}</td>
    </tr>
    <tr>
      <td class='liq-label'>Tipo de Contrato</td>
      <td class='liq-value'>{{{{ contract_type | default: 'Tiempo Indefinido' }}}}</td>
      <td class='liq-label'>Antigüedad</td>
      <td class='liq-value'>{{{{ seniority | default: '____________' }}}}</td>
    </tr>
    <tr>
      <td class='liq-label'>Cargo Desempeñado</td>
      <td class='liq-value'>{{{{ position | default: '____________' }}}}</td>
      <td></td>
      <td></td>
    </tr>
  </table>

  <div class='salary-box'>
    <table>
      <tr>
        <td class='liq-label'>Salario Ordinario Mensual</td>
        <td class='liq-value'>{{{{ Country.Currency }}}} {{{{ salario_mensual | default: '0.00' }}}}</td>
        <td class='liq-label'>Sueldo Diario</td>
        <td class='liq-value'>{{{{ Country.Currency }}}} {{{{ sueldo_diario | default: '-' }}}}</td>
        <td class='liq-label'>Sueldo por Hora</td>
        <td class='liq-value'>{{{{ Country.Currency }}}} {{{{ sueldo_por_hora | default: '-' }}}}</td>
      </tr>
      <tr>
        <td class='liq-label'>Salario Promedio (6 meses)</td>
        <td class='liq-value'>{{{{ Country.Currency }}}} {{{{ salario_promedio | default: '-' }}}}</td>
        <td class='liq-label'>Salario Más Alto (6 meses)</td>
        <td class='liq-value'>{{{{ Country.Currency }}}} {{{{ salario_mas_alto | default: '-' }}}}</td>
        <td class='liq-label'>Valor de una Hora Extra</td>
        <td class='liq-value'>{{{{ Country.Currency }}}} {{{{ hora_extra_valor | default: '-' }}}}</td>
      </tr>
    </table>
  </div>

  <div class='section-title'>Ingresos</div>
  <table class='items'>
    <thead><tr><th>Concepto</th><th style='text-align:right;'>Monto en {{{{ Country.Currency }}}}</th></tr></thead>
    <tbody>
      <tr><td>Sueldo pendiente de pago</td><td style='text-align:right;'>{{{{ sueldo_pendiente | default: '-' }}}}</td></tr>
      <tr><td>Horas extras</td><td style='text-align:right;'>{{{{ horas_extras | default: '-' }}}}</td></tr>
      <tr><td>{{{{ otros_conceptos | default: 'Otros conceptos' }}}}</td><td style='text-align:right;'>{{{{ otros_monto | default: '-' }}}}</td></tr>
    </tbody>
  </table>

  <div class='section-title'>Prestaciones Sociales</div>
  <table class='items'>
    <thead><tr><th>Concepto</th><th>Periodo (DEL - AL)</th><th style='text-align:center;'>Días</th><th style='text-align:right;'>Monto</th></tr></thead>
    <tbody>
      <tr>
        <td><strong>Aguinaldo</strong><br /><small>Art. 93 Código del Trabajo</small></td>
        <td>{{{{ periodo_aguinaldo | default: '-' }}}}</td>
        <td style='text-align:center;'>{{{{ aguinaldo_dias | default: '-' }}}}</td>
        <td style='text-align:right;'>{{{{ aguinaldo_monto | default: '-' }}}}</td>
      </tr>
      <tr>
        <td><strong>Vacaciones</strong><br /><small>Art. 78 Código del Trabajo</small></td>
        <td>{{{{ periodo_vacaciones | default: '-' }}}}</td>
        <td style='text-align:center;'>{{{{ vacaciones_dias | default: '-' }}}} <small>(deducción: {{{{ vac_deducciones | default: '0' }}}})</small></td>
        <td style='text-align:right;'>{{{{ vacaciones_monto | default: '-' }}}}</td>
      </tr>
      <tr>
        <td><strong>Indemnización por Antigüedad</strong><br /><small>Art. 45 Código del Trabajo</small></td>
        <td>{{{{ periodo_indem | default: '-' }}}}</td>
        <td style='text-align:center;'>-</td>
        <td style='text-align:right;'>{{{{ indemnizacion_monto | default: '-' }}}}</td>
      </tr>
    </tbody>
  </table>
  <div class='totals-row'>TOTAL INGRESOS: {{{{ Country.Currency }}}} {{{{ total_ingresos | default: '0.00' }}}}</div>

  <div class='section-title'>Deducciones - Trabajador</div>
  <table class='items'>
    <thead><tr><th>Concepto</th><th>Referencia legal</th><th style='text-align:right;'>Monto</th></tr></thead>
    <tbody>
      <tr><td>INSS Laboral</td><td>7.00% sobre el salario</td><td style='text-align:right;'>{{{{ inss_laboral | default: '-' }}}}</td></tr>
      <tr><td>Impuesto sobre la Renta (IR)</td><td>Salario ordinario - Ley 822</td><td style='text-align:right;'>{{{{ ir_ord | default: '-' }}}}</td></tr>
      <tr><td>Impuesto sobre la Renta (IR)</td><td>Pagos ocasionales: vacaciones, bonos o incentivos (Art. 19.2 del Reglamento de la Ley 822)</td><td style='text-align:right;'>{{{{ ir_ocasional | default: '-' }}}}</td></tr>
      <tr><td>Impuesto sobre la Renta (IR)</td><td>Retención definitiva por indemnización (Arts. 19.3 y 24.1 de la Ley 822 y 15.3 de su Reglamento)</td><td style='text-align:right;'>{{{{ ir_indemnizacion | default: '-' }}}}</td></tr>
      <tr><td>Saldo de préstamo</td><td>-</td><td style='text-align:right;'>{{{{ prestamo | default: '-' }}}}</td></tr>
      <tr><td>Aguinaldo pagado</td><td>Anticipo del Art. 93 CT</td><td style='text-align:right;'>{{{{ aguinaldo_pagado | default: '-' }}}}</td></tr>
    </tbody>
  </table>
  <div class='totals-row'>TOTAL DEDUCCIONES: {{{{ Country.Currency }}}} {{{{ total_deducciones | default: '0.00' }}}}</div>

  <div class='section-title'>Empleador - Aportes de Referencia</div>
  <table class='items'>
    <thead><tr><th>Aporte</th><th>Tasa</th><th style='text-align:right;'>Monto</th></tr></thead>
    <tbody>
      <tr><td>INSS Patronal</td><td>21.50%</td><td style='text-align:right;'>{{{{ inss_patronal | default: '-' }}}}</td></tr>
      <tr><td>INATEC</td><td>2.00%</td><td style='text-align:right;'>{{{{ inatec | default: '-' }}}}</td></tr>
    </tbody>
  </table>

  <div class='net-box'>NETO A RECIBIR: {{{{ Country.Currency }}}} {{{{ neto_a_recibir | default: '0.00' }}}}</div>
  <div class='amount-words'><strong>Cantidad en letras:</strong> {{{{ neto_en_letras | default: '________________________________________' }}}}</div>

  <p class='legal-text'>Por este medio hago constar que he recibido de {{{{ Company.LegalName }}}} la cantidad de {{{{ neto_en_letras | default: '____________' }}}}, a mi entera satisfacción, en pago total de mi liquidación final, a la que tengo derecho conforme a la legislación de {{{{ Country.Name }}}}. Eximo a {{{{ Company.LegalName }}}} de cualquier reclamo posterior derivado de la relación laboral. Finiquitando el vínculo laboral entre Empleador y Trabajador al firmar la presente liquidación y otorgando el más amplio y total finiquito a favor.</p>

  <div class='sig-grid'>
    <div class='sig-block'><div class='sig-line'>Elaborado por</div><small>{{{{ elaborado_por | default: '____________' }}}}</small></div>
    <div class='sig-block'><div class='sig-line'>Revisado por</div><small>{{{{ revisado_por | default: '____________' }}}}</small></div>
    <div class='sig-block'><div class='sig-line'>Recibí conforme</div><small>{{{{ employee_name }}}}</small><br /><small>{{{{ employee_id }}}}</small></div>
    <div class='sig-block'><div class='sig-line'>Autorizado por</div><small>{{{{ autorizado_por | default: '____________' }}}}</small></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Company.Date }}}}</div>
</body></html>";
}
