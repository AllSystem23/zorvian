# Plan de UI móvil (teléfonos) — Zorvian ERP

> **Estado**: Fase 0 ✅ · Fase 1 ✅ · Fase 2 (fundaciones + kanban + diálogos + formularios) ✅ · Fase 3 ✅ · Fase 5.1 ✅ · **Fecha**: 2026-10-20
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
| `1ac638e` | Fase 2 (formularios) — `ResponsiveFormRow` y campos en una columna en teléfono |
| `3d928e9` | Fase 2/5.5 — `ZPageHeader` adaptativo + `test/helpers/viewport.dart` (`pumpAt`) |
| `46e6e9c` | Fase 3/5.5 — barrido a 360 px: `isExpanded` en 65 dropdowns, `BiPieChart` con leyenda debajo, tarjetas de plan flexibles |
| `661d091` | Docs del barrido a 360 px |
| `46323ba` | Fase 4 — `ZCartLine` adaptativo en carritos de venta y cotización |
| `03f7c8c` | Docs del carrito de línea |
| `93621ff` | Fase 3 — dashboards BI financiero y operacional a 360 px (`BiGauge` adaptativo, charts sin alto fijo, anomalías) |
| `4d664d2` | Fase 3 — pantallas de flota a 360 px (`ZStatGrid`, badges/tendencias/acciones en `Wrap`, barra de exportación) |
| `4832929` | Fase 3/4 — POS y GPS de flota a 360 px (grilla de 2 columnas con más alto, badge "en carrito" inline, banner del período con ellipsis, marcador 64×68, `Material` del `ListTile`) |
| `2685c70` | Docs del barrido de POS y GPS de flota |
| `1e6230a` | Fase 3 — los 10 formularios de flota a 360 px (`Expanded` en el título de `_buildSection`, `SwitchListTile` en `Material`), `test/helpers/fake_api.dart` |
| `3fb7aca` | Fase 3 — 6 formularios de catálogo y 9 listados a 360 px (sin cambios de código) |
| `2c1fe71` | Fase 3 — formularios transaccionales a 360 px (`Wrap` en "Productos (n) + Agregar producto", `ResponsiveFormRow` en el período de pagos a prestadores, línea y estimados en columna) |
| `082b92f` | Fase 3 — 11 pantallas de detalle a 360 px (filas etiqueta/valor con `Expanded`, acciones en `Wrap`) |
| `4667989` | Fase 3 — flujo de aprobación y liquidación final a 360 px |
| `5794fcb` | Fase 3 — 23 detalles más (caja, nómina, CRM, documentos) a 360 px |
| `5b23609` | Fase 3 — nómina, presupuestos y webhooks a 360 px: `ListTile` en `Material`, tarjetas de resumen apiladas, `_summaryRow` con `Expanded` y el drift test que vigila el `ZCard` |
| `c375470` | Fase 3 — contabilidad, tesorería y reportes a 360 px: `ZButton` `fullWidth: false` dentro de `Row`, `_row` del estado de resultados con `Expanded` y `ZStatGrid` en el centro de reportes |
| `3258bc9` | Fase 3 - RRHH, CRM, conciliaciones, documentos y asistencia a 360 px (encabezado y acciones del centro documental en columna) |
| `08ee68c` | Fase 3 - PalmTrack, objetivos, garantías y motor documental a 360 px (+3 bugs: "Mis Objetivos", perfil SSO y asserts de ink splashes) |
| `58899e4` | Fase 3 - aprobaciones, reportes personalizados, perfil, onboarding, chat, predicciones, garantías, años fiscales, kiosco y QR a 360 px |
| `5fe6d86` | Fase 3 - listados de catálogo, operativos y de flota a 360 px (7 + 8 + 10 casos, 6 overflows) |
| (este lote) | Fase 3 - dashboards y paneles a 360 px (9 casos, 1 bug y 4 overflows) |

**Próximos pasos sugeridos** (en orden de valor):

1. Resto de formularios y detalles que aún no se montaron (conciliaciones, CRM
   lead/oportunidad, contratos de proveedor, PalmTrack y nómina de liquidación).
2. Fase 5.3/5.4: rendimiento percibido (skeletons/paginación) y accesibilidad móvil.

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

**Hecho (formularios)**:

- [x] **`ResponsiveFormRow`** (`core/widgets/responsive_layout.dart`): fila de campos que en
      teléfono se **apila en una columna** (`Column(stretch)`, mismo `spacing` vertical) y desde
      600 px reparte el ancho en `Row` con `Expanded` por hijo. A 360–430 px dos campos
      compartiendo fila quedan por debajo de ~170 px y el label con icono se recorta.
- [x] Migradas las filas de campos que **no tenían fallback móvil**: cliente (municipio/
      departamento), producto (precio/costo, stock/mínimo), venta (cuotas/interés), presupuesto
      (año/mes), contrato (país/moneda), lead (nombre/apellido), webhook (reintentos/intervalo),
      incapacidad (fechas), flujo de aprobación (monto mín/máx), líneas de compra, orden de
      compra y solicitud de cotización (cantidad/costo/dto), y `workshop_form` (5 bloques
      `if (isWide) Row(...) else [...]` duplicados → una sola declaración; `isWide` eliminado).
- [x] Test: `test/responsive_form_row_test.dart` (3: apilado a 360, fila con `Expanded` a 900,
      espaciado configurable).

**Pendiente**:

- [ ] Inputs móviles: teclados numéricos, `textInputAction`, autofoco ordenado.
- [x] POS en teléfono: carrito en hoja inferior (el layout móvil ya existía; ver el barrido
      a 360 px en Fase 3 — grilla de 2 columnas, badge "en carrito" y banner del período).
      El escáner como FAB queda fuera de alcance (no hay lector implementado).
- [x] **`ZPageHeader`** (encabezado de página adaptativo): en teléfono el título ocupa todo el
      ancho (parte en dos líneas) y las acciones bajan a una segunda fila en `Wrap`; desde
      600 px comparten fila con el título expandido. A 360 px "Nueva Transferencia entre
      Bodegas" + "Guardar" desbordaba. Migrado en orden de compra, solicitud de cotización y
      transferencia entre bodegas; los totales de esas pantallas pasan a `Expanded` con
      alineación al final (un monto largo ya no empuja el `Row`).
- [x] Chips de saldo de vacaciones (`Acumulados/Tomados/Pendientes/Disponibles`) en `Wrap`
      con `spaceEvenly`: bajan a 2×2 si no caben en una línea.

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
- [x] **`BiPieChart` adaptativo**: en tarjetas angostas (`< size + 140`, el caso de un dashboard a
      2 columnas en teléfono) la **leyenda baja debajo de la dona** y la dona se limita al ancho
      disponible (`math.min(size, maxWidth)`). Antes la leyenda quedaba con ~30 px de ancho y
      desbordaba 42 px. Probado a 202 px (teléfono) y 520 px (escritorio).
- [x] **`BiBarChart` (horizontal y vertical), `BiLineChart` y `BiGauge`** verificados a 202 px:
      no desbordan (los ejes con `reservedSize` y el gauge en una tarjeta de 160×140).
- [x] Tests: `test/bi_charts_mobile_test.dart` (7: los 5 del barrido anterior + gauge en la
      tarjeta de 80×80 y sin alto acotado) y el dashboard poblado a 360 px en
      `test/dashboard_page_test.dart`.
- [ ] Gráficos con `AspectRatio` (sin alto fijo), `RepaintBoundary` y lectura de valores por tap.
- [ ] Secciones de dashboard como tarjetas full width con "Ver todo".
- [x] Tabla "Comparativo por Sucursal" → tarjetas en teléfono. Se resolvió en el componente:
      `ZDataTable` detecta **altura no acotada** (cuando vive dentro de un `ListView`/`Column`,
      como en el dashboard ejecutivo) y pasa a `shrinkWrap` + `NeverScrollableScrollPhysics`
      en vez de `Expanded`. **Era un bug real**: ese caso lanzaba
      "RenderFlex children have non-zero flex but incoming height constraints are unbounded".
- [ ] Botón "Reporte PDF" a `PopupMenu` si no cabe en el `Row` (medido a 360 px: el botón cabe
      y el título del dashboard hace wrap, así que no hace falta).
- [x] **Dashboards financiero y operacional a 360 px** (`test/bi_dashboards_mobile_test.dart`):
      - **`BiGauge`**: la dona era fija de 72×72 y en la tarjeta de **80×80** que calcula el
        dashboard a 4 columnas (`(ancho − 48) / 4`, mínimo 80) el alto disponible es ~37 px, así
        que **desbordaba 9 px en las 7 tarjetas**. Ahora usa `LayoutBuilder`: el diámetro se
        calcula del ancho y del alto reales (mínimo 24) y el valor central escala con `FittedBox`.
        Funciona con alto acotado y sin acotar (dentro de un `ListView`).
      - **Barra de "Cuentas por Cobrar"/"por Pagar"**: `SizedBox(height: 150)` alrededor de un
        `BiBarChart` que ya trae su propio alto + padding → **40 px de desborde**. Se usa el
        parámetro `height:` del chart y se elimina el `SizedBox`.
      - **Pie de flujo de caja / inventario por categoría**: `SizedBox(height: 200)` con la leyenda
        ya debajo de la dona → **52 px de desborde**. Se elimina el `SizedBox` y la tarjeta crece
        lo que necesite.
      - **`AnomalyDetectionSection`**: la frase del estado vacío iba en un `Row` sin `Expanded` y
        **desbordaba 668 px**; el número de asiento pasa a `Expanded` + ellipsis.
- [x] Tests del barrido de dashboards: `test/bi_dashboards_mobile_test.dart` (4: financiero,
      operacional y la sección de anomalías vacía y con anomalías). Los dashboards se montan con
      **alto 4000** para que el `ListView` construya todas las secciones: con el alto real de un
      teléfono solo se montan las primeras y los overflows del final pasan desapercibidos.
      El `Timer.periodic` del `AutoRefreshMixin` se cancela desmontando el árbol al final.
- [x] **Pantallas de flota a 360 px** (`test/fleet_pages_mobile_test.dart`): alertas, predictivo
      (3 pestañas), seguimiento de entregas y reportes.
      - **`ZStatGrid` (nuevo, en el DS)**: las 4 pantallas usaban `GridView.count(childAspectRatio:
        1.4–2.2)`; a 360 px con 2 columnas la tarjeta mide ~158 px y el alto derivado queda en
        72–113 px cuando la tarjeta compacta necesita ~92 px → **desbordaba 20–40 px en todas las
        tarjetas**. Debajo de 600 px (o cuando la columna queda < `compactWidth = 200` px) la
        rejilla usa `Wrap` con **alto intrínseco**; desde 600 px conserva el `GridView` con el
        `aspectRatio` recibido, así que el escritorio no cambia.
      - **Filas → `Wrap`**: título + 2 badges de la tarjeta de alerta, los 3 badges de la anomalía
        de combustible, las **5 estadísticas** de la tarjeta de tendencia (y su matrícula +
        variación), código + badge del listado y del encabezado del timeline, y los botones
        "Enviar ETA" / "Confirmar Entrega" (no caben juntos a 360 px).
      - **Barra de exportación de reportes**: los botones PDF + Excel en un `Row` desbordaban
        **105 px**; ahora van en un `Wrap` dentro de un `Expanded` (se apilan). El encabezado de
        "Tendencia de Combustible" cambia el `Spacer` por un `Expanded` en el título.
      - **`ZProgress`**: el `Row` de etiqueta + valor (`spaceBetween`) desbordaba **18 px** cuando
        el widget vive dentro de un `Expanded` angosto; la etiqueta pasa a `Flexible` + ellipsis
        (mismo aspecto a ancho normal).
      - **KPIs por cuenta contable**: 3 `Expanded` dejaban cada tarjeta en ~101 px en teléfono;
        ahora `ZStatGrid` a 2 columnas.
- [x] Tests: `test/fleet_pages_mobile_test.dart` (7) y `test/z_stat_grid_test.dart` (4: alto
      intrínseco en teléfono, `GridView` con `aspectRatio` y 5 columnas en escritorio, vuelta al
      `Wrap` cuando la columna es angosta, y rejilla vacía).
- [x] **GPS de flota a 360 px** (grupo `FleetGpsPage` de `test/fleet_pages_mobile_test.dart`):
      el layout ya apilaba mapa (300 px) + lista en teléfono, así que solo hubo dos correcciones
      de tamaño y una de plataforma.
      - **Marcador del vehículo**: `Marker(width: 44, height: 56)` dejaba al `Column` (icono 36 +
        etiqueta de matrícula) **6 px corto** → pasa a **64×68**, que además evita que la
        matrícula de 8 px se recorte. Sigue anclado por el centro del punto (`Alignment.center`),
        así que el icono solo sube ~6 px.
      - **`ListTile` dentro de `ZCard`**: el `ZCard` pinta el fondo con `DecoratedBox`, y el
        `ListTile` de la lista de vehículos disparaba el assert de debug *"ListTile background
        color or ink splashes may be invisible"* (no es un overflow, pero rompía cualquier test
        que montara la página) → se envuelve en `Material(type: MaterialType.transparency)`,
        igual que en `fleet_tracking_page.dart`.
      - **`flutter_map` en tests**: los tiles de OSM piden red y el binding devuelve 400, pero el
        `TileLayer` consume el error por su cuenta (solo aparecen los avisos de uso de OSM) → la
        página se puede montar sin mocks de imagen.
      - La matrícula aparece 2 veces (marcador + lista): el test assertea `findsNWidgets(2)`, lo
        que de paso prueba que el marcador se dibujó.
- [x] **POS a 360 px** (`test/pos_mobile_test.dart`, 4 casos): grilla de productos, alta de un
      producto, hoja del carrito y método Crédito. Tres defectos reales encontrados:
      - **`_ProductTile` desbordaba 20 px**: con 2 columnas a 360 px la tarjeta queda en 164 px y
        el `childAspectRatio: 1.7` dejaba **75 px** de alto útil para ~92 px de contenido (nombre
        a 2 líneas + código + precio + stock). El ratio de 2 columnas pasa a **1.3** (unos 126 px);
        3 y 4 columnas conservan 1.5, así que tablet/escritorio no cambian.
      - **Badge "en carrito" reventaba en debug**: era un `Positioned` **dentro de un `Column`**
        (no un `Stack`) → `Incorrect use of ParentDataWidget` en cuanto el producto entraba al
        carrito. Ahora es un `Icons.check_circle` inline al lado del nombre (`Row` + `Expanded`),
        que además no tapa el texto en una tarjeta tan baja.
      - **Banner del período abierto desbordaba 24 px**: el `Text` "Período … abierto" va en
        `Expanded` + `maxLines: 1` + ellipsis.
      - De paso, los **skeletons** de carga usaban 4 columnas fijas (tarjetas de ~76 px a 360 px):
        ahora replican el reparto de la grilla real (2 columnas, ratio 1.3 en teléfono) para que
        el esqueleto se parezca al contenido que reemplaza.
      - La hoja del carrito (`_CartSummary`, `_PaymentMethodSelector` de 4 `Expanded` y los
        campos de crédito) ya cabía; el test lo fija como contrato.
- [x] **Formularios de flota a 360 px** (`test/fleet_forms_mobile_test.dart`, 11 casos): los 10
      formularios (entrega, documento, conductor, gasto, combustible, mantenimiento, ruta, viaje,
      vehículo y taller) a 360/390/430 px, más alta y edición del gasto.
      - **`_buildSection` desbordaba hasta 73 px**: los 10 archivos comparten el helper, que pinta
        `Row(Icon + Text(title))` **sin `Expanded`**. En los tests la fuente dibuja cada carácter
        del ancho del `fontSize`, así que "Información del Gasto" (21 caracteres a 16 px) no cabe en
        los ~294 px útiles a 360 px; en un dispositivo real aguanta, pero con `textScale` o un
        título más largo también rompería. El título pasa a `Expanded` (parte en dos líneas).
      - **`SwitchListTile` dentro de `ZCard`** (gasto "Reembolsable" y taller "Taller interno"):
        el `ZCard` pinta el fondo con un `DecoratedBox`, y el `ListTile` dispara el assert de debug
        *"ListTile background color or ink splashes may be invisible"* → se envuelve en
        `Material(type: MaterialType.transparency)` (mismo patrón que el GPS y el seguimiento).
- [x] **Formularios de catálogo y listados a 360 px** (`test/catalog_forms_mobile_test.dart` y
      `test/list_pages_mobile_test.dart`): producto, cliente, proveedor, bodega, sucursal y empleado
      (6) más ventas, productos, clientes, proveedores, empleados, compras, cotizaciones, créditos,
      asientos contables y cajas (10) **pasan sin tocar código** — el trabajo de las fases 1/2/3
      (tarjetas de `ZDataTable`, `ZPageHeader`, `ResponsiveFormRow`, `isExpanded` en los dropdowns)
      ya cubría esas pantallas. El test assertea además que el cuerpo se montó (≥3 campos), para no
      dar por bueno un estado de carga vacío.
- [x] **Formularios transaccionales a 360 px** (`test/commercial_forms_mobile_test.dart`, 10 casos):
      venta, cotización, compra, orden de compra, solicitud de cotización, transferencia entre
      bodegas, garantía, permiso, vacación y pago a prestador. Cuatro defectos reales:
      - **"Productos (n)" + "Agregar producto"** en orden de compra, solicitud de cotización y
        transferencia: `Row(spaceBetween)` → **47 px** de desborde. Pasa a `Wrap` con
        `alignment: spaceBetween`, así en teléfono el botón baja a la línea siguiente (en
        escritorio se ve igual, una sola línea).
      - **Período del pago a prestador** (año + mes + quincena): los tres `Expanded` dejaban al
        `SegmentedButton` de "1ª/2ª quincena" ~92 px y desbordaba **8.9 px**. Se reemplaza el `Row`
        por `ResponsiveFormRow` (apila en teléfono, misma fila desde 600 px).
      - **`_lineRow`** (línea / recaudo / contrato / comisión % / cálculo / borrar) tenía tres
        `SizedBox` fijos de 130/90/110 px: **104 px** de desborde. Con `LayoutBuilder` por debajo de
        600 px los campos van en columna y la comisión calculada comparte la última fila con el
        botón de borrar; desde 600 px se mantiene la fila original.
      - **`_estRow`** (etiqueta + monto): la etiqueta no era flexible y "Total acumulado (bono +
        comisiones)" desbordaba 33/19/4.9 px según el ancho → `Expanded` + separación de 12 px.
- [x] **Helpers de test nuevos**:
      - `test/helpers/fake_api.dart`: `FakeApi` (adapter de Dio falso que registra los requests,
        `FakeApi.routes(routes, {fallback})`) + `FakeApiStorage` (token en memoria) + `jsonResponse`.
        Necesario porque el interceptor de `DioClient` pide el token a `flutter_secure_storage`, que
        no existe en widget tests: el request se queda esperando y `pumpAndSettle` expira.
      - `test/helpers/offline_db.dart`: `memoryDb()` = `AppDatabase.forTesting(NativeDatabase.memory())`.
        Las pantallas offline-first (ventas, productos, cotizaciones, garantías…) cuelgan del
        `AppDatabase` real, que abre un archivo con sqflite y nunca completa en tests → la página se
        queda en el spinner. Con la DB en memoria se montan de verdad (el listado de productos se
        reincorporó al barrido gracias a esto).
- [x] **Nómina, presupuestos y webhooks a 360 px** (`test/payroll_budget_pages_mobile_test.dart`,
      10 casos): tres defectos reales y un bug de Riverpod:
      - **`ListTile` dentro de `ZCard` (assert de ink splashes)**: la página de nómina y la de
        sucursales pintaban su `ListTile` con `onTap` sobre un `ZCard` (que pinta el fondo con un
        `DecoratedBox`) → assert de debug *"ListTile background color or ink splashes may be
        invisible"* y, en release, un splash invisible. Se envuelve en
        `Material(type: MaterialType.transparency)` (patrón ya usado en flota, POS y proveedor).
      - **Tarjetas de resumen de la nómina desbordaban 92 px y 43 px**: `_SummaryCard` pintaba
        `Row(Avatar + Column(texto))` **sin `Expanded`**; la columna tomaba el ancho natural del
        texto y la fila no cabía en los ~108 px útiles de media pantalla. Ahora la columna va en
        `Expanded` con `maxLines: 1` + ellipsis, y en teléfono las **dos tarjetas se apilan**
        (`ResponsiveBuilder`), porque a 360 px una `ZCard` con padding 24 deja ~108 px de texto.
      - **`_summaryRow` de "Presupuesto vs real" desbordaba 36 px**: etiqueta y monto iban en un
        `Row(spaceBetween)`; la etiqueta pasa a `Expanded` + ellipsis y el monto conserva su ancho.
      - **Bug de Riverpod (no era de layout)**: `BudgetVsActualPage.initState` llamaba
        `loadReport()` directo → *"Tried to modify a provider while the widget tree was building"*.
        La carga va en `WidgetsBinding.instance.addPostFrameCallback` (convención del proyecto).
- [x] **Drift test del patrón `ZCard` + `ListTile`** (`test/zcard_list_tile_drift_test.dart`): escanea
      `lib/**/*.dart` y falla si un `ListTile` con `onTap` cuelga de un `ZCard` sin un `Material`
      intermedio (mismo espíritu que `permissions_catalog_drift_test.dart`, que lee el catálogo C#).
      Sale del barrido anterior: **8 páginas** tenían el assert latente (nómina, sucursales,
      estado de cuenta del cliente ×2, dashboard, detalle de permiso, ajustes de compañía,
      inventario y talleres). El segundo test del archivo monta un `ZCard` con `ListTile` y
      comprueba en runtime que no salta la excepción.
- [x] **Contabilidad, tesorería y reportes a 360 px**
      (`test/accounting_treasury_pages_mobile_test.dart`, 16 casos): catálogo de cuentas,
      vínculos, períodos contables, años fiscales, balance de prueba, estado de resultados,
      cambios en el patrimonio, comparativos, dashboard de tesorería, depósito / comisión /
      transferencia / cobranza bancaria, emisión de cheques, centro de reportes y bitácora de
      auditoría. Tres defectos, **uno de ellos rompía la pantalla en cualquier ancho**:
      - **`ZButton` dentro de `Row` reventaba el layout**: `trial_balance_page` y
        `income_statement_page` ponen el botón "Consultar" en la fila de filtros. `ZButton` es
        `fullWidth: true` por defecto → `SizedBox(width: double.infinity)`, y en un `Row` el ancho
        es ilimitado → *"RenderBox was not laid out"* / `hasSize` en cuanto se elegía un período
        (no era un problema de móvil: pasaba también en escritorio). Ahora llevan
        `fullWidth: false`.
      - **`_row` del estado de resultados desbordaba 33 px**: `Row(spaceBetween)` con la etiqueta y
        el monto; la etiqueta pasa a `Expanded` + ellipsis (mismo patrón que `_summaryRow` del
        presupuesto vs real).
      - **Centro de reportes con `GridView.count(childAspectRatio: 1.5)` desbordaba 70–93 px**:
        a 360 px cada tarjeta mide ~158 × 105 px y el contenido (icono 40 + etiqueta, más el
        padding 24 del `ZCard` y el 16 interno) necesita ~147 px. Se sustituye por `ZStatGrid`
        (`Wrap` con alto intrínseco en teléfono, `GridView` con la relación 1.5 desde 600 px)
        dentro de un `SingleChildScrollView`.
      - **Los reportes contables se montan con datos**: el test selecciona el período en el
        `ZSelect` y pulsa "Consultar" para que la tabla (`ZDataTable` → tarjetas en teléfono) y las
        filas de totales se midan de verdad.
- [x] **RRHH, CRM, conciliaciones, documentos y asistencia a 360 px**
      (`test/hr_crm_pages_mobile_test.dart`, 13 casos): listados y formularios de vacaciones,
      recibos de vacaciones, permisos, incapacidades, pipeline de CRM (Kanban = lista vertical
      por etapa en teléfono), conciliaciones, centro documental, historial y marcación de
      asistencia. Dos defectos reales, ambos en el centro documental:
      - **Encabezado del centro documental desbordaba 330 px**: un solo `Row` con el título
        (`titleLarge`), el `Spacer`, 2 `IconButton` y el `FilledButton.icon` "Nueva Plantilla".
        En teléfono el título va en `Expanded` + ellipsis junto a los iconos y el botón baja a su
        propia línea a todo lo ancho (`LayoutBuilder` + `ZBreakpoints.phone`).
      - **Tarjeta de plantilla desbordaba 112 px**: `Row` con icono 48 + textos + `Wrap` de
        acciones ("Generar" + 2 iconos). Las acciones son contenido no flexible, así que la
        `Expanded` de los textos quedaba sin ancho y el `Row` desbordaba; en teléfono las acciones
        bajan a su propia línea (`Column`).
      - **Fixtures que respetan los modelos**: `Opportunity.fromJson` exige `createdAt`
        (`DateTime.parse` sin fallback) y `loadTemplates` lee `total` (no `totalCount`) — sin esas
        claves la pantalla caía en su estado de error y el test solo habría medido el mensaje.
- [x] **PalmTrack, objetivos, garantías y motor documental a 360 px**
      (`test/palmtrack_goals_pages_mobile_test.dart`, 17 casos): las 10 pantallas de PalmTrack
      (fincas, lotes, productores, inventario, producción, ventas, conflictos, sincronización,
      ajustes y perfil SSO), los 3 de objetivos (dashboard, "mis objetivos" y configurador),
      el dashboard y el formulario de garantías, el asistente de generación rápida y el editor de
      plantillas. Tres defectos de layout y **tres bugs reales**:
      - **Chips en `Row` (fincas 41 px, sincronización 198/118 px)**: los chips son contenido no
        flexible; pasan a `Wrap(spacing: 8, runSpacing: 4)`. El encabezado de sincronización pone
        el título en `Expanded` y la fecha en `Flexible` con `textAlign: end`.
      - **`DataTable` nativa en producción (56/84 px)**: migrada a `ZDataTable` con `ZColumn`, así
        que en teléfono se pinta como tarjetas (la nativa no cabe a 360 px).
      - **Paso a paso del asistente (104/78/26 px)**: las tres etiquetas ("Seleccionar",
        "Confirmar", "Listo") en línea no caben; en teléfono se muestran solo los círculos con sus
        conectores y debajo "Paso N de 3 · <etiqueta>".
      - **Editor de plantillas (136 px en el encabezado y 460 px en la barra de herramientas)**: el
        título va en `Expanded` + ellipsis y "Guardar" baja a su propia línea; los 4 botones de
        formato pasan a un `Wrap` bajo la etiqueta.
      - **BUG — "Mis Objetivos" reventaba al abrirla**: leía `stats['percentage'] as double`, pero
        `goalStatsProvider` expone `globalCompliance`/`total`/`active`/`incentiveBudget` → el
        null-cast lanzaba `type 'Null' is not a subtype of type 'double'`. Ahora lee
        `globalCompliance` (num-safe) y la tarjeta se llama "Cumplimiento Global", que es lo que
        realmente muestra.
      - **BUG — perfil SSO de PalmTrack**: `GoRouterState.of(context)` se llamaba en `initState`,
        cuando la dependencia de `ModalRoute` todavía no se puede registrar ("dependOnInheritedWidget
        ofExactType was called before initState completed"). La lectura pasa a
        `didChangeDependencies` con guard (`_claimsLoaded`).
      - **BUG — assert de ink splashes en ajustes de PalmTrack y en el asistente**: los
        `SwitchListTile` de feature flags y el `ExpansionTile` ("O ingresa el UUID manualmente")
        cuelgan de un `ZCard` sin `Material` intermedio. Se envuelven en
        `Material(type: MaterialType.transparency)`.
      - **`zcard_list_tile_drift_test.dart` endurecido**: el escáner solo miraba `ListTile` +
        `onTap`/`onLongPress` en las líneas siguientes, así que se le escapaban `SwitchListTile`
        (`onChanged`) y `ExpansionTile` (construye su propio `ListTile`), igual que un `onTap` en la
        misma línea del constructor. Ahora cubre los tres casos (la subcadena `ListTile(` ya incluye
        `Switch`/`Checkbox`/`Radio`) y el trigger también se busca en la línea del constructor.
      - **Harness**: las páginas de catálogo (garantías) necesitan `appDatabaseProvider` con
        `memoryDb()` (Drift) o `pumpAndSettle` expira esperando el repositorio offline-first, y el
        perfil SSO necesita un `GoRouter` real (`MaterialApp.router`) porque lee `GoRouterState`.
- [x] **Aprobaciones, reportes personalizados, perfil, onboarding, chat, predicciones, garantías,
      años fiscales, kiosco y QR a 360 px** (`test/misc_pages_mobile_test.dart`, 13 casos):
      bandeja y flujos de aprobación, lista / constructor / resultado de reportes personalizados,
      "Mi Perfil", onboarding de compañía, chat, predicción de ventas, seguimiento de garantía,
      años fiscales, escáner QR de asistencia y kiosco. Dos overflows reales:
      - **Predicción de ventas desbordaba 125 px**: `Row(título 20 px, Spacer, IconButton)`. El
        título pasa a `Expanded` con `maxLines: 1` + ellipsis.
      - **Botones del kiosco desbordaban 24 px**: dos `SizedBox(width: 160)` + separador dentro de
        un padding de 24 por lado (336 > 312 útiles a 360 px). Pasan a `Expanded` dentro de un
        `ConstrainedBox(maxWidth: 360)`, así en kiosco horizontal no crecen sin límite.
      - **Hallazgo — el escáner QR sí se puede montar en tests**: `mobile_scanner` degrada a un
        hueco (sin `MissingPluginException` que tumbe el caso), así que el overlay de 250 px y las
        guías se miden a 360/390/430 px sin necesidad de scope-out por cámara.
      - **Harness**: `ApprovalFlowListPage` no dispara `loadFlows()` (lo hace el shell) → se monta
        con un bootstrap `ConsumerStatefulWidget` que lo llama en `initState`. `CustomReportListPage`
        igual, pero ahí el notifier **sí es subclasificable**, así que se siembra con
        `customReportProvider.overrideWith(...)`. `ApprovalNotifier` es `final class` (no se puede
        subclasear) → se usa el notifier real contra `FakeApi`. El kiosco tiene un
        `Timer.periodic` de 1 s: se bombea a mano (`settle: false`) y se desmonta al final para no
        dejar el timer pendiente.
      - **`Override` no lo exporta `flutter_riverpod.dart`** (solo `riverpod`): el helper de montaje
        tipa la lista de overrides como `List<dynamic>` y la esparce.
- [x] **Listados de catálogo, operativos y de flota a 360 px** (`test/catalog_list_pages_mobile_test.dart`
      7 casos + `test/ops_list_pages_mobile_test.dart` 8 + `test/fleet_lists_mobile_test.dart` 10):
      marcas, categorías, centros de costo, tipos de cambio, sucursales, bodegas, talleres; órdenes de
      compra, solicitudes de cotización, transferencias, notas de crédito (cliente y proveedor), pagos a
      proveedores y a prestadores, movimientos de inventario; los 10 listados de flota. Seis overflows
      reales y un caso de legibilidad:
      - **Tipos de cambio (27 px)**: el `subtitle` era `Row(tasa + icono + fecha)`; pasa a `Wrap` con el
        bloque icono+fecha agrupado (la fecha ISO no cabe junto a la tasa a 360 px).
      - **Bodegas**: el `title` era `Row(Flexible(nombre) + 2 Chip)`. No desbordaba, pero con dos chips
        el nombre se encogía a **0 px** (el `Flexible` cede todo el ancho). Pasa a `Wrap`.
      - **Pagos a prestadores (259 px)**: `title: Row(Flexible(nº) + Chip(período) + Chip(meta))`. Los
        chips no son flexibles y el del período ("Quincena 1 · Octubre 2026") no cabe; pasa a `Wrap`.
      - **Rutas y entregas de flota (~60 px)**: el `trailing` era
        `Row(fecha ISO + pill de estado + PopupMenu)`. En teléfono la fecha y el estado bajan a un
        `Wrap` del `subtitle` y el menú queda solo como `trailing`.
      - **Documentos de flota (48 px)**: dos pills + menú en el `trailing`; en teléfono los pills pasan
        al `subtitle` (que ya era `Column`) y el menú queda solo.
      - **Gastos de flota (60 / 251 / 51-82 px, cuatro filas distintas)**: (a) el título de la página
        competía con 3 `IconButton` → `Expanded` + ellipsis y `Spacer` fuera; (b) los 3 `ChoiceChip` de
        filtro iban en `Row` → `Wrap`; (c) el pill de forma de pago salía del `trailing` al `subtitle`
        en teléfono; (d) `_KpiItem` metía `Row(icono + Column(3 textos))` → la columna a `Expanded` +
        ellipsis y la barra de KPIs pasa a **2 + 1 con `Wrap`** en teléfono (cada tarjeta quedaba con
        ~82 px). En escritorio conserva la fila de 3 con `Expanded`.
      - **Patrón para no tocar escritorio**: `final isPhone = MediaQuery.sizeOf(context).width <
        ZBreakpoints.phone;` dentro del `itemBuilder`; los widgets compartidos (`pill`, `menu`) se
        extraen a variables locales y cada uno se inserta en **una sola** rama del ternario.
      - **Harness**: los tres archivos montan la página real contra `FakeApi.routes(...)` a 360/390/430
        px; `find.text` falla cuando el `ListTile` concatena campos en el `subtitle`
        (`'$title · $branchName'`) → se usa `find.textContaining`.
- [x] **Dashboards y paneles a 360 px** (`test/dashboards_mobile_test.dart`, 9 casos):
      `MobileDashboardPage`, `DashboardV2Page`, panel ejecutivo (`dashboard/executive`), dashboard
      ejecutivo y comercial de BI, cartera vencida, inventario, flota y calendario de ausencias. Cuatro
      páginas con defectos reales:
      - **Panel ejecutivo — BUG, no layout (`Null check operator used on a null value`)**: el primer
        frame se construía con `state.loading == false` y `state.kpis == null` (la carga arranca en una
        microtask) y el `ListView` hacía `state.kpis!`. Ahora la rama de carga es
        `state.loading || state.kpis == null`. Nunca se había montado en un test.
      - **Dashboard BI (ejecutivo y comercial) — 40 px por abajo**: la página envolvía los gráficos en
        `SizedBox(height: 200)`, pero `BiLineChart`/`BiBarChart` ya miden 200 + título + 8 + padding
        (24) por dentro → el `Column` del gráfico desbordaba. Se quitan los `SizedBox` externos y el
        gráfico toma su altura natural. Mismo defecto en `SalesPredictionSection`.
      - **Sección de predicción — 69 px a la derecha**: `Row(caja del día 100 px + monto + Spacer +
        rango "$inferior – $superior")`; el rango pasa a `Flexible` con `textAlign: end` y el monto a
        `Expanded` (en escritorio se ve igual, el rango sigue a la derecha).
      - **`MobileDashboardPage` — 111 px a la derecha**: "Acciones Rápidas" era
        `Row(spaceAround, 4 × Column(icono 52 + etiqueta))`; las etiquetas no son flexibles. Cada acción
        pasa a `Expanded` con la etiqueta a 2 líneas + ellipsis.
      - **`MobileDashboardPage` — 5.3 px por abajo**: el `GridView` de KPIs usaba
        `childAspectRatio: 1.6` (celda de ~99 px) y el contenido pedía ~104; ahora `mainAxisExtent: 116`.
      - **Harness**: las páginas con `AutoRefreshMixin` se montan con `height: 4000` (con el alto real
        solo se construirían las primeras secciones) y se desmontan al final para cancelar el
        `Timer.periodic`. El dashboard comercial arrastra tres secciones de IA
        (`SalesPredictionSection`, `PurchaseRecommendationSection`, `ExpenseClassifierSection`) que
        piden por red: si no se les da respuesta quedan en el `CircularProgressIndicator` de carga y
        `pumpAndSettle` no termina nunca (animación infinita) → se sirven por `FakeApi` con payloads
        mínimos.
      - **Inventario es offline-first**: `InventoryDashboardPage` solo `ref.watch(productProvider)`, así
        que se siembra el `AppDatabase` en memoria (`upsertProduct`) con conectividad fija en `offline`
        y un bootstrap que dispara `productProvider.notifier.load()`.

**Aceptación**: dashboard ejecutivo y comercial legibles a 360 px, 2 KPIs por fila, 0 overflows.

---

## 6. Fase 4 — Pantallas operativas (P1)

1. Ventas y cobros: lista, detalle con resumen pegajoso, pago de crédito en 3 pasos.
2. Inventario: transferencias, kardex, escaneo (permiso de cámara).
3. Asistencia / kiosco / QR check-in: permisos + fallback sin cámara.
4. Aprobaciones: bandeja con Aprobar/Rechazar de 48 dp.
5. Formularios que exigen sucursal: el aviso debe **abrir el selector**.
6. Flota: GPS a pantalla completa con bottom sheet de parada, entregas y hoja de ruta
   (alertas, predictivo, seguimiento, reportes y GPS ya barridos a 360 px — ver Fase 3).

**Aceptación**: cada flujo completo en 390 px con una sola mano.

### 6.1 Carrito de línea adaptativo ✅

- [x] `ZCartLine` en el DS (`shared/ds/components/z_cart_line.dart`): `title`, `quantity`,
      `extra?`, `total`, `onRemove`. En teléfono el título y el borrar van en la primera fila y
      cantidad/descuento/total en la segunda; desde 600 px comparten una sola fila.
- [x] Migrados **venta (POS)** y **cotización**. En ventas los cuatro elementos en una fila dejaban
      el nombre del producto en ~100 px (3 líneas); en cotización la fila completa medía ~324 px
      fijos y desbordaba a 360 px.
- [x] **`IntrinsicHeight` interno**: un `TextField` en la línea (cantidad, descuento) se estiraba a
      todo el alto disponible cuando la tarjeta vive en un contenedor de alto acotado y el campo
      aparecía centrado a media pantalla.
- [x] **Compras se deja como está**: su línea ya apila tres campos con `ResponsiveFormRow` y no
      desborda.
- [x] Tests: `test/z_cart_line_test.dart` (6: dos filas en teléfono, una en escritorio, sin
      desborde en 360/390/430, con control extra, `onRemove` y nombre muy largo).

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

- [x] `test/helpers/viewport.dart` con `pumpAt(360/390/430)`: `pumpAt` fija `devicePixelRatio`
      en 1 (para que `physicalSize` sea el tamaño lógico), restaura la vista con
      `addTearDown(tester.view.reset)` y `pumpAcrossPhones` recorre los tres anchos fallando con
      el ancho en el mensaje. Un `RenderFlex overflow` ya hace fallar el test automáticamente.
- [x] Paginas completas a 360/390/430 px: `test/mobile_pages_smoke_test.dart` (invitación,
      reportes personalizados, garantías y asistente IA) y casos de 360 px en roles, usuarios,
      planes de suscripción y dashboard.
- [x] **Dropdowns**: los 65 `DropdownButtonFormField` que no tenían `isExpanded: true` lo ganaron
      (`z_select` incluido). Sin él, el texto del valor seleccionado ("Administrador de Empresa")
      se pintaba a ancho intrínseco y **desbordaba 108 px** en un campo de 280 px en teléfono.
      Verificado antes del cambio que ninguno es hijo directo de un `Row`/`Wrap` (todos viven en
      `Column`/`Expanded`/`ListView`/`SizedBox`), así que `isExpanded` no puede recibir ancho
      infinito.
- [ ] Golden de 5 pantallas × 3 anchos.
- [x] **GPS de flota y POS montados a 360/390/430 px** (`fleet_pages_mobile_test.dart` y
      `pos_mobile_test.dart`) — ver el barrido de Fase 3.
- [x] **Formularios y listados montados a 360/390/430 px** (`fleet_forms_mobile_test.dart`,
      `catalog_forms_mobile_test.dart`, `commercial_forms_mobile_test.dart` y
      `list_pages_mobile_test.dart`): 10 + 6 + 10 + 10 pantallas — ver el barrido de Fase 3.
- [x] **Nómina, presupuestos y webhooks montados a 360/390/430 px**
      (`payroll_budget_pages_mobile_test.dart`, 10 casos) y **detalle de 23+ pantallas**
      (`detail_pages_mobile_test.dart`) — ver el barrido de Fase 3.
- [x] **Drift del patrón `ZCard` + `ListTile`** (`zcard_list_tile_drift_test.dart`): escanea
      `lib/**/*.dart` y falla si un `ListTile` con `onTap` queda sin `Material` dentro de un `ZCard`
      (el `DecoratedBox` del `ZCard` escondería el ink splash y Flutter lanzaría el assert). Cubre
      también `SwitchListTile`/`CheckboxListTile`/`RadioListTile` (`onChanged`) y `ExpansionTile`
      (construye su propio `ListTile` con `onTap`), y detecta el trigger en la misma línea del
      constructor.
- [x] **Contabilidad, tesorería y reportes montados a 360/390/430 px**
      (`accounting_treasury_pages_mobile_test.dart`, 16 casos, con los reportes consultados con
      datos) — ver el barrido de Fase 3.
- [x] **RRHH, CRM, conciliaciones, documentos y asistencia montados a 360/390/430 px**
      (`hr_crm_pages_mobile_test.dart`, 13 casos) — ver el barrido de Fase 3.
- [x] **PalmTrack, objetivos, garantías y motor documental montados a 360/390/430 px**
      (`palmtrack_goals_pages_mobile_test.dart`, 17 casos) — ver el barrido de Fase 3.
- [ ] Barrido del resto de rutas top a 360 px (liquidación, aprobaciones, kiosco/QR, asistencia
      restante, reportes personalizados, fiscal, predicciones, perfil, onboarding, chat).
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
