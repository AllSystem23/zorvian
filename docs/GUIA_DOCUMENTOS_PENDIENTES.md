# Guía de Documentos Pendientes — Zorvian ERP

Esta guía sirve para completar la implementación de documentos faltantes usando el Motor Documental Inteligente (Liquid + DocumentService.GenerateProfessionalDocumentAsync).

## Estado actual (2026-10-16)
- ✅ RRHH: Constancia de Empleo, Contrato de Trabajo, Carta de Oferta, Constancias vía solicitudes (salarial/laboral/contrato/liquidacion/poder), Liquidación Final (NIC), **Planilla de Pago** (corrida de nómina) y **Colilla de Pago** (stub por trabajador)
- ✅ Comerciales: Cotización, Proforma, Factura, Guía de Despacho, **Guía de Remisión / Traslado**, Nota de Crédito
- ✅ Compras: Orden de Compra (**con sello por estado: APROBADA / RECIBIDA PARCIALMENTE / COMPLETADA / CANCELADA**), Acta de Recepción de Compras, **Recibo de Pago (Proveedor)**, **Solicitud de Cotización** (entidad PurchaseRequest construida en esta tanda)
- ✅ Finanzas/Caja: Recibo de Pago (Cliente), Comprobante de Caja (Ingreso/Egreso — una plantilla con `movement_title` variable), **Ficha de Activo Fijo**
- ✅ Fleet: Orden de Trabajo, **Hoja de Ruta / Entrega**, **Acta de Recepción (Taller)**
- ✅ Garantías: **Certificado de Garantía**
- ✅ Inventario: **Kardex de Producto** (endpoint por producto: totales de entradas/salidas, período y stock), **Transferencia entre Bodegas** (entidad StockTransfer construida en esta tanda)
- ✅ Otros: Poder Notarial General
- ✅ **Ítems tabulados**: 13 plantillas renderizan filas reales vía `{% for %}` (Factura, Cotización, Nota de Crédito, OC, Acta de Compras, Hoja de Ruta, Kardex, Solicitud de Cotización, Transferencia entre Bodegas, Guía de Despacho, Guía de Remisión, Planilla de Pago —lista de trabajadores—, Colilla de Pago —conceptos del trabajador—)

Plantillas sembradas: **26** (6 en v2.1 multi-país + Liquidación NIC 1.1 + 19 en v1.0). El seeder **inserta las plantillas faltantes idempotentemente** en BDs existentes (`InsertMissingSeededTemplatesAsync`), así las nuevas llegan en el próximo arranque sin borrar la BD.

### Endpoints implementados (POST → genera + devuelve downloadUrl `/zorvian/v1/documents/{id}/pdf`)
| Documento | Endpoint | Contexto | Plantilla |
|---|---|---|---|
| Factura / Guía | `/zorvian/v1/sales/{id}/invoice/generate` · `/delivery-guide/generate` · `/remission-guide/generate` | `sale` | Factura / Guía de Despacho / Guía de Remisión |
| Nota de Crédito | `/zorvian/v1/credit-notes/{id}/pdf/generate` | `creditnote` | Nota de Crédito |
| Orden de Compra | `/zorvian/v1/purchase-orders/{id}/pdf/generate` | `purchaseorder` | Orden de Compra |
| Acta de Recepción | `/zorvian/v1/purchases/{id}/pdf/generate` | `purchase` | Acta de Recepción de Compras |
| Recibo de Pago (Cliente) | `/zorvian/v1/credits/payments/{paymentId}/pdf/generate` | `creditpayment` | Recibo de Pago (Cliente) |
| Comprobante de Caja | `/zorvian/v1/cash-registers/movements/{movementId}/pdf/generate` | `cashmovement` | Comprobante de Caja |
| Orden de Trabajo | `/zorvian/v1/fleet/work-orders/{id}/pdf/generate` | `workorder` | Orden de Trabajo |
| Acta de Recepción (Taller) | `/zorvian/v1/fleet/work-orders/{id}/receipt/pdf/generate` | `workshopreceipt` | Acta de Recepción (Taller) (botón "Generar Acta Taller (PDF)" en el form de OT) |
| Recibo de Pago (Proveedor) | `/zorvian/v1/supplier-payments/{id}/pdf/generate` | `supplierpayment` | Recibo de Pago (Proveedor) |
| Certificado de Garantía | `/zorvian/v1/warranties/{id}/pdf/generate` | `warranty` | Certificado de Garantía |
| Hoja de Ruta / Entrega | `/zorvian/v1/fleet/deliveries/{id}/pdf/generate` | `delivery` | Hoja de Ruta / Entrega |
| Ficha de Activo Fijo | `/zorvian/v1/fixed-assets/{id}/pdf/generate` | `fixedasset` | Ficha de Activo Fijo (backend-only: no hay página de activos fijos en el frontend) |
| Kardex de Producto | `/zorvian/v1/products/{id}/kardex/generate` | `kardex` | Kardex de Producto (botón en el popup por fila del listado de productos) |
| Solicitud de Cotización | `/zorvian/v1/purchase-requests/{id}/pdf/generate` | `purchaserequest` | Solicitud de Cotización (botón "Generar PDF" en el detalle; sello por estado + prioridad) |
| Transferencia entre Bodegas | `/zorvian/v1/stock-transfers/{id}/pdf/generate` | `stocktransfer` | Transferencia entre Bodegas (botón "Generar PDF" en el detalle; sello BORRADOR/EN TRÁNSITO/COMPLETADA) |
| Planilla de Pago | `/zorvian/v1/payroll/runs/{id}/pdf/generate` | `payroll` | Planilla de Pago (botón junto a "Exportar ACH" en el detalle de la corrida; resumen de totales + tabla de trabajadores) |
| Colilla de Pago | `/zorvian/v1/payroll/details/{detailId}/pdf/generate` | `payrolldetail` | Colilla de Pago (ícono de recibo por fila en la tabla de trabajadores; Ingresos/Deducciones en paralelo, sello PAGADO) |

## 1. Compras / Proveedores
| Documento | Entidad | Tipo | Prioridad | Notas |
|---|---|---|---|---|
| ~~Orden de Compra Aprobada~~ | PurchaseOrder | purchase_order_approved | ✅ | **Resuelto**: mismo template OC + `{% if order_status == 'approved' %}` (badge APROBADA; también parcial/completada/cancelada). Clave `order_status` en el contexto `purchaseorder`. |
| ~~Solicitud de Cotización (SC)~~ | PurchaseRequest | purchase_request | ✅ | **Resuelto (tanda 4)**: entidad `PurchaseRequest` + `PurchaseRequestDetail` construidas (estados draft→submitted→approved/rejected/cancelled, numeración `SC-YYYYMMDD-NNNN`, sucursal obligatoria), plantilla "Solicitud de Cotización" (Module Purchase), contexto `purchaserequest` con `items` + sello por estado + etiqueta de prioridad, endpoint `POST /purchase-requests/{id}/pdf/generate`, frontend completo (`/purchase-requests`: lista + form + detalle con acciones Enviar/Aprobar/Rechazar/Anular y botón PDF). |

## 2. Logística / Taller
| Documento | Entidad | Tipo | Prioridad | Notas |
|---|---|---|---|---|
| ~~Acta de Recepción (Taller)~~ | WorkOrder | workshop_receipt | ✅ | **Resuelto**: plantilla "Acta de Recepción (Taller)" + contexto `workshopreceipt` + endpoint `receipt/pdf/generate` + botón en frontend. |
| ~~Transferencia entre Bodegas~~ | StockTransfer | stock_transfer | ✅ | **Resuelto (tanda 4)**: entidad `StockTransfer` + `StockTransferDetail` construidas (estados draft→in_transit→completed/cancelled, numeración `TR-YYYYMMDD-NNNN`, SIN columna BranchId: filtro origen O destino, clon de producto en destino si el código no existe allí, costo promedio ponderado al recibir), plantilla "Transferencia entre Bodegas" (Module Inventory), contexto `stocktransfer` con `items` + sello por estado, endpoint `POST /stock-transfers/{id}/pdf/generate`, frontend completo (`/stock-transfers`: lista + form origen/destino + detalle con Despachar/Recibir/Anular y botón PDF). |

## Patrón de implementación
1. Plantilla HTML en DocumentTemplateSeeder (Build{Nombre}Html) — escaping C#: en `$@""` las claves Liquid van con 4 llaves `{{{{ X }}}}` y `{% if %}` como `{{% if %}}`
2. Registrar en BuildTemplates con Module/Category/CountryCode "ALL" + Variables JSON
3. Enriquecer contexto en **`EntityDocumentContextBuilder.BuildAsync`** (case nuevo) — el switch vive en `src/Zorvian.Application/Services/Documentary/EntityDocumentContextBuilder.cs` (extraído de DocumentGenerationService, que ahora solo orquesta: certificados + quick-generate)
4. Endpoint POST /{entidad}/{id}/pdf/generate en controlador (template repo + doc service + doc gen service)
5. Botón en frontend: POST generate → `generatedDocumentId` → GET `documents/{id}/pdf` (bytes) → SharePlus
6. Tests: theory de builders en DocumentTemplateLiquidTests (parse + render sin placeholders + sin hardcode de país) + asserts en DocumentTemplateSeederTests + tests de contexto en DocumentGenerationServiceContractContextTests

## Ítems tabulados (pipeline de listas)
- `EntityContextResponse.Data` es `Dictionary<string, object>` (ya NO `Dictionary<string,string>`): admite **listas** de diccionarios además de escalares.
- `DocumentService.WithCompanyContextAsync` mergea `Dictionary<string,string>` y `Dictionary<string,object>` sobre la base `DocumentCompanyContext` y devuelve `Dictionary<string,object>`; `LiquidModelNormalizer.Normalize` preserva valores no-string tal cual.
- Los contextos exponen `items` (factura, cotización, NC, OC, compras, entrega) o `movements` (kardex) como `List<Dictionary<string,object>>` con valores string formateados.
- En las plantillas: `{{% for item in items %}}…{{% else %}}…placeholder…{{% endfor %}}` — el `{% else %}` de Liquid conserva el placeholder cuando no hay items (entrada manual del wizard). Fluid itera `List<Dictionary<string,object>>` y resuelve `{{ item.description }}` porque `MemberAccessStrategy` registra `Dictionary<string,object>` (el tipo raíz normalizado = tipo de cada fila).
- Certificados: `GetCertificateContextAsync` aplana `Data` a string antes de los `Enrich*` (sigue devolviendo `Dictionary<string,string>` por contrato de interfaz).

## Próxima tanda recomendada
**El inventario original está 100% cerrado** — los 2 documentos descartados por falta de entidad se resolvieron construyendo las entidades (tanda 4: PurchaseRequest y StockTransfer), y el primer candidato de la tanda 6 cerró el ciclo comercial de transporte (tanda 6: Guía de Remisión). Candidatos naturales si el backlog pide nuevos documentos:
- ~~**Guía de Remisión/Traslado por sucursal**~~ — **Resuelto (tanda 6)**: plantilla "Guía de Remisión" (Module Sale, v1.0, ALL) reutilizando el contexto `sale`, con bloque ORIGEN→DESTINO, motivo de traslado, items loop y valor declarado; endpoint `POST /zorvian/v1/sales/{id}/remission-guide/generate`; botón "Generar Guía de Remisión (PDF)" en el popup del detalle de venta. Además la **Guía de Despacho** ganó `{% for %}` de items (antes mostraba placeholder estático).
- Documentos de nómina multi-país (actualizar Liquidación NIC al resto de países).
- Picking/packing list para preparación de pedidos (reutilizaría `delivery`).

> **Nota (2026-10-16, cierre tanda 7 — nómina)**: Planilla de Pago y Colilla de Pago agregadas como plantillas 25 y 26 (Module `Employee`, v1.0, ALL). Contextos nuevos `payroll` (período, estado, totales y lista `employees`) y `payrolldetail` (trabajador, ingresos, `concepts`/`employer_concepts` separados por `IsEmployerCost`) en `EntityDocumentContextBuilder` — el ctor pasó a **21 repos** (+`IPayrollRepository`; el mock del CreateSut en `DocumentGenerationServiceContractContextTests` debe pasarlos). Endpoints `POST /payroll/runs/{id}/pdf/generate` y `POST /payroll/details/{detailId}/pdf/generate` en `PayrollController` (el ctor ganó `IDocumentTemplateRepository` + `IDocumentService` + `IDocumentGenerationService` + logger). Las colillas imprimen lo que el sistema ya calculó (conceptos y deducciones vienen de la BD) → sin fórmulas legales por país en el template. Suite al cierre: **1125/1125** backend, `flutter analyze lib` 0 issues, **357/357** frontend.
>
> **Nota (2026-10-16, cierre tanda 6)**: Guía de Remisión agregada como plantilla 24 del seeder + endpoint de venta + frontend; Guía de Despacho migrada a items loop. Sin entidad ni contexto nuevos (reutiliza `sale`). Suite al cierre: **1117/1117** backend, `flutter analyze lib` 0 issues, **357/357** frontend.
>
> **Nota (2026-10-16, cierre tanda 4)**: las 2 entidades nuevas incluyen migración EF (`AddPurchaseRequestAndStockTransfer` + secuencias `seq_purchase_request_number`/`seq_stock_transfer_number`), RLS en `SECURITY_RLS.sql` (bloques 1 y 2, Phase 8) + `test_rls_staging.sql`, enforcement de módulos (`PurchaseRequests`→Purchases, `StockTransfers`→Inventory), servicios con máquina de estados y BranchValidator, y tests (contexto, render Liquid, seeder 23/24, visibilidad, servicios y matrices de repositorio). Suite al cierre: **1114/1114** backend, `flutter analyze lib` 0 issues, **357/357** frontend.
