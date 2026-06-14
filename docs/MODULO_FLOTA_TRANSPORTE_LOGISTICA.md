# Módulo de Gestión de Flotas, Transporte y Logística

> **Documento de Especificación Técnica y Funcional — Zorvian ERP**
> Versión 1.0 — Junio 2026
> Clasificación: **Interno / Cliente**

---

## Índice

1. [Resumen Ejecutivo](#1-resumen-ejecutivo)
2. [Alcance General del Módulo](#2-alcance-general-del-módulo)
3. [Estructura del Menú Lateral](#3-estructura-del-menú-lateral)
4. [Dashboard Ejecutivo](#4-dashboard-ejecutivo)
5. [Gestión de Vehículos](#5-gestión-de-vehículos)
6. [Gestión de Conductores](#6-gestión-de-conductores)
7. [Planificación de Rutas](#7-planificación-de-rutas)
8. [Gestión de Entregas](#8-gestión-de-entregas)
9. [Control de Combustible](#9-control-de-combustible)
10. [Mantenimiento Preventivo](#10-mantenimiento-preventivo)
11. [Mantenimiento Correctivo](#11-mantenimiento-correctivo)
12. [Gestión de Documentos](#12-gestión-de-documentos)
13. [GPS y Monitoreo](#13-gps-y-monitoreo)
14. [Control de Gastos](#14-control-de-gastos)
15. [Reportes Empresariales](#15-reportes-empresariales)
16. [Automatizaciones](#16-automatizaciones)
17. [Integración con Otros Módulos](#17-integración-con-otros-módulos)
18. [Diseño UX/UI](#18-diseño-uxui)
19. [Modelo de Base de Datos](#19-modelo-de-base-de-datos)
20. [Permisos y Seguridad](#20-permisos-y-seguridad)
21. [Escalabilidad](#21-escalabilidad)
22. [Análisis Competitivo](#22-análisis-competitivo)
23. [Roadmap de Implementación](#23-roadmap-de-implementación)
24. [Inteligencia Artificial](#24-inteligencia-artificial)
25. [Documento Final](#25-documento-final)

---

# 1. Resumen Ejecutivo

## Qué es el módulo

El **Módulo de Gestión de Flotas, Transporte y Logística** de Zorvian ERP es una solución integral para la administración del ciclo de vida completo de vehículos, conductores, rutas, entregas, combustible, mantenimiento, documentación regulatoria y costos operativos. Está diseñado como un subsistema nativo dentro de la arquitectura modular de Zorvian ERP, compartiendo identidad visual, lógica de permisos, motor de base de datos, sistema de notificaciones y flujos de integración con los módulos existentes de CRM, Ventas, Inventario, Compras, RRHH, Nómina, Contabilidad y Activos Fijos.

## Problemas que resuelve

| Problema | Solución Zorvian |
|---|---|
| Flota dispersa sin control centralizado | Repositorio único de vehículos, conductores, documentos y costos |
| Costos operativos no trazables | Registro granular de combustible, mantenimiento, viáticos, peajes, multas |
| Mantenimiento reactivo y costoso | Programación preventiva por fecha, kilometraje y horómetro con alertas |
| Entregas sin visibilidad en tiempo real | Flujo completo desde venta → preparación → ruta → entrega → firma digital |
| Conductores sin historial unificado | Perfil completo con licencias, infracciones, capacitaciones y desempeño |
| Documentación regulatoria vencida | Alertas automáticas de vencimientos de seguros, circulación, permisos |
| Desconexión entre módulos del ERP | Integración nativa con 8+ módulos existentes sin adaptadores |
| Falta de inteligencia de negocio | Dashboard ejecutivo con 14 KPIs, 4 gráficos dinámicos y reportes gerenciales |

## Beneficios empresariales

- **Reducción de costos operativos** del 15–25% mediante control de combustible, mantenimiento preventivo y optimización de rutas
- **Aumento de productividad** del 20–30% vía asignación inteligente de conductores y planificación logística
- **Cumplimiento regulatorio** automatizado con alertas de vencimiento documental
- **Visibilidad 360°** de cada vehículo, conductor, entrega y costo desde un solo panel
- **Trazabilidad completa** de cada evento: desde la orden de compra de combustible hasta el asiento contable

## Beneficios operativos

- Planificación centralizada de rutas con asignación automática por capacidad, ubicación y disponibilidad
- Mantenimiento predictivo que reduce el tiempo fuera de servicio en un 40%
- Captura de evidencias digitales (firma, foto, GPS) en cada entrega
- Geolocalización en tiempo real con geocercas y alertas
- Rendimiento por conductor y por vehículo con detección de anomalías

## Beneficios financieros

- Costo por kilómetro trazable y reportable
- Presupuestos de flota vs. ejecución real
- Integración contable automática de todos los gastos operativos
- Depreciación de activos (vehículos) sincronizada con Activos Fijos
- Viáticos y bonificaciones de conductores integrados con Nómina

## Impacto estratégico

El módulo posiciona a Zorvian ERP como un competidor directo de soluciones especializadas como Fleetio, Odoo Fleet y SAP TM, pero con la ventaja de ser un módulo nativo dentro de un ERP completo. Esto elimina la necesidad de integrar herramientas de terceros, reduce la fricción operativa y ofrece una propuesta de valor diferenciada para empresas de logística, distribución, mensajería, servicios de campo, ventas móviles y operaciones con flota propia.

---

# 2. Alcance General del Módulo

## Gestión de Flota

### Vehículos livianos
- Automóviles ejecutivos y de representación
- Vehículos de ventas y visita comercial
- Unidades de supervisión

### Camiones y vehículos pesados
- Camiones de carga (liviana, mediana, pesada)
- Camiones refrigerados
- Volquetes y dumpers
- Tractocamiones y remolques
- Furgonetas y vans de reparto

### Motocicletas
- Motos de mensajería y reparto urbano
- Motos de supervisión

### Maquinaria móvil
- Montacargas y forklifts
- Grúas móviles
- Plataformas elevadoras
- Equipos de construcción móviles

### Equipos móviles
- Remolques
- Semirremolques
- Contenedores
- Cisternas

## Transporte

### Distribución
- Distribución primaria (centro → bodegas regionales)
- Distribución secundaria (bodega → punto de venta)
- Distribución urbana y metropolitana
- Distribución rural y larga distancia

### Reparto
- Reparto de última milla
- Mensajería y paquetería
- Reparto programado (suscripciones, recurrente)

### Entregas
- Entrega directa al cliente
- Entrega en punto de recogida
- Entrega con instalación y montaje
- Contra entrega con cobro

### Recolecciones
- Recolección de mercancía de proveedores
- Recolección de devoluciones
- Recolección de material reciclable o retorno

## Logística

### Planificación
- Planificación semanal y diaria de rutas
- Consolidación de cargas
- Ventanas horarias de entrega
- Restricciones de circulación (horarias, ambientales, peso)

### Rutas
- Optimización por distancia, tiempo y costo
- Rutas estáticas (predecibles) y dinámicas (bajo demanda)
- Múltiples puntos de entrega por ruta
- Replanificación en tiempo real

### Seguimiento
- Tracking en vivo de cada unidad
- Estado de cada entrega
- Notificaciones al cliente (WhatsApp, correo, SMS)
- ETA (Estimated Time of Arrival) dinámico

### Control de costos
- Costo por ruta individual
- Costo por kilómetro (promedio global y por vehículo)
- Costo por entrega
- Costo por hora operativa
- Margen logístico por ruta y cliente

---

# 3. Estructura del Menú Lateral

El módulo se integra como un nuevo `NavModule` en la navegación lateral de Zorvian ERP, dentro del grupo **OPERACIONES**, con color distintivo **Naranja Logístico** (`#FF6D00`).

```dart
const NavModule(
  id: 'flota',
  label: 'Flota y Logística',
  icon: Icons.local_shipping_outlined,
  color: Color(0xFFFF6D00),
  group: 'operations',
  children: [
    NavItem(id: 'flota-dashboard', label: 'Dashboard', icon: Icons.dashboard_outlined, route: '/fleet'),
    NavItem(id: 'flota-vehiculos', label: 'Vehículos', icon: Icons.time_to_leave_outlined, route: '/fleet/vehicles'),
    NavItem(id: 'flota-conductores', label: 'Conductores', icon: Icons.person_outline, route: '/fleet/drivers'),
    NavItem(id: 'flota-rutas', label: 'Rutas', icon: Icons.alt_route_outlined, route: '/fleet/routes'),
    NavItem(id: 'flota-entregas', label: 'Entregas', icon: Icons.inventory_2_outlined, route: '/fleet/deliveries'),
    NavItem(id: 'flota-viajes', label: 'Viajes', icon: Icons.trip_outlined, route: '/fleet/trips'),
    NavItem(id: 'flota-combustible', label: 'Combustible', icon: Icons.local_gas_station_outlined, route: '/fleet/fuel'),
    NavItem(id: 'flota-mantenimientos', label: 'Mantenimientos', icon: Icons.build_outlined, route: '/fleet/maintenance'),
    NavItem(id: 'flota-taller', label: 'Taller', icon: Icons.precision_manufacturing_outlined, route: '/fleet/workshop'),
    NavItem(id: 'flota-documentos', label: 'Documentos', icon: Icons.description_outlined, route: '/fleet/documents'),
    NavItem(id: 'flota-gps', label: 'Monitoreo GPS', icon: Icons.near_me_outlined, route: '/fleet/gps'),
    NavItem(id: 'flota-gastos', label: 'Gastos', icon: Icons.account_balance_wallet_outlined, route: '/fleet/expenses'),
    NavItem(id: 'flota-reportes', label: 'Reportes', icon: Icons.analytics_outlined, route: '/fleet/reports'),
    NavItem(id: 'flota-config', label: 'Configuración', icon: Icons.settings_outlined, route: '/fleet/settings'),
  ],
)
```

### Función de cada opción

| Opción | Función |
|---|---|
| **Dashboard** | Panel ejecutivo con KPIs, gráficos, alertas y resumen operativo del día |
| **Vehículos** | CRUD completo de vehículos con datos generales, operativos, documentación y estado |
| **Conductores** | Perfiles de conductores, licencias, categorías, infracciones, capacitaciones e historial |
| **Rutas** | Planificación, optimización y asignación de rutas con mapa interactivo |
| **Entregas** | Flujo completo de entregas con preparación, carga, ruta, confirmación y evidencias |
| **Viajes** | Registro y seguimiento de viajes individuales con origen, destino, tiempos y costos |
| **Combustible** | Control de abastecimientos, rendimiento por vehículo/conductor y detección de anomalías |
| **Mantenimientos** | Programación preventiva por fecha, kilometraje y horómetro con alertas |
| **Taller** | Órdenes de trabajo correctivas, diagnóstico, repuestos, mano de obra y aprobaciones |
| **Documentos** | Gestión de seguros, circulación, licencias, permisos con alertas de vencimiento |
| **Monitoreo GPS** | Mapa en vivo, historial de ubicaciones, geocercas y alertas de geolocalización |
| **Gastos** | Registro de todos los gastos operativos (combustible, peajes, viáticos, multas, parqueos) |
| **Reportes** | Reportes operativos, financieros y gerenciales con exportación PDF/Excel |
| **Configuración** | Parámetros globales del módulo, tipos de vehículo, categorías de gasto, talleres externos |

---

# 4. Dashboard Ejecutivo

## Diseño

Dashboard de una sola vista (scroll vertical) con layout de cuadrícula responsive:

- **Fila 1**: 4 KPI Cards grandes (Vehículos Activos, Entregas Hoy, Combustible, Costo/km)
- **Fila 2**: 4 KPI Cards secundarios (Disponibles, En Mantenimiento, Pendientes, Completadas)
- **Fila 3**: Gráfico Costos por Mes (barra) + Gráfico Consumo por Vehículo (barra horizontal)
- **Fila 4**: Gráfico Entregas por Zona (pastel/donut) + Tabla "Próximos Mantenimientos"
- **Fila 5**: Mapa de calor de entregas + Alerta de vencimientos documentales

## KPIs

| KPI | Fórmula | Tipo | Destacado |
|---|---|---|---|
| Vehículos Activos | COUNT WHERE estado = 'Activo' | Número | Primario |
| Vehículos Disponibles | COUNT WHERE estado = 'Disponible' | Número | Secundario |
| Vehículos en Mantenimiento | COUNT WHERE estado = 'Mantenimiento' | Número | Secundario |
| Entregas del Día | COUNT WHERE fecha = HOY | Número | Primario |
| Entregas Pendientes | COUNT WHERE estado = 'Asignado' | Número | Secundario |
| Entregas Completadas | COUNT WHERE estado = 'Entregado' Y fecha = HOY | Número | Secundario |
| Consumo Combustible (Hoy) | SUM(litros) WHERE fecha = HOY | Decimal (L) | Primario |
| Gastos Operativos (Mes) | SUM(monto) WHERE mes = ACTUAL | Moneda | Secundario |
| Costo por Kilómetro | SUM(gastos totales) / SUM(km recorridos) | Moneda | Primario |
| Rendimiento Promedio | SUM(km recorridos) / SUM(litros consumidos) | Decimal (km/L) | Secundario |
| Órdenes de Trabajo Abiertas | COUNT WHERE tipo = 'Correctivo' Y estado != 'Cerrado' | Número | Secundario |
| Alertas de Vencimiento | COUNT WHERE fecha_vencimiento < HOY+30 | Número (rojo si >0) | Crítico |

## Gráficos

### Costos por Mes
- Tipo: Barra vertical agrupada
- Eje X: Meses (Ene–Dic)
- Eje Y: Monto
- Series: Combustible, Mantenimiento, Taller, Viáticos, Peajes, Multas
- Interacción: Tooltip con desglose, click para ver detalle mensual

### Consumo por Vehículo (Top 10)
- Tipo: Barra horizontal
- Eje Y: Vehículo (placa + marca)
- Eje X: Litros consumidos
- Orden: Descendente
- Color: Escala gradiente naranja (menor consumo = más claro)

### Entregas por Zona
- Tipo: Donut / Pastel
- Segmentos: Zonas geográficas configuradas
- Tooltip: Nombre zona, cantidad entregas, % del total
- Interacción: Click para filtrar dashboard por zona

### Rentabilidad Logística
- Tipo: Línea + barra combinado
- Barras: Ingresos por flete / Costos logísticos
- Línea: Margen %
- Eje X: Meses
- Umbral: Línea horizontal en 0% (pérdida/ganancia)

---

# 5. Gestión de Vehículos

## Datos Generales

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| Código interno | String (20) | Sí | Código único auto-generado o manual |
| Placa | String (15) | Sí | Placa de circulación |
| Marca | FK → brand | Sí | Marca del vehículo (catálogo) |
| Modelo | String (50) | Sí | Modelo específico |
| Año | Integer (4) | Sí | Año de fabricación |
| VIN | String (17) | No | Número de identificación vehicular |
| Número de Motor | String (30) | No | Número de motor |
| Número de Chasis | String (30) | No | Número de chasis |
| Color | String (30) | No | Color predominante |
| Tipo | FK → vehicle_type | Sí | Tipo de vehículo (liviano, pesado, moto, etc.) |
| Combustible | FK → fuel_type | Sí | Tipo de combustible (gasolina, diésel, eléctrico, híbrido) |
| Sucursal | FK → branch | Sí | Sucursal a la que pertenece |

## Datos Operativos

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| Kilometraje Actual | Decimal (10,2) | Sí | Km actuales del odómetro |
| Kilometraje Anterior | Decimal (10,2) | Sí | Km en el último registro |
| Horómetro Actual | Decimal (10,2) | No | Horas de operación actuales |
| Capacidad de Carga (kg) | Decimal (10,2) | Sí | Peso máximo de carga |
| Capacidad Volumétrica (m³) | Decimal (10,2) | No | Volumen máximo de carga |
| Capacidad Pasajeros | Integer | No | Número de pasajeros |
| Estado | Enum | Sí | Activo, Disponible, En Ruta, Mantenimiento, Fuera de Servicio, Vendido, Baja |
| Asignado a | FK → driver | No | Conductor asignado actualmente |
| GPS Device ID | String (50) | No | ID del dispositivo GPS instalado |

## Documentación del Vehículo

| Documento | Requerido | Alerta Vencimiento | Días Anticipación |
|---|---|---|---|
| Tarjeta de Circulación | Sí | Sí | 30 |
| Póliza de Seguro | Sí | Sí | 45 |
| Revisión Técnica Mecánica | Sí | Sí | 30 |
| Permiso de Carga | Según tipo | Sí | 30 |
| Permiso Ambiental | Según tipo | Sí | 30 |
| Certificado de Propiedad | Sí | No | — |

## Estados del Vehículo

```
Activo ──→ Disponible ──→ En Ruta ──→ Disponible
  │                        │
  └──→ Mantenimiento ←─────┘
  │
  └──→ Fuera de Servicio
  │
  └──→ Vendido / Baja
```

## UI: Lista de Vehículos

- Tabla con columnas: Código, Placa, Marca/Modelo, Año, Estado, Conductor, Kilometraje, Sucursal
- Filtros: Estado, Tipo, Marca, Sucursal, Búsqueda (placa/marca/VIN)
- Acciones rápidas: Ver, Editar, Duplicar, Asignar Conductor, Generar OT, Ver Historial
- Badge de color por estado (verde=Activo, azul=Disponible, naranja=En Ruta, rojo=Mantenimiento, gris=Fuera Servicio)
- Tarjeta resumen al seleccionar fila (documentos próximos a vencer, último mantenimiento, promedio consumo)

## UI: Formulario de Vehículo

- Diseño en tabs: General, Operativo, Documentos, GPS, Costos, Adjuntos
- Formularios con validación en tiempo real
- Selectores con búsqueda (marca, tipo, sucursal)
- Sección de documentos con upload de archivos y fechas de vencimiento
- Timeline de eventos del vehículo en la vista de detalle

---

# 6. Gestión de Conductores

## Perfil del Conductor

| Campo | Tipo | Requerido |
|---|---|---|
| Nombre completo | String (100) | Sí |
| Identificación (DNI/RUC) | String (20) | Sí |
| Fecha de nacimiento | Date | Sí |
| Teléfono | String (20) | Sí |
| Correo electrónico | String (100) | Sí |
| Dirección | String (200) | No |
| Tipo de licencia | FK → license_category | Sí |
| Número de licencia | String (30) | Sí |
| Fecha de expedición licencia | Date | Sí |
| Fecha de vencimiento licencia | Date | Sí |
| Categorías adicionales | String (100) | No |
| Fecha de ingreso | Date | Sí |
| Estado | Enum | Activo, Suspensión, Vacaciones, Inactivo, Despedido |
| Sucursal | FK → branch | Sí |
| Empleado relacionado | FK → employee | No |
| Foto | Image | No |

## Historial del Conductor

- Infracciones (multas, puntos, fecha, monto, estado)
- Capacitaciones (curso, fecha, institución, vigencia)
- Accidentes (fecha, gravedad, descripción, costo)
- Evaluaciones (desempeño, puntuación 1–5, período)
- Historial de viajes (cantidad, km totales, horas)
- Rendimiento de combustible promedio por conductor

## Automatizaciones de Conductores

| Evento | Disparador | Acción |
|---|---|---|
| Licencia próxima a vencer | Fecha vencimiento ≤ 30 días | Alerta en dashboard + notificación push + correo a RH |
| Licencia vencida | Fecha vencimiento < HOY | Bloqueo de asignación a rutas + notificación a supervisor |
| Capacitación requerida | Categoría requiere curso específico | Recordatorio semanal hasta completar |
| Infracción registrada | Nuevo registro de infracción | Notificación a supervisor + RH |
| Aniversario de ingreso | Fecha ingreso + 1 año | Reconocimiento automático |

## UI: Lista de Conductores

- Tabla: Nombre, Identificación, Licencia, Vencimiento Licencia (con badge), Estado, Teléfono, Sucursal
- Badge de color para estado de licencia: verde (vigente), amarillo (próximo a vencer), rojo (vencido)
- Filtros: Estado, Tipo licencia, Sucursal, Búsqueda
- Acciones: Ver perfil, Editar, Asignar Vehículo, Ver Viajes, Desactivar

---

# 7. Planificación de Rutas

## Estructura de Ruta

| Campo | Tipo | Descripción |
|---|---|---|
| Código de ruta | String (20) | Código único |
| Nombre | String (100) | Nombre descriptivo |
| Tipo | Enum | Urbana, Metropolitana, Regional, Nacional, Internacional |
| Fecha programada | Date | Fecha de ejecución |
| Hora salida estimada | Time | Hora de inicio planificada |
| Hora regreso estimada | Time | Hora de finalización planificada |
| Origen | FK → branch / Address | Punto de partida |
| Destino principal | FK → Address | Destino final |
| Distancia estimada (km) | Decimal | Km total estimados |
| Duración estimada (min) | Integer | Tiempo total estimado |
| Vehículo asignado | FK → vehicle | Vehículo de la ruta |
| Conductor asignado | FK → driver | Conductor responsable |
| Copiloto/Ayudante | FK → driver | Segundo tripulante (opcional) |
| Estado | Enum | Planificada, Asignada, En Curso, Completada, Cancelada |
| Costo estimado | Decimal | Costo planificado total |
| Notas | Text | Instrucciones especiales |

## Puntos de Ruta (Waypoints)

| Campo | Tipo | Descripción |
|---|---|---|
| Orden | Integer | Secuencia del punto |
| Tipo | Enum | Carga, Entrega, Recolección, Descanso, Peaje, Combustible |
| Dirección | FK → Address | Ubicación del punto |
| Cliente | FK → client | Cliente asociado (para entregas/recolecciones) |
| Venta/Orden | FK → sale/purchase | Documento origen |
| Ventana inicio | Time | Hora más temprana permitida |
| Ventana fin | Time | Hora más tardía permitida |
| Tiempo estimado | Integer | Minutos estimados en el punto |
| Distancia desde anterior | Decimal | Km desde el punto anterior |
| Instrucciones | Text | Instrucciones para el conductor |

## Capacidades de Planificación

### Optimización de Rutas
- Algoritmo de optimización por distancia más corta
- Algoritmo por menor tiempo (considerando tráfico histórico)
- Algoritmo por menor costo (peajes + combustible + horas conductor)
- Optimización multi-objetivo ponderada

### Agrupación de Entregas
- Por zona geográfica (automática mediante geocodificación)
- Por cliente (múltiples entregas al mismo cliente)
- Por tipo de entrega (urgente, programada, regular)
- Por ventana horaria del cliente

### Asignación Automática
- Por capacidad del vehículo (peso + volumen)
- Por disponibilidad del conductor (horas trabajadas, descanso)
- Por competencia del conductor (tipo de licencia requerida)
- Por ubicación actual (desde último viaje o GPS)

## UI: Planificador de Rutas

- **Calendar view**: Vista semanal con rutas programadas por día
- **Mapa interactivo**: Visualización de rutas en mapa con cada waypoint numerado
- **Drag & drop de entregas**: Asignar entregas a rutas arrastrando desde lista de pendientes
- **Panel de optimización**: "Optimizar ruta" con selección de criterio (distancia/tiempo/costo)
- **Indicador de carga**: Barra de progreso de capacidad (peso/volumen) del vehículo
- **Timeline**: Línea de tiempo vertical con cada punto de ruta, hora estimada y estado

---

# 8. Gestión de Entregas

## Flujo Completo de Entrega

```
Venta / Orden
    │
    ▼
Preparación (Picking)
    │
    ▼
Carga (Asignación a ruta/vehículo)
    │
    ▼
Salida (Inicio de ruta)
    │
    ▼
Tránsito (Seguimiento GPS + actualizaciones)
    │
    ▼
Llegada a destino (Geocerca detecta arribo)
    │
    ▼
Entrega (Descarga + verificación)
    │
    ▼
Confirmación (Firma digital + fotografía)
    │
    ▼
Cierre (Actualización de estado + contabilidad)
```

## Estructura de Entrega

| Campo | Tipo | Descripción |
|---|---|---|
| Código de entrega | String (20) | Código único auto-generado |
| Venta / Origen | FK → sale/purchase | Documento que origina la entrega |
| Cliente | FK → client | Cliente destino |
| Dirección de entrega | FK → address | Dirección específica |
| Fecha programada | Date | Fecha compromiso |
| Ventana horaria | Time range | Horario acordado |
| Ruta asignada | FK → route | Ruta que incluye esta entrega |
| Vehículo | FK → vehicle | Vehículo asignado |
| Conductor | FK → driver | Conductor asignado |
| Estado | Enum | Pendiente, En Preparación, Preparado, Cargado, En Ruta, En Destino, Entregado, Parcial, Devuelto, Cancelado |
| Fecha de entrega real | DateTime | Timestamp de confirmación |
| Persona que recibe | String (100) | Nombre de quien firma |
| Identificación receptor | String (20) | DNI/RUC del receptor |
| Firma digital | Image | Captura de firma |
| Fotografías | Image[] | Evidencias fotográficas |
| Geolocalización entrega | GeoPoint | Coordenadas de la entrega |
| Observaciones | Text | Notas del conductor |
| Documento de respaldo | File | PDF guía/remisión |

## Items de Entrega

| Campo | Tipo | Descripción |
|---|---|---|
| Producto | FK → product | Producto entregado |
| Cantidad solicitada | Decimal | Cantidad en la venta |
| Cantidad entregada | Decimal | Cantidad real entregada |
| Cantidad devuelta | Decimal | Diferencia no aceptada |
| Lote / Serie | String | Trazabilidad |
| Estado item | Enum | Entregado, Pendiente, Devuelto, Dañado, Faltante |

## UI: Gestión de Entregas

### Lista de Entregas
- Tabla: Código, Cliente, Dirección, Fecha, Estado (con badge), Vehículo, Conductor
- Filtros: Estado, Fecha (rango), Cliente, Conductor, Vehículo, Zona
- Vista kanban opcional para operaciones: Pendiente → Preparado → En Ruta → Entregado
- Mapa con marcadores de entregas del día

### Detalle de Entrega
- Timeline visual del flujo
- Sección de evidencias (firma, fotos, GPS)
- Mapa de la ruta desde origen hasta destino
- Items con cantidades
- Historial de cambios de estado

### App Móvil (Conductor)
- Lista de entregas asignadas para el día
- Navegación al siguiente destino
- Captura de firma digital
- Captura de fotografías (cámara integrada)
- Confirmación de entrega (con GPS embebido)
- Registro de incidencias (devolución, daño, falta)

---

# 9. Control de Combustible

## Registro de Abastecimientos

| Campo | Tipo | Descripción |
|---|---|---|
| Fecha y hora | DateTime | Momento del abastecimiento |
| Vehículo | FK → vehicle | Vehículo abastecido |
| Conductor | FK → driver | Conductor que reporta |
| Tipo combustible | FK → fuel_type | Gasolina, Diésel, Eléctrico (kWh), Gas |
| Cantidad (litros) | Decimal (10,2) | Litros cargados |
| Precio por litro | Decimal (10,4) | Costo unitario |
| Costo total | Decimal (12,2) | Monto total |
| Kilometraje actual | Decimal (10,2) | Km al momento de carga |
| Horómetro actual | Decimal (10,2) | Horas al momento de carga |
| Proveedor | FK → supplier | Estación o proveedor |
| Tipo de abastecimiento | Enum | Completo, Parcial, Emergencia |
| Método de pago | Enum | Efectivo, Tarjeta, Crédito, Vale |
| Factura / Comprobante | File | PDF de la factura |
| Observaciones | Text | Notas adicionales |
| Válido para cálculo | Boolean | Si se incluye en rendimiento |

## Cálculos Automáticos

### Rendimiento por Vehículo
```
Rendimiento (km/L) = (Km actual - Km anterior) / Litros cargados
```
- Calculado automáticamente en cada carga válida
- Promedio ponderado de últimas 5 cargas
- Promedio histórico general

### Costo por Kilómetro
```
Costo por km = Costo total combustible / (Km actual - Km anterior)
```

### Detección de Anomalías
- Consumo > 20% del promedio histórico → alerta amarilla
- Consumo > 40% del promedio histórico → alerta roja (posible fuga o robo)
- Rendimiento cae más de 15% en 3 cargas consecutivas → posible problema mecánico
- Carga sin km recorridos entre cargas → posible mal registro

## KPIs de Combustible

| KPI | Cálculo | Visualización |
|---|---|---|
| Km/L promedio flota | SUM(km) / SUM(litros) | Número grande + indicador tendencia |
| Costo/L promedio ponderado | SUM(costo) / SUM(litros) | Moneda |
| Costo/km flota | SUM(costo total) / SUM(km) | Moneda |
| Consumo mensual (L) | SUM(litros) período | Gráfico barra por mes |
| Vehículo más eficiente | MAX(km/L) en período | Tarjeta destacada |
| Vehículo menos eficiente | MIN(km/L) en período | Tarjeta alerta |
| Anomalías detectadas | COUNT alertas activas | Badge en sidebar |

## UI: Control de Combustible

- **Lista**: Tabla con cargas recientes (vehículo, conductor, litros, costo, km, rendimiento)
- **Gráfico**: Rendimiento histórico del vehículo seleccionado (línea)
- **Formulario**: Carga rápida con auto-completado de vehículo/conductor, km sugerido del último registro
- **Panel de anomalías**: Lista de cargas marcadas como anómalas con justificación
- **Exportación**: Reporte mensual de consumo por vehículo

---

# 10. Mantenimiento Preventivo

## Programación

### Por Fecha
- Intervalo fijo en días (ej. cada 90 días)
- Fecha específica del mes (ej. día 1 de cada mes)
- Día de semana específico (ej. cada lunes)

### Por Kilometraje
- Intervalo fijo en km (ej. cada 5,000 km)
- Mantenimiento mayor (ej. cada 50,000 km)
- Al alcanzar kilometraje específico

### Por Horómetro
- Intervalo en horas de operación (ej. cada 250 horas)
- Ideal para maquinaria y equipos estacionarios

## Plantillas de Mantenimiento

| Plantilla | Aplicación | Intervalo típico |
|---|---|---|
| Cambio de aceite + filtro | Todos | 5,000 km / 3 meses |
| Rotación de llantas | Livianos | 10,000 km |
| Revisión de frenos | Todos | 10,000 km / 6 meses |
| Cambio de filtro de aire | Todos | 15,000 km |
| Cambio de filtro de combustible | Diésel | 20,000 km |
| Alineación y balanceo | Livianos | 10,000 km |
| Revisión de batería | Todos | 6 meses |
| Cambio de líquido refrigerante | Todos | 40,000 km / 2 años |
| Revisión de transmisión | Todos | 30,000 km |
| Cambio de llantas | Todos | 50,000 km |
| Mantenimiento mayor (general) | Todos | 50,000 km / 1 año |
| Revisión del sistema de escape | Todos | 20,000 km |

## Estructura de Mantenimiento Programado

| Campo | Tipo | Descripción |
|---|---|---|
| Vehículo | FK → vehicle | Vehículo objetivo |
| Plantilla | FK → maintenance_template | Tipo de mantenimiento |
| Tipo de programación | Enum | Fecha, Kilometraje, Horómetro |
| Valor del intervalo | Integer | Días, km, o horas según tipo |
| Próxima ejecución | Date/Km/Horas | Valor calculado |
| Última ejecución | Date | Fecha del último mantenimiento |
| Tolerancia | Integer | Días/km extras permitidos antes de alertar |
| Estado | Enum | Activo, Pausado, Completado, Vencido |

## UI: Plan de Mantenimiento

- **Calendario**: Vista mensual con eventos de mantenimiento programados
- **Lista por vehículo**: Próximos 5 mantenimientos de cada vehículo
- **Alertas**: Badges en el sidebar de mantenimientos vencidos y próximos
- **Checklist**: Lista de verificación por tipo de mantenimiento
- **Historial**: Timeline completo de mantenimientos realizados por vehículo

---

# 11. Mantenimiento Correctivo

## Órdenes de Trabajo (OT)

| Campo | Tipo | Descripción |
|---|---|---|
| Número OT | String (20) | Auto-generado |
| Vehículo | FK → vehicle | Vehículo con problema |
| Conductor reporta | FK → driver | Quien detecta el problema |
| Fecha de reporte | DateTime | Cuándo se reportó |
| Tipo de falla | FK → failure_type | Categoría del problema |
| Descripción del problema | Text | Detalle del síntoma |
| Diagnóstico | Text | Resultado de inspección |
| Causa raíz | Text | Causa identificada |
| Solución aplicada | Text | Reparación realizada |
| Prioridad | Enum | Baja, Media, Alta, Urgente |
| Estado | Enum | Reportado, Diagnosticado, Aprobado, En Reparación, En Espera Repuestos, Completado, Cerrado, Cancelado |
| Taller | FK → workshop | Taller interno o externo |
| Mecánico responsable | String (100) | Técnico asignado |
| Fecha inicio | DateTime | Inicio de reparación |
| Fecha fin | DateTime | Fin de reparación |
| Tiempo fuera de servicio | Integer | Horas que el vehículo estuvo fuera |
| Costo estimado | Decimal | Presupuesto aprobado |
| Costo total mano obra | Decimal | Costo de mano de obra |
| Costo total repuestos | Decimal | Costo de piezas |
| Costo total | Decimal | Suma de todos los costos |
| Documentos | File[] | Facturas, presupuestos, informes |
| Aprobado por | FK → user | Quien autorizó |

## Repuestos Utilizados

| Campo | Tipo | Descripción |
|---|---|---|
| Producto | FK → product | Repuesto del inventario |
| Cantidad | Decimal | Unidades utilizadas |
| Costo unitario | Decimal | Precio de compra |
| Código de proveedor | String | Referencia del fabricante |
| Garantía | Date | Fecha de vencimiento de garantía del repuesto |

## Mano de Obra

| Campo | Tipo | Descripción |
|---|---|---|
| Técnico | String | Nombre del técnico |
| Horas trabajadas | Decimal | Horas dedicadas |
| Tarifa por hora | Decimal | Costo por hora |
| Costo total | Decimal | Horas × Tarifa |
| Descripción | Text | Trabajo realizado |

## Indicadores de Mantenimiento

| Indicador | Fórmula | Propósito |
|---|---|---|
| Tiempo Medio Entre Fallas (MTBF) | Total horas operación / Número de fallas | Confiabilidad |
| Tiempo Medio de Reparación (MTTR) | Total horas reparación / Número de reparaciones | Eficiencia de taller |
| Disponibilidad | (Tiempo total - Tiempo fuera) / Tiempo total | % de tiempo operativo |
| Costo de Mantenimiento por Km | Total mantenimiento / Km recorridos | Eficiencia de costos |
| % Correctivo vs Preventivo | Costo correctivo / Costo total | Madurez del programa |

## UI: Taller y Órdenes de Trabajo

- **Lista OT**: Tabla con número, vehículo, prioridad, estado, fechas, costo
- **Filtros**: Estado, Prioridad, Vehículo, Taller, Rango de fechas
- **Kanban**: Tablero visual por estado (Reportado → Diagnosticado → Aprobado → En Reparación → Completado)
- **Detalle OT**: Timeline con fotos del antes/después, checklist de diagnóstico, sección de repuestos, firma de conformidad
- **Aprobación**: Flujo de aprobación con presupuesto antes de iniciar reparación mayor

---

# 12. Gestión de Documentos

## Tipos de Documento

| Tipo | Entidad Asociada | Vencimiento | Alerta |
|---|---|---|---|
| Tarjeta de Circulación | Vehículo | Sí | 30 días antes |
| Póliza de Seguro | Vehículo | Sí | 45 días antes |
| Revisión Técnica Mecánica | Vehículo | Sí | 30 días antes |
| Permiso de Carga | Vehículo | Sí | 30 días antes |
| Permiso Ambiental | Vehículo | Sí | 30 días antes |
| Certificado de Propiedad | Vehículo | No | — |
| Licencia de Conducir | Conductor | Sí | 30 días antes |
| Certificado Médico | Conductor | Sí | 30 días antes |
| Curso de Capacitación | Conductor | Sí | 15 días antes |
| Certificado de Antecedentes | Conductor | Sí | 30 días antes |
| Permiso Especial (Mercancías Peligrosas) | Conductor | Sí | 30 días antes |
| Factura de Combustible | Abastecimiento | No | — |
| Factura de Mantenimiento | OT | No | — |

## Estructura de Documento

| Campo | Tipo | Descripción |
|---|---|---|
| Entidad tipo | Enum | vehicle, driver |
| Entidad ID | FK | ID del vehículo o conductor |
| Tipo documento | FK → document_type | Catálogo |
| Número de documento | String (50) | Número de referencia |
| Fecha de emisión | Date | Fecha de expedición |
| Fecha de vencimiento | Date | Fecha de expiración |
| Archivo | File | PDF, imagen del documento |
| Notas | Text | Observaciones |
| Estado | Enum | Vigente, Próximo a Vencer, Vencido |
| Alerta enviada | Boolean | Control de notificación |

## UI: Gestión de Documentos

- **Dashboard documental**: Tarjetas por vehículo/conductor con semáforo de documentos
- **Vista de calendario**: Próximos vencimientos en calendario mensual
- **Lista filtrable**: Por entidad, tipo, estado, rango de vencimiento
- **Upload**: Arrastrar y soltar archivos, vista previa de PDF/imagen
- **Acciones**: Renovar (crea nuevo desde el anterior), Ver histórico, Descargar

---

# 13. GPS y Monitoreo

## Integración con Proveedores GPS

| Proveedor | Protocolo | Tipo | Funcionalidad |
|---|---|---|---|
| **Traccar** | API REST + WebSocket | Open Source | Tiempo real, historial, geocercas |
| **Teltonika** | TCP/UDP + AVL | Hardware | Datos de motor, temperatura, consumo |
| **Concox** | TCP + GMT | Hardware | Tracking básico, batería, alarma |
| **Queclink** | TCP/UDP + MV | Hardware | Tracking, geocerca, sensor de ignición |
| **Integración genérica** | API REST | Cualquiera | Endpoint para recibir datos en formato estándar |

## Datos de GPS

| Campo | Tipo | Descripción |
|---|---|---|
| Dispositivo ID | String | Identificador único del dispositivo |
| Vehículo | FK → vehicle | Vehículo asociado |
| Latitud | Decimal (10,7) | Coordenada |
| Longitud | Decimal (10,7) | Coordenada |
| Altitud | Decimal | Metros sobre nivel del mar |
| Velocidad | Decimal | Km/h actual |
| Rumbo | Integer | Grados (0–360) |
| Fecha y hora GPS | DateTime | Timestamp del dispositivo |
| Encendido | Boolean | Motor on/off |
| Odómetro | Decimal | Km totales desde dispositivo |
| Nivel combustible | Decimal | % de tanque |
| Temperatura | Decimal | °C (para refrigerados) |
| Batería dispositivo | Decimal | % de batería del GPS |
| Señal GSM | Integer | dBm |
| Satélites | Integer | Número de satélites |

## Funcionalidades de GPS

### Mapa en Tiempo Real
- Marcadores de cada vehículo con ícono por tipo
- Color del marcador según estado (verde=en ruta, azul=detenido, rojo=alarma)
- Popup al hacer clic: placa, conductor, velocidad, dirección
- Actualización cada 5–30 segundos (configurable)
- Capas: tráfico, satélite, geocercas

### Historial de Rutas
- Reproductor de ruta (play/pause/velocidad)
- Rango de fechas seleccionable
- Color de línea por velocidad (verde=normal, amarillo=media, rojo=alta)
- Marcadores de paradas (detención > 5 min)

### Geocercas
- Círculo (radio en metros)
- Polígono (área personalizada)
- Asociadas a: sucursal, cliente, zona de entrega, zona restringida
- Eventos: Entrada, Salida, Permanencia excesiva

### Alertas GPS
| Alerta | Condición | Acción |
|---|---|---|
| Exceso de velocidad | Velocidad > límite configurado | Notificación al supervisor |
| Salida de geocerca | Vehículo sale de zona permitida | Alerta + registro |
| Entrada a geocerca | Vehículo llega a destino | Actualización de estado entrega |
| Detención prolongada | Velocidad = 0 por > X min | Notificación si no es punto de entrega |
| Pérdida de señal | Sin datos por > 10 min | Alarma de posible robo |
| Encendido en horario no laboral | Motor on fuera de horario | Alerta de uso no autorizado |
| Batería GPS baja | Batería < 20% | Notificación a administrador |
| Sobre temperatura (refrigerado) | Temp > umbral | Alarma inmediata |

---

# 14. Control de Gastos

## Categorías de Gastos

| Categoría | Subcategorías | Entidad Asociada |
|---|---|---|
| Combustible | Gasolina, Diésel, Eléctrico, Gas, Aditivos | Vehículo, Viaje |
| Mantenimiento | Preventivo, Correctivo, Llantas, Baterías | Vehículo |
| Taller | Mano de obra, Diagnóstico, Servicios externos | Vehículo, OT |
| Viáticos | Alimentación, Hospedaje, Transporte público | Conductor, Viaje |
| Peajes | Manual, Telepeaje (TAG) | Vehículo, Ruta |
| Multas | Tránsito, Estacionamiento, Ambientales | Vehículo, Conductor |
| Parqueos | Diurno, Nocturno, Mensualidad | Vehículo |
| Seguros | Prima, Endosos, Renovación | Vehículo |
| Impuestos | Circulación, Rodaje, Municipales | Vehículo |
| Otros | Lavado, Insumos, Varios | Vehículo |

## Estructura de Gasto

| Campo | Tipo | Descripción |
|---|---|---|
| Fecha | Date | Fecha del gasto |
| Categoría | FK → expense_category | Categoría del gasto |
| Subcategoría | FK → expense_subcategory | Subcategoría |
| Vehículo | FK → vehicle | Vehículo asociado (opcional) |
| Conductor | FK → driver | Conductor asociado (opcional) |
| Viaje | FK → trip | Viaje asociado (opcional) |
| Ruta | FK → route | Ruta asociada (opcional) |
| Entidad tipo | Enum | vehicle, driver, trip, route |
| Entidad ID | FK | ID de la entidad |
| Proveedor | FK → supplier | A quién se pagó |
| Descripción | String (200) | Concepto del gasto |
| Monto | Decimal (12,2) | Valor monetario |
| Moneda | String (3) | Código ISO (USD, NIO, CRC) |
| Tipo de cambio | Decimal (10,4) | TC aplicado |
| Monto en moneda base | Decimal (12,2) | Convertido a moneda base |
| Método de pago | Enum | Efectivo, Tarjeta Débito, Tarjeta Crédito, Transferencia, Cheque |
| Documento | File | Factura o comprobante |
| Reembolsable | Boolean | Si el conductor debe reembolsar |
| Reembolsado | Boolean | Si ya fue reembolsado |
| Aprobado | Boolean | Aprobación de supervisor |
| Cuenta contable | FK → account | Cuenta para asiento contable |

## Cálculo de Costos

| Indicador | Fórmula | Período |
|---|---|---|
| Costo operativo total | SUM(todos los gastos) | Mensual / Anual |
| Costo por vehículo | SUM(gastos por vehículo) | Mensual / Anual |
| Costo por kilómetro | SUM(gastos totales) / SUM(km) | Mensual |
| Costo por hora | SUM(gastos totales) / SUM(horas) | Mensual |
| Costo por entrega | SUM(gastos logísticos) / COUNT(entregas) | Mensual |
| Costo de combustible | SUM(gastos combustible) | Mensual |
| % Combustible / Total | Costo combustible / Costo operativo | Mensual |
| % Mantenimiento / Total | Costo mantenimiento / Costo operativo | Mensual |

## UI: Control de Gastos

- **Lista**: Tabla con fecha, categoría, vehículo, monto, método pago, estado aprobación
- **Filtros**: Categoría, Vehículo, Rango fechas, Estado aprobación
- **Vista por vehículo**: Costos agrupados por vehículo con desglose por categoría
- **Gráficos circulares**: Distribución de gastos por categoría
- **Comparativa**: Mes actual vs. mes anterior
- **Exportación**: Reporte contable de gastos

---

# 15. Reportes Empresariales

## Reportes Operativos

### Uso de Vehículos
- Vehículos más utilizados (km recorridos en período)
- Tasa de uso (días operativos / días totales)
- Vehículos subutilizados (< 50% del tiempo)
- Horas de operación por vehículo
- Comparativa uso planificado vs. real

### Reporte de Entregas
- Entregas por día / semana / mes
- Entregas por conductor
- Entregas por zona / ruta
- Tasa de entregas a tiempo (on-time delivery rate)
- Tasa de entregas completas (fill rate)
- Devoluciones y causas
- Tiempo promedio de entrega

### Reporte de Rutas
- Distancia recorrida por ruta
- Tiempo real vs. estimado
- Costo real vs. estimado
- Paradas realizadas vs. planificadas
- Eficiencia de ruta (km directos / km reales)

## Reportes Financieros

### Costos Operativos
- Costo total por período (mensual, trimestral, anual)
- Costo por vehículo
- Costo por categoría (combustible, mantenimiento, etc.)
- Costo por kilómetro (tendencia)
- Costo por hora operativa
- Comparativa presupuesto vs. real
- Proyección de costos

### Rentabilidad Logística
- Ingresos por flete / servicio de transporte
- Costos logísticos totales
- Margen bruto logístico
- Rentabilidad por cliente
- Rentabilidad por ruta
- Rentabilidad por vehículo

## Reportes Gerenciales

### KPIs de Flota
- Dashboard ejecutivo (sección 4)
- Scorecard de conductores
- Scorecard de vehículos
- Cumplimiento de mantenimiento preventivo
- Tendencias de consumo de combustible
- Disponibilidad de flota

### Productividad
- Entregas por conductor por día
- Km por conductor por día
- Tiempo productivo vs. tiempo muerto
- Eficiencia de combustible por conductor
- Índice de accidentes / infracciones

## Formatos de Exportación

| Formato | Reportes |
|---|---|
| PDF | Todos los reportes |
| Excel (XLSX) | Reportes operativos y financieros |
| CSV | Datos crudos |
| JSON | API response |

---

# 16. Automatizaciones

## Matriz de Automatizaciones

| # | Evento Disparador | Condición | Acción | Canal | Destinatario |
|---|---|---|---|---|---|
| 1 | Mantenimiento próximo | Fecha ≤ (próximo - tolerancia) | Recordatorio | Dashboard + Push + Correo | Administrador flota |
| 2 | Mantenimiento vencido | Fecha < HOY | Alerta urgente | Dashboard + Push + Correo + WhatsApp | Administrador + Supervisor |
| 3 | Licencia por vencer | Días ≤ 30 | Recordatorio | Dashboard + Push + Correo | Conductor + RH |
| 4 | Licencia vencida | Fecha < HOY | Bloqueo + Alerta | Dashboard + Push + WhatsApp | Conductor + Supervisor |
| 5 | Seguro por vencer | Días ≤ 45 | Recordatorio renovación | Dashboard + Push + Correo | Administrador flota |
| 6 | Documento vencido | Fecha < HOY | Alerta + Restricción | Dashboard + Push | Administrador |
| 7 | Exceso de velocidad | Velocidad > límite | Alerta inmediata | Push + WhatsApp | Supervisor |
| 8 | Geocerca violada | Vehículo sale de zona | Alerta | Push + WhatsApp | Administrador |
| 9 | Detención prolongada | Velocidad = 0 por > X min | Alerta | Push | Supervisor |
| 10 | Consumo anómalo | Rendimiento cae > 20% | Notificación | Dashboard + Push | Administrador |
| 11 | Carga de combustible | Registro de carga > umbral diario | Alerta de gasto | Push | Supervisor financiero |
| 12 | OT creada | Nueva orden de trabajo | Notificación | Dashboard + Push | Taller + Administrador |
| 13 | Entrega completada | Estado = Entregado | Confirmación | WhatsApp + Correo + Push | Cliente |
| 14 | Entrega en ruta | Estado = En Ruta | Notificación ETA | WhatsApp | Cliente |
| 15 | Viaje iniciado | Estado = En Progreso | Notificación | Push | Administrador |
| 16 | Viaje completado | Estado = Completado | Solicitud de cierre | Dashboard + Push | Conductor |
| 17 | Presupuesto excedido | Costo real > presupuesto + 10% | Alerta | Dashboard + Correo | Gerente |
| 18 | Cumpleaños conductor | Fecha = HOY | Felicitación | Correo + Dashboard | Conductor |

## Reglas de Automatización

El módulo incluye un motor de reglas configurable donde el administrador puede:

- Activar/desactivar cada automatización
- Configurar umbrales (días, km, %, velocidad)
- Seleccionar canales de notificación
- Definir destinatarios por rol
- Programar hora de envío (no enviar notificaciones en madrugada)

## Integración con Notificaciones

- **Notificaciones push**: App móvil Zorvian
- **Correo electrónico**: MailKit / SendGrid
- **WhatsApp**: API de WhatsApp Business (Twilio / Meta Cloud API)
- **SMS**: Para alertas críticas sin conexión a internet
- **Dashboard**: Central de notificaciones con historial

---

# 17. Integración con Otros Módulos

## Matriz de Integración

| Módulo Zorvian | Integración | Dirección | Datos Compartidos |
|---|---|---|---|
| **CRM** | Visitas comerciales planificadas como viajes | CRM → Flota | Oportunidades convertidas en visitas |
| **CRM** | Seguimiento de entregas visibles desde ficha de cliente | Flota → CRM | Estado de entregas del cliente |
| **Ventas** | Órdenes de venta generan entregas automáticas | Ventas → Flota | Items, cliente, dirección |
| **Ventas** | Estado de entrega actualiza estado de venta | Flota → Ventas | Entregado/Pendiente/Devuelto |
| **Inventario** | Salidas de inventario por carga de vehículo | Flota ←→ Inventario | Productos, lotes, cantidades |
| **Inventario** | Repuestos de taller descontados de inventario | Taller → Inventario | Productos, cantidades |
| **Compras** | Órdenes de compra de combustible y repuestos | Compras ←→ Flota | Proveedores, precios |
| **Compras** | Recepción de combustible como compra | Compras → Flota | Litros, costo, proveedor |
| **RRHH** | Conductores vinculados a expediente de empleado | RRHH ←→ Flota | Datos personales, contacto |
| **Nómina** | Viáticos y bonificaciones enviados a nómina | Flota → Nómina | Montos, períodos, conductor |
| **Nómina** | Horas extras de conductores integradas | Flota → Nómina | Horas, tarifas |
| **Contabilidad** | Asientos contables de gastos operativos | Flota → Contabilidad | Cuenta, monto, centro costo |
| **Contabilidad** | Depreciación de vehículos | Activos → Contabilidad | Monto depreciación mensual |
| **Activos Fijos** | Vehículos registrados como activos fijos | Flota ←→ Activos Fijos | Código, valor, depreciación |
| **Activos Fijos** | Actualización de estado del activo por venta/baja | Flota → Activos Fijos | Fecha venta, valor residual |
| **Sucursales** | Vehículos y conductores por sucursal | Flota ←→ Sucursales | Asignación geográfica |
| **Presupuestos** | Presupuesto de flota vs. ejecución real | Flota → Presupuestos | Gastos reales |
| **Centros de Costo** | Gastos asignados a centros de costo | Flota → C. Costo | Monto por centro |
| **Aprobaciones** | Flujo de aprobación de OT y gastos mayores | Flota ←→ Aprobaciones | Solicitudes, aprobadores |
| **Reportes** | Datos de flota en reportes personalizados | Flota → Reportes | Todos los datos |

## Diagrama de Integración

```
┌──────────┐     ┌───────────┐     ┌───────────┐     ┌──────────┐
│   CRM    │◄───►│  Ventas   │◄───►│ Inventario│◄───►│ Compras  │
└────┬─────┘     └─────┬─────┘     └─────┬─────┘     └────┬─────┘
     │                  │                 │                │
     │                  │                 │                │
     ▼                  ▼                 ▼                ▼
┌─────────────────────────────────────────────────────────────────┐
│                   FLOTA Y LOGÍSTICA (Fleet)                     │
│  Vehículos │ Conductores │ Rutas │ Entregas │ Combustible      │
│  Mantenimiento │ Taller │ GPS │ Gastos │ Documentos           │
└────────┬────────────────────────┬──────────────────────┬────────┘
         │                        │                      │
         ▼                        ▼                      ▼
┌──────────┐     ┌───────────┐     ┌───────────┐
│  RRHH /  │     │   Nómina  │     │Contabilidad│
│ Empleados│     │           │     │            │
└──────────┘     └───────────┘     └─────┬──────┘
                                         │
                                         ▼
                                  ┌───────────┐
                                  │  Activos   │
                                  │   Fijos    │
                                  └───────────┘
```

---

# 18. Diseño UX/UI

## Principios de Diseño

1. **Jerarquía visual clara**: Lo más importante (KPIs, alertas) primero y más grande
2. **Datos primero**: Números, fechas y estados sin ambigüedad
3. **Acción mínima**: Las tareas más frecuentes en 1 clic
4. **Contexto permanente**: Filtros globales visibles en todas las pantallas
5. **Feedback inmediato**: Toast, animaciones sutiles, confirmaciones

## Paleta de Colores del Módulo

| Token | Color | Uso |
|---|---|---|
| `moduleFleet` | `#FF6D00` (Naranja Logístico) | Color principal del módulo |
| `moduleFleetLight` | `#FF9E40` | Hover, fondos suaves |
| `moduleFleetDark` | `#C43E00` | Active state, acentos fuertes |
| `fleetStatusActive` | `#00C853` | Vehículo activo |
| `fleetStatusAvailable` | `#2979FF` | Vehículo disponible |
| `fleetStatusInRoute` | `#FF6D00` | En ruta |
| `fleetStatusMaintenance` | `#D50000` | En mantenimiento |
| `fleetStatusOutOfService` | `#78909C` | Fuera de servicio |

## Arquitectura de Pantallas

### Dashboard
```
┌──────────────────────────────────────────────────────────────┐
│  🚛 Dashboard de Flota                       [Hoy] [Filtrar] │
├──────────────────────────────────────────────────────────────┤
│ ┌──────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐             │
│ │ 45   │ │  128     │ │  3,420 L │ │ $0.42   │             │
│ │Activos│ │Entregas  │ │Combust.  │ │ Costo/km│             │
│ └──────┘ └──────────┘ └──────────┘ └──────────┘             │
│ ┌──────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐             │
│ │ 12   │ │  8       │ │  23      │ │  8.5     │             │
│ │Disp. │ │Manten.   │ │Pendientes│ │ km/L     │             │
│ └──────┘ └──────────┘ └──────────┘ └──────────┘             │
├──────────────────────────────────────────────────────────────┤
│  ┌──────────────────────┐  ┌──────────────────────────┐      │
│  │ Costos por Mes       │  │ Consumo por Vehículo     │      │
│  │ [gráfico barras]     │  │ [gráfico barras horiz]   │      │
│  └──────────────────────┘  └──────────────────────────┘      │
├──────────────────────────────────────────────────────────────┤
│  ┌──────────────────────┐  ┌──────────────────────────┐      │
│  │ Entregas por Zona    │  │ Próximos Mantenimientos  │      │
│  │ [gráfico donut]      │  │ [tabla 5 items]         │      │
│  └──────────────────────┘  └──────────────────────────┘      │
└──────────────────────────────────────────────────────────────┘
```

### Lista de Vehículos
```
┌──────────────────────────────────────────────────────────────┐
│  🚗 Vehículos        [+ Nuevo] [Importar] [Exportar]        │
├──────────────────────────────────────────────────────────────┤
│  [Buscar...     ] [Estado: ▼] [Tipo: ▼] [Sucursal: ▼]      │
├──────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────┐    │
│  │ CÓDIGO │ PLACA │ MARCA/MODELO    │ AÑO │ ESTADO    │    │
│  ├──────────────────────────────────────────────────────┤    │
│  │ VH-001 │ NIC- │ Toyota Hilux    │ 2024│ 🟢 Activo  │    │
│  │        │ 1234  │                 │     │           │    │
│  │ VH-002 │ NIC- │ Hino 300        │ 2023│ 🟠 Ruta    │    │
│  │        │ 5678  │                 │     │           │    │
│  │ VH-003 │ NIC- │ Suzuki Address  │ 2024│ 🔴 Manten. │    │
│  │        │ 9012  │                 │     │           │    │
│  └──────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
```

### Detalle de Vehículo
```
┌──────────────────────────────────────────────────────────────┐
│  ← Volver     🚗 NIC-1234 · Toyota Hilux 2024      [Editar] │
├──────────────────────────────────────────────────────────────┤
│  [General] [Operativo] [Documentos] [GPS] [Costos] [Hist.]  │
├──────────────────────────────────────────────────────────────┤
│  Datos Generales                    │ Documentación          │
│  ┌────────────────────┐            │ ┌────────────────────┐ │
│  │ Código: VH-001     │            │ │ 📄 Circulación     │ │
│  │ Placa: NIC-1234    │            │ │   ✅ Vigente       │ │
│  │ Marca: Toyota      │            │ │ 📄 Seguro          │ │
│  │ Modelo: Hilux SRV  │            │ │   ⚠️ Vence en 15d  │ │
│  │ Año: 2024          │            │ │ 📄 RTV             │ │
│  │ VIN: 123...        │            │ │   ✅ Vigente       │ │
│  │ Estado: 🟢 Activo  │            │ └────────────────────┘ │
│  └────────────────────┘            │                        │
├────────────────────────────────────┴────────────────────────┤
│  Últimos Movimientos                                        │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ FECHA     │ TIPO          │ KM    │ COSTO            │   │
│  │ 10/06     │ Combustible   │ 15,420│ $85.00           │   │
│  │ 08/06     │ Mantenimiento │ 15,300│ $320.00          │   │
│  │ 05/06     │ Viaje         │ 15,200│ $45.00 (peajes)  │   │
│  └──────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────┘
```

## Componentes UI Específicos

### FleetKpiCard
- Variante de `ZStatCard` con icono del módulo, color `moduleFleet`
- Indicador de tendencia (↑ ↓ →) con color verde/rojo/gris
- Subtexto con comparativa vs. período anterior

### FleetStatusBadge
- Badge circular con color semántico
- Tooltip con nombre del estado
- Versión parpadeante para alertas activas

### FleetMap
- Componente de mapa embebido (Mapbox / Google Maps / OpenStreetMap)
- Marcadores personalizados por tipo de vehículo
- Popup con info rápida
- Capas toggleables (tráfico, geocercas, calor)

### FleetTimeline
- Timeline vertical para eventos de viaje, OT, historial de vehículo
- Iconos por tipo de evento
- Tooltip con detalle expandido

### FleetGauge
- Componente de medidor radial (velocidad, % carga, % combustible)
- Escala de color (verde → amarillo → rojo)

### FleetKanban
- Tablero kanban para entregas y OT
- Drag & drop entre columnas de estado
- Contador de items por columna

## Responsive Design

| Breakpoint | Layout | Comportamiento |
|---|---|---|
| < 576px (Mobile) | 1 columna, apilado | Tablas → tarjetas, menú → bottom nav |
| 576–992px (Tablet) | 2 columnas | Sidebar colapsable, gráficos reducidos |
| ≥ 992px (Desktop) | 3–4 columnas | Layout completo, sidebar expandido, mapas grandes |

## Estados de Carga

- **Skeleton screens** para todas las listas y dashboards
- **Spinner minimalista** para acciones inline
- **Transiciones suaves** (300ms ease) entre estados
- **Optimistic UI** para acciones frecuentes (cambiar estado, asignar)

---

# 19. Modelo de Base de Datos

## Diagrama Entidad-Relación (Texto)

```
┌─────────────────┐       ┌──────────────────┐       ┌──────────────────┐
│    vehicles      │       │     drivers      │       │      routes      │
├─────────────────┤       ├──────────────────┤       ├──────────────────┤
│ PK: id           │       │ PK: id           │       │ PK: id           │
│ code             │◄──────┤ FK: employee_id  │       │ code             │
│ plate            │       │ first_name       │       │ name             │
│ brand_id (FK)    │       │ last_name        │       │ vehicle_id (FK)  │
│ model            │       │ id_document      │       │ driver_id (FK)   │
│ year             │       │ license_number   │       │ date             │
│ vin              │       │ license_category  │       │ origin_address   │
│ engine_number    │       │ license_expiry    │       │ distance_est     │
│ chassis_number   │       │ phone            │       │ duration_est     │
│ color            │       │ email            │       │ status           │
│ vehicle_type_id  │       │ status           │       │ cost_est         │
│ fuel_type_id     │       │ branch_id (FK)   │       │ branch_id (FK)   │
│ branch_id (FK)   │       │ hire_date        │       └────────┬─────────┘
│ current_km       │       │ photo_url        │                │
│ status           │       └──────────────────┘                │
│ driver_id (FK)   │◄──────                                    │
│ gps_device_id    │       ┌──────────────────┐                │
│ asset_id (FK)    │       │   route_points   │                │
│ purchase_value   │       ├──────────────────│                │
│ purchase_date    │       │ PK: id           │                │
└─────────────────┘       │ FK: route_id     │                │
        │                 │ order            │                │
        │                 │ type             │                │
        ▼                 │ address          │                │
┌─────────────────┐       │ client_id (FK)   │                │
│ fuel_refills     │       │ sale_id (FK)     │                │
├─────────────────┤       │ time_window_start │                │
│ PK: id           │       │ time_window_end   │                │
│ FK: vehicle_id   │       │ duration_est      │                │
│ FK: driver_id    │       └──────────────────┘                │
│ refill_datetime  │                                           │
│ liters           │       ┌──────────────────┐                │
│ total_cost       │       │    deliveries     │                │
│ current_km       │       ├──────────────────┤                │
│ anomaly_flag     │       │ PK: id           │                │
└─────────────────┘       │ code             │                │
                          │ FK: sale_id      │                │
┌─────────────────┐       │ FK: route_id     │                │
│ maintenance_     │       │ FK: vehicle_id   │                │
│ schedules        │       │ status           │                │
├─────────────────┤       │ receiver_name    │                │
│ PK: id           │       │ signature_url    │                │
│ FK: vehicle_id   │       │ photos (json)    │                │
│ schedule_type    │       │ gps_lat/lng      │                │
│ interval_value   │       │ delivered_at     │                │
│ next_execution   │       └──────────────────┘                │
│ status           │                │                         │
└─────────────────┘                ▼                         │
                          ┌──────────────────┐                │
┌─────────────────┐       │ delivery_items   │                │
│ work_orders      │       ├──────────────────┤                │
├─────────────────┤       │ PK: id           │                │
│ PK: id           │       │ FK: delivery_id  │                │
│ number           │       │ FK: product_id   │                │
│ FK: vehicle_id   │       │ qty_delivered    │                │
│ status           │       │ qty_returned     │                │
│ priority         │       │ status           │                │
│ cost_total       │       └──────────────────┘                │
│ downtime_hours   │                                           │
└─────────────────┘       ┌──────────────────┐                │
        │                 │    expenses      │                │
        ▼                 ├──────────────────┤                │
┌─────────────────┐       │ PK: id           │                │
│ wo_parts         │       │ category_id (FK) │                │
├─────────────────┤       │ amount           │                │
│ PK: id           │       │ vehicle_id (FK)  │                │
│ FK: work_order_id│       │ account_id (FK)  │                │
│ FK: product_id   │       │ approved         │                │
│ qty              │       └──────────────────┘                │
└─────────────────┘                                           │
                          ┌──────────────────┐                │
┌─────────────────┐       │   gps_positions  │                │
│ geofences        │       ├──────────────────┤                │
├─────────────────┤       │ PK: id (BIGSERIAL)│               │
│ name             │       │ vehicle_id (FK)  │                │
│ type             │       │ latitude         │                │
│ coordinates(json)│       │ longitude        │                │
│ radius           │       │ speed            │                │
│ active           │       │ gps_timestamp    │                │
└─────────────────┘       └──────────────────┘                │
```

## Resumen de Tablas

| # | Tabla | Propósito | Volumen estimado |
|---|---|---|---|
| 1 | `vehicles` | Catálogo de vehículos | Bajo (10–10,000) |
| 2 | `drivers` | Perfiles de conductores | Bajo (10–5,000) |
| 3 | `routes` | Planificación de rutas | Medio |
| 4 | `route_points` | Puntos de ruta | Medio-Alto |
| 5 | `deliveries` | Entregas | Alto |
| 6 | `delivery_items` | Items de entrega | Alto |
| 7 | `fuel_refills` | Abastecimientos | Alto |
| 8 | `maintenance_schedules` | Programación preventiva | Bajo |
| 9 | `work_orders` | Órdenes de trabajo | Medio |
| 10 | `wo_parts` | Repuestos de OT | Medio |
| 11 | `gps_positions` | Posiciones GPS | Muy Alto (millones) |
| 12 | `geofences` | Geocercas | Bajo |
| 13 | `trips` | Viajes | Medio-Alto |
| 14 | `expenses` | Gastos operativos | Alto |
| 15 | `documents` | Documentos digitales | Bajo |
| 16 | `driver_infractions` | Infracciones | Bajo |
| 17 | `driver_trainings` | Capacitaciones | Bajo |
| 18 | `maintenance_templates` | Plantillas mantenimiento | Bajo |
| 19+ | Tablas catálogo (7) | Datos de referencia | Bajo |

## Políticas de RLS (Row Level Security)

```sql
ALTER TABLE vehicles ENABLE ROW LEVEL SECURITY;
ALTER TABLE drivers ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON vehicles
  USING (tenant_id = current_setting('app.tenant_id')::UUID);

CREATE POLICY gps_read ON gps_positions
  FOR SELECT
  USING (tenant_id = current_setting('app.tenant_id')::UUID);
```

---

# 20. Permisos y Seguridad

## Roles del Módulo

| Rol | Nivel | Descripción |
|---|---|---|
| `FleetAdmin` | Administrativo | Acceso completo: configurar, crear, editar, eliminar, aprobar |
| `FleetSupervisor` | Supervisor | Ver todos los datos, aprobar OT y gastos, asignar rutas y conductores |
| `FleetDispatcher` | Operativo | Crear rutas, asignar entregas, ver GPS, gestionar viajes |
| `FleetDriver` | Conductor | App móvil: ver entregas, reportar incidencias, registrar combustible |
| `FleetViewer` | Consulta | Solo lectura: dashboards, reportes, KPIs |
| `FleetAccountant` | Financiero | Ver y aprobar gastos, reportes financieros, integración contable |
| `FleetMechanic` | Taller | Ver y actualizar OT, registrar repuestos, cerrar mantenimientos |

## Matriz de Permisos (selección)

| Permiso | Admin | Supervisor | Dispatcher | Driver | Viewer | Accountant | Mechanic |
|---|---|---|---|---|---|---|---|
| Ver dashboard | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Crear/editar vehículos | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Eliminar vehículos | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Crear/editar conductores | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Crear/editar rutas | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Confirmar entrega | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Registrar combustible | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Aprobar OT > $500 | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Configurar módulo | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

## Auditoría

- **`created_at` / `updated_at`** en todas las tablas
- **`created_by` / `updated_by`**: usuario que ejecuta la acción
- **`audit_logs`**: captura INSERT, UPDATE, DELETE con valores anteriores/nuevos, usuario, fecha, IP, entidad, acción

Eventos de auditoría específicos: `VEHICLE_CREATED`, `VEHICLE_STATUS_CHANGED`, `DELIVERY_STATUS_CHANGED`, `FUEL_REFILL_ANOMALY`, `WORK_ORDER_APPROVED`, `EXPENSE_APPROVED`, `DOCUMENT_EXPIRING_SENT`, `GEOFENCE_ALERT`.

---

# 21. Escalabilidad

## Por Volumen

### 10 Vehículos (PYME)
- PostgreSQL estándar, sin particionamiento
- GPS: pooling cada 30 segundos
- Servidor: 1 CPU, 2 GB RAM

### 100 Vehículos (Mediana)
- PostgreSQL con índices optimizados, particionamiento mensual de `gps_positions`
- GPS: WebSocket cada 10 segundos
- Redis para últimas posiciones (TTL 5 min)
- Servidor: 2 CPU, 4 GB RAM

### 1,000 Vehículos (Grande)
- PostgreSQL replicación lectura/escritura
- GPS: RabbitMQ + worker batch cada 5 segundos
- Elasticsearch para búsqueda
- CDN para imágenes
- Servidor: 4 CPU, 16 GB RAM

### 10,000+ Vehículos (Corporación)
- Sharding por tenant o región
- TimescaleDB para GPS
- Cluster Traccar con balanceo de carga
- Redis Cluster, RabbitMQ cluster
- API Gateway con rate limiting
- MinIO / S3 para almacenamiento

## Estrategia de Datos GPS

```
GPS Device → Traccar Server → RabbitMQ → Worker GPS → TimescaleDB + Redis
                                                          │
                                                          ▼
                                                     API REST → Frontend
```

## Estrategia de Caché

| Dato | Cache | TTL |
|---|---|---|
| Última posición GPS | Redis string | 5 seg |
| Datos de vehículo | Redis hash | 5 min |
| KPIs del dashboard | Redis sorted set | 5 min |
| Geocercas activas | Redis set | 10 min |

---

# 22. Análisis Competitivo

## Comparativa

| Característica | Zorvian Fleet | Odoo Fleet | SAP TM | Oracle OTM | Zoho Fleet | Fleetio |
|---|---|---|---|---|---|---|
| **ERP integrado** | ✅ Nativo | ✅ Nativo | ✅ Suite | ✅ Suite | ⚠️ Parcial | ❌ Standalone |
| **Multiempresa nativo** | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| **GPS en tiempo real** | ✅ (Traccar) | ⚠️ (App externa) | ✅ | ✅ | ✅ | ✅ |
| **Optimización rutas** | ✅ | ❌ | ✅ | ✅ | ✅ | ❌ |
| **App móvil conductores** | ✅ (Flutter) | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Firma digital** | ✅ Nativo | ❌ | ⚠️ Add-on | ⚠️ | ❌ | ❌ |
| **Geocercas** | ✅ Ilimitadas | ❌ | ✅ | ✅ | ✅ | ✅ |
| **Mantenimiento predictivo IA** | ✅ (Fase 4) | ❌ | ✅ Add-on | ✅ | ❌ | ⚠️ Básico |
| **Control combustible** | ✅ + Detección anomalías | ✅ Básico | ✅ | ✅ | ✅ | ✅ |
| **Multi-moneda** | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| **WhatsApp integrado** | ✅ Nativo | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Cumplimiento LATAM** | ✅ (DGI, SAT, Hacienda) | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Precio** | Incluido en ERP | $24.90/usuario | $5,000+/mes | $7,500+/mes | $20/vehículo | $5/vehículo |

## Fortalezas

1. **Integración nativa**: Sin middleware, APIs externas ni sincronización
2. **Costo total**: Incluido en suscripción ERP, sin costos adicionales
3. **Cobertura LATAM**: Cumplimiento regulatorio de Nicaragua, Costa Rica, Guatemala
4. **WhatsApp integrado**: Notificaciones nativas sin servicios externos
5. **Arquitectura multiempresa**: Aislamiento completo desde el diseño
6. **Firma digital nativa**: Captura en app móvil con geolocalización
7. **Detección de anomalías**: Algoritmos de consumo anómalo sin configuración
8. **ERP completo**: CRM + Ventas + Inventario + Contabilidad + RRHH + Nómina + Flota

## Debilidades

1. Reconocimiento de marca menor que SAP, Oracle, Dynamics
2. Módulo nuevo con menos casos de uso probados
3. Sin integración con Waze/Google Maps Traffic (Fase 4)
4. Menor ecosistema de partners y extensiones

## Oportunidades

1. Mercado LATAM desatendido por soluciones globales
2. PYMEs en crecimiento necesitan ERP integrado, no soluciones standalone
3. Demanda post-pandemia de tracking en tiempo real y entregas sin contacto
4. Sostenibilidad: control de huella de carbono y reportes ESG
5. IA predictiva como diferenciador clave

---

# 23. Roadmap de Implementación

## FASE 1: MVP (8–10 semanas)

**Objetivo**: Módulo funcional para empresas con 5–30 vehículos.

| Semana | Entregables |
|---|---|
| 1–2 | Modelo de datos + migraciones EF Core + menú lateral + routing + catálogos |
| 3–4 | CRUD vehículos + conductores + documentos + vinculación sucursales/empleados |
| 5–6 | Flujo entregas desde venta + rutas manuales + app móvil conductores + firma digital |
| 7–8 | Dashboard KPIs + reportes básicos + alertas vencimiento + integración contable |

**Entrega**: Módulo estable para 5–30 vehículos, UI web + app móvil, integración Ventas/Inventario/Contabilidad.

## FASE 2: Operación Completa (6–8 semanas)

**Objetivo**: Cubrir todas las operaciones logísticas diarias.

| Semana | Entregables |
|---|---|
| 9–10 | Control combustible + detección anomalías + mantenimiento preventivo + OT + taller/repuestos |
| 11–12 | Gastos + costo/km + presupuestos + centros costo + activos fijos + viáticos/nómina |
| 13–14 | Reportes financieros + gerenciales + exportación PDF/Excel + reportes personalizados |
| 15–16 | Motor de reglas + notificaciones push/correo/WhatsApp + alertas automáticas + bloqueo conductores |

**Entrega**: Módulo completo para 30–200 vehículos, automatizaciones operativas, integración RRHH/Nómina/Activos Fijos.

## FASE 3: GPS y Automatización Inteligente (6–8 semanas)

**Objetivo**: Geolocalización en tiempo real y automatización avanzada.

| Semana | Entregables |
|---|---|
| 17–18 | Integración Traccar + recepción datos Teltonika/Concox/Queclink + almacenamiento GPS + mapa tiempo real |
| 19–20 | Geocercas + alertas velocidad/detención + historial rutas + correlación GPS-entregas |
| 21–22 | Algoritmo optimización rutas + asignación automática + ventanas horarias + replanificación dinámica |
| 23–24 | Tracking público clientes + notificaciones ETA + confirmación WhatsApp + encuesta post-entrega |

**Entrega**: GPS tiempo real para 200–1,000 vehículos, optimización rutas, tracking cliente, geocercas.

## FASE 4: IA Predictiva y Escalamiento (8–10 semanas)

**Objetivo**: Inteligencia artificial y escalabilidad enterprise.

| Semana | Entregables |
|---|---|
| 25–27 | Modelo ML predicción fallas + score salud vehículo + detección patrones anómalos |
| 28–30 | Algoritmo genético multi-objetivo + predicción tiempos entrega + detección fraude combustible |
| 31–33 | TimescaleDB + sharding + RabbitMQ cluster + workers + Elasticsearch |
| 34–36 | Dashboard sostenibilidad (huella carbono) + reportes ESG + recomendaciones eficiencia |

**Entrega**: IA predictiva, escalamiento 1,000–10,000+ vehículos, dashboard ESG, arquitectura enterprise.

---

# 24. Inteligencia Artificial

## Funciones de IA Propuestas

### 1. Predicción de Mantenimiento
Modelo de regresión que predice probabilidad de falla en 30 días basado en: historial de fallas, kilometraje, tipo de rutas, carga promedio, condiciones climáticas, conductor, marca/modelo.
- **Output**: Score de riesgo (0–100) + recomendación

### 2. Optimización de Rutas con ML
Modelo que aprende de patrones históricos de velocidad por hora/día/zona.
- Predicción de tiempo de viaje por tramo horario
- Ventanas óptimas de salida
- Ajuste dinámico durante la ruta

### 3. Detección de Anomalías en Combustible
Modelo no supervisado de clustering para identificar:
- Outliers en rendimiento (km/L)
- Cargas en horarios no habituales
- Consumo esperado vs. real por ruta
- Tasa de falsos positivos < 5%

### 4. Análisis Predictivo de Costos
Modelo de series temporales (Prophet) que proyecta:
- Costo combustible próximos 3 meses
- Costo mantenimiento esperado
- Escenarios what-if (ej. +10% combustible)

### 5. Asignación Inteligente de Conductores
Sistema de recomendación basado en:
- Score eficiencia combustible
- Historial infracciones/accidentes
- Límite legal de horas
- Experiencia en tipo de ruta
- Compatibilidad licencia-vehículo

## Arquitectura de IA

```
                    ZORVIAN IA ENGINE
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ Modelo       │  │ Modelo       │  │ Modelo       │
│ Mantenimiento│  │ Rutas        │  │ Anomalías    │
│ Predictivo   │  │ Optimización │  │ Combustible  │
└──────┬───────┘  └──────┬───────┘  └──────┬───────┘
       │                 │                 │
       ▼                 ▼                 ▼
┌─────────────────────────────────────────────────────┐
│           Feature Store (Redis + PostgreSQL)         │
└─────────────────────────────────────────────────────┘
       │                 │                 │
       ▼                 ▼                 ▼
┌─────────────────────────────────────────────────────┐
│      ML Pipeline (Python + ONNX Runtime)            │
│      Entrenamiento: semanal / Inferencia: API REST  │
└─────────────────────────────────────────────────────┘
```

## Stack de IA

| Componente | Tecnología | Propósito |
|---|---|---|
| Framework ML | scikit-learn, XGBoost, Prophet | Modelos tabulares y series temporales |
| Deep Learning | TensorFlow / PyTorch | Modelos complejos (LSTM) |
| Formato modelos | ONNX | Portable Python ↔ .NET |
| Serving | ONNX Runtime (.NET) | Inferencia sin Python |
| Orquestación | Apache Airflow | Reentrenamiento programado |
| Monitoreo | Prometheus + Grafana | Drift y performance |

---

# 25. Documento Final

## Resumen Ejecutivo Final

El **Módulo de Gestión de Flotas, Transporte y Logística** es una incorporación estratégica a Zorvian ERP que cierra una brecha crítica en la oferta del producto. Diseñado con la misma filosofía arquitectónica, visual y de negocio que el resto del ecosistema, este módulo permite gestionar integralmente la operación de transporte: desde el registro de vehículos y conductores, hasta la optimización de rutas con IA, pasando por control de combustible, mantenimiento predictivo, geolocalización en tiempo real, firma digital de entregas y reportes financieros.

Con 18 tablas transaccionales, 7 tablas catálogo, 14 secciones funcionales, 14 KPIs en dashboard, 19 automatizaciones, 20 integraciones con módulos existentes y un plan de 4 fases para escalar de 10 a 10,000+ vehículos, el módulo está preparado para competir con Fleetio, Odoo Fleet y SAP TM, ofreciendo la ventaja diferencial de ser un componente nativo de un ERP completo con cobertura LATAM, WhatsApp integrado e IA predictiva.

## Conclusiones

1. **Necesidad de mercado validada**: Empresas con flota en LATAM carecen de soluciones ERP que integren logística con contabilidad, nómina e inventario.
2. **Viabilidad técnica**: La arquitectura actual (Clean Architecture, multiempresa, Riverpod, PostgreSQL, RabbitMQ) es completamente extensible.
3. **Diferenciación competitiva**: WhatsApp, firma digital, detección de anomalías y cobertura LATAM son ventajas únicas.
4. **ROI claro**: El módulo se paga con reducción de costos operativos del 15–25%.
5. **Escalabilidad probada**: Soporta desde 10 hasta 10,000+ vehículos.

## Recomendaciones

1. Iniciar FASE 1 inmediatamente — el MVP cubre el 70% del mercado objetivo (PYMEs 5–30 vehículos)
2. Priorizar integración Ventas → Entregas (caso de uso más crítico)
3. Desarrollar app móvil en paralelo (experiencia del conductor es clave para adopción)
4. Invertir en detección de anomalías de combustible (ROI más rápido)
5. No retrasar GPS (FASE 3) — sin geolocalización el módulo se percibe incompleto
6. Partnership con Traccar (open source, gratuito, comunidad madura)

## Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Complejidad integración GPS multi-fabricante | Media | Alto | Traccar como middleware universal |
| Volumen GPS satura PostgreSQL | Media | Alto | TimescaleDB o particionamiento desde FASE 1 |
| Adopción de app móvil por conductores | Media | Medio | UI simple, offline-first, onboarding guiado |
| Precisión de detección de anomalías | Baja | Medio | Calibración por vehículo, umbrales configurables |
| Competencia de Odoo Fleet (gratuito) | Alta | Bajo | Enfoque en clientes que necesitan ERP completo, no solo flota |

## Oportunidades

- Mercado LATAM de logística: $1,200M TAM en Centroamérica
- Transformación digital de PYMEs: 70% aún usan Excel para gestión de flota
- Sostenibilidad: demanda creciente de reportes de huella de carbono
- IA predictiva: diferenciador frente a competidores que solo ofrecen tracking

## Prioridades de Desarrollo

1. **CRITICAL**: Vehículos + Conductores + Entregas (FASE 1, Semanas 1–6)
2. **HIGH**: Dashboard + Reportes básicos + Alertas (FASE 1, Semanas 7–8)
3. **HIGH**: Combustible + Mantenimiento + Taller (FASE 2, Semanas 9–10)
4. **MEDIUM**: Gastos + Integración contable/nómina (FASE 2, Semanas 11–12)
5. **MEDIUM**: GPS + Geocercas (FASE 3, Semanas 17–20)
6. **LOW**: Optimización rutas + IA predictiva (FASE 3–4, Semanas 21+)

## Valor Estratégico para Zorvian ERP

**Alto**. El módulo de flota representa una expansión natural del ERP hacia un segmento de mercado con alta demanda insatisfecha en LATAM. Agrega valor a clientes existentes (que ya tienen vehículos de reparto o flota de ventas) y abre la puerta a nuevos segmentos (logística, distribución, mensajería, servicios de campo).

## Nivel de Competitividad Esperado

| Contra | Nivel | Fundamento |
|---|---|---|
| Odoo Fleet | ⚔️ Competitivo | Inferior en precio (si usan Community), superior en integración LATAM |
| SAP TM | 🏆 Superior en PYME | No compite en el mismo segmento (SAP es enterprise caro) |
| Fleetio | ⚔️ Competitivo | Fleetio tiene mejor UI, Zorvian tiene ERP completo |
| Zoho Fleet | ✅ Ventaja | Zorvian cubre LATAM + multiempresa que Zoho no tiene |
| Excel/Google Sheets | 🏆 Destrucción total | Automatización vs. hojas de cálculo manuales |

## Impacto Comercial Esperado

- **Nuevos clientes**: +15–25% en pipeline de ventas (segmento logística/distribución)
- **Upsell a clientes existentes**: +20–30% de clientes actuales con flota propia
- **Precio premium**: Módulo permite aumentar el ticket promedio en 15–20%
- **Retención**: Reducción de churn al cubrir más necesidades del cliente con un solo proveedor
- **Tiempo de recuperación de inversión**: 6–9 meses post-lanzamiento FASE 1

## Evaluación Final del Módulo

| Criterio | Puntuación (0–100) | Comentario |
|---|---|---|
| Valor funcional | 92 | Cubre todas las necesidades operativas y gerenciales |
| Madurez del diseño | 88 | Arquitectura sólida, escalable, integrada |
| UX/UI | 85 | Consistente con Zorvian, moderna, profesional |
| Competitividad | 82 | Fuerte en LATAM, buena contra ERP globales |
| Viabilidad técnica | 90 | Stack probado, riesgos mitigados |
| ROI para cliente | 95 | Reducción de costos rápida y medible |
| Valor estratégico | 93 | Expande TAM, mejora retención, upselling |
| Innovación (IA) | 78 | Diferenciador a futuro, pero no crítico para MVP |
| **PROMEDIO PONDERADO** | **88/100** | **Módulo viable, rentable y estratégico** |

## Recomendación Final para Implementación

**APROBADO — PRIORIDAD ALTA**

Se recomienda iniciar la FASE 1 (MVP) de inmediato con un equipo dedicado de:
- 2 desarrolladores backend (.NET 9, EF Core, PostgreSQL)
- 2 desarrolladores frontend (Flutter, Riverpod, GoRouter)
- 1 diseñador UX/UI (conocimiento de logística)
- 1 QA (con cobertura de integraciones)
- 1 Product Owner (conocimiento del dominio logístico)

Costo estimado FASE 1: $60,000–$80,000 USD
Tiempo estimado FASE 1: 8–10 semanas
ROI esperado: Recuperación en 6–9 meses post-lanzamiento

El módulo posiciona a Zorvian ERP como la solución más completa para empresas con flota propia en Centroamérica y LATAM, cerrando la brecha con ERPs globales y ofreciendo una propuesta de valor única en el mercado regional.

---

> **Fin del Documento**
>
> Zorvian ERP — Módulo de Gestión de Flotas, Transporte y Logística
> Versión 1.0 — Junio 2026
> Clasificación: Interno / Cliente
