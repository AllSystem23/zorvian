# Módulo Contable — Guía de Seguimiento

## Leyenda
- [x] Completado — backend + frontend + tests
- [~] En progreso — código existente pero incompleto o sin conectar
- [ ] Pendiente — no iniciado
- [!] Bloqueado — requiere otra tarea

---

## 1. Dashboard Financiero en Tiempo Real (KPIs)

| Item | Backend | Frontend | Tests | Notas |
|------|---------|----------|-------|-------|
| Executive Summary (ventas, créditos, inventario, caja, RRHH) | [x] `BiService.GetExecutiveAsync()` | [x] `ExecutiveDashboardPage` | ✅ | Consulta única, no polling |
| KPIs Financieros (ratios, liquidez, endeudamiento) | [x] `BiService.GetFinancialRatiosAsync()` | [x] `FinancialDashboardPage` | ✅ | |
| Dashboard Comercial | [x] sales/category/seller | [x] `CommercialDashboardPage` | ✅ | |
| Dashboard Operacional | [x] inventario, nómina | [x] `OperationalDashboardPage` | ✅ | |
| **Tiempo real (auto-refresh cada N seg)** | [x] | [x] | ✅ | Completado vía Timer.periodic |
| **Alertas automáticas (KPIs críticos)** | [x] | [x] | ✅ | Completado en FinancialDashboardPage |

**Próximo paso:** Agregar auto-refresh vía `Timer.periodic` en los dashboards + mostrar `BiAlertItem` como cards de alerta.

---

## 2. Flujo de Efectivo + Estado de Cambios en el Patrimonio

| Item | Backend | Frontend | Tests | Notas |
|------|---------|----------|-------|-------|
| Flujo de Efectivo (método directo) | [x] `EnhancedReportService.GetCashFlowStatementAsync()` | [x] `financial-reports/cash-flow/{periodId}` | ✅ | Clasifica operativo/inversión/financiamiento |
| Estado de Cambios en el Patrimonio | [x] `EnhancedReportService.GetEquityChangesAsync()` | [x] `financial-reports/equity-changes/{periodId}` | ✅ | Apertura, aumentos, disminuciones, cierre |
| Exportar a PDF/Excel | [x] | [x] | ✅ | Completado en ReportsService |
| UI Flujo de Efectivo | [x] endpoint expuesto | [~] `enhancedEquityChangesProvider` + página | — | La página existe pero usa `periodId` manual; falta selector de períodos real |
| UI Cambios Patrimonio | [x] endpoint expuesto | [~] página creada | — | Misma limitación: sin selector de períodos real |
| Exportar a PDF/Excel | [x] | [x] | ✅ | Completado en ReportsService |

**Próximo paso:** Conectar selector de períodos real (usar `AccountingPeriod` data del provider existente).

---

## 3. Reportes Comparativos (Mensual / Trimestral / Anual)

| Item | Backend | Frontend | Tests | Notas |
|------|---------|----------|-------|-------|
| Comparativo 2 períodos (income statement) | [x] `EnhancedReportService.GetComparativeReportAsync()` | [x] `financial-reports/comparative` (POST) | ✅ | Acepta `reportType` + `periodIds` |
| Comparativo trimestral | [~] mismo método, pasar 3 periodIds | [~] misma UI | — | Técnicamente funciona pero UI solo soporta 2 |
| Comparativo anual (12 meses) | [~] mismo método, pasar 12 periodIds | [ ] | — | UI no soporta multi-período |
| Tabla con varianza $ y % | [x] incluido en DTO | [x] DataTable en página | ✅ | |
| **Gráficos de barra comparativos** | [x] | [x] | ✅ | Completado en ComparativeReportsPage |


**Próximo paso:** Agregar selector de rango (mes inicial → mes final) y generar lista de periodIds automáticamente.

---

## 4. Asistente IA Conversacional (Lenguaje Natural)

| Item | Backend | Frontend | Tests | Notas |
|------|---------|----------|-------|-------|
| Clasificador de intenciones | [x] `FinancialAssistantService.ClassifyIntent()` | — | ✅ | 9 intents: trial balance, income, balance, cash flow, equity, comparative, sales, expenses, anomalies |
| Resolvedor de períodos | [x] `FinancialAssistantService.ResolvePeriodAsync()` | — | ✅ | Regex MM-YYYY + nombres de mes ES/EN |
| Handler: Balance de Comprobación | [x] delegado a `FinancialReportService` | — | ✅ | |
| Handler: Estado de Resultados | [x] delegado a `FinancialReportService` | — | ✅ | |
| Handler: Balance General | [x] delegado a `FinancialReportService` | — | ✅ | |
| Handler: Flujo de Efectivo | [x] delegado a `EnhancedReportService` | — | ✅ | |
| Handler: Cambios Patrimonio | [x] delegado a `EnhancedReportService` | — | ✅ | |
| Handler: Reporte Comparativo | [x] delegado a `EnhancedReportService` | — | ✅ | |
| Handler: Ventas/BI | [x] delegado a `BiService` | — | ✅ | |
| Handler: Anomalías | [x] redirige a `AccountingAssistantService` | — | ✅ | |
| Endpoint REST | [x] `POST financial-reports/assistant/ask` | — | — | En `FinancialReportsController` |
| UI Chat (AiAssistantPage) | — | [x] `AiAssistantPage` | — | Burbujas usuario/asistente, datos estructurados |
| **Memoria de conversación** | [x] | [x] | ✅ | Completado en AiAssistantPage |
| **Feedback loop (útil/no útil)** | [ ] | [ ] | — | ❌ Pendiente |

**Próximo paso:** Agregar memoria de conversación (últimas N interacciones en sesión) para contexto.

---

## 5. Auto-Contabilización (Cheques, Bancos, Cobranza, CxP)

| Item | Backend | Frontend | Tests | Notas |
|------|---------|----------|-------|-------|
| `TransactionTypes.Check` + `AccountRoles.Bank` | [x] agregado a `AccountLink.cs` | — | ✅ | |
| `GenerateCheckEntryAsync` (emisión/cancelación/conciliación) | [x] en `AutoAccountingService` | — | ✅ | 3 modos: issuance, cancellation, reconciliation |
| `GenerateBankDepositEntryAsync` | [x] en `AutoAccountingService` | — | ✅ | Débito banco, crédito caja |
| `GenerateBankTransferEntryAsync` | [x] en `AutoAccountingService` | — | ✅ | Débito destino, crédito origen |
| `GenerateBankCommissionEntryAsync` | [x] en `AutoAccountingService` | — | ✅ | Gasto bancario vs banco |
| `GenerateCollectionEntryAsync` (intereses + recargos) | [x] en `AutoAccountingService` | — | ✅ | Interés (`4.3.01`) + Recargo (`4.3.02`) |
| `GenerateAdvanceToSupplierEntryAsync` | [x] en `AutoAccountingService` | — | ✅ | Anticipo (`1.1.06`) vs caja |
| `GenerateSupplierAdvanceApplicationEntryAsync` | [x] en `AutoAccountingService` | — | ✅ | CxP vs anticipo |
| **Controller que exponga estos métodos** | [x] `TreasuryController` renovado con 7 endpoints | — | ✅ | 10 endpoints: checks (issue, status, template, accounting-entry), bank-deposits/accounting-entry, bank-transfers/accounting-entry, bank-commissions/accounting-entry, collections/accounting-entry, advances-to-suppliers/accounting-entry, supplier-advance-applications/accounting-entry |
| **Conectar con UI de cheques/bancos existente** | [x] | [x] | ✅ | Refactorizado y Estandarizado en CheckIssuancePage, BankDepositPage, BankCollectionPage, BankTransferPage y BankCommissionPage |
| **Reglas de auto-contabilización configurables** | [~] `AccountingRuleRepository` existe | [ ] | — | El motor de reglas existe pero no está conectado a estos nuevos métodos |

**Próximo paso:** Crear `TreasuryController` con endpoints que llamen a los nuevos métodos + UI de tesorería.

---

## 6. Consolidación Multiempresa

| Item | Backend | Frontend | Tests | Notas |
|------|---------|----------|-------|-------|
| Reporte consolidado multi-CompanyId | [ ] | [ ] | — | ❌ Pendiente completo |
| Eliminación de saldos intercompañía | [ ] | [ ] | — | ❌ Pendiente |
| Tipo de cambio común para consolidación | [~] `ExchangeRate` entity existe | [~] UI existe | — | Ya hay `ExchangeRatesController` |
| **Entidad `IntercompanyTransaction`** | [ ] | [ ] | — | ❌ Pendiente |
| **Endpoint: consolidar N empresas → 1 reporte** | [ ] | [ ] | — | ❌ Pendiente |

**Próximo paso:** Diseñar entidad de transacciones intercompañía + servicio de consolidación.

---

## Resumen de Brechas

| # | Componente | Estado | Prioridad | Esfuerzo estimado |
|---|-----------|--------|-----------|-------------------|
| 1 | Dashboard tiempo real (polling/refresh) | ✅ Completado | Alta | 4h |
| 2 | Selector de períodos funcional en Flutter | ✅ Completado | Alta | 3h |
| 3 | Exportar reportes a PDF/Excel | ✅ Completado | Media | 8h |
| 4 | Gráficos comparativos (barras) | ✅ Completado | Media | 4h |
| 5 | Memoria de conversación en asistente IA | ✅ Completado | Baja | 3h |
| 6 | Controller REST para auto-contabilización | ✅ Completado | Alta | 4h |
| 7 | UI Tesorería (cheques, bancos, cobranza, transferencias, comisiones) | ✅ Completado | Alta | 16h |
| 8 | Consolidación multiempresa | 🔴 Pendiente | Alta | 24h |
| 9 | Alertas críticas visibles en dashboard | ✅ Completado | Media | 3h |
| 10 | Feedback loop en asistente IA | ⚪ Pendiente | Baja | 4h |

---

## Prioridades Recomendadas (Siguiente Sprint)

1. **Controller REST para auto-contabilización** — porque los métodos ya están implementados pero no hay forma de llamarlos desde el frontend.
~~2. **Selector de períodos funcional** — porque las páginas de reportes (equity, cash flow, comparativo) no pueden operar sin él.~~ ✅
3. **Dashboard tiempo real (auto-refresh)** — porque los KPIs financieros se quedan obsoletos en pantalla.
4. **UI Tesorería** — para cheques/bancos/cobranza, conectada a los nuevos métodos de auto-contabilización.
