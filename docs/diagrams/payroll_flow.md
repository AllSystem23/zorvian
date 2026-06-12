# Flujo de Nómina Completo

**Zorvian ERP** — Módulo de Nómina y Compensaciones

---

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#1A0A3E', 'primaryTextColor': '#fff', 'primaryBorderColor': '#E040FB', 'lineColor': '#E040FB', 'secondaryColor': '#9C27B0', 'tertiaryColor': '#141838'}}}%%

graph TB
  subgraph Inputs["📥 Datos de Entrada"]
    EMP["Colaboradores<br/>Salario + Tipo + Banco"]
    ATT["Asistencia<br/>Días laborados + Ausencias"]
    PERM["Permisos<br/>Enfermedad, Maternidad, etc."]
    VAC["Vacaciones<br/>Días tomados en el período"]
    BENEF["Beneficios<br/>Vales, Seguros, Bonos"]
    DED["Deducciones Fijas<br/>Préstamos, Embargos"]
  end

  subgraph Calculation["🧮 Cálculo de Nómina"]
    GROSS["Salario Bruto<br/>Salario Base + Horas Extra + Bonos"]
    INSS_LAB["INSS Laboral<br/>7% (Nicaragua)"]
    INSS_PAT["INSS Patronal<br/>21.5% (Nicaragua)"]
    IR["IR (Impuesto Renta)<br/>Tabla progresiva"]
    OTROS["Otras Deducciones<br/>Préstamos, Embargos"]
    NETO["Salario Neto<br/>Bruto - Deducciones"]
  end

  subgraph Outputs["📤 Salidas"]
    PAYSLIP["Colilla de Pago<br/>PDF Individual"]
    REPORT["Reporte de Nómina<br/>Resumen por período"]
    ACH["Archivo ACH<br/>Transferencia Bancaria"]
    ACCOUNTING["Asiento Contable<br/>Automático"]
    INSS_RPT["Reporte INSS<br/>Patronal + Laboral"]
    IR_RPT["Reporte IR<br/>Retenciones"]
  end

  subgraph Accounting["🧾 Contabilización"]
    DEBE1["DEBE: Gasto Salarios<br/>Bruto Total"]
    DEBE2["DEBE: INSS Patronal<br/>Aporte Patrono"]
    HABER1["HABER: INSS Laboral x Pagar"]
    HABER2["HABER: IR x Pagar"]
    HABER3["HABER: Bancos (Neto)"]
    HABER4["HABER: Prestamos x Pagar"]
  end

  subgraph Legal["⚖️ Cumplimiento Legal"]
    NI["🇳🇮 Nicaragua<br/>INSS 7%+21.5%, IR progresivo"]
    CR["🇨🇷 Costa Rica<br/>CCSS 9.17%+26%, Renta"]
    GT["🇬🇹 Guatemala<br/>IGSS 4.83%+10.67%, ISR"]
    HN["🇭🇳 Honduras<br/>IHSS 3.5%+7.5%, ISR"]
    SV["🇸🇻 El Salvador<br/>ISSS 3%+7.5%, AFP, ISR"]
    PA["🇵🇦 Panamá<br/>CSS 9.75%+12.25%, ISR"]
  end

  Inputs --> GROSS
  ATT --> GROSS
  PERM --> GROSS
  VAC --> GROSS
  BENEF --> GROSS
  GROSS --> INSS_LAB
  GROSS --> INSS_PAT
  INSS_LAB --> IR
  IR --> OTROS
  OTROS --> NETO

  NETO --> PAYSLIP
  NETO --> ACH
  NETO --> REPORT
  GROSS --> ACCOUNTING
  INSS_LAB --> INSS_RPT
  INSS_PAT --> INSS_RPT
  IR --> IR_RPT

  ACCOUNTING --> DEBE1
  ACCOUNTING --> DEBE2
  ACCOUNTING --> HABER1
  ACCOUNTING --> HABER2
  ACCOUNTING --> HABER3
  ACCOUNTING --> HABER4

  Calculation --> Legal
```

---

## Flujo de Cálculo de Nómina (Secuencia)

```mermaid
sequenceDiagram
    actor RH as RRHH
    participant S as Sistema
    participant P as Proveedor ACH
    participant C as Contabilidad

    Note over RH,C: Día 1-5 del mes
    RH->>S: Selecciona período de nómina
    S->>S: Valida asistencia y permisos del período
    S->>S: Calcula salario bruto (base + horas extra + bonos)
    S->>S: Aplica deducciones INSS/IR según país
    S->>S: Calcula salario neto por empleado
    
    RH->>S: Revisa resultados
    alt Errores encontrados
        RH->>S: Corrige datos
        S->>S: Recalcula
    else Todo correcto
        RH->>S: Aprueba nómina
    end
    
    S->>P: Genera y envía archivo ACH
    P-->>S: Confirmación de envío
    
    S->>C: Genera asiento contable automático
    S->>S: Actualiza saldos de vacaciones y permisos
    S->>RH: Envía colillas de pago a empleados
    S->>RH: Genera reporte INSS/IR para declaración
```

---

## Estados de la Nómina

```mermaid
stateDiagram-v2
    [*] --> Draft : Crear período
    Draft --> Calculating : Iniciar cálculo
    Calculating --> Completed : Cálculo exitoso
    Calculating --> Error : Error en cálculo
    Error --> Calculating : Recalcular
    Completed --> Reviewed : RRHH revisa
    Reviewed --> Approved : Aprobada
    Reviewed --> Draft : Devolver con correcciones
    Approved --> Paid : Pago ACH enviado
    Paid --> Posted : Asiento contabilizado
    Posted --> [*]
```

---

## Ejemplo: Cálculo de Nómina Nicaragua

| Concepto | Empleado A | Empleado B |
|----------|:----------:|:----------:|
| Salario Base | $1,200.00 | $800.00 |
| Horas Extra | $150.00 | $0.00 |
| Bono | $100.00 | $50.00 |
| **Salario Bruto** | **$1,450.00** | **$850.00** |
| INSS Laboral (7%) | -$101.50 | -$59.50 |
| IR Progresivo | -$164.25 | -$0.00 |
| Préstamo | -$50.00 | -$0.00 |
| **Salario Neto** | **$1,134.25** | **$790.50** |
| INSS Patronal (21.5%) | $311.75 | $182.75 |
| **Costo Total Empresa** | **$1,761.75** | **$1,032.75** |

---

## KPIs del Módulo de Nómina

| KPI | Fórmula | Objetivo |
|-----|---------|:--------:|
| Tiempo de Proceso | Días desde cierre hasta pago | < 3 días |
| Precisión | % nóminas sin correcciones | > 99% |
| Automatización | % de asientos generados automáticamente | 100% |
| Costo por Colilla | Costo operativo / # empleados | < $0.50 |
| Cumplimiento Legal | % de declaraciones a tiempo | 100% |
