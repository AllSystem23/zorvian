# CRM: Pipeline Comercial Completo

**Zorvian ERP** — Módulo CRM y Ventas

---

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#1A0A3E', 'primaryTextColor': '#fff', 'primaryBorderColor': '#00BCD4', 'lineColor': '#00BCD4', 'secondaryColor': '#006064', 'tertiaryColor': '#141838'}}}%%

graph TB
  subgraph Inbound["📥 Captura de Leads"]
    WEB["Web Forms<br/>Landing Pages"]
    REF["Referidos<br/>Clientes Existentes"]
    MANUAL["Carga Manual<br/>Ejecutivo Comercial"]
    IMPORT["Importación Masiva<br/>Excel / CSV"]
    WHATSAPP["WhatsApp<br/>Chat Inbound"]
  end

  subgraph Qualification["🔍 Calificación"]
    AUTO_SCORE["Auto Scoring<br/>IA Lead Scoring"]
    ASSIGN["Asignación Automática<br/>Round-Robin / Skills"]
    FIRST_CONTACT["Primer Contacto<br/>Email + WhatsApp"]
  end

  subgraph Pipeline["📊 Pipeline de Ventas"]
    direction TB
    subgraph Stages["Etapas"]
      NEW["🆕 Nuevo<br/>Lead sin contactar"]
      CONTACTED["📞 Contactado<br/>Primer contacto realizado"]
      QUALIFIED["🎯 Calificado<br/>Presupuesto + Need + Timeline"]
      PROPOSAL["📄 Cotización<br/>Propuesta enviada"]
      NEGOTIATION["🤝 Negociación<br/>Términos finales"]
      CLOSED_WON["✅ Cerrado Ganado<br/>Cliente activo"]
      CLOSED_LOST["❌ Cerrado Perdido<br/>No concretado"]
    end

    NEW --> CONTACTED
    CONTACTED --> QUALIFIED
    QUALIFIED --> PROPOSAL
    PROPOSAL --> NEGOTIATION
    NEGOTIATION --> CLOSED_WON
    NEGOTIATION --> CLOSED_LOST
    QUALIFIED --> CLOSED_LOST
  end

  subgraph Activities["📋 Actividades"]
    CALL["📞 Llamada"]
    EMAIL["📧 Correo"]
    MEETING["🤝 Reunión"]
    TASK["✅ Tarea"]
    NOTE["📝 Nota"]
  end

  subgraph Metrics["📈 Métricas Clave"]
    CONV_RATE["Tasa Conversión<br/>Lead → Cliente"]
    AVG_DEAL["Ticket Promedio<br/>$ por venta"]
    CYCLE["Ciclo Promedio<br/>Días para cerrar"]
    PIPELINE_VAL["Valor Pipeline<br/>$ total activo"]
  end

  subgraph Automation["⚡ Automatizaciones"]
    WELCOME["Email Bienvenida<br/>Automático"]
    FOLLOW_UP["Follow-up 24h<br/>Si no hay respuesta"]
    BIRTHDAY["Cumpleaños Cliente<br/>Oferta Personalizada"]
    WIN_BACK["Win-Back<br/>Clientes inactivos 90d"]
    NPS["NPS Survey<br/>Post-venta 7 días"]
  end

  subgraph Integration["🔗 Integración ERP"]
    QUOTE["Cotización →<br/>Módulo Ventas"]
    INVOICE["Facturación<br/>Contabilidad"]
    INVENTORY["Stock Check<br/>Inventario"]
    CREDIT["Análisis Crédito<br/>Módulo Creditos"]
  end

  %% Flujo principal
  WEB --> AUTO_SCORE
  REF --> AUTO_SCORE
  MANUAL --> AUTO_SCORE
  IMPORT --> AUTO_SCORE
  WHATSAPP --> AUTO_SCORE

  AUTO_SCORE --> ASSIGN
  ASSIGN --> FIRST_CONTACT
  FIRST_CONTACT --> NEW

  Pipeline --> Activities
  Pipeline --> Metrics
  Pipeline --> Automation
  CLOSED_WON --> Integration
```

---

## Estados del Lead y Oportunidad

```mermaid
stateDiagram-v2
    [*] --> New : Lead creado
    New --> Contacted : Primer contacto
    New --> Disqualified : No califica
    Contacted --> Qualified : Demo/Reunión
    Contacted --> Lost : No responde
    Qualified --> Proposal : Cotización enviada
    Qualified --> Lost : Rechazado en calificación
    Proposal --> Negotiation : Contrapropuesta
    Proposal --> Lost : Rechazado
    Negotiation --> ClosedWon : Aceptado
    Negotiation --> ClosedLost : Rechazado
    ClosedWon --> [*]
    ClosedLost --> [*]
    Disqualified --> [*]
    Lost --> [*]
```

---

## Pipeline Visual (Embudo de Ventas)

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#1A0A3E', 'primaryTextColor': '#fff', 'primaryBorderColor': '#00BCD4', 'lineColor': '#00BCD4'}}}%%

graph LR
    subgraph Funnel["Embudo de Ventas — Junio 2026"]
        L1["Leads<br/>🥇 100"
            style L1 fill:#00BCD4,stroke:#0097A7,color:#fff
        ]
        L2["Contactados<br/>🥈 65"
            style L2 fill:#26C6DA,stroke:#0097A7,color:#fff
        ]
        L3["Calificados<br/>🥉 40"
            style L3 fill:#4DD0E1,stroke:#0097A7,color:#fff
        ]
        L4["Cotizados<br/>🎯 25"
            style L4 fill:#80DEEA,stroke:#0097A7,color:#fff
        ]
        L5["Cerrados<br/>🏆 12"
            style L5 fill:#00E5FF,stroke:#0097A7,color:#1A0A3E
        ]
    end
    L1 --> L2
    L2 --> L3
    L3 --> L4
    L4 --> L5
```

---

## Reglas de Negocio CRM

| Regla | Descripción | Automatización |
|-------|-------------|:--------------:|
| Lead Scoring Automático | Puntúa leads por fuente, industria, cargo | ✅ IA |
| Round-Robin Assignment | Distribución equitativa entre ejecutivos | ✅ |
| Follow-up 24h | Si no hay actividad en 24h, asigna tarea automática | ✅ |
| Win-Back 90d | Clientes sin compra en 90 días → campaña automática | ✅ |
| NPS Post-Venta | Encuesta 7 días después del cierre | ✅ |
| Crédito Automático | Si el monto > $1,000, solicita análisis de crédito | ✅ |

---

## KPIs del Módulo CRM

| KPI | Fórmula | Benchmark | Zorvian Objetivo |
|-----|---------|:---------:|:----------------:|
| Tasa de Conversión | (Closed Won / Leads totales) × 100 | 15-25% | > 20% |
| Ticket Promedio | $ ventas totales / N° ventas | — | $500-$5,000 |
| Ciclo de Venta | Días desde Lead → Closed Won | 15-45 días | < 30 días |
| Lead Response Time | Tiempo hasta primer contacto | < 5 min | < 2 min |
| Pipeline Coverage | Valor pipeline / Cuota del mes | 3x | > 3.5x |
| Tasa de Ganancia | Won / (Won + Lost) × 100 | 40-60% | > 50% |
