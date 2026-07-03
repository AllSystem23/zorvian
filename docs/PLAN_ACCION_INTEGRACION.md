# Plan de Acción — Integración y Completitud del Sistema

> **Fecha:** 2026-07-03
> **Contexto:** Auditoría módulo por módulo tras migración `PendingModelChanges_Jul2026`
> **Objetivo:** Cerrar todos los gaps de integración frontend↔backend, corregir bugs críticos y completar features faltantes

---

## 🔴 CRÍTICOS — Prioridad Inmediata

### C-1: Crear `OpportunitiesController` (CRM)
| Ítem | Detalle |
|------|---------|
| **Síntoma** | Oportunidades no funcionan — todas las llamadas del frontend dan 404 |
| **Causa** | Service, Repository, DTOs existen pero **no hay controller** REST |
| **Acción** | Crear `OpportunitiesController` con endpoints: `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`, `GET /by-pipeline-stage/{stageId}` |
| **Archivos** | `src/Zorvian.Web/Controllers/OpportunitiesController.cs` (nuevo) |
| **Verificación** | `curl GET /zorvian/v1/crm/opportunities` → 200 |
| **Estado** | ⬜ Pendiente |

### C-2: Corregir path duplicado en `leads_provider.dart` (CRM)
| Ítem | Detalle |
|------|---------|
| **Síntoma** | `updateLead()` y `deleteLead()` fallan con URL duplicada |
| **Causa** | Usan `zorvian/v1/crm/leads/$id` pero `DioClient.baseUrl` ya incluye `/zorvian/v1` |
| **Acción** | Cambiar paths a `crm/leads/$id` |
| **Archivos** | `frontend/lib/features/crm/providers/leads_provider.dart` |
| **Verificación** | PUT/DELETE leads responde 200 |
| **Estado** | ⬜ Pendiente |

### C-3: Fix `CheckIssuancePage` — enviar GUID real como `payeeId` (Tesorería)
| Ítem | Detalle |
|------|---------|
| **Síntoma** | Al emitir cheque se envía el nombre del beneficiario donde el backend espera un GUID → 400 |
| **Causa** | `_beneficiaryController.text` se envía como `payeeId` |
| **Acción** | Agregar selector de beneficiario (cliente/proveedor/empleado) que envíe el GUID correspondiente, no el nombre |
| **Archivos** | `frontend/lib/features/treasury/pages/check_issuance_page.dart` |
| **Verificación** | POST `/treasury/checks/issue` con GUID válido → 200 |
| **Estado** | ⬜ Pendiente |

### C-4: Fix `CheckRepository.GetAllAsync` — agregar tenant scoping (Tesorería)
| Ítem | Detalle |
|------|---------|
| **Síntoma** | Cheques de todas las empresas visibles sin filtro |
| **Causa** | `GetAllAsync()` no aplica `Where(c => c.CompanyId == companyId)` |
| **Acción** | Agregar filtro por `CompanyId` en la query |
| **Archivos** | `src/Zorvian.Infrastructure/Repositories/CheckRepository.cs` |
| **Verificación** | Tests de integración con multi-tenant verifican aislamiento |
| **Estado** | ⬜ Pendiente |

### C-5: Crear `BankAccountsController` corporativo vs renombrar existente (Tesorería)
| Ítem | Detalle |
|------|---------|
| **Síntoma** | `BankAccountsController` en realidad maneja `EmployeeBankAccount`, no cuentas bancarias de la empresa |
| **Causa** | Confusión de naming: el controller debería llamarse `EmployeeBankAccountsController` |
| **Acción** | Renombrar controller existente a `EmployeeBankAccountsController` y crear nuevo `BankAccountsController` para cuentas corporativas |
| **Archivos** | `src/Zorvian.Web/Controllers/BankAccountsController.cs`, nuevo controller |
| **Verificación** | Ambos controllers funcionan con endpoints separados |
| **Estado** | ⬜ Pendiente |

### C-6: Crear páginas frontend faltantes para `/payroll/salaries` y `/payroll/deduction-types` (RRHH)
| Ítem | Detalle |
|------|---------|
| **Síntoma** | Rutas existen en `router.dart` pero archivos `.dart` no existen → 404 |
| **Causa** | Las páginas nunca fueron creadas |
| **Acción** | Crear `salaries_page.dart` y `deduction_types_page.dart` con formularios CRUD |
| **Archivos** | `frontend/lib/features/payroll/pages/salaries_page.dart` (nuevo), `frontend/lib/features/payroll/pages/deduction_types_page.dart` (nuevo) |
| **Verificación** | Navegación a `/payroll/salaries` y `/payroll/deduction-types` renderiza sin error |
| **Estado** | ⬜ Pendiente |

---

## 🟡 IMPORTANTES — Prioridad Alta

### I-1: Agregar `/purchases/*` al NavConfig (Compras)
| Ítem | Detalle |
|------|---------|
| **Descripción** | Las rutas `/purchases`, `/purchases/new`, `/:id` existen en router pero no tienen NavItem en sidebar |
| **Acción** | Agregar NavItem "Facturas de Compra" → `/purchases` en módulo `Compras` |
| **Archivos** | `frontend/lib/core/navigation/nav_config.dart` |
| **Estado** | ⬜ Pendiente |

### I-2: Agregar role restrictions a treasury sub-routes (Integración)
| Ítem | Detalle |
|------|---------|
| **Descripción** | `/treasury/checks`, `/transfers`, `/deposits`, `/commissions`, `/collections` no están en `_routeRoles` → cualquier Employee puede acceder |
| **Acción** | Agregar entrada explícita en `_routeRoles` para cada sub-ruta con roles: `SuperAdmin, CompanyAdmin, Accountant` |
| **Archivos** | `frontend/lib/app/router.dart` |
| **Estado** | ⬜ Pendiente |

### I-3: Agregar role restrictions a `/pos`, `/crm`, `/chat`
| Ítem | Detalle |
|------|---------|
| **Descripción** | Mismo problema: sin entrada en `_routeRoles` → acceso Employee por defecto |
| **Acción** | Agregar roles: POS (`SuperAdmin, CompanyAdmin, Supervisor, Employee`), CRM (`SuperAdmin, CompanyAdmin, Supervisor`), Chat (`SuperAdmin, CompanyAdmin, Employee`) |
| **Archivos** | `frontend/lib/app/router.dart` |
| **Estado** | ⬜ Pendiente |

### I-4: Reemplazar `TextEditingController` por `ZSelect` en treasury forms (Tesorería)
| Ítem | Detalle |
|------|---------|
| **Descripción** | `bank_deposit_page.dart`, `bank_commission_page.dart`, `bank_collection_page.dart` usan campos de texto para GUIDs |
| **Acción** | Reemplazar con `ZSelect` que cargue lista de cuentas bancarias desde el backend |
| **Archivos** | `frontend/lib/features/treasury/pages/bank_deposit_page.dart`, `bank_commission_page.dart`, `bank_collection_page.dart` |
| **Estado** | ⬜ Pendiente |

### I-5: Eliminar/refactorizar `CrmService.cs` stub (CRM)
| Ítem | Detalle |
|------|---------|
| **Descripción** | `CrmService.cs` tiene métodos que retornan datos vacíos, no usa repositorio, compite con `OpportunityService` |
| **Acción** | Eliminar `CrmService.cs` y migrar cualquier funcionalidad válida a `OpportunityService` |
| **Archivos** | `src/Zorvian.Application/Services/CrmService.cs`, `src/Zorvian.Application/Interfaces/ICrmService.cs` |
| **Estado** | ⬜ Pendiente |

### I-6: Crear UI para Sick Leave (RRHH)
| Ítem | Detalle |
|------|---------|
| **Descripción** | Backend tiene controller + service completo pero cero frontend |
| **Acción** | Crear páginas: sick leave list, form, detail + provider + rutas + NavItem |
| **Archivos** | Nuevos en `frontend/lib/features/sick_leave/` |
| **Estado** | ⬜ Pendiente |

### I-7: Agregar nuevos campos de Employee al formulario frontend (RRHH)
| Ítem | Detalle |
|------|---------|
| **Descripción** | `EmployeeFormPage` no incluye `DeductInss`, `DeductIr`, `DeductAguinaldo`, `IsTrustPosition`, `IsDomesticWorkerWithBoard` |
| **Acción** | Agregar campos faltantes al formulario de empleado |
| **Archivos** | `frontend/lib/features/employees/pages/employee_form_page.dart` |
| **Estado** | ⬜ Pendiente |

### I-8: Fix `SettlementService` — usar `DioClient` inyectado en vez de `new Dio()` (RRHH)
| Ítem | Detalle |
|------|---------|
| **Descripción** | Crea instancia propia de `Dio`, bypassando auth interceptors |
| **Acción** | Inyectar `dioClientProvider` en el provider |
| **Archivos** | `frontend/lib/features/payroll/services/settlement_service.dart` |
| **Estado** | ⬜ Pendiente |

### I-9: Fix `SettlementFormPage` — eliminar dependencia de `dart:html` (RRHH)
| Ítem | Detalle |
|------|---------|
| **Descripción** | Usa `universal_html` que es incompatible con mobile/web |
| **Acción** | Reemplazar con `file_saver_service` existente |
| **Archivos** | `frontend/lib/features/payroll/pages/settlement_form_page.dart` |
| **Estado** | ⬜ Pendiente |

### I-10: Extender RLS a tablas faltantes (Admin)
| Ítem | Detalle |
|------|---------|
| **Descripción** | Solo 14 tablas con RLS; Fleet, Payroll, Goals, Budgets y ~20 más no tienen políticas |
| **Acción** | Generar script RLS para todas las tablas con `TenantId`/`CompanyId` |
| **Archivos** | `docs/SECURITY_RLS.sql`, nueva migración |
| **Estado** | ⬜ Pendiente |

### I-11: Crear UI para predicciones ML (IA)
| Ítem | Detalle |
|------|---------|
| **Descripción** | Sales predictions, expense classification, purchase recommendations, absenteeism prediction, vacation recommendations existen en backend sin frontend |
| **Acción** | Crear páginas/dashboards para cada feature o integrar en dashboards existentes |
| **Archivos** | Múltiples en `frontend/lib/features/` |
| **Estado** | ⬜ Pendiente |

### I-12: Corregir path duplicado en `opportunities_provider.dart`
| Ítem | Detalle |
|------|---------|
| **Descripción** | `updateOpportunityStage()`, `updateOpportunity()`, `deleteOpportunity()` usan `zorvian/v1/crm/opportunities/$id` |
| **Acción** | Cambiar a `crm/opportunities/$id` (depende de C-1) |
| **Archivos** | `frontend/lib/features/crm/providers/opportunities_provider.dart` |
| **Estado** | ⬜ Pendiente (bloqueado por C-1) |

---

## 🟢 MEJORAS — Prioridad Media

### M-1: Agregar tests a módulos sin cobertura
| Módulo | Tests faltantes | Prioridad |
|--------|----------------|-----------|
| Ventas | SaleService, CreditService, CreditNoteService | Alta |
| Inventario | ProductService, InventoryMovementService | Media |
| Compras | PurchaseService, PurchaseOrderService, SupplierService | Media |
| Flota | Todos (0 tests actualmente) | Baja |
| IA | SalesPredictionService, FinancialAssistantService | Media |

### M-2: Agregar `FluentValidation` a requests faltantes
| Request | Módulo |
|---------|--------|
| `CreateQuoteRequest` | Ventas |
| `CreateCreditRequest` | Ventas |
| `CreateCreditNoteRequest` | Ventas |
| `CreateSupplierPaymentRequest` | Compras |

### M-3: Agregar frontend para Supplier Payments y Supplier Credit Notes (Compras)
| Ítem | Detalle |
|------|---------|
| **Backend** | Controllers y servicios existen |
| **Frontend** | Sin páginas |
| **Acción** | Crear páginas de pago a proveedores y notas de crédito de proveedor |

### M-4: Eliminar `crm_provider.dart` deprecated (CRM)
| Ítem | Detalle |
|------|---------|
| **Descripción** | Marcado `@deprecated`, llama a API de clients, sin uso en widgets actuales |
| **Acción** | Eliminar archivo y referencias |
| **Archivos** | `frontend/lib/features/crm/providers/crm_provider.dart` |

### M-5: Agregar validators de Quote y Credit (Backend)
| Ítem | Detalle |
|------|---------|
| **Descripción** | Existe placeholder `CreditValidators.cs` vacío; Quote validators no existen |
| **Acción** | Implementar validación FluentValidation |
| **Archivos** | `src/Zorvian.Web/Validators/` |

### M-6: Refactorizar contraseñas hardcodeadas (SickLeaveService)
| Ítem | Detalle |
|------|---------|
| **Descripción** | `SickLeaveService.CreateAsync` usa `dailyWage = 100m` hardcodeado |
| **Acción** | Obtener salario real desde `EmployeeSalary` |
| **Archivos** | `src/Zorvian.Application/Services/SickLeaveService.cs` |

### M-7: Refactorizar constructores múltiples de `AutoAccountingService`
| Ítem | Detalle |
|------|---------|
| **Descripción** | 3 constructores frágiles (DI parameterless vs legacy test vs full DI) |
| **Acción** | Unificar a un solo constructor con todas las dependencias |

### M-8: Agregar RLS restante para Fleet, Payroll, Goals, Budgets
| Ítem | Detalle |
|------|---------|
| **Acción** | Generar script RLS para todas las tablas y crear migración |

### M-9: Unificar `TerminationReason` y `TerminationType` (RRHH)
| Ítem | Detalle |
|------|---------|
| **Descripción** | Dos enums con valores ligeramente diferentes causan confusión |
| **Acción** | Unificar en un solo enum o mapear explícitamente |

---

## 📐 ARQUITECTURA — Deuda Técnica

### A-1: `CompanyPlanPricing` debería heredar de `BaseEntity`
| Ítem | Detalle |
|------|---------|
| **Riesgo** | Omite TenantId, CreatedBy, soft-delete fields |
| **Acción** | Refactorizar para heredar de BaseEntity + migración |

### A-2: EntryNumber generation no es thread-safe
| Ítem | Detalle |
|------|---------|
| **Riesgo** | `CountAsync + 1` puede duplicar números bajo concurrencia |
| **Acción** | Usar secuencia PostgreSQL o `Interlocked` |

### A-3: `PermissionsController` nombre confuso
| Ítem | Detalle |
|------|---------|
| **Descripción** | Controla permisos de RRHH (leave), no RBAC |
| **Acción** | Renombrar a `LeavePermissionsController` |

---

## 📈 Progreso General

| Fase | Issues | Completados | % |
|------|--------|:-----------:|:-:|
| 🔴 Críticos | 6 | 0 | 0% |
| 🟡 Importantes | 12 | 0 | 0% |
| 🟢 Mejoras | 9 | 0 | 0% |
| 📐 Deuda Técnica | 3 | 0 | 0% |
| **Total** | **30** | **0** | **0%** |

---

## 📋 Checklist de Verificación por Módulo

### CRM
- [ ] C-1: Crear `OpportunitiesController`
- [ ] C-2: Fix path leads_provider
- [ ] I-5: Eliminar CrmService stub
- [ ] I-12: Fix path opportunities_provider
- [ ] M-4: Eliminar crm_provider deprecated

### Ventas
- [ ] M-1: Agregar tests
- [ ] M-2: Agregar validators (Quote, Credit, CreditNote)

### Inventario / Compras
- [ ] I-1: Agregar /purchases al NavConfig
- [ ] M-1: Agregar tests
- [ ] M-2: Agregar validators (SupplierPayment)
- [ ] M-3: UI para Supplier Payments y Supplier Credit Notes

### Tesorería
- [ ] C-3: Fix CheckIssuancePage
- [ ] C-4: Fix CheckRepository tenant scoping
- [ ] C-5: Renombrar/crear BankAccountsController
- [ ] I-4: Reemplazar TextEditingController por ZSelect
- [ ] I-2: Role restrictions treasury routes

### RRHH
- [ ] C-6: Crear salaries_page.dart y deduction_types_page.dart
- [ ] I-6: UI para Sick Leave
- [ ] I-7: Agregar campos faltantes a EmployeeForm
- [ ] I-8: Fix SettlementService (Dio)
- [ ] I-9: Fix SettlementFormPage (dart:html)

### IA / Inteligencia
- [ ] I-11: UI para predicciones ML

### Admin / Seguridad
- [ ] I-10: Extender RLS
- [ ] I-3: Role restrictions para /pos, /crm, /chat

### Integración General
- [ ] I-2: Role restrictions treasury sub-routes
- [ ] I-3: Role restrictions /pos, /crm, /chat

---

## 🧪 Estrategia de Verificación

Cada issue debe verificarse con:
1. **Build** → `dotnet build` 0 errores
2. **Tests backend** → `dotnet test` 100% passing
3. **Analyze frontend** → `flutter analyze` 0 errors
4. **Tests frontend** → `flutter test` 100% passing
5. **Migraciones** → `dotnet ef migrations has-pending-model-changes` → "No changes"
6. **Push** → `git push` exitoso
