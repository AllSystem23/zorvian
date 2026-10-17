import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/bi/models/bi_models.dart';
import 'package:zorvian/shared/printing/bi_report_pdf.dart';

BiReportMeta _meta({String title = 'Reporte Ejecutivo'}) => BiReportMeta(
      title: title,
      companyName: 'Empresa Demo S.A.',
      branchLabel: 'Todas las sucursales',
      currencyCode: 'NIO',
    );

BiExecutive _executive() => BiExecutive(
      sales: BiSalesKpi(
        todaySales: 1500,
        yesterdaySales: 1400,
        salesChangePercent: 7.1,
        monthSales: 45000,
        monthSalesChangePercent: 3.2,
        averageTicket: 320.5,
        todaySalesCount: 5,
        weeklyTrend: const [1, 2, 3],
      ),
      credits: BiCreditKpi(
        activeCredits: 12,
        overdueCredits: 3,
        monthlyRecovery: 8000,
        totalPortfolio: 150000,
        collectionRate: 92,
        dsoDays: 45,
      ),
      inventory: BiInventoryKpi(
        outOfStockProducts: 2,
        lowStockProducts: 8,
        totalProducts: 340,
        totalStockValue: 900000,
        turnoverRate: 4.2,
        topSelling: const [],
      ),
      cash: BiCashKpi(todayIncome: 2000, todayExpense: 900, netCashFlow: 1100, openRegisters: 2),
      hr: BiHrKpi(
        activeEmployees: 20,
        totalEmployees: 22,
        pendingRequests: 3,
        payrollTotal: 180000,
        avgCostPerEmployee: 8000,
      ),
      alerts: [BiAlert(type: 'stock', message: 'Producto X con stock bajo', severity: 'warning')],
    );

List<BiMonthSales> _trend({int months = 12}) => [
      for (var m = 1; m <= months; m++)
        BiMonthSales(
          year: 2026,
          month: m,
          total: 10000.0 * m,
          count: 20 + m,
          averageTicket: 300.0,
          totalCost: 6000.0 * m,
          margin: 0.4,
        ),
    ];

BranchComparison _comparison() => BranchComparison(
      branches: [
        BranchComparisonItem(
          branchId: 'b1',
          branchName: 'Centro',
          monthSales: 30000,
          salesCount: 90,
          monthRecovery: 12000,
          portfolio: 80000,
          activeCredits: 10,
          overdueCredits: 2,
          inventoryValue: 400000,
        ),
        BranchComparisonItem(
          branchId: 'b2',
          branchName: 'Extensión',
          monthSales: 15000,
          salesCount: 40,
          monthRecovery: 6000,
          portfolio: 45000,
          activeCredits: 7,
          overdueCredits: 1,
          inventoryValue: 250000,
        ),
      ],
      totalMonthSales: 45000,
      totalSalesCount: 130,
      totalMonthRecovery: 18000,
      totalPortfolio: 125000,
      totalActiveCredits: 17,
      totalOverdueCredits: 3,
      totalInventoryValue: 650000,
    );

BiArAging _aging() => BiArAging(
      current: 90000,
      days30: 25000,
      days60: 15000,
      days90: 12000,
      days90Plus: 8000,
      totalOverdue: 60000,
      totalPortfolio: 150000,
      overduePercent: 40,
      byClient: [
        BiClientAging(clientName: 'Cliente A', balance: 30000, current: 20000, days30: 6000, days60: 2500, days90: 1000, days90Plus: 500),
        BiClientAging(clientName: 'Cliente B', balance: 20000, current: 15000, days30: 3000, days60: 1000, days90: 700, days90Plus: 300),
      ],
    );

BiFinancialRatios _ratios() => BiFinancialRatios(
      currentRatio: 1.8,
      quickRatio: 1.1,
      debtRatio: 0.4,
      debtToEquity: 0.7,
      grossMargin: 0.35,
      netMargin: 0.12,
      operatingMargin: 0.18,
      roa: 0.09,
      roe: 0.15,
      workingCapital: 250000,
      breakEvenPoint: 95000,
    );

String _header(List<int> bytes) => String.fromCharCodes(bytes.sublist(0, 4));

void main() {
  test('reporte ejecutivo con todas las secciones genera un PDF valido', () async {
    final bytes = await buildExecutiveBiReportPdf(
      executive: _executive(),
      trend: _trend(),
      comparison: _comparison(),
      aging: _aging(),
      ratios: _ratios(),
      meta: _meta(),
    );

    expect(_header(bytes), '%PDF');
    expect(bytes.length, greaterThan(8000));
  });

  test('reporte ejecutivo sin secciones opcionales y con un solo punto de tendencia', () async {
    final bytes = await buildExecutiveBiReportPdf(
      executive: _executive(),
      trend: _trend(months: 1),
      meta: _meta(),
    );

    expect(_header(bytes), '%PDF');
  });

  test('reporte comercial genera un PDF valido con 12 meses', () async {
    final bytes = await buildCommercialBiReportPdf(trend: _trend(), meta: _meta(title: 'Reporte Comercial'));

    expect(_header(bytes), '%PDF');
    expect(bytes.length, greaterThan(6000));
  });

  test('reporte comercial con tendencia vacia sigue siendo un PDF valido', () async {
    final bytes = await buildCommercialBiReportPdf(trend: const [], meta: _meta(title: 'Reporte Comercial'));

    expect(_header(bytes), '%PDF');
  });

  test('reporte ejecutivo sin logo y con cero maximos no lanza division por cero', () async {
    final executive = _executive();
    final bytes = await buildExecutiveBiReportPdf(
      executive: executive,
      trend: [
        BiMonthSales(year: 2026, month: 1, total: 0, count: 0, averageTicket: 0, totalCost: 0, margin: 0),
        BiMonthSales(year: 2026, month: 2, total: 0, count: 0, averageTicket: 0, totalCost: 0, margin: 0),
      ],
      aging: BiArAging(
        current: 0,
        days30: 0,
        days60: 0,
        days90: 0,
        days90Plus: 0,
        totalOverdue: 0,
        totalPortfolio: 0,
        overduePercent: 0,
        byClient: const [],
      ),
      meta: _meta(),
    );

    expect(_header(bytes), '%PDF');
  });
}
