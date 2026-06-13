using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.DTOs.Bi;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class FinancialAssistantService
{
    private readonly FinancialReportService _reports;
    private readonly BiService _bi;
    private readonly EnhancedReportService _enhanced;
    private readonly IAccountingPeriodRepository _periodRepo;
    private readonly ITenantContext _tenant;
    private readonly ICompanyRepository _companyRepo; // Nueva dependencia
    private static readonly string[] Separators = [" ", ",", ".", ":", ";", "\t", "\n"];

    public FinancialAssistantService(
        FinancialReportService reports,
        BiService bi,
        EnhancedReportService enhanced,
        IAccountingPeriodRepository periodRepo,
        ITenantContext tenant,
        ICompanyRepository companyRepo)
    {
        _reports = reports; _bi = bi;
        _enhanced = enhanced; _periodRepo = periodRepo;
        _tenant = tenant;
        _companyRepo = companyRepo;
    }

    private Guid CompanyId =>
        Guid.TryParse(_tenant.TenantId, out var id) ? id : throw new InvalidOperationException("Invalid tenant");

    private async Task<string> GetCountryCodeAsync()
    {
        var company = await _companyRepo.GetByIdAsync(CompanyId);
        return company?.Country switch
        {
            "Nicaragua" => "NIC",
            "Costa Rica" => "CR",
            "Honduras" => "HN",
            "Guatemala" => "GT",
            "El Salvador" => "SV",
            "Panamá" => "PA",
            _ => "NIC"
        };
    }

    public async Task<FinancialAssistantResponse> AskAsync(string query)
    {
        var q = query.Trim().ToLowerInvariant();
        var tokens = q.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var intent = ClassifyIntent(tokens, q);
        return intent switch
        {
            "trial_balance" => await HandleTrialBalanceAsync(q),
            "income_statement" => await HandleIncomeStatementAsync(q),
            "balance_sheet" => await HandleBalanceSheetAsync(q),
            "cash_flow" => await HandleCashFlowAsync(q),
            "equity_changes" => await HandleEquityChangesAsync(q),
            "comparative" => await HandleComparativeAsync(q),
            "bi_sales" => await HandleBiSalesAsync(q),
            "bi_expenses" => await HandleBiExpensesAsync(q),
            "anomalies" => await HandleAnomaliesAsync(q),
            "help" => await HelpResponse(),
            _ => await UnknownResponse(q),
        };
    }

    public async Task SaveFeedbackAsync(FinancialAssistantFeedbackRequest feedback)
    {
        // Implementación de almacenamiento de feedback
        // Por ahora, lo registramos en logs o audit trail
        // En el futuro, esto se conectará a un repositorio dedicado
        await Task.CompletedTask;
    }

    private string ClassifyIntent(string[] tokens, string q)
    {
        if (MatchAny(tokens, "ayuda", "help", "qué puedes hacer", "commands", "comandos"))
            return "help";

        if (MatchAny(tokens, "anomalía", "anomaly", "anomalías", "anomalies"))
            return "anomalies";

        if (MatchAny(tokens, "venta", "ventas", "sales", "ingresos", "income", "revenue"))
            return "bi_sales";

        if (MatchAny(tokens, "gasto", "gastos", "expense", "expenses", "egresos"))
            return "bi_expenses";

        if (MatchAny(tokens, "balance", "general", "situación", "financial", "position"))
            return "balance_sheet";

        if (MatchAny(tokens, "resultado", "pérdida", "ganancia", "profit", "loss", "pnl", "income"))
        {
            if (MatchAny(tokens, "comparativo", "comparative", "vs", "vs."))
                return "comparative";
            return "income_statement";
        }

        if (MatchAny(tokens, "flujo", "efectivo", "cash", "flow"))
            return "cash_flow";

        if (MatchAny(tokens, "patrimonio", "equity", "capital", "cambios"))
            return "equity_changes";

        if (MatchAny(tokens, "comparativo", "comparative", "vs", "período", "period", "mes", "month"))
            return "comparative";

        if (MatchAny(tokens, "balance", "comprobación", "trial", "sumas", "saldos"))
            return "trial_balance";

        return "unknown";
    }

    private static bool MatchAny(string[] tokens, params string[] words)
    {
        return words.Any(w => tokens.Contains(w));
    }

    private async Task<FinancialAssistantResponse> HandleTrialBalanceAsync(string q)
    {
        var period = await ResolvePeriodAsync(q);
        if (period == null)
            return new FinancialAssistantResponse(
                "No se encontró un período contable abierto o válido. Usa 'período MM-YYYY' para especificar.", "low", null);

        var tb = await _reports.GetTrialBalanceAsync(period.Id);
        if (tb == null)
            return new FinancialAssistantResponse(
                "No hay datos de balance de comprobación para este período.", "low", null);

        var data = tb.Items.Select(i => new FinancialAssistantDataPoint(
            $"{i.AccountCode} {i.AccountName}", i.EndingBalance, "decimal")).ToList();

        return new FinancialAssistantResponse(
            $"Balance de Comprobación - {period.Name}: {tb.Items.Count} cuentas, Débitos: {tb.TotalEndingDebit:N2}, Créditos: {tb.TotalEndingCredit:N2}",
            "high", data);
    }

    private async Task<FinancialAssistantResponse> HandleIncomeStatementAsync(string q)
    {
        var period = await ResolvePeriodAsync(q);
        if (period == null)
            return new FinancialAssistantResponse(
                "No se encontró un período válido.", "low", null);

        var income = await _reports.GetIncomeStatementAsync(period.Id);
        if (income == null)
            return new FinancialAssistantResponse(
                "No hay datos de estado de resultados para este período.", "low", null);

        var netIncome = income.NetIncome;

        var data = new List<FinancialAssistantDataPoint>
        {
            new("Total Ingresos", income.TotalIncome, "decimal"),
            new("Costo de Ventas", income.TotalCost, "decimal"),
            new("Utilidad Bruta", income.GrossProfit, "decimal"),
            new("Total Gastos", income.TotalExpenses, "decimal"),
            new("Resultado Neto", netIncome, netIncome >= 0 ? "decimal_positive" : "decimal_negative"),
        };

        return new FinancialAssistantResponse(
            $"Estado de Resultados - {period.Name}: Ingresos {income.TotalIncome:N2}, Costo {income.TotalCost:N2}, " +
            $"Gastos {income.TotalExpenses:N2}, Resultado {(netIncome >= 0 ? "Positivo" : "Negativo")}: {Math.Abs(netIncome):N2}",
            "high", data);
    }

    private async Task<FinancialAssistantResponse> HandleBalanceSheetAsync(string q)
    {
        var period = await ResolvePeriodAsync(q);
        if (period == null)
            return new FinancialAssistantResponse(
                "No se encontró un período válido.", "low", null);

        var bs = await _reports.GetBalanceSheetAsync(period.Id);
        if (bs == null)
            return new FinancialAssistantResponse(
                "No hay datos de balance general para este período.", "low", null);

        var check = bs.TotalAssets - (bs.TotalLiabilities + bs.TotalEquity);

        var data = new List<FinancialAssistantDataPoint>
        {
            new("Total Activos", bs.TotalAssets, "decimal"),
            new("Total Pasivos", bs.TotalLiabilities, "decimal"),
            new("Total Patrimonio", bs.TotalEquity, "decimal"),
            new("Diferencia (A - P - P)", check, Math.Abs(check) < 0.01m ? "decimal_positive" : "decimal_negative"),
        };

        foreach (var section in bs.Sections)
        {
            foreach (var item in section.Items)
            {
                data.Add(new FinancialAssistantDataPoint(
                    $"{item.AccountCode} {item.AccountName}", item.Balance, "decimal"));
            }
        }

        var status = Math.Abs(check) < 0.01m
            ? "Balance cuadra correctamente."
            : $"Balance NO cuadra por diferencia de {check:N2}.";

        return new FinancialAssistantResponse(
            $"Balance General - {period.Name}: Activos {bs.TotalAssets:N2}, Pasivos {bs.TotalLiabilities:N2}, " +
            $"Patrimonio {bs.TotalEquity:N2}. {status}",
            "high", data);
    }

    private async Task<FinancialAssistantResponse> HandleCashFlowAsync(string q)
    {
        var period = await ResolvePeriodAsync(q);
        if (period == null)
            return new FinancialAssistantResponse(
                "No se encontró un período válido.", "low", null);

        var cf = await _enhanced.GetCashFlowStatementAsync(period.Id);
        if (cf == null)
            return new FinancialAssistantResponse(
                "No hay datos de flujo de efectivo para este período.", "low", null);

        var data = new List<FinancialAssistantDataPoint>
        {
            new("Flujo Operativo", cf.NetOperatingCashFlow, "decimal"),
            new("Flujo Inversión", cf.NetInvestingCashFlow, "decimal"),
            new("Flujo Financiamiento", cf.NetFinancingCashFlow, "decimal"),
            new("Incremento Neto", cf.NetCashIncrease, "decimal"),
            new("Efectivo Inicial", cf.BeginningCash, "decimal"),
            new("Efectivo Final", cf.EndingCash, "decimal"),
        };

        return new FinancialAssistantResponse(
            $"Estado de Flujo de Efectivo - {cf.PeriodName}: Operativo {cf.NetOperatingCashFlow:N2}, " +
            $"Inversión {cf.NetInvestingCashFlow:N2}, Financiamiento {cf.NetFinancingCashFlow:N2}, " +
            $"Efectivo Final {cf.EndingCash:N2}",
            "high", data);
    }

    private async Task<FinancialAssistantResponse> HandleEquityChangesAsync(string q)
    {
        var period = await ResolvePeriodAsync(q);
        if (period == null)
            return new FinancialAssistantResponse(
                "No se encontró un período válido.", "low", null);

        var equity = await _enhanced.GetEquityChangesAsync(period.Id);
        if (equity == null)
            return new FinancialAssistantResponse(
                "No hay datos de cambios en el patrimonio para este período.", "low", null);

        var data = new List<FinancialAssistantDataPoint>
        {
            new("Patrimonio Inicial", equity.TotalOpeningEquity, "decimal"),
            new("Aumentos", equity.TotalAdditions, "decimal_positive"),
            new("Disminuciones", equity.TotalDeductions, "decimal_negative"),
            new("Patrimonio Final", equity.TotalEndingEquity, "decimal"),
        };
        data.AddRange(equity.Items.Select(i => new FinancialAssistantDataPoint(
            i.Concept, i.EndingBalance, "decimal")));

        return new FinancialAssistantResponse(
            $"Estado de Cambios en el Patrimonio - {equity.PeriodName}: Inicial {equity.TotalOpeningEquity:N2}, " +
            $"Final {equity.TotalEndingEquity:N2}",
            "high", data);
    }

    private async Task<FinancialAssistantResponse> HandleComparativeAsync(string q)
    {
        var periods = await _periodRepo.GetAllAsync(CompanyId);
        var openPeriods = periods.Where(p => p.Status == "open").OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToList();
        var closedPeriods = periods.Where(p => p.Status == "closed").OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToList();

        var selected = openPeriods.Take(1).Concat(closedPeriods.Take(1)).Select(p => p.Id).ToList();
        if (selected.Count < 2)
            return new FinancialAssistantResponse(
                "Se necesitan al menos 2 períodos para comparar.", "low", null);

        var comp = await _enhanced.GetComparativeReportAsync("income_statement", selected);
        var data = comp.Lines.Select(l => new FinancialAssistantDataPoint(
            $"{l.Concept} (var: {l.VariancePercent}%)", l.Variance, "decimal")).ToList();

        return new FinancialAssistantResponse(
            $"Reporte Comparativo: período actual vs anterior. Varianza total: {comp.TotalVariance:N2} ({comp.TotalVariancePercent}%)",
            "high", data);
    }

    private async Task<FinancialAssistantResponse> HandleBiSalesAsync(string q)
    {
        var exec = await _bi.GetExecutiveAsync();
        var kpi = exec.Sales;
        var data = new List<FinancialAssistantDataPoint>
        {
            new("Ventas Hoy", kpi.TodaySales, "decimal"),
            new("Ventas del Mes", kpi.MonthSales, "decimal"),
            new("Ticket Promedio", kpi.AverageTicket, "decimal"),
            new("Variación Mensual %", (decimal)kpi.MonthSalesChangePercent, "percentage"),
            new("Transacciones Hoy", kpi.TodaySalesCount, "integer"),
        };

        return new FinancialAssistantResponse(
            $"Ventas: Hoy {kpi.TodaySales:N2}, Mes {kpi.MonthSales:N2}, Ticket Promedio {kpi.AverageTicket:N2}",
            "high", data);
    }

    private async Task<FinancialAssistantResponse> HandleBiExpensesAsync(string q)
    {
        var exec = await _bi.GetExecutiveAsync();
        var kpi = exec.Cash;
        var data = new List<FinancialAssistantDataPoint>
        {
            new("Gastos Hoy", kpi.TodayExpense, "decimal"),
            new("Ingresos Hoy", kpi.TodayIncome, "decimal"),
            new("Flujo Neto Hoy", kpi.NetCashFlow, kpi.NetCashFlow >= 0 ? "decimal_positive" : "decimal_negative"),
        };

        return new FinancialAssistantResponse(
            $"Gastos Hoy: {kpi.TodayExpense:N2}, Ingresos: {kpi.TodayIncome:N2}, Flujo Neto: {kpi.NetCashFlow:N2}",
            "high", data);
    }

    private Task<FinancialAssistantResponse> HandleAnomaliesAsync(string q)
    {
        var days = ExtractDays(q, 30);
        return Task.FromResult(new FinancialAssistantResponse(
            $"Revisión de anomalías solicitada (últimos {days} días). Usa el endpoint de anomalías dedicado para ver el reporte completo.",
            "medium", new List<FinancialAssistantDataPoint>
            {
                new("Días analizados", days, "integer"),
            }));
    }

    private static Task<FinancialAssistantResponse> HelpResponse()
    {
        var commands = new List<FinancialAssistantDataPoint>
        {
            new("balance general [período]", 0, "text"),
            new("estado de resultados [período]", 0, "text"),
            new("flujo de efectivo [período]", 0, "text"),
            new("cambios en el patrimonio [período]", 0, "text"),
            new("ventas [últimos N días]", 0, "text"),
            new("gastos [últimos N días]", 0, "text"),
            new("comparativo [períodos]", 0, "text"),
            new("anomalías [últimos N días]", 0, "text"),
            new("balance de comprobación [período]", 0, "text"),
        };
        return Task.FromResult(new FinancialAssistantResponse(
            "Comandos disponibles: balance general, estado de resultados, flujo de efectivo, " +
            "cambios en el patrimonio, ventas, gastos, comparativo, anomalías, balance de comprobación. " +
            "Ej: 'balance general período 01-2025'",
            "high", commands));
    }

    private static Task<FinancialAssistantResponse> UnknownResponse(string q)
    {
        return Task.FromResult(new FinancialAssistantResponse(
            $"No entendí la consulta: '{q}'. Escribe 'ayuda' para ver los comandos disponibles.",
            "low", null));
    }

    private async Task<AccountingPeriod?> ResolvePeriodAsync(string q)
    {
        var periodMatch = System.Text.RegularExpressions.Regex.Match(q, @"(\d{1,2})\s*[-/]\s*(\d{4})");
        if (periodMatch.Success)
        {
            var month = int.Parse(periodMatch.Groups[1].Value);
            var year = int.Parse(periodMatch.Groups[2].Value);
            return await _periodRepo.GetByYearMonthAsync(year, month, CompanyId);
        }

        var monthNames = new Dictionary<string, int>
        {
            ["enero"] = 1, ["febrero"] = 2, ["marzo"] = 3, ["abril"] = 4,
            ["mayo"] = 5, ["junio"] = 6, ["julio"] = 7, ["agosto"] = 8,
            ["septiembre"] = 9, ["octubre"] = 10, ["noviembre"] = 11, ["diciembre"] = 12,
            ["january"] = 1, ["february"] = 2, ["march"] = 3, ["april"] = 4,
            ["may"] = 5, ["june"] = 6, ["july"] = 7, ["august"] = 8,
            ["september"] = 9, ["october"] = 10, ["november"] = 11, ["december"] = 12,
        };

        foreach (var (name, num) in monthNames)
        {
            if (q.Contains(name))
            {
                var yearMatch = System.Text.RegularExpressions.Regex.Match(q, @"\b(20\d{2})\b");
                var year = yearMatch.Success ? int.Parse(yearMatch.Value) : DateTime.UtcNow.Year;
                return await _periodRepo.GetByYearMonthAsync(year, num, CompanyId);
            }
        }

        return await _periodRepo.GetCurrentOpenAsync(CompanyId);
    }

    private static int ExtractDays(string q, int defaultDays)
    {
        var match = System.Text.RegularExpressions.Regex.Match(q, @"(\d+)\s*(días|dias|days|día)");
        return match.Success ? int.Parse(match.Groups[1].Value) : defaultDays;
    }
}
