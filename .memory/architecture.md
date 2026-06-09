# Arquitectura

## Backend (.NET)
- Clean Architecture: Core → Application → Infrastructure → Web
- Multi-tenancy via TenantId + Query Filters (EF Core) + RLS (PostgreSQL)
- JWT Bearer authentication
- Rate limiting, security headers middleware

## Frontend (Flutter)
- Feature-first structure bajo `lib/features/`
- DS (Design System) compartido en `lib/shared/ds/`
- Providers por feature con Riverpod
- Servicios core: network (Dio + interceptors), offline (sync engine), utilidades
