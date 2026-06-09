# Zorvian ERP — Public API Reference

**Base URL:** `https://api.zorvian.com/zorvian/v1`
**Authentication:** Bearer Token (JWT)
**API Version:** 1.0
**Status:** Production

---

## Introducción

Bienvenido a la API pública de Zorvian ERP. Esta API le permite integrar Zorvian ERP con sistemas externos como e-commerce, marketplaces, ERPs de terceros, y más.

### ¿Quién puede usar esta API?

- ✅ Plan **Pro**: 1000 requests/día, webhooks
- ✅ Plan **Enterprise**: Unlimited requests, SLA garantizado
- ❌ Plan **Starter**: No disponible (solo UI)

### Autenticación

Todas las llamadas requieren un Bearer Token en el header `Authorization`:

```http
GET /zorvian/v1/sales HTTP/1.1
Host: api.zorvian.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
X-Tenant-Id: ten_abc123
Content-Type: application/json
```

### Obtener un Access Token

**Endpoint:** `POST /zorvian/v1/auth/login`

```bash
curl -X POST https://api.zorvian.com/zorvian/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "api@empresa.com",
    "password": "********"
  }'
```

**Response:**

```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJl...",
    "expiresIn": 3600,
    "tokenType": "Bearer"
  }
}
```

Los tokens expiran en **1 hora**. Use el refresh token para obtener uno nuevo.

---

## Endpoints Principales

### Clientes

#### Listar Clientes

```http
GET /zorvian/v1/clients?page=1&pageSize=20&status=active
```

**Query Params:**
| Param | Tipo | Descripción |
|-------|------|-------------|
| page | int | Número de página (default: 1) |
| pageSize | int | Items por página (max 100, default: 20) |
| status | string | Filtrar por status: active, inactive, blocked |
| search | string | Buscar por nombre, email o identificación |

**Response 200:**

```json
{
  "success": true,
  "data": [
    {
      "id": "cli_abc123",
      "code": "C001",
      "firstName": "Maria",
      "lastName": "Lopez",
      "email": "maria@example.com",
      "phone": "+505 8888-9999",
      "status": "active",
      "creditLimit": 50000.00,
      "balance": 1500.00,
      "createdAt": "2026-01-15T10:00:00Z"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 20,
    "totalItems": 142,
    "totalPages": 8
  }
}
```

#### Crear Cliente

```http
POST /zorvian/v1/clients
```

```json
{
  "code": "C002",
  "firstName": "Juan",
  "lastName": "Pérez",
  "email": "juan@example.com",
  "phone": "+505 7777-8888",
  "identificationType": "CEDULA",
  "identificationNumber": "001-010101-0001A",
  "creditLimit": 30000.00,
  "address": {
    "street": "Calle Principal 123",
    "city": "Managua",
    "country": "NI"
  }
}
```

### Productos

#### Listar Productos

```http
GET /zorvian/v1/products?categoryId=cat_001&inStock=true
```

#### Actualizar Stock

```http
PATCH /zorvian/v1/products/{id}/stock
```

```json
{
  "quantity": 50,
  "warehouseId": "wh_001",
  "reason": "Restock from supplier"
}
```

### Ventas

#### Crear Venta

```http
POST /zorvian/v1/sales/cash
```

```json
{
  "clientId": "cli_abc123",
  "items": [
    {
      "productId": "prd_001",
      "quantity": 2,
      "unitPrice": 100.00,
      "discount": 0
    }
  ],
  "payments": [
    {
      "method": "cash",
      "amount": 200.00
    }
  ]
}
```

#### Consultar Venta

```http
GET /zorvian/v1/sales/{id}
```

### Webhooks

#### Configurar Webhook

```http
POST /zorvian/v1/webhooks
```

```json
{
  "url": "https://your-app.com/webhooks/zorvian",
  "events": ["sale.created", "sale.paid", "client.created"],
  "description": "Sincronización con nuestro e-commerce"
}
```

#### Eventos Disponibles

| Evento | Descripción | Payload |
|--------|-------------|---------|
| `sale.created` | Nueva venta creada | `{ saleId, clientId, total, ... }` |
| `sale.paid` | Venta pagada | `{ saleId, paymentMethod, amount }` |
| `sale.cancelled` | Venta cancelada | `{ saleId, reason }` |
| `client.created` | Nuevo cliente | `{ clientId, ... }` |
| `product.low_stock` | Stock bajo mínimo | `{ productId, currentStock, minStock }` |
| `invoice.issued` | Factura emitida | `{ invoiceId, saleId, total }` |
| `payment.received` | Pago recibido | `{ paymentId, saleId, amount }` |

#### Verificar Firma del Webhook

```python
import hmac
import hashlib

def verify_webhook(payload, signature, secret):
    expected = hmac.new(
        secret.encode('utf-8'),
        payload.encode('utf-8'),
        hashlib.sha256
    ).hexdigest()
    return hmac.compare_digest(expected, signature)
```

### Rate Limiting

| Plan | Límite | Ventana |
|------|--------|---------|
| Pro | 1000 requests | 1 día |
| Enterprise | Ilimitado | - |

Headers de respuesta:

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 742
X-RateLimit-Reset: 1717003600
```

Si excedes el límite:

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 3600
```

### Errores

| Código | Significado |
|--------|-------------|
| 400 | Solicitud malformada |
| 401 | Token inválido o expirado |
| 403 | Permiso insuficiente |
| 404 | Recurso no encontrado |
| 422 | Regla de negocio violada |
| 429 | Rate limit excedido |
| 500 | Error del servidor |

**Formato de error:**

```json
{
  "success": false,
  "error": {
    "code": "INSUFFICIENT_STOCK",
    "message": "Stock insuficiente para el producto PRD-001",
    "details": {
      "productId": "prd_001",
      "requested": 10,
      "available": 3
    },
    "traceId": "req_abc123"
  }
}
```

### SDKs Oficiales

- **JavaScript/TypeScript**: `npm install @zorvian/sdk`
- **Python**: `pip install zorvian`
- **PHP**: `composer require zorvian/zorvian`
- **C#/.NET**: `dotnet add package Zorvian.SDK`

### Sandbox

Para pruebas, usa el entorno sandbox:

```
https://sandbox.zorvian.com/zorvian/v1
```

Las cuentas sandbox usan datos ficticios y se resetean cada 24 horas.

### Soporte

- 📧 Email: api-support@zorvian.com
- 💬 Chat: https://zorvian.com/support
- 📚 Docs: https://docs.zorvian.com
- 🐛 Status: https://status.zorvian.com

### Changelog

| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0.0 | 2026-06-09 | Release inicial |
</content>
<parameter name="task_progress">