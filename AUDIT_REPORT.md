# INFORME DE AUDITORÍA INTEGRAL - ZORVIAN ERP

**Fecha:** 4 de Junio, 2026
**Versión del Sistema:** .NET 9.0 (C#) con Entity Framework Core + PostgreSQL
**Alcance:** Módulos de Compras, Ventas, Inventario, Contabilidad, Caja, Créditos

---

## 1. HALLAZGOS GENERALES

### 1.1 Hallazgo H1 - El Módulo de Contabilidad SÍ EXISTE

El sistema ya cuenta con un módulo de contabilidad completo implementado con las siguientes capacidades:

| Componente | Estado | Archivo |
|---|---|---|
| Catálogo de Cuentas (`Account`) | ✅ Implementado | `Account.cs` |
| Libro Diario (`AccountingEntry` + `AccountingEntryDetail`) | ✅ Implementado | `AccountingEntry.cs` |
| Períodos Contables (`AccountingPeriod`) | ✅ Implementado | `AccountingPeriod.cs` |
| Vinculación Contable (`AccountLink`) | ✅ Implementado | `AccountLink.cs` |
| Reglas de Asientos (`AccountingRule`) | ⚠️ Existe pero NO se usa | `AccountingRule.cs` |
| Servicio de Asientos Automáticos (`AutoAccountingService`) | ✅ Implementado | `AutoAccountingService.cs` |
| Servicio de Cuentas (`AccountService`) | ✅ Implementado | `AccountService.cs` |
| Servicio de Asientos Manuales (`AccountingEntryService`) | ✅ Implementado | `AccountService.cs` (misma clase) |
| Servicio de Períodos (`AccountingPeriodService`) | ✅ Implementado | `AccountService.cs` (misma clase) |
| Reportes Financieros (`FinancialReportService`) | ✅ Implementado | `FinancialReportService.cs` |
| Balanza de Comprobación | ✅ Implementado | `GetTrialBalanceAsync()` |
| Estado de Resultados | ✅ Implementado | `GetIncomeStatementAsync()` |
| Balance General | ✅ Implementado | `GetBalanceSheetAsync()` |
| Libro Mayor | ✅ Implementado | `GetGeneralLedgerAsync()` |
| API REST (Controllers) | ✅ Implementado | 5 Controllers de Contabilidad |

---

## 2. RIESGOS CRÍTICOS IDENTIFICADOS

### 2.1 Riesgo R1 - 🔴 EL GENERADOR AUTOMÁTICO DE ASIENTOS NUNCA SE INVOCA

**Severidad:** CRÍTICA

`AutoAccountingService` tiene los métodos `GenerateSaleEntryAsync()`, `GenerateCostOfSaleEntryAsync()`, `GeneratePurchaseEntryAsync()` e `GenerateInventoryEntryAsync()`, pero **NINGUNO** de estos métodos es invocado desde los servicios operativos:

| Servicio | Método | ¿Llama a contabilidad? |
|---|---|---|
| `SaleService` | `CreateCashSaleAsync()` | ❌ NO |
| `SaleService` | `CreateCreditSaleAsync()` | ❌ NO |
| `PurchaseService` | `CreateAsync()` | ❌ NO |
| `PurchaseService` | `CancelAsync()` | ❌ NO |
| `InventoryMovementService` | `CreateAsync()` | ❌ NO |
| `CreditService` | (no auditado aún) | ❓ Pendiente |

**Impacto:** Toda transacción que ocurre en Compras, Ventas e Inventario **NO GENERA ASIENTOS CONTABLES**. La contabilidad está completamente desconectada de la operación.

### 2.2 Riesgo R2 - 🔴 La entidad `AccountingRule` EXISTE PERO NO SE USA

**Severidad:** ALTA

La entidad `AccountingRule` está definida en el DbContext con su tabla correspondiente, pero:

```csharp
public DbSet<AccountingRule> AccountingRules => Set<AccountingRule>();
```

Sin embargo:
1. No existe un `IAccountingRuleRepository` 
2. `AutoAccountingService` no recibe ni consulta reglas desde la BD
3. No hay ningún controlador API para CRUD de reglas
4. Las reglas están **hardcodeadas** en los métodos del servicio

### 2.3 Riesgo R3 - 🟡 Método de Costeo no implementado

**Severidad:** MEDIA

- `Product.CostPrice` se actualiza manualmente pero NO se recalcula al recibir compras
- `PurchaseService.CreateAsync()` usa `detail.UnitPrice` como `UnitCost` en el movimiento de inventario, pero **NO actualiza** el `CostPrice` del producto con costo promedio ponderado
- `SaleService` usa `product.CostPrice` para el costo de venta, pero este valor puede estar desactualizado
- **No hay implementación de costeo promedio (promedio ponderado)**
- **No hay soporte para FIFO ni LIFO**

### 2.4 Riesgo R4 - 🟡 Las Notas de Crédito no tienen soporte contable

**Severidad:** MEDIA

- No existe entidad `CreditNote` o `PurchaseReturn` en el sistema
- No hay métodos para revertir asientos contables de ventas anuladas
- `PurchaseService.CancelAsync()` genera un movimiento de inventario `purchase_cancellation` pero **NO genera el asiento contable reversado**

### 2.5 Riesgo R5 - 🟡 Créditos y Pagos no integrados con contabilidad

**Severidad:** MEDIA

- `CreditPayment` (pagos de créditos) no genera asientos contables
- El ingreso por intereses de créditos no se contabiliza
- Los cobros no se reflejan en libros contables automáticamente

### 2.6 Riesgo R6 - 🟢 Performance en Balanza de Comprobación

**Severidad:** BAJA

`GetTrialBalanceAsync()` carga TODAS las entradas del período en memoria mediante `Task.WhenAll`, lo cual puede causar problemas de rendimiento con grandes volúmenes de datos.

---

## 3. MEJORAS SUGERIDAS

### 3.1 Mejora M1 - Integrar AutoAccountingService en Servicios Operativos

**Archivos a modificar:**
- `SaleService.cs`
- `PurchaseService.cs`
- `InventoryMovementService.cs`
- `CreditService.cs`

**Acción:** Inyectar `AutoAccountingService` en cada servicio y llamar al método correspondiente después de guardar la transacción.

**Ejemplo para SaleService:**
```csharp
// Después de SaveChangesAsync en CreateCashSaleAsync
await _autoAccounting.GenerateSaleEntryAsync(
    sale.Id, sale.Subtotal, sale.Tax, sale.Discount, sale.Total, sale.PaidAmount, sale.SaleType);

// Después de SaveChangesAsync - costo de venta
var totalCost = request.Details.Sum(d => {
    var product = await _productRepo.GetByIdAsync(d.ProductId);
    return (product?.CostPrice ?? 0) * d.Quantity;
});
await _autoAccounting.GenerateCostOfSaleEntryAsync(sale.Id, totalCost);
```

### 3.2 Mejora M2 - Implementar motor de reglas contables desde BD

**Archivos a crear/modificar:**
- `IAccountingRuleRepository.cs` (nuevo)
- `AccountingRuleRepository.cs` (nuevo)
- Refactorizar `AutoAccountingService` para leer reglas desde la BD
- Crear `AccountingRulesController.cs`

**Diseño:**
```csharp
public class AccountingRule
{
    public string EventType { get; set; } // "Sale", "Purchase", "InventoryMovement", "CreditPayment", etc.
    public int LineNumber { get; set; }
    public string LineType { get; set; } // "Debit" o "Credit"
    public string AccountRole { get; set; } // "AccountsReceivable", "Inventory", etc.
    public string? Formula { get; set; } // "{Total}", "{Subtotal}", "{Tax}", "{Quantity} * {UnitCost}"
    public bool IsActive { get; set; } = true;
}
```

### 3.3 Mejora M3 - Implementar Costeo Promedio Ponderado

**Archivos a modificar:**
- `PurchaseService.CreateAsync()` - actualizar `CostPrice` con promedio ponderado
- `Product` - considerar agregar `TotalCost` y `TotalQuantity` para cálculo de promedio

**Fórmula:**
```
Nuevo CostPrice = (StockAnterior * CostPriceAnterior + CantidadComprada * PrecioCompra) / (StockAnterior + CantidadComprada)
```

### 3.4 Mejora M4 - Agregar Notas de Crédito y Devoluciones

**Archivos a crear:**
- `CreditNote.cs` entidad
- `CreditNoteDetail.cs` entidad
- `CreditNoteService.cs`
- `CreditNotesController.cs`

**Lógica contable:**
- Nota de Crédito (Ventas): Revierte el asiento original
  - Débito: Ventas
  - Débito: IVA por Pagar
  - Crédito: Clientes / Caja
- Nota de Crédito (Compras): Revierte el asiento original
  - Débito: Proveedores
  - Crédito: Inventario
  - Crédito: IVA Crédito Fiscal

### 3.5 Mejora M5 - Integrar Créditos y Pagos con Contabilidad

**Archivos a modificar:**
- `CreditService` - agregar generación de asientos para:
  - Desembolso de crédito (si aplica)
  - Cobro de cuota (capital + intereses)
  - Castigo de crédito

**Asientos para cobro de cuota:**
```
Débito: Caja / Bancos (Monto total)
Crédito: Clientes (Capital)
Crédito: Ingresos por Intereses (Intereses)
```

### 3.6 Mejora M6 - Agregar Depreciación y Cierre Contable

**Funcionalidades faltantes:**
- Asiento de cierre de período (utilidad neta → utilidades retenidas)
- Depreciación de activos fijos
- Diferencia cambiaria (multi-monedas)

---

## 4. DISEÑO DEL MÓDULO CONTABLE (REFINAMIENTO)

El módulo existe pero necesita refinamiento. Aquí está el diseño mejorado:

### 4.1 Arquitectura de Capas

```
┌─────────────────────────────────────────────┐
│  API REST (Controllers)                      │
│  AccountsController / AccountingEntries /    │
│  AccountLinks / FinancialReports / Periods   │
├─────────────────────────────────────────────┤
│  Servicios de Aplicación                     │
│  AccountService / AccountingEntryService /   │
│  AccountLinkService / AutoAccountingService / │
│  FinancialReportService / PeriodService      │
├─────────────────────────────────────────────┤
│  Repositorios                               │
│  IAccountRepository / IAccountingEntryRepo   │
│  IAccountLinkRepository / IAccountingRuleRepo│
│  IAccountingPeriodRepository                │
├─────────────────────────────────────────────┤
│  Entidades Core                             │
│  Account / AccountingEntry / Detail          │
│  AccountLink / AccountingRule / Period       │
├─────────────────────────────────────────────┤
│  PostgreSQL (Entity Framework Core)          │
└─────────────────────────────────────────────┘
```

### 4.2 Flujo de Asientos Automáticos (Propuesto)

```
Venta → SaleService
  → Guardar Venta + Movimiento Inventario
  → AutoAccountingService.GenerateSaleEntryAsync()
    → Buscar AccountLinks para "Sale" (AccountsReceivable, SalesRevenue, VatPayable)
    → Buscar período contable abierto
    → Crear AccountingEntry con Details
    → Guardar

  → AutoAccountingService.GenerateCostOfSaleEntryAsync()
    → Buscar AccountLinks (CostOfSales, Inventory)
    → Crear AccountingEntry
    → Guardar

Compra → PurchaseService  
  → AutoAccountingService.GeneratePurchaseEntryAsync()
    → Buscar AccountLinks (Inventory, VatReceivable, AccountsPayable)
    → Crear AccountingEntry
    → Guardar
```

---

## 5. CATÁLOGO DE CUENTAS RECOMENDADO (MEJORADO)

El catálogo actual es bueno pero incompleto. Recomiendo:

```
1. ACTIVOS
  1.1 Activos Circulantes
    1.1.01 Caja General ✅
    1.1.02 Bancos ✅
    1.1.03 Clientes ✅
    1.1.04 Inventario ✅
    1.1.05 IVA Crédito Fiscal ✅
    1.1.06 Deudores Diversos 🆕
    1.1.07 Anticipos a Proveedores 🆕
  1.2 Activos No Circulantes
    1.2.01 Propiedad, Planta y Equipo ✅
    1.2.02 Depreciación Acumulada 🆕
    1.2.03 Intangibles 🆕

2. PASIVOS
  2.1 Pasivos Circulantes
    2.1.01 Proveedores ✅
    2.1.02 IVA Débito Fiscal ✅
    2.1.03 ISR por Pagar ✅
    2.1.04 Acreedores Diversos 🆕
    2.1.05 Préstamos Bancarios CP 🆕
    2.1.06 Provisiones por Pagar 🆕
  2.2 Pasivos No Circulantes
    2.2.01 Préstamos Bancarios LP 🆕

3. PATRIMONIO
  3.1.01 Capital Social ✅
  3.1.02 Utilidades Retenidas ✅
  3.1.03 Resultado del Ejercicio 🆕
  3.1.04 Reserva Legal 🆕

4. INGRESOS
  4.1.01 Ventas ✅
  4.1.02 Devoluciones sobre Ventas 🆕
  4.1.03 Descuentos sobre Ventas 🆕
  4.1.04 Ingresos por Intereses 🆕
  4.1.05 Otros Ingresos 🆕

5. COSTOS
  5.1.01 Costo de Ventas ✅
  5.1.02 Costo de Transporte 🆕

6. GASTOS
  6.1.01 Gastos Administrativos ✅
  6.1.02 Gastos de Venta ✅
  6.1.03 Gastos Financieros 🆕
  6.1.04 Depreciación 🆕
  6.1.05 Otros Gastos 🆕
```

---

## 6. VINCULACIÓN CONTABLE CONFIGURADA

| Transacción | Rol | Cuenta Vinculada |
|---|---|---|
| Sale | Inventory | 1.1.04 Inventario |
| Sale | AccountsReceivable | 1.1.03 Clientes |
| Sale | Cash | 1.1.01 Caja General |
| Sale | SalesRevenue | 4.1.01 Ventas |
| Sale | CostOfSales | 5.1.01 Costo de Ventas |
| Sale | VatPayable | 2.1.02 IVA Débito Fiscal |
| Purchase | Inventory | 1.1.04 Inventario |
| Purchase | AccountsPayable | 2.1.01 Proveedores |
| Purchase | VatReceivable | 1.1.05 IVA Crédito Fiscal |
| InventoryMovement | Inventory | 1.1.04 Inventario |
| InventoryMovement | InventoryAdjustment | 1.1.04 Inventario |

---

## 7. REGLAS DE ASIENTOS AUTOMÁTICOS (PROPUESTAS)

### 7.1 Venta al Contado

```
Evento: Venta_Contado
Débito:  Caja General (Total)
Crédito: Ventas (Subtotal - Descuento)
Crédito: IVA Débito Fiscal (Impuesto)

Evento: Costo_Venta
Débito:  Costo de Ventas (Costo Total)
Crédito: Inventario (Costo Total)
```

### 7.2 Venta al Crédito

```
Evento: Venta_Crédito
Débito:  Clientes (Total - Cuota Inicial)
Débito:  Caja General (Cuota Inicial, si existe)
Crédito: Ventas (Subtotal - Descuento)
Crédito: IVA Débito Fiscal (Impuesto)

Evento: Costo_Venta
Débito:  Costo de Ventas (Costo Total)
Crédito: Inventario (Costo Total)
```

### 7.3 Compra

```
Evento: Compra
Débito:  Inventario (Subtotal)
Débito:  IVA Crédito Fiscal (Impuesto)
Crédito: Proveedores (Total)
```

### 7.4 Cobro de Crédito

```
Evento: Cobro_Crédito
Débito:  Caja General (Monto Cobrado)
Crédito: Clientes (Capital)
Crédito: Ingresos por Intereses (Intereses)
```

### 7.5 Nota de Crédito (Ventas)

```
Evento: Nota_Credito_Venta
Débito:  Ventas (Subtotal)
Débito:  IVA Débito Fiscal (Impuesto)
Crédito: Clientes / Caja General (Total)
Débito:  Inventario (Costo)
Crédito: Costo de Ventas (Costo)
```

---

## 8. CAMBIOS REQUERIDOS EN BASE DE DATOS

### 8.1 Tablas existentes - Sin cambios

Las tablas contables ya existen y están bien diseñadas:
- `Accounts` ✅
- `AccountingEntries` ✅
- `AccountingEntryDetails` ✅
- `AccountingPeriods` ✅
- `AccountLinks` ✅
- `AccountingRules` ✅

### 8.2 Cambios necesarios en migraciones

No se requieren nuevas migraciones. Las tablas están en su lugar.

### 8.3 Pendiente: Índices recomendados

```sql
-- Mejorar performance de balanza de comprobación
CREATE INDEX IX_AccountingEntryDetails_AccountId_PeriodId 
ON "AccountingEntryDetails" ("AccountId")
INCLUDE ("DebitAmount", "CreditAmount");

CREATE INDEX IX_AccountingEntries_PeriodId_Status
ON "AccountingEntries" ("AccountingPeriodId", "Status")
INCLUDE ("Id");
```

---

## 9. CAMBIOS REQUERIDOS EN API

### 9.1 Controladores existentes (sin cambios estructurales)
- `AccountsController` ✅
- `AccountingEntriesController` ✅
- `AccountingPeriodsController` ✅
- `AccountLinksController` ✅
- `FinancialReportsController` ✅

### 9.2 Cambios en servicios existentes

**SaleService.cs:**
```csharp
// Agregar dependencia
private readonly AutoAccountingService _autoAccounting;

// En CreateCashSaleAsync() - después de SaveChangesAsync()
var entryId = await _autoAccounting.GenerateSaleEntryAsync(
    sale.Id, sale.Subtotal, sale.Tax, sale.Discount, sale.Total, sale.PaidAmount, sale.SaleType);

var totalCost = request.Details.Sum(d => {
    var p = productsCache[d.ProductId];
    return (p?.CostPrice ?? 0) * d.Quantity;
});
await _autoAccounting.GenerateCostOfSaleEntryAsync(sale.Id, totalCost);
```

**PurchaseService.cs:**
```csharp
// En CreateAsync() - después de SaveChangesAsync()
await _autoAccounting.GeneratePurchaseEntryAsync(
    purchase.Id, purchase.Subtotal, purchase.Tax, purchase.Total);

// Actualizar costo promedio del producto
foreach (var detail in request.Details)
{
    var product = await _productRepo.GetByIdAsync(detail.ProductId);
    if (product != null)
    {
        var totalCost = (product.CostPrice * (product.Stock - detail.Quantity)) + (detail.UnitPrice * detail.Quantity);
        product.CostPrice = totalCost / product.Stock;
    }
}
```

**InventoryMovementService.cs:**
```csharp
// En CreateAsync() - después de SaveChangesAsync()
await _autoAccounting.GenerateInventoryEntryAsync(
    movement.Id, movement.ProductId, movement.MovementType, 
    movement.Quantity, movement.UnitCost);
```

### 9.3 Nuevos endpoints necesarios

| Método | Ruta | Propósito |
|---|---|---|
| POST | /zorvian/v1/accounting-rules | Crear regla |
| GET | /zorvian/v1/accounting-rules | Listar reglas |
| DELETE | /zorvian/v1/accounting-rules/{id} | Eliminar regla |

---

## 10. PLAN DE IMPLEMENTACIÓN PASO A PASO

### Fase 1: Corrección Crítica (1-2 días)

- [ ] **P1.1** - Inyectar `AutoAccountingService` en `SaleService` y llamar a `GenerateSaleEntryAsync` + `GenerateCostOfSaleEntryAsync`
- [ ] **P1.2** - Inyectar `AutoAccountingService` en `PurchaseService` y llamar a `GeneratePurchaseEntryAsync`
- [ ] **P1.3** - Inyectar `AutoAccountingService` en `InventoryMovementService` y llamar a `GenerateInventoryEntryAsync`
- [ ] **P1.4** - Registrar `AutoAccountingService` en DI (si no lo está)
- [ ] **P1.5** - Pruebas de integración: crear venta → verificar asiento generado

### Fase 2: Costeo Promedio (1 día)

- [ ] **P2.1** - Implementar actualización de `CostPrice` en `PurchaseService.CreateAsync()` usando promedio ponderado
- [ ] **P2.2** - Verificar que `SaleService` use el costo actualizado

### Fase 3: Notas de Crédito (2 días)

- [ ] **P3.1** - Crear entidades `CreditNote` y `CreditNoteDetail`
- [ ] **P3.2** - Agregar DbSet en `ZorvianDbContext`
- [ ] **P3.3** - Crear migración
- [ ] **P3.4** - Implementar servicio con lógica contable de reversión
- [ ] **P3.5** - Crear controlador API

### Fase 4: Integración de Créditos (1-2 días)

- [ ] **P4.1** - Agregar métodos en `AutoAccountingService` para cobros de crédito
- [ ] **P4.2** - Integrar con `CreditService` al registrar pagos
- [ ] **P4.3** - Agregar cuenta "Ingresos por Intereses" al catálogo

### Fase 5: Motor de Reglas desde BD (2 días)

- [ ] **P5.1** - Crear `IAccountingRuleRepository` y `AccountingRuleRepository`
- [ ] **P5.2** - Refactorizar `AutoAccountingService` para leer reglas desde `AccountingRules`
- [ ] **P5.3** - Crear controlador CRUD para reglas
- [ ] **P5.4** - Agregar seed de reglas por defecto

### Fase 6: Reportes y Cierre Contable (2 días)

- [ ] **P6.1** - Agregar generación de asiento de cierre de período
- [ ] **P6.2** - Agregar reporte de libro diario
- [ ] **P6.3** - Optimizar `GetTrialBalanceAsync()` con consultas agregadas
- [ ] **P6.4** - Agregar validación de balance (Total Débitos = Total Créditos)

---

## RESUMEN DE ACCIONES PRIORIZADAS

| Prioridad | Acción | Esfuerzo | Impacto |
|---|---|---|---|
| 🔴 CRÍTICA | Integrar AutoAccountingService en SaleService | 4 hrs | Alto |
| 🔴 CRÍTICA | Integrar AutoAccountingService en PurchaseService | 2 hrs | Alto |
| 🔴 CRÍTICA | Integrar AutoAccountingService en InventoryMovementService | 2 hrs | Alto |
| 🟡 ALTA | Implementar costeo promedio ponderado | 4 hrs | Alto |
| 🟡 ALTA | Agregar Notas de Crédito (Ventas/Compras) | 8 hrs | Alto |
| 🟡 ALTA | Integrar pagos de créditos con contabilidad | 4 hrs | Medio |
| 🟢 MEDIA | Refactorizar AutoAccountingService con reglas desde BD | 8 hrs | Medio |
| 🟢 MEDIA | Optimizar balanza de comprobación | 2 hrs | Bajo |
| 🟢 MEDIA | Agregar índices para performance | 1 hr | Medio |

---

## CONCLUSIÓN

**Zorvian ERP tiene una base sólida** con un módulo de contabilidad bien diseñado, que incluye catálogo de cuentas, vinculación contable, libro diario, libro mayor, balanza de comprobación y estados financieros.

**Sin embargo, existe una falla crítica de integración:** El generador automático de asientos (`AutoAccountingService`) nunca es invocado desde los servicios operativos (Ventas, Compras, Inventario). Esto significa que actualmente **NINGÚN movimiento operativo genera asientos contables**, dejando la contabilidad vacía.

**El resto del diseño es correcto** y las correcciones necesarias son principalmente de orquestación (inyectar dependencias y llamar a los métodos existentes), no de rediseño arquitectónico.