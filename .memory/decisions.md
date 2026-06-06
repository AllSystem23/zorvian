# Decisions

## 2026-01-06 Render deployment via Docker

**Context:** Needed to deploy backend to Render free tier.
**Decision:** Use Dockerfile with multi-stage build targeting linux-x64.
**Reason:** Render supports Docker natively; avoids buildpack compatibility issues.
**Consequences:** Need to set `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` and publish with `-r linux-x64`. Dockerfile must copy entire `src/` context.

## 2026-01-06 Firebase credentials as Render Secret File

**Context:** Firebase Admin SDK needs service account JSON file in production.
**Decision:** Store as Render Secret File mounted at `/etc/secrets/`.
**Reason:** Render secrets are mounted as files, not env vars. The app checks both content root and `/etc/secrets/` for the file.
**Consequences:** Config path must be dynamic; env var `Firebase__CredentialPath` points to the secret file.

## 2026-01-06 Super Admin as multi-tenant bypass

**Context:** Super Admin needs to manage all companies; normal query filters would restrict access.
**Decision:** Use `TenantId = "superadmin"` with explicit `IgnoreQueryFilters()` in EF queries for Super Admin users.
**Reason:** Avoids special-case schema; leverages existing TenantId column with a sentinel value.
**Consequences:** DbContext SaveChanges must not auto-assign TenantId when it's explicitly "superadmin" (already non-empty).

## 2026-01-06 Exposed SeedController endpoint publicly

**Context:** No Super Admin user exists on first deployment; no one can log in to create one.
**Decision:** Create `POST /api/v1/seed/super-admin` as AllowAnonymous.
**Reason:** Allows bootstrapping the first admin user without authentication.
**Consequences:** This endpoint is public (no auth). Should be callable only during initial setup; could add IP restriction later.

## 2026-06-01 New module architecture (Comercial, Inventario, Créditos, Caja, Garantías)

**Context:** Needed to extend Nexora ERP from RH-only to a full multi-module ERP suite.

**Decision:**
1. Created standalone entity files in `Nexora.Core.Entities` following existing patterns (inherit BaseEntity, sealed class, file-scoped namespace)
2. Added `Branch` entity for multisucursal support — all operational tables include `CompanyId` and `BranchId`
3. Used explicit repository pattern (one interface + one implementation per aggregate) matching existing convention
4. Services are plain sealed classes with constructor injection (no base service class)
5. DTOs are sealed records in module-specific subdirectories under `DTOs/`
6. Controllers follow existing pattern: `[ApiController]`, `[Authorize]`, `[Route("api/v1/{resource}")]`, `[Audit]` for mutations
7. Moved `PagedResult<T>` from `EmployeeDtos.cs` to `DTOs/Common/PagedResult.cs` to eliminate cross-module dependency
8. Sales auto-create InventoryMovement (kardex) entries to maintain real-time stock
9. Credit sales auto-generate installment schedule with principal/interest split
10. Cash register movements update running totals for expected balance calculation
11. Executive dashboard aggregates KPIs across all modules via existing `DashboardService`

**Reason:** Follow existing Clean Architecture conventions, maintain consistency, avoid code duplication, enable per-branch data isolation.

**Consequences:** ~80 new files across all layers, 18 new DbSets, ~30 new API endpoints, one new EF Core migration.

## 2026-06-01 Gap remediation: Global Exception Middleware + search bars + missing fields + KPIs

**Context:** Gap analysis vs master prompt revealed missing Global Exception Middleware, no search/filter on list pages, missing form fields (Client: City/State/References, Product: MaxStock), hardcoded zeros in dashboard KPIs, and unparsed nested API response.

**Decision:**
1. Created `GlobalExceptionMiddleware` that handles `UnauthorizedAccessException` (403), `KeyNotFoundException` (404), `InvalidOperationException`/`ArgumentException` (400), and generic `Exception` (500) with structured JSON error responses including `traceId`.
2. Added client-side search bars to Client and Product list pages (text field filtering by name/code/doc/phone).
3. Added `city`, `state`, `references` fields to Flutter Client form (backing entity already had them).
4. Added `maxStock` field to Flutter Product form (backing entity already had it), renamed fields to match backend (`sellingPrice`, `costPrice`, `unitOfMeasure`).
5. Added `GetTodaySalesCountAsync`, `GetTotalPortfolioAsync`, `GetTotalCountAsync`, `GetOpenRegistersCountAsync` to backend repositories to eliminate hardcoded zeros in dashboard.
6. Updated `DashboardService.GetExecutiveDashboardAsync()` to use all real values including `TopSelling` products with `TotalSold` counts.
7. Updated Flutter `ExecutiveKpis.fromJson()` to parse the nested backend response structure (commercial/credits/inventory/cash/hr sub-objects).
8. Added `AverageTicket`, `TodaySalesCount`, `OverdueCredits`, `TotalPortfolio`, `MonthlyRecovery`, `OutOfStockProducts`, `TotalProducts`, `TodayIncome`, `TodayExpense`, `OpenRegisters`, `ActiveEmployees`, `PendingVacations`, `ActivePermissions`, `TotalEmployees`, and `TopSelling` products to the Flutter executive dashboard.

**Reason:** Address highest-impact gaps identified in master prompt comparison: production-quality error handling, UX discoverability via search, data completeness, and KPI accuracy.

**Consequences:** Backend builds with 0 errors/warnings. Flutter analyze shows 0 errors (only pre-existing info hints).

## 2026-06-06 TenantId query filter: use .ToString() for InMemory compatibility

**Context:** EF Core InMemory provider can't evaluate the implicit `TenantId ↔ string` operator inside global query filters, causing all filtered queries to return 0 results. This also broke `.Include()` for navigation entities with non-nullable FKs when the related entity has a query filter.
**Decision:** Changed all 78 `HasQueryFilter` expressions from `== _tenantContext.TenantId` to `== _tenantContext.TenantId.ToString()`.
**Reason:** `.ToString()` is well-known and LINQ-friendly; both produce identical SQL (`WHERE "TenantId" = @p0`). The implicit operator is a C# concept that EF Core's expression tree compiler can't always resolve, especially in InMemory.
**Consequences:** Fixes InMemory tests; PostgreSQL behavior is identical (same parameterized query).

## 2026-06-06 InMemory tests: seed related entities for Include resolution

**Context:** EF Core InMemory with `.Include(w => w.Client)` returns 0 results when the Client entity doesn't exist in the InMemory store, even though the navigation is optional (left join in SQL).
**Decision:** Tests now seed all related entities (Client, Product, Brand, Category) in the InMemory database before querying with `.Include()`.
**Reason:** InMemory's navigation resolution requires the related entity to exist in the store; it does not emulate SQL left joins for missing foreign key targets.
**Consequences:** Repository tests are more thorough (verify full Includes), but require more setup code (helper methods for seeding).

## 2026-06-06 router.refresh() for GoRouter redirect

**Context:** Login navigates to Firebase Auth and sets auth state, but GoRouter doesn't redirect because the redirect is only evaluated on navigation events.
**Decision:** Call `router.refresh()` via `ref.listen` in NexoraApp when auth status changes.
**Reason:** GoRouter 17.x requires explicit `refresh()` to re-evaluate redirect; recreating GoRouter via Provider doesn't trigger redirect.
**Consequences:** Extra `ref.listen` in app shell; clean separation of concerns.
