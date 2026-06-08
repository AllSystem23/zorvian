# Decisions

## 2026-06-07 SalesPrediction ML.NET pattern sigue Absenteeism

**Contexto:** Necesitábamos un servicio de predicción de ventas; existía ya `AbsenteeismPredictionService` como referencia.

**Decisión:** Seguir el mismo patrón:
- Infrastructure: `SalesPredictionService` (FastTreeRegression, 8 features)
- Web: `SalesPredictionController` (endpoints next-week, next-month, monthly-total)
- Web: `SalesPredictionTrainingJob` (Hangfire recurrente)

**Razón:** Consistencia arquitectónica, menor riesgo, patrón probado.

**Consecuencias:** Fácil de mantener; ambos modelos comparten paradigma ML.NET.

---

## 2026-06-07 ZSkipLink implementado con altura 1→44px on focus

**Contexto:** Necesitábamos skip link accesible visible solo al hacer Tab.

**Decisión:** Skip link ocupa 1px normalmente, al enfocarse crece a 44px (tap target mínimo WCAG).

**Razón:** No ocupa espacio visual pero es funcional para teclado.

**Consecuencias:** Cumple WCAG 2.2 AA 2.4.1 Bypass Blocks.
