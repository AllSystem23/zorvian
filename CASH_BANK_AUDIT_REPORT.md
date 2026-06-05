# INFORME DE AUDITORÍA Y PLAN DE MEJORA - MÓDULO DE CAJA Y BANCOS

**Fecha:** 4 de Junio, 2026
**Auditor:** Especialista en Arquitectura ERP / Auditoría Financiera
**Alcance:** Módulo de Caja, Bancos, Conciliación, y Monedas.

---

## 1. RESUMEN EJECUTIVO
El módulo de Caja y Bancos actual funciona como un registro transaccional básico, pero carece de controles fundamentales para un sistema de grado empresarial. Se detectaron riesgos significativos de fraude y errores contables debido a la falta de segregación de funciones, ausencia de conciliación automática y trazabilidad documental deficiente.

---

## 2. DIAGNÓSTICO DE RIESGOS

| Proceso | Hallazgo | Riesgo de Fraude |
|---|---|---|
| **Cierres de Caja** | Basado en saldo de sistema | Alto: Manipulación de efectivo o descuadres intencionales. |
| **Ingresos/Egresos** | Sin soporte documental adjunto | Medio: Fácil duplicación o registros ficticios. |
| **Conciliación** | Inexistente | Alto: Ocultamiento de desvíos de fondos bancarios. |
| **Segregación** | No definida | Alto: Usuario que registra es el que aprueba. |

---

## 3. PROPUESTA DE ARQUITECTURA (NIVEL EMPRESARIAL)

### 3.1 Entidades y DB
1.  **BankAccounts:** Soporte multimoneda, saldos activos.
2.  **BankReconciliations:** Gestión de cierres bancarios formales.
3.  **CashMovements:** Inmutabilidad, estado de aprobación, referencia a documento soporte.

### 3.2 Flujo Operativo (Auditable)
1.  **Draft:** Solicitud inicial.
2.  **Pending Approval:** Espera autorización superior.
3.  **Posted:** Aprobado y contabilizado automáticamente.
4.  **Reconciled:** Vinculado a extracto bancario.

---

## 4. HOJA DE RUTA DE IMPLEMENTACIÓN

*   **Fase 9.1:** Fortalecimiento del Modelo de Datos (Caja/Bancos).
*   **Fase 9.2:** Integración Contable Automática (Asientos automáticos).
*   **Fase 9.3:** Flujo de Aprobación y Conciliación.
