# Plan: Implementación de la Fase 3 - Metas e Incentivos

## Objetivo
Implementar el `GoalEngine` para la gestión automatizada de metas y su integración con los módulos existentes del ERP (CRM, Ventas, Inventario, Garantías, Servicios), junto con la interfaz de usuario para configuración y seguimiento.

## Alcance
- **Backend:** 
  - `GoalEngine` (Core: `GoalDefinition`, `GoalTracker`, `GoalEvaluator`).
  - Integración de eventos de dominio (`IntegrationService` para CRM, Sales, etc.).
- **Frontend:** 
  - Configurador de metas (Admin).
  - Dashboard de cumplimiento (General/Admin).
  - Portal "Mis Metas" (Empleado).

## Fases de Implementación

### Fase 3.1: Core `GoalEngine` (Backend)
- [ ] Definir Entidades (`GoalDefinition`, `GoalAssignment`, `GoalMetric`, `GoalResult`).
- [ ] Implementar `GoalEngine` (Lógica de evaluación de fórmulas, condiciones puerta, aceleradores).
- [ ] Crear `GoalService` (Gestión de metas, asignaciones y resultados).
- [ ] Implementar repositorio (`IGoalRepository`).

### Fase 3.2: Integración ERP (Backend)
- [ ] Crear `GoalIntegrationService` para escuchar eventos de otros módulos.
- [ ] Integrar eventos de:
  - CRM (Clientes nuevos).
  - Ventas (Facturación/Cobranza).
  - Inventario (Entregas).
  - Garantías (Casos resueltos).
  - Servicios (Órdenes completadas).

### Fase 3.3: Interfaz de Usuario (Frontend)
- [ ] Implementar configurador de metas (CRUD de `GoalDefinitions`).
- [ ] Implementar dashboard de cumplimiento (Visualización de `GoalResults`).
- [ ] Implementar portal "Mis Metas" (Vista de usuario final).

## Verificación
- **Backend:** Test unitarios para `GoalEngine` (cálculos) y tests de integración para `GoalIntegrationService` (flujo de eventos).
- **Frontend:** Test de componentes para el configurador y el dashboard.

## Riesgos y Consideraciones
- La complejidad de las fórmulas de metas puede requerir un motor de evaluación dinámico (p. ej., `NCalc` o similar).
- Es necesario asegurar que las integraciones sean asíncronas para no afectar el rendimiento del ERP.
