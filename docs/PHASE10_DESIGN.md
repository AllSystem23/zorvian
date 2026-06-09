# Diseño Técnico Fase 10: Interfaz de Liquidaciones y PDF

Este documento define el diseño para el módulo de cálculo y emisión de liquidaciones finales en Zorvian ERP.

## 1. Diseño de Interfaz (Flutter)
- Pantalla: `SettlementFormPage`
- Campos:
  - Selector de Empleado (Autocomplete)
  - Tipo de terminación (Resignation, UnjustifiedDismissal, JustifiedDismissal, EndOfContract)
  - Fechas (Contratación, Salida)
  - Salario mensual (Cargado del histórico)
- Funcionalidad:
  - Botón "Calcular" (Llama a `PayrollLocalizationService.CalculateConceptAsync` con el `TerminationContext`).
  - Tabla de resultados (Aguinaldo, Vacaciones, Indemnización).
  - Botón "Generar PDF".

## 2. Reporte PDF (Backend)
- Servicio: `SettlementPdfService`
- Tecnología: `QuestPDF` (recomendado para .NET) o `pdf` (Dart, si se genera en cliente).
- Estructura:
  - Header: Logo, Fecha, Datos Empresa, Datos Trabajador.
  - Body: Tabla detallada de conceptos, días, meses, años, montos.
  - Footer: Firmas, lugar de elaboración, pie legal.

## 3. Integración
- La UI consumirá el endpoint `api/payroll/calculate-settlement`.
- El reporte se generará en el backend y se devolverá como `FileResult`.
