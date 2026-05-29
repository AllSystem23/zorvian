# Nexora — Plan de Implementacion y Seguimiento

**Inicio:** Junio 2026
**MVP Estimado:** Septiembre 2026
**Stack:** Flutter + ASP.NET Core 9 + PostgreSQL (Neon) + Firebase

---

## Convenciones

- [ ] **Pendiente**
- [~] **En progreso**
- [x] **Completado**
- [!] **Bloqueado**

Cada tarea incluye: `[Area]` | `Prioridad` | `Estimado` | `Depende de`

---

## Sprint 0 — Fundacion (Semana 1)

| # | Tarea | Area | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 0.1 | Crear repositorio GitHub con README y .gitignore (C#, Dart) | DevOps | Alta | 1h | — |
| 0.2 | Configurar proyecto Flutter con estructura de carpetas | Frontend | Alta | 2h | — |
| 0.3 | Configurar Clean Architecture .NET (Core, Application, Infrastructure, Web) | Backend | Alta | 3h | — |
| 0.4 | Crear proyecto Firebase (Auth, Hosting, Storage, FCM) | DevOps | Alta | 1h | — |
| 0.5 | Crear base de datos Neon + cadena de conexion | DB | Alta | 1h | — |
| 0.6 | Configurar Riverpod + GoRouter en Flutter | Frontend | Alta | 2h | — |
| 0.7 | Configurar EF Core + Npgsql + migracion inicial | Backend | Alta | 2h | 0.5 |
| 0.8 | Configurar Serilog, Swagger, CORS, Health Checks | Backend | Alta | 2h | 0.3 |
| 0.9 | Configurar Firebase Admin SDK en .NET | Backend | Alta | 1h | 0.4 |
| 0.10 | Desplegar API base a Render + Flutter Web a Firebase Hosting | DevOps | Alta | 2h | 0.2, 0.3 |
| 0.11 | Configurar GitHub Actions (build + test basico) | DevOps | Media | 2h | 0.1 |

**Milestone 1:** Repositorio funcional con API saludable en Render y Flutter en Firebase.
**Check:** `curl https://api.nexora.app/health` → 200 OK

---

## Sprint 1 — Auth y Multi-Tenant (Semana 2)

| # | Tarea | Area | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 1.1 | Crear entidades `User`, `Company`, `Role`, `UserRole`, `RolePermission` | Backend | Alta | 3h | 0.7 |
| 1.2 | Implementar `ITenantContext` + `TenantMiddleware` | Backend | Alta | 2h | 1.1 |
| 1.3 | Implementar query filters globales en DbContext | Backend | Alta | 1h | 1.2 |
| 1.4 | Endpoint `POST /auth/login` (Firebase idToken → JWT propio) | Backend | Alta | 3h | 0.9, 1.1 |
| 1.5 | Endpoint `POST /auth/refresh` | Backend | Alta | 2h | 1.4 |
| 1.6 | Endpoint `POST /auth/logout` | Backend | Media | 1h | 1.4 |
| 1.7 | JWT Validation Middleware con claims (tenant_id, role, permissions) | Backend | Alta | 2h | 1.4 |
| 1.8 | Pantalla de Login en Flutter con Firebase Auth | Frontend | Alta | 4h | 0.6 |
| 1.9 | AuthProvider + token storage + Dio interceptor para JWT | Frontend | Alta | 3h | 1.8 |
| 1.10 | Proteccion de rutas por rol en GoRouter | Frontend | Alta | 2h | 1.9 |
| 1.11 | Onboarding de empresa (registro inicial) | Ambos | Alta | 4h | 1.4, 1.8 |
| 1.12 | Tests de integracion de auth | QA | Media | 3h | 1.4 |

**Milestone 2:** Usuario puede registrarse, iniciar sesion y recibir JWT con contexto de tenant.

---

## Sprint 2 — Empleados y Departamentos (Semana 3)

| # | Tarea | Area | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 2.1 | Entidades `Employee`, `Department`, `EmployeeSupervisor`, `EmployeeDocument` | Backend | Alta | 3h | 1.1 |
| 2.2 | `LeaveBalances` + `EmployeeHistory` entidades | Backend | Alta | 2h | 2.1 |
| 2.3 | CRUD Employees (CreateEmployeeCommand, GetEmployeeQuery, etc.) | Backend | Alta | 6h | 1.7, 2.1 |
| 2.4 | CRUD Departments | Backend | Alta | 3h | 2.1 |
| 2.5 | EmployeeController REST (GET, POST, PUT, DELETE) | Backend | Alta | 3h | 2.3 |
| 2.6 | DepartmentController REST | Backend | Alta | 2h | 2.4 |
| 2.7 | Importacion masiva desde Excel (ClosedXML) | Backend | Media | 4h | 2.3 |
| 2.8 | Pantallas de listado de empleados (DataTable con filtros) | Frontend | Alta | 6h | 1.9 |
| 2.9 | Formulario de creacion/edicion de empleado | Frontend | Alta | 4h | 2.8 |
| 2.10 | Pagina de detalle de empleado con historial | Frontend | Alta | 4h | 2.9 |
| 2.11 | Gestion de departamentos (CRUD UI) | Frontend | Media | 3h | 2.9 |
| 2.12 | Validaciones con FluentValidation en todos los DTOs | Backend | Alta | 2h | 2.3 |
| 2.13 | Seed data para desarrollo (empresa demo + 20 empleados) | Backend | Media | 2h | 2.1 |

**Milestone 3:** CRUD completo de empleados con UI funcional, filtros y validaciones.

---

## Sprint 3 — Modulo de Vacaciones (Semana 4–5)

| # | Tarea | Area | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 3.1 | Entidad `VacationRequest` + `ApprovalFlow` | Backend | Alta | 2h | 2.1 |
| 3.2 | Servicio de calculo de saldo de vacaciones | Backend | Alta | 4h | 3.1 |
| 3.3 | Logica de acumulacion automatica (cron job mensual con Hangfire) | Backend | Alta | 3h | 3.2 |
| 3.4 | `POST /vacations` con validacion de saldo, fechas, reglas 30% equipo | Backend | Alta | 4h | 1.7, 3.1 |
| 3.5 | `GET /vacations` con filtros y paginacion | Backend | Alta | 2h | 3.1 |
| 3.6 | `PUT /vacations/{id}/approve` + `PUT /vacations/{id}/reject` | Backend | Alta | 3h | 3.4 |
| 3.7 | `GET /vacations/balance` con calculo en tiempo real | Backend | Alta | 2h | 3.2 |
| 3.8 | ApprovalFlowEngine (cadena supervisor → RRHH → Admin configurable) | Backend | Alta | 4h | 3.6, 1.7 |
| 3.9 | Validacion de no solapamiento con equipo (>30%) | Backend | Alta | 3h | 3.4 |
| 3.10 | Pantalla "Solicitar Vacaciones" con calendario y saldo visible | Frontend | Alta | 6h | 2.9 |
| 3.11 | Pantalla "Mis Vacaciones" con historial | Frontend | Alta | 3h | 3.10 |
| 3.12 | Panel de aprobaciones para supervisores/RRHH | Frontend | Alta | 5h | 3.11 |
| 3.13 | Calendario de vacaciones del equipo | Frontend | Media | 4h | 3.12 |
| 3.14 | Tests unitarios del VacationService | QA | Alta | 3h | 3.2 |
| 3.15 | Tests de integracion del flujo completo vacaciones | QA | Alta | 3h | 3.4 |

**Milestone 4:** Empleado solicita vacaciones, supervisor aprueba, RRHH confirma, saldo se descuenta.

---

## Sprint 4 — Modulo de Permisos (Semana 6–7)

| # | Tarea | Area | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 4.1 | Entidad `LeaveType` + seed de tipos predefinidos | Backend | Alta | 2h | 2.1 |
| 4.2 | Entidad `PermissionRequest` | Backend | Alta | 2h | 4.1 |
| 4.3 | `GET /permissions/types` (tipos configurables por tenant) | Backend | Alta | 2h | 1.7, 4.1 |
| 4.4 | `POST /permissions` con validaciones por tipo de permiso | Backend | Alta | 4h | 1.7, 4.2 |
| 4.5 | `PUT /permissions/{id}/approve` + `reject` | Backend | Alta | 2h | 4.4 |
| 4.6 | Subida de adjuntos a Firebase Storage + validacion (PDF/JPG, 10MB) | Backend | Alta | 3h | 4.4 |
| 4.7 | Logica de reglas por tipo (limites, adjunto obligatorio, pago) | Backend | Alta | 3h | 4.1 |
| 4.8 | Pantalla "Solicitar Permiso" con selector de tipo y subida de archivo | Frontend | Alta | 6h | 3.10 |
| 4.9 | Pantalla "Mis Permisos" con historial y estados | Frontend | Alta | 3h | 4.8 |
| 4.10 | Panel de aprobacion de permisos | Frontend | Alta | 3h | 4.9 |
| 4.11 | Vista de adjuntos con visor PDF/imagen | Frontend | Alta | 3h | 4.8 |
| 4.12 | Reglas de negocio: maternidad automatica, paternidad, personales | Backend | Alta | 4h | 4.7 |
| 4.13 | Tests del modulo de permisos | QA | Alta | 3h | 4.4 |

**Milestone 5:** Permisos funcionales con 10 tipos, adjuntos, reglas laborales por pais.

---

## Sprint 5 — Portal del Empleado + Dashboard (Semana 8–9)

| # | Tarea | Area | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 5.1 | Pantalla de perfil del empleado (datos personales, foto, documentos) | Frontend | Alta | 4h | 2.10 |
| 5.2 | Edicion de perfil por el empleado (campos permitidos) | Frontend | Alta | 3h | 5.1 |
| 5.3 | Dashboard KPI service en backend | Backend | Alta | 4h | 2.3 |
| 5.4 | `GET /dashboard/kpis` endpoint | Backend | Alta | 2h | 5.3 |
| 5.5 | `GET /dashboard/vacation-calendar` endpoint | Backend | Alta | 2h | 3.1 |
| 5.6 | `GET /dashboard/recent-requests` endpoint | Backend | Alta | 1h | 4.2 |
| 5.7 | Widgets del dashboard (headcount, ausentismo, graficos) | Frontend | Alta | 8h | 5.4 |
| 5.8 | Calendario interactivo de ausencias | Frontend | Alta | 6h | 5.5 |
| 5.9 | Tarjetas de alerta (cumpleanos, aniversarios, pendientes) | Frontend | Media | 3h | 5.7 |
| 5.10 | Notificaciones en UI (campana con contador + dropdown) | Frontend | Alta | 4h | 5.1 |
| 5.11 | Descarga de constancias (carta laboral, ingresos) | Ambos | Media | 4h | 2.3 |

**Milestone 6:** Empleado tiene portal completo. Admin ve dashboard con KPIs en tiempo real.

---

## Sprint 6 — Asistencia + Notificaciones (Semana 10–11)

| # | Tarea | Area | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 6.1 | Entidad `AttendanceRecord` | Backend | Alta | 1h | 2.1 |
| 6.2 | `POST /attendance/check-in` con geolocalizacion | Backend | Alta | 3h | 1.7, 6.1 |
| 6.3 | `POST /attendance/check-out` | Backend | Alta | 1h | 6.2 |
| 6.4 | `GET /attendance/my` con resumen mensual | Backend | Alta | 2h | 6.1 |
| 6.5 | Logica de calculo de retardos, horas extra, tolerancia | Backend | Alta | 3h | 6.2 |
| 6.6 | MArcaje con QR (generacion + lectura) | Backend | Media | 4h | 6.2 |
| 6.7 | Pantalla de marcaje con reloj en vivo y mapa | Frontend | Alta | 4h | 6.2 |
| 6.8 | Historial de asistencia con graficos semanales | Frontend | Alta | 4h | 6.4 |
| 6.9 | Configuracion de SignalR Hub en .NET | Backend | Alta | 2h | 0.3 |
| 6.10 | Cliente SignalR en Flutter | Frontend | Alta | 3h | 6.9 |
| 6.11 | Servicio de notificaciones FCM en .NET | Backend | Alta | 3h | 0.9 |
| 6.12 | Integracion de flutter_local_notifications | Frontend | Alta | 2h | 6.11 |
| 6.13 | Eventos de notificacion (vacaciones, permisos, alertas) | Backend | Alta | 4h | 6.9, 6.11 |
| 6.14 | Cron job de recordatorio de marcaje (Hangfire 9AM) | Backend | Media | 2h | 6.2 |
| 6.15 | Tests de asistencia | QA | Alta | 3h | 6.2 |
| 6.16 | Tests de notificaciones | QA | Media | 2h | 6.11 |

**Milestone 7:** Asistencia funcional con geolocalizacion. Notificaciones push + tiempo real operativas.

---

## Sprint 7 — Reportes + Auditoria (Semana 12–13)

| # | Tarea | Area | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 7.1 | Entidad `AuditLog` + AuditMiddleware | Backend | Alta | 4h | 1.7 |
| 7.2 | Decorador `[Audit]` para acciones especificas | Backend | Alta | 2h | 7.1 |
| 7.3 | `GET /audit/logs` con filtros (fecha, accion, usuario) | Backend | Media | 2h | 7.1 |
| 7.4 | Servicio de reportes con ClosedXML (Excel) | Backend | Alta | 4h | 2.3 |
| 7.5 | Servicio de reportes con QuestPDF (PDF) | Backend | Alta | 4h | 7.4 |
| 7.6 | `POST /reports/generate` (asincrono con Hangfire) | Backend | Alta | 3h | 7.4 |
| 7.7 | `GET /reports/{id}/download` | Backend | Alta | 1h | 7.6 |
| 7.8 | Reporte: Nomina de vacaciones | Backend | Alta | 2h | 7.4 |
| 7.9 | Reporte: Historial de permisos | Backend | Alta | 2h | 7.4 |
| 7.10 | Reporte: Asistencia mensual | Backend | Alta | 2h | 6.4 |
| 7.11 | Reporte: Control de saldos | Backend | Media | 2h | 7.4 |
| 7.12 | Pantalla de reportes con selector de tipo/filtros | Frontend | Alta | 4h | 7.6 |
| 7.13 | Vista de logs de auditoria para Admin | Frontend | Media | 3h | 7.3 |
| 7.14 | Exportacion directa de tablas a Excel/CSV | Frontend | Media | 2h | 7.4 |
| 7.15 | Tests de reportes | QA | Alta | 3h | 7.4 |

**Milestone 8:** Auditoria completa de todas las acciones. 4 reportes exportables.

---

## Sprint 8 — Admin + Configuracion (Semana 14)

| # | Tarea | Area | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 8.1 | `GET/PUT /company/settings` | Backend | Alta | 3h | 1.7 |
| 8.2 | Servicio de configuracion regional (pais, moneda, zona horaria) | Backend | Alta | 2h | 8.1 |
| 8.3 | Configuracion de flujo de aprobacion (JSON configurable) | Backend | Alta | 3h | 8.1 |
| 8.4 | Gestion de tipos de permiso por empresa | Backend | Alta | 3h | 4.1 |
| 8.5 | Pantalla de configuracion de empresa | Frontend | Alta | 4h | 8.1 |
| 8.6 | Gestion de usuarios y roles (invitar, asignar rol) | Backend | Alta | 4h | 1.4 |
| 8.7 | Pantalla de gestion de usuarios | Frontend | Alta | 4h | 8.6 |
| 8.8 | Gestion de supervisores (asignacion masiva) | Frontend | Media | 3h | 2.9 |
| 8.9 | Perfil de empresa (logo, nombre legal, RUC) | Frontend | Media | 2h | 8.5 |
| 8.10 | Tests de configuracion | QA | Media | 2h | 8.1 |

**Milestone 9:** Admin puede configurar la empresa completamente desde el panel.

---

## Sprint 9 — Pulido y Calidad (Semana 15–16)

| # | Tarea | Area | Prioridad | Est. | Depende |
|---|---|---|---|---|---|
| 9.1 | Dark mode completo en Flutter | Frontend | Media | 4h | 0.6 |
| 9.2 | Responsive design: ajustes para tablet y mobile | Frontend | Alta | 6h | — |
| 9.3 | Command Palette (Cmd+K) | Frontend | Baja | 4h | — |
| 9.4 | Empty states, skeleton loaders, animaciones | Frontend | Media | 4h | — |
| 9.5 | Manejo global de errores en Flutter | Frontend | Alta | 3h | — |
| 9.6 | Pantallas de carga y transiciones | Frontend | Media | 2h | — |
| 9.7 | Pruebas de integracion end-to-end (flujos criticos) | QA | Alta | 8h | Todos |
| 9.8 | Pruebas de carga (k6 o similar) en endpoints criticos | QA | Media | 4h | — |
| 9.9 | Auditoria de seguridad (OWASP Top 10) | QA | Alta | 4h | — |
| 9.10 | Optimizacion de consultas SQL + indices faltantes | DB | Alta | 4h | — |
| 9.11 | Documentacion de API con Swagger comentada | Docs | Alta | 3h | — |
| 9.12 | README tecnico para desarrolladores | Docs | Alta | 2h | — |

**Milestone 10:** MVP listo para produccion con calidad enterprise.

---

## Post-MVP — Fase 2 (Q4 2026)

| # | Tarea | Prioridad | Depende |
|---|---|---|---|
| F2.1 | Modulo de nomina basica (salarios, deducciones, INSS/IR) | Alta | 2.3 |
| F2.2 | Integracion con dispositivos biometricos (huella, rostro) | Alta | 6.2 |
| F2.3 | Kiosko (app tablet para marcaje en sucursales) | Alta | 6.2 |
| F2.4 | Archivos ACH para pago de nomina | Media | F2.1 |
| F2.5 | API Publica + Webhooks para integraciones | Media | — |
| F2.6 | Multi-idioma (ingles) | Media | — |
| F2.7 | Administracion de documentos avanzada (vencimientos, alertas) | Baja | 2.10 |

## Post-MVP — Fase 3 (Q1–Q2 2027)

| # | Tarea | Prioridad | Depende |
|---|---|---|---|
| F3.1 | OCR de certificados medicos (Tesseract + Google Vision) | Alta | 4.6 |
| F3.2 | Chatbot RRHH con RAG + LLM | Alta | — |
| F3.3 | Prediccion de ausentismo (XGBoost) | Media | 6.1 |
| F3.4 | Recomendacion automatica de fechas de vacaciones | Media | 3.2 |
| F3.5 | Evaluacion de desempeno con OKRs | Media | 2.3 |

---

## Resumen de Milestones

| # | Milestone | Sprint | Fecha Objetivo |
|---|---|---|---|
| M1 | Repositorio + API saludable + Flutter en Firebase | S0 | Semana 1 |
| M2 | Auth completo (login, JWT, multi-tenant) | S1 | Semana 2 |
| M3 | CRUD empleados + departamentos funcional | S2 | Semana 3 |
| M4 | Ciclo completo de vacaciones | S3 | Semana 4-5 |
| M5 | Modulo de permisos con adjuntos | S4 | Semana 6-7 |
| M6 | Portal empleado + dashboard KPIs | S5 | Semana 8-9 |
| M7 | Asistencia + notificaciones push/tiempo real | S6 | Semana 10-11 |
| M8 | Reportes + auditoria | S7 | Semana 12-13 |
| M9 | Admin + configuracion empresarial | S8 | Semana 14 |
| M10 | MVP listo para produccion | S9 | Semana 15-16 |

---

## Registro de Decisiones Tecnicas (ADR)

| Fecha | Decision | Opcion Elegida | Alternativa | Sprint |
|---|---|---|---|---|
| — | State Management | Riverpod 2.x | Bloc, GetX | S0 |
| — | ORM | EF Core + Npgsql | Dapper, NHibernate | S0 |
| — | Auth | Firebase Auth + JWT propio | Auth0, IdentityServer | S1 |
| — | Cache (futuro) | Redis | In-memory, Memcached | — |
| — | Background Jobs | Hangfire | Quartz.NET, Azure Functions | S3 |

*(Completar durante el desarrollo)*

---

## Notas

- Las estimaciones son en horas de desarrollo real, no horas calendario.
- Multiplicar por ~1.5 para incluir reuniones, revisiones y bugs.
- Los sprints asumen 1 desarrollador full-stack. Con 2 personas, los tiempos se reducen ~40%.
- Marcar tareas con [~] cuando se inicien y [x] solo cuando pasen code review + tests.
