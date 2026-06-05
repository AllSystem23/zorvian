# REPORTE DE MEJORAS Y BRECHAS - NÓMINA EMPRESARIAL (ZORVIAN ERP)

**Fecha:** 4 de Junio, 2026
**Especialista:** RRHH / Nómina / ERP
**Alcance:** Consolidación del módulo de nómina a nivel empresarial.

---

## 1. ANÁLISIS INTEGRAL DEL MÓDULO

Tras la revisión de los procesos actuales, se determina que el sistema posee una base sólida (95/100), pero para cubrir las necesidades reales de una empresa de alto nivel, debemos cerrar las brechas pendientes en gestión del talento y finanzas.

### 1.1 Brechas identificadas

| Proceso | Hallazgo | Riesgo Legal/Financiero |
|---|---|---|
| **Incapacidades** | No diferenciadas de permisos comunes. | Errores en pago de subsidio INSS (60/40). |
| **Liquidaciones** | Cálculo manual incompleto. | Riesgo de demandas por error en indemnización. |
| **Contratos** | Falta trazabilidad de documentos firmados. | Incapacidad de certificar legalmente el vínculo. |
| **Asistencia** | Falta "descuento automático" por ausencias. | Riesgo de pago en exceso. |

---

## 2. RECOMENDACIONES ESTRUCTURALES

1.  **Motor de Cálculo de Incapacidades:** Implementar reglas diferenciadas por tipo de incapacidad (Accidente laboral vs. enfermedad común) vinculadas a los subsidios del INSS.
2.  **Motor de Liquidaciones:** Automatizar el cálculo de indemnización (cesantía) basado en el Código Laboral, considerando los últimos 6 meses de salario promedio.
3.  **Conciliación Bancaria Automática:** Implementar el proceso de lectura de archivos de respuesta bancaria para actualizar el estado del pago a nivel de detalle.

---

## 3. DISEÑO DE DATOS (NUEVAS ENTIDADES)

### 3.1 Incapacidades (SickLeaveRecords)
Necesaria para gestionar el subsidio del INSS y el complemento patronal.

### 3.2 Liquidaciones (TerminationRecords)
Necesaria para registrar la ruptura del contrato laboral y los cálculos finales.

---

## 4. FLUJO IDEAL DE NÓMINA (ESCALABLE)

1.  **Cierre de Asistencia:** RRHH bloquea marcajes.
2.  **Pre-nómina:** Cálculo automático de salario + conceptos variables + ausencias/incapacidades.
3.  **Aprobación:** Flujo multi-nivel.
4.  **Contabilización:** Generación automática de asiento contable.
5.  **Pago:** Dispersión bancaria y registro de pago.
6.  **Conciliación:** Verificación automática de éxito o error bancario.

---

## 5. ASIENTOS CONTABLES AUTOMATIZADOS

| Acción | Débito | Haber |
|---|---|---|
| **Nómina (Devengo)** | Gastos de Salarios / HE | Sueldos por Pagar |
| **Deducciones** | Sueldos por Pagar | INSS/IR/Otros Retenidos |
| **Pago (ACH)** | Sueldos por Pagar | Bancos |
| **Recuperación INSS** | Cuentas por cobrar INSS | Gastos de Salarios |

---

## 6. HOJA DE RUTA DE IMPLEMENTACIÓN

*   **Fase 8.1:** Módulo de Incapacidades (Semanas 1-2).
*   **Fase 8.2:** Módulo de Liquidaciones y Finiquitos (Semanas 3-4).
*   **Fase 8.3:** Conciliación Bancaria Automática (Semanas 5-6).
