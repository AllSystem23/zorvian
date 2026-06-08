# Architecture

## Structure

```
src/
├── Zorvian.Core/         — Entities, Interfaces, Enums
├── Zorvian.Application/  — DTOs, Services, AutoMapper profiles
├── Zorvian.Infrastructure/ — EF Core DbContext, Repositories, ML Services
├── Zorvian.Web/          — Controllers, Jobs, Middleware, Program.cs

frontend/
├── lib/
│   ├── core/
│   │   ├── api/          — DioClient, interceptors
│   │   ├── widgets/bi/   — BiKpiCard, BiLineChart, BiBarChart, BiPieChart, BiGauge, SalesPredictionSection
│   │   └── ...
│   ├── features/bi/      — Dashboard pages (executive, financial, commercial, operational)
│   │   ├── models/       — bi_models.dart
│   │   ├── providers/    — bi_provider.dart
│   │   └── pages/        — dashboard pages
│   └── shared/ds/        — Design system tokens + components
```

## BI Module

- 4 dashboard pages: Executive, Financial, Commercial, Operational
- Providers: FutureProvider.autoDispose pattern
- Charts: fl_chart (LineChart, BarChart, PieChart), CustomPaint (Gauge)
- KPI cards: BiKpiCard with sparkline support
- Sales prediction: 3 providers → API → ML.NET → visualization
