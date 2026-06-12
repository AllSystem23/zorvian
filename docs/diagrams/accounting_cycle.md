# Ciclo Contable Completo

**Zorvian ERP** — Módulo de Contabilidad

---

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#1A0A3E', 'primaryTextColor': '#fff', 'primaryBorderColor': '#1B5E20', 'lineColor': '#2E7D32', 'secondaryColor': '#1B5E20', 'tertiaryColor': '#141838'}}}%%

graph TB
  subgraph Sources["📥 Fuentes de Asientos"]
    SALES["Ventas<br/>Facturación Electrónica"]
    PURCHASES["Compras<br/>Órdenes + Facturas Proveedor"]
    PAYROLL["Nómina<br/>Cálculo INSS/IR"]
    TREASURY["Tesorería<br/>Cobros + Pagos"]
    CASH["Caja<br/>Arqueos + Movimientos"]
    CREDIT["Créditos<br/>Desembolsos + Cobranza"]
    FIXED["Activos Fijos<br/>Depreciación + Bajas"]
    MANUAL["Asientos Manuales<br/>Ajustes + Provisiones"]
    AUTO["Auto Accounting<br/>Reglas Automáticas IA"]
  end

  subgraph Journal["📒 Diario General"]
    JE["Asiento Contable<br/>Debe / Haber"]
    VAL["Validación<br/>Cuadre + Centro Costo"]
    APPROVE["Aprobación<br/>Contador / Supervisor"]
    POST["Contabilización<br/>Aplica a Mayor"]
  end

  subgraph Ledger["📊 Mayor General"]
    GL["Mayor General<br/>Cuentas T"]
    SUBLEDGER["Sub-Mayores<br/>Clientes, Proveedores, Bancos"]
    TRIAL_BALANCE["Balance de Comprobación<br/>Sumas y Saldos"]
    ADJUST["Ajustes<br/>Diferencia Cambiaria, Devengado"]
  end

  subgraph Statements["📑 Estados Financieros"]
    BALANCE["Balance General<br/>Activo = Pasivo + Capital"]
    INCOME["Estado de Resultados<br/>Ingresos - Gastos = Utilidad"]
    CASH_FLOW["Flujo de Efectivo<br/>Operativo / Inversión / Financiamiento"]
    EQUITY["Estado de Cambios<br/>en el Patrimonio"]
  end

  subgraph Close["🔒 Cierre Contable"]
    CLOSE_TEMP["Cierre de Cuentas<br/>de Resultado"]
    RETAINED["Cálculo Utilidad<br/>del Ejercicio"]
    LOCK["Bloqueo de Período<br/>Solo Lectura"]
    NEXT["Apertura Nuevo<br/>Período Contable"]
  end

  subgraph Reports["📋 Reportes Fiscales"]
    DGI["Declaración DGI<br/>IVA + ISR"]
    INSS["Planilla INSS<br/>Patronal + Laboral"]
    ALD["Libros Contables<br/>Diario + Mayor + Balance"]
    AUDIT_TRAIL["Pista de Auditoría<br/>Trazabilidad completa"]
  end

  %% Flujo Principal
  Sources --> JE
  JE --> VAL
  VAL --> APPROVE
  APPROVE --> POST
  POST --> GL
  GL --> SUBLEDGER
  GL --> TRIAL_BALANCE
  SUBLEDGER --> TRIAL_BALANCE
  TRIAL_BALANCE --> ADJUST
  ADJUST --> TRIAL_BALANCE

  TRIAL_BALANCE --> BALANCE
  TRIAL_BALANCE --> INCOME
  TRIAL_BALANCE --> CASH_FLOW
  TRIAL_BALANCE --> EQUITY

  INCOME --> CLOSE_TEMP
  CLOSE_TEMP --> RETAINED
  RETAINED --> LOCK
  LOCK --> NEXT
  NEXT --> Sources

  Statements --> Reports
```

---

## Políticas Contables por País

| País | Moneda | IVA | ISR | Frecuencia |
|------|--------|:---:|:---:|:-----------|
| 🇳🇮 Nicaragua | NIO / USD | 15% | 30% | Mensual |
| 🇨🇷 Costa Rica | CRC | 13% | 30% | Mensual |
| 🇬🇹 Guatemala | GTQ | 12% | 25% | Mensual |
| 🇭🇳 Honduras | HNL | 15% | 25% | Mensual |
| 🇸🇻 El Salvador | USD | 13% | 30% | Mensual |
| 🇵🇦 Panamá | USD | 7% (ITBMS) | 25% | Trimestral |

---

## Catálogo de Cuentas (Estructura)

```
1-00-000  ACTIVO
│
├── 1-01-000  Activo Corriente
│   ├── 1-01-001  Efectivo y Equivalentes
│   ├── 1-01-002  Bancos
│   ├── 1-01-003  Cuentas por Cobrar
│   └── 1-01-004  Inventarios
│
├── 1-02-000  Activo No Corriente
│   ├── 1-02-001  Propiedad, Planta y Equipo
│   └── 1-02-002  Activos Intangibles
│
2-00-000  PASIVO
│
├── 2-01-000  Pasivo Corriente
└── 2-02-000  Pasivo No Corriente

3-00-000  PATRIMONIO
4-00-000  INGRESOS
5-00-000  COSTOS
6-00-000  GASTOS
7-00-000  CUENTAS DE ORDEN
```

---

## Ejemplo: Asiento Automático de Venta

```mermaid
flowchart LR
    VENTA["Venta: $1,000<br/>IVA 15%"] 
    --> ASIENTO["Asiento Contable"]
    
    subgraph Debe["DEBE"]
        D1["Bancos/Clientes: $1,150"]
        D2["Costo de Venta: $600"]
    end
    
    subgraph Haber["HABER"]
        H1["Ventas: $1,000"]
        H2["IVA Débito: $150"]
        H3["Inventario: $600"]
    end
    
    ASIENTO --> Debe
    ASIENTO --> Haber
```

---

## KPIs del Módulo Contable

| KPI | Descripción | Objetivo |
|-----|-------------|:--------:|
| Tiempo de Cierre Mensual | Días para cerrar mes contable | < 5 días |
| Automatización | % de asientos generados automáticamente | > 80% |
| Precisión | % de asientos sin correcciones | > 98% |
| Diferencia Cambiaria | % de exposición cambiaria monitoreada | 100% |
| Conciliación Bancaria | % de cuentas conciliadas mensualmente | 100% |
