# Diseño Técnico Fase 1: PGCZ-CA y Motores Regionales

Este documento detalla la estructura propuesta para el Plan Global de Cuentas Zorvian Centroamérica (PGCZ-CA) y las nuevas entidades configurables.

## 1. PGCZ-CA (Estructura de Cuentas)
Se propone una segmentación jerárquica para soportar multiempresa, consolidación y multisucursal:
**Estructura:** `C.GG.SS.AAA.XXXX`
- `C` (Clase): 1 dígito (1-9)
- `GG` (Grupo): 2 dígitos
- `SS` (Subgrupo): 2 dígitos
- `AAA` (Cuenta Principal): 3 dígitos
- `XXXX` (Auxiliar): 4 dígitos

**Ejemplo:** `1.01.01.001.0001` (Activo Corriente -> Efectivo -> Caja General -> Caja Principal)

## 2. Nuevas Entidades Configurables (Arquitectura)

### RegionalTaxConfiguration
- `Id`: Guid
- `CountryCode`: string (e.g., 'NI', 'CR', 'HN', etc.)
- `TaxType`: string (e.g., 'IVA', 'IR', 'ISV')
- `Rate`: decimal
- `EffectiveDate`: DateTime
- `IsActive`: bool

### PayrollConcept
- `Id`: Guid
- `CountryCode`: string
- `Code`: string (e.g., 'INSS_PAT', 'IR_SAL')
- `Name`: string
- `CalculationFormula`: string (e.g., "Salary * 0.0625")
- `AccountMappingId`: Guid (Relación a PGCZ-CA)

### AccountingRuleTemplate
- `Id`: Guid
- `CountryCode`: string
- `ProcessTrigger`: string (e.g., 'SALE_INVOICE')
- `EntryStructureJson`: string (JSON definido para mapear cuentas débito/crédito según reglas de negocio)
