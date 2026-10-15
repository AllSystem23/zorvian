# Guía de Documentos Pendientes — Zorvian ERP

Esta guía sirve para completar la implementación de documentos faltantes usando el Motor Documental Inteligente (Liquid + DocumentService.GenerateProfessionalDocumentAsync).

## Estado actual (2026-10-15)
- ✅ RRHH: Constancia de Empleo, Contrato de Trabajo, Carta de Oferta, Constancias vía solicitudes (salarial/laboral/contrato/liquidacion/poder), Liquidación Final (NIC)
- ✅ Comerciales: Cotización, Proforma, Factura, Guía de Despacho, Nota de Crédito
- ✅ Compras: Orden de Compra (**con sello por estado: APROBADA / RECIBIDA PARCIALMENTE / COMPLETADA / CANCELADA**), Acta de Recepción de Compras, **Recibo de Pago (Proveedor)**
- ✅ Finanzas/Caja: Recibo de Pago (Cliente), Comprobante de Caja (Ingreso/Egreso — una plantilla con `movement_title` variable), **Ficha de Activo Fijo**
- ✅ Fleet: Orden de Trabajo, **Hoja de Ruta / Entrega**, **Acta de Recepción (Taller)**
- ✅ Garantías: **Certificado de Garantía**
- ✅ Inventario: **Kardex de Producto** (endpoint por producto: totales de entradas/salidas, período y stock)
- ✅ Otros: Poder Notarial General
- ✅ **Ítems tabulados**: 7 plantillas renderizan filas reales vía `{% for %}` (Factura, Cotización, Nota de Crédito, OC, Acta de Compras, Hoja de Ruta, Kardex)

Plantillas sembradas: **21** (6 en v2.1 multi-país + Liquidación NIC 1.1 + 14 en v1.0). El seeder **inserta las plantillas faltantes idempotentemente** en BDs existentes (`InsertMissingSeededTemplatesAsync`), así las nuevas llegan en el próximo arranque sin borrar la BD.

### Endpoints implementados (POST → genera + devuelve downloadUrl `/zorvian/v1/documents/{id}/pdf`)
| Documento | Endpoint | Contexto | Plantilla |
|---|---|---|---|
| Factura / Guía | `/zorvian/v1/sales/{id}/invoice/generate` · `/delivery-guide/generate` | `sale` | Factura / Guía de Despacho |
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

## 1. Compras / Proveedores
| Documento | Entidad | Tipo | Prioridad | Notas |
|---|---|---|---|---|
| ~~Orden de Compra Aprobada~~ | PurchaseOrder | purchase_order_approved | ✅ | **Resuelto**: mismo template OC + `{% if order_status == 'approved' %}` (badge APROBADA; también parcial/completada/cancelada). Clave `order_status` en el contexto `purchaseorder`. |
| Solicitud de Cotización (SC) | ~~PurchaseRequest~~ | purchase_request | ⛔ | **Descartado**: la entidad `PurchaseRequest` NO existe en el codebase. Requiere construir el módulo de solicitudes de compra (entidad + migración + flujo de aprobación) antes que el documento. |

## 2. Logística / Taller
| Documento | Entidad | Tipo | Prioridad | Notas |
|---|---|---|---|---|
| ~~Acta de Recepción (Taller)~~ | WorkOrder | workshop_receipt | ✅ | **Resuelto**: plantilla "Acta de Recepción (Taller)" + contexto `workshopreceipt` + endpoint `receipt/pdf/generate` + botón en frontend. |
| Transferencia entre Bodegas | ~~StockTransfer~~ | stock_transfer | ⛔ | **Descartado**: la entidad `StockTransfer` NO existe (solo hay movimientos de inventario con tipos sale/purchase/initial/…, sin tipo transfer). Requiere construir el flujo de transferencias antes que el documento. |

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
**No quedan documentos del inventario originalemente previsto** — los 4 pendientes se resolvieron o descartaron (ver tablas 1 y 2). Pendientes transversales que sí quedaron cerrados en esta pasada:
- ✅ Refactor `IEntityDocumentContextBuilder` (constructor de DocumentGenerationService: 19 → 6 deps).
- ✅ Ítems tabulados (pipeline + 7 plantillas).

Si el backlog pide nuevos documentos, los candidatos naturales son los que requieren **entidades nuevas primero**: Solicitud de Cotización (PurchaseRequest) y Transferencia entre Bodegas (StockTransfer). Un candidato ya viable sin entidad nueva: **Guía de Remisión/Traslado por sucursal** (reutilizaría el contexto `sale`).

> **Nota (2026-10-15, cierre)**: `DocumentGenerationService` quedó en **6 dependencias** (`IDocumentService`, `IEntityDocumentContextBuilder`, `IEmployeeRepository`, `ICompanyRepository`, `ICertificateCalculator`, `ITenantContext`) y `EntityDocumentContextBuilder` concentra los **18 repos** (incluye `IQuoteRepository` — el caso `quote` es nuevo y alimenta la plantilla Cotización Comercial). Registro DI en `DependencyInjectionExtensions`. Suite al cierre: **1061/1061** backend, `flutter analyze lib` 0 issues, **351/351** frontend.
