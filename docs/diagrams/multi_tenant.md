# Arquitectura Multi-Tenant

**Zorvian ERP** — Estrategia de Aislamiento y Escalabilidad

---

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#1A0A3E', 'primaryTextColor': '#fff', 'primaryBorderColor': '#546E7A', 'lineColor': '#546E7A', 'secondaryColor': '#2D1B69', 'tertiaryColor': '#141838'}}}%%

graph TB
  subgraph Users["👥 Usuarios por Tenant"]
    T1["🏢 Empresa A<br/>Nicaragua<br/>150 empleados"]
    T2["🏢 Empresa B<br/>Costa Rica<br/>45 empleados"]
    T3["🏢 Empresa C<br/>Guatemala<br/>500 empleados"]
    TN["🏢 Empresa N<br/>(Nuevos Tenants)"]
  end

  subgraph Auth["🔐 Autenticación"]
    FA["Firebase Auth<br/>Multi-tenant por proyecto"]
    JWT["JWT con claims:<br/>- tenant_id<br/>- role<br/>- permissions"]
    TENANT_MW["TenantMiddleware<br/>Extrae tenant_id del JWT"]
  end

  subgraph Isolation["🛡️ Aislamiento"]
    direction TB
    subgraph Phase1["Fase 1: Shared DB (MVP - 100 empresas)"]
      SHD["Single PostgreSQL<br/>Tenant_id en cada tabla"]
      QF["EF Core Query Filters<br/>WHERE tenant_id = @tid"]
      RLS["Row Level Security<br/>Políticas por tenant"]
    end
    subgraph Phase2["Fase 2: Schema-per-Tenant (100-500 empresas)"]
      SCH["Esquemas separados<br/>tenant_a.*, tenant_b.*"]
      MIG["Migraciones por schema<br/>Scripts parametrizados"]
    end
    subgraph Phase3["Fase 3: DB-per-Tenant (500+ empresas)"]
      DBP["Base de datos independiente<br/>Por cliente enterprise"]
      CONN["Connection string routing<br/>Según tenant_id"]
    end
  end

  subgraph Data["📊 Datos por Tenant"]
    CONF["Configuración:<br/>Moneda, País, Zona Horaria"]
    DOCS["Documentos legales<br/>Almacenamiento aislado"]
    REPORTS["Reportes fiscales<br/>Específicos por país"]
  end

  subgraph Billing["💳 Facturación"]
    PLAN["Planes:<br/>Starter / Pro / Enterprise"]
    LIMITS["Límites por plan:<br/>Empleados, Storage, API"]
    METER["Metering:<br/>API calls, Storage GB"]
  end

  %% Flujo de autenticación
  T1 --> FA
  T2 --> FA
  T3 --> FA
  TN --> FA
  FA --> JWT
  JWT --> TENANT_MW

  %% Aislamiento
  TENANT_MW --> Isolation
  Isolation --> Data

  %% Facturación
  Billing --> Limits
  Billing --> Meter
```

---

## Estrategia de Migración

```mermaid
%%{init: {'theme': 'base', 'themeVariables': { 'primaryColor': '#1A0A3E', 'primaryTextColor': '#fff', 'primaryBorderColor': '#546E7A', 'lineColor': '#546E7A'}}}%%

graph LR
    subgraph Now["Hoy: Shared DB"]
        S1["1 Base de Datos<br/>180 tablas con tenant_id"]
        S2["Índices compuestos<br/>(tenant_id + id)"]
        S3["Query Filters EF Core"]
        S4["RLS en PostgreSQL"]
    end
    
    subgraph Next["Próximo: Schema-per-Tenant"]
        N1["N schemas<br/>tenant_a, tenant_b..."]
        N2["Migraciones por schema"]
        N3["Connection pooling<br/>por schema"]
    end
    
    subgraph Future["Futuro: DB-per-Tenant"]
        F1["N bases de datos<br/>PostgreSQL independientes"]
        F2["Router por tenant_id"]
        F3["Aislamiento total"]
    end
    
    S1 --> S2
    S2 --> S3
    S3 --> S4
    S4 --> N1
    N1 --> N2
    N2 --> N3
    N3 --> F1
    F1 --> F2
    F2 --> F3
```

---

## Implementación Actual (Shared Database)

```csharp
// Query Filter en DbContext
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.Entity<Employee>().HasQueryFilter(e =>
        e.TenantId == _tenantContext.TenantId);
    builder.Entity<Product>().HasQueryFilter(p =>
        p.TenantId == _tenantContext.TenantId);
    // Aplica a todas las 180+ entidades
}

// Middleware que inyecta TenantId
public class TenantMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value;
        _tenantContext.SetTenantId(tenantId);
        await _next(context);
    }
}
```

---

## Matriz de Aislamiento por Fase

| Aspecto | Shared DB | Schema-per-Tenant | DB-per-Tenant |
|---------|:---------:|:-----------------:|:-------------:|
| Costo operativo | 🟢 Bajo | 🟡 Medio | 🔴 Alto |
| Aislamiento de datos | 🟡 Parcial | 🟢 Bueno | 🟢 Total |
| Complejidad código | 🟢 Baja | 🟡 Media | 🔴 Alta |
| Backup/Restore | 🟡 Masivo | 🟢 Por tenant | 🟢 Por tenant |
| Escalabilidad | 🟡 100 tenants | 🟢 500 tenants | 🟢 Ilimitado |
| Mantenimiento DBA | 🟢 Simple | 🟡 Medio | 🔴 Complejo |
| Cross-tenant queries | 🟢 Fáciles | 🟡 Posibles | 🔴 Imposibles |

---

## Seguridad Multi-Tenant

| Capa | Mecanismo |
|------|-----------|
| **API** | JWT con claim `tenant_id` validado en cada request |
| **Middleware** | `TenantMiddleware` extrae y valida tenant_id |
| **ORM** | EF Core Query Filters globales en todas las queries |
| **Base de Datos** | Row Level Security (RLS) como defensa adicional |
| **Storage** | Buckets/Paths separados por tenant en GCS |
| **Logs** | Todos los audit logs incluyen tenant_id |
| **Cache** | Redis keys con prefijo `tenant:{id}:` |
| **Queues** | RabbitMQ exchanges separados por tenant |
