# Integraciones: WhatsApp, Email, Webhooks, API

**Zorvian ERP** — Arquitectura de Integración

---

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#1A0A3E', 'primaryTextColor': '#fff', 'primaryBorderColor': '#546E7A', 'lineColor': '#7C4DFF', 'secondaryColor': '#2D1B69', 'tertiaryColor': '#141838'}}}%%

graph TB
  subgraph ERP["🏛️ Zorvian ERP Core"]
    EVENTS["Event Bus<br/>RabbitMQ"]
    API["REST API<br/>zorvian/v1/*"]
    WH_SVC["Webhook Dispatcher<br/>Retry + Logging"]
    NOTIF["Notification Hub<br/>SignalR + FCM"]
  end

  subgraph Outbound["📤 Outbound Integrations"]
    WHATSAPP["WhatsApp Business API<br/>Plantillas + Mensajes"]
    EMAIL["SMTP / Brevo<br/>Transaccional + Marketing"]
    SMS["SMS Gateway<br/>Alertas + OTP"]
    ACH["ACH / Bancos<br/>Pagos masivos"]
    DGI["DGI / Hacienda<br/>Facturación Electrónica"]
  end

  subgraph Inbound["📥 Inbound Integrations"]
    WH_IN["Webhooks Entrantes<br/>De terceros"]
    EMAIL_IN["Email Inbound<br/>Parsing automático"]
    API_EXT["API Externa<br/>Llamadas entrantes"]
    IMPORT["Importación Masiva<br/>Excel / CSV / XML"]
  end

  subgraph WebhookSystem["🔗 Sistema de Webhooks"]
    SUB["Suscripciones<br/>Por evento + tenant"]
    DELIVERY["Delivery Engine<br/>HTTP POST + Retry"]
    LOG["Webhook Logs<br/>Request + Response"]
    SECRET["Firma HMAC-SHA256<br/>Verificación"]
  end

  subgraph Automation["⚡ Automatizaciones"]
    AUTO_EMAIL["Email Automático<br/>Bienvenida, Factura, Recordatorio"]
    AUTO_WA["WhatsApp Auto<br/>Confirmación, Notificación"]
    AUTO_WH["Webhook Auto<br/>Evento → Tercero"]
    AUTO_ACH["Pago ACH Auto<br/>Nómina + Proveedores"]
  end

  %% Flujo
  EVENTS --> NOTIF
  EVENTS --> WH_SVC
  EVENTS --> Automation

  API --> Outbound
  WH_SVC --> WebhookSystem
  WebhookSystem --> API_EXT

  Inbound --> API
  Inbound --> EVENTS

  Automation --> WHATSAPP
  Automation --> EMAIL
  Automation --> WH_IN
  Automation --> ACH
  Automation --> DGI
  
  NOTIF --> EMAIL
  NOTIF --> WHATSAPP
  NOTIF --> SMS
```

---

## Catálogo de Webhooks

| Evento | Descripción | Payload | Frecuencia |
|--------|-------------|---------|:-----------:|
| `sale.created` | Venta facturada | Cliente, monto, productos | Inmediato |
| `sale.credit_note` | Nota de crédito emitida | Referencia, monto, razón | Inmediato |
| `inventory.low_stock` | Stock por debajo del mínimo | Producto, cantidad actual | Inmediato |
| `employee.created` | Nuevo empleado registrado | Datos del empleado | Inmediato |
| `payroll.processed` | Nómina procesada | Período, total brutos/netos | Mensual |
| `warranty.status_change` | Cambio de estado de garantía | Estado anterior → nuevo | Inmediato |
| `accounting.entry_posted` | Asiento contable contabilizado | Cuenta, debe, haber | Inmediato |

---

## Arquitectura de Notificaciones Multi-Canal

```mermaid
flowchart LR
    EVENT[Evento ERP] --> ROUTER[Router de Notificaciones]
    ROUTER --> CHAN1[SignalR<br/>Web + Mobile connected]
    ROUTER --> CHAN2[FCM Push<br/>Mobile background]
    ROUTER --> CHAN3[WhatsApp<br/>Plantilla + API]
    ROUTER --> CHAN4[Email<br/>SMTP transaccional]
    ROUTER --> CHAN5[SMS<br/>OTP + Alertas críticas]

    CHAN1 --> DELIVERED{Entregado?}
    CHAN2 --> DELIVERED
    CHAN3 --> DELIVERED
    CHAN4 --> DELIVERED
    CHAN5 --> DELIVERED

    DELIVERED -->|Sí| LOG[Log de entrega]
    DELIVERED -->|No| RETRY[Retry 3x + Escalar]
```

---

## API Pública — Endpoints Principales

| Método | Endpoint | Autenticación | Descripción |
|--------|----------|:-------------:|-------------|
| `GET` | `/api/v1/products` | API Key | Listar productos |
| `GET` | `/api/v1/products/{id}` | API Key | Detalle de producto |
| `POST` | `/api/v1/sales` | API Key + JWT | Crear venta |
| `GET` | `/api/v1/clients` | API Key | Listar clientes |
| `POST` | `/api/v1/webhooks/subscribe` | JWT Admin | Suscribir webhook |
| `DELETE` | `/api/v1/webhooks/{id}` | JWT Admin | Eliminar suscripción |
| `POST` | `/api/v1/inventory/adjust` | API Key + JWT | Ajuste de inventario |
| `GET` | `/api/v1/reports/{type}` | JWT Admin | Generar reporte |

---

## Formatos de Importación Soportados

| Formato | Módulos | Tamaño Máximo |
|---------|---------|:-------------:|
| Excel (.xlsx) | Productos, Clientes, Empleados, Proveedores | 10,000 filas |
| CSV (.csv) | Productos, Movimientos de inventario | 50,000 filas |
| XML (.xml) | Facturas electrónicas (DGI) | 10MB |
| JSON (.json) | Configuración, Webhooks | 5MB |
