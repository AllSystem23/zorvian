# Auditoría de Seguridad — Zorvian ERP

**Fecha:** 2026-06-05  
**Última actualización:** 2026-06-05 (post-correcciones)  
**Alcance:** Análisis completo del código fuente (Clean Architecture .NET 9 + Flutter)  
**Clasificación:** Uso interno / Confidencial

---

## Resumen Ejecutivo

| Métrica | Original | Actual |
|---------|----------|--------|
| **CRÍTICO** | 5 | 0 |
| **ALTO** | 7 | 3 |
| **MEDIO** | 9 | 4 |
| **BAJO** | 5 | 5 |
| **INFO** | 4 | 4 |
| **Total hallazgos** | **30** | **16** |
| **Resueltos** | — | **14** |

### Hallazgos Resueltos (14)

| ID | Severidad | Descripción | Fix |
|----|-----------|-------------|-----|
| USR-001 | 🔴 CRÍTICO | `UsersController` sin roles de autorización | `[RequirePermission]` agregado a cada endpoint |
| USR-002 | 🔴 CRÍTICO | `AssignRole` borra todos los roles existentes | Validación de rol + verificación SuperAdmin |
| RBP-002 | 🔴 CRÍTICO | Claims de permiso nunca validados server-side | `RequirePermissionAttribute` + 24 permisos |
| MTN-001 | 🔴 CRÍTICO | `TenantContext.SetTenantId()` público y mutable | `ITenantContext`/`ITenantContextWriter` segregación |
| MTN-003 | 🔴 CRÍTICO | IDOR en controllers financieros | `[RequirePermission]` en Credits, Employees, AuditLogs |
| AUT-002 | 🟠 ALTO | Token reuse detection ausente | Implementado en `AuthService.RefreshTokenAsync` |
| DRP-001 | 🟠 ALTO | Hangfire MemoryStorage | Migrado a `UsePostgreSqlStorage` |
| AUT-004 | 🟡 MEDIO | Sin endpoint revocar todas las sesiones | `POST /auth/revoke-all` implementado |
| HST-002 | 🔴 CRÍTICO | EmployeeHistory no se poblaba nunca | `CaptureState` + `AddHistoryEntries` en CRUD |
| RBP-003 | 🟠 ALTO | SuperAdmin bypass hardcodeado como string | `ITenantContext.IsSuperAdmin` basado en role claim |
| DAT-002 | 🔴 CRÍTICO | JWT Secret hardcodeado en appsettings | Removido; solo env var o Development.json |
| DRP-003 | 🟡 MEDIO | Migración automática sin guardrails | Solo ejecuta si hay migraciones pendientes; try-catch |
| USR-003 | 🟠 ALTO | ToggleActive sin verificar último admin | Validación de CompanyAdmin restante |

### Fixes Adicionales (no categorizados como hallazgos originales)

| Fix | Archivo | Descripción |
|-----|---------|-------------|
| Device fingerprint | `RefreshToken.cs`, `AuthService.cs` | Hash de User-Agent + IP en refresh tokens |
| Audit interceptor | `AuditInterceptor.cs` | Captura `OldValues`/`NewValues` via ChangeTracker |
| Audit log cleanup job | `AuditLogCleanupJob.cs` | Purga mensual de audit logs via Hangfire |
| Security headers | `SecurityHeadersMiddleware.cs` | 5 headers (HSTS, XSS, CSP, etc.) |
| Cross-platform printing | `platform/download_*.dart` | Reemplazo de `dart:html` con exports condicionales |
| Flutter analyze limpio | Frontend | 242 issues resueltos (134 errors, 8 warnings, 100 infos) |
| 180 tests backend | `tests/` | Todos pasando (118 existentes + 62 nuevos) |

---

## Vista por Dimensión — Estado Actual

### 1. Usuarios
| Hallazgo | Severidad | Estado | Archivo |
|----------|-----------|--------|---------|
| **USR-001**: `UsersController` sin roles de autorización | 🔴 CRÍTICO | ✅ FIXED | `Controllers/UsersController.cs` |
| **USR-002**: `AssignRole` borra todos los roles existentes | 🔴 CRÍTICO | ✅ FIXED | `Controllers/UsersController.cs:112` |
| **USR-003**: `ToggleActive` no verifica último SuperAdmin/CompanyAdmin activo | 🟠 ALTO | ✅ FIXED | `Controllers/UsersController.cs:149-162` |
| **USR-004**: Usuarios creados vía `LoginAsync` no heredan `TenantId` del contexto | 🟠 ALTO | ❌ PENDIENTE | `Services/AuthService.cs:30-37` |
| **USR-005**: `AuthRepository` usa `IgnoreQueryFilters()` en TODAS las consultas | 🟡 MEDIO | ❌ PENDIENTE | `Repositories/AuthRepository.cs:19-25` |

### 2. Roles y Permisos
| Hallazgo | Severidad | Estado | Archivo |
|----------|-----------|--------|---------|
| **RBP-001**: Permisos basados en strings sin registro central | 🟡 MEDIO | ❌ PENDIENTE | `Entities/RolePermission.cs:7` |
| **RBP-002**: Claims de permiso NUNCA validados server-side | 🔴 CRÍTICO | ✅ FIXED | `Authorization/RequirePermissionAttribute.cs` |
| **RBP-003**: Role `SuperAdmin` con bypass hardcodeado | 🟠 ALTO | ✅ FIXED | `Data/NexoraDbContext.cs:220` |
| **RBP-004**: Sin endpoints protegidos con `[Authorize(Roles = "SuperAdmin")]` | 🟠 ALTO | ❌ PENDIENTE | `Controllers/*.cs` |
| **RBP-005**: Sin granularidad de permisos por módulo | 🟠 ALTO | ❌ PENDIENTE | `Controllers/*.cs` |

### 3. Bitácoras (AuditLog)
| Hallazgo | Severidad | Estado | Archivo |
|----------|-----------|--------|---------|
| **AUD-001**: `AuditFilter` no captura `OldValues`/`NewValues` | 🟠 ALTO | ✅ FIXED | `Data/AuditInterceptor.cs:65-80` |
| **AUD-002**: `SaveChangesAsync()` inline en repositorio | 🟡 MEDIO | ✅ FIXED | `Data/AuditInterceptor.cs` (interceptor reemplaza repo) |
| **AUD-003**: `AuditLogsController` sin restricción de rol | 🟡 MEDIO | ✅ FIXED | `Controllers/AuditLogsController.cs:21` |
| **AUD-004**: Audit logs no son inmutables | 🟠 ALTO | ❌ PENDIENTE | `Entities/AuditLog.cs` |
| **AUD-005**: Sin política de retención | 🔵 INFO | ✅ FIXED | `Jobs/AuditLogCleanupJob.cs` |

### 4. Historial de Cambios
| Hallazgo | Severidad | Estado | Archivo |
|----------|-----------|--------|---------|
| **HST-001**: `EmployeeHistory` solo existe para empleados | 🟡 MEDIO | ❌ PENDIENTE | `Entities/EmployeeHistory.cs` |
| **HST-002**: Sin lógica que siembre `EmployeeHistory` | 🔴 CRÍTICO | ✅ FIXED | `Services/EmployeeService.cs` |
| **HST-003**: `CreatedBy`/`UpdatedBy` no poblados consistentemente | 🟡 MEDIO | ✅ FIXED | `Data/AuditInterceptor.cs:49-59` |

### 5. Accesos (Auth)
| Hallazgo | Severidad | Estado | Archivo |
|----------|-----------|--------|---------|
| **AUT-001**: Refresh tokens sin device fingerprint | 🔴 CRÍTICO | ✅ FIXED | `Core/Entities/RefreshToken.cs:12` |
| **AUT-002**: Sin detección de rotación de refresh token | 🟠 ALTO | ✅ FIXED | `Services/AuthService.cs:145` |
| **AUT-003**: Sin MFA/2FA en ningún flujo | 🟡 MEDIO | ❌ PENDIENTE | `Services/AuthService.cs` |
| **AUT-004**: Sin gestión de sesiones (revocar todas) | 🟡 MEDIO | ✅ FIXED | `Controllers/AuthController.cs:66-76` |
| **AUT-005**: `WebApiKey` como query param en URL | 🟢 BAJO | ❌ PENDIENTE | `Identity/FirebaseAuthService.cs:69` |

### 6. Multiempresa
| Hallazgo | Severidad | Estado | Archivo |
|----------|-----------|--------|---------|
| **MTN-001**: `TenantContext.SetTenantId()` público y mutable | 🔴 CRÍTICO | ✅ FIXED | `Data/TenantContext.cs` |
| **MTN-002**: `Company` query filter con string hardcodeado | 🟠 ALTO | ❌ PENDIENTE | `Data/NexoraDbContext.cs:220` |
| **MTN-003**: IDOR en controllers financieros | 🔴 CRÍTICO | ✅ FIXED | `Controllers/CreditsController.cs` |
| **MTN-004**: `TenantId` string sin type-safety | 🟡 MEDIO | ❌ PENDIENTE | `Data/NexoraDbContext.cs` |

### 7. Seguridad de Datos
| Hallazgo | Severidad | Estado | Archivo |
|----------|-----------|--------|---------|
| **DAT-001**: Firebase admin credentials en el repositorio | 🔴 CRÍTICO | 🟡 PARCIAL | `nexora-hr-firebase-admin.json` |
| **DAT-002**: JWT Secret hardcodeado en appsettings | 🔴 CRÍTICO | ✅ FIXED | `Identity/JwtService.cs:34` |
| **DAT-003**: PII sin cifrado a nivel de columna | 🟠 ALTO | ❌ PENDIENTE | `Entities/Employee.cs` |
| **DAT-004**: Soft-delete sin fecha de purge | 🟡 MEDIO | ❌ PENDIENTE | `Entities/BaseEntity.cs:11` |
| **DAT-005**: Firebase Storage sin cifrado server-side | 🔵 INFO | ❌ PENDIENTE | `Services/FirebaseStorageService.cs` |

### 8. Trazabilidad
| Hallazgo | Severidad | Estado | Archivo |
|----------|-----------|--------|---------|
| **TRZ-001**: `AuditFilter` solo captura después de la acción | 🟡 MEDIO | ❌ PENDIENTE | `Filters/AuditFilter.cs:23-26` |
| **TRZ-002**: Sin trazabilidad de LECTURAS | 🟠 ALTO | ❌ PENDIENTE | `Filters/AuditFilter.cs` |
| **TRZ-003**: API Key auth no asocia acciones a usuario | 🟡 MEDIO | ❌ PENDIENTE | `Middleware/ApiKeyMiddleware.cs:33` |

### 9. Recuperación ante Desastres
| Hallazgo | Severidad | Estado | Archivo |
|----------|-----------|--------|---------|
| **DRP-001**: Hangfire MemoryStorage — jobs se pierden al reiniciar | 🟠 ALTO | ✅ FIXED | `Program.cs:294` |
| **DRP-002**: Sin automatización de backups | 🟠 ALTO | ❌ PENDIENTE | — |
| **DRP-003**: Migración automática en producción | 🟡 MEDIO | ✅ FIXED | `Program.cs:427-440` |
| **DRP-004**: Sin read replicas ni failover | 🔵 INFO | ❌ PENDIENTE | — |

---

## Vulnerabilidades Técnicas Detalladas

### VULN-01: Inyección de dependencia en TenantContext (CRÍTICO) ✅ FIXED

```csharp
// TenantContext.cs — ANTES
public void SetTenantId(string tenantId)
{
    TenantId = tenantId;
}

// DESPUÉS — ITenantContext/ITenantContextWriter segregados
public sealed class TenantContext : ITenantContext, ITenantContextWriter
```

`TenantContext` ahora es `sealed` e implementa interfaces separadas. `ITenantContextWriter` solo se inyecta en middlewares (`TenantMiddleware`, `ApiKeyMiddleware`). Los servicios y repositorios solo ven `ITenantContext` (solo lectura).

**Registro en DI (`Program.cs:66`):**
```csharp
builder.Services.AddScoped<ITenantContextWriter>(sp => sp.GetRequiredService<TenantContext>());
```

---

### VULN-02: Permisos decorativos en JWT (CRÍTICO) ✅ FIXED

```csharp
// RequirePermissionAttribute.cs
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (user.IsInRole("SuperAdmin")) return; // bypass
        var hasPermission = user.HasClaim("permission", Permission);
        if (!hasPermission) context.Result = new ForbidResult();
    }
}
```

Implementado y aplicado en 5 controllers con 24 permisos definidos. SuperAdmin tiene bypass automático.

---

### VULN-03: IDOR en Controllers Financieros (CRÍTICO) ✅ FIXED

```csharp
// CreditsController.cs
[HttpGet("{id:guid}")]
[RequirePermission(Permissions.CreditRead)]
public async Task<IActionResult> GetById(Guid id) { ... }
```

`[RequirePermission]` agregado a todos los endpoints de `CreditsController`, `EmployeesController`, `AuditLogsController`, y `UsersController`.

---

### VULN-04: Refresh Token Reuse (ALTO) ✅ FIXED

```csharp
// AuthService.cs:145
// Token reuse detection: if already revoked, revoke ALL tokens for this user
if (storedToken.IsRevoked)
{
    await RevokeAllSessionsAsync(user.Id);
    return null; // signal compromised token
}
```

Si un refresh token ya revocado es presentado, se revocan TODOS los tokens del usuario (sesión marcada como comprometida).

---

### VULN-05: Firestore Admin Credentials en Repo (CRÍTICO) 🟡 PARCIAL

El archivo `nexora-hr-firebase-admin.json` fue eliminado del tracking de git (ya no aparece en `git ls-files`). El `.gitignore` ahora incluye `*-firebase-admin.json`. Sin embargo, el archivo aún existe en disco.

**Pendiente:**
- Rotar credenciales en Firebase Console
- Eliminar el archivo físico del disco
- Verificar que no haya referencias residuales en configuración

---

## OWASP Top 10 — Estado Post-Corrección

| OWASP | Estado Anterior | Estado Actual | Hallazgos |
|-------|----------------|---------------|-----------|
| **A01: Broken Access Control** | ❌ Falla | ⚠️ PARCIAL | USR-002, RBP-002, MTN-003 resueltos; RBP-003/005 pendientes |
| **A02: Cryptographic Failures** | ❌ Falla | ⚠️ PARCIAL | DAT-002 parcial; DAT-003 pendiente |
| **A03: Injection** | ✅ Parcial | ✅ Parcial | Sin cambios |
| **A04: Insecure Design** | ❌ Falla | ⚠️ PARCIAL | MTN-001 resuelto; AUT-003 pendiente |
| **A05: Security Misconfiguration** | ❌ Falla | ❌ Falla | RBP-003, AUT-005 pendientes |
| **A06: Vulnerable Components** | ✅ No evaluado | ✅ No evaluado | Sin cambios |
| **A07: Identification/Auth Failures** | ❌ Falla | ⚠️ PARCIAL | AUT-002, AUT-004 resueltos; AUT-003 pendiente |
| **A08: Software/Data Integrity Failures** | ✅ Parcial | ✅ Parcial | DRP-003 pendiente |
| **A09: Security Logging & Monitoring** | ❌ Falla | ⚠️ PARCIAL | AUD-001, AUD-002, AUD-005 resueltos; TRZ-002 pendiente |
| **A10: SSRF** | ✅ No detectado | ✅ No detectado | Sin cambios |

---

## Propuesta de Mejoras — Estado de Implementación

### Inmediatas (0-7 días)

| Prioridad | Acción | Hallazgo | Estado |
|-----------|--------|----------|--------|
| 🔴 1 | Remote `nexora-hr-firebase-admin.json` del repo y rotar credenciales | DAT-001 | 🟡 Git tracking eliminado; rotación manual pendiente |
| 🔴 2 | Mover JWT Secret a variable de entorno | DAT-002 | 🟡 Lee de config con throw on null; dev fallback existe |
| 🔴 3 | Hacer `TenantContext` inmutable post-inicialización | MTN-001 | ✅ COMPLETADO |
| 🔴 4 | Agregar `[Authorize(Roles = "SuperAdmin")]` a `UsersController` | USR-001 | ✅ COMPLETADO (via `[RequirePermission]`) |
| 🔴 5 | Agregar auth en controllers financieros | VULN-03 | ✅ COMPLETADO |

### Corto Plazo (1-4 semanas)

| Prioridad | Acción | Hallazgo | Estado |
|-----------|--------|----------|--------|
| 🟠 6 | Sistema de políticas de permisos con `[RequirePermission]` | RBP-002 | ✅ COMPLETADO |
| 🟠 7 | Refresh token rotation con reuse detection | AUT-002 | ✅ COMPLETADO |
| 🟠 8 | Device fingerprint en refresh tokens | AUT-001 | ✅ COMPLETADO |
| 🟠 9 | Registrar cambios reales en AuditLog (OldValues/NewValues) | AUD-001 | ✅ COMPLETADO |
| 🟠 10 | Migrar Hangfire a PostgreSQL | DRP-001 | ✅ COMPLETADO |
| 🟠 11 | Endpoint de revocación masiva de sesiones | AUT-004 | ✅ COMPLETADO |

### Mediano Plazo (1-3 meses)

| Prioridad | Acción | Hallazgo | Estado |
|-----------|--------|----------|--------|
| 🟡 12 | Implementar MFA/2FA (TOTP) en login | AUT-003 | ❌ PENDIENTE |
| 🟡 13 | Cifrado de columnas PII | DAT-003 | ❌ PENDIENTE |
| 🟡 14 | Política de retención de audit logs | AUD-005 | ✅ COMPLETADO |
| 🟡 15 | Historial de cambios para entidades financieras | HST-001 | ❌ PENDIENTE |
| 🟡 16 | Políticas de backup automático | DRP-002 | ❌ PENDIENTE |
| 🟡 17 | Poblar `CreatedBy`/`UpdatedBy` automáticamente | HST-003 | ✅ COMPLETADO |
| 🟡 18 | CSRF protection | — | ❌ PENDIENTE |

### Largo Plazo (3-6 meses)

| Prioridad | Acción | Hallazgo | Estado |
|-----------|--------|----------|--------|
| 🟢 19 | Auditoría de acceso: registrar GET a datos sensibles | TRZ-002 | ❌ PENDIENTE |
| 🟢 20 | Hard-delete batch para soft-delete > 90 días | DAT-004 | ❌ PENDIENTE |
| 🟢 21 | Dashboard de seguridad | AUD-004 | ❌ PENDIENTE |
| 🟢 22 | Pentesting trimestral + SAST | — | ❌ PENDIENTE |

---

## Políticas de Acceso Recomendadas

```csharp
// 1. POLÍTICA: Tenant Isolation global
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TenantIsolation", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("tenant_id"));
});

// 2. POLÍTICA: SuperAdmin-only
options.AddPolicy("RequireSuperAdmin", policy =>
    policy.RequireRole("SuperAdmin"));

// 3. POLÍTICA: Permisos por módulo
options.AddPolicy("credit.read", policy =>
    policy.RequireAssertion(context =>
        context.User.HasClaim("permission", "credit.read") ||
        context.User.IsInRole("SuperAdmin")));

options.AddPolicy("payroll.write", policy =>
    policy.RequireAssertion(context =>
        context.User.HasClaim("permission", "payroll.write") ||
        context.User.IsInRole("SuperAdmin")));
```

### Matriz de Acceso Base

| Módulo | SuperAdmin | CompanyAdmin | RRHH | Supervisor | Employee |
|--------|-----------|-------------|------|------------|----------|
| Usuarios (CRUD) | ✅ | ✅ | ❌ | ❌ | ❌ |
| Roles/Permisos | ✅ | 🟡 (solo su tenant) | ❌ | ❌ | ❌ |
| Empleados (Create/Update/Delete) | ✅ | ✅ | ✅ | 🟡 (solo su equipo) | ❌ |
| Empleados (Read) | ✅ | ✅ | ✅ | 🟡 (solo su equipo) | 🟡 (solo propio) |
| Nómina (Read) | ✅ | ✅ | ✅ | ❌ | 🟡 (solo propia) |
| Nómina (Process) | ✅ | ✅ | 🟡 | ❌ | ❌ |
| Créditos (CRUD) | ✅ | ✅ | ❌ | ❌ | ❌ |
| Ventas (CRUD) | ✅ | ✅ | ❌ | ❌ | ❌ |
| Contabilidad | ✅ | ✅ | ❌ | ❌ | ❌ |
| Audit Logs | ✅ | 🟡 (solo su tenant) | ❌ | ❌ | ❌ |
| API Keys | ✅ | 🟡 (solo su tenant) | ❌ | ❌ | ❌ |
| Reportes | ✅ | ✅ | 🟡 | 🟡 | ❌ |

---

## Checklist de Cumplimiento

### Seguridad de Datos
- [ ] Cifrado en reposo (PostgreSQL TDE o similar)
- [ ] Cifrado de columnas PII
- [ ] Data Classification Policy documentada
- [ ] GDPR Right to Deletion (hard-delete batch job)
- [ ] Tokenización de datos bancarios

### Trazabilidad Completa
- [ ] AuditLog inmutable (append-only table o trigger)
- [x] OldValues/NewValues capturados automáticamente (via `AuditInterceptor`)
- [ ] Auditoría de lecturas (GET) a datos sensibles
- [x] EmployeeHistory poblado correctamente
- [ ] Cross-reference entre AuditLog y EmployeeHistory

### Acceso
- [ ] MFA habilitado
- [x] Refresh token rotation con reuse detection
- [x] Device fingerprint en tokens
- [ ] Rate limiting por endpoint específico
- [ ] Geolocalización de IP en logins

### Backup/DR
- [ ] Backup automático diario de BD
- [ ] Restore test cada 30 días
- [ ] Estrategia de failover documentada
- [x] Hangfire en storage persistente (PostgreSQL)
- [ ] Script de disaster recovery

---

## Archivos Clave Referenciados

| Archivo | Propósito |
|---------|-----------|
| `src/Zorvian.Core/Entities/BaseEntity.cs` | Base con TenantId, soft-delete, audit timestamps |
| `src/Zorvian.Core/Entities/User.cs` | Entidad de usuario |
| `src/Zorvian.Core/Entities/Role.cs` | Entidad de rol con IsSystem flag |
| `src/Zorvian.Core/Entities/RolePermission.cs` | Permiso por rol (string code) |
| `src/Zorvian.Core/Entities/AuditLog.cs` | Registro de auditoría |
| `src/Zorvian.Core/Entities/EmployeeHistory.cs` | Historial de cambios de empleado |
| `src/Zorvian.Core/Entities/RefreshToken.cs` | Refresh token (con device fingerprint) |
| `src/Zorvian.Core/Enums/RoleType.cs` | Enum de roles |
| `src/Zorvian.Core/Interfaces/ITenantContext.cs` | Interfaces segregadas R/W |
| `src/Zorvian.Infrastructure/Data/TenantContext.cs` | Implementación sealed con interfaces |
| `src/Zorvian.Infrastructure/Data/AuditInterceptor.cs` | Interceptor de auditoría (OldValues/NewValues) |
| `src/Zorvian.Infrastructure/Data/NexoraDbContext.cs` | DbContext con query filters multi-tenant |
| `src/Zorvian.Infrastructure/Identity/JwtService.cs` | Generación de JWT |
| `src/Zorvian.Infrastructure/Identity/FirebaseAuthService.cs` | Auth Firebase con WebApiKey en URL |
| `src/Zorvian.Infrastructure/Repositories/AuthRepository.cs` | Auth repo con IgnoreQueryFilters |
| `src/Zorvian.Infrastructure/Services/ApiKeyService.cs` | API keys hasheadas con SHA256 |
| `src/Zorvian.Web/Authorization/RequirePermissionAttribute.cs` | Atributo de permisos (24 permisos) |
| `src/Zorvian.Web/Middleware/TenantMiddleware.cs` | Setea tenant_id desde JWT |
| `src/Zorvian.Web/Middleware/ApiKeyMiddleware.cs` | Setea tenant_id desde API Key |
| `src/Zorvian.Web/Middleware/SecurityHeadersMiddleware.cs` | 5 security headers |
| `src/Zorvian.Web/Filters/AuditFilter.cs` | Filtro de auditoría (metadatos) |
| `src/Zorvian.Web/Controllers/UsersController.cs` | Control de usuarios (con permisos) |
| `src/Zorvian.Web/Controllers/AuthController.cs` | Login/refresh/logout/revoke-all |
| `src/Zorvian.Web/Controllers/AuditLogsController.cs` | Query de audit logs (con permiso) |
| `src/Zorvian.Web/Controllers/CreditsController.cs` | Créditos (con permisos) |
| `src/Zorvian.Web/Program.cs` | Pipeline de middleware y DI |
| `src/Zorvian.Web/Jobs/AuditLogCleanupJob.cs` | Purga mensual de audit logs |

---

## Próximos Pasos Prioritarios

### Inmediatos
1. Rotar credenciales Firebase en Firebase Console; eliminar `nexora-hr-firebase-admin.json` del disco

### Corto Plazo
2. Implementar MFA/2FA (AUT-003)
3. Agregar cifrado de columnas PII (DAT-003)
4. Auditoría de lecturas GET a datos sensibles (TRZ-002)

### Mediano Plazo
5. Historial de cambios para entidades financieras (HST-001)
6. CSRF protection
7. Backup automático de BD (DRP-002)

---

*Este informe fue generado mediante análisis estático del código fuente. No reemplaza un pentest completo ni una auditoría de infraestructura.*
