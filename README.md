# 🏛️ Zorvian ERP

**"Tan simple como una aplicación moderna, tan robusto como un ERP empresarial."**

Zorvian ERP es un sistema ERP moderno multiplataforma construido con **Flutter** (frontend) y **ASP.NET Core 9** (backend), diseñado para empresas de Centroamérica y el Caribe.

**Países soportados:** 🇳🇮 Nicaragua · 🇨🇷 Costa Rica · 🇬🇹 Guatemala · 🇭🇳 Honduras · 🇸🇻 El Salvador · 🇵🇦 Panamá

---

## 🚀 Stack Tecnológico

| Capa | Tecnología |
|------|-----------|
| **Frontend** | Flutter 3.x, Riverpod 2.x, GoRouter, Material 3 |
| **Backend** | ASP.NET Core 9, Entity Framework Core 9 |
| **Base de Datos** | PostgreSQL 16 |
| **Cache** | Redis 7 |
| **Auth** | Firebase Auth + JWT |
| **Realtime** | SignalR |
| **Jobs** | Hangfire |
| **AI** | Google AI (Embeddings, OCR, Predictions) |
| **CI/CD** | GitHub Actions |
| **Container** | Docker + Docker Compose |

---

## 📋 Módulos

| Módulo | Estado | Descripción |
|--------|--------|-------------|
| 🏢 **Multisucursal** | ✅ | Gestión de sucursales por empresa |
| 🛒 **Comercial** | ✅ | Clientes, cotizaciones, ventas, notas de crédito |
| 📦 **Inventario** | ✅ | Productos, kardex, ajustes, proveedores |
| 💳 **Créditos** | ✅ | Cartera de créditos, mora, cobranza, refinanciamiento |
| 💰 **Caja** | ✅ | Cajas registradoras, movimientos, arqueos |
| 🏦 **Tesorería** | ✅ | Cheques, libros de cheques, conciliación |
| 🧾 **Contabilidad** | ✅ | Catálogo, asientos automáticos, centros de costo, presupuestos |
| 👥 **RRHH** | ✅ | Empleados, asistencia, vacaciones, permisos |
| 💵 **Nómina** | ✅ | Cálculo INSS/IR, préstamos, embargos, ACH |
| 🔧 **Garantías** | ✅ | SLA, proveedores, talleres, costos, rentabilidad |
| 📊 **Activos Fijos** | ✅ | Depreciación, revaluación, baja, mantenimiento |
| 🤖 **IA** | ✅ | Chatbot, OCR, predicciones de ausentismo y ventas |
| 📈 **BI** | ✅ | Dashboards ejecutivo, financiero, comercial, operativo |
| 🔗 **Webhooks** | ✅ | Suscripciones, entregas, logs |

---

## 🏗️ Arquitectura

```
Zorvian ERP
├── frontend/                    # Flutter (Web, Android, iOS)
│   └── lib/
│       ├── app/                 # Router, Theme, App Shell
│       ├── auth/                # Auth Provider
│       ├── core/                # Services, Network, Storage
│       ├── features/            # Feature modules (30+)
│       ├── shared/ds/           # Design System (22 components)
│       └── l10n/                # Localization (es/en)
├── src/                         # ASP.NET Core Backend
│   ├── Zorvian.Core/            # Entities, Enums, Interfaces
│   ├── Zorvian.Application/     # Services, DTOs, Interfaces
│   ├── Zorvian.Infrastructure/  # EF Core, Repos, External Services
│   └── Zorvian.Web/             # Controllers, Middleware, Extensions
├── tests/                       # Unit & Integration Tests
├── Dockerfile                   # Multi-stage optimized build
└── docker-compose.yml           # Local development stack
```

---

## ⚡ Inicio Rápido

### Opción 1: Docker Compose (Recomendado)

```bash
# Clonar el repositorio
git clone https://github.com/AllSystem23/nexora.git
cd nexora

# Copiar variables de entorno
cp src/Zorvian.Web/.env.example src/Zorvian.Web/.env

# Iniciar servicios
docker-compose up -d

# Verificar
curl http://localhost:8080/health
```

### Opción 2: Desarrollo Local

**Requisitos:**
- .NET 9 SDK
- Flutter 3.x
- PostgreSQL 16
- Redis 7 (opcional)

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

## 🔐 Seguridad

- **Autenticación**: Firebase Auth + JWT tokens
- **Autorización**: RBAC con permisos granulares (RequirePermission)
- **Multi-tenant**: Query Filters en EF Core con TenantId
- **Auditoría**: Logs inmutables + Entity History
- **Rate Limiting**: 120 req/min general, 5 req/15min para auth
- **Security Headers**: CSP, HSTS, X-Frame-Options, Permissions-Policy
- **Circuit Breaker**: Polly con retry + circuit breaker para servicios externos
- **Validación**: FluentValidation + XSS protection

---

## 🧪 Testing

```bash
# Backend tests
dotnet test

# Frontend tests
cd frontend
flutter test --coverage
```

---

## 🚢 Deploy

### Producción

```bash
# Build
docker build -t zorvian-api .

# Run
docker run -p 8080:8080 \
  -e ConnectionStrings__ZorvianDb="your-connection-string" \
  -e Jwt__Secret="your-jwt-secret" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  zorvian-api
```

### CI/CD

El pipeline de GitHub Actions ejecuta automáticamente:
1. **Build** y **Test** del backend y frontend
2. **Security Scan** para detectar credenciales
3. **Deploy** a producción en push a `main`

---

## 📊 Calificación de Auditoría

| Categoría | Calificación |
|-----------|:------------:|
| Arquitectura | 8.5/10 |
| Frontend | 7.5/10 |
| Backend | 8.5/10 |
| Base de Datos | 7.5/10 |
| Seguridad | 7.5/10 |
| UX/UI | 8.0/10 |
| **Global** | **7.8/10** |

---

## 📄 Licencia

Uso interno — Zorvian ERP © 2026