# Facturación Electrónica Multi-País

**Zorvian ERP** — Cumplimiento Fiscal Centroamericano (DGI / Hacienda / SAT / SAR / MH)

---

## Ciclo de Vida de la Factura Electrónica

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#1A0A3E', 'primaryTextColor': '#fff', 'primaryBorderColor': '#FFD54F', 'lineColor': '#FFD54F', 'secondaryColor': '#2D1B69', 'tertiaryColor': '#141838'}}}%%

stateDiagram-v2
    [*] --> Pending : Venta creada
    Pending --> XmlGenerated : Generar XML firmado
    XmlGenerated --> Submitted : Enviar a entidad fiscal
    Submitted --> Authorized : Aceptada por entidad
    Submitted --> Rejected : Rechazada por entidad
    Rejected --> XmlGenerated : Reenviar (max 3 intentos)
    Authorized --> Cancelled : Nota de crédito / anulación
    Authorized --> Contabilized : Asiento contable automático
    Contabilized --> [*]
    Cancelled --> [*]
```

---

## Arquitectura del Módulo

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#1A0A3E', 'primaryTextColor': '#fff', 'primaryBorderColor': '#FFD54F', 'lineColor': '#FFD54F'}}}%%

graph TB
    subgraph Input["📥 Entrada"]
        SALE["Venta / Factura<br/>SaleCreated Event"]
        CFE["Comprobante<br/>Fiscal Electrónico"]
        CREDIT_NOTE["Nota de Crédito"]
    end

    subgraph Core["⚙️ ElectronicInvoiceService"]
        GEN_XML["Generar XML<br/>Por país + versión"]
        SIGN["Firmar XML<br/>Firma Digital"]
        SUBMIT["Enviar a Entidad<br/>HTTP POST + Retry"]
        VALIDATE["Validar Respuesta<br/>Parsear + Almacenar"]
        PDF["Generar PDF<br/>Plantilla por país"]
    end

    subgraph Countries["🌎 Entidades Fiscales"]
        NI["🇳🇮 Nicaragua<br/>DGI"]
        CR["🇨🇷 Costa Rica<br/>Hacienda"]
        GT["🇬🇹 Guatemala<br/>SAT"]
        HN["🇭🇳 Honduras<br/>SAR"]
        SV["🇸🇻 El Salvador<br/>MH"]
        PA["🇵🇦 Panamá<br/>DGI"]
    end

    subgraph Storage["💾 Persistencia"]
        DB[("PostgreSQL<br/>ElectronicInvoices")]
        XML_DB[("PostgreSQL<br/>ElectronicInvoiceXmls")]
        BLOB[("Blob Storage<br/>PDF + XML firmado")]
    end

    subgraph Output["📤 Salidas"]
        INVOICE_DOC["Factura Electrónica<br/>XML + PDF"]
        ACCOUNTING["Asiento Contable<br/>Automático"]
        DGI_REPORT["Reporte DGI<br/>Período fiscal"]
        CUSTOMER_EMAIL["Envío al Cliente<br/>Email + WhatsApp"]
    end

    Input --> Core
    Core --> Countries
    Countries -->|Respuesta| Core
    Core --> Storage
    Storage --> Output
```

---

## Flujo por País

```mermaid
sequenceDiagram
    participant V as Venta
    participant S as Sistema Zorvian
    participant FE as Entidad Fiscal (País)
    participant C as Contabilidad
    participant CL as Cliente

    V->>S: SaleCreated (con productos, impuestos, cliente)
    S->>S: Generar XML según país (timbrado/validación local)
    S->>S: Firmar digitalmente XML
    S->>FE: POST invoice (XML firmado)
    
    alt Aceptada
        FE-->>S: Código de Autorización + Número CFE
        S->>S: Guardar respuesta + estado Authorized
        S->>C: Generar asiento contable
        S->>CL: Enviar PDF + XML por email/WhatsApp
    else Rechazada
        FE-->>S: Error + código de rechazo
        S->>S: Guardar error, estado Rejected
        S->>S: Reintentar (max 3)
        alt Reintento exitoso
            S->>C: Generar asiento contable
        else Máximos intentos
            S->>V: Notificar a usuario para intervención manual
        end
    end
```

---

## Endpoints de la API

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/zorvian/v1/ElectronicInvoice/issue` | Emitir factura electrónica para una venta |
| `GET` | `/zorvian/v1/ElectronicInvoice/sale/{saleId}` | Obtener factura por ID de venta |
| `GET` | `/zorvian/v1/ElectronicInvoice/{id}` | Obtener factura por ID |
| `GET` | `/zorvian/v1/ElectronicInvoice` | Listar facturas por compañía + país |
| `POST` | `/zorvian/v1/ElectronicInvoice/{id}/resubmit` | Reenviar factura rechazada |
| `POST` | `/zorvian/v1/ElectronicInvoice/{id}/cancel` | Anular factura autorizada |
| `GET` | `/zorvian/v1/ElectronicInvoice/sale/{saleId}/xml` | Obtener XML de factura |
| `GET` | `/zorvian/v1/ElectronicInvoice/{id}/pdf` | Obtener URL del PDF |

---

## Modelo de Datos

```mermaid
classDiagram
    class ElectronicInvoice {
        +Guid Id
        +Guid SaleId
        +string CountryCode
        +string InvoiceNumber
        +string AuthorizationCode
        +DateTime AuthorizationDate
        +InvoiceStatus Status
        +string XmlContent
        +string SignedXml
        +string DgiResponse
        +string ErrorMessage
        +int Attempts
        +DateTime SubmittedAt
        +DateTime AuthorizedAt
        +string CancelReason
        +DateTime CancelledAt
        +string PdfUrl
    }

    class ElectronicInvoiceXml {
        +Guid Id
        +Guid ElectronicInvoiceId
        +string XmlType
        +string XmlContent
        +string FileHash
        +long FileSizeBytes
    }

    class Sale {
        +Guid Id
        +decimal Total
        +string Currency
        +string CountryCode
    }

    class CountryTaxConfig {
        +string CountryCode
        +string CountryName
        +string Currency
        +decimal InssEmployeeRate
        +decimal InssEmployerRate
        +decimal IrExemptAmount
        +string IrTableJson
        +int VacationDaysPerYear
        +decimal ChristmasBonusPercentage
    }

    ElectronicInvoice --> Sale
    ElectronicInvoiceXml --> ElectronicInvoice
    ElectronicInvoice --> CountryTaxConfig : CountryCode
```

---

## Formatos XML por País

| País | Entidad | Namespace | Prefijo Factura | Versión |
|:----:|:-------:|:---------:|:----------------:|:-------:|
| 🇳🇮 Nicaragua | DGI | `http://www.dgi.gob.ni` | NIC- | 1.0 |
| 🇨🇷 Costa Rica | Hacienda | `https://www.hacienda.go.cr` | CRI- | 4.3 |
| 🇬🇹 Guatemala | SAT | `http://www.sat.gob.gt` | GTM- | 2.0 |
| 🇭🇳 Honduras | SAR | `http://www.sar.gob.hn` | HND- | 1.1 |
| 🇸🇻 El Salvador | MH | `http://www.mh.gob.sv` | SLV- | 2.0 |
| 🇵🇦 Panamá | DGI | `http://www.dgi.gob.pa` | PAN- | 1.0 |

---

## Estados de la Factura Electrónica

| Estado | Descripción | Acción Siguiente |
|--------|-------------|------------------|
| `Pending` | Factura creada, pendiente de generar XML | Generar XML firmado |
| `XmlGenerated` | XML generado y firmado | Enviar a entidad fiscal |
| `Submitted` | Enviado a entidad fiscal | Esperar respuesta |
| `Authorized` | Aceptada por la entidad fiscal | ✅ Factura válida — generar asiento |
| `Rejected` | Rechazada por la entidad | Revisar error, corregir, reenviar |
| `Cancelled` | Anulada (nota de crédito) | Generar asiento de anulación |

---

## KPIs del Módulo

| KPI | Definición | Objetivo |
|-----|-----------|:--------:|
| Tasa de Autorización | % facturas autorizadas al primer intento | > 95% |
| Tiempo de Emisión | Segundos desde venta → factura autorizada | < 30s |
| Tasa de Rechazo | % facturas rechazadas | < 5% |
| Automatización Contable | % asientos generados automáticamente | 100% |
| Tiempo de Procesamiento Batch | Tiempo para procesar n facturas | < 1s por factura |

---

## Referencias en el Código

| Componente | Ruta |
|------------|------|
| Controller | `src/Zorvian.Web/Controllers/ElectronicInvoiceController.cs` |
| Service | `src/Zorvian.Application/Services/ElectronicInvoiceService.cs` |
| Entity | `src/Zorvian.Core/Entities/ElectronicInvoice.cs` |
| Repository | `src/Zorvian.Infrastructure/Repositories/ElectronicInvoiceRepository.cs` |
| Country Config | `src/Zorvian.Core/Entities/CountryTaxConfig.cs` |
| Regional Tax | `src/Zorvian.Core/Entities/RegionalTaxConfiguration.cs` |
