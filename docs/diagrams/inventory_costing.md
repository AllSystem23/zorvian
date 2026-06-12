# Kardex y Costeo de Inventario

**Zorvian ERP** — Módulo de Inventario

---

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#1A0A3E', 'primaryTextColor': '#fff', 'primaryBorderColor': '#FF8F00', 'lineColor': '#FF8F00', 'secondaryColor': '#E65100', 'tertiaryColor': '#141838'}}}%%

graph TB
  subgraph Inputs["📥 Entradas de Inventario"]
    PURCHASE["Compra a Proveedor<br/>Factura + Orden de Compra"]
    TRANSFER_IN["Transferencia<br/>Desde otra sucursal"]
    RETURN["Devolución de Cliente<br/>Nota de Crédito"]
    ADJ_IN["Ajuste de Entrada<br/>Sobrante / Inventario Inicial"]
    PRODUCTION["Producción<br/>Producto Terminado"]
  end

  subgraph Kardex["📊 Kardex (Control de Inventario)"]
    direction TB
    subgraph Methods["Métodos de Costeo"]
      AVG["Promedio Ponderado<br/>Costo = (Inv.Inicial + Compras) / Unidades"]
      FIFO["PEPS (FIFO)<br/>Primeras entradas, primeras salidas"]
      SPEC["Costo Específico<br/>Por lote / serial / IMEI"]
    end
    CARD["Tarjeta Kardex<br/>Fecha / Concepto / Entrada / Salida / Saldo"]
  end

  subgraph Outputs["📤 Salidas de Inventario"]
    SALE["Venta a Cliente<br/>Factura + Salida"]
    TRANSFER_OUT["Transferencia<br/>Hacia otra sucursal"]
    RETURN_SUP["Devolución a Proveedor<br/>Nota de Débito"]
    ADJ_OUT["Ajuste de Salida<br/>Merma / Robo / Vencimiento"]
    WARRANTY["Garantía<br/>Repuesto utilizado"]
  end

  subgraph Costing["💰 Costeo"]
    COGS["Costo de Venta (COGS)<br/>Calculado al facturar"]
    LANDED["Costo Landing<br/>Flete + Seguro + Arancel"]
    MARGIN["Margen Bruto<br/>Venta - COGS"]
    VALUATION["Valoración Inventario<br/>Balance General"]
  end

  subgraph Reports["📋 Reportes"]
    KARDEX_RPT["Kardex Detallado<br/>Por producto + período"]
    AGING_RPT["Rotación de Inventario<br/>Días en almacén"]
    COMP_RPT["Composición Inventario<br/>Por categoría / sucursal"]
    COST_RPT["Análisis de Costos<br/>Vs. Período anterior"]
  end

  %% Flujo
  Inputs --> Kardex
  Outputs --> Kardex
  Kardex --> Costing
  Costing --> Reports
```

---

## Estados del Producto en Inventario

```mermaid
stateDiagram-v2
    [*] --> Active : Producto creado
    Active --> LowStock : Stock < mínimo
    Active --> OutOfStock : Stock = 0
    Active --> Discontinued : Descontinuado
    LowStock --> Active : Reabastecido
    LowStock --> OutOfStock : Stock = 0
    OutOfStock --> Active : Nueva compra
    Discontinued --> [*]
    Active --> Damaged : Dañado / Merma
    Damaged --> [*] : Baja
```

---

## Ejemplo: Kardex con Promedio Ponderado

| Fecha | Concepto | Entrada Qty | Entrada $ | Salida Qty | Salida $ | Saldo Qty | Saldo $ | Costo Prom. |
|------|----------|:-----------:|:---------:|:----------:|:--------:|:---------:|:-------:|:-----------:|
| 01/06 | Inventario Inicial | 10 | $500.00 | — | — | 10 | $500.00 | $50.00 |
| 05/06 | Compra | 20 | $1,200.00 | — | — | 30 | $1,700.00 | $56.67 |
| 10/06 | Venta | — | — | 15 | $850.05 | 15 | $849.95 | $56.67 |
| 15/06 | Compra | 10 | $650.00 | — | — | 25 | $1,499.95 | $60.00 |
| 20/06 | Venta | — | — | 20 | $1,200.00 | 5 | $299.95 | $60.00 |
| 30/06 | Ajuste (Merma) | — | — | 1 | $60.00 | 4 | $239.95 | $60.00 |

---

## Integración Contable (Asientos Automáticos)

```mermaid
flowchart LR
    INVOICE["Factura de Venta"] --> COGS_ENTRY["Asiento COGS"]
    COGS_ENTRY --> DEBE1["DEBE: Costo de Venta<br/>$850.05"]
    COGS_ENTRY --> HABER1["HABER: Inventario<br/>$850.05"]
    
    PURCH["Factura de Compra"] --> INV_ENTRY["Asiento Compra"]
    INV_ENTRY --> DEBE2["DEBE: Inventario<br/>$1,200.00"]
    INV_ENTRY --> HABER2["HABER: Proveedores<br/>$1,200.00"]
    
    ADJ["Ajuste por Merma"] --> ADJ_ENTRY["Asiento Ajuste"]
    ADJ_ENTRY --> DEBE3["DEBE: Gasto Merma<br/>$60.00"]
    ADJ_ENTRY --> HABER3["HABER: Inventario<br/>$60.00"]
```

---

## Reglas de Negocio de Inventario

| Regla | Descripción | Automatización |
|-------|-------------|:--------------:|
| Stock Mínimo | Alerta cuando stock < mínimo definido | ✅ Automático |
| Lote Económico | Sugiere cantidad óptima de compra | ✅ IA |
| Vencimiento | Alerta 30 días antes de vencimiento | ✅ Automático |
| Costeo Automático | COGS calculado al momento de la venta | ✅ Automático |
| Transferencia | Cambio de costo si cambia sucursal | ✅ Según método |
| Garantía | Producto en garantía → inventario especial | ✅ Automático |

---

## KPIs del Módulo de Inventario

| KPI | Fórmula | Objetivo |
|-----|---------|:--------:|
| Rotación de Inventario | COGS / Inventario Promedio | > 6x año |
| Días en Almacén | 365 / Rotación | < 60 días |
| Exactitud de Inventario | (Stock Físico / Stock Sistema) × 100 | > 98% |
| Merma | (Valor Merma / COGS) × 100 | < 1% |
| Stockout Rate | Días sin stock / Días del período | < 2% |
| Cobertura de Stock | Stock Actual / Venta Diaria Promedio | 30-60 días |
