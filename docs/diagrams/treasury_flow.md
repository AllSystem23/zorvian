# Flujo de Tesorería y Gestión de Caja

**Zorvian ERP** — Módulo de Tesorería

---

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#1A0A3E', 'primaryTextColor': '#fff', 'primaryBorderColor': '#2E7D32', 'lineColor': '#2E7D32', 'secondaryColor': '#1B5E20', 'tertiaryColor': '#141838'}}}%%

graph TB
  subgraph Inflows["💰 Ingresos"]
    SALES_COLLECT["Cobro a Clientes<br/>Facturas + Contado"]
    CREDIT_COLLECT["Cobranza Créditos<br/>Cuotas + Mora"]
    CASH_SALES["Ventas de Contado<br/>POS + Caja"]
    OTHER_IN["Otros Ingresos<br/>Notas de Crédito, Ajustes"]
  end

  subgraph Outflows["💸 Egresos"]
    PROVIDER_PAY["Pago a Proveedores<br/>Facturas + Órdenes"]
    PAYROLL_PAY["Pago Nómina<br/>Salarios + Deducciones"]
    TAX_PAY["Pago Impuestos<br/>DGI + INSS + Municipales"]
    OTHER_OUT["Otros Egresos<br/>Gastos Varios, Préstamos"]
  end

  subgraph CashRegisters["🪙 Cajas"]
    CR_MAIN["Caja Principal<br/>Central / Oficina"]
    CR_BRANCH["Cajas Sucursales<br/>Por sucursal"]
    CR_POS["Cajas POS<br/>Punto de Venta"]
    CR_ACTIVE["Caja Activa<br/>Turno Actual"]
  end

  subgraph Bank["🏦 Bancos"]
    BC["Cuentas Bancarias<br/>Múltiples Monedas"]
    CHECKS["Chequeras<br/>Cheques Emitidos"]
    CB["Conciliación Bancaria<br/>Automática"]
    RECON["Diferencias<br/>Ajustes Automáticos"]
  end

  subgraph Treasury["📊 Gestión de Tesorería"]
    CASH_FLOW["Flujo de Caja<br/>Proyectado vs Real"]
    BUDGET["Ejecución Presupuestaria<br/>Ingreso vs Gasto"]
    FORECAST["Pronóstico<br/>IA de Tesorería"]
    ALERTS["Alertas<br/>Saldo Mínimo, Mora"]
  end

  subgraph Reports["📋 Reportes"]
    DAILY["Arqueo Diario<br/>Caja Chica"]
    MONTHLY["Estado de Cuenta<br/>Bancos Mensual"]
    CASH_POS["Posición de Caja<br/>General Consolidado"]
    AGING["Antigüedad de Saldos<br/>CxC + CxP"]
  end

  %% Flujo
  Inflows --> CashRegisters
  Inflows --> Bank
  Outflows --> CashRegisters
  Outflows --> Bank
  CashRegisters --> Treasury
  Bank --> Treasury
  Treasury --> Reports
  Treasury --> Budget

  %% Conciliación
  Bank --> CB
  CashRegisters --> CB
  CB --> RECON
  RECON --> Treasury
```

---

## Estados de Cuentas Bancarias

```mermaid
stateDiagram-v2
    [*] --> Active : Cuenta abierta
    Active --> Frozen : Bloqueo judicial
    Active --> Dormant : Sin movimiento 6m
    Frozen --> Active : Desbloqueo
    Dormant --> Active : Transacción
    Active --> Closed : Cierre
    Frozen --> Closed : Cierre forzoso
    Closed --> [*]
```

---

## Flujo de Pago a Proveedores

```mermaid
sequenceDiagram
    actor AP as Aprobador
    actor FIN as Finanzas
    participant S as Sistema
    participant B as Banco
    participant PROV as Proveedor

    Note over AP,PROV: Día 1 — Factura Recibida
    FIN->>S: Registra factura proveedor
    S->>S: Valida datos fiscales
    
    Note over AP,PROV: Día 1-5 — Aprobación
    S->>AP: Notifica aprobación pendiente
    AP->>S: Aprueba factura
    S->>S: Programa pago (fecha compromiso)
    
    Note over AP,PROV: Día compromiso — Pago
    FIN->>S: Inicia lote de pagos
    S->>S: Valita saldo disponible
    alt Saldo Suficiente
        S->>B: Archivo ACH / Transferencia
        B-->>S: Confirmación
        S->>S: Registra pago + asiento contable
        S->>PROV: Notificación email/WhatsApp
    else Saldo Insuficiente
        S->>FIN: Alerta saldo insuficiente
        FIN->>S: Reprograma fecha o cancela
    end
```

---

## Conciliación Bancaria Automática

| Paso | Descripción | Automatización |
|------|-------------|:--------------:|
| 1 | Exportar movimientos bancarios (CSV/OFX) | ✅ Importación automática |
| 2 | Matching automático: monto + fecha + referencia | ✅ Algoritmo de fuzzy matching |
| 3 | Identificar diferencias (cheques no cobrados, cargos no registrados) | ✅ Automático |
| 4 | Sugerir ajustes automáticos | ✅ IA sugiere asientos |
| 5 | Generar reporte de conciliación | ✅ PDF/Excel automático |
| 6 | Cerrar período conciliado | ✅ Bloqueo de modificaciones |

---

## KPIs del Módulo de Tesorería

| KPI | Fórmula | Objetivo |
|-----|---------|:--------:|
| Días de Cobro (DSO) | (CxC / Ventas) × 30 | < 45 días |
| Días de Pago (DPO) | (CxP / Compras) × 30 | > 30 días |
| Ciclo de Efectivo | DSO - DPO | < 15 días |
| Precisión de Pronóstico | (Pronóstico / Real) × 100 | > 85% |
| Cobertura de Liquidez | (Efectivo + CxC) / (Pasivo Corto Plazo) | > 1.2x |
| % Conciliación Automática | (Match auto / Total transacciones) × 100 | > 90% |
