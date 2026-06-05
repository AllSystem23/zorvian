# Architecture

## High-level structure
```
Zorvian/
├── frontend/         # Flutter app (web + mobile)
│   ├── lib/
│   │   ├── app/          # App shell, router, theme
│   │   ├── auth/         # Auth state, Firebase login
│   │   ├── core/         # Theme, network, widgets, errors
│   │   ├── features/     # Feature modules (dashboard, employees, etc.)
│   │   └── l10n/         # Localization
│   └── web/              # Web entry point (Firebase JS SDK in index.html)
├── src/
│   ├── Zorvian.Web/       # ASP.NET Core host, controllers, middleware
│   ├── Zorvian.Application/  # Business logic, services, interfaces
│   ├── Zorvian.Core/         # Entities, enums, domain rules (Domain Layer)
│   └── Zorvian.Infrastructure/ # EF Core, repositories, external services
└── docker/               # Docker configuration
```

## Backend Layers (Clean Architecture)
- **Web**: Controllers, middleware, Hangfire dashboard, Swagger
- **Application**: AuthService, SeedService, Module Services (Comercial, Inventario, etc.)
- **Core (Domain)**: User, Employee, Company, and all module entities (Sale, Product, etc.)
- **Infrastructure**: DbContext, repositories, Firebase integration, Migrations

## Auth Flow
1. User enters email/password on Flutter LoginPage
2. Flutter calls Firebase Auth `signInWithEmailAndPassword` (client-side)
3. Returns Firebase ID token → sent to backend `POST /zorvian/v1/auth/login`
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

## Modules Definition

### Multisucursal (Cross-cutting)
- **Entity**: Branch (belongs to Company)
- **Controller**: BranchesController
- All operational entities include CompanyId + BranchId for per-branch isolation and multi-branch management.

### Zorvian Comercial
- **Entities**: Client, Quote, QuoteDetail, Sale, SaleDetail, SalePayment
- **Controllers**: ClientsController, QuotesController, SalesController
- **Routes**: `/zorvian/v1/clients`, `/zorvian/v1/quotes`, `/zorvian/v1/sales`
- **Features**: Cash sales, credit sales, quotes with line items, kardex auto-update on sale.

### Zorvian Inventario
- **Entities**: Category, Brand, Supplier, Product, InventoryMovement (Kardex)
- **Controllers**: ProductsController, CategoriesController, BrandsController, SuppliersController, InventoryMovementsController
- **Routes**: `/zorvian/v1/products`, `/zorvian/v1/categories`, `/zorvian/v1/brands`, `/zorvian/v1/suppliers`, `/zorvian/v1/inventory-movements`
- **Features**: Stock tracking, low-stock alerts, kardex movement audit, top-selling products.

### Zorvian Créditos
- **Entities**: Credit, CreditInstallment, CreditPayment
- **Controllers**: CreditsController
- **Routes**: `/zorvian/v1/credits`
- **Features**: Installment generation, payment registration with principal/interest split, balance tracking.

### Zorvian Caja
- **Entities**: CashRegister, CashMovement
- **Controllers**: CashRegistersController
- **Routes**: `/zorvian/v1/cash-registers`
- **Features**: Open/close cash register, income/expense tracking, difference calculation, per-branch control.

### Garantías
- **Entities**: Warranty, WarrantyClaim
- **Controllers**: WarrantiesController
- **Routes**: `/zorvian/v1/warranties`
- **Features**: Product warranties linked to client+invoice, claim management, expiration tracking.

### Executive Dashboard
- **Endpoint**: `GET /zorvian/v1/dashboard/executive`
- Returns KPIs across all modules: sales, credits, inventory, cash, HR.

## Key Endpoints
- `POST /zorvian/v1/auth/login` — exchange Firebase token for JWT
- `POST /zorvian/v1/auth/login-password` — login with email/password (Firebase validation)
- `POST /zorvian/v1/seed/super-admin` — Super Admin creation (AllowAnonymous, public)
- `GET /health` — health check
- `GET /zorvian/v1/dashboard/executive` — executive KPI dashboard
