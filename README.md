# Zorvian ERP

**"Tan simple como una aplicación moderna, tan robusto como un ERP empresarial."**

Zorvian ERP es un sistema ERP moderno multiplataforma construido con **Flutter** (frontend) y **ASP.NET Core 9** (backend), diseñado para empresas de Centroamérica y el Caribe.

**Países soportados:** Nicaragua · Costa Rica · Guatemala · Honduras · El Salvador · Panamá

---

## Identidad Visual

| Rol | Color | Módulo |
|-----|-------|--------|
| Primario | `#1A0A3E` Deep Violet-Navy | Core / Plataforma |
| Secundario | `#00E5FF` Cyan Eléctrico | Frontend / Ventas |
| Éxito | `#00C853` Green Gemini | Transacciones exitosas |
| Advertencia | `#FF6D00` Amber Alert | Pendientes |
| Z-IA | `#B388FF` Purple Aura | Inteligencia Artificial |
| CRM | `#00BCD4` Cyan Comercial | Clientes |
| Finanzas | `#1B5E20` Green Bosque | Contabilidad |
| Inventario | `#FF8F00` Amber Logístico | Bodegas |
| RRHH | `#E040FB` Magenta Talento | Personas |

---

## Stack Tecnológico

| Capa | Tecnología |
|------|-----------|
| **Frontend** | Flutter 3.x, Riverpod 2.x, GoRouter, Material 3 |
| **Backend** | ASP.NET Core 9, Entity Framework Core 9, Clean Architecture |
| **Base de Datos** | PostgreSQL 16 |
| **Cache** | Redis 7 |
| **Message Queue** | RabbitMQ |
| **Auth** | Firebase Auth + JWT (multi-tenant) |
| **Realtime** | SignalR (Redis Backplane) |
| **Jobs** | Hangfire |
| **AI** | Vertex AI, ML.NET, XGBoost, pgvector |
| **Observability** | Prometheus, Grafana, Sentry, OpenTelemetry |
| **CI/CD** | GitHub Actions |
| **Container** | Docker + Docker Compose |
| **Edge** | CloudFlare WAF + CDN |

---

## Módulos

| Módulo | Estado | Color |
|--------|--------|-------|
| Multisucursal | ✅ | Admin |
| Comercial | ✅ | CRM |
| Inventario | ✅ | Inventario |
| Créditos | ✅ | Finanzas |
| Caja | ✅ | Tesorería |
| Tesorería | ✅ | Tesorería |
| Contabilidad | ✅ | Finanzas |
| RRHH | ✅ | RRHH |
| Nómina | ✅ | RRHH |
| Garantías | ✅ | Inventario |
| Activos Fijos | ✅ | Finanzas |
| IA (Z-IA) | ✅ | Z-IA |
| BI | ✅ | Z-IA |
| Webhooks | ✅ | Admin |

---

## Diagramas de Arquitectura

| Diagrama | Enlace |
|----------|--------|
| Arquitectura General | `docs/architecture_overview.md` |
| Z-IA (IA + ML + OCR + Chatbot) | `docs/diagrams/z_ia_architecture.md` |
| Pipeline CRM Completo | `docs/diagrams/crm_pipeline.md` |
| Ciclo Contable | `docs/diagrams/accounting_cycle.md` |
| Flujo de Tesorería | `docs/diagrams/treasury_flow.md` |
| Kardex y Costeo de Inventario | `docs/diagrams/inventory_costing.md` |
| Arquitectura Multi-Tenant | `docs/diagrams/multi_tenant.md` |
| Auditoría Visual Completa | `docs/INFORME_AUDITORIA_VISUAL.md` |

---

## Arquitectura

```
Zorvian ERP
├── frontend/                    # Flutter (Web, Android, iOS)
│   └── lib/
│       ├── app/                 # Router, Theme, App Shell
│       ├── auth/                # Auth Provider
│       ├── core/                # Services, Network, Storage
│       ├── features/            # Feature modules (46)
│       ├── shared/ds/           # Design System (45 componentes Z-*)
│       └── l10n/                # Localization (es/en)
├── src/                         # ASP.NET Core Backend
│   ├── Zorvian.Core/            # 154 Entities, Enums, Interfaces
│   ├── Zorvian.Application/    # 87 Services, DTOs, CQRS
│   ├── Zorvian.Infrastructure/ # EF Core, 86 Repos, External Services
│   └── Zorvian.Web/            # 86 Controllers, Middleware, SignalR
├── tests/                       # Unit & Integration Tests
│   ├── Zorvian.Tests/          # Backend (xUnit + Moq)
│   └── load/                   # k6 Load Testing
├── docs/
│   ├── architecture_overview.md
│   ├── diagrams/               # Diagramas Mermaid por módulo
│   ├── INFORME_AUDITORIA_VISUAL.md
│   └── ...
├── Dockerfile                   # Multi-stage optimized build
└── docker-compose.yml           # Local dev stack
```

---

## Inicio Rápido

### Docker Compose (Recomendado)

```bash
git clone https://github.com/AllSystem23/nexora.git
cd nexora
cp src/Zorvian.Web/.env.example src/Zorvian.Web/.env
docker-compose up -d
curl http://localhost:8080/health
```

### Desarrollo Local

**Requisitos:** .NET 9 SDK, Flutter 3.x, PostgreSQL 16, Redis 7

```bash
# Backend
cd src/Zorvian.Web
dotnet restore
dotnet run

# Frontend
cd frontend
flutter pub get
flutter run -d chrome
```

---

## Seguridad

- **Autenticación**: Firebase Auth + JWT tokens multi-tenant
- **Autorización**: RBAC con permisos granulares (RequirePermission)
- **Multi-tenant**: Query Filters EF Core + RLS PostgreSQL
- **Edge**: CloudFlare WAF + Rate Limiting
- **Auditoría**: Logs inmutables + Elasticsearch
- **Rate Limiting**: 120 req/min general, 5 req/15min auth
- **Security Headers**: CSP, HSTS, X-Frame-Options, Permissions-Policy
- **Circuit Breaker**: Polly con retry + circuit breaker
- **Validación**: FluentValidation + XSS protection

---

## Testing

```bash
# Backend tests
dotnet test

# Frontend tests
cd frontend
flutter test --coverage

# Load testing
cd tests/load
k6 run zorvian-load-test.js
```

---

## Roadmap Visual 2026-2027

| Fase | Periodo | Entregable |
|------|---------|------------|
| 🔴 Foundation | Jul-Sep 2026 | Nueva paleta, diagramas C4 Z-IA, CRM, Contable |
| 🟡 Expansión | Oct 2026-Ene 2027 | Tesorería, Multi-Tenant, Inventory, Dashboard Redesign |
| 🟢 Enterprise | Feb-Jun 2027 | Seguridad C4, Sidebar/Header redesign, Disaster Recovery |

---

## Calificación de Auditoría Visual (Junio 2026)

| Categoría | Puntaje |
|-----------|:-------:|
| Arquitectura Técnica | 8.0/10 |
| Visual / Diagramas | 6.0/10 → **8.5/10** (post-mejora) |
| Branding / Identidad | 5.5/10 → **9.0/10** (post-mejora) |
| UX/UI Empresarial | 7.0/10 |
| Cobertura Funcional | 8.0/10 |
| Competitividad | 6.5/10 |
| Documentación | 7.0/10 → **9.0/10** (post-mejora) |
| **Global** | **6.9/10 → 8.5/10** (proyectado) |

---

## Licencia

Uso interno — Zorvian ERP © 2026
