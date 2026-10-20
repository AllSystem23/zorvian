# Plan de UI móvil (teléfonos) — Zorvian ERP

> **Estado**: Fase 0 ✅ · Fase 1 ✅ · Fase 2 (fundaciones + kanban + diálogos) ✅ · Fase 3 en curso · Fase 5.1 ✅ · **Fecha**: 2026-10-20
> **Alcance**: capa de presentación Flutter (`frontend/`). Sin cambios de negocio ni de backend.
> **Objetivo**: que un usuario pueda operar el ERP completo desde un teléfono de 360–430 px,
> sin scroll horizontal, sin overflows y con acciones primarias alcanzables con una mano.

## Progreso (commits de esta tanda)

| Commit | Alcance |
|---|---|
| `70abe9e` | Fase 0 — tokens (`ZBreakpoints`/`ZTouchTarget`), `NavigationBar` M3 con hubs, `MobileScopeBar`, touch targets |
| `1cc57be` | Fase 1 — `ZDataTable` en tarjetas (<600 px), fix de desalineación al ocultar columnas |
| `4f04e5c` | Fase 2 (fundaciones) — `showZAdaptiveDialog`, `ZResponsiveDialog`, `ZStickyActionBar`, `ZConfirmDialog`/`ZModal` |
| `a799402` | Fase 5.1 — permisos Android/iOS + `FlutterFragmentActivity` (biometría) |
| `74978fa` | Fase 2 (kanban) — CRM y cotizaciones en lista vertical por etapa |
| `adfc46f` | Fase 3 (parcial) — `ZStatCard` compacto + `FittedBox` en KPIs |
| `1a46a1b` | `ZDataTable` con altura no acotada (crash del comparativo por sucursal) |
| `08090fc` | Fase 2 (diálogos) — 28 `AlertDialog` de páginas → `showZAdaptiveDialog`/`ZResponsiveDialog` + `ZConfirmDialog` |

**Próximos pasos sugeridos** (en orden de valor):

1. Formularios de 2–3 columnas → una columna (empezar por `client_form`, `product_form`,
   `sale_form`, `purchase_form`).
2. Barrido de rutas a 360 px con un helper de test (`pumpAt(360/390/430)`) y corregir los
   overflows que aparezcan; luego dashboards (gráficos con `AspectRatio`) y POS.

---

## 0. Contexto y diagnóstico

Base medida al iniciar el plan: **481 archivos `.dart`**, **217 páginas** (`*_page.dart`),
**254 rutas** `GoRoute`, **90 `NavItem`** en **11 módulos**, **42 archivos de test**.

La app ya tiene `_MobileLayout` y una barra inferior, pero el 95 % de las páginas se diseñó
para escritorio y **ningún test corre por debajo de 900 px**.

| # | Problema | Evidencia |
|---|---|---|
| 1 | Breakpoints como literales duplicados (`<576`, `<992`, `>=900`) | `responsive_layout.dart`, `global_header.dart`, `app_shell.dart` |
| 2 | El tab "Más" nunca se marcaba activo (`clamp(0,3)` sobre 5 tabs) | `mobile_bottom_nav.dart:158` |
| 3 | Sin selectores de compañía/sucursal/bodega en móvil | `GlobalHeader` solo en tablet/desktop |
| 4 | `ZDataTable` sin modo móvil (filas con anchos fijos, sin scroll horizontal) | `z_data_table.dart` |
| 5 | 20 listados usan `ZDataTable` | grep `ZDataTable<` |
| 6 | Diálogos con ancho fijo (560/460/420/400/360/320) | `z_command_palette.dart`, `subscription_plans_page.dart`, `pos_page.dart`, … |
| 7 | Gráficos con tamaño fijo (`BiPieChart` 160, `BiGauge` 72) y dashboards 1/2/4 columnas | `shared/ds/components/bi/*` |
| 8 | Sin tests por debajo de 900 px | `test/*_test.dart` |
| 9 | Permisos de plataforma ausentes (cámara, notificaciones, `INTERNET` en release, FaceID) | `AndroidManifest.xml`, `Info.plist` |
| 10 | Código muerto: `MobileDashboardPage` (sin ruta), `ResponsivePadding` (sin consumidores) | `features/dashboard/`, `responsive_layout.dart` |
| 11 | Touch targets de 36 px (header) y 28 px (sidebar) | `global_header.dart`, `sidebar_item.dart` |
| 12 | `applicationId` legacy `com.nexora.nexora` | `android/app/build.gradle.kts` |

---

## 1. Principios

1. **Un solo código, tres layouts.** Nada de forks por pantalla (`MobileDashboardPage` es el
   contraejemplo: quedó sin ruta).
2. **La adaptación vive en el Design System**, no en cada página: si hay que decidir
   "tarjeta vs tabla" en 20 sitios, se decide una vez en `ZDataTable`.
3. **Teléfono = una columna, una acción primaria, cero scroll horizontal.**
4. **Contexto visible siempre**: compañía, sucursal y bodega no pueden quedar escondidos
   (una escritura en la sucursal equivocada es un error de negocio).
5. **48 dp de área táctil** (44 mínimo), cuerpo ≥ 12 sp, contraste AA, `textScale` hasta 1.3.
6. **Sin features nuevas**: solo representación y ergonomía.

---

## 2. Fase 0 — Fundaciones (P0)

**Meta**: que la app deje de ser "un escritorio encogido": breakpoints centralizados,
navegación inferior correcta y contexto multi-tenant visible en el teléfono.

### Checklist

- [x] `ZBreakpoints` + `ZTouchTarget` en `shared/ds/tokens/breakpoints.dart` (fuente única).
- [x] `ResponsiveBuilder`/`ResponsiveGrid`/`ResponsivePadding` usan los tokens (sin literales).
- [x] `MobileBottomNav` migrado a **`NavigationBar` (M3)**; el tab activo ya no usa `clamp`.
- [x] Tabs **derivados de rol + permisos + plan** (módulos accesibles) + tab **"Más"**.
- [x] "Más" abre hoja a pantalla completa con **buscador** y todos los módulos agrupados.
- [x] **`MobileScopeBar`**: franja bajo el AppBar con compañía · sucursal · bodega, tappable.
- [x] Hoja de contexto con las 3 secciones (empresa / sucursal / bodega), incluida
      "Todas las sucursales" y "Todas las bodegas", y auto-selección de la primera sucursal.
- [x] Touch targets: acciones del header 44 dp, ítems del sidebar 44 dp.
- [x] Tests: `mobile_navigation_test.dart`, `mobile_scope_bar_test.dart`.
- [x] `flutter analyze lib test` en 0 issues y suites verdes.

### Decisiones de diseño

- **Breakpoints**: `< 600` teléfono, `600–991` tablet, `≥ 992` escritorio.
  Se mantienen los cortes históricos de tablet/escritorio para no regresar layouts existentes;
  el único cambio real es el umbral de teléfono (576 → 600).
- **Tabs**: 4 hubs + "Más". Los hubs se eligen de una lista preferente
  (`ventas`, `inventario`, `talento`, `finanzas`, `compras`, `flota`) filtrando por los módulos
  que el usuario realmente puede ver (`NavConfig.getModulesForRole`). Si no tiene ninguno,
  ve "Inicio" + "Más". El hub navega al **primer ítem permitido** del módulo.
- **"Más"**: hoja `isScrollControlled` con buscador local, grupos del sidebar
  (`NavConfig.groupLabels`) y un `ListTile` de 48 dp por ítem. Siempre es el destino
  seleccionado cuando la ruta no pertenece a ningún hub.
- **Contexto**: la franja es parte del `AppBar` (`bottom`), así hereda el `SafeArea` superior
  y el color de la superficie. La selección replica exactamente la semántica del header de
  escritorio (reset de sucursal al cambiar de compañía, reset de bodega al cambiar de sucursal,
  `switchTenant` para SuperAdmin, invalidación de listas).

### Criterios de aceptación

- A 360, 390 y 430 px: compañía/sucursal/bodega visibles; tab activo correcto en todas las rutas;
  "Más" lista solo lo permitido; ningún target < 44 dp.

---

## 3. Fase 1 — Listados: tabla → tarjeta (P0) ✅

**Meta**: los listados con `ZDataTable` legibles en teléfono sin scroll horizontal.

**Resultado**: la adaptación se hizo **una sola vez en el componente**, no en 17 páginas.
Cualquier `ZDataTable` con ancho menor a `ZBreakpoints.phone` renderiza tarjetas; a partir
de 600 px se comporta exactamente igual que antes (misma cabecera, mismos anchos, misma
paginación).

- [x] `ZDataTable.mobileCardBuilder` (tarjeta a medida, opcional) + **renderer automático**:
      `título = primera columna visible`, resto como `ETIQUETA → valor` (numéricos a la derecha).
- [x] Selección (checkbox), `expandedRowBuilder` (panel bajo la tarjeta) y `onTap` por fila
      conservados en el modo tarjeta.
- [x] `bulkActions` en un `Wrap` de 48 dp dentro de la barra de selección (antes desbordaba).
      No se movieron a un menú `⋮` porque **no existe una API de acciones por fila**: cada
      página pone su `PopupMenuButton` dentro de la celda y eso ya es vertical.
- [x] Paginación compacta: `1–20 de 340` + flechas de 48 dp en teléfono; `1 / 17` en escritorio.
- [x] Toolbar de dos líneas en teléfono (buscador arriba, acciones a la derecha).
- [x] **Bug corregido**: ocultar una columna desalineaba las celdas. Ahora el índice de celda
      se resuelve por `column.id`/posición original, no por la posición filtrada.
- [x] `ZColumn.width` se ignora por debajo de 600 px (sin `SizedBox` que desborde).
- [x] Celdas faltantes devuelven vacío en vez de lanzar `RangeError`.
- [x] **Altura no acotada soportada**: si la tabla vive dentro de un `ListView`/`Column`
      (comparativo por sucursal del dashboard ejecutivo), se renderiza con `shrinkWrap` y sin
      scroll propio en vez de `Expanded`. Antes ese caso lanzaba
      "RenderFlex children have non-zero flex but incoming height constraints are unbounded".
- [x] Los **17 listados** que usan `ZDataTable` quedan cubiertos sin tocar sus páginas
      (`user_list`, `super_admin_companies`, `invitation_history`, `document_center`,
      `provider_list/invoices/contracts`, `kardex`, `inventory_valuation`,
      `cash_register_list/detail`, `payroll_run_detail`, `trial_balance`,
      `comparative_reports`, `equity_changes`, `goals_dashboard`, `executive_dashboard`).
- [x] Tests: `test/z_data_table_mobile_test.dart` (6: tarjetas, desalineación al ocultar
      columna, `mobileCardBuilder`, `width` ignorado, paginación compacta y escritorio intacto).

**Aceptación**: listados a 360 px sin overflow ni scroll horizontal; a 1280 px idénticos a hoy.

---

## 4. Fase 2 — Formularios y modales (P0) — en curso

**Hecho (fundaciones del DS)**:

- [x] `showZAdaptiveDialog` + `ZResponsiveDialog`: en `< 600` se abre como **hoja inferior**
      a pantalla completa (handle, scroll propio, acciones fijas al pie); en `≥ 600` es un
      `Dialog` centrado con `maxWidth` (560 por defecto). Sustituye los anchos fijos.
- [x] `ZStickyActionBar`: acciones apiladas a ancho completo con la **primaria arriba** en
      teléfono, `leading` para un resumen, y `includeViewInsets` para que el pie suba con el
      teclado (la hoja inferior no lo hace sola).
- [x] **`ZConfirmDialog` 100 % adaptativo** (lo usan ~45 pantallas) y `ZModal` migrados: todas
      las confirmaciones de la app pasan a hoja en teléfono sin tocar cada página.
- [x] Tests: `test/z_responsive_dialog_test.dart` (5).

**Hecho (kanban)**:

- [x] **Kanban móvil**: `features/crm/widgets/kanban_board.dart` y
      `features/quotes/pages/quote_kanban_page.dart` detectan `< 600` y renderizan una
      **lista vertical agrupada por etapa/estado** (encabezado con color + contador, tarjetas
      a ancho completo, "Sin oportunidades"/"Sin cotizaciones" en las vacías). El drag & drop
      (inusable con columnas de 280–300 px) se reemplaza por un menú `⋮` **"Mover a otra
      etapa/estado"** que llama al mismo `updateOpportunityStage`/`updateStatus`.
      Las cotizaciones además pasan de `PENDING/SENT` a etiquetas en español
      (PENDIENTE/ENVIADA/…) también en escritorio.
- [x] Tests: `test/kanban_mobile_test.dart` (3: secciones verticales sin `Draggable`, menú que
      mueve de etapa, y escritorio con columnas y drag intactos).

**Hecho (diálogos de páginas)**:

- [x] **28 `AlertDialog` directos migrados** a `showZAdaptiveDialog` + `ZResponsiveDialog`
      (ventas, compras/OC, solicitudes de cotización, transferencias, vacaciones, permisos,
      nómina/liquidación, servicios/prestadores, garantías, talleres, CRM, flota/PalmTrack,
      perfil, administración: usuarios, roles, invitaciones, certificados, planes,
      empresas SuperAdmin, reportes personalizados, catálogo de cuentas, QR/etiquetas).
      Patrón: `content:` → `child:`, se elimina el `SingleChildScrollView`/`SizedBox(width:)`
      interno (el diálogo ya scrollea) y se conserva `maxWidth` explícito solo donde el
      contenido lo pide.
- [x] Confirmaciones simples (borrar cuenta/rol/reporte, desactivar empresa, acciones
      destructivas) → `ZConfirmDialog.show(..., isDestructive: true)`.
- [x] **Cero `AlertDialog` en `lib/`** (verificado por grep). `analysis_options` desactiva
      `sort_child_properties_last` porque estos diálogos reciben el cuerpo antes del pie.
- [x] Tests ajustados a `ZResponsiveDialog`: `roles_permissions_crud_test.dart` (3).

**Pendiente**:

- [ ] Formularios de 2–3 columnas → una columna con secciones colapsables.
- [ ] Inputs móviles: teclados numéricos, `textInputAction`, autofoco ordenado.
- [ ] POS en teléfono: carrito como sheet persistente, scanner como FAB.

**Aceptación**: ningún diálogo excede el ancho; formularios usables con teclado abierto;
acción primaria siempre alcanzable.

---

## 5. Fase 3 — Dashboards y BI (P1) — en curso

- [x] **KPIs a 2 columnas con valor que se encoge**: `ZStatCard` gana un modo compacto
      (`< 220 px`, que es el ancho real de una tarjeta a 360 px) con icono de 32, padding
      reducido y la tendencia movida a la fila del valor; el valor va en `FittedBox`
      (`scaleDown`). `BiKpiCard` también. Antes, a 360 px, la tarjeta desbordaba 16 px
      (icono 40 + padding 24 + chip de tendencia no caben en 158 px) y el monto se partía
      en dos líneas.
- [x] Tests: `test/mobile_stat_cards_test.dart` (2, a 158 px con montos de 7 cifras).
- [ ] Gráficos con `LayoutBuilder` + `AspectRatio` (sin alto fijo), donut con leyenda debajo,
      gauge escalado, `RepaintBoundary` y lectura de valores por tap.
- [ ] Secciones de dashboard como tarjetas full width con "Ver todo".
- [x] Tabla "Comparativo por Sucursal" → tarjetas en teléfono. Se resolvió en el componente:
      `ZDataTable` detecta **altura no acotada** (cuando vive dentro de un `ListView`/`Column`,
      como en el dashboard ejecutivo) y pasa a `shrinkWrap` + `NeverScrollableScrollPhysics`
      en vez de `Expanded`. **Era un bug real**: ese caso lanzaba
      "RenderFlex children have non-zero flex but incoming height constraints are unbounded".
- [ ] Botón "Reporte PDF" a `PopupMenu` si no cabe en el `Row` (medido a 360 px: el botón cabe
      y el título del dashboard hace wrap, así que no hace falta).

**Aceptación**: dashboard ejecutivo y comercial legibles a 360 px, 2 KPIs por fila, 0 overflows.

---

## 6. Fase 4 — Pantallas operativas (P1)

1. Ventas y cobros: lista, detalle con resumen pegajoso, pago de crédito en 3 pasos.
2. Inventario: transferencias, kardex, escaneo (permiso de cámara).
3. Asistencia / kiosco / QR check-in: permisos + fallback sin cámara.
4. Aprobaciones: bandeja con Aprobar/Rechazar de 48 dp.
5. Formularios que exigen sucursal: el aviso debe **abrir el selector**.
6. Flota: GPS a pantalla completa con bottom sheet de parada, entregas y hoja de ruta.

**Aceptación**: cada flujo completo en 390 px con una sola mano.

---

## 7. Fase 5 — Calidad móvil (P1/P2)

### 7.1 Plataforma ✅ (parcial)

- [x] `AndroidManifest.xml` (main): `INTERNET` + `ACCESS_NETWORK_STATE` (**bloqueante de release**:
      solo existían en `debug/` y `profile/`), `CAMERA` + `<uses-feature camera required="false">`,
      `ACCESS_FINE/COARSE_LOCATION` (GPS solo en primer plano), `POST_NOTIFICATIONS` y
      `USE_BIOMETRIC`. No se declara el micrófono: la cámara se abre con `enableAudio: false`.
- [x] `Info.plist`: `NSCameraUsageDescription`, `NSPhotoLibraryUsageDescription`,
      `NSPhotoLibraryAddUsageDescription`, `NSLocationWhenInUseUsageDescription`,
      `NSFaceIDUsageDescription` (textos en español, con el motivo real de cada permiso).
- [x] **Bloqueante corregido**: `MainActivity` pasó de `FlutterActivity` a
      `FlutterFragmentActivity`. `local_auth` usa `BiometricPrompt`, que exige una
      `FragmentActivity`; con `FlutterActivity` la biometría fallaba con
      `PlatformException(no_fragment_activity)` en Android.
- [ ] Orientación: portrait en captura (asistencia, QR, firma).
- [ ] `applicationId` → `com.zorvian.erp` **antes** de publicar (hoy `com.nexora.nexora`;
      cambiarlo exige coordinar `build.gradle`, Firebase y las tiendas).
- [ ] `PopScope` en formularios con cambios sin guardar.
- [ ] Deep links de recuperación de contraseña e invitaciones.

### 7.2 Offline y estados

- [ ] Banner "Sin conexión — N cambios pendientes" + reintento manual (Drift ya existe).
- [ ] Estados vacío/error/cargando unificados y accionables.
- [ ] Cola de sincronización visible y pantalla de conflictos (P2).

### 7.3 Rendimiento percibido

- [ ] `ListView.builder` en todas las listas; quitar `shrinkWrap` + `NeverScrollableScrollPhysics`
      dentro de `SingleChildScrollView`.
- [ ] Imágenes redimensionadas; `RepaintBoundary` en gráficos; `const` en widgets estáticos.

### 7.4 Accesibilidad

- [ ] `tooltip`/semántica en iconos, `textScale` 1.3 sin overflow, contraste AA.

### 7.5 Tests

- [ ] `test/helpers/viewport.dart` con `pumpAt(360/390/430)`.
- [ ] Golden/overflow de 5 pantallas × 3 anchos.
- [ ] Barrido de ~30 rutas top a 360 px que falla ante cualquier `RenderFlex overflow`.
- [ ] Regresiones: "Más" activo fuera de hubs; selectores operativos en móvil.

**Aceptación**: 0 overflows en la matriz 360/390/430 × `textScale` 1.0/1.3; cámara y
notificaciones funcionando en dispositivo real.

---

## 8. Priorización y cronograma

| Fase | Alcance | Prioridad | Días | Valor |
|---|---|---|---|---|
| 0 | Tokens, `NavigationBar`, "Más", contexto visible, touch targets | **P0** | 4 | Deja de ser un escritorio encogido |
| 1 | `ZDataTable` → tarjetas (20 listados) | **P0** | 6 | Desbloquea la mayoría de pantallas |
| 2 | Diálogos/sheets + formularios + POS + kanban | **P0** | 5 | Permite crear y editar desde el teléfono |
| 3 | Dashboards/BI adaptativos | P1 | 5 | Vista de supervisor/ejecutivo |
| 4 | Flujos operativos | P1 | 7 | Trabajo diario de campo |
| 5 | Permisos, offline, a11y, tests de ancho | P1-P2 | 5 | Confiabilidad y release |

**Hito "móvil usable" = Fases 0+1+2 (≈3 semanas).**

---

## 9. Fuera de alcance

- Rediseño de negocio, nuevos reportes o endpoints.
- Layout específico de tablet (hereda tokens, no se rediseña).
- Reescritura del Design System o migración de tema.
- Notificaciones push (solo se deja preparado el permiso).
- Offline completo (extender Drift a todas las entidades es un proyecto aparte).

---

## 10. Decisiones abiertas

1. Tabs inferiores: fijos por rol (implementado) o configurables por usuario.
2. Contexto de compañía/sucursal: en el AppBar (implementado) o dentro de "Más".
3. POS en teléfono: flujo completo de venta o solo consulta + cobro.
4. Orientación: ¿bloquear portrait en teléfono?
5. Publicación en stores: define el momento de cambiar `applicationId`.
