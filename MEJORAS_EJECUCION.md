# PLAN DE EJECUCIÓN DE MEJORAS - AUDITORÍA 2026

Este documento rastrea el progreso de las mejoras identificadas en la auditoría integral.

## Prioridad 1: Crítica (Antes del Lanzamiento)

- [x] **1.1 Validación Fiscal y Esquemas de Impuestos**: Implementar configuraciones predefinidas para Nicaragua, Costa Rica y Panamá.
    - [x] Investigar estructura actual de `TaxCategory` y `CountryTaxConfig`.
    - [x] Crear `ITaxCategoryRepository` y `TaxCategoryRepository`.
    - [x] Crear `FiscalService` con lógica para NIC, CRI y PAN.
    - [x] Integrar `FiscalService` en `CompanyService` y `SeedService`.
    - [x] Validar cálculos en `SaleService` y `PurchaseService` (Asegurar que usen las nuevas categorías y fallback a configuración de empresa).
- [x] **1.2 Pruebas de Carga y Estrés**: Validar middleware de multi-tenancy.
    - [x] Crear script de k6 o similar (implementado como test de integración en LoadTests.cs).
    - [x] Validar que no hay errores de DI bajo carga.

## Prioridad 2: Alta (Próximos 3 Meses)

## Prioridad 2: Alta (Próximos 3 Meses)

- [x] **2.1 Visual Pipeline CRM (Kanban)**:
    - [x] Refactorizar `QuoteStatus` a Enum y actualizar servicios.
    - [x] Crear vista de Kanban en Flutter para Oportunidades.
    - [x] Implementar drag-and-drop para cambio de estados.
    - [x] Añadir Widget Tests para Kanban.
- [x] **2.2 Report Designer Visual**:
    - [x] Potenciar `CustomReportService` y crear modelos frontend.
    - [x] Crear interfaz visual del diseñador de reportes.
    - [x] Añadir Widget Tests para Report Designer.

## Prioridad 3: Media

- [x] **3.1 Sincronización Offline Total**:
    - [x] Refactorizar `SyncEngine` a arquitectura genérica basada en repositorios.
    - [x] Implementar `ProductLocalRepository` con patrón genérico.
    - [x] Extender sincronización offline al módulo de Cotizaciones (`QuotesLocal`, `QuoteLocalRepository`).
    - [ ] Expansión a otros módulos (CRM, Ventas).

---
**Estado Actual**: Iniciando 1.1 Validación Fiscal.
