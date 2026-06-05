# Auditoría de Seguridad — Zorvian ERP

**Fecha:** 2026-06-05  
**Alcance:** Análisis completo del código fuente (Clean Architecture .NET 9 + Flutter)  
**Clasificación:** Uso interno / Confidencial

---

## Resumen Ejecutivo

| Gravedad | Cantidad |
|----------|----------|
| **CRÍTICO** | 5 |
| **ALTO** | 7 |
| **MEDIO** | 9 |
| **BAJO** | 5 |
| **INFO** | 4 |

**Total hallazgos: 30**

---

## Vista por Dimensión

### 1. Usuarios
| Hallazgo | Severidad | Archivo |
|----------|-----------|---------|
| **USR-001**: `UsersController` sin roles de autorización — cualquier usuario autenticado puede listar, asignar roles y desactivar usuarios | 🔴 CRÍTICO | `Controllers/UsersController.cs:34,92,123` |
| **USR-002**: `AssignRole` borra todos los roles existentes y asigna uno solo — permite que un Employee se autonombrade SuperAdmin si hay un bug en la validación | 🔴 CRÍTICO | `Controllers/UsersController.cs:112` |
| **USR-003**: `ToggleActive` no verifica si el blanco es el último SuperAdmin/CompanyAdmin activo — riesgo de bloqueo administrativo | 🟠 ALTO | `Controllers/UsersController.cs:126-137` |
| **USR-004**: Usuarios creados vía `LoginAsync` no heredan `TenantId` del contexto — el tenant se asigna del FirebaseUid lookup, no del token | 🟠 ALTO | `Services/AuthService.cs:30-37` |
| **USR-005**: `AuthRepository` usa `IgnoreQueryFilters()` en TODAS las consultas lookup — búsqueda cross-tenant de usuarios por email sin restricción | 🟡 MEDIO | `Repositories/AuthRepository.cs:19-25` |

### 2. Roles y Permisos
| Hallazgo | Severidad | Archivo |
|----------|-----------|---------|
| **RBP-001**: Sistema de permisos basado en strings (`PermissionCode`) sin registro central — propenso a typos e inconsistencyes | 🟡 MEDIO | `Entities/RolePermission.cs:7` |
| **RBP-002**: Claims de permiso embebidos en JWT pero NUNCA validados server-side — ningún middleware ni PolicyAttribute los verifica | 🔴 CRÍTICO | `Identity/JwtService.cs:48-51` |
| **RBP-003**: Role `SuperAdmin` con bypass hardcodeado (`"superadmin"` string) en query filter de `Company` | 🟠 ALTO | `Data/NexoraDbContext.cs:220` |
| **RBP-004**: Role `SuperAdmin` existe en el enum pero no hay evidencia de endpoints protegidos con `[Authorize(Roles = "SuperAdmin")]` — la mayoría usa solo `[Authorize]` | 🟠 ALTO | `Controllers/*.cs` |
| **RBP-005**: No hay granularidad de permisos por módulo — un rol RRHH puede ver nómina, créditos, contabilidad si el controller no lo restringe | 🟠 ALTO | `Controllers/*.cs` |

### 3. Bitácoras (AuditLog)
| Hallazgo | Severidad | Archivo |
|----------|-----------|---------|
| **AUD-001**: `AuditFilter` no captura `OldValues`/`NewValues` — solo registra metadatos, no el cambio real de datos | 🟠 ALTO | `Filters/AuditFilter.cs:35-45` |
| **AUD-002**: `AuditLogRepository.AddAsync()` llama `SaveChangesAsync()` inline — rompe patrón Unit of Work y puede generar logs parciales si la transacción principal falla | 🟡 MEDIO | `Repositories/AuditLogRepository.cs:19-20` |
| **AUD-003**: `AuditLogsController` no tiene restricción de rol — cualquier empleado autenticado puede leer todo el log de auditoría del tenant | 🟡 MEDIO | `Controllers/AuditLogsController.cs:26` |
| **AUD-004**: Audit logs no son inmutables — no hay trigger de BD ni append-only para prevenir alteración retroactiva | 🟠 ALTO | `Entities/AuditLog.cs` |
| **AUD-005**: No hay política de retención — los audit logs crecen indefinidamente sin purge ni archive | 🔵 INFO | — |

### 4. Historial de Cambios
| Hallazgo | Severidad | Archivo |
|----------|-----------|---------|
| **HST-001**: `EmployeeHistory` solo existe para empleados — entidades financieras (Sales, Credits, Payroll, Accounting) sin historial de cambios | 🟡 MEDIO | `Entities/EmployeeHistory.cs` |
| **HST-002**: No se encontró lógica que SIEMBRE `EmployeeHistory` en el servicio o repositorio — el historial podría no poblarse nunca | 🔴 CRÍTICO | `Services/EmployeeService.cs` |
| **HST-003**: `BaseEntity.CreatedBy`/`UpdatedBy` son strings no validados ni poblados consistentemente — no hay trazabilidad real de quién creó/modificó | 🟡 MEDIO | `Entities/BaseEntity.cs:8-10` |

### 5. Accesos (Auth)
| Hallazgo | Severidad | Archivo |
|----------|-----------|---------|
| **AUT-001**: Refresh tokens sin device fingerprint — robo de token = acceso permanente hasta expiración (7 días) | 🔴 CRÍTICO | `Core/Entities/RefreshToken.cs` |
| **AUT-002**: No hay detección de rotación de refresh token — un token rotado puede ser reusado para obtener uno nuevo (token reuse detection ausente) | 🟠 ALTO | `Services/AuthService.cs:137-178` |
| **AUT-003**: No hay MFA/2FA en ningún flujo de autenticación | 🟡 MEDIO | `Services/AuthService.cs` |
| **AUT-004**: No hay gestión de sesiones — no existe endpoint para revocar TODAS las sesiones de un usuario | 🟡 MEDIO | `Controllers/AuthController.cs` |
| **AUT-005**: `SignInWithPasswordAsync` envía `WebApiKey` como query param en URL — queda en logs del servidor y proxies | 🟢 BAJO | `Identity/FirebaseAuthService.cs:69` |

### 6. Multiempresa
| Hallazgo | Severidad | Archivo |
|----------|-----------|---------|
| **MTN-001**: `TenantContext.SetTenantId()` es público y mutable — cualquier servicio en el request puede cambiar el tenant activo | 🔴 CRÍTICO | `Data/TenantContext.cs:11-14` |
| **MTN-002**: `Company` query filter usa string hardcodeado `"superadmin"` — frágil y no configurable | 🟠 ALTO | `Data/NexoraDbContext.cs:220` |
| **MTN-003**: Controllers de finanzas (`CreditsController`, `SalesController`, etc.) usan `[Authorize]` sin verificar que el recurso pertenezca al tenant del caller — IDOR crítico | 🔴 CRÍTICO | `Controllers/CreditsController.cs:24-27` |
| **MTN-004**: `TenantId` es string sin type-safety — propenso a valores vacíos o inválidos que pasan el filter | 🟡 MEDIO | `Data/NexoraDbContext.cs` |

### 7. Seguridad de Datos
| Hallazgo | Severidad | Archivo |
|----------|-----------|---------|
| **DAT-001**: Firebase admin credentials (`nexora-hr-firebase-admin.json`) en el repositorio | 🔴 CRÍTICO | `nexora-hr-firebase-admin.json` |
| **DAT-002**: JWT Secret hardcodeado en appsettings — si no hay override por env var, cualquiera puede forjar tokens | 🔴 CRÍTICO | `Identity/JwtService.cs:34` |
| **DAT-003**: PII (identificación, teléfono, bank accounts) sin cifrado a nivel de columna | 🟠 ALTO | `Entities/Employee.cs` |
| **DAT-004**: Datos "eliminados" vía soft-delete permanecen en BD sin fecha de purge | 🟡 MEDIO | `Entities/BaseEntity.cs:11` |
| **DAT-005**: Archivos de empleados en Firebase Storage sin verificación de cifrado server-side | 🔵 INFO | `Services/FirebaseStorageService.cs` |

### 8. Trazabilidad
| Hallazgo | Severidad | Archivo |
|----------|-----------|---------|
| **TRZ-001**: `AuditFilter` solo captura después de la acción — si el endpoint falla antes, no hay log | 🟡 MEDIO | `Filters/AuditFilter.cs:23-26` |
| **TRZ-002**: No hay trazabilidad de LECTURAS — solo quién modificó, no quién consultó datos sensibles | 🟠 ALTO | `Filters/AuditFilter.cs` |
| **TRZ-003**: API Key auth no asocia acciones a un usuario humano — `PerformedBy` queda null | 🟡 MEDIO | `Middleware/ApiKeyMiddleware.cs:33` |

### 9. Recuperación ante Desastres
| Hallazgo | Severidad | Archivo |
|----------|-----------|---------|
| **DRP-001**: Hangfire usa `MemoryStorage` — todos los jobs programados se pierden al reiniciar el servidor | 🟠 ALTO | `Program.cs:284` |
| **DRP-002**: Sin automatización de backups de BD en el código | 🟠 ALTO | — |
| **DRP-003**: Migración automática en producción (`Database.MigrateAsync()`) — riesgo de downtime si hay migración conflictiva | 🟡 MEDIO | `Program.cs:408-411` |
| **DRP-004**: Sin read replicas ni estrategia de failover evidente | 🔵 INFO | — |

---

## Vulnerabilidades Técnicas Detalladas

### VULN-01: Inyección de dependencia en TenantContext (CRÍTICO)

```csharp
// TenantContext.cs:11-14
public void SetTenantId(string tenantId)
{
    TenantId = tenantId;
}
```

`TenantContext` es scoped y su `SetTenantId` es público. Cualquier servicio en la cadena de dependencias puede mutar el tenant activo. Si un servicio malicioso o comprometido llama `SetTenantId("otro-tenant")`, todas las queries posteriores en el mismo request filtrarán datos cross-tenant.

**Fix:** Hacer `TenantContext` inmutable después de inicialización. Usar una interfaz de solo lectura (`ITenantContext`) y una de solo escritura (`ITenantContextWriter`) registrada como scoped por separado, o usar `IHttpContextAccessor` como source of truth.

---

### VULN-02: Permisos decorativos en JWT (CRÍTICO)

```csharp
// JwtService.cs:48-51
foreach (var permission in role.RolePermissions)
{
    claims.Add(new Claim("permission", permission.PermissionCode));
}
```

Los claims de permiso se inyectan en el JWT pero nunca se verifican en los controllers. No hay `[Authorize(Policy = "...")]` ni middleware que evalúe los `permission` claims. El sistema entero de permisos es decorativo — solo `[Authorize]` (autenticación) y `[Authorize(Roles = "...")]` tienen efecto real.

**Fix:**
1. Registrar políticas de autorización basadas en claims en `Program.cs`
2. Crear atributo `[RequirePermission("credit.read")]` que valide contra los claims
3. Refactorizar controllers para usar `[Authorize(Policy = "credit.read")]`

---

### VULN-03: IDOR en Controllers Financieros (CRÍTICO)

```csharp
// CreditsController.cs:21-28
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id)
{
    var credit = await _service.GetByIdAsync(id);
    if (credit is null) return NotFound();
    return Ok(credit);
}
```

No hay verificación de que el `Credit` solicitado pertenezca al tenant del usuario autenticado. La única protección son los `HasQueryFilter` de EF Core, que se aplican automáticamente — PERO solo funcionan si `TenantContext.TenantId` está correctamente seteado (VULN-01).

Este patrón se repite en **todos los controllers**: `SalesController`, `CreditsController`, `PayrollController`, `AccountingController`, `EmployeesController.GetById`, etc.

**Fix:**
- Crear un repository base que inyecte `ITenantContext` y siempre filtre por tenant
- Eliminar métodos que reciben `Guid id` sin tenant context
- Agregar auditoría de acceso a datos sensibles

---

### VULN-04: Refresh Token Reuse (ALTO)

```csharp
// AuthService.cs:139-148
var storedToken = await _authRepo.GetRefreshTokenAsync(request.RefreshToken);
if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
    return null;

storedToken.IsRevoked = true;
storedToken.RevokedAt = DateTime.UtcNow;
// ... generates new token ...
storedToken.ReplacedByToken = newRefreshToken;
```

Un atacante que robe un refresh token puede usarlo una vez. Si el usuario legítimo también usa su token, causará que el atacante reciba un 401 (token revocado). Pero no hay detección proactiva — si el atacante usa el token primero y el legítimo después, el legítimo queda locked out sin notificación.

**Fix:**
- Implementar "refresh token rotation con reuse detection": si un token ya revocado es presentado, revocar TODOS los refresh tokens del usuario (sesión comprometida)
- Notificar al usuario por email/FCM cuando un token es rotado

---

### VULN-05: Firestore Admin Credentials en Repo (CRÍTICO)

`nexora-hr-firebase-admin.json` contiene credenciales de Firebase Admin con los scopes:
- `https://www.googleapis.com/auth/cloud-platform`
- `https://www.googleapis.com/auth/firebase.database`
- `https://www.googleapis.com/auth/firebase.messaging`
- `https://www.googleapis.com/auth/identitytoolkit`
- `https://www.googleapis.com/auth/userinfo.email`

Un atacante con este JSON puede crear/eliminar usuarios en Firebase Authentication, acceder a Firebase Storage, y enviar notificaciones FCM.

---

## Análisis de OWASP Top 10

| OWASP | Estado | Hallazgos |
|-------|--------|-----------|
| **A01: Broken Access Control** | ❌ Falla | USR-002, RBP-002, MTN-003, VULN-03 |
| **A02: Cryptographic Failures** | ❌ Falla | DAT-001, DAT-002 |
| **A03: Injection** | ✅ Parcial | SEC-010 (prompt injection) — bajo riesgo |
| **A04: Insecure Design** | ❌ Falla | MTN-001, AUT-001, DRP-001 |
| **A05: Security Misconfiguration** | ❌ Falla | AUT-005, SEC-007, SEC-008, RBP-003 |
| **A06: Vulnerable Components** | ✅ No evaluado | Sin análisis de dependencias |
| **A07: Identification/Auth Failures** | ❌ Falla | AUT-002, AUT-003, AUT-004 |
| **A08: Software/Data Integrity Failures** | ✅ Parcial | Migración automática en prod (DRP-003) |
| **A09: Security Logging & Monitoring** | ❌ Falla | AUD-001, AUD-004, TRZ-002 |
| **A10: SSRF** | ✅ No detectado | — |

---

## Propuesta de Mejoras

### Inmediatas (0-7 días)

| Prioridad | Acción | Hallazgo |
|-----------|--------|----------|
| 🔴 1 | Remote `nexora-hr-firebase-admin.json` del repo y rotar credenciales en Firebase Console | DAT-001 |
| 🔴 2 | Mover JWT Secret a variable de entorno; rotarlo si no estaba overrideado | DAT-002 |
| 🔴 3 | Hacer `TenantContext` inmutable post-inicialización | MTN-001 |
| 🔴 4 | Agregar `[Authorize(Roles = "SuperAdmin")]` a `UsersController` completo | USR-001 |
| 🔴 5 | Agregar `[Authorize(Roles = "CompanyAdmin")]` mínimo en controllers financieros | VULN-03 |

### Corto Plazo (1-4 semanas)

| Prioridad | Acción | Hallazgo |
|-----------|--------|----------|
| 🟠 6 | Implementar sistema de políticas de permisos con `[RequirePermission]` | RBP-002 |
| 🟠 7 | Implementar refresh token rotation con reuse detection | AUT-002 |
| 🟠 8 | Agregar device fingerprint en refresh tokens (hash de User-Agent + IP) | AUT-001 |
| 🟠 9 | Registrar cambios reales en AuditLog (OldValues/NewValues via ChangeTracker) | AUD-001 |
| 🟠 10 | Migrar Hangfire de MemoryStorage a PostgreSQL | DRP-001 |
| 🟠 11 | Agregar campo `LastActiveAt` en RefreshToken y endpoint de revocación masiva | AUT-004 |

### Mediano Plazo (1-3 meses)

| Prioridad | Acción | Hallazgo |
|-----------|--------|----------|
| 🟡 12 | Implementar MFA/2FA (TOTP) en login | AUT-003 |
| 🟡 13 | Agregar cifrado de columnas PII (Phone, IdentificationNumber, BankAccount) | DAT-003 |
| 🟡 14 | Implementar política de retención de audit logs (archive + purge) | AUD-005 |
| 🟡 15 | Agregar historial de cambios para entidades financieras (Credit, Sale, Payroll) | HST-001 |
| 🟡 16 | Implementar políticas de backup automático + exportación | DRP-002 |
| 🟡 17 | Poblar `CreatedBy`/`UpdatedBy` automáticamente en SaveChanges | HST-003 |
| 🟡 18 | Agregar CSRF protection (doblar Submit Cookie o header validation) | — |

### Largo Plazo (3-6 meses)

| Prioridad | Acción | Hallazgo |
|-----------|--------|----------|
| 🟢 19 | Auditoría de acceso: registrar cada GET a datos sensibles (PII, finanzas) | TRZ-002 |
| 🟢 20 | Implementar hard-delete batch para registros soft-delete > 90 días (GDPR) | DAT-004 |
| 🟢 21 | Dashboard de seguridad: logins fallidos, actividad anómala, IPs sospechosas | AUD-004 |
| 🟢 22 | Pentesting trimestral + SAST (SonarQube) en CI/CD | — |

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
- [ ] OldValues/NewValues capturados automáticamente
- [ ] Auditoría de lecturas (GET) a datos sensibles
- [ ] EmployeeHistory poblado correctamente
- [ ] Cross-reference entre AuditLog y EmployeeHistory

### Acceso
- [ ] MFA habilitado
- [ ] Refresh token rotation con reuse detection
- [ ] Device fingerprint en tokens
- [ ] Rate limiting por endpoint específico
- [ ] Geolocalización de IP en logins

### Backup/DR
- [ ] Backup automático diario de BD
- [ ] Restore test cada 30 días
- [ ] Estrategia de failover documentada
- [ ] Hangfire en storage persistente
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
| `src/Zorvian.Core/Entities/RefreshToken.cs` | Refresh token (sin device info) |
| `src/Zorvian.Core/Enums/RoleType.cs` | Enum de roles |
| `src/Zorvian.Core/Interfaces/ITenantContext.cs` | Interfaz de contexto multi-tenant |
| `src/Zorvian.Infrastructure/Data/TenantContext.cs` | Implementación mutable de TenantContext |
| `src/Zorvian.Infrastructure/Data/NexoraDbContext.cs` | DbContext con query filters multi-tenant |
| `src/Zorvian.Infrastructure/Identity/JwtService.cs` | Generación de JWT (claims de permisos decorativos) |
| `src/Zorvian.Infrastructure/Identity/FirebaseAuthService.cs` | Auth Firebase con WebApiKey en URL |
| `src/Zorvian.Infrastructure/Repositories/AuthRepository.cs` | Auth repo con IgnoreQueryFilters |
| `src/Zorvian.Infrastructure/Repositories/AuditLogRepository.cs` | Audit repo con SaveChanges inline |
| `src/Zorvian.Infrastructure/Services/ApiKeyService.cs` | API keys hasheadas con SHA256 |
| `src/Zorvian.Web/Middleware/TenantMiddleware.cs` | Setea tenant_id desde JWT |
| `src/Zorvian.Web/Middleware/ApiKeyMiddleware.cs` | Setea tenant_id desde API Key |
| `src/Zorvian.Web/Middleware/RateLimitingMiddleware.cs` | Rate limiter (in-memory sliding window) |
| `src/Zorvian.Web/Filters/AuditFilter.cs` | Filtro de auditoría (solo metadatos) |
| `src/Zorvian.Web/Controllers/UsersController.cs` | Control de usuarios (sin roles restrictivos) |
| `src/Zorvian.Web/Controllers/AuthController.cs` | Login/refresh/logout |
| `src/Zorvian.Web/Controllers/AuditLogsController.cs` | Query de audit logs |
| `src/Zorvian.Web/Controllers/CreditsController.cs` | Créditos (IDOR) |
| `src/Zorvian.Web/Program.cs` | Pipeline de middleware y DI |

---

*Este informe fue generado automáticamente mediante análisis estático del código fuente. No reemplaza un pentest completo ni una auditoría de infraestructura.*
