using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zorvian.Core.Entities;

namespace Zorvian.Infrastructure.Data;

/// <summary>
/// Seeds 11 professional document templates (SYSTEM tenant): upgrades outdated
/// versions and inserts any seeded template missing from the database, so new
/// templates reach existing installations on startup.
/// </summary>
public static class DocumentTemplateSeeder
{
    public static async Task SeedAsync(ZorvianDbContext db, ILogger logger)
    {
        await UpgradeSeededTemplatesAsync(db, logger);
        await InsertMissingSeededTemplatesAsync(db, logger);
    }

    /// <summary>
    /// Inserta las plantillas sembradas cuyo nombre (normalizado) no exista aún,
    /// idempotente. Así las plantillas nuevas (p. ej. Factura, Guía de Despacho,
    /// Nota de Crédito, Orden de Compra) llegan a instalaciones existentes en el
    /// próximo arranque sin depender de que la tabla esté vacía.
    /// </summary>
    private static async Task InsertMissingSeededTemplatesAsync(ZorvianDbContext db, ILogger logger)
    {
        var fresh = BuildTemplates(DateTime.UtcNow);

        var existingNames = (await db.DocumentTemplates.IgnoreQueryFilters()
                .Select(t => t.Name)
                .ToListAsync())
            .Select(NormalizeTemplateName)
            .ToHashSet();

        var missing = fresh
            .Where(t => !existingNames.Contains(NormalizeTemplateName(t.Name)))
            .ToList();

        if (missing.Count == 0)
            return;

        await db.DocumentTemplates.AddRangeAsync(missing);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Seeded {Count} missing document template(s): {Names}.",
            missing.Count,
            string.Join(", ", missing.Select(m => m.Name)));
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

        // ── Sales: Factura (electrónica comercial) ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Factura",
            Category = "Sales",
            Module = "Sale",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"client_name\",\"label\":\"Nombre del Cliente\",\"type\":\"text\",\"required\":true},{\"key\":\"client_id\",\"label\":\"No. Identificación Cliente\",\"type\":\"text\",\"required\":false},{\"key\":\"client_address\",\"label\":\"Dirección del Cliente\",\"type\":\"text\",\"required\":false},{\"key\":\"invoice_number\",\"label\":\"No. Factura\",\"type\":\"text\",\"required\":true},{\"key\":\"invoice_date\",\"label\":\"Fecha de Emisión\",\"type\":\"date\",\"required\":true},{\"key\":\"payment_terms\",\"label\":\"Condiciones de Pago\",\"type\":\"text\",\"required\":false},{\"key\":\"notes\",\"label\":\"Observaciones\",\"type\":\"textarea\",\"required\":false}]",
            Content = BuildInvoiceHtml()
        });

        // ── Sales: Guía de Despacho ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Guía de Despacho",
            Category = "Sales",
            Module = "Sale",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"client_name\",\"label\":\"Nombre del Cliente\",\"type\":\"text\",\"required\":true},{\"key\":\"client_address\",\"label\":\"Dirección de Entrega\",\"type\":\"text\",\"required\":false},{\"key\":\"guide_number\",\"label\":\"No. Guía\",\"type\":\"text\",\"required\":true},{\"key\":\"delivery_date\",\"label\":\"Fecha de Entrega\",\"type\":\"date\",\"required\":true},{\"key\":\"invoice_ref\",\"label\":\"Ref. Factura\",\"type\":\"text\",\"required\":false},{\"key\":\"carrier\",\"label\":\"Transportista\",\"type\":\"text\",\"required\":false},{\"key\":\"received_by\",\"label\":\"Recibido Por\",\"type\":\"text\",\"required\":false}]",
            Content = BuildDeliveryGuideHtml()
        });

        // ── Sales: Guía de Remisión / Traslado ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Guía de Remisión",
            Category = "Sales",
            Module = "Sale",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"guide_number\",\"label\":\"No. Guía de Remisión\",\"type\":\"text\",\"required\":true},{\"key\":\"delivery_date\",\"label\":\"Fecha de Traslado\",\"type\":\"date\",\"required\":true},{\"key\":\"client_name\",\"label\":\"Nombre del Cliente\",\"type\":\"text\",\"required\":true},{\"key\":\"client_id\",\"label\":\"No. Identificación Cliente\",\"type\":\"text\",\"required\":false},{\"key\":\"client_address\",\"label\":\"Dirección de Destino\",\"type\":\"text\",\"required\":false},{\"key\":\"invoice_ref\",\"label\":\"Ref. Factura\",\"type\":\"text\",\"required\":false},{\"key\":\"remission_reason\",\"label\":\"Motivo del Traslado\",\"type\":\"text\",\"required\":false},{\"key\":\"carrier\",\"label\":\"Transportista\",\"type\":\"text\",\"required\":false}]",
            Content = BuildRemissionGuideHtml()
        });

        // ── Sales: Nota de Crédito ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Nota de Crédito",
            Category = "Sales",
            Module = "Sale",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"credit_note_number\",\"label\":\"No. Nota de Crédito\",\"type\":\"text\",\"required\":true},{\"key\":\"issue_date\",\"label\":\"Fecha de Emisión\",\"type\":\"date\",\"required\":true},{\"key\":\"invoice_ref\",\"label\":\"Ref. Factura\",\"type\":\"text\",\"required\":true},{\"key\":\"client_name\",\"label\":\"Nombre del Cliente\",\"type\":\"text\",\"required\":true},{\"key\":\"reason\",\"label\":\"Motivo\",\"type\":\"textarea\",\"required\":true}]",
            Content = BuildCreditNoteHtml()
        });

        // ── Purchases: Orden de Compra ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Orden de Compra",
            Category = "Purchases",
            Module = "Purchase",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"order_number\",\"label\":\"No. Orden de Compra\",\"type\":\"text\",\"required\":true},{\"key\":\"order_date\",\"label\":\"Fecha de Orden\",\"type\":\"date\",\"required\":true},{\"key\":\"expected_date\",\"label\":\"Fecha Esperada de Entrega\",\"type\":\"date\",\"required\":false},{\"key\":\"supplier_name\",\"label\":\"Proveedor\",\"type\":\"text\",\"required\":true},{\"key\":\"supplier_id\",\"label\":\"No. Identificación Proveedor\",\"type\":\"text\",\"required\":false},{\"key\":\"supplier_address\",\"label\":\"Dirección Proveedor\",\"type\":\"text\",\"required\":false},{\"key\":\"payment_terms\",\"label\":\"Condiciones de Pago\",\"type\":\"text\",\"required\":false},{\"key\":\"notes\",\"label\":\"Notas\",\"type\":\"textarea\",\"required\":false}]",
            Content = BuildPurchaseOrderHtml()
        });

        // ── Purchases: Acta de Recepción de Compras ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Acta de Recepción de Compras",
            Category = "Purchases",
            Module = "Purchase",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"acta_number\",\"label\":\"No. Acta\",\"type\":\"text\",\"required\":true},{\"key\":\"acta_date\",\"label\":\"Fecha de Recepción\",\"type\":\"date\",\"required\":true},{\"key\":\"supplier_name\",\"label\":\"Proveedor\",\"type\":\"text\",\"required\":true},{\"key\":\"supplier_id\",\"label\":\"No. Identificación Proveedor\",\"type\":\"text\",\"required\":false},{\"key\":\"order_ref\",\"label\":\"Ref. Orden de Compra\",\"type\":\"text\",\"required\":false},{\"key\":\"invoice_ref\",\"label\":\"Ref. Factura Proveedor\",\"type\":\"text\",\"required\":false},{\"key\":\"received_by\",\"label\":\"Recibido Por\",\"type\":\"text\",\"required\":false},{\"key\":\"notes\",\"label\":\"Observaciones\",\"type\":\"textarea\",\"required\":false}]",
            Content = BuildReceivingReportHtml()
        });

        // ── Finance: Recibo de Pago (Cliente) ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Recibo de Pago (Cliente)",
            Category = "Finance",
            Module = "Credit",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"receipt_number\",\"label\":\"No. Recibo\",\"type\":\"text\",\"required\":true},{\"key\":\"payment_date\",\"label\":\"Fecha de Pago\",\"type\":\"date\",\"required\":true},{\"key\":\"client_name\",\"label\":\"Cliente\",\"type\":\"text\",\"required\":true},{\"key\":\"credit_number\",\"label\":\"No. Crédito\",\"type\":\"text\",\"required\":false},{\"key\":\"payment_method\",\"label\":\"Método de Pago\",\"type\":\"text\",\"required\":false},{\"key\":\"payment_amount\",\"label\":\"Monto Pagado\",\"type\":\"number\",\"required\":true},{\"key\":\"credit_balance\",\"label\":\"Saldo Restante\",\"type\":\"number\",\"required\":false}]",
            Content = BuildClientPaymentReceiptHtml()
        });

        // ── Treasury: Comprobante de Caja (Ingreso/Egreso) ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Comprobante de Caja",
            Category = "Treasury",
            Module = "Treasury",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"movement_title\",\"label\":\"Tipo (Ingreso/Egreso)\",\"type\":\"text\",\"required\":true},{\"key\":\"movement_date\",\"label\":\"Fecha\",\"type\":\"date\",\"required\":true},{\"key\":\"concept\",\"label\":\"Concepto\",\"type\":\"text\",\"required\":true},{\"key\":\"amount\",\"label\":\"Monto\",\"type\":\"number\",\"required\":true},{\"key\":\"register_code\",\"label\":\"Caja\",\"type\":\"text\",\"required\":false},{\"key\":\"reference\",\"label\":\"Referencia\",\"type\":\"text\",\"required\":false},{\"key\":\"employee_name\",\"label\":\"Responsable\",\"type\":\"text\",\"required\":false}]",
            Content = BuildCashVoucherHtml()
        });

        // ── Fleet: Orden de Trabajo ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Orden de Trabajo",
            Category = "Fleet",
            Module = "Fleet",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"wo_number\",\"label\":\"No. Orden de Trabajo\",\"type\":\"text\",\"required\":true},{\"key\":\"report_date\",\"label\":\"Fecha de Reporte\",\"type\":\"date\",\"required\":true},{\"key\":\"vehicle_plate\",\"label\":\"Placa\",\"type\":\"text\",\"required\":true},{\"key\":\"vehicle_desc\",\"label\":\"Vehículo\",\"type\":\"text\",\"required\":false},{\"key\":\"driver_name\",\"label\":\"Conductor\",\"type\":\"text\",\"required\":false},{\"key\":\"problem_description\",\"label\":\"Problema Reportado\",\"type\":\"textarea\",\"required\":true},{\"key\":\"diagnosis\",\"label\":\"Diagnóstico\",\"type\":\"textarea\",\"required\":false},{\"key\":\"solution_applied\",\"label\":\"Solución Aplicada\",\"type\":\"textarea\",\"required\":false},{\"key\":\"priority\",\"label\":\"Prioridad\",\"type\":\"text\",\"required\":false},{\"key\":\"workshop_name\",\"label\":\"Taller\",\"type\":\"text\",\"required\":false},{\"key\":\"mechanic\",\"label\":\"Mecánico Responsable\",\"type\":\"text\",\"required\":false},{\"key\":\"cost_total\",\"label\":\"Costo Total\",\"type\":\"number\",\"required\":false}]",
            Content = BuildWorkOrderHtml()
        });

        // ── Fleet: Acta de Recepción (Taller) ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Acta de Recepción (Taller)",
            Category = "Fleet",
            Module = "Fleet",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"acta_number\",\"label\":\"No. Acta\",\"type\":\"text\",\"required\":true},{\"key\":\"report_date\",\"label\":\"Fecha de Recepción\",\"type\":\"date\",\"required\":true},{\"key\":\"workshop_name\",\"label\":\"Taller\",\"type\":\"text\",\"required\":false},{\"key\":\"workshop_address\",\"label\":\"Dirección del Taller\",\"type\":\"text\",\"required\":false},{\"key\":\"workshop_phone\",\"label\":\"Teléfono del Taller\",\"type\":\"text\",\"required\":false},{\"key\":\"vehicle_plate\",\"label\":\"Placa\",\"type\":\"text\",\"required\":true},{\"key\":\"vehicle_desc\",\"label\":\"Vehículo\",\"type\":\"text\",\"required\":false},{\"key\":\"driver_name\",\"label\":\"Conductor Responsable\",\"type\":\"text\",\"required\":false},{\"key\":\"problem_description\",\"label\":\"Problema Reportado\",\"type\":\"textarea\",\"required\":true},{\"key\":\"priority\",\"label\":\"Prioridad\",\"type\":\"text\",\"required\":false},{\"key\":\"cost_estimate\",\"label\":\"Costo Estimado\",\"type\":\"number\",\"required\":false},{\"key\":\"notes\",\"label\":\"Observaciones\",\"type\":\"textarea\",\"required\":false}]",
            Content = BuildWorkshopReceiptHtml()
        });

        // ── Purchases: Recibo de Pago (Proveedor) ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Recibo de Pago (Proveedor)",
            Category = "Purchases",
            Module = "Purchase",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"receipt_number\",\"label\":\"No. Recibo\",\"type\":\"text\",\"required\":true},{\"key\":\"payment_date\",\"label\":\"Fecha de Pago\",\"type\":\"date\",\"required\":true},{\"key\":\"supplier_name\",\"label\":\"Proveedor\",\"type\":\"text\",\"required\":true},{\"key\":\"purchase_ref\",\"label\":\"Ref. Compra\",\"type\":\"text\",\"required\":false},{\"key\":\"payment_method\",\"label\":\"Método de Pago\",\"type\":\"text\",\"required\":false},{\"key\":\"payment_amount\",\"label\":\"Monto Pagado\",\"type\":\"number\",\"required\":true},{\"key\":\"purchase_balance\",\"label\":\"Saldo Pendiente\",\"type\":\"number\",\"required\":false},{\"key\":\"notes\",\"label\":\"Notas\",\"type\":\"textarea\",\"required\":false}]",
            Content = BuildSupplierPaymentReceiptHtml()
        });

        // ── Warranty: Certificado de Garantía ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Certificado de Garantía",
            Category = "Warranty",
            Module = "Warranty",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"warranty_number\",\"label\":\"No. Garantía\",\"type\":\"text\",\"required\":true},{\"key\":\"client_name\",\"label\":\"Cliente\",\"type\":\"text\",\"required\":true},{\"key\":\"product_name\",\"label\":\"Producto\",\"type\":\"text\",\"required\":true},{\"key\":\"serial_number\",\"label\":\"No. Serie\",\"type\":\"text\",\"required\":false},{\"key\":\"start_date\",\"label\":\"Fecha de Inicio\",\"type\":\"date\",\"required\":true},{\"key\":\"end_date\",\"label\":\"Fecha de Vencimiento\",\"type\":\"date\",\"required\":true},{\"key\":\"duration_months\",\"label\":\"Duración (meses)\",\"type\":\"number\",\"required\":true},{\"key\":\"terms\",\"label\":\"Condiciones\",\"type\":\"textarea\",\"required\":false}]",
            Content = BuildWarrantyCertificateHtml()
        });

        // ── Fleet: Hoja de Ruta / Entrega ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Hoja de Ruta / Entrega",
            Category = "Fleet",
            Module = "Fleet",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"delivery_code\",\"label\":\"Código de Entrega\",\"type\":\"text\",\"required\":true},{\"key\":\"scheduled_date\",\"label\":\"Fecha Programada\",\"type\":\"date\",\"required\":true},{\"key\":\"time_window\",\"label\":\"Franja Horaria\",\"type\":\"text\",\"required\":false},{\"key\":\"client_name\",\"label\":\"Cliente\",\"type\":\"text\",\"required\":false},{\"key\":\"delivery_address\",\"label\":\"Dirección de Entrega\",\"type\":\"text\",\"required\":true},{\"key\":\"driver_name\",\"label\":\"Conductor\",\"type\":\"text\",\"required\":false},{\"key\":\"vehicle_plate\",\"label\":\"Placa\",\"type\":\"text\",\"required\":false},{\"key\":\"route_name\",\"label\":\"Ruta\",\"type\":\"text\",\"required\":false},{\"key\":\"receiver_name\",\"label\":\"Recibido Por\",\"type\":\"text\",\"required\":false},{\"key\":\"observations\",\"label\":\"Observaciones\",\"type\":\"textarea\",\"required\":false}]",
            Content = BuildDeliveryRouteSheetHtml()
        });

        // ── Finance: Ficha de Activo Fijo ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Ficha de Activo Fijo",
            Category = "Accounting",
            Module = "Finance",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"asset_code\",\"label\":\"Código\",\"type\":\"text\",\"required\":true},{\"key\":\"asset_name\",\"label\":\"Nombre\",\"type\":\"text\",\"required\":true},{\"key\":\"acquisition_date\",\"label\":\"Fecha de Adquisición\",\"type\":\"date\",\"required\":true},{\"key\":\"acquisition_cost\",\"label\":\"Costo de Adquisición\",\"type\":\"number\",\"required\":true},{\"key\":\"supplier_name\",\"label\":\"Proveedor\",\"type\":\"text\",\"required\":false},{\"key\":\"useful_life_years\",\"label\":\"Vida Útil (años)\",\"type\":\"number\",\"required\":true},{\"key\":\"residual_value\",\"label\":\"Valor Residual\",\"type\":\"number\",\"required\":false},{\"key\":\"depreciation_method\",\"label\":\"Método de Depreciación\",\"type\":\"text\",\"required\":false},{\"key\":\"assigned_to\",\"label\":\"Responsable\",\"type\":\"text\",\"required\":false},{\"key\":\"status\",\"label\":\"Estado\",\"type\":\"text\",\"required\":false}]",
            Content = BuildFixedAssetCardHtml()
        });

        // ── Inventory: Kardex de Producto ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Kardex de Producto",
            Category = "Inventory",
            Module = "Inventory",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"product_code\",\"label\":\"Código\",\"type\":\"text\",\"required\":true},{\"key\":\"product_name\",\"label\":\"Producto\",\"type\":\"text\",\"required\":true},{\"key\":\"unit_of_measure\",\"label\":\"Unidad\",\"type\":\"text\",\"required\":false},{\"key\":\"current_stock\",\"label\":\"Stock Actual\",\"type\":\"number\",\"required\":false},{\"key\":\"period\",\"label\":\"Período\",\"type\":\"text\",\"required\":false},{\"key\":\"total_in_qty\",\"label\":\"Total Entradas\",\"type\":\"number\",\"required\":false},{\"key\":\"total_out_qty\",\"label\":\"Total Salidas\",\"type\":\"number\",\"required\":false},{\"key\":\"current_cost\",\"label\":\"Costo Actual\",\"type\":\"number\",\"required\":false}]",
            Content = BuildKardexHtml()
        });

        // ── Purchases: Solicitud de Cotización ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Solicitud de Cotización",
            Category = "Purchase",
            Module = "Purchase",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"request_number\",\"label\":\"No. Solicitud\",\"type\":\"text\",\"required\":true},{\"key\":\"request_date\",\"label\":\"Fecha\",\"type\":\"date\",\"required\":true},{\"key\":\"title\",\"label\":\"Título\",\"type\":\"text\",\"required\":true},{\"key\":\"priority\",\"label\":\"Prioridad\",\"type\":\"text\",\"required\":false},{\"key\":\"needed_by_date\",\"label\":\"Requerida Para\",\"type\":\"date\",\"required\":false},{\"key\":\"requested_by\",\"label\":\"Solicitante\",\"type\":\"text\",\"required\":false}]",
            Content = BuildPurchaseRequestHtml()
        });

        // ── Inventory: Transferencia entre Bodegas ──
        templates.Add(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedAt = now,
            CreatedBy = "system",
            Name = "Transferencia entre Bodegas",
            Category = "Inventory",
            Module = "Inventory",
            CountryCode = "ALL",
            IsActive = true,
            Version = "1.0",
            Variables = "[{\"key\":\"transfer_number\",\"label\":\"No. Transferencia\",\"type\":\"text\",\"required\":true},{\"key\":\"transfer_date\",\"label\":\"Fecha\",\"type\":\"date\",\"required\":true},{\"key\":\"from_branch\",\"label\":\"Sucursal Origen\",\"type\":\"text\",\"required\":true},{\"key\":\"to_branch\",\"label\":\"Sucursal Destino\",\"type\":\"text\",\"required\":true},{\"key\":\"dispatched_at\",\"label\":\"Despachada\",\"type\":\"text\",\"required\":false},{\"key\":\"received_at\",\"label\":\"Recibida\",\"type\":\"text\",\"required\":false}]",
            Content = BuildStockTransferHtml()
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
    <tbody>{{% for item in items %}}<tr><td>{{{{ item.description }}}}</td><td style='text-align:center;'>{{{{ item.quantity }}}}</td><td style='text-align:right;'>{{{{ item.unit_price }}}}</td><td style='text-align:right;'>{{{{ item.subtotal }}}}</td></tr>{{% else %}}<tr><td colspan='4' style='text-align:center; color:#888; padding:20px;'><em>Los items se cargan dinámicamente desde la cotización del sistema.</em></td></tr>{{% endfor %}}</tbody>
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

    private static string BuildInvoiceHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #1A0A3E; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .company-block {{ text-align: left; }}
    .meta-table {{ width: 100%; margin: 15px 0; }} .meta-table td {{ padding: 4px 0; }}
    .meta-label {{ color: #666; width: 140px; }} .meta-value {{ font-weight: 600; color: #1A0A3E; }}
    .totals {{ text-align: right; margin-top: 10px; }}
    .totals-grand {{ font-size: 18px; font-weight: 700; color: #1A0A3E; border-top: 2px solid #1A0A3E; padding-top: 8px; }}
    .fiscal-note {{ margin-top: 16px; font-size: 11px; color: #666; text-align: justify; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-block'>
      <div class='company-name'>{{{{ Company.LegalName }}}}</div>
      <div style='font-size:12px;color:#555;'>{{{{ Company.TaxIdLabel }}}} No. {{{{ Company.TaxId | default: '____________' }}}}</div>
      <div style='font-size:12px;color:#555;'>{{{{ Company.Address | default: '____________' }}}}</div>
      <div style='font-size:12px;color:#555;'>{{{{ Country.Name }}}}</div>
    </div>
    <div class='doc-badge'>FACTURA</div>
  </div>
  <table class='meta-table'>
    <tr><td class='meta-label'>No. Factura:</td><td class='meta-value'>{{{{ invoice_number }}}}</td></tr>
    <tr><td class='meta-label'>Fecha de Emisión:</td><td class='meta-value'>{{{{ invoice_date }}}}</td></tr>
    <tr><td class='meta-label'>Cliente:</td><td class='meta-value'>{{{{ client_name }}}}</td></tr>
    {{% if client_id != blank %}}<tr><td class='meta-label'>{{{{ Country.IdDocument }}}}:</td><td class='meta-value'>{{{{ client_id }}}}</td></tr>{{% endif %}}
    {{% if client_address != blank %}}<tr><td class='meta-label'>Dirección:</td><td class='meta-value'>{{{{ client_address }}}}</td></tr>{{% endif %}}
    {{% if payment_terms != blank %}}<tr><td class='meta-label'>Condiciones de Pago:</td><td class='meta-value'>{{{{ payment_terms }}}}</td></tr>{{% endif %}}
  </table>
  <table class='items'>
    <thead><tr><th>Descripción</th><th style='text-align:center;'>Cant.</th><th style='text-align:right;'>Precio Unit.</th><th style='text-align:right;'>Subtotal</th></tr></thead>
    <tbody>{{% for item in items %}}<tr><td>{{{{ item.description }}}}</td><td style='text-align:center;'>{{{{ item.quantity }}}}</td><td style='text-align:right;'>{{{{ item.unit_price }}}}</td><td style='text-align:right;'>{{{{ item.subtotal }}}}</td></tr>{{% else %}}<tr><td colspan='4' style='text-align:center; color:#888; padding:20px;'><em>Los items se cargan dinámicamente desde la venta del sistema.</em></td></tr>{{% endfor %}}</tbody>
  </table>
  <div class='totals'>
    <div>Subtotal: <strong>{{{{ Sale.Subtotal | default: '0.00' }}}}</strong></div>
    <div>Descuento: <strong>{{{{ Sale.Discount | default: '0.00' }}}}</strong></div>
    <div>{{{{ Country.VatLabel | default: 'Impuesto' }}}}: <strong>{{{{ Sale.Tax | default: '0.00' }}}}</strong></div>
    <div class='totals-grand'>TOTAL: {{{{ Country.Currency | default: '' }}}} <strong>{{{{ Sale.Total | default: '0.00' }}}}</strong></div>
  </div>
  {{% if notes != blank %}}<p style='margin-top:20px; padding:12px; background:#f0f0f0; border-radius:6px;'><strong>Observaciones:</strong> {{{{ notes }}}}</p>{{% endif %}}
  <p class='fiscal-note'>Documento fiscal válido sin necesidad de firma. Conservar para efectos de {{{{ Country.TaxAuthority | default: 'auditoría fiscal' }}}}. Emisor sujeto a la normativa tributaria de {{{{ Country.Name }}}}. Gracias por su compra.</p>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente</div>
</body></html>";

    private static string BuildDeliveryGuideHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #2EE59D; color: #1A0A3E; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 700; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; border-bottom: 3px solid #2EE59D; padding-bottom: 16px; }}
    .meta-box {{ display: flex; gap: 20px; margin: 15px 0; flex-wrap: wrap; }}
    .meta-card {{ flex: 1; min-width: 160px; background: #f0fdf4; padding: 12px 16px; border-radius: 8px; border-left: 3px solid #2EE59D; }}
    .meta-card small {{ color: #666; display: block; margin-bottom: 4px; }}
    .meta-card strong {{ color: #1A0A3E; }}
    .sig-area {{ display: flex; justify-content: space-between; margin-top: 80px; }}
    .sig-block {{ text-align: center; width: 30%; }}
  </style>
</head><body>
  <div class='header'>
    <div>
      <div class='company-name'>{{{{ Company.LegalName }}}}</div>
      <div style='font-size:12px;color:#555;'>{{{{ Company.TaxIdLabel }}}} No. {{{{ Company.TaxId | default: '____________' }}}}</div>
    </div>
    <div class='doc-badge'>GUÍA DE DESPACHO</div>
  </div>
  <div class='meta-box'>
    <div class='meta-card'><small>No. Guía</small><strong>{{{{ guide_number }}}}</strong></div>
    <div class='meta-card'><small>Fecha de Entrega</small><strong>{{{{ delivery_date }}}}</strong></div>
    <div class='meta-card'><small>Cliente</small><strong>{{{{ client_name }}}}</strong></div>
    {{% if invoice_ref != blank %}}<div class='meta-card'><small>Ref. Factura</small><strong>{{{{ invoice_ref }}}}</strong></div>{{% endif %}}
  </div>
  {{% if client_address != blank %}}<p><strong>Dirección de entrega:</strong> {{{{ client_address }}}}</p>{{% endif %}}
  <table class='items'>
    <thead><tr><th>Código</th><th>Descripción</th><th style='text-align:center;'>Cantidad</th><th style='text-align:right;'>Subtotal</th></tr></thead>
    <tbody>{{% for item in items %}}<tr><td>{{{{ item.code }}}}</td><td>{{{{ item.description }}}}</td><td style='text-align:center;'>{{{{ item.quantity }}}}</td><td style='text-align:right;'>{{{{ item.subtotal }}}}</td></tr>{{% else %}}<tr><td colspan='4' style='text-align:center; color:#888; padding:20px;'><em>Los items se cargan dinámicamente desde la venta del sistema.</em></td></tr>{{% endfor %}}</tbody>
  </table>
  {{% if carrier != blank %}}<p><strong>Transportista:</strong> {{{{ carrier }}}}</p>{{% endif %}}
  <p><strong>Recibido por:</strong> {{{{ received_by | default: '_________________________' }}}}</p>
  <p style='margin-top:20px; font-size:13px;'>Al firmar esta guía, el receptor confirma la correcta recepción de la mercancía descrita, en cantidad y estado.</p>
  <div class='sig-area'>
    <div class='sig-block'><div class='sig-line'>Entregado por</div></div>
    <div class='sig-block'><div class='sig-line'>Recibido por</div></div>
    <div class='sig-block'><div class='sig-line'>Vo.Bo. Administración</div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildRemissionGuideHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #7C4DFF; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 700; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; border-bottom: 3px solid #7C4DFF; padding-bottom: 16px; }}
    .meta-box {{ display: flex; gap: 20px; margin: 15px 0; flex-wrap: wrap; }}
    .meta-card {{ flex: 1; min-width: 150px; background: #f5f3ff; padding: 12px 16px; border-radius: 8px; border-left: 3px solid #7C4DFF; }}
    .meta-card small {{ color: #666; display: block; margin-bottom: 4px; }}
    .meta-card strong {{ color: #1A0A3E; }}
    .reason-box {{ margin: 14px 0; padding: 10px 16px; background: #f5f3ff; border-left: 4px solid #7C4DFF; border-radius: 6px; }}
    .route-box {{ display: flex; gap: 16px; margin: 16px 0; align-items: stretch; }}
    .route-point {{ flex: 1; background: #faf9ff; padding: 12px 16px; border-radius: 8px; border: 1px solid #e4dcff; }}
    .route-point small {{ color: #666; display: block; margin-bottom: 4px; font-weight: 600; letter-spacing: 1px; }}
    .route-arrow {{ align-self: center; font-size: 22px; color: #7C4DFF; font-weight: 700; }}
    .value-box {{ text-align: right; margin-top: 10px; font-size: 15px; }}
    .sig-area {{ display: flex; justify-content: space-between; margin-top: 70px; }}
    .sig-block {{ text-align: center; width: 30%; }}
  </style>
</head><body>
  <div class='header'>
    <div>
      <div class='company-name'>{{{{ Company.LegalName }}}}</div>
      <div style='font-size:12px;color:#555;'>{{{{ Company.TaxIdLabel }}}} No. {{{{ Company.TaxId | default: '____________' }}}}</div>
      <div style='font-size:12px;color:#555;'>{{{{ Company.Address | default: '____________' }}}}</div>
    </div>
    <div class='doc-badge'>GUÍA DE REMISIÓN</div>
  </div>
  <div class='meta-box'>
    <div class='meta-card'><small>No. Guía de Remisión</small><strong>{{{{ guide_number }}}}</strong></div>
    <div class='meta-card'><small>Fecha de Traslado</small><strong>{{{{ delivery_date }}}}</strong></div>
    <div class='meta-card'><small>Cliente</small><strong>{{{{ client_name }}}}</strong></div>
    {{% if invoice_ref != blank %}}<div class='meta-card'><small>Ref. Factura</small><strong>{{{{ invoice_ref }}}}</strong></div>{{% endif %}}
  </div>
  <div class='reason-box'><strong>Motivo del traslado:</strong> {{{{ remission_reason | default: 'Venta de mercadería' }}}}</div>
  <div class='route-box'>
    <div class='route-point'><small>ORIGEN</small><strong>{{{{ Company.Address | default: Contract.City | default: '____________' }}}}</strong><br /><span style='font-size:12px;color:#555;'>{{{{ Company.LegalName }}}}</span></div>
    <div class='route-arrow'>&rarr;</div>
    <div class='route-point'><small>DESTINO</small><strong>{{{{ client_address | default: '____________' }}}}</strong><br /><span style='font-size:12px;color:#555;'>{{{{ client_name }}}}</span></div>
  </div>
  <table class='items'>
    <thead><tr><th>Código</th><th>Descripción</th><th style='text-align:center;'>Cantidad</th><th style='text-align:right;'>Subtotal</th></tr></thead>
    <tbody>{{% for item in items %}}<tr><td>{{{{ item.code }}}}</td><td>{{{{ item.description }}}}</td><td style='text-align:center;'>{{{{ item.quantity }}}}</td><td style='text-align:right;'>{{{{ item.subtotal }}}}</td></tr>{{% else %}}<tr><td colspan='4' style='text-align:center; color:#888; padding:20px;'><em>Los items se cargan dinámicamente desde la venta del sistema.</em></td></tr>{{% endfor %}}</tbody>
  </table>
  <div class='value-box'><strong>Valor declarado:</strong> {{{{ Country.Currency | default: '' }}}} {{{{ Sale.Total | default: '0.00' }}}}</div>
  {{% if carrier != blank %}}<p><strong>Transportista:</strong> {{{{ carrier }}}}</p>{{% endif %}}
  {{% if client_id != blank %}}<p><strong>Identificación del cliente:</strong> {{{{ client_id }}}}</p>{{% endif %}}
  <p style='margin-top:14px; font-size:13px;'>Documento que acompaña la mercancía durante su traslado. Al firmar, el receptor confirma la recepción de los bienes descritos, en cantidad y estado.</p>
  <div class='sig-area'>
    <div class='sig-block'><div class='sig-line'>Entregado por</div></div>
    <div class='sig-block'><div class='sig-line'>Recibido por</div></div>
    <div class='sig-block'><div class='sig-line'>Transportista</div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildCreditNoteHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #b91c1c; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .meta-table {{ width: 100%; margin: 15px 0; }} .meta-table td {{ padding: 4px 0; }}
    .meta-label {{ color: #666; width: 160px; }} .meta-value {{ font-weight: 600; color: #1A0A3E; }}
    .totals {{ text-align: right; margin-top: 10px; }}
    .totals-grand {{ font-size: 18px; font-weight: 700; color: #b91c1c; border-top: 2px solid #b91c1c; padding-top: 8px; }}
    .reason-box {{ margin: 16px 0; padding: 12px 16px; background: #fef2f2; border-left: 4px solid #b91c1c; border-radius: 6px; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-badge'>NOTA DE CRÉDITO</div>
  </div>
  <table class='meta-table'>
    <tr><td class='meta-label'>No. Nota de Crédito:</td><td class='meta-value'>{{{{ credit_note_number }}}}</td></tr>
    <tr><td class='meta-label'>Fecha de Emisión:</td><td class='meta-value'>{{{{ issue_date }}}}</td></tr>
    <tr><td class='meta-label'>Ref. Factura:</td><td class='meta-value'>{{{{ invoice_ref }}}}</td></tr>
    <tr><td class='meta-label'>Cliente:</td><td class='meta-value'>{{{{ client_name }}}}</td></tr>
  </table>
  <div class='reason-box'><strong>Motivo:</strong> {{{{ reason | default: '____________' }}}}</div>
  <p style='text-align:justify;'>Por medio de la presente se emite la presente Nota de Crédito por los conceptos detallados a continuación, la cual aplica como ajuste a la factura de referencia arriba indicada.</p>
  <table class='items'>
    <thead><tr><th>Descripción</th><th style='text-align:center;'>Cant.</th><th style='text-align:right;'>Precio Unit.</th><th style='text-align:right;'>Subtotal</th></tr></thead>
    <tbody>{{% for item in items %}}<tr><td>{{{{ item.description }}}}</td><td style='text-align:center;'>{{{{ item.quantity }}}}</td><td style='text-align:right;'>{{{{ item.unit_price }}}}</td><td style='text-align:right;'>{{{{ item.subtotal }}}}</td></tr>{{% else %}}<tr><td colspan='4' style='text-align:center; color:#888; padding:20px;'><em>Los conceptos se cargan dinámicamente desde la nota de crédito del sistema.</em></td></tr>{{% endfor %}}</tbody>
  </table>
  <div class='totals'>
    <div>Subtotal: <strong>{{{{ Sale.Subtotal | default: '0.00' }}}}</strong></div>
    <div>{{{{ Country.VatLabel | default: 'Impuesto' }}}}: <strong>{{{{ Sale.Tax | default: '0.00' }}}}</strong></div>
    <div class='totals-grand'>TOTAL CRÉDITO: {{{{ Country.Currency | default: '' }}}} <strong>{{{{ Sale.Total | default: '0.00' }}}}</strong></div>
  </div>
  <div style='margin-top:70px; text-align:center;'>
    <div style='display:inline-block; width:250px; border-top: 1px solid #333; padding-top: 8px;'>
      <strong>{{{{ Company.LegalName }}}}</strong><br /><small>Representante Legal</small>
    </div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildPurchaseOrderHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #7C4DFF; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .parties {{ display: flex; gap: 20px; margin: 16px 0; }}
    .party-card {{ flex: 1; background: #f5f3ff; padding: 14px 18px; border-radius: 8px; border-left: 3px solid #7C4DFF; font-size: 13px; }}
    .party-card .party-title {{ font-weight: 700; color: #7C4DFF; text-transform: uppercase; font-size: 11px; letter-spacing: 1px; margin-bottom: 6px; }}
    .totals {{ text-align: right; margin-top: 10px; }}
    .totals-grand {{ font-size: 18px; font-weight: 700; color: #7C4DFF; border-top: 2px solid #1A0A3E; padding-top: 8px; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div style='text-align:right;'>
      <div class='doc-badge'>ORDEN DE COMPRA</div>
      {{% if order_status == 'approved' %}}<div style='margin-top:8px; display:inline-block; border:2px solid #2EE59D; color:#0f766e; background:#f0fdfa; padding:6px 18px; border-radius:6px; font-weight:800; letter-spacing:2px;'>APROBADA</div>{{% endif %}}
      {{% if order_status == 'partially_received' %}}<div style='margin-top:8px; display:inline-block; border:2px solid #FFD54F; color:#92400e; background:#fffbeb; padding:6px 18px; border-radius:6px; font-weight:800; letter-spacing:2px;'>RECIBIDA PARCIALMENTE</div>{{% endif %}}
      {{% if order_status == 'completed' %}}<div style='margin-top:8px; display:inline-block; border:2px solid #1A0A3E; color:#1A0A3E; background:#f5f3ff; padding:6px 18px; border-radius:6px; font-weight:800; letter-spacing:2px;'>COMPLETADA</div>{{% endif %}}
      {{% if order_status == 'cancelled' %}}<div style='margin-top:8px; display:inline-block; border:2px solid #b91c1c; color:#b91c1c; background:#fef2f2; padding:6px 18px; border-radius:6px; font-weight:800; letter-spacing:2px;'>CANCELADA</div>{{% endif %}}
    </div>
  </div>
  <table class='meta-table'>
    <tr><td class='meta-label'>No. Orden:</td><td class='meta-value'>{{{{ order_number }}}}</td></tr>
    <tr><td class='meta-label'>Fecha:</td><td class='meta-value'>{{{{ order_date }}}}</td></tr>
    {{% if expected_date != blank %}}<tr><td class='meta-label'>Entrega Esperada:</td><td class='meta-value'>{{{{ expected_date }}}}</td></tr>{{% endif %}}
    {{% if payment_terms != blank %}}<tr><td class='meta-label'>Condiciones de Pago:</td><td class='meta-value'>{{{{ payment_terms }}}}</td></tr>{{% endif %}}
  </table>
  <div class='parties'>
    <div class='party-card'>
      <div class='party-title'>Comprador</div>
      <strong>{{{{ Company.LegalName }}}}</strong><br />
      {{{{ Company.TaxIdLabel }}}} No. {{{{ Company.TaxId | default: '____________' }}}}<br />
      {{{{ Company.Address | default: '____________' }}}}<br />
      {{{{ Country.Name }}}}
    </div>
    <div class='party-card'>
      <div class='party-title'>Proveedor</div>
      <strong>{{{{ supplier_name }}}}</strong><br />
      {{% if supplier_id != blank %}}{{{{ Country.IdDocument }}}} No. {{{{ supplier_id }}}}<br />{{% endif %}}
      {{% if supplier_address != blank %}}{{{{ supplier_address }}}}<br />{{% endif %}}
    </div>
  </div>
  <table class='items'>
    <thead><tr><th>Descripción</th><th style='text-align:center;'>Cant.</th><th style='text-align:right;'>Precio Unit.</th><th style='text-align:right;'>Subtotal</th></tr></thead>
    <tbody>{{% for item in items %}}<tr><td>{{{{ item.description }}}}</td><td style='text-align:center;'>{{{{ item.quantity }}}}</td><td style='text-align:right;'>{{{{ item.unit_price }}}}</td><td style='text-align:right;'>{{{{ item.subtotal }}}}</td></tr>{{% else %}}<tr><td colspan='4' style='text-align:center; color:#888; padding:20px;'><em>Los items se cargan dinámicamente desde la orden de compra del sistema.</em></td></tr>{{% endfor %}}</tbody>
  </table>
  <div class='totals'>
    <div>Subtotal: <strong>{{{{ Order.Subtotal | default: '0.00' }}}}</strong></div>
    <div>Descuento: <strong>{{{{ Order.Discount | default: '0.00' }}}}</strong></div>
    <div>{{{{ Country.VatLabel | default: 'Impuesto' }}}}: <strong>{{{{ Order.Tax | default: '0.00' }}}}</strong></div>
    <div class='totals-grand'>TOTAL: {{{{ Order.Currency | default: Country.Currency }}}} <strong>{{{{ Order.Total | default: '0.00' }}}}</strong></div>
  </div>
  {{% if notes != blank %}}<p style='margin-top:20px; padding:12px; background:#f0f0f0; border-radius:6px;'><strong>Notas:</strong> {{{{ notes }}}}</p>{{% endif %}}
  <p style='margin-top:16px; font-size:12px;'>El proveedor deberá confirmar la recepción de la presente orden. Cualquier variación deberá notificarse antes del despacho de la mercancía.</p>
  <div class='sig-area' style='display:flex; justify-content:space-between; margin-top:70px;'>
    <div class='sig-block' style='text-align:center; width:30%;'><div class='sig-line'>Elaborado por</div></div>
    <div class='sig-block' style='text-align:center; width:30%;'><div class='sig-line'>Vo.Bo. Jefatura</div></div>
    <div class='sig-block' style='text-align:center; width:30%;'><div class='sig-line'>Proveedor (Conformidad)</div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildReceivingReportHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #0f766e; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .meta-table {{ width: 100%; margin: 15px 0; }} .meta-table td {{ padding: 4px 0; }}
    .meta-label {{ color: #666; width: 180px; }} .meta-value {{ font-weight: 600; color: #1A0A3E; }}
    .totals {{ text-align: right; margin-top: 10px; }}
    .totals-grand {{ font-size: 17px; font-weight: 700; color: #0f766e; border-top: 2px solid #0f766e; padding-top: 8px; }}
    .confirm-box {{ margin-top: 16px; padding: 12px 16px; background: #f0fdfa; border-left: 4px solid #0f766e; border-radius: 6px; font-size: 13px; text-align: justify; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-badge'>ACTA DE RECEPCIÓN</div>
  </div>
  <table class='meta-table'>
    <tr><td class='meta-label'>No. Acta:</td><td class='meta-value'>{{{{ acta_number }}}}</td></tr>
    <tr><td class='meta-label'>Fecha de Recepción:</td><td class='meta-value'>{{{{ acta_date }}}}</td></tr>
    <tr><td class='meta-label'>Proveedor:</td><td class='meta-value'>{{{{ supplier_name }}}}</td></tr>
    {{% if supplier_id != blank %}}<tr><td class='meta-label'>{{{{ Country.IdDocument }}}}:</td><td class='meta-value'>{{{{ supplier_id }}}}</td></tr>{{% endif %}}
    {{% if order_ref != blank %}}<tr><td class='meta-label'>Ref. Orden de Compra:</td><td class='meta-value'>{{{{ order_ref }}}}</td></tr>{{% endif %}}
    {{% if invoice_ref != blank %}}<tr><td class='meta-label'>Ref. Factura Proveedor:</td><td class='meta-value'>{{{{ invoice_ref }}}}</td></tr>{{% endif %}}
  </table>
  <p style='text-align:justify;'>En la fecha de referencia, se realiza la recepción física de la mercancía descrita en el acta, verificando cantidad, estado y conformidad con lo documentado por el proveedor arriba identificado.</p>
  <table class='items'>
    <thead><tr><th>Descripción</th><th style='text-align:center;'>Cant. Recibida</th><th style='text-align:right;'>Costo Unit.</th><th style='text-align:right;'>Subtotal</th></tr></thead>
    <tbody>{{% for item in items %}}<tr><td>{{{{ item.description }}}}</td><td style='text-align:center;'>{{{{ item.quantity }}}}</td><td style='text-align:right;'>{{{{ item.unit_price }}}}</td><td style='text-align:right;'>{{{{ item.subtotal }}}}</td></tr>{{% else %}}<tr><td colspan='4' style='text-align:center; color:#888; padding:20px;'><em>Los ítems recibidos se cargan dinámicamente desde la compra del sistema.</em></td></tr>{{% endfor %}}</tbody>
  </table>
  <div class='totals'>
    <div>Subtotal: <strong>{{{{ Purchase.Subtotal | default: '0.00' }}}}</strong></div>
    <div>Descuento: <strong>{{{{ Purchase.Discount | default: '0.00' }}}}</strong></div>
    <div>{{{{ Country.VatLabel | default: 'Impuesto' }}}}: <strong>{{{{ Purchase.Tax | default: '0.00' }}}}</strong></div>
    <div class='totals-grand'>TOTAL RECIBIDO: {{{{ Purchase.Currency | default: Country.Currency }}}} <strong>{{{{ Purchase.Total | default: '0.00' }}}}</strong></div>
  </div>
  <div class='confirm-box'>
    <strong>Conformidad:</strong> El personal receptor certifica que la mercancía fue recibida en las condiciones descritas.
    {{% if notes != blank %}}<br /><strong>Observaciones:</strong> {{{{ notes }}}}{{% endif %}}
  </div>
  <div style='margin-top:70px; display:flex; justify-content:space-between;'>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Recibido por</strong><br /><small>{{{{ received_by | default: '____________' }}}}</small></div></div>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>{{{{ Company.LegalName }}}}</strong><br /><small>Almacén / Recepción</small></div></div>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Vo.Bo. Compras</strong></div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildClientPaymentReceiptHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #1A0A3E; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .meta-table {{ width: 100%; margin: 15px 0; }} .meta-table td {{ padding: 4px 0; }}
    .meta-label {{ color: #666; width: 170px; }} .meta-value {{ font-weight: 600; color: #1A0A3E; }}
    .amount-box {{ text-align: center; margin: 22px auto; width: 70%; background: #f5f3ff; border: 2px solid #7C4DFF; border-radius: 8px; padding: 16px; }}
    .amount-box small {{ color: #666; display: block; margin-bottom: 6px; text-transform: uppercase; letter-spacing: 1px; }}
    .amount-box strong {{ font-size: 24px; color: #1A0A3E; }}
    .legal-text {{ text-align: justify; font-size: 12.5px; margin-top: 16px; line-height: 1.7; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-badge'>RECIBO DE PAGO</div>
  </div>
  <table class='meta-table'>
    <tr><td class='meta-label'>No. Recibo:</td><td class='meta-value'>{{{{ receipt_number }}}}</td></tr>
    <tr><td class='meta-label'>Fecha de Pago:</td><td class='meta-value'>{{{{ payment_date }}}}</td></tr>
    <tr><td class='meta-label'>Cliente:</td><td class='meta-value'>{{{{ client_name }}}}</td></tr>
    {{% if credit_number != blank %}}<tr><td class='meta-label'>No. Crédito:</td><td class='meta-value'>{{{{ credit_number }}}}</td></tr>{{% endif %}}
    {{% if payment_method != blank %}}<tr><td class='meta-label'>Método de Pago:</td><td class='meta-value'>{{{{ payment_method }}}}</td></tr>{{% endif %}}
  </table>
  <div class='amount-box'>
    <small>Suma Recibida</small>
    <strong>{{{{ Country.Currency | default: '' }}}} {{{{ payment_amount | default: '0.00' }}}}</strong>
    {{% if principal_amount != blank %}}<div style='margin-top:10px; font-size:12px; color:#555;'>Capital: {{{{ principal_amount }}}}{{% if interest_amount != blank %}} · Interés: {{{{ interest_amount }}}}{{% endif %}}</div>{{% endif %}}
  </div>
  {{% if credit_total != blank or credit_balance != blank %}}
  <table class='meta-table'>
    {{% if credit_total != blank %}}<tr><td class='meta-label'>Monto Total del Crédito:</td><td class='meta-value'>{{{{ Country.Currency | default: '' }}}} {{{{ credit_total }}}}</td></tr>{{% endif %}}
    {{% if credit_balance != blank %}}<tr><td class='meta-label'>Saldo Restante:</td><td class='meta-value'>{{{{ Country.Currency | default: '' }}}} {{{{ credit_balance }}}}</td></tr>{{% endif %}}
  </table>
  {{% endif %}}
  <p class='legal-text'>Se deja constancia de haber recibido de <strong>{{{{ client_name }}}}</strong> la cantidad indicada arriba, en abono a {{% if credit_number != blank %}}el crédito {{{{ credit_number }}}}{{% else %}}su cuenta corriente{{% endif %}}, aplicándose conforme al método de pago registrado. Este recibo no anula obligaciones pendientes salvo indicación expresa.</p>
  <div style='margin-top:60px; text-align:center;'>
    <div style='display:inline-block; width:250px; border-top:1px solid #333; padding-top:8px;'>
      <strong>{{{{ Company.LegalName }}}}</strong><br /><small>Recibido por / Autorizado por</small>
    </div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildCashVoucherHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #b45309; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .meta-table {{ width: 100%; margin: 15px 0; }} .meta-table td {{ padding: 4px 0; }}
    .meta-label {{ color: #666; width: 150px; }} .meta-value {{ font-weight: 600; color: #1A0A3E; }}
    .amount-box {{ text-align: center; margin: 22px auto; width: 70%; background: #fffbeb; border: 2px solid #b45309; border-radius: 8px; padding: 16px; }}
    .amount-box small {{ color: #666; display: block; margin-bottom: 6px; text-transform: uppercase; letter-spacing: 1px; }}
    .amount-box strong {{ font-size: 24px; color: #1A0A3E; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-badge'>{{{{ movement_title | default: 'COMPROBANTE DE CAJA' }}}}</div>
  </div>
  <table class='meta-table'>
    <tr><td class='meta-label'>Fecha:</td><td class='meta-value'>{{{{ movement_date }}}}</td></tr>
    <tr><td class='meta-label'>Concepto:</td><td class='meta-value'>{{{{ concept | default: '____________' }}}}</td></tr>
    {{% if register_code != blank %}}<tr><td class='meta-label'>Caja:</td><td class='meta-value'>{{{{ register_code }}}}</td></tr>{{% endif %}}
    {{% if reference != blank %}}<tr><td class='meta-label'>Referencia:</td><td class='meta-value'>{{{{ reference }}}}</td></tr>{{% endif %}}
    {{% if employee_name != blank %}}<tr><td class='meta-label'>Responsable:</td><td class='meta-value'>{{{{ employee_name }}}}</td></tr>{{% endif %}}
  </table>
  <div class='amount-box'>
    <small>Monto</small>
    <strong>{{{{ Country.Currency | default: '' }}}} {{{{ amount | default: '0.00' }}}}</strong>
  </div>
  <p style='text-align:justify; font-size:12.5px;'>Por medio del presente comprobante se hace constar el movimiento de caja arriba descrito, el cual queda registrado en el sistema de tesorería de <strong>{{{{ Company.LegalName }}}}</strong> conforme al concepto y monto indicados.</p>
  <div style='margin-top:60px; display:flex; justify-content:space-between;'>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Elaborado por</strong></div></div>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Autorizado por</strong></div></div>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Recibí Conforme</strong></div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildWorkOrderHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #1A0A3E; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .meta-box {{ display: flex; gap: 16px; margin: 14px 0; flex-wrap: wrap; }}
    .meta-card {{ flex: 1; min-width: 150px; background: #f5f3ff; padding: 10px 14px; border-radius: 8px; border-left: 3px solid #7C4DFF; }}
    .meta-card small {{ color: #666; display: block; margin-bottom: 3px; }}
    .meta-card strong {{ color: #1A0A3E; font-size: 13px; }}
    .section-title {{ background: #1A0A3E; color: #fff; padding: 5px 12px; font-weight: 700; font-size: 12px; letter-spacing: 1px; margin-top: 16px; text-transform: uppercase; }}
    .section-body {{ padding: 10px 4px; font-size: 13px; text-align: justify; line-height: 1.7; }}
    .costs-table {{ width: 100%; border-collapse: collapse; margin-top: 8px; }}
    .costs-table th {{ background: #f5f3ff; padding: 8px 12px; text-align: left; font-size: 12px; }}
    .costs-table td {{ padding: 8px 12px; border-bottom: 1px solid #eee; font-size: 13px; }}
    .cost-total {{ text-align: right; font-weight: 700; font-size: 16px; color: #7C4DFF; padding-top: 10px; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-badge'>ORDEN DE TRABAJO</div>
  </div>
  <div class='meta-box'>
    <div class='meta-card'><small>No. Orden</small><strong>{{{{ wo_number }}}}</strong></div>
    <div class='meta-card'><small>Fecha de Reporte</small><strong>{{{{ report_date }}}}</strong></div>
    <div class='meta-card'><small>Placa</small><strong>{{{{ vehicle_plate }}}}</strong></div>
    {{% if vehicle_desc != blank %}}<div class='meta-card'><small>Vehículo</small><strong>{{{{ vehicle_desc }}}}</strong></div>{{% endif %}}
    {{% if driver_name != blank %}}<div class='meta-card'><small>Conductor</small><strong>{{{{ driver_name }}}}</strong></div>{{% endif %}}
    {{% if priority != blank %}}<div class='meta-card'><small>Prioridad</small><strong>{{{{ priority }}}}</strong></div>{{% endif %}}
  </div>
  <div class='section-title'>Problema Reportado</div>
  <div class='section-body'>{{{{ problem_description | default: '____________' }}}}</div>
  {{% if diagnosis != blank %}}<div class='section-title'>Diagnóstico</div><div class='section-body'>{{{{ diagnosis }}}}</div>{{% endif %}}
  {{% if root_cause != blank %}}<div class='section-title'>Causa Raíz</div><div class='section-body'>{{{{ root_cause }}}}</div>{{% endif %}}
  {{% if solution_applied != blank %}}<div class='section-title'>Solución Aplicada</div><div class='section-body'>{{{{ solution_applied }}}}</div>{{% endif %}}
  <div class='section-title'>Costos</div>
  <table class='costs-table'>
    <thead><tr><th>Concepto</th><th style='text-align:right;'>Monto ({{{{ Country.Currency | default: '' }}}})</th></tr></thead>
    <tbody>
      <tr><td>Mano de Obra</td><td style='text-align:right;'>{{{{ WorkOrder.CostLabor | default: '0.00' }}}}</td></tr>
      <tr><td>Repuestos / Materiales</td><td style='text-align:right;'>{{{{ WorkOrder.CostParts | default: '0.00' }}}}</td></tr>
      <tr><td><strong>Total</strong></td><td style='text-align:right;'><strong>{{{{ WorkOrder.CostTotal | default: '0.00' }}}}</strong></td></tr>
    </tbody>
  </table>
  <div class='meta-box' style='margin-top:14px;'>
    {{% if workshop_name != blank %}}<div class='meta-card'><small>Taller</small><strong>{{{{ workshop_name }}}}</strong></div>{{% endif %}}
    {{% if mechanic != blank %}}<div class='meta-card'><small>Mecánico Responsable</small><strong>{{{{ mechanic }}}}</strong></div>{{% endif %}}
    {{% if downtime_hours != blank %}}<div class='meta-card'><small>Tiempo de Inmovilización</small><strong>{{{{ downtime_hours }}}} horas</strong></div>{{% endif %}}
  </div>
  <div style='margin-top:50px; display:flex; justify-content:space-between;'>
    <div style='text-align:center; width:32%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Reportado por</strong></div></div>
    <div style='text-align:center; width:32%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Ejecutado por</strong></div></div>
    <div style='text-align:center; width:32%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Vo.Bo. Jefatura Fleet</strong></div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildWorkshopReceiptHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #b45309; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .meta-box {{ display: flex; gap: 16px; margin: 14px 0; flex-wrap: wrap; }}
    .meta-card {{ flex: 1; min-width: 150px; background: #fffbeb; padding: 10px 14px; border-radius: 8px; border-left: 3px solid #b45309; }}
    .meta-card small {{ color: #666; display: block; margin-bottom: 3px; }}
    .meta-card strong {{ color: #1A0A3E; font-size: 13px; }}
    .section-title {{ background: #1A0A3E; color: #fff; padding: 5px 12px; font-weight: 700; font-size: 12px; letter-spacing: 1px; margin-top: 16px; text-transform: uppercase; }}
    .section-body {{ padding: 10px 4px; font-size: 13px; text-align: justify; line-height: 1.7; }}
    .condition-row {{ display: flex; gap: 16px; margin-top: 8px; }}
    .condition-card {{ flex: 1; background: #f5f3ff; padding: 10px 14px; border-radius: 8px; border-left: 3px solid #7C4DFF; font-size: 12.5px; line-height: 1.6; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-badge'>ACTA DE RECEPCIÓN (TALLER)</div>
  </div>
  <div class='meta-box'>
    <div class='meta-card'><small>No. Acta</small><strong>{{{{ acta_number }}}}</strong></div>
    <div class='meta-card'><small>Fecha de Recepción</small><strong>{{{{ report_date }}}}</strong></div>
    <div class='meta-card'><small>Prioridad</small><strong>{{{{ priority | default: 'Media' }}}}</strong></div>
    {{% if cost_estimate != blank %}}<div class='meta-card'><small>Costo Estimado</small><strong>{{{{ Country.Currency | default: '' }}}} {{{{ cost_estimate }}}}</strong></div>{{% endif %}}
  </div>
  {{% if workshop_name != blank %}}<div class='section-title'>Taller Receptor</div><div class='section-body'><strong>{{{{ workshop_name }}}}</strong>{{% if workshop_address != blank %}} — {{{{ workshop_address }}}}{{% endif %}}{{% if workshop_phone != blank %}} — Tel. {{{{ workshop_phone }}}}{{% endif %}}</div>{{% endif %}}
  <div class='section-title'>Vehículo / Equipo</div>
  <div class='meta-box'>
    <div class='meta-card'><small>Placa</small><strong>{{{{ vehicle_plate }}}}</strong></div>
    {{% if vehicle_desc != blank %}}<div class='meta-card'><small>Vehículo</small><strong>{{{{ vehicle_desc }}}}</strong></div>{{% endif %}}
    {{% if driver_name != blank %}}<div class='meta-card'><small>Responsable</small><strong>{{{{ driver_name }}}}</strong></div>{{% endif %}}
  </div>
  <div class='section-title'>Problema Reportado</div>
  <div class='section-body'>{{{{ problem_description | default: '____________' }}}}</div>
  <div class='condition-row'>
    <div class='condition-card'><strong>Condiciones de Recepción:</strong> el taller recibe el vehículo descrito en el acta para las reparaciones indicadas. Se considera entregado en el estado descrito en el reporte de falla.</div>
    <div class='condition-card'><strong>Plazo y Alcance:</strong> el taller deberá informar diagnóstico y presupuesto antes de ejecutar trabajos adicionales a los reportados. Cualquier trabajo extra requiere autorización del responsable de la flota.</div>
  </div>
  {{% if notes != blank %}}<div class='section-title'>Observaciones</div><div class='section-body'>{{{{ notes }}}}</div>{{% endif %}}
  <div style='margin-top:50px; display:flex; justify-content:space-between;'>
    <div style='text-align:center; width:32%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Entregado por</strong><br /><small>{{{{ driver_name | default: '____________' }}}}</small></div></div>
    <div style='text-align:center; width:32%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Recibido por (Taller)</strong><br /><small>{{{{ workshop_name | default: '____________' }}}}</small></div></div>
    <div style='text-align:center; width:32%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Vo.Bo. Jefatura Fleet</strong></div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildSupplierPaymentReceiptHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #b45309; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .meta-table {{ width: 100%; margin: 15px 0; }} .meta-table td {{ padding: 4px 0; }}
    .meta-label {{ color: #666; width: 170px; }} .meta-value {{ font-weight: 600; color: #1A0A3E; }}
    .amount-box {{ text-align: center; margin: 22px auto; width: 70%; background: #fffbeb; border: 2px solid #b45309; border-radius: 8px; padding: 16px; }}
    .amount-box small {{ color: #666; display: block; margin-bottom: 6px; text-transform: uppercase; letter-spacing: 1px; }}
    .amount-box strong {{ font-size: 24px; color: #1A0A3E; }}
    .legal-text {{ text-align: justify; font-size: 12.5px; margin-top: 16px; line-height: 1.7; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-badge'>RECIBO DE PAGO A PROVEEDOR</div>
  </div>
  <table class='meta-table'>
    <tr><td class='meta-label'>No. Recibo:</td><td class='meta-value'>{{{{ receipt_number }}}}</td></tr>
    <tr><td class='meta-label'>Fecha de Pago:</td><td class='meta-value'>{{{{ payment_date }}}}</td></tr>
    <tr><td class='meta-label'>Proveedor:</td><td class='meta-value'>{{{{ supplier_name }}}}</td></tr>
    {{% if purchase_ref != blank %}}<tr><td class='meta-label'>Ref. Compra:</td><td class='meta-value'>{{{{ purchase_ref }}}}</td></tr>{{% endif %}}
    {{% if payment_method != blank %}}<tr><td class='meta-label'>Método de Pago:</td><td class='meta-value'>{{{{ payment_method }}}}</td></tr>{{% endif %}}
  </table>
  <div class='amount-box'>
    <small>Suma Pagada</small>
    <strong>{{{{ Country.Currency | default: '' }}}} {{{{ payment_amount | default: '0.00' }}}}</strong>
    {{% if purchase_balance != blank %}}<div style='margin-top:10px; font-size:12px; color:#555;'>Saldo Pendiente: {{{{ Country.Currency | default: '' }}}} {{{{ purchase_balance }}}}</div>{{% endif %}}
  </div>
  <p class='legal-text'>Se deja constancia de haber realizado el pago arriba indicado a favor del proveedor <strong>{{{{ supplier_name }}}}</strong>, aplicándose al {{% if purchase_ref != blank %}}documento {{{{ purchase_ref }}}}{{% else %}}cuenta corriente del proveedor{{% endif %}}. Este comprobante no anula obligaciones pendientes salvo indicación expresa.
    {{% if notes != blank %}}<br /><strong>Notas:</strong> {{{{ notes }}}}{{% endif %}}</p>
  <div style='margin-top:60px; display:flex; justify-content:space-between;'>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Elaborado por</strong></div></div>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Autorizado por</strong></div></div>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Proveedor (Conformidad)</strong></div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildWarrantyCertificateHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #2EE59D; color: #0b3d2e; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .meta-table {{ width: 100%; margin: 15px 0; }} .meta-table td {{ padding: 4px 0; }}
    .meta-label {{ color: #666; width: 180px; }} .meta-value {{ font-weight: 600; color: #1A0A3E; }}
    .warranty-box {{ text-align: center; margin: 20px auto; width: 75%; background: #f0fdfa; border: 2px solid #2EE59D; border-radius: 8px; padding: 16px; }}
    .warranty-box small {{ color: #666; display: block; margin-bottom: 6px; text-transform: uppercase; letter-spacing: 1px; }}
    .warranty-box strong {{ font-size: 22px; color: #0b3d2e; }}
    .terms {{ text-align: justify; font-size: 12.5px; margin-top: 16px; line-height: 1.8; }}
    .terms ol {{ margin: 8px 0 0 20px; padding: 0; }}
    .terms li {{ margin-bottom: 6px; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-badge'>CERTIFICADO DE GARANTÍA</div>
  </div>
  <div class='warranty-box'>
    <small>No. de Garantía</small>
    <strong>{{{{ warranty_number }}}}</strong>
  </div>
  <table class='meta-table'>
    <tr><td class='meta-label'>Cliente:</td><td class='meta-value'>{{{{ client_name }}}}</td></tr>
    <tr><td class='meta-label'>Producto:</td><td class='meta-value'>{{{{ product_name }}}}</td></tr>
    {{% if serial_number != blank %}}<tr><td class='meta-label'>No. de Serie:</td><td class='meta-value'>{{{{ serial_number }}}}</td></tr>{{% endif %}}
    <tr><td class='meta-label'>Inicio de Vigencia:</td><td class='meta-value'>{{{{ start_date }}}}</td></tr>
    <tr><td class='meta-label'>Vencimiento:</td><td class='meta-value'>{{{{ end_date }}}}</td></tr>
    <tr><td class='meta-label'>Duración:</td><td class='meta-value'>{{{{ duration_months | default: '0' }}}} meses</td></tr>
  </table>
  <div class='terms'>
    <p><strong>{{{{ Company.LegalName }}}}</strong> certifica que el producto arriba identificado cuenta con garantía por el plazo indicado, sujeta a las siguientes condiciones:</p>
    <ol>
      <li>La garantía cubre defectos de fabricación y fallas en los materiales bajo uso normal del producto.</li>
      <li>NO cubre daños por mal uso, golpes, humedad, manipulación no autorizada ni desgaste natural.</li>
      <li>Para hacerla valer, presentar este certificado junto con el comprobante de compra.</li>
      <li>Los tiempos de respuesta se rigen por el SLA contratado con el distribuidor.</li>
      {{% if terms != blank %}}<li>{{{{ terms }}}}</li>{{% endif %}}
    </ol>
  </div>
  <div style='margin-top:50px; display:flex; justify-content:space-between;'>
    <div style='text-align:center; width:32%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Cliente</strong><br /><small>{{{{ client_name }}}}</small></div></div>
    <div style='text-align:center; width:32%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>{{{{ Company.LegalName }}}}</strong></div></div>
    <div style='text-align:center; width:32%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Vo.Bo. Soporte Técnico</strong></div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildDeliveryRouteSheetHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #1A0A3E; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .meta-box {{ display: flex; gap: 16px; margin: 14px 0; flex-wrap: wrap; }}
    .meta-card {{ flex: 1; min-width: 150px; background: #f5f3ff; padding: 10px 14px; border-radius: 8px; border-left: 3px solid #7C4DFF; }}
    .meta-card small {{ color: #666; display: block; margin-bottom: 3px; }}
    .meta-card strong {{ color: #1A0A3E; font-size: 13px; }}
    .section-title {{ background: #1A0A3E; color: #fff; padding: 5px 12px; font-weight: 700; font-size: 12px; letter-spacing: 1px; margin-top: 16px; text-transform: uppercase; }}
    .section-body {{ padding: 10px 4px; font-size: 13px; text-align: justify; line-height: 1.7; }}
    .items-note {{ background: #f5f3ff; border-left: 4px solid #7C4DFF; padding: 10px 14px; font-size: 12.5px; margin-top: 12px; border-radius: 6px; }}
    .signature-area {{ margin-top: 70px; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-badge'>HOJA DE RUTA / ENTREGA</div>
  </div>
  <div class='meta-box'>
    <div class='meta-card'><small>Código</small><strong>{{{{ delivery_code }}}}</strong></div>
    <div class='meta-card'><small>Fecha Programada</small><strong>{{{{ scheduled_date }}}}</strong></div>
    {{% if time_window != blank %}}<div class='meta-card'><small>Franja Horaria</small><strong>{{{{ time_window }}}}</strong></div>{{% endif %}}
    {{% if driver_name != blank %}}<div class='meta-card'><small>Conductor</small><strong>{{{{ driver_name }}}}</strong></div>{{% endif %}}
    {{% if vehicle_plate != blank %}}<div class='meta-card'><small>Placa</small><strong>{{{{ vehicle_plate }}}}</strong></div>{{% endif %}}
    {{% if route_name != blank %}}<div class='meta-card'><small>Ruta</small><strong>{{{{ route_name }}}}</strong></div>{{% endif %}}
  </div>
  {{% if client_name != blank %}}<div class='section-title'>Destinatario</div><div class='section-body'><strong>{{{{ client_name }}}}</strong><br />{{{{ delivery_address }}}}</div>{{% else %}}<div class='section-title'>Dirección de Entrega</div><div class='section-body'>{{{{ delivery_address }}}}</div>{{% endif %}}
  <div class='section-title'>Ítems de la Entrega</div>
  <table class='items'>
    <thead><tr><th>Producto</th><th style='text-align:center;'>Cant. Ordenada</th><th style='text-align:center;'>Entregada</th><th style='text-align:center;'>Devuelta</th><th>Lote / Serie</th></tr></thead>
    <tbody>{{% for item in items %}}<tr><td>{{{{ item.product }}}}</td><td style='text-align:center;'>{{{{ item.qty_ordered }}}}</td><td style='text-align:center;'>{{{{ item.qty_delivered }}}}</td><td style='text-align:center;'>{{{{ item.qty_returned }}}}</td><td>{{{{ item.lot_serial }}}}</td></tr>{{% else %}}<tr><td colspan='5' style='text-align:center; color:#888; padding:20px;'><em>Los ítems (cantidad ordenada, entregada y devuelta por producto) se cargan dinámicamente desde la entrega registrada en el sistema.</em></td></tr>{{% endfor %}}</tbody>
  </table>
  {{% if observations != blank %}}<div class='section-title'>Observaciones</div><div class='section-body'>{{{{ observations }}}}</div>{{% endif %}}
  <div class='signature-area' style='display:flex; justify-content:space-between;'>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Entregado por</strong><br /><small>{{{{ driver_name | default: '____________' }}}}</small></div></div>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Recibí Conforme</strong><br /><small>{{{{ receiver_name | default: '____________' }}}}</small></div></div>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>{{{{ Company.LegalName }}}}</strong></div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildFixedAssetCardHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #7C4DFF; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .meta-box {{ display: flex; gap: 16px; margin: 14px 0; flex-wrap: wrap; }}
    .meta-card {{ flex: 1; min-width: 150px; background: #f5f3ff; padding: 10px 14px; border-radius: 8px; border-left: 3px solid #7C4DFF; }}
    .meta-card small {{ color: #666; display: block; margin-bottom: 3px; }}
    .meta-card strong {{ color: #1A0A3E; font-size: 13px; }}
    .meta-table {{ width: 100%; margin: 12px 0; }} .meta-table td {{ padding: 5px 0; }}
    .meta-label {{ color: #666; width: 220px; }} .meta-value {{ font-weight: 600; color: #1A0A3E; }}
    .section-title {{ background: #1A0A3E; color: #fff; padding: 5px 12px; font-weight: 700; font-size: 12px; letter-spacing: 1px; margin-top: 16px; text-transform: uppercase; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-badge'>FICHA DE ACTIVO FIJO</div>
  </div>
  <div class='meta-box'>
    <div class='meta-card'><small>Código</small><strong>{{{{ asset_code }}}}</strong></div>
    <div class='meta-card'><small>Nombre</small><strong>{{{{ asset_name }}}}</strong></div>
    {{% if status != blank %}}<div class='meta-card'><small>Estado</small><strong>{{{{ status }}}}</strong></div>{{% endif %}}
    {{% if assigned_to != blank %}}<div class='meta-card'><small>Responsable</small><strong>{{{{ assigned_to }}}}</strong></div>{{% endif %}}
  </div>
  <div class='section-title'>Adquisición</div>
  <table class='meta-table'>
    <tr><td class='meta-label'>Fecha de Adquisición:</td><td class='meta-value'>{{{{ acquisition_date }}}}</td></tr>
    <tr><td class='meta-label'>Costo de Adquisición:</td><td class='meta-value'>{{{{ Country.Currency | default: '' }}}} {{{{ acquisition_cost | default: '0.00' }}}}</td></tr>
    {{% if supplier_name != blank %}}<tr><td class='meta-label'>Proveedor:</td><td class='meta-value'>{{{{ supplier_name }}}}</td></tr>{{% endif %}}
  </table>
  <div class='section-title'>Depreciación</div>
  <table class='meta-table'>
    <tr><td class='meta-label'>Vida Útil:</td><td class='meta-value'>{{{{ useful_life_years | default: '0' }}}} años</td></tr>
    <tr><td class='meta-label'>Valor Residual:</td><td class='meta-value'>{{{{ Country.Currency | default: '' }}}} {{{{ residual_value | default: '0.00' }}}}</td></tr>
    {{% if depreciation_method != blank %}}<tr><td class='meta-label'>Método:</td><td class='meta-value'>{{{{ depreciation_method }}}}</td></tr>{{% endif %}}
  </table>
  <div style='margin-top:60px; display:flex; justify-content:space-between;'>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Registrado por</strong></div></div>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Vo.Bo. Contabilidad</strong></div></div>
    <div style='text-align:center; width:30%;'><div style='border-top:1px solid #333; padding-top:8px;'><strong>Responsable del Activo</strong></div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildKardexHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #0f766e; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .meta-box {{ display: flex; gap: 16px; margin: 14px 0; flex-wrap: wrap; }}
    .meta-card {{ flex: 1; min-width: 150px; background: #f0fdfa; padding: 10px 14px; border-radius: 8px; border-left: 3px solid #0f766e; }}
    .meta-card small {{ color: #666; display: block; margin-bottom: 3px; }}
    .meta-card strong {{ color: #1A0A3E; font-size: 13px; }}
    .items-note {{ background: #f0fdfa; border-left: 4px solid #0f766e; padding: 10px 14px; font-size: 12.5px; margin-top: 12px; border-radius: 6px; text-align: justify; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div class='doc-badge'>KARDEX DE PRODUCTO</div>
  </div>
  <div class='meta-box'>
    <div class='meta-card'><small>Código</small><strong>{{{{ product_code }}}}</strong></div>
    <div class='meta-card'><small>Producto</small><strong>{{{{ product_name }}}}</strong></div>
    {{% if unit_of_measure != blank %}}<div class='meta-card'><small>Unidad</small><strong>{{{{ unit_of_measure }}}}</strong></div>{{% endif %}}
    {{% if current_stock != blank %}}<div class='meta-card'><small>Stock Actual</small><strong>{{{{ current_stock }}}}</strong></div>{{% endif %}}
  </div>
  <div class='meta-box'>
    {{% if period != blank %}}<div class='meta-card'><small>Período</small><strong>{{{{ period }}}}</strong></div>{{% endif %}}
    {{% if total_in_qty != blank %}}<div class='meta-card'><small>Total Entradas</small><strong>{{{{ total_in_qty }}}}</strong></div>{{% endif %}}
    {{% if total_out_qty != blank %}}<div class='meta-card'><small>Total Salidas</small><strong>{{{{ total_out_qty }}}}</strong></div>{{% endif %}}
    {{% if current_cost != blank %}}<div class='meta-card'><small>Costo Actual</small><strong>{{{{ Country.Currency | default: '' }}}} {{{{ current_cost }}}}</strong></div>{{% endif %}}
  </div>
  <table class='items'>
    <thead><tr><th>Fecha</th><th>Tipo</th><th style='text-align:center;'>Sentido</th><th style='text-align:center;'>Cantidad</th><th style='text-align:right;'>Stock Anterior</th><th style='text-align:right;'>Stock Posterior</th><th>Referencia</th></tr></thead>
    <tbody>{{% for m in movements %}}<tr><td>{{{{ m.date }}}}</td><td>{{{{ m.type }}}}</td><td style='text-align:center;'>{{{{ m.direction }}}}</td><td style='text-align:center;'>{{{{ m.quantity }}}}</td><td style='text-align:right;'>{{{{ m.stock_before }}}}</td><td style='text-align:right;'>{{{{ m.stock_after }}}}</td><td>{{{{ m.reference }}}}</td></tr>{{% else %}}<tr><td colspan='7' style='text-align:center; color:#888; padding:20px;'><em>No hay movimientos registrados para este producto.</em></td></tr>{{% endfor %}}</tbody>
  </table>
  <div style='margin-top:60px; text-align:center;'>
    <div style='display:inline-block; width:250px; border-top:1px solid #333; padding-top:8px;'>
      <strong>{{{{ Company.LegalName }}}}</strong><br /><small>Responsable de Almacén</small>
    </div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildPurchaseRequestHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #7C4DFF; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .meta-table {{ width: 100%; border-collapse: collapse; margin: 14px 0; }}
    .meta-table td {{ padding: 6px 10px; font-size: 13px; border-bottom: 1px solid #eee; }}
    .meta-label {{ font-weight: 600; color: #1A0A3E; width: 180px; }}
    .totals {{ text-align: right; margin-top: 10px; }}
    .totals-grand {{ font-size: 18px; font-weight: 700; color: #7C4DFF; border-top: 2px solid #1A0A3E; padding-top: 8px; }}
    .reject-box {{ margin-top: 14px; padding: 10px 14px; background: #fef2f2; border-left: 4px solid #b91c1c; border-radius: 6px; font-size: 13px; color: #991b1b; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div style='text-align:right;'>
      <div class='doc-badge'>SOLICITUD DE COTIZACIÓN</div>
      {{% if request_status == 'submitted' %}}<div style='margin-top:8px; display:inline-block; border:2px solid #3b82f6; color:#1d4ed8; background:#eff6ff; padding:6px 18px; border-radius:6px; font-weight:800; letter-spacing:2px;'>ENVIADA</div>{{% endif %}}
      {{% if request_status == 'approved' %}}<div style='margin-top:8px; display:inline-block; border:2px solid #2EE59D; color:#0f766e; background:#f0fdfa; padding:6px 18px; border-radius:6px; font-weight:800; letter-spacing:2px;'>APROBADA</div>{{% endif %}}
      {{% if request_status == 'rejected' %}}<div style='margin-top:8px; display:inline-block; border:2px solid #b91c1c; color:#b91c1c; background:#fef2f2; padding:6px 18px; border-radius:6px; font-weight:800; letter-spacing:2px;'>RECHAZADA</div>{{% endif %}}
      {{% if request_status == 'cancelled' %}}<div style='margin-top:8px; display:inline-block; border:2px solid #6b7280; color:#6b7280; background:#f3f4f6; padding:6px 18px; border-radius:6px; font-weight:800; letter-spacing:2px;'>CANCELADA</div>{{% endif %}}
    </div>
  </div>
  <table class='meta-table'>
    <tr><td class='meta-label'>No. Solicitud:</td><td class='meta-value'>{{{{ request_number }}}}</td></tr>
    <tr><td class='meta-label'>Fecha:</td><td class='meta-value'>{{{{ request_date }}}}</td></tr>
    {{% if title != blank %}}<tr><td class='meta-label'>Título:</td><td class='meta-value'>{{{{ title }}}}</td></tr>{{% endif %}}
    {{% if priority != blank %}}<tr><td class='meta-label'>Prioridad:</td><td class='meta-value'>{{{{ priority }}}}</td></tr>{{% endif %}}
    {{% if needed_by_date != blank %}}<tr><td class='meta-label'>Requerida Para:</td><td class='meta-value'>{{{{ needed_by_date }}}}</td></tr>{{% endif %}}
    {{% if requested_by != blank %}}<tr><td class='meta-label'>Solicitante:</td><td class='meta-value'>{{{{ requested_by }}}}</td></tr>{{% endif %}}
    {{% if branch_name != blank %}}<tr><td class='meta-label'>Sucursal:</td><td class='meta-value'>{{{{ branch_name }}}}</td></tr>{{% endif %}}
  </table>
  <table class='items'>
    <thead><tr><th>Código</th><th>Descripción</th><th style='text-align:center;'>Cant.</th><th style='text-align:right;'>P. Estimado</th><th style='text-align:right;'>Subtotal</th></tr></thead>
    <tbody>{{% for item in items %}}<tr><td>{{{{ item.code }}}}</td><td>{{{{ item.description }}}}</td><td style='text-align:center;'>{{{{ item.quantity }}}}</td><td style='text-align:right;'>{{{{ item.unit_price }}}}</td><td style='text-align:right;'>{{{{ item.subtotal }}}}</td></tr>{{% else %}}<tr><td colspan='5' style='text-align:center; color:#888; padding:20px;'><em>Los items se cargan dinámicamente desde la solicitud del sistema.</em></td></tr>{{% endfor %}}</tbody>
  </table>
  <div class='totals'>
    <div class='totals-grand'>TOTAL ESTIMADO: {{{{ Country.Currency }}}} <strong>{{{{ Request.EstimatedTotal | default: '0.00' }}}}</strong></div>
  </div>
  {{% if request_status == 'rejected' %}}
  <div class='reject-box'><strong>Motivo de rechazo:</strong> {{{{ rejection_reason | default: 'Sin motivo indicado' }}}}</div>
  {{% endif %}}
  {{% if notes != blank %}}<p style='margin-top:20px; padding:12px; background:#f0f0f0; border-radius:6px;'><strong>Notas:</strong> {{{{ notes }}}}</p>{{% endif %}}
  <div class='sig-area' style='display:flex; justify-content:space-between; margin-top:70px;'>
    <div class='sig-block' style='text-align:center; width:30%;'><div class='sig-line'>Solicitado por</div></div>
    <div class='sig-block' style='text-align:center; width:30%;'><div class='sig-line'>Vo.Bo. Compras</div></div>
    <div class='sig-block' style='text-align:center; width:30%;'><div class='sig-line'>Aprobado por</div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";

    private static string BuildStockTransferHtml() => $@"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
  {SharedStyles()}
  <style>
    .doc-badge {{ background: #0f766e; color: white; padding: 8px 20px; border-radius: 20px; font-size: 14px; font-weight: 600; }}
    .header {{ display: flex; justify-content: space-between; align-items: center; }}
    .parties {{ display: flex; gap: 20px; margin: 16px 0; }}
    .party-card {{ flex: 1; background: #f0fdfa; padding: 14px 18px; border-radius: 8px; border-left: 3px solid #0f766e; font-size: 13px; }}
    .party-card .party-title {{ font-weight: 700; color: #0f766e; text-transform: uppercase; font-size: 11px; letter-spacing: 1px; margin-bottom: 6px; }}
    .meta-table {{ width: 100%; border-collapse: collapse; margin: 14px 0; }}
    .meta-table td {{ padding: 6px 10px; font-size: 13px; border-bottom: 1px solid #eee; }}
    .meta-label {{ font-weight: 600; color: #1A0A3E; width: 180px; }}
    .totals {{ text-align: right; margin-top: 10px; }}
    .totals-grand {{ font-size: 18px; font-weight: 700; color: #0f766e; border-top: 2px solid #1A0A3E; padding-top: 8px; }}
  </style>
</head><body>
  <div class='header'>
    <div class='company-name'>{{{{ Company.LegalName }}}}</div>
    <div style='text-align:right;'>
      <div class='doc-badge'>TRANSFERENCIA ENTRE BODEGAS</div>
      {{% if transfer_status == 'in_transit' %}}<div style='margin-top:8px; display:inline-block; border:2px solid #FFD54F; color:#92400e; background:#fffbeb; padding:6px 18px; border-radius:6px; font-weight:800; letter-spacing:2px;'>EN TRÁNSITO</div>{{% endif %}}
      {{% if transfer_status == 'completed' %}}<div style='margin-top:8px; display:inline-block; border:2px solid #1A0A3E; color:#1A0A3E; background:#f5f3ff; padding:6px 18px; border-radius:6px; font-weight:800; letter-spacing:2px;'>COMPLETADA</div>{{% endif %}}
      {{% if transfer_status == 'cancelled' %}}<div style='margin-top:8px; display:inline-block; border:2px solid #b91c1c; color:#b91c1c; background:#fef2f2; padding:6px 18px; border-radius:6px; font-weight:800; letter-spacing:2px;'>CANCELADA</div>{{% endif %}}
    </div>
  </div>
  <div class='parties'>
    <div class='party-card'>
      <div class='party-title'>Sucursal Origen</div>
      <strong>{{{{ from_branch | default: '____________' }}}}</strong><br />
      Despacho de mercancía
    </div>
    <div class='party-card'>
      <div class='party-title'>Sucursal Destino</div>
      <strong>{{{{ to_branch | default: '____________' }}}}</strong><br />
      Recepción de mercancía
    </div>
  </div>
  <table class='meta-table'>
    <tr><td class='meta-label'>No. Transferencia:</td><td class='meta-value'>{{{{ transfer_number }}}}</td></tr>
    <tr><td class='meta-label'>Fecha de Registro:</td><td class='meta-value'>{{{{ transfer_date }}}}</td></tr>
    {{% if dispatched_at != blank %}}<tr><td class='meta-label'>Despachada:</td><td class='meta-value'>{{{{ dispatched_at }}}}</td></tr>{{% endif %}}
    {{% if received_at != blank %}}<tr><td class='meta-label'>Recibida:</td><td class='meta-value'>{{{{ received_at }}}}</td></tr>{{% endif %}}
    {{% if requested_by != blank %}}<tr><td class='meta-label'>Registrada por:</td><td class='meta-value'>{{{{ requested_by }}}}</td></tr>{{% endif %}}
  </table>
  <table class='items'>
    <thead><tr><th>Código</th><th>Producto</th><th style='text-align:center;'>Cant.</th><th style='text-align:right;'>Costo U.</th><th style='text-align:right;'>Subtotal</th></tr></thead>
    <tbody>{{% for item in items %}}<tr><td>{{{{ item.code }}}}</td><td>{{{{ item.description }}}}</td><td style='text-align:center;'>{{{{ item.quantity }}}}</td><td style='text-align:right;'>{{{{ item.unit_price }}}}</td><td style='text-align:right;'>{{{{ item.subtotal }}}}</td></tr>{{% else %}}<tr><td colspan='5' style='text-align:center; color:#888; padding:20px;'><em>Los items se cargan dinámicamente desde la transferencia del sistema.</em></td></tr>{{% endfor %}}</tbody>
  </table>
  <div class='totals'>
    <div class='totals-grand'>VALOR TOTAL: {{{{ Country.Currency }}}} <strong>{{{{ items_total | default: '0.00' }}}}</strong></div>
  </div>
  {{% if notes != blank %}}<p style='margin-top:20px; padding:12px; background:#f0f0f0; border-radius:6px;'><strong>Notas:</strong> {{{{ notes }}}}</p>{{% endif %}}
  <p style='margin-top:16px; font-size:12px;'>La recepción en la sucursal destino confirma la conformidad de cantidades y estado de la mercancía transferida.</p>
  <div class='sig-area' style='display:flex; justify-content:space-between; margin-top:70px;'>
    <div class='sig-block' style='text-align:center; width:40%;'><div class='sig-line'>Despachó (Origen)</div></div>
    <div class='sig-block' style='text-align:center; width:40%;'><div class='sig-line'>Recibió (Destino)</div></div>
  </div>
  <div class='footer'>Documento generado por Zorvian ERP - Motor Documental Inteligente · {{{{ Country.Name }}}} · {{{{ Company.Date }}}}</div>
</body></html>";
}
