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
- **Testing**: xUnit y Moq (backend). Flutter test (frontend). K6 (load testing). 136 archivos de test backend (973 tests)

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
- **Rate limiting**: ACTIVADO por defecto (`"Enabled": true` en appsettings.json). Global 100 req/min por IP (X-Forwarded-For aware) + `AuthLimit` 10/5min para endpoints de credenciales (`/auth/login`, `/auth/login-password`, `/auth/register`, `/auth/forgot-password`, `/auth/reset-password`, `/auth/mfa/login`, `/auth/mfa/verify`, `/auth/sso/*`, `/auth/complete-profile`) + `AuthOtherLimit` 30/5min para el resto de `/auth/` (refresh/logout/me — automáticos). Presupuesto POR ENDPOINT de credenciales: un ataque a /auth/login no bloquea /auth/refresh (clave `auth:{ip}:{path}`); un único bucket compartido por IP habría lockouteado oficinas tras NAT. `/health` exento. Desactivado en `appsettings.Testing.json` para no romper tests de integración. **Gotcha**: las variables de entorno de Render pisan `appsettings.json`; si en algún momento se vuelve a desactivar por env, el límite no se verá aunque el archivo diga `true`. **Verificado en producción (2026-10-14)**: ráfaga de intentos a `/auth/login` → 10 pasan (400) y del 11º responde 429 con `Retry-After`, exacto al `AuthLimit` configurado.
- **Incidencias de deploy (2026-10-14, corregidas)**: (1) `DinkToPdf 1.0.8` rompía el `dotnet restore --runtime linux-x64` del Dockerfile con NU1605 (arrastra `NETStandard.Library 1.6.0` y degrada 5 paquetes `System.*`); eliminado — QuestPDF es ahora el único renderer y el contenedor igual nunca tenía `libwkhtmltox.so`, así que el camino DinkToPdf siempre caía al fallback. (2) Verificar el restore SIEMPRE con `--runtime linux-x64`: en local sin RID el conflicto no aparece. (3) **Mojibake UTF-8 doblemente codificado** en 11 archivos (Página, crédito, Nómina): reparado decodificando el nivel anidado; ojo al editar archivos con PowerShell — `Get-Content`/`Set-Content` sin `-Encoding` leen/escriben en ANSI y convierten cualquier carácter fuera de Windows-1252 (p. ej. `→`) en `U+FFFD`. Usar `[System.IO.File]::ReadAllText/WriteAllText` con UTF-8 explícito.
- **JWT**: 1h access / 7d refresh. Refresh tokens persistidos con **rotación + detección de reuso** (`AuthService.cs`): si se presenta un token ya revocado se revocan TODAS las sesiones del usuario (`RevokeAllUserTokensAsync`). (Corrige nota previa que decía "no hay blacklist" — esa nota era incorrecta.)
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

## Pago de vacaciones (frontend, 2026-10)
- **Backend ya existía**: `VacationPaymentService.PayVacationAsync` (calcula sueldo diario = mensual/30, días hábiles, INSS por país, neto; número secuencial `RV-YYYYMMDD-NNNN`; marca la solicitud `taken`; suma `VacationDaysTaken` en `LeaveBalances`), `VacationReceiptPdfService` (QuestPDF, formato recibo nicaragüense), endpoints `POST /vacations/{id}/pay`, `GET /vacations/receipts`, `GET /vacations/{id}/receipt`, `GET /vacations/receipts/{id}/pdf`.
- **Frontend añadido**: `features/vacations/providers/vacation_receipt_provider.dart` (lista + detalle tipados, pago, descarga PDF vía Dio bytes + SharePlus), `pages/vacation_receipt_detail_page.dart` (conceptos A001/R001, totales, neto, observaciones), `pages/vacation_receipts_list_page.dart` (filtro por año + paginación contra el backend), botones **Procesar Pago** (`approved`) y **Ver Recibo** (`taken`) en `vacation_detail_page.dart`. Rutas `/vacations/receipts` y `/vacations/receipts/:receiptId`; nav item "Recibos de Vacaciones" (rol Rrhh+).
- **Modelos tipados**: `VacationItem` ahora carga `businessDays`, `rejectionReason`, `isAdvanced`, `approvalSteps` (`VacationApprovalStep`) y `VacationNotifier.loadDetail(id)` — el detalle dejó de leer `Map<String,dynamic>` con string keys. La página recarga al cambiar de sucursal (patrón `static _lastScope` de AGENTS.md).
- **Moneda**: NO construir símbolos a mano — usar `ZFormatters.currency(monto, currencyCode: codigo)` (`core/utils/formatters.dart`: NIO/GTQ/HNL/CRC/SVC/PAB/USD). El recibo trae `currency` (NIO/HNL...) y `countryCode` (NIC/HND...) en el JSON.
- State al cierre: 972/972 backend, 349/349 frontend, `flutter analyze lib` 0 issues, build 0 warnings.

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

## Reactividad al cambio de empresa/sucursal en el header (2026-10)
Barrido completo del frontend. `branchScopeProvider` (`core/providers/company_branch_provider.dart`) es la key de invalidación `'${companyId}_${branchId}'`; el `DioClient` lee `companyBranchProvider` en cada request (headers `X-Tenant-Id` / `X-Branch-Id`), así que el backend siempre ve el scope nuevo.
- **Patrón obligatorio para `NotifierProvider` con `load()` en `initState`** (35 notifiers: dashboard, client, sale, calendar, executive, accounting, approval, products, categories, brands, departments, cost_centers, quotes, suppliers, purchases, purchase_orders, employees, users, warranties, vacations, permissions, credit_notes, reconciliations, webhooks, workshops, custom_reports, cash_registers, exchange_rates, fleet×9, driver…):
  ```dart
  static String? _lastScope;   // ⚠️ DEBE ser static

  @override
  XState build() {
    final scope = ref.watch(branchScopeProvider);
    if (_lastScope != null && _lastScope != scope) {
      Future.microtask(() => load());
    }
    _lastScope = scope;
    return const XState();
  }
  ```
- **GOTCHA Riverpod 3 (verificado con test)**: `ref.listen(branchScopeProvider, …)` dentro de `build()` **NO funciona**: el propio provider se reconstruye por la dependencia y descarta el listener antes de dispararlo. Tampoco sirve guardar el último scope en un campo de instancia: Riverpod **crea una nueva instancia del notifier en cada rebuild**, así que el campo se pierde → tiene que ser `static`. El `if (_lastScope != null …)` evita un fetch extra en el primer build (las páginas ya cargan en `initState`) y mantiene los tests sin timers pendientes.
- **`AsyncNotifierProvider`** (`creditProvider`, `inventoryMovementProvider`, `employeeProvider`, `purchaseProvider`, `purchaseOrderProvider`, `userProvider`, `biProvider`): basta `ref.watch(branchScopeProvider)` al inicio de `build()` porque `build()` retorna el fetch directamente y Riverpod recarga solo.
- **`FutureProvider` sin observar nada = caché eterno**: convertidos a observar `branchScopeProvider` los de treasury (3 dropdowns: cuentas bancarias, centros de costo, facturas), `treasuryDashboardSummaryProvider`, `provider_state.dart` (5), `goal_provider.dart` (3), `provider_dashboard_page.dart` (4), `service_contract_detail_page.dart` (2), `check_issuance_page._beneficiariesProvider`.
- **`_invalidateScopeProviders()`** (`auth/auth_provider.dart`) solo corre en `switchTenant()` (SuperAdmin) y `logout()`; su rol es el wipe de Drift + refresco del header, NO la reactividad general. No se usa para el cambio de sucursal de usuario normal.
- Test de regresión: `frontend/test/branch_scope_reload_test.dart` (3 casos; documenta por qué `ref.listen` y el campo de instancia no sirven).

## Filtro de sucursal en lecturas (2026-10) — invariante y auditoríaDecisión de diseño (evaluada y confirmada, NO es un bug): los 163 `HasQueryFilter` de `ZorvianDbContext` aíslan por **compañía** (TenantId) y NUNCA por sucursal. La sucursal es filtro **explícito y opt-in** por read-path. Motivo: el frontend manda `X-Branch-Id` siempre que hay sucursal seleccionada; un filtro global haría branch-scoped en silencio a reportes, contabilidad y BI, y rompería lecturas cross-branch (traspasos, pagos en otra sucursal, supervisión SuperAdmin).
- **Helper único**: `BranchScopeExtensions.ApplyBranchScope(query, branchId, selector)` (`Zorvian.Infrastructure/Data/BranchScopeExtensions.cs`). Semántica canónica: `null` o `Guid.Empty` = todas las sucursales; cualquier otro GUID filtra por igualdad exacta. El valor viaja **parametrizado** (holder interno) para no matar el plan cache de PostgreSQL. Test de traducción real con `UseNpgsql(...).ToQueryString()`.
- **Helper transitivo**: `BranchScopeExtensions.ApplyBranchScopeTransitive(query, branchId, unlinkedPredicate, params branchSelectors)` para entidades sin `BranchId` propio (Trip, Delivery, FuelRefill, WorkOrder, FleetExpense): cada selector navega (`e => e.Vehicle.BranchId`, `e => e.Trip.Vehicle.BranchId`…), el helper añade null-guards por nivel (`nav1 != null && nav2 != null && … && BranchId == @value`) y desenvuelve los `Convert` con `UnwrapNullable`. `unlinkedPredicate` marca filas sin vínculo (p.ej. gasto sin Vehicle/Trip/Route): se consideran company-wide y SIEMPRE se incluyen. Los `Include` de navs nullable genera CS8602 en las lambdas → `#pragma warning disable CS8602` en `DeliveryRepository.cs`/`FleetExpenseRepository.cs` (falso positivo; el helper guarda en runtime).
- **RLS POR SUCURSAL Y TENANT — FORZADO desde 2026-10-14** (`ForceTenantRowLevelSecurity`, + afirmado en `SECURITY_RLS.sql` embebido): las políticas ya no son inertes. **Escape de contexto ausente** = `coalesce(current_setting('app.tenant_id', true), '') IN ('', '00000000-…')` → sin tenant resuelto NO se acota nada (paridad con `HasQueryFilter`): es lo que mantiene vivo login, `MigrateAsync` de arranque y los jobs globales (`SalesPredictionTrainingJob` consulta con `IgnoreQueryFilters()` y tenant-cero — con política estricta habría devuelto 0 filas y entrenado en silencio con datos vacíos). **Bug de OR corregido**: las tablas con columna `BranchId` usan UNA política `tenant_branch_scope_*` (tenant AND sucursal); las políticas permisivas de PostgreSQL se combinan con OR, así que la `branch_scope_*` anterior pasaba siempre sin sucursal elegida y al forzar RLS habría dejado visibles filas de OTROS tenants. `super_admin_bypass_*` se mantiene (OR) para SuperAdmin sin compañía. Guarda `to_regclass(...) IS NULL → CONTINUE`: en BD nuevas `EnableRLS` corre antes de que existan tablas de migraciones posteriores. **Rol no-owner sigue pendiente** (reducción de privilegios: rol `zorvian_app` + GRANT DML + connection string); el forzado NO lo requiere, y cambiarlo implica tocar variables de entorno de Render que no viven en el repo. Al desplegar: correr `docs/test_rls_staging.sql` y verificar login + un job recurrente antes de promover.
- **BUG de dollar-quotes (2026-10-14, corregido)**: dentro de un bloque `DO $$` NO se puede usar `$$` para delimitar los strings de `format()` — PostgreSQL no anida dollar-quotes con la misma etiqueta, así que el primer delimitador interno **cerraba el cuerpo del DO** y `CREATE POLICY` quedaba como SQL suelto → `42601: syntax error at or near "CREATE"` en el arranque. Los bloques DO usan ahora `$do$` y los strings internos mantienen `$$`. **Esto significa que `EnableRLS` estuvo fallando en cada arranque desde junio**: las políticas RLS nunca se aplicaron y la "inercia" no era (solo) por owner-bypass como decía la nota anterior, sino porque la migración abortaba. El arranque continuaba por el catch "app may still start if DB is already compatible". Lección: validar el emparejamiento de delimitadores (aperturas DO == cierres, `$$` internos en número par) antes de comitear SQL generado desde C#.
- **AsyncLocal — gotcha del `TenantMiddleware` (2026-10)**: una escritura a `AsyncLocal` hecha **dentro de un método async separado** (p.ej. `ResolveBranchAsync`) queda confinada a su contexto de ejecución y **NO fluye al caller ni a `_next`** → los repos leerían `CurrentBranchId = null` (todas las sucursales). Fix: el middleware resuelve/valida y **devuelve** el `Guid?`, y la asignación `tenantWriter.SetBranchId(...)` se hace en la continuación del propio `InvokeAsync` antes de `await _next(context)`. Test E2E: `tests/Zorvian.Tests/Middleware/TenantMiddlewareTests.cs` (6 tests; el harness captura el estado dentro de `_next` porque leer el contexto tras `await InvokeAsync` tampoco ve la escritura, por la misma razón).
- **Arranque (2026-10)**: `Program.cs` envuelve `MigrateAsync()` con lock asesor Postgres (`pg_advisory_lock(742139851)` en conexión dedicada, unlock en `finally`) y flag `Database:ApplyMigrationsOnStartup` (default `true`; si `false` salta migración con log). El RLS dinámico de arranque se conserva. CI: `dotnet ef migrations has-pending-model-changes --no-build` (bloqueante) en `backend-build` — guarda anti-drift de modelo.
- **Índices (2026-10)**: migración `AddBranchScopeIndexes` crea índice compuesto `(TenantId, BranchId)` en Sales, Credits, Quotes, Purchases, PurchaseOrders, Warranties, CashRegisters, InventoryMovements, Clients, Products, AccountingEntries (TenantId primero porque el query filter de compañía siempre precede). Vehicle/Driver/Route ya tenían índice `BranchId` propio. Ojo al desplegar: `CreateIndex` toma lock breve de escritura; en tablas grandes conviene crearlos con `CONCURRENTLY` a mano.
- **Drift corregido (2026-10)**: la entidad `WarrantySlaConfig` (Fase 1) existía en el modelo pero NUNCA se migró — la migración `AddWarrantySlaConfigTable` la crea. Si en algún entorno la tabla ya estaba creada a mano, esa migración fallará; verificar antes de aplicar.
- **Convención de servicios**: `filter.BranchId ?? _tenant.ResolveBranchId() ?? Guid.Empty` (el filtro explícito del usuario manda sobre el contexto). Aplicado en Sale, Purchase, Product, InventoryMovement, Client, Account, Quote, Credit, Warranty, CashRegister (PurchaseOrder vía `PurchaseOrdersController` con `Guid? branchId`).
- **Bugs corregidos en esta pasada**: `QuoteRepository` y `PurchaseOrderRepository` aplicaban `.Where(x => x.BranchId == branchId)` sin guardia → con `Guid.Empty` ("todas") solo devolvían entidades SIN sucursal (las cotizaciones/OCs con branch real eran invisibles en el listado). Ahora usan el helper. Regresión en `QuoteRepositoryTests`/`PurchaseOrderRepositoryTests` (in-memory, matriz Empty→todas / concreto→filtrado / inexistente→vacío).
- **Company-wide POR DISEÑO (no tocar)**: reportes financieros, contabilidad, BI/dashboards, nómina, `DynamicReportEngine`, y el historial cross-branch de un cliente (`ClientService.GetHistoryAsync` pasa `Guid.Empty` a propósito).
- **Fleet (2026-10, pass completado)**: las 33 entidades Fleet solo exponían `BranchId` en **Vehicle, Driver y Route**. Ese pass las cubrió. **Ampliación transitiva (2026-10)**: `Trip`, `Delivery`, `FuelRefill`, `WorkOrder` y `FleetExpense` no tienen `BranchId` propio; su sucursal se hereda por navegación vía el helper `ApplyBranchScopeTransitive` (ver sección del helper). `IVehicleRepository`/`IDriverRepository` exponen `GetAllAsync(companyId, Guid? branchId = null)`, `IRouteRepository`/`ITripRepository`/`IDeliveryRepository` `GetAllAsync(Guid? branchId = null)` e `IFuelRefillRepository`/`IWorkOrderRepository`/`IFleetExpenseRepository` `GetAllAsync(Guid companyId, Guid? branchId = null)`; los servicios pasan `branchId ?? _tenant.ResolveBranchId()` (parametro opcional → ojo: Moq exige pasar el argumento explícito en los Setup/Verify). `FleetExpenseService.GetExportResultAsync`/`GetPendingCountAsync`/`GetPendingAmountAsync` pasan `Guid.Empty` explícito (company-wide por diseño). Los listados operativos (`Vehicles/Drivers/Routes/Trips/Deliveries/FuelRefills/WorkOrders/FleetExpensesController.GetAll`) aceptan `[FromQuery] Guid? branchId`. Reportes y automatización (`FleetReportService`, `FleetAlertService`, `GpsService`, `PredictiveMaintenanceService`, `RouteOptimizationService`) siguen company-wide por diseño.
- **Bug de filtro tenant corregido (2026-10)**: `FuelRefillRepository`, `WorkOrderRepository` y `FleetExpenseRepository` tenían `.Where(e => e.TenantId == companyId.ToString())` — comparaban el `TenantId` (string de tenant) contra el `companyId` (Guid de compañía): **nunca coincidía → 0 filas**. Se eliminó el predicado; estos repos se apoyan en el `HasQueryFilter` de compañía (igual que Vehicle/Driver). El parámetro `companyId` se conserva por contrato de interfaz pero no se usa.
- **Employee ya NO es company-wide (2026-10)**: `Employee` gana `Guid? BranchId` + nav `Branch` (FK `Restrict`, índice `(TenantId, BranchId)`; migración `AddEmployeeBranch`). El guardado sella la sucursal del header vía `TenantAuditInterceptor.StampBranchFromContext` (o `CreateEmployeeRequest.BranchId` explícito); el listado filtra con `filter.BranchId ?? _tenant.ResolveBranchId()` en `EmployeeService.GetFilteredAsync` + `EmployeeRepository.GetFilteredAsync/GetFilteredCountAsync` (usa `ApplyBranchScope`). Nómina/procesamiento de planilla SIGUE company-wide (`PayrollService`/`VacationAutomatedJob` pasan branch omitido = todas; `EnforceEmployeeLimitAsync` cuenta todas las sucursales de la compañía a propósito).
- Tests de contrato por entidad: matriz filtro×contexto (sin branch→Empty; contexto→esa sucursal; filtro explícito→manda) en `QuoteServiceTests`, `CreditServiceTests`, `WarrantyServiceTests`, `CashRegisterServiceTests`, `VehicleServiceTests`, `DriverServiceTests`, `RouteServiceTests`, `EmployeeServiceBranchScopeTests` + `BranchScopeExtensionsTests`; matriz de visibilidad en repos in-memory (`QuoteRepositoryTests`, `PurchaseOrderRepositoryTests`, `FleetRepositoryBranchScopeTests`, `EmployeeRepositoryBranchScopeTests`). Ojo Moq: agregar `branchId` opcional a un método rompe los `Setup`/`Verify` con expression tree (CS0854) → pasar `It.IsAny<Guid?>()` explícito.
- Gotcha de tests: con el proveedor in-memory, un `Include` de nav **requerida** (`Client`, `Supplier`, `Branch`, `Brand`…) funciona como INNER JOIN → si el seed no crea la fila principal, el repo devuelve 0 filas y el test "falla" sin querer. Seedear siempre las principales.

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
  - **RLS**: `[Migration("20260617190000_EnableRLS")]` añadido (EF la ignora sin él) + SQL embebido como recurso (`SecurityScripts.SECURITY_RLS.sql` via csproj, fallback a disco) → funciona en Render/Docker sin depender del CWD. `SECURITY_RLS.sql`: 8 nombres de tablas inventados corregidos contra el esquema real (`FleetVehicles→Vehicles`, `FleetDrivers→Drivers`, `FleetRoutes→Routes`, `FleetRoutePoints→RoutePoints`, `FleetDeliveries→Deliveries`, `FleetDeliveryItems→DeliveryItems`, `FleetTrips→Trips`, `GoalAssignmentProgressEntries→GoalProgressEntries`), Phase 1 con `IF EXISTS`, idempotente. `test_rls_staging.sql` alineado. **FORCE ACTIVADO 2026-10-14** (`ForceTenantRowLevelSecurity`): ver la nota de la sección "Filtro de sucursal" — escape de contexto ausente + política combinada tenant/sucursal por el OR de PostgreSQL. Tests: `EnableRlsMigrationTests` (5).
  - **API Keys** (antes: 401 siempre): `ApiKeyService.ValidateKeyAndGetInfoAsync` con `IgnoreQueryFilters()` (corre antes de resolver tenant; el hash SHA-256 es unívoco y la key ES la credencial); `ApiKeyMiddleware` fabrica `ClaimsPrincipal` autenticado (`authenticationType: ApiKey`) con `tenant_id`/`sub` + roles y permisos heredados del usuario dueño (misma construcción de claims que JwtService) → `[Authorize]` y `RequirePermission` funcionan; `TenantMiddleware` ya no pisa el usuario fijado por API key cuando no hay claims JWT. Tests: `ApiKeyServiceTests` (5).
  - **IdempotentAttribute**: cache key = `tenant|método|path|X-Idempotency-Key` (antes solo la key → la misma key de dos compañías se solapaba y devolvía la respuesta del otro tenant). Hoy 0 controllers lo usan; corregido por si se activa.
  - **Limpieza**: `TenantContextExtensions.ValidateBranchOwnership` (no-op, 0 referencias) eliminada — la validación real vive en `BranchValidator.ResolveForWriteAsync` y `TenantMiddleware.ResolveBranchAsync`.
  - **FirebaseStorageService (2026-10)** → ya NO devuelve URLs públicas crudas; ver sección "Almacenamiento de archivos".
- **Estado global remediación: COMPLETA (Fases 1-4).**

## Licenciamiento modular (2026-10) — venta por módulos
Sistema de entitlements plan→módulo para vender el ERP por módulos. Build limpio + **897/897** backend + **344/344** frontend (`flutter analyze` limpio).
- **Catálogo global**: `ModuleType` enum (`Zorvian.Core/Enums/ModuleType.cs`): Sales, Inventory, Purchases, Fleet, PalmTrack, Finance, Hr, Bi, Communication, Administration, Tax. Entidad `PlanModule` (`Zorvian.Core/Entities/PlanModule.cs`) **NO hereda `BaseEntity`** (es catálogo global, sin TenantId/CompanyId ni query filter) con índice único `(PlanId, Module)`. Migración `20261012211601_AddPlanModuleEntity` (también captura drift previo de `BonusRecords`/`OvertimeRecords`: max lengths + FKs `Restrict`). DbSet `CompanyPlanPricings` corregido (era `Set<CompanyPlanPricings>()`).
- **Seed** (`SeedService`): idempotente y global (`if (!await _db.PlanModules.AnyAsync())`). `starter` = Sales/Inventory/Hr/Administration; `professional` = + Purchases/Fleet/Finance/Bi/Communication; `enterprise` = todo (`Enum.GetValues<ModuleType>()`).
- **Servicio**: `IModuleEntitlementService` (interfaz en `Zorvian.Application.Services`) + impl `Zorvian.Infrastructure.Services.ModuleEntitlementService` (usa `ZorvianDbContext`; `Application` NO referencia EF/Infrastructure → la impl va en Infrastructure). **Fallback permisivo**: si el plan no tiene NINGÚN `PlanModule` (tenant pre-licenciamiento o sin seed), `IsModuleEnabledAsync`/`GetEnabledModulesAsync` devuelven acceso total — evita romper instalaciones existentes. Sólo con mappings configurados se aplica el filtro estricto. Registrado scoped en `DependencyInjectionExtensions`. **1 sola query**: ambos métodos traen las filas del plan (`Select(new { Module, IsEnabled })`) y evalúan en memoria (antes 2 queries por request).
- **Enforcement backend**: `ModuleEnforcementFilter` (`Zorvian.Web/Filters/`) registrado **global** en `Program.cs` (`options.Filters.Add<ModuleEnforcementFilter>()`). Mantiene un `Dictionary<string, ModuleType>` controller→módulo (coincide con el nombre de clase sin sufijo `Controller`); controllers no mapeados = core (siempre permitidos). Lee el claim `plan_id` (default `starter`), salta SuperAdmin y no autenticados, y devuelve **403** (ProblemDetails) si el módulo no está en el plan. (**Nota**: el `RequireModuleAttribute` que existió se eliminó — el filtro global con mapa lo reemplaza; granularidad por controller, no por acción.) Mapa actual: los 8 `Warranty*` (Communications/Costs/Dashboard/PartRequests/PartUsages/Profitability/Providers/SlaConfigs) → `ModuleType.Inventory`; `Notifications`/`Reports` se dejaron SIN mapear (transversales/core). Únicos controllers sin mapear: `Auth, Badges, Dashboard, Notifications, Registration, Reports, Sso`.
- **JWT**: `IJwtService.GenerateTokens`/`JwtService` ahora reciben `planId` (default `starter`) y emiten el claim `plan_id`. `AuthService` resuelve el plan con `_authRepo.GetCompanyByTenantIdAsync(tenantId).SubscriptionPlan` en login, refresh, `auth/me` y switch-tenant. `UserInfo` DTO expone `Plan`. `SsoService` (PalmTrack) resuelve el plan y lo pasa en sus 2 flujos (ya NO usa el default). **`ApiKeyMiddleware` también emite `plan_id`** (resuelve `company.SubscriptionPlan` vía `_authRepo.GetCompanyByTenantIdAsync(tenantId)`; el filtro de módulos aplica igual con API Keys).
- **`MaxEmployees`**: `Company.MaxEmployees` (default 50). `EmployeeService.CreateAsync` llama `EnforceEmployeeLimitAsync()` (usa `IAuthRepository.GetCompanyByTenantIdAsync` + `IEmployeeRepository.GetFilteredCountAsync(null,null,null)`), lanza `InvalidOperationException` con mensaje "Límite de empleados del plan alcanzado" si `count >= MaxEmployees`. `EmployeeService` ahora inyecta `IAuthRepository` + `ITenantContext` (tests constructores actualizados).
- **Frontend**: `AuthState.plan` (default `starter`, poblado desde `user['plan']` en checkAuth/login/mfa/register/switchTenant). `NavModule.module` (clave `ModuleType`) + `NavConfig.modulesByPlan`/`enabledModulesForPlan` (espejo del seed; plan desconocido → `starter`). `getModulesForRole(role, searchQuery, enabledModules)` filtra por plan (SuperAdmin ve todo). `filteredModulesProvider` (`nav_provider.dart`) pasa `enabledModules: NavConfig.enabledModulesForPlan(plan)`.
- **Tests**: `ModuleEntitlementServiceTests` (5), `ModuleEnforcementFilterTests` (6), `EmployeeServiceMaxEmployeesTests` (3). **Gotcha Moq reintroducido**: el nuevo `planId` opcional en `GenerateTokens` rompió los `Setup` con expression tree (CS0854) → los mocks deben pasar `It.IsAny<string>()` explícito (AuthServiceTests, AuthServiceExtendedTests, SsoServiceTests).
- **Pendiente**: aplicar el mapa a más controllers si se agregan módulos nuevos; gating de rutas directas en el router (hoy el backend es la fuente de verdad).

## Almacenamiento de archivos (2026-10) — bucket privado + URLs firmadas V4
Decisión: el bucket de Firebase (`zorvian-erp.firebasestorage.app`, default) es **privado** (403 anónimo verificado en vivo); antes `GetFileUrl` devolvía la URL pública → assets rotos en producción (logos de compañía, fotos de asistencia, adjuntos de permisos). Ahora se **persiste la ruta del objeto** y se **firman URLs V4 al leer** (TTL 15 min por defecto).
- **Contrato**: `IDocumentStorageService` reescrito — `UploadFileAsync` devuelve el **path** (no URL); **eliminado `GetFileUrl`**; nuevo `Task<string?> GetSignedUrlAsync(string? storedPathOrUrl, TimeSpan? ttl = null)`. `FirebaseStorageService` firma con `UrlSigner.FromCredential(...)` (`Google.Cloud.Storage.V1 4.14.0`, `FirebaseAdmin 3.5.0`), credencial de `FirebaseApp.DefaultInstance.Options.Credential` con fallback `GetApplicationDefault()`, y cae a URL pública + `LogWarning` si el firmado falla. `LocalFileStorageService` devuelve `/{path}` (sin firma). Registro DI en `DependencyInjectionExtensions` (~línea 60) con `ILogger<FirebaseStorageService>`.
- **Normalización centralizada**: `StoragePathHelper.ExtractStoragePath(string?)` (`Zorvian.Application/Helpers/`) acepta URL GCS pública o firmada (host exacto `storage.googleapis.com` — **no substring**, para no confundir `firebasestorage.googleapis.com`), `/path`, y path desnudo; URLs externas (`://` a otro host) → `null`. Regla de persistencia: guardar `ExtractStoragePath(x) ?? x` (aplica `PermissionService`, `CompanyService.Update/UpdateById`). Al leer, `GetSignedUrlAsync(stored)` firma solo si es path de storage; externas → passthrough; null → null. **No hace falta migración de datos** (legacy con URLs públicas se re-normaliza al leer).
- **Puntos de firma**: `CompanyService` (GetAll/Create/GetCurrent/Update/UpdateById + `UploadLogoAsync` guarda path y devuelve URL firmada), `AttendanceService.MapToResponseAsync` (CheckIn/CheckOut), `PermissionService.MapToResponseAsync` (SupportingDocumentUrl). `LeavePermissionsController.Upload` devuelve `{ url: firmada, path, fileName }` — el frontend reenvía `url` y el server normaliza.
- **Jobs**: `OcrProcessingJob` inyecta storage y firma antes del GET; `OrphanFileCleanupService` detecta cualquier storage path (externo no) vía `ExtractStoragePath`; `AttendancePhotoCleanupJob.DeletePhotoAsync` usa `ExtractStoragePath` (antes `new Uri` rompía con bare paths).
- **Frontend**: sin cambios (sigue leyendo `url`/`supportingDocumentUrl`; campos de URL siguen siendo strings). Gotcha: una URL firmada usada para vista previa expira a los 15 min — siempre volver a pedirla al API (los formularios no cachean la URL persistida).
- **Tests**: `StoragePathHelperTests` (10 casos), `DocumentStorageServiceTests` (6 local + 3 firebase passthrough/null). No se testea la firma real (requiere credencial). Suite backend: **897/897**.


## Motor documental (2026-10) - Liquid multi-país y contrato de trabajo
- **Claves con punto en Liquid (bug latente corregido)**: Fluid (Fluid.Core 2.31) NO resuelve ` {{ Company.Name }} ` sobre ` Dictionary<string,string> ` - lo parte en acceso anidado y en un diccionario plano no encuentra ` Company ` → renderiza **vacío** (todas las plantillas sembradas usaban ese patrón). Fix: ` LiquidModelNormalizer.Normalize() ` (` Zorvian.Application/Helpers/LiquidModelNormalizer.cs `) anida las claves con punto (` Company.Name ` → ` { Company: { Name } } `) antes de renderizar; se aplica en ` DocumentService.RenderLiquidTemplate ` (única puerta de render: generación + preview). Claves planas sin punto (` employee_name `) se conservan y las planas no pisan estructuras anidadas. Tests: ` DocumentTemplateLiquidTests ` (incluye tests que documentan el comportamiento crudo del motor).
- **Contrato de Trabajo multi-país v2.1**: ` DocumentTemplateSeeder.BuildContractHtml() ` reescrito - 11 cláusulas, texto con acentos, ` CountryCode = "ALL" ` (aparece para los 6 países). Datos por país vía ` ContractCountries.Get() ` (` Zorvian.Application/Helpers/ContractCountryData.cs `): moneda símbolo+nombre, horas semanales, meses de prueba, días de vacaciones, tipo de ID fiscal, documento de identidad, ley aplicable, entidad tributaria, seguro social, décimo mes y ` VatLabel ` (IVA 15% NIC/GTM, IV 13% CRI/SLV, ISV 15% HND, ITBMS 7% PAN). **Las 6 plantillas migradas a v2.1** (Oferta, Constancia, Cotización, Nota de Entrega, Poder Notarial): acentos, ` Contract.City ` (ya NO "Managua" hardcodeado), moneda/días de ID/país desde ` Country.* `, ` CountryCode = "ALL" ` en Constancia y Poder, totals de cotización con ` Sale.Subtotal/Tax/Discount ` (casing correcto, antes ` sale.* ` renderizaba 0) + ` Country.VatLabel `, labels del wizard en "trabajador". **Upgrade generalizado** (` UpgradeSeededTemplatesAsync `): refresca cualquier plantilla sembrada con ` Version ∈ {1.0, 2.0} ` emparejando por nombre normalizado (sin acentos, minúsculas) → renombres como "Cotización Comercial" matchean "Cotizacion Comercial"; se ejecuta ANTES del check ` AnyAsync ` (incluso BD ya sembrada se refresca). Extracción: ` BuildTemplates(now) `.
- **Visibilidad en producción (bug corregido - accesible desde RRHH y Motor Documental)**: el seed crea las plantillas con ` TenantId = "SYSTEM" ` una sola vez en Startup, pero el query filter de ` DocumentTemplate ` exigía ` TenantId == actual ` → **ninguna compañía veía las 6 plantillas en producción** (lista del motor documental vacía, ` GetByIdAsync ` → 404 en quick-generate, ` Include(Template) ` null en detalle). Fix: filtro **híbrido** en ` ZorvianDbContext ` (` TenantId == actual || TenantId == DocumentTemplate.SystemTenantId || BypassTenantFilter `) - constant ` DocumentTemplate.SystemTenantId = "SYSTEM" ` en la entidad. Las plantillas de otras compañías siguen aisladas; SuperAdmin con bypass ve todo. ` SECURITY_RLS.sql ` NO cubre DocumentTemplates (sin política tenant, sin impacto RLS). Tests: ` DocumentTemplateSystemVisibilityTests ` (5).
- **Wizard frontend - módulo mal mapeado (bug corregido)**: ` quick_generate_wizard_page ` filtraba ` t.module == 'HR'/'Sales' ` pero el vocabulario real de ` module ` (editor + seed) es ` Employee / Sale / Credit / Warranty / General ` (` HR `/` Sales ` son ` category `) → **cero plantillas en los chips Trabajador/Venta/Cliente** ("Sin plantillas para RRHH"). Fix: employee→` {Employee, General} `, sale/client→` {Sale, General} ` (General = Poder Notarial aplica a toda entidad) y preselect de ` t.module ` acepta ` Employee`/` Sale ` además de los legacy ` HR`/` Sales `. Rutas: ` /documents ` NO está en `_routeRoles ` → ` defaultRoles ` (incluye Rrhh) ✓. ` Documents → ModuleType.Administration ` en ` ModuleEnforcementFilter ` (starter incluye Administration ✓).
- **Contexto base para el wizard manual**: ` DocumentCompanyContext.Build(company) ` (` Zorvian.Application/Helpers/ `) centraliza ` Company.*/Country.(15 claves)/Contract.City `; lo usan ` DocumentGenerationService.GetEntityContextAsync ` (refactor - ya no inline) y ` DocumentService.WithCompanyContextAsync ` que **mergea la base BAJO las variables del usuario** en ` GenerateProfessionalDocumentAsync ` + ` RenderLiquidPreviewAsync ` (nuevos deps ` ICompanyRepository + ITenantContext `) → el path manual ` POST /documents/generate|preview ` también renderiza datos multi-país sin pasarlos a mano. Contexto de venta ampliado: ` Sale.Subtotal/Tax/Discount ` + alias planos ` quote_number/quote_date/delivery_date `.
- **Tests nuevos**: ` DocumentTemplateLiquidTests ` (19: 9 previos + theory de los 6 builders con modelo rico y asserts anti-hardcode de Managua/Nicaragua + 4 de contenido) + ` DocumentTemplateSeederTests ` (4: seed 6×v2.1, idempotente, upgrade por nombre normalizado sin tocar plantillas de usuario, upgrade idempotente) + ` DocumentTemplateSystemVisibilityTests ` (5). Gotcha C#: en ` $@"..." ` las variables Liquid van con 4 llaves ` {{{{ X }}}} ` (2 escapean a 1) y ` {{% if %}} ` produce ` {% if %} `; una llave suelta en el body rompe el build (CS8086) - hay que contar que cada run cierre con 4. Gotcha Edit/regex: los builders usan saltos LF (no CRLF). Suite backend al cierre: **939/939**, ` flutter analyze lib ` 0 issues, ` flutter test ` **347/347**.
- **Pendiente local**: el reset de la BD local (` zorvian `) NO se ejecutó - PostgreSQL local exige password scram y no está en repo/user-secrets/env; el upgrade en startup refresca el contenido en sitio la próxima vez que corra la API (no hace falta borrar filas).
- **Contexto de entidad**: ` DocumentGenerationService.GetEntityContextAsync ` expone ` Company.LegalName/TaxIdLabel/CountryCode/Currency/Address `, ` Branch.Name/Address `, ` Contract.City ` (**lugar de emisión = domicilio de la compañía**, Company NO tiene campo City; fallback: sucursal del empleado) y 15 claves ` Country.* ` (incluye ` WeeklySchedule `: NIC/PAN "de lunes a sábado", resto "de lunes a viernes"). El empleado se carga con ` IEmployeeRepository.GetByIdWithBranchAsync ` (nuevo, ` Include(Branch) ` - ojo: ` GetFilteredAsync ` NO incluye Branch). País resuelto con ` FiscalYearHelper.MapCountryToCode ` (ISO-3; default NIC). Wording del contrato: **"EL TRABAJADOR"** (nunca "empleado"; label del wizard "Nombre del Trabajador").
