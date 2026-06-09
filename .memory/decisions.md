# Decisiones Técnicas

## 2026-06-09 RLS + Query Filters para multi-tenancy

**Contexto:** La auditoría recomendó implementar RLS en PostgreSQL para aislar datos entre tenants.

**Decisión:** Implementar ambos: `HasQueryFilter` en EF Core (filtro a nivel aplicación) + `TenantSessionInterceptor` + script RLS (filtro a nivel base de datos).

**Razón:** Defensa en profundidad — si un query evade el filtro de EF Core, RLS en PostgreSQL lo bloquea.

**Consecuencias:** El interceptor `TenantSessionInterceptor` ejecuta `SET app.tenant_id = '{tenant}'` al abrir cada conexión. El script `RLS_Enable.sql` habilita RLS en tablas con columna TenantId.

## 2026-06-09 Mantener drift/web.dart con ignore

**Contexto:** `flutter analyze` marcaba como deprecado `drift/web.dart` (reemplazo: `drift/wasm.dart`).

**Decisión:** Mantener `drift/web.dart` con `// ignore_for_file: deprecated_member_use`.

**Razón:** `WasmDatabase` requiere sql.js wasm + setup complejo para web. La migración no es drop-in replacement.

## 2026-06-09 Package:nexora como prefijo de imports

**Contexto:** Había imports relativos rotos que referenciaban archivos inexistentes.

**Decisión:** Usar `package:nexora/...` como prefijo consistente en lugar de rutas relativas.

**Razón:** Elimina errores de resolución al mover archivos y es el estándar de Dart.

## 2026-06-09 DioClient expone métodos directamente

**Contexto:** El provider `dioClientProvider` devuelve `DioClient`, y algunos componentes intentaban acceder a `.dio` (getter inexistente).

**Decisión:** `DioClient` expone `get/post/put/delete` directamente, sin getter `.dio`.

**Razón:** Es un wrapper que centraliza headers, interceptors y manejo de errores. No hay razón para exponer el `Dio` interno.
