# Progress

## 2026-06-07

### Fase 5.1.1 — Predicción de ventas ML.NET (COMPLETED)

**Backend:**
- `SalesPredictionController` con 3 endpoints
- `SalesPredictionTrainingJob` (Hangfire recurrente domingo 3AM)
- DI registration en `Program.cs`
- `dotnet build` = 0 errors, 0 warnings

**Frontend:**
- Modelos: `BiSalesPredictionDaily`, `BiMonthlySalesPrediction`, `BiMonthlyProjection`
- 3 providers `FutureProvider.autoDispose`
- `SalesPredictionSection` (line chart 30 días + KPI cards + tabla 7 días)
- Integrado en `CommercialDashboardPage`
- `flutter analyze` = No issues found

### Fase 5.1.2 — Recomendación de compras (COMPLETED)

**Backend:**
- `PurchaseRecommendationDto` + `PurchaseRecommendationSummaryDto`
- `PurchaseRecommendationService` (demanda 30d, stock bajo, días hasta agotar, prioridad critical/warning)
- `PurchaseRecommendationController` — `GET /zorvian/v1/purchases/recommendations`
- DI registration en `Program.cs`
- `dotnet build` = 0 errors, 0 warnings

**Frontend:**
- Modelos: `PurchaseRecommendationItem`, `PurchaseRecommendationSummary` en `bi_models.dart`
- Provider: `purchaseRecommendationProvider`
- Widget: `PurchaseRecommendationSection` (badges críticos/advertencia/saludables + tiles producto)
- Integrado en `CommercialDashboardPage` después del pronóstico
- `flutter analyze` = No issues found

### Fase 5.1.3 — Clasificación automática de gastos (COMPLETED)

**Backend:**
- `ExpenseClassificationService` (ML.NET SDCA multiclass text classifier + amount feature)
- `ExpenseClassificationTrainingJob` (Hangfire recurrente domingo 4AM, entrena con últimos 5000 asientos)
- `ExpenseClassificationController` — `GET /zorvian/v1/expense-classification/predict?description=...&amount=...`
- DI registration + recurring job en `Program.cs`
- `dotnet build` = 0 errors, 0 warnings

**Frontend:**
- Modelos: `ExpenseClassificationResult`, `ExpenseClassificationResponse` en `bi_models.dart`
- Provider: `expenseClassificationProvider` (Family con record params, debounced 500ms)
- Widget: `ExpenseClassifierSection` (TextField con sugerencias en vivo + barra de confianza)
- Integrado en `CommercialDashboardPage` después de recomendaciones de compra
- `flutter analyze` = No issues found
