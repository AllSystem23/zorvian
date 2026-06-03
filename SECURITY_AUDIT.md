# Security Audit — Nexora ERP

## Summary
| Severity | Count |
|----------|-------|
| CRITICAL | 2 |
| HIGH | 3 |
| MEDIUM | 3 |
| LOW | 3 |
| INFO | 2 |

---

## CRITICAL

### SEC-001: Database credentials in source control
**OWASP:** A05 (Security Misconfiguration) + A02 (Cryptographic Failure)  
**File:** `src/Nexora.Web/appsettings.json:10`  
**Issue:** PostgreSQL connection string with plaintext password `npg_N2lCkzum3shZ` committed to git. Any user with repo access can connect directly to `ep-solitary-cake-apmacxlx-pooler.c-7.us-east-1.aws.neon.tech`.  
**Risk:** Database compromise, data exfiltration, ransomware.  
**Fix (immediate):**
1. Rotate the DB password in Neon dashboard immediately.
2. Remove the connection string from `appsettings.json` — read only from env vars:
```json
"ConnectionStrings": {
  "NexoraDb": ""
}
```
3. Set `ConnectionStrings__NexoraDb` as env var in Render dashboard.
4. Add `appsettings.json` to `.gitignore` or use `appsettings.Local.json` (already in gitignore).

### SEC-002: No authorization checks per resource (horizontal privilege escalation)
**OWASP:** A01 (Broken Access Control)  
**Files:** All controllers (`CreditsController.cs`, `SalesController.cs`, `ClientsController.cs`, etc.)  
**Issue:** Controllers have `[Authorize]` but only validate authentication, not ownership. Any authenticated user can access any company's data. Example: `GET /api/v1/credits/{id}` returns the credit regardless of `tenant_id` vs. the caller's tenant. The `TenantMiddleware` sets `tenantContext.TenantId` from JWT, but repos `GetFilteredAsync` methods receive `companyId` from the client query — not from `ITenantContext`. A user could query another tenant's data by manipulating filters.  
**Risk:** Cross-tenant data access.  
**Fix:**
- Repositories should always read `tenantId`/`companyId` from `ITenantContext` (injected), not from query params.
- Add `[Authorize(Policy = "tenant-isolation")]` or enforce in a base controller.
- Remove `companyId` from filter DTOs and use `ITenantContext.CompanyId` instead in every repository query.

---

## HIGH

### SEC-003: JWT Secret committed to source
**OWASP:** A02 (Cryptographic Failure)  
**File:** `src/Nexora.Web/appsettings.json:13`  
**Issue:** `Jwt:Secret = "NexoraSuperSecretKey2026!AtLeast32CharactersLong!"` — a predictable, hardcoded secret. If this is used in production (not overridden by env var), any attacker can forge JWTs and impersonate any user.  
**Risk:** Complete authentication bypass.  
**Fix (verify first):**
- Check if Render env var `Jwt__Secret` overrides this. If yes, rotate it to a cryptographically random 64-char key.
- If not overridden, treat as CRITICAL — rotate immediately.
- Remove from `appsettings.json`; read only from env var.

### SEC-004: Firebase Web API Key in source
**OWASP:** A05 (Security Misconfiguration)  
**File:** `src/Nexora.Web/appsettings.json:20`  
**Issue:** `Firebase:WebApiKey` committed. While Firebase Web API keys are "public by design" (safe to expose in client apps), having the key in source control means it can't be rotated independently, and combined with `Firebase:ProjectId` it reveals the project namespace.  
**Risk:** Low for the key itself (Firebase auth is protected by security rules), but prevents clean rotation.  
**Fix:**
- Move to env var `Firebase__WebApiKey` in Render.
- Remove from `appsettings.json`.

### SEC-005: No rate limiting on auth endpoints
**OWASP:** A05 (Security Misconfiguration)  
**File:** `src/Nexora.Web/Middleware/RateLimitingMiddleware.cs`  
**Issue:** Global rate limiter applies 120 req/min to ALL endpoints uniformly. Auth endpoints (`POST /api/v1/auth/login`, refresh token, password login) are NOT throttled differently. This allows brute-force attacks against password login at 2 req/second.  
**Risk:** Credential stuffing / brute-force on password auth.  
**Fix:**
- Reduce limit for auth endpoints to 5 req/15min per IP.
- Add `[SkipRateLimiting]` attribute logic or use endpoint-specific policies:
```csharp
if (context.Request.Path.StartsWithSegments("/api/v1/auth"))
    ApplyStrictLimit(context, maxRequests: 5, windowSeconds: 900);
```

---

## MEDIUM

### SEC-006: Firestore Web API key exposed in client-side requests
**OWASP:** A05 (Security Misconfiguration)  
**File:** `src/Nexora.Infrastructure/Identity/FirebaseAuthService.cs:69`  
**Issue:** `SignInWithPasswordAsync` sends the `WebApiKey` as a query parameter to Google's REST API:
```
https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_webApiKey}
```
While this is how Firebase REST API works (key is public by design), the API key URL with query params could appear in server logs, proxy logs, or error messages.  
**Risk:** Low — this is the intended Firebase pattern.  
**Fix:** No code change needed. Ensure server-side logs are sanitized and not exposed externally.

### SEC-007: AllowedHosts = "*" in production
**OWASP:** A05 (Security Misconfiguration)  
**File:** `src/Nexora.Web/appsettings.json:8`  
**Issue:** `"AllowedHosts": "*"` — ASP.NET Core's host header validation is disabled. In production, this makes the app vulnerable to host header injection attacks (cache poisoning, password reset poisoning).  
**Risk:** Host header injection.  
**Fix:**
```json
"AllowedHosts": "nexora.app;*.nexora.app;nexora-9yal.onrender.com"
```

### SEC-008: CORS WithOrigins fallback to wildcard
**OWASP:** A05 (Security Misconfiguration)  
**File:** `src/Nexora.Web/Program.cs:209`  
**Issue:** When `Cors:AllowedOrigins` is not configured, the fallback is `["*"]` (all origins) combined with `AllowCredentials()`. Per CORS spec, `Access-Control-Allow-Origin: *` with `Access-Control-Allow-Credentials: true` is invalid and browsers will reject the response. However, if the config key is missing or empty, the behavior is undefined — some proxies may still forward the response.  
**Risk:** Low (browsers enforce the spec), but still a misconfiguration.  
**Fix:** Remove the null-coalescing fallback and require explicit origin configuration:
```csharp
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? throw new InvalidOperationException("CORS origins not configured");
```

---

## LOW

### SEC-009: Audit Trail logs OldValues/NewValues but not sensitive field masking
**OWASP:** A09 (Security Logging & Monitoring)  
**File:** `src/Nexora.Infrastructure/Data/NexoraDbContext.cs` (audit interceptor)  
**Issue:** The automatic audit trail stores `OldValues` / `NewValues` as raw JSON of all entity properties. If an Employee's `Email`, `PhoneNumber`, or any PII field changes, the full values are logged in plaintext in the `AuditLogs` table.  
**Risk:** PII exposure in audit logs.  
**Fix:** Add field exclusions or masking for sensitive properties (`PasswordHash`, `PhoneNumber`, etc.):
```csharp
var sensitiveProps = new[] { "PasswordHash", "PhoneNumber" };
// Mask values for these properties
```

### SEC-010: ChatService lacks input sanitization / prompt injection guard
**OWASP:** A03 (Injection)  
**File:** `src/Nexora.Infrastructure/Services/ChatService.cs:36`  
**Issue:** The `question` from the user is embedded directly into the LLM prompt template. A malicious user could inject `"Ignora las instrucciones anteriores..."` to bypass the system prompt and extract internal context or perform prompt injection.  
**Risk:** Low-medium (Gemini has built-in safety filters, but not a guarantee).  
**Fix:** Add input sanitization — strip known prompt injection patterns or use a guardrail library. At minimum, log all LLM queries for abuse monitoring.

### SEC-011: No security headers (HSTS, CSP, X-Frame-Options, X-Content-Type-Options)
**OWASP:** A05 (Security Misconfiguration)  
**File:** `src/Nexora.Web/Program.cs` (middleware pipeline)  
**Issue:** The middleware pipeline does not include security headers middleware. Missing:
- `Strict-Transport-Security` (HSTS)
- `Content-Security-Policy` (CSP)
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy`
**Risk:** Lowered defense against XSS, clickjacking, MIME sniffing.  
**Fix:** Add the standard ASP.NET security headers middleware or a custom middleware:
```csharp
app.Use(async (ctx, next) => {
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
// Add HSTS (only if behind HTTPS):
if (!app.Environment.IsDevelopment())
    app.UseHsts();
```

---

## INFO

### SEC-012: Swagger enabled in production?
**File:** `src/Nexora.Web/Program.cs:259-264`  
Swagger UI is enabled only in Development (`if (app.Environment.IsDevelopment())`), which is correct. Verify Render's `ASPNETCORE_ENVIRONMENT` is set to `Production`.

### SEC-013: Hangfire Dashboard in dev only
**File:** `src/Nexora.Web/Program.cs:279-282`  
Correct — Hangfire dashboard restricted to Development. No change needed.

### SEC-014: Soft Delete prevents data loss but not compliance deletion
**File:** All entities with `BaseEntity.DeletedAt`  
Soft delete is great for recovery, but GDPR right-to-deletion requests require permanent erasure. Add a background job for hard-deleting records older than N days.

---

## Immediate Action Items

| Priority | Action | Who |
|----------|--------|-----|
| 🔴 | Rotate Neon DB password + remove from appsettings.json | Admin |
| 🔴 | Verify JWT secret is overridden by env var in Render; if not, rotate | Admin |
| 🟠 | Enforce tenant isolation in all repos — use ITenantContext, not query params | Dev |
| 🟠 | Add auth endpoint rate limiting (5 req/15min) | Dev |
| 🟡 | Configure AllowedHosts + CORS without wildcard fallback | Dev |
| 🟡 | Add security headers middleware | Dev |
