# Architecture

## High-level structure
```
Nexora/
├── frontend/         # Flutter app (web + mobile)
│   ├── lib/
│   │   ├── app/          # App shell, router, theme
│   │   ├── auth/         # Auth state, Firebase login
│   │   ├── core/         # Theme, network, widgets, errors
│   │   ├── features/     # Feature modules (dashboard, employees, etc.)
│   │   └── l10n/         # Localization
│   └── web/              # Web entry point (Firebase JS SDK in index.html)
├── src/
│   ├── Nexora.Web/       # ASP.NET Core host, controllers, middleware
│   ├── Nexora.Application/  # Business logic, services, interfaces
│   ├── Nexora.Domain/    # Entities, enums, domain rules
│   └── Nexora.Infrastructure/ # EF Core, repositories, external services
└── docker/               # Docker configuration
```

## Backend Layers (Clean Architecture)
- **Web**: Controllers, middleware, Hangfire dashboard, Swagger
- **Application**: AuthService, SeedService, etc.
- **Domain**: User, Employee, Company, etc.
- **Infrastructure**: DbContext, repositories, Firebase integration

## Auth Flow
1. User enters email/password on Flutter LoginPage
2. Flutter calls Firebase Auth `signInWithEmailAndPassword` (client-side)
3. Returns Firebase ID token → sent to backend `POST /api/v1/auth/login`
4. Backend validates ID token via Firebase Admin SDK
5. Backend finds or creates User in DB, generates custom JWT
6. Backend returns `{accessToken, refreshToken, userId, role, tenantId}`
7. Flutter stores tokens in SecureStorage, updates AuthState
8. GoRouter redirect fires → navigates to /dashboard or /onboarding

## Multi-tenancy
- TenantId on all entities
- Global query filter in EF Core: `TenantId == CurrentTenantId`
- Super Admin has explicit `TenantId = "superadmin"`, bypasses filter via `IgnoreQueryFilters()`
- Login determines tenant context from FirebaseUid → User.TenantId

## New Modules (2026-06-01)

### Nexora Comercial
- **Entities**: Client, Quote, QuoteDetail, Sale, SaleDetail, SalePayment
- **Controllers**: ClientsController, QuotesController, SalesController
- **Routes**: `/api/v1/clients`, `/api/v1/quotes`, `/api/v1/sales`
- **Features**: Cash sales, credit sales, quotes with line items, kardex auto-update on sale

### Nexora Inventario
- **Entities**: Category, Brand, Supplier, Product, InventoryMovement (Kardex)
- **Controllers**: ProductsController, CategoriesController, BrandsController, SuppliersController, InventoryMovementsController
- **Routes**: `/api/v1/products`, `/api/v1/categories`, `/api/v1/brands`, `/api/v1/suppliers`, `/api/v1/inventory-movements`
- **Features**: Stock tracking, low-stock alerts, kardex movement audit, top-selling products

### Nexora Créditos
- **Entities**: Credit, CreditInstallment, CreditPayment
- **Controllers**: CreditsController
- **Routes**: `/api/v1/credits`
- **Features**: Installment generation, payment registration with principal/interest split, balance tracking

### Nexora Caja
- **Entities**: CashRegister, CashMovement
- **Controllers**: CashRegistersController
- **Routes**: `/api/v1/cash-registers`
- **Features**: Open/close cash register, income/expense tracking, difference calculation, per-branch control

### Garantías
- **Entities**: Warranty, WarrantyClaim
- **Controllers**: WarrantiesController
- **Routes**: `/api/v1/warranties`
- **Features**: Product warranties linked to client+invoice, claim management, expiration tracking

### Multisucursal (cross-cutting)
- **Entity**: Branch (belongs to Company)
- **Controller**: BranchesController
- All operational entities include CompanyId + BranchId for per-branch isolation

### Executive Dashboard
- **Endpoint**: `GET /api/v1/dashboard/executive`
- Returns KPIs across all modules: sales, credits, inventory, cash, HR

## Key Endpoints
- `POST /api/v1/auth/login` — exchange Firebase token for JWT
- `POST /api/v1/seed/super-admin` — Super Admin creation (AllowAnonymous, public)
- `GET /health` — health check
- `GET /api/v1/dashboard/executive` — executive KPI dashboard
