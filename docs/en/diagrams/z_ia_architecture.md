# Z-IA: Artificial Intelligence Architecture

**Zorvian ERP** — Enterprise AI Module

---

## Overview

Z-IA is the artificial intelligence ecosystem of Zorvian ERP, composed of four main capabilities: **Natural Language Processing (RAG)**, **Computer Vision (OCR)**, **Predictive Machine Learning**, and **Intelligent Rule Engine**.

---

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#1A0A3E', 'primaryTextColor': '#fff', 'primaryBorderColor': '#B388FF', 'lineColor': '#7C4DFF', 'secondaryColor': '#2D1B69', 'tertiaryColor': '#141838', 'clusterBkg': '#0A0E27', 'clusterBorder': '#2D1B69'}}}%%

graph TB
  subgraph Users["👤 Users"]
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
    ERP["ERP Events Bus<br/>RabbitMQ + MassTransit<br/>4 Consumers"]
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

## Components

### 1. NLP & Conversational (Z-IA Chatbot)

| Component | Technology | Purpose |
|------------|-----------|-----------|
| LLM Orchestrator | Vertex AI / OpenAI GPT | Multi-turn response orchestration with context |
| Embeddings Service | Google text-embedding-004 | Document and query vectorization |
| Vector Database | pgvector (PostgreSQL) | Semantic search and storage of embeddings |
| RAG Pipeline | LangChain / Custom C# | Retrieval-Augmented Generation with tenant context |
| Context Builder | ASP.NET Core | Builds enterprise context (role, tenant, history) |

### 2. Computer Vision (OCR)

| Component | Technology | Purpose |
|------------|-----------|-----------|
| OCR Engine | Google Vision API + Tesseract | Text recognition in scanned documents |
| Document Classifier | ML.NET Multiclass | Classifies document type (invoice, ID, contract) |
| Data Extractor | Regex + CRF | Extracts key fields (name, amount, date, ID/Tax ID) |

### 3. Predictive ML

| Model | Algorithm | Features | Output |
|--------|-----------|----------|--------|
| Absenteeism Prediction | XGBoost | History, day, dept, seniority | Score 0-100 per employee |
| Sales Prediction | Prophet / SARIMA | Sales history, seasonality, promotions | Weekly/monthly projection |
| Expense Classification | Random Forest | Category, amount, provider, department | Automatic category |
| Churn Risk | XGBoost + SHAP | Seniority, absenteeism, overtime, salary changes | Score + top 3 factors |

### 4. Intelligent Rule Engine

| Rule | Trigger | Action |
|-------|-----------|--------|
| Auto Accounting | Sale / Payroll / Purchase | Generates automatic accounting entry |
| SLA Warranty | Warranty registration | Calculates deadline, alerts before expiration |
| Anomaly Detection | Unusual transaction | Alerts administrator |
| Optimal Dates | Vacation request | Suggests optimal dates based on team occupancy |

---

## Query Flow (RAG)

```mermaid
sequenceDiagram
    actor U as User
    participant C as Chat UI
    participant O as Orchestrator
    participant R as RAG Pipeline
    participant V as Vector DB
    participant E as ERP Context
    participant L as LLM

    U->>C: "How much vacation time do I have left?"
    C->>O: User query + tenant_id
    O->>R: Retrieve context
    R->>V: Semantic search (query embedding)
    V-->>R: Relevant docs chunks
    R->>E: Fetch user data (balance, role)
    E-->>R: { availableDays: 7.5, role: employee }
    R->>L: Prompt = query + chunks + context
    L-->>R: "You have 7.5 days left. Do you want to request them?"
    R-->>O: Formatted response
    O-->>C: Display answer + suggested actions
    C->>U: "You have 7.5 days left. Request now? [Yes] [No]"
```

---

## Prediction Architecture (ML Pipeline)

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

## Z-IA Tech Stack

| Layer | Technology | Status |
|------|-----------|--------|
| LLM Orchestration | Vertex AI (Gemini) / OpenAI | ✅ |
| Embeddings | Google text-embedding-004 | ✅ |
| Vector DB | pgvector (PostgreSQL 16) | 🟡 In progress |
| OCR | Google Cloud Vision + Tesseract | ✅ |
| ML Training | ML.NET + Python (XGBoost) | ✅ |
| Feature Store | Redis + PostgreSQL | 🟡 In progress |
| Model Registry | MLflow / Custom | 🟡 In progress |
| Explainability | SHAP (Python) | 📅 Q4 2026 |
| Chat UI | Flutter (Z-IA Chat Widget) | ✅ |
| WhatsApp Bot | WhatsApp Business API | 📅 Q1 2027 |

---

## Z-IA Module KPIs

| KPI | Target | Actual |
|-----|--------|--------|
| Avg RAG Latency | < 2s | 1.8s |
| OCR Accuracy | > 95% | 93% |
| Absenteeism Prediction Accuracy | > 85% | 82% |
| Chat Auto-resolution Rate | > 70% | 65% |
| ML Training Time | < 30 min | 25 min |
