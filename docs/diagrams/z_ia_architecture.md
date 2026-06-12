# Z-IA: Arquitectura de Inteligencia Artificial

**Zorvian ERP** — Módulo de IA Empresarial

---

## Visión General

Z-IA es el ecosistema de inteligencia artificial de Zorvian ERP, compuesto por cuatro capacidades principales: **Procesamiento de Lenguaje Natural (RAG)**, **Visión por Computadora (OCR)**, **Machine Learning Predictivo** y **Motor de Reglas Inteligente**.

---

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#1A0A3E', 'primaryTextColor': '#fff', 'primaryBorderColor': '#B388FF', 'lineColor': '#7C4DFF', 'secondaryColor': '#2D1B69', 'tertiaryColor': '#141838', 'clusterBkg': '#0A0E27', 'clusterBorder': '#2D1B69'}}}%%

graph TB
  subgraph Users["👤 Usuarios"]
    CHAT["Chat Interface<br/>Web + Mobile"]
    MOBILE_OCR["Mobile Camera<br/>Document Capture"]
    DASH["BI Dashboards<br/>Predictions View"]
  end

  subgraph ZIA["🤖 Z-IA Core Platform"]
    direction TB
    
    subgraph NLP["🧠 NLP & Conversational"]
      LLM["LLM Orchestrator<br/>Vertex AI / GPT"]
      EMB["Embeddings Service<br/>text-embedding-004"]
      VDB[("Vector Database<br/>pgvector / Pinecone")]
      RAG["RAG Pipeline<br/>Retrieval-Augmented Gen"]
      CTX["Context Builder<br/>Tenant + Role + History"]
    end

    subgraph CV["👁️ Computer Vision"]
      OCR["OCR Engine<br/>Google Vision + Tesseract"]
      DOC_CLASS["Document Classifier<br/>Invoice, Contract, ID"]
      VALID["Data Extractor<br/>Regex + ML NER"]
      FACE["Face Recognition<br/>(Future)"]
    end

    subgraph ML["📊 ML Prediction Engine"]
      FEAT["Feature Store<br/>Redis + PostgreSQL"]
      TRAIN["Model Training<br/>ML.NET / XGBoost"]
      PRED["Prediction API<br/>REST gRPC"]
      EXPLAIN["SHAP Explainability<br/>Why this prediction?"]
    end

    subgraph RULES["⚙️ Intelligent Rules"]
      AUTO["Auto Accounting<br/>Rule Engine"]
      SLA["SLA Monitor<br/>Warranty Deadlines"]
      ALERT["Alert Engine<br/>Anomaly Detection"]
      RECO["Recommender<br/>Optimal Dates Engine"]
    end
  end

  subgraph DataSources["📡 Data Sources"]
    ERP["ERP Events Bus<br/>RabbitMQ"]
    PG[("PostgreSQL<br/>Transactional Data")]
    DOCS[("Document Store<br/>GCS / S3")]
    AUDIT[("Audit Logs<br/>Elasticsearch")]
  end

  subgraph Integrations["🔗 Integrations"]
    WHATSAPP["WhatsApp Bot<br/>Chat API"]
    EMAIL["Email Processor<br/>Inbound/Outbound"]
    WEBHOOK["Webhook Outbound<br/>Customer Systems"]
    API_REST["REST API<br/>Z-IA Endpoints"]
  end

  %% NLP Flow
  CHAT --> LLM
  LLM --> RAG
  RAG --> VDB
  RAG --> EMB
  RAG --> CTX
  CTX --> ERP
  CTX --> PG

  %% OCR Flow
  MOBILE_OCR --> OCR
  OCR --> DOC_CLASS
  DOC_CLASS --> VALID
  VALID --> ERP
  VALID --> DOCS

  %% ML Flow
  ERP --> FEAT
  PG --> FEAT
  FEAT --> TRAIN
  TRAIN --> PRED
  PRED --> EXPLAIN
  EXPLAIN --> DASH
  PRED --> ALERT

  %% Rules
  ERP --> RULES
  RULES --> AUTO
  RULES --> SLA
  RULES --> ALERT
  RULES --> RECO

  %% Integrations
  ZIA --> WHATSAPP
  ZIA --> EMAIL
  ZIA --> WEBHOOK
  ZIA --> API_REST
```

---

## Componentes

### 1. NLP & Conversacional (Chatbot Z-IA)

| Componente | Tecnología | Propósito |
|------------|-----------|-----------|
| LLM Orchestrator | Vertex AI / OpenAI GPT | Orquestación de respuestas con contexto multi-turno |
| Embeddings Service | Google text-embedding-004 | Vectorización de documentos y consultas |
| Vector Database | pgvector (PostgreSQL) | Almacenamiento y búsqueda semántica de embeddings |
| RAG Pipeline | LangChain / Custom C# | Retrieval-Augmented Generation con contexto de tenant |
| Context Builder | ASP.NET Core | Construye contexto empresarial (rol, tenant, historial) |

### 2. Computer Vision (OCR)

| Componente | Tecnología | Propósito |
|------------|-----------|-----------|
| OCR Engine | Google Vision API + Tesseract | Reconocimiento de texto en documentos escaneados |
| Document Classifier | ML.NET Multiclass | Clasifica tipo de documento (factura, cédula, contrato) |
| Data Extractor | Regex + CRF | Extrae campos clave (nombre, monto, fecha, Cédula RUC) |

### 3. ML Predictivo

| Modelo | Algoritmo | Features | Output |
|--------|-----------|----------|--------|
| Predicción Ausentismo | XGBoost | Historial, día, depto, antigüedad | Score 0-100 por empleado |
| Predicción Ventas | Prophet / SARIMA | Historial ventas, estacionalidad, promociones | Proyección semanal/mensual |
| Clasificación Gastos | Random Forest | Categoría, monto, proveedor, departamento | Categoría automática |
| Riesgo Rotación | XGBoost + SHAP | Antigüedad, ausentismo, horas extra, cambios salariales | Score + top 3 factores |

### 4. Motor de Reglas Inteligente

| Regla | Disparador | Acción |
|-------|-----------|--------|
| Auto Accounting | Venta / Nómina / Compra | Genera asiento contable automático |
| SLA Warranty | Registro de garantía | Calcula deadline, alerta antes de vencer |
| Anomaly Detection | Transacción inusual | Alerta a administrador |
| Optimal Dates | Solicitud de vacaciones | Sugiere fechas óptimas según ocupación del equipo |

---

## Flujo de Consulta (RAG)

```mermaid
sequenceDiagram
    actor U as Usuario
    participant C as Chat UI
    participant O as Orchestrator
    participant R as RAG Pipeline
    participant V as Vector DB
    participant E as ERP Context
    participant L as LLM

    U->>C: "¿Cuántas vacaciones me quedan?"
    C->>O: User query + tenant_id
    O->>R: Retrieve context
    R->>V: Semantic search (query embedding)
    V-->>R: Relevant docs chunks
    R->>E: Fetch user data (saldo, role)
    E-->>R: { availableDays: 7.5, role: employee }
    R->>L: Prompt = query + chunks + context
    L-->>R: "Te quedan 7.5 días. ¿Quieres solicitar?"
    R-->>O: Formatted response
    O-->>C: Display answer + suggested actions
    C->>U: "Te quedan 7.5 días. ¿Solicitar ahora? [Sí] [No]"
```

---

## Arquitectura de Predicción (ML Pipeline)

```mermaid
graph LR
    RAW[(Raw Data<br/>PostgreSQL)] --> FEAT[Feature Engineering<br/>Python / ML.NET]
    FEAT --> STORE[(Feature Store<br/>Redis)]
    STORE --> TRAIN[Model Training<br/>XGBoost / Prophet]
    TRAIN --> REG[(Model Registry<br/>MLflow)]
    REG --> API[Prediction API<br/>REST Endpoint]
    API --> CACHE[(Prediction Cache<br/>Redis TTL 1h)]
    CACHE --> DASH[Dashboard BI]
    API --> ALERT[Alert Engine<br/>if score > 80%]
    NEW[New Data<br/>Stream] --> FEAT
```

---

## Stack Tecnológico Z-IA

| Capa | Tecnología | Estado |
|------|-----------|--------|
| LLM Orchestration | Vertex AI (Gemini) / OpenAI | ✅ |
| Embeddings | Google text-embedding-004 | ✅ |
| Vector DB | pgvector (PostgreSQL 16) | 🟡 En progreso |
| OCR | Google Cloud Vision + Tesseract | ✅ |
| ML Training | ML.NET + Python (XGBoost) | ✅ |
| Feature Store | Redis + PostgreSQL | 🟡 En progreso |
| Model Registry | MLflow / Custom | 🟡 En progreso |
| Explainability | SHAP (Python) | 📅 Q4 2026 |
| Chat UI | Flutter (Z-IA Chat Widget) | ✅ |
| WhatsApp Bot | WhatsApp Business API | 📅 Q1 2027 |

---

## KPIs del Módulo Z-IA

| KPI | Objetivo | Actual |
|-----|----------|--------|
| Latencia RAG promedio | < 2s | 1.8s |
| Precisión OCR | > 95% | 93% |
| Precisión predicción ausentismo | > 85% | 82% |
| Tasa de resolución automática chat | > 70% | 65% |
| Tiempo de entrenamiento ML | < 30 min | 25 min |
