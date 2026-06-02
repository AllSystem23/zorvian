# Stack

## Backend
- **Runtime:** .NET 9.0 (C#)
- **Framework:** ASP.NET Core Controllers
- **ORM:** Entity Framework Core 9
- **Database:** PostgreSQL 15+ hosted on Neon (`ep-solitary-*.neon.tech`)
- **Auth:** Firebase Authentication (JWT validation via Firebase Admin SDK) + custom JWT (symmetric key)
- **Jobs:** Hangfire (recurring tasks)
- **Hosting:** Render (Docker, free tier)

## Backend Modules (2026-06-01)
- **Nexora RH** (existing): Employees, Departments, Vacations, Permissions, Attendance, Payroll
- **Nexora Comercial** (new): Clients, Quotes, Sales, Invoice generation
- **Nexora Inventario** (new): Products, Categories, Brands, Suppliers, Kardex
- **Nexora Créditos** (new): Credits, Installments, Payments, Collections
- **Nexora Caja** (new): Cash register open/close, income/expense tracking
- **Garantías** (new): Product warranties, claims management
- **Multisucursal** (new): Branch entity with CompanyId FK on all operational tables

## Frontend
- **Framework:** Flutter 3.x with `--dart-define` for env vars
- **State Management:** Riverpod (flutter_riverpod)
- **Routing:** go_router 17.x
- **HTTP:** Dio (with interceptors)
- **Auth (client):** firebase_auth (web via Firebase JS SDK compat v11)
- **Platforms:** Web (Firebase Hosting), Android (APK), iOS (pending)
- **Hosting (web):** Firebase Hosting (`nexora-hr.web.app`)

## Infrastructure
- **CI/CD:** Manual (local build + deploy)
- **Docker:** Multi-stage build for Render (linux-x64, globalization invariant)
- **Secrets:** Render Secret Files for Firebase credentials (`/etc/secrets/`)

## Key Versions
- Firebase JS SDK: 11.0.1 (compat)
- firebase_core: ^4.9.0
- firebase_auth: ^6.5.1
- go_router: ^17.2.3
- flutter_riverpod: ^2.x

## Dependencies (Backend NuGet)
- FirebaseAdmin (Firebase Admin SDK)
- Microsoft.AspNetCore.Authentication.JwtBearer
- Hangfire
- Swashbuckle (Swagger)
- Serilog
