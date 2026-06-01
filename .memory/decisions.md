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

## 2026-01-06 router.refresh() for GoRouter redirect

**Context:** Login navigates to Firebase Auth and sets auth state, but GoRouter doesn't redirect because the redirect is only evaluated on navigation events.
**Decision:** Call `router.refresh()` via `ref.listen` in NexoraApp when auth status changes.
**Reason:** GoRouter 17.x requires explicit `refresh()` to re-evaluate redirect; recreating GoRouter via Provider doesn't trigger redirect.
**Consequences:** Extra `ref.listen` in app shell; clean separation of concerns.
