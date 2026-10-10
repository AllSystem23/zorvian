# Zorvian ERP — Project Memory

## Stack
- **Frontend**: Flutter 3.x · Riverpod · GoRouter · Material 3
- **Backend**: ASP.NET Core 9 · EF Core 9 · Clean Architecture · FluentValidation
- **Database**: PostgreSQL 16 (Neon) · Row-Level Security
- **Cache**: Redis 7 (RedisCacheService con IDistributedCache + auto-fallback a InMemoryCacheService)
- **Messaging**: SignalR (NotificationHub) · Hangfire (job scheduler) · **RabbitMQ 3.13 via MassTransit** (4 consumers activos)
- **Jobs**: Hangfire con persistencia PostgreSQL (15 jobs: 11 recurring + 4 ad-hoc)
- **Search/Logs**: Serilog (console + Elasticsearch condicional + file). Elasticsearch integrado via Serilog.Sinks.Elasticsearch
- **Auth**: Firebase Auth + JWT (1h access / 7d refresh). API Keys. MFA (backend + frontend UI)
- **Observability**: OpenTelemetry SDK (tracing + metrics + OTLP). Serilog (console + ES + file). Sentry (frontend). Prometheus/Grafana pendientes
- **AI/ML**: ML.NET · Vertex AI (ChatService) · RAG (EmbeddingService) · OCR (OcrService)
- **Infra**: Render.com · Firebase Hosting · CloudFlare · Docker
- **CI/CD**: GitHub Actions (1 workflow: ci-cd.yml, 5 jobs)
- **Testing**: xUnit y Moq (backend). Flutter test (frontend). K6 (load testing). 93 archivos de test backend

## Documentación: Lo que NO está en el código (actualizar al implementar)
- ❌ **CQRS/MediatR** — Mencionado en SPEC.md pero 0 referencias en código
- ✅ **Elasticsearch** — Integrado via Serilog.Sinks.Elasticsearch (configurable en appsettings.json)
- ❌ **Prometheus/Grafana** — 0 referencias en código
- ✅ **OpenTelemetry SDK** — Implementado en OpenTelemetryExtensions.cs (ASP.NET Core, HTTP, EF Core, OTLP exporter)
- ❌ **XGBoost** — Mencionado en docs pero no implementado en código
- ❌ **pgvector** — Mencionado en docs pero no implementado en código
- ✅ **Redis como cache** — RedisCacheService implementado con IDistributedCache + auto-fallback a InMemoryCache
- ✅ **MFA** — `MfaService` backend + `MfaLoginPage`/`MfaSettingsPage` frontend (qr_flutter para QR local)
- ✅ **Offline-First** — SyncEngine + Drift implementados para productos, cotizaciones y créditos (3 repositorios locales, schema v3)

## RabbitMQ / MassTransit — Consumers implementados
- `SaleCreatedConsumer` — Enqueuea GoalIntegrationService + CommissionService
- `SaleCancelledConsumer` — Enqueuea CommissionService.ClawbackCommissionsBySaleAsync
- `PaymentReceivedConsumer` — Enqueuea GoalIntegrationService + CommissionService
- `EmployeeCreatedConsumer` — Inicializa LeaveBalances + EmployeeSalary
- Config: 3 retries con 5s intervalo, circuit breaker (15 trips / 5min reset)
- Host: configurable via appsettings (default localhost:5672)

## Directory Structure
```
/ (repo root)
├── frontend/lib/
│   ├── app/                    # Router, theme, app entry
│   │   ├── router.dart          # GoRouter: 130+ routes, ShellRoute, role redirect
│   │   └── theme.dart           # Material 3 light/dark, new brand tokens
│   ├── auth/                    # AuthProvider, auth state machine
│   ├── core/
│   │   ├── navigation/
│   │   │   └── nav_config.dart  # 46 NavItems, 9 NavModules, 5 groups, role-filtered
│   │   ├── widgets/
│   │   │   ├── sidebar/         # sidebar.dart + section.dart + item.dart
│   │   │   ├── bi/              # BarChart, PieChart, LineChart, KpiCard, Gauge
│   │   │   ├── responsive_layout.dart
│   │   │   └── command_palette.dart
│   │   ├── theme/              # ThemeModeProvider
│   │   └── services/           # SignalR, Dio, SecureStorage
│   ├── features/
│   │   ├── splash/             # SplashPage (logo + loading on auth check)
│   │   ├── dashboard/          # DashboardPage (v1 redesign with charts)
│   │   ├── dashboard_v2/       # Alternative dashboard
│   │   ├── executive_dashboard/ # Executive KPI view
│   │   ├── login/              # LoginPage + RegisterPage + ForgotPasswordPage
│   │   ├── bi/                 # 4 BI dashboards (exec, financial, commercial, operational)
│   │   ├── goals/              # Goals dashboard
│   │   └── credits/            # Overdue credits dashboard
│   └── shared/ds/
│       ├── tokens/             # colors.dart, spacing.dart, typography.dart, etc.
│       └── components/         # z_button, z_card, z_stat_card, z_data_table, etc.
│
├── src/                        # ASP.NET Core 9 backend
│   ├── Zorvian.Web/            # Controllers, middleware, DI
│   ├── Zorvian.Core/           # Entities, enums
│   ├── Zorvian.Application/    # Services, DTOs, interfaces
│   └── Zorvian.Infrastructure/ # EF Core DbContext, repositories, migrations
│
├── docs/
│   ├── INFORME_AUDITORIA_VISUAL.md  # Full audit (6.93→8.5 roadmap)
│   ├── PRESENTACION_EJECUTIVA.md    # Investor deck ($1.2B TAM, $500K ARR)
│   └── diagrams/                    # 12 Mermaid diagrams
│       ├── architecture_overview.md # v2.0 with API Gateway, LB, WAF
│       ├── z_ia_architecture.md     # Z-IA: RAG, OCR, ML, rules engine
│       ├── crm_pipeline.md          # Lead → qualification → pipeline → funnel
│       ├── accounting_cycle.md      # Full cycle, multi-country policies
│       ├── treasury_flow.md         # Inflows/outflows, reconciliation
│       ├── inventory_costing.md     # Kardex FIFO/avg/specific
│       ├── multi_tenant.md          # 3-phase isolation strategy
│       ├── payroll_flow.md          # INSS/IR → ACH → accounting entry
│       ├── security_architecture.md # Defense in depth, RBAC, compliance
│       ├── integrations_architecture.md # WhatsApp, Email, Webhooks, API
│       ├── electronic_invoicing.md  # Multi-country DGI/Hacienda/SAT flow
│       └── disaster_recovery.md     # PITR, failover, DR site, runbook
│
├── .github/workflows/
│   └── ci-cd.yml              # Full pipeline: build, test, security, deploy
│
└── assets/                    # zorvian_enterprise_logo_refined.png, Zorvian.png
```

## Brand System
- **Primary**: Deep Violet-Navy `#1A0A3E`
- **Secondary**: Cyan Eléctrico `#00E5FF`
- **Accent**: Medium Violet `#7C4DFF` (CTAs, highlights)
- **Teal**: `#2EE59D` (secondary accent)
- **Gold**: `#FFD54F` (premium emphasis)
- **Module colors**: 10 distinct colors (IA, CRM, Sales, Inventory, Purchases, Finance, Treasury, HR, Admin, Security)
- **Gradients**: `brandGradient` (violet→navy→purple), `accentGradient` (cyan→violet→purple)

## Key Conventions
- **Router** (`router.dart`): `ShellRoute` wraps authenticated pages; `GoRoute` per page; `_routeRoles` map for RBAC
- **State**: Riverpod `NotifierProvider` with immutable state + `copyWith`
- **Pages**: `ConsumerStatefulWidget` with `ConsumerState`, loading in `initState` via `addPostFrameCallback`
- **Widgets**: Design System (`ZCard`, `ZButton`, `ZStatCard`, `ZDataTable`, etc.) from `shared/ds/ds.dart`
- **API**: Dio client injected via `ref.read(dioClientProvider)`
- **Sidebar**: 9 modules in 5 groups (Operations, Financial, Talent, Intelligence, Admin)
- **Responsive**: `ResponsiveBuilder`, `ResponsiveGrid`, `ResponsivePadding` (mobile <576px, tablet <992px, desktop 992px+)
- **Accessibility**: `ZSkipLink`, `ZLiveRegion`, semantic labels, keyboard nav

## Important Notes
- The `/splash` page shows Zorvian logo + loading spinner during auth initialization
- Dashboard is NOT in sidebar; it's the default landing page via `/dashboard` redirect
- ZDataTable replaces Flutter's native DataTable; ensure all lists use `ZColumn` not `DataColumn`
- All architecture diagrams live in `docs/diagrams/` (12 files)
- Electronic invoicing supports 6 countries with per-country XML generation
- Current deployment: Render.com (512MB RAM) + Neon PostgreSQL — bottleneck for multi-tenant production
- **Rate limiting**: Configurable via `appsettings.json` pero deshabilitado POR DEFECTO (`"Enabled": false`)
- **JWT**: 1h access / 7d refresh. NO hay blacklist de tokens revocados
- **Build status**: `dotnet build` produce 0 errores, 0 warnings ✅
- **Controllers**: 123 total (87 core + 28 Fleet + 8 Warranty)
- **Application Services**: 111 archivos
- **Flutter Providers**: 92 archivos
- **Repositories**: 109 total (86 core + 23 Fleet)
- Legacy "Nexora" naming appears in `SPEC.md` and some env defaults
- [x] Consolidate 3 CI/CD workflows into 1
- [x] Fix `DataColumn`/`ZColumn` type errors in 5 files
- [x] Add password recovery flow (currently TODO placeholder)
- [x] Add preloader/splash screen on auth check
- [x] Add tenant switcher for multi-company users
- [x] Add `SaleStatus`/`CreditStatus` string constants to prevent typos (`Zorvian.Core/Enums/SaleStatus.cs`)
- [x] Fix dashboard blank screen on reload: null-safe tenant casts (`ZCompanySwitcher`/`ZCompanyDropdown`/`getMyTenants`), visible ErrorWidget in release (`main.dart` `_BuildErrorView`), y fix overflow del Row del nombre en `dashboard_page.dart`; regresión en `test/dashboard_page_test.dart`
- [x] Auto-seed chart of accounts + account links on company creation (`SeedService.SeedAsync`)
- [x] Fix account code mismatch: `AccountLinkService.ResolveAccountAsync` tries 3 code formats + name fallback
- [x] Fix account code mismatch: `AutoAccountingService.GetAccountIdByCodeAsync` tries padded variants
- [x] Add name-based fallback in `AutoAccountingService.GetAccountIdAsync` when no AccountLink found
- [x] Add transaction safety to `CreditNoteService.CreateAsync`
- [x] Add `CancelSaleAsync` endpoint (`POST /zorvian/v1/sales/{id}/cancel`)
- [x] Add cancel sale + accounting entries link to frontend `sale_detail_page.dart`
- [x] Handle null `Product.TaxCategory` gracefully in `AutoAccountingService.GenerateSaleEntryAsync`
- [x] Seed de Tienda Brizuela Romero: crea la compañía si no existe, CSV/JSON embebidos en `Zorvian.Infrastructure` (funciona en Render/Docker), idempotente y con upsert de `AccountingRuleTemplate` (`SeedBrizuelaRomeroAsync(string tenantId, bool isSuperAdminCaller)`, `POST /zorvian/v1/seed/brizuela-romero`). Guard anti-contaminación: si el SuperAdmin llega auto-seleccionado a otra compañía, crea la de Brizuela aparte en vez de importar ahí; 7 tests en `SeedServiceBrizuelaTests.cs`
- [x] Wizard de documentos: extras de router (`entityType`/`entityId`/`entityDisplayName`/`preselectedTemplateId`), buscador de entidades (`entity_picker_dialog.dart`), `DocumentService` guarda `EntityType` real, PDF real vía SharePlus, errores legibles
- [x] Rediseño de propagación de companyId/branchId (SuperAdmin acotado + sucursal server-side) — ver sección "Multi-tenant scope (compañía + sucursal)"

## Multi-tenant scope (compañía + sucursal)
Semántica de ejecución (2026-10):
- `ITenantContext.BypassTenantFilter = IsSuperAdmin && !HasCompanySelection` — es el ÚNICO flag de bypass; los 163 `HasQueryFilter` de `ZorvianDbContext` y el SQL crudo (Dashboard/Bi/Fleet/Warranty) lo usan. SuperAdmin con compañía seleccionada queda acotado; sin selección mantiene acceso total.
- `ITenantContext` expone `CurrentBranchId`, `SelectedCompanyId`, `HasCompanySelection`, `EffectiveCompanyId` (Dim). `TenantContext` los resuelve con `AsyncLocal`.
- `TenantMiddleware` resuelve: tenant (claim JWT o header `X-Tenant-Id`) → SuperAdmin auto-selecciona 1ª empresa activa si no tiene → sucursal desde header `X-Branch-Id` validada contra `IBranchRepository.GetAllAsync(companyId)` (si no pertenece → null + warning log).
- `TenantAuditInterceptor` stampa `BranchId` en Add cuando la entidad lo tiene vacío y el contexto tiene sucursal.
- `TenantSessionInterceptor` envía `@is_super_admin = BypassTenantFilter` → políticas RLS `super_admin_bypass` existentes respetan la selección (paridad sin migración).
- Escrituras con `BranchId` del cliente (Sale/Purchase/Product/InventoryMovement) pasan por `IBranchValidator.ResolveForWriteAsync` (`BranchValidator`): valida pertenencia a la compañía operativa vía BD o devuelve la del contexto; lanza si es cross-company. Tests en `BranchValidatorTests.cs`.
- `ClientRepository`/`SaleRepository`/`PurchaseRepository` tratan `branchId == Guid.Empty` como "todas las sucursales" (guardia `if (branchId != Guid.Empty)`).
- **Frontend**: `DioClient` adjunta `X-Tenant-Id` (claim JWT) y `X-Branch-Id` (`companyBranchProvider`) vía `dynamicHeaders`. `CompanyBranchNotifier` persiste selección en SharedPreferences (`whenHydrated` para evitar carreras con auto-selección), resetea la sucursal al cambiar de compañía, y `clearSelection()` en logout. Todos los paths de switch (`global_header`, `ZCompanySwitcher`, `super_admin_companies_page`, `app_shell`) sincronizan `companyBranchProvider` + invalidan `headerBranchListProvider`.
- Cuidado: mocks Moq de `ITenantContext` deben stubbear `SelectedCompanyId`/`HasCompanySelection`/`BypassTenantFilter` (Moq no ejecuta default interface implementations). Las extensiones `ResolveCompanyId`/`RequireCompanyId`/`ResolveTenantIdString` tienen fallback a `TenantId` para mocks que sólo stubbean `TenantId`.

## Contabilidad multi-sucursal (2026-10)
- **Invariante**: el `AccountingEntry.BranchId` SIEMPRE refleja la sucursal de la transacción origen. `IAutoAccountingService` expone `Guid? branchId = null` en los 16 generadores clásicos (venta, reversión, costo de venta, compra, reversión compra, inventario, nómina, caja, nota de crédito y 7 de tesorería); los 9 "nuevos" (crédito/proveedores/activos fijos/garantía/fleet) ya lo tenían non-nullable.
- Los servicios pasan el branch de la transacción validada (`sale.BranchId`, `purchase.BranchId`, `movement.BranchId`, etc.). Tesorería (7 endpoints de `TreasuryController`) e nómina pasan `_tenant.ResolveBranchId()`. Si el branch es null, `TenantAuditInterceptor.StampBranchFromContext` stampa el del contexto (fallback).
- **Lectura del diario**: `AccountingEntriesController.GetFiltered` acepta `branchId` opcional; `AccountingEntryService.GetFilteredAsync` usa `branchId ?? _tenant.ResolveBranchId()` y lo pasa a `AccountingEntryRepository.GetFiltered/GetFilteredCount` (filtro `e.BranchId == b` sólo si b no es Empty; `Guid.Empty` explícito = todas las sucursales). Los reportes financieros (`FinancialReportService`/`EnhancedReportService`/`GetPostedWithDetailsAsync`) NO filtran por sucursal — son company-wide por diseño.
- Ojo con los doubles de test que implementan `IAccountingEntryRepository` (p.ej. `AccountingIntegrationTests.EntryRepo`): mantener firmas al día. Los `Verify` de Moq de `Generate*EntryAsync` necesitan `It.IsAny<Guid?>()` en el nuevo parámetro.
- Gotcha EF: `Include(AccountingPeriod)` con nav requerida + query filter requiere que el `AccountingPeriod` exista en BD, o el asiento no aparece (INNER JOIN).

## Remediación multi-tenant por fases (2026-10) — barrido completo del proyecto
Barrido (4 audits) verificó: núcleo comercial/financiero HTTP ✅; 2ª capa ❌; RLS inerte ❌; jobs/consumers ❌; API Keys rotas ❌; frontend headers ✅ pero datos locales ❌. Remediación en curso, build+tests al cierre de cada fase:
- **Fase 1 ✅ (794/794)** — 16 entidades críticas con `HasQueryFilter` en `ZorvianDbContext.OnModelCreating` (bloque "Multi-tenant query filters": PayrollDetail, PayrollDetailConcept, Lead, Opportunity, PipelineStage, CommercialActivity, Reconciliation, ReconciliationDetail, KpiDefinition, KpiRecord, Ranking, TaxCategory, BudgetDetail, BudgetTracking, WarrantySlaConfig, BenefitProvision). `DocumentTemplateRepository` sin `IgnoreQueryFilters` (plantillas son por-compañía). `PipelineStagesController` usa `BypassTenantFilter`. Decisión: NO filtrar `UserTenant` (switcher lee membresías cross-tenant) ni `CountryTaxConfig` (catálogo global documentado).
- **Fase 2 ✅ (795/795) — background tenant-safe**:
  - `TenantAuditInterceptor`: GUID-cero = contexto no configurado → warning + NO stamp `00000000-...` compartido (test `AddedEntity_ZeroGuidTenant_LogsWarning_DoesNotStampZero`).
  - Patrón `NeedsBypass` (bypass SOLO si tenant-zero o SuperAdmin sin selección, como `AuthRepository`) en `CompanyRepository.GetByIdAsync`, `PermissionRepository.GetByIdAsync` y `GoalRepository` (lookups de assignment/definition).
  - Jobs loop-por-empresa (`IAuthRepository.GetAllCompaniesAsync` + `ITenantContextWriter.SetTenantId`, patrón VacationAutomatedJob): CheckInReminder, DocumentExpiration, AttendancePhotoCleanup, FleetAlert, WarrantySlaMonitor.
  - Bypass documentado (modelos ML.NET globales con agregados sin PII / métricas globales): 3 training jobs + HealthCheckJob.
  - `WebhookDeliveryJob` carga sub/log con `IgnoreQueryFilters` (Ids de cola interna).
  - Consumers: `SaleCreatedConsumer` pasa `EmployeeId` (NO `SaleId`) a metas; `PaymentReceivedConsumer` → `CreditService.RegisterPaymentForCompanyAsync(companyId, req)` (fija tenant antes de delegar); `EmployeeCreatedConsumer` funciona vía bypass de CompanyRepository; `PalmTrackEventMapper` fija `ITenantContextWriter.SetTenantId` (los 163 filtros EF leen ITenantContext, no la variable SQL de RLS); `GoalEngine.ProcessProgressAsync` adopta `assignment.TenantId` tras el bypass-load; `FleetExpenseNotificationJob` con try/finally en `SetIsSuperAdmin`.
  - Ojo: `CommissionService.ProcessCommissionForSaleAsync/ClawbackCommissionsBySaleAsync` YA fijaban tenant (no requerían fix).
- **Fase 3 ✅ (344/344 + `dart analyze` limpio) — frontend tenant-safe**:
  - `AppDatabase.clearAllData()` (`core/offline/app_database.dart`): borra las 5 tablas (PendingMutations incluida — replay cross-tenant sería corrupción).
  - `AuthNotifier` (`auth_provider.dart`): helpers `_wipeLocalCache()` (best-effort, try/catch; en `logout` con `unawaited` para no bloquear tests FakeAsync) y `_invalidateScopeProviders()` (dashboardProvider, creditProvider, companyListProvider, headerBranchListProvider). `logout()`: SignalR `disconnect()` + wipe + invalidación. `switchTenant()`: wipe + invalidación + `clearBranch()` si la sucursal no pertenece a la nueva compañía + reconexión SignalR con el token nuevo (`connect(ApiConfig.originUrl, token)`, try/catch best-effort). Teardown centralizado → los 4 paths de switch heredan sin duplicar lógica.
  - `branchId` hardcodeado → `ref.read(companyBranchProvider).branchId`: purchase_form, purchase_order_form, quote_form (zero-GUID), workshop_form y fleet_route_form (`...-0001`), y `app_shell` open de caja (zero-GUID). Si es null, el servidor resuelve desde el contexto.
  - Certificado laboral (`profile_page.dart`): descarga autenticada vía Dio (header Authorization) a archivo temporal + `SharePlus.instance.share` — el JWT ya NO viaja en query string (`access_token` eliminado; import de url_launcher quitado).
  - Tests: `logout()` en tests ahora no bloquea (wipe sin await); 344/344 ✅.
- **Fase 4 ✅ (800/800) — RLS, API Keys, higiene multi-tenant**:
  - **RLS**: `[Migration("20260617190000_EnableRLS")]` añadido (EF la ignora sin él) + SQL embebido como recurso (`SecurityScripts.SECURITY_RLS.sql` via csproj, fallback a disco) → funciona en Render/Docker sin depender del CWD. `SECURITY_RLS.sql`: 8 nombres de tablas inventados corregidos contra el esquema real (`FleetVehicles→Vehicles`, `FleetDrivers→Drivers`, `FleetRoutes→Routes`, `FleetRoutePoints→RoutePoints`, `FleetDeliveries→Deliveries`, `FleetDeliveryItems→DeliveryItems`, `FleetTrips→Trips`, `GoalAssignmentProgressEntries→GoalProgressEntries`), Phase 1 con `IF EXISTS`, idempotente. `test_rls_staging.sql` alineado. **FORCE evaluado y NO activado**: la app conecta como owner (Neon) y el owner bypasea RLS sin FORCE — activarlo rompería auth/login (Users tiene TenantId y la GUC de tenant aún no es válida al hacer login); runbook documentado en el encabezado del SQL (rol no-owner + excluir tablas auth + staging). Tests: `EnableRlsMigrationTests` (4).
  - **API Keys** (antes: 401 siempre): `ApiKeyService.ValidateKeyAndGetInfoAsync` con `IgnoreQueryFilters()` (corre antes de resolver tenant; el hash SHA-256 es unívoco y la key ES la credencial); `ApiKeyMiddleware` fabrica `ClaimsPrincipal` autenticado (`authenticationType: ApiKey`) con `tenant_id`/`sub` + roles y permisos heredados del usuario dueño (misma construcción de claims que JwtService) → `[Authorize]` y `RequirePermission` funcionan; `TenantMiddleware` ya no pisa el usuario fijado por API key cuando no hay claims JWT. Tests: `ApiKeyServiceTests` (5).
  - **IdempotentAttribute**: cache key = `tenant|método|path|X-Idempotency-Key` (antes solo la key → la misma key de dos compañías se solapaba y devolvía la respuesta del otro tenant). Hoy 0 controllers lo usan; corregido por si se activa.
  - **Limpieza**: `TenantContextExtensions.ValidateBranchOwnership` (no-op, 0 referencias) eliminada — la validación real vive en `BranchValidator.ResolveForWriteAsync` y `TenantMiddleware.ResolveBranchAsync`.
  - **Pendiente documentado** (fuera de alcance de remediación): `FirebaseStorageService.GetFileUrl` devuelve URLs públicas de bucket; pasar a URLs firmadas exige credenciales de firma + expiración + refresco en frontend.
- **Estado global remediación: COMPLETA (Fases 1-4).**
