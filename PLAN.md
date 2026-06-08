# Zorvian ERP — Plan Maestro de Implementación

**Versión:** 2.0
**Fecha:** Junio 2026
**Stack:** Flutter + ASP.NET Core 9 + PostgreSQL (Neon) + Firebase
**Arquitectura:** Clean Architecture + DDD + Multi-tenant

---

## Convenciones

- [ ] **Pendiente**
- [~] **En progreso**
- [x] **Completado**
- [!] **Bloqueado**

---

## VISIÓN GENERAL

### Estado Actual vs Master Prompt

| Dimensión | Cobertura | Estado |
|-----------|-----------|--------|
| Multiempresa (Multi-tenant) | 100% | ✅ |
| Multisucursal (Branch) | 95% | ✅ |
| Multimoneda | 20% | 🔴 Pendiente |
| API First | 90% | ✅ |
| Seguridad (Roles/Permisos) | 80% | 🟡 |
| Auditoría (AuditLog) | 85% | 🟡 |
| Gestión Documental | 60% | 🟡 |
| Motor de Aprobaciones | 90% | ✅ |
| Motor de Notificaciones | 85% | 🟡 |
| Contabilidad | 85% | 🟡 |
| Catálogo de Cuentas | 90% | ✅ |
| Centros de Costo | 100% | ✅ |
| Presupuestos | 100% | ✅ |
| Dashboards | 75% | 🟡 |
| Reportes | 65% | 🟡 |
| Automatización | 65% | 🟡 |
| IA/ML | 25% | 🔴 |
| Experiencia Móvil | 50% | 🟡 |
| Design System (Frontend) | 30% | 🔴 |
| Testing | 85% | 🟡 |
| Cobranza y Mora | 90% | ✅ |
| Arqueo de Caja | 100% | ✅ |

### Brechas Prioritarias vs Master Prompt

| Prioridad | Brecha | Impacto | Módulo |
|-----------|--------|---------|--------|
| 🔴 CRÍTICA | Design System inexistente | UX/UI, productividad | Frontend |
| 🟡 ALTA | Multimoneda no implementada | Operaciones multi-país | Core |
| 🟡 ALTA | Constructor de Reportes no existe | Reportes | Reportes |
| 🟡 MEDIA | Predicción IA no implementada | IA/ML | Dashboard |
| 🟡 MEDIA | Búsqueda global no implementada | UX | Frontend |
| 🟡 MEDIA | Dashboard v2 (KPIs por rol, widgets) | UX | Dashboard |

---

## FASE 1 — CORRECCIONES CRÍTICAS BACKEND (SEMANA 1-2)

### 1.1 Contabilidad — Conectar servicios faltantes

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 1.1.1 | Inyectar `IAutoAccountingService` en `CreditService.RegisterPaymentAsync` | Backend | 🔴 Alta | 1h | — |
| 1.1.2 | Implementar reversión contable en `PurchaseService.CancelAsync` | Backend | 🔴 Alta | 2h | 1.1.1 |
| 1.1.3 | Conectar `CashRegisterService.AddMovementAsync` con contabilidad | Backend | 🟡 Media | 1h | 1.1.1 |
| 1.1.4 | Tests de integración: ciclo completo Venta→Asiento→Pago→Asiento | QA | 🔴 Alta | 4h | 1.1.1-3 |

### 1.2 Seguridad — SoftDelete + Change History

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| [x] 1.2.1 | Implementar `IsDeleted` en `BaseEntity` + query filter global | Backend | 🟡 Alta | 3h | — |
| [x] 1.2.2 | Implementar `EntityHistory` para Change History (antes/después) | Backend | 🟡 Alta | 4h | 1.2.1 |
| [x] 1.2.3 | Implementar Rate Limiting middleware (fixed window) | Backend | 🟡 Alta | 2h | — |
| [x] 1.2.4 | Search bars server-side en listas restantes (Sales, Quotes, Credits, Purchases, Inventory) | Frontend | 🟡 Alta | 4h | — |
| [x] 1.2.5 | Tests de integración de seguridad | QA | 🟡 Alta | 3h | 1.2.1-3 |

---

## FASE 2 — MÓDULOS FINANCIEROS (SEMANA 3-5)

### 2.1 Centros de Costo

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| [x] 2.1.1 | Entidad `CostCenter` (Id, Name, Code, IsActive, CompanyId) | Backend | 🔴 Alta | 2h | — |
| [x] 2.1.2 | Agregar `CostCenterId` a `Account` y `AccountingEntryDetail` | Backend | 🔴 Alta | 2h | 2.1.1 |
| [x] 2.1.3 | CRUD `CostCentersController` + Service + Repository | Backend | 🔴 Alta | 3h | 2.1.1 |
| [x] 2.1.4 | Asignar centro de costo en asientos automáticos (Ventas, Compras, Gastos) | Backend | 🔴 Alta | 4h | 2.1.2 |
| [x] 2.1.5 | Reporte de gastos por centro de costo | Backend | 🟡 Alta | 3h | 2.1.4 |
| [x] 2.1.6 | UI: CRUD Centros de Costo + selector en formularios | Frontend | 🟡 Alta | 4h | 2.1.3 |

### 2.2 Presupuestos vs Real

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| [x] 2.2.1 | Entidad `Budget` (Year, Month, AccountId, CostCenterId, BudgetedAmount, CompanyId) | Backend | 🔴 Alta | 3h | 2.1.1 |
| [x] 2.2.2 | CRUD Budgets Controller + Service + Repository | Backend | 🔴 Alta | 3h | 2.2.1 |
| [x] 2.2.3 | Reporte Budget vs Actual (comparar BudgetedAmount vs AccountingEntryDetail real) | Backend | 🔴 Alta | 4h | 2.2.1 |
| [x] 2.2.4 | Dashboard de Presupuestos | Frontend | 🟡 Alta | 4h | 2.2.3 |

### 2.3 Notas de Crédito y Devoluciones

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| [x] 2.3.1 | Entidades `CreditNote` + `CreditNoteDetail` | Backend | 🟡 Alta | 3h | — |
| [x] 2.3.2 | `CreditNoteService` (crear, anular venta, revertir inventario) | Backend | 🟡 Alta | 4h | 2.3.1 |
| [x] 2.3.3 | `CreditNotesController` API | Backend | 🟡 Alta | 2h | 2.3.2 |
| [x] 2.3.4 | Asiento contable reversado en `AutoAccountingService` | Backend | 🟡 Alta | 2h | 2.3.2 |
| [x] 2.3.5 | UI: Notas de Crédito en detalle de venta | Frontend | 🟡 Media | 4h | 2.3.3 |

---

## FASE 3 — WORKFLOWS AVANZADOS (SEMANA 6-8)

### 3.1 Motor de Aprobaciones Configurable

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| [x] 3.1.1 | Entidad `ApprovalFlowConfig` (Module, EventType, Steps[], Conditions) | Backend | 🟡 Alta | 4h | — |
| [x] 3.1.2 | Motor genérico `ApprovalEngine` (evalúa reglas, notifica, ejecuta) | Backend | 🟡 Alta | 6h | 3.1.1 |
| [x] 3.1.3 | Integrar ApprovalEngine en Compras | Backend | 🟡 Alta | 4h | 3.1.2 |
| [x] 3.1.4 | UI: Flujos de aprobación + pendientes | Frontend | 🟡 Media | 6h | 3.1.1 |

### 3.2 Cobranza y Mora

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| [x] 3.2.1 | Workflow de cobranza (refinanciamiento, reestructuración) | Backend | 🟡 Alta | 4h | — |
| [x] 3.2.2 | Refinanciamiento de créditos (reestructurar cuotas) | Backend | 🟡 Media | 4h | — |
| [x] 3.2.3 | Dashboard de cobranza (mora, efectividad, gestión) | Ambos | 🟡 Media | 4h | 3.2.1 |

### 3.3 Arqueo de Caja

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| [x] 3.3.1 | Arqueo backend (repo, DTOs, service, controller, mapping) | Backend | 🟡 Alta | 3h | — |
| [x] 3.3.2 | UI: Arqueo (formulario denominaciones + tab detalle) | Frontend | 🟡 Media | 4h | 3.3.1 |
| [x] 3.3.3 | Visualización de arqueo en detalle de caja | Frontend | 🟡 Media | 2h | 3.3.1 |

---

## FASE 4 — UX/UI Y DESIGN SYSTEM (SEMANA 9-12)

### 4.1 Design Tokens

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 4.1.1 | Crear tokens semánticos (colors, typography, spacing, radii) | Frontend | 🔴 Alta | 1 sem | — |
| 4.1.2 | Migrar `theme.dart` a usar tokens | Frontend | 🔴 Alta | 3d | 4.1.1 |

### 4.2 Componentes Base

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 4.2.1 | `ZDataTable` (sort, paginate, filter, export server-side) | Frontend | 🔴 Alta | 3 sem | 4.1.1 |
| 4.2.2 | `ZButton`, `ZTextField`, `ZSelect`, `ZBadge`, `ZCard` | Frontend | 🔴 Alta | 2 sem | 4.1.1 |
| 4.2.3 | `ZModal`, `ZToast`, `ZStepper`, `ZTimeline`, `ZCalendar` | Frontend | 🟡 Alta | 3 sem | 4.1.1 |

### 4.3 Dashboard v2

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 4.3.1 | KPIs por rol (Vendedor, RRHH, Contador, Gerente) | Frontend | 🟡 Alta | 2 sem | 4.2.2 |
| 4.3.2 | Widgets modulares drag-to-reorder | Frontend | 🟡 Media | 2 sem | 4.3.1 |
| 4.3.3 | Drill-down en cada KPI | Frontend | 🟡 Media | 1 sem | 4.3.1 |

### 4.4 Accesibilidad WCAG 2.2 AA

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 4.4.1 | Fase 1: Contraste, tap targets, Semantics labels | Frontend | 🟡 Alta | 2 sem | 4.1.1 |
| 4.4.2 | Fase 2: Focus order, live regions, skip links | Frontend | 🟡 Media | 2 sem | 4.4.1 |
| 4.4.3 | Fase 3: Screen reader testing, keyboard-only nav | Frontend | 🟡 Baja | 2 sem | 4.4.2 |

---

## FASE 5 — INTELIGENCIA ARTIFICIAL (SEMANA 13-15)

### 5.1 Predicción y Recomendación

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 5.1.1 | Predicción de ventas (ML.NET + datos históricos) | Backend | 🟡 Media | 1 sem | — |
| 5.1.2 | Recomendación de compras (stock bajo + demanda) | Backend | 🟡 Media | 1 sem | 5.1.1 |
| 5.1.3 | Clasificación automática de gastos | Backend | 🟡 Baja | 1 sem | — |

### 5.2 Asistente Inteligente

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 5.2.1 | Chatbot ERP con RAG (consulta de datos, reportes) | Ambos | 🟡 Media | 3 sem | — |
| 5.2.2 | Asistente contable (sugerir cuentas, detectar anomalías) | Backend | 🟡 Baja | 2 sem | — |

---

## FASE 6 — MULTIMONEDA Y MULTI-IDIOMA (SEMANA 16-17)

### 6.1 Multimoneda

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 6.1.1 | Agregar `Currency` a entidades (Sale, Purchase, Account, etc.) | Backend | 🟡 Alta | 4h | — |
| 6.1.2 | Tipo de cambio configurable por empresa | Backend | 🟡 Alta | 3h | 6.1.1 |
| 6.1.3 | Conversión automática en reportes financieros | Backend | 🟡 Alta | 4h | 6.1.2 |
| 6.1.4 | UI: selector de moneda en ventas/compras | Frontend | 🟡 Media | 4h | 6.1.1 |

### 6.2 Multi-idioma

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 6.2.1 | Completar traducciones (actual: en/es, agregar: pt, fr) | Frontend | 🟡 Media | 1 sem | — |

---

## FASE 7 — CONSTRUCTOR DE REPORTES (SEMANA 18-19)

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 7.1 | Entidad `CustomReport` (Name, Fields[], Filters[], GroupBy) | Backend | 🟡 Alta | 4h | — |
| 7.2 | Motor de consultas dinámicas para reportes personalizados | Backend | 🟡 Alta | 8h | 7.1 |
| 7.3 | Export PDF/Excel desde constructor | Backend | 🟡 Alta | 4h | 7.2 |
| 7.4 | UI: Report Builder drag-and-drop | Frontend | 🟡 Alta | 2 sem | 7.2 |
| 7.5 | Reportes guardados por usuario/rol | Ambos | 🟡 Media | 2d | 7.4 |

---

## FASE 8 — COREAVANZADO (SEMANA 20-22)

### 8.1 Offline-First

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 8.1.1 | Drift/Isar para caché local | Frontend | 🟡 Media | 3 sem | — |
| 8.1.2 | Sincronización bidireccional con backend | Ambos | 🟡 Media | 3 sem | 8.1.1 |

### 8.2 API Pública + Webhooks

| # | Tarea | Área | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 8.2.1 | Documentación OpenAPI completa | Backend | 🟡 Alta | 1 sem | — |
| 8.2.2 | Webhooks con firma HMAC y reintentos | Backend | 🟡 Media | 2 sem | — |

---

## RESUMEN DE FASES

| Fase | Nombre | Semanas | Prioridad |
|------|--------|---------|-----------|
| F1 | Correcciones Críticas Backend | 1-2 | 🔴 CRÍTICA ✅ |
| F2 | Módulos Financieros | 3-5 | 🔴 CRÍTICA ✅ |
| F3 | Workflows Avanzados | 6-8 | 🟡 ALTA ✅ |
| F4 | UX/UI + Design System | 9-12 | 🔴 CRÍTICA ← EN CURSO |
| F5 | Inteligencia Artificial | 13-15 | 🟡 MEDIA |
| F6 | Multimoneda + Multi-idioma | 16-17 | 🟡 ALTA |
| F7 | Constructor de Reportes | 18-19 | 🟡 ALTA |
| F8 | Core Avanzado | 20-22 | 🟡 MEDIA |

---

## ADRs

| Fecha | Decisión | Opción Elegida | Alternativa |
|-------|----------|---------------|-------------|
| 2026-01-06 | State Management | Riverpod 3.x | Bloc, GetX |
| 2026-01-06 | ORM | EF Core + Npgsql | Dapper, NHibernate |
| 2026-01-06 | Auth | Firebase Auth + JWT propio | Auth0, IdentityServer |
| 2026-01-06 | Background Jobs | Hangfire + PostgreSQL | Quartz.NET |
| 2026-06-01 | Arquitectura Módulos | Entity/repo/service/controller | MediatR/CQRS puro |
| 2026-06-01 | Multisucursal | BranchId en todas las entidades operativas | Schema por sucursal |
| 2026-06-07 | Plan Maestro | Fases priorizadas por impacto vs Master Prompt | — |

---

## Notas

- Estimaciones en horas/días de desarrollo real.
- Multiplicar por ~1.5 para incluir code review + tests + bugs.
- Priorización basada en: impacto empresarial + esfuerzo + dependencias.
