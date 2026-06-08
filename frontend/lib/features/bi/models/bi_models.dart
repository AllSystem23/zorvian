

class BiExecutive {
  final BiSalesKpi sales;
  final BiCreditKpi credits;
  final BiInventoryKpi inventory;
  final BiCashKpi cash;
  final BiHrKpi hr;
  final List<BiAlert> alerts;

  BiExecutive({required this.sales, required this.credits, required this.inventory, required this.cash, required this.hr, required this.alerts});

  factory BiExecutive.fromJson(Map<String, dynamic> j) => BiExecutive(
    sales: BiSalesKpi.fromJson(j['sales']),
    credits: BiCreditKpi.fromJson(j['credits']),
    inventory: BiInventoryKpi.fromJson(j['inventory']),
    cash: BiCashKpi.fromJson(j['cash']),
    hr: BiHrKpi.fromJson(j['humanResources']),
    alerts: (j['alerts'] as List?)?.map((e) => BiAlert.fromJson(e)).toList() ?? [],
  );
}

class BiSalesKpi {
  final double todaySales, yesterdaySales, salesChangePercent, monthSales, monthSalesChangePercent, averageTicket;
  final int todaySalesCount;
  final List<double> weeklyTrend;
  BiSalesKpi({required this.todaySales, required this.yesterdaySales, required this.salesChangePercent, required this.monthSales, required this.monthSalesChangePercent, required this.averageTicket, required this.todaySalesCount, required this.weeklyTrend});
  factory BiSalesKpi.fromJson(Map<String, dynamic> j) => BiSalesKpi(
    todaySales: (j['todaySales'] ?? 0).toDouble(), yesterdaySales: (j['yesterdaySales'] ?? 0).toDouble(),
    salesChangePercent: (j['salesChangePercent'] ?? 0).toDouble(), monthSales: (j['monthSales'] ?? 0).toDouble(),
    monthSalesChangePercent: (j['monthSalesChangePercent'] ?? 0).toDouble(), averageTicket: (j['averageTicket'] ?? 0).toDouble(),
    todaySalesCount: j['todaySalesCount'] ?? 0, weeklyTrend: (j['weeklyTrend'] as List?)?.map((e) => (e as num).toDouble()).toList() ?? []);
}

class BiCreditKpi {
  final int activeCredits, overdueCredits;
  final double monthlyRecovery, totalPortfolio, collectionRate, dsoDays;
  BiCreditKpi({required this.activeCredits, required this.overdueCredits, required this.monthlyRecovery, required this.totalPortfolio, required this.collectionRate, required this.dsoDays});
  factory BiCreditKpi.fromJson(Map<String, dynamic> j) => BiCreditKpi(
    activeCredits: j['activeCredits'] ?? 0, overdueCredits: j['overdueCredits'] ?? 0,
    monthlyRecovery: (j['monthlyRecovery'] ?? 0).toDouble(), totalPortfolio: (j['totalPortfolio'] ?? 0).toDouble(),
    collectionRate: (j['collectionRate'] ?? 0).toDouble(), dsoDays: (j['dsoDays'] ?? 0).toDouble());
}

class BiInventoryKpi {
  final int outOfStockProducts, lowStockProducts, totalProducts;
  final double totalStockValue, turnoverRate;
  final List<BiTopProduct> topSelling;
  BiInventoryKpi({required this.outOfStockProducts, required this.lowStockProducts, required this.totalProducts, required this.totalStockValue, required this.turnoverRate, required this.topSelling});
  factory BiInventoryKpi.fromJson(Map<String, dynamic> j) => BiInventoryKpi(
    outOfStockProducts: j['outOfStockProducts'] ?? 0, lowStockProducts: j['lowStockProducts'] ?? 0,
    totalProducts: j['totalProducts'] ?? 0, totalStockValue: (j['totalStockValue'] ?? 0).toDouble(),
    turnoverRate: (j['turnoverRate'] ?? 0).toDouble(),
    topSelling: (j['topSelling'] as List?)?.map((e) => BiTopProduct.fromJson(e)).toList() ?? []);
}

class BiTopProduct { final String productName; final int totalSold; BiTopProduct({required this.productName, required this.totalSold}); factory BiTopProduct.fromJson(Map<String, dynamic> j) => BiTopProduct(productName: j['productName'] ?? '', totalSold: j['totalSold'] ?? 0); }

class BiCashKpi { final double todayIncome, todayExpense, netCashFlow; final int openRegisters; BiCashKpi({required this.todayIncome, required this.todayExpense, required this.netCashFlow, required this.openRegisters}); factory BiCashKpi.fromJson(Map<String, dynamic> j) => BiCashKpi(todayIncome: (j['todayIncome'] ?? 0).toDouble(), todayExpense: (j['todayExpense'] ?? 0).toDouble(), netCashFlow: (j['netCashFlow'] ?? 0).toDouble(), openRegisters: j['openRegisters'] ?? 0); }

class BiHrKpi { final int activeEmployees, totalEmployees, pendingRequests; final double payrollTotal, avgCostPerEmployee; BiHrKpi({required this.activeEmployees, required this.totalEmployees, required this.pendingRequests, required this.payrollTotal, required this.avgCostPerEmployee}); factory BiHrKpi.fromJson(Map<String, dynamic> j) => BiHrKpi(activeEmployees: j['activeEmployees'] ?? 0, totalEmployees: j['totalEmployees'] ?? 0, pendingRequests: j['pendingRequests'] ?? 0, payrollTotal: (j['payrollTotal'] ?? 0).toDouble(), avgCostPerEmployee: (j['avgCostPerEmployee'] ?? 0).toDouble()); }

class BiAlert { final String type, message, severity; BiAlert({required this.type, required this.message, required this.severity}); factory BiAlert.fromJson(Map<String, dynamic> j) => BiAlert(type: j['type'] ?? '', message: j['message'] ?? '', severity: j['severity'] ?? ''); }

class BiMonthSales {
  final int year, month, count;
  final double total, averageTicket, totalCost, margin;
  BiMonthSales({required this.year, required this.month, required this.total, required this.count, required this.averageTicket, required this.totalCost, required this.margin});
  factory BiMonthSales.fromJson(Map<String, dynamic> j) => BiMonthSales(year: j['year'], month: j['month'], total: (j['total'] ?? 0).toDouble(), count: j['count'] ?? 0, averageTicket: (j['averageTicket'] ?? 0).toDouble(), totalCost: (j['totalCost'] ?? 0).toDouble(), margin: (j['margin'] ?? 0).toDouble());
}

class BiArAging {
  final double current, days30, days60, days90, days90Plus, totalOverdue, totalPortfolio, overduePercent;
  final List<BiClientAging> byClient;
  BiArAging({required this.current, required this.days30, required this.days60, required this.days90, required this.days90Plus, required this.totalOverdue, required this.totalPortfolio, required this.overduePercent, required this.byClient});
  factory BiArAging.fromJson(Map<String, dynamic> j) => BiArAging(current: (j['current'] ?? 0).toDouble(), days30: (j['days30'] ?? 0).toDouble(), days60: (j['days60'] ?? 0).toDouble(), days90: (j['days90'] ?? 0).toDouble(), days90Plus: (j['days90Plus'] ?? 0).toDouble(), totalOverdue: (j['totalOverdue'] ?? 0).toDouble(), totalPortfolio: (j['totalPortfolio'] ?? 0).toDouble(), overduePercent: (j['overduePercent'] ?? 0).toDouble(), byClient: (j['byClient'] as List?)?.map((e) => BiClientAging.fromJson(e)).toList() ?? []);
}

class BiClientAging {
  final String clientName; final double balance, current, days30, days60, days90, days90Plus;
  BiClientAging({required this.clientName, required this.balance, required this.current, required this.days30, required this.days60, required this.days90, required this.days90Plus});
  factory BiClientAging.fromJson(Map<String, dynamic> j) => BiClientAging(clientName: j['clientName'] ?? '', balance: (j['balance'] ?? 0).toDouble(), current: (j['current'] ?? 0).toDouble(), days30: (j['days30'] ?? 0).toDouble(), days60: (j['days60'] ?? 0).toDouble(), days90: (j['days90'] ?? 0).toDouble(), days90Plus: (j['days90Plus'] ?? 0).toDouble());
}

class BiApAging {
  final double current, days30, days60, days90, days90Plus, totalOverdue, totalPayable, overduePercent;
  final List<BiSupplierAging> bySupplier;
  BiApAging({required this.current, required this.days30, required this.days60, required this.days90, required this.days90Plus, required this.totalOverdue, required this.totalPayable, required this.overduePercent, required this.bySupplier});
  factory BiApAging.fromJson(Map<String, dynamic> j) => BiApAging(current: (j['current'] ?? 0).toDouble(), days30: (j['days30'] ?? 0).toDouble(), days60: (j['days60'] ?? 0).toDouble(), days90: (j['days90'] ?? 0).toDouble(), days90Plus: (j['days90Plus'] ?? 0).toDouble(), totalOverdue: (j['totalOverdue'] ?? 0).toDouble(), totalPayable: (j['totalPayable'] ?? 0).toDouble(), overduePercent: (j['overduePercent'] ?? 0).toDouble(), bySupplier: (j['bySupplier'] as List?)?.map((e) => BiSupplierAging.fromJson(e)).toList() ?? []);
}

class BiSupplierAging {
  final String supplierName; final double balance, current, days30, days60, days90, days90Plus;
  BiSupplierAging({required this.supplierName, required this.balance, required this.current, required this.days30, required this.days60, required this.days90, required this.days90Plus});
  factory BiSupplierAging.fromJson(Map<String, dynamic> j) => BiSupplierAging(supplierName: j['supplierName'] ?? '', balance: (j['balance'] ?? 0).toDouble(), current: (j['current'] ?? 0).toDouble(), days30: (j['days30'] ?? 0).toDouble(), days60: (j['days60'] ?? 0).toDouble(), days90: (j['days90'] ?? 0).toDouble(), days90Plus: (j['days90Plus'] ?? 0).toDouble());
}

class BiFinancialRatios {
  final double currentRatio, quickRatio, debtRatio, debtToEquity, grossMargin, netMargin, operatingMargin, roa, roe, workingCapital, breakEvenPoint;
  BiFinancialRatios({required this.currentRatio, required this.quickRatio, required this.debtRatio, required this.debtToEquity, required this.grossMargin, required this.netMargin, required this.operatingMargin, required this.roa, required this.roe, required this.workingCapital, required this.breakEvenPoint});
  factory BiFinancialRatios.fromJson(Map<String, dynamic> j) => BiFinancialRatios(currentRatio: (j['currentRatio'] ?? 0).toDouble(), quickRatio: (j['quickRatio'] ?? 0).toDouble(), debtRatio: (j['debtRatio'] ?? 0).toDouble(), debtToEquity: (j['debtToEquity'] ?? 0).toDouble(), grossMargin: (j['grossMargin'] ?? 0).toDouble(), netMargin: (j['netMargin'] ?? 0).toDouble(), operatingMargin: (j['operatingMargin'] ?? 0).toDouble(), roa: (j['roa'] ?? 0).toDouble(), roe: (j['roe'] ?? 0).toDouble(), workingCapital: (j['workingCapital'] ?? 0).toDouble(), breakEvenPoint: (j['breakEvenPoint'] ?? 0).toDouble());
}

class BiComparativeIncome {
  final BiIncomeStatement current, previous;
  final List<BiLineChange> changes;
  BiComparativeIncome({required this.current, required this.previous, required this.changes});
  factory BiComparativeIncome.fromJson(Map<String, dynamic> j) => BiComparativeIncome(current: BiIncomeStatement.fromJson(j['current']), previous: BiIncomeStatement.fromJson(j['previous']), changes: (j['changes'] as List?)?.map((e) => BiLineChange.fromJson(e)).toList() ?? []);
}

class BiIncomeStatement { final String periodName; final double totalIncome, totalCost, grossProfit, totalExpenses, netIncome; BiIncomeStatement({required this.periodName, required this.totalIncome, required this.totalCost, required this.grossProfit, required this.totalExpenses, required this.netIncome}); factory BiIncomeStatement.fromJson(Map<String, dynamic> j) => BiIncomeStatement(periodName: j['periodName'] ?? '', totalIncome: (j['totalIncome'] ?? 0).toDouble(), totalCost: (j['totalCost'] ?? 0).toDouble(), grossProfit: (j['grossProfit'] ?? 0).toDouble(), totalExpenses: (j['totalExpenses'] ?? 0).toDouble(), netIncome: (j['netIncome'] ?? 0).toDouble()); }

class BiLineChange { final String name; final double currentValue, previousValue, changePercent; BiLineChange({required this.name, required this.currentValue, required this.previousValue, required this.changePercent}); factory BiLineChange.fromJson(Map<String, dynamic> j) => BiLineChange(name: j['name'] ?? '', currentValue: (j['currentValue'] ?? 0).toDouble(), previousValue: (j['previousValue'] ?? 0).toDouble(), changePercent: (j['changePercent'] ?? 0).toDouble()); }

class BiCashFlow {
  final List<BiCashFlowItem> operating, investing, financing;
  final double netIncrease, beginningCash, endingCash;
  BiCashFlow({required this.operating, required this.investing, required this.financing, required this.netIncrease, required this.beginningCash, required this.endingCash});
  factory BiCashFlow.fromJson(Map<String, dynamic> j) => BiCashFlow(operating: (j['operating'] as List?)?.map((e) => BiCashFlowItem.fromJson(e)).toList() ?? [], investing: (j['investing'] as List?)?.map((e) => BiCashFlowItem.fromJson(e)).toList() ?? [], financing: (j['financing'] as List?)?.map((e) => BiCashFlowItem.fromJson(e)).toList() ?? [], netIncrease: (j['netIncrease'] ?? 0).toDouble(), beginningCash: (j['beginningCash'] ?? 0).toDouble(), endingCash: (j['endingCash'] ?? 0).toDouble());
}

class BiCashFlowItem { final String description; final double amount; final String type; BiCashFlowItem({required this.description, required this.amount, required this.type}); factory BiCashFlowItem.fromJson(Map<String, dynamic> j) => BiCashFlowItem(description: j['description'] ?? '', amount: (j['amount'] ?? 0).toDouble(), type: j['type'] ?? ''); }

class BiInventorySummary {
  final double totalValue, turnoverRate, deadStockValue;
  final int totalProducts, lowStockCount, outOfStockCount;
  final List<BiCategoryInventory> byCategory;
  BiInventorySummary({required this.totalValue, required this.totalProducts, required this.lowStockCount, required this.outOfStockCount, required this.turnoverRate, required this.deadStockValue, required this.byCategory});
  factory BiInventorySummary.fromJson(Map<String, dynamic> j) => BiInventorySummary(totalValue: (j['totalValue'] ?? 0).toDouble(), totalProducts: j['totalProducts'] ?? 0, lowStockCount: j['lowStockCount'] ?? 0, outOfStockCount: j['outOfStockCount'] ?? 0, turnoverRate: (j['turnoverRate'] ?? 0).toDouble(), deadStockValue: (j['deadStockValue'] ?? 0).toDouble(), byCategory: (j['byCategory'] as List?)?.map((e) => BiCategoryInventory.fromJson(e)).toList() ?? []);
}

class BiCategoryInventory { final String categoryName; final int count; final double totalCost, totalValue; BiCategoryInventory({required this.categoryName, required this.count, required this.totalCost, required this.totalValue}); factory BiCategoryInventory.fromJson(Map<String, dynamic> j) => BiCategoryInventory(categoryName: j['categoryName'] ?? '', count: j['count'] ?? 0, totalCost: (j['totalCost'] ?? 0).toDouble(), totalValue: (j['totalValue'] ?? 0).toDouble()); }

class BiPayrollSummary {
  final List<BiPayrollDept> byDepartment;
  final List<BiPayrollTrend> trend;
  final double totalCost, averagePerEmployee, overtimeTotal, employerCostTotal;
  BiPayrollSummary({required this.byDepartment, required this.trend, required this.totalCost, required this.averagePerEmployee, required this.overtimeTotal, required this.employerCostTotal});
  factory BiPayrollSummary.fromJson(Map<String, dynamic> j) => BiPayrollSummary(byDepartment: (j['byDepartment'] as List?)?.map((e) => BiPayrollDept.fromJson(e)).toList() ?? [], trend: (j['trend'] as List?)?.map((e) => BiPayrollTrend.fromJson(e)).toList() ?? [], totalCost: (j['totalCost'] ?? 0).toDouble(), averagePerEmployee: (j['averagePerEmployee'] ?? 0).toDouble(), overtimeTotal: (j['overtimeTotal'] ?? 0).toDouble(), employerCostTotal: (j['employerCostTotal'] ?? 0).toDouble());
}

class BiPayrollDept { final String department; final double amount; final int employeeCount; BiPayrollDept({required this.department, required this.amount, required this.employeeCount}); factory BiPayrollDept.fromJson(Map<String, dynamic> j) => BiPayrollDept(department: j['department'] ?? '', amount: (j['amount'] ?? 0).toDouble(), employeeCount: j['employeeCount'] ?? 0); }

class BiPayrollTrend { final String period; final double grossPay, deductions, netPay, employerCosts; BiPayrollTrend({required this.period, required this.grossPay, required this.deductions, required this.netPay, required this.employerCosts}); factory BiPayrollTrend.fromJson(Map<String, dynamic> j) => BiPayrollTrend(period: j['period'] ?? '', grossPay: (j['grossPay'] ?? 0).toDouble(), deductions: (j['deductions'] ?? 0).toDouble(), netPay: (j['netPay'] ?? 0).toDouble(), employerCosts: (j['employerCosts'] ?? 0).toDouble()); }

class BiSalesPredictionDaily {
  final String date, dayOfWeek;
  final double predictedSales, lowerBound, upperBound;
  BiSalesPredictionDaily({required this.date, required this.dayOfWeek, required this.predictedSales, required this.lowerBound, required this.upperBound});
  factory BiSalesPredictionDaily.fromJson(Map<String, dynamic> j) => BiSalesPredictionDaily(
    date: j['date'] ?? '',
    dayOfWeek: j['dayOfWeek'] ?? '',
    predictedSales: (j['predictedSales'] ?? 0).toDouble(),
    lowerBound: (j['lowerBound'] ?? 0).toDouble(),
    upperBound: (j['upperBound'] ?? 0).toDouble(),
  );
}

class BiMonthlySalesPrediction {
  final double totalPredicted, dailyAverage;
  final List<BiSalesPredictionDaily> predictions;
  BiMonthlySalesPrediction({required this.totalPredicted, required this.dailyAverage, required this.predictions});
  factory BiMonthlySalesPrediction.fromJson(Map<String, dynamic> j) => BiMonthlySalesPrediction(
    totalPredicted: (j['totalPredicted'] ?? 0).toDouble(),
    dailyAverage: (j['dailyAverage'] ?? 0).toDouble(),
    predictions: (j['predictions'] as List?)?.map((e) => BiSalesPredictionDaily.fromJson(e)).toList() ?? [],
  );
}

class BiMonthlyProjection {
  final int month, year, remainingDays;
  final double salesSoFar, predictedTotal, projectedTotal;
  BiMonthlyProjection({required this.month, required this.year, required this.salesSoFar, required this.predictedTotal, required this.projectedTotal, required this.remainingDays});
  factory BiMonthlyProjection.fromJson(Map<String, dynamic> j) => BiMonthlyProjection(
    month: j['month'] ?? 0,
    year: j['year'] ?? 0,
    salesSoFar: (j['salesSoFar'] ?? 0).toDouble(),
    predictedTotal: (j['predictedTotal'] ?? 0).toDouble(),
    projectedTotal: (j['projectedTotal'] ?? 0).toDouble(),
    remainingDays: j['remainingDays'] ?? 0,
  );
}

class PurchaseRecommendationItem {
  final String productId, productCode, productName, priority;
  final String? categoryName, supplierName;
  final String? supplierId;
  final int currentStock, minStock, recommendedQuantity, lastMonthSold;
  final double costPrice, averageDailyDemand, daysUntilStockout;
  PurchaseRecommendationItem({
    required this.productId, required this.productCode, required this.productName, required this.priority,
    this.categoryName, this.supplierName, this.supplierId,
    required this.currentStock, required this.minStock, required this.recommendedQuantity, required this.lastMonthSold,
    required this.costPrice, required this.averageDailyDemand, required this.daysUntilStockout,
  });
  factory PurchaseRecommendationItem.fromJson(Map<String, dynamic> j) => PurchaseRecommendationItem(
    productId: j['productId'] ?? '',
    productCode: j['productCode'] ?? '',
    productName: j['productName'] ?? '',
    priority: j['priority'] ?? 'low',
    categoryName: j['categoryName'],
    supplierName: j['supplierName'],
    supplierId: j['supplierId']?.toString(),
    currentStock: j['currentStock'] ?? 0,
    minStock: j['minStock'] ?? 0,
    recommendedQuantity: j['recommendedQuantity'] ?? 0,
    lastMonthSold: j['lastMonthSold'] ?? 0,
    costPrice: (j['costPrice'] ?? 0).toDouble(),
    averageDailyDemand: (j['averageDailyDemand'] ?? 0).toDouble(),
    daysUntilStockout: (j['daysUntilStockout'] ?? 0).toDouble(),
  );
}

class PurchaseRecommendationSummary {
  final int totalProducts, criticalCount, warningCount, healthyCount;
  final double totalRecommendedCost;
  final List<PurchaseRecommendationItem> recommendations;
  PurchaseRecommendationSummary({
    required this.totalProducts, required this.criticalCount, required this.warningCount, required this.healthyCount,
    required this.totalRecommendedCost, required this.recommendations,
  });
  factory PurchaseRecommendationSummary.fromJson(Map<String, dynamic> j) => PurchaseRecommendationSummary(
    totalProducts: j['totalProducts'] ?? 0,
    criticalCount: j['criticalCount'] ?? 0,
    warningCount: j['warningCount'] ?? 0,
    healthyCount: j['healthyCount'] ?? 0,
    totalRecommendedCost: (j['totalRecommendedCost'] ?? 0).toDouble(),
    recommendations: (j['recommendations'] as List?)?.map((e) => PurchaseRecommendationItem.fromJson(e)).toList() ?? [],
  );
}

class ExpenseClassificationResult {
  final String accountId, accountCode, accountName;
  final double confidence;
  ExpenseClassificationResult({required this.accountId, required this.accountCode, required this.accountName, required this.confidence});
  factory ExpenseClassificationResult.fromJson(Map<String, dynamic> j) => ExpenseClassificationResult(
    accountId: j['accountId']?.toString() ?? '',
    accountCode: j['accountCode'] ?? '',
    accountName: j['accountName'] ?? '',
    confidence: (j['confidence'] ?? 0).toDouble(),
  );
}

class ExpenseClassificationResponse {
  final String description;
  final double amount;
  final List<ExpenseClassificationResult> suggestions;
  ExpenseClassificationResponse({required this.description, required this.amount, required this.suggestions});
  factory ExpenseClassificationResponse.fromJson(Map<String, dynamic> j) => ExpenseClassificationResponse(
    description: j['description'] ?? '',
    amount: (j['amount'] ?? 0).toDouble(),
    suggestions: (j['suggestions'] as List?)?.map((e) => ExpenseClassificationResult.fromJson(e)).toList() ?? [],
  );
}

class AccountingAnomaly {
  final String accountingEntryId, entryNumber, description, referenceType, anomalyType, detail, severity;
  final String entryDate;
  final double totalDebit, totalCredit;
  AccountingAnomaly({
    required this.accountingEntryId, required this.entryNumber, required this.description,
    required this.referenceType, required this.anomalyType, required this.detail,
    required this.severity, required this.entryDate, required this.totalDebit, required this.totalCredit,
  });
  factory AccountingAnomaly.fromJson(Map<String, dynamic> j) => AccountingAnomaly(
    accountingEntryId: j['accountingEntryId']?.toString() ?? '',
    entryNumber: j['entryNumber'] ?? '',
    description: j['description'] ?? '',
    referenceType: j['referenceType'] ?? '',
    anomalyType: j['anomalyType'] ?? '',
    detail: j['detail'] ?? '',
    severity: j['severity'] ?? 'low',
    entryDate: j['entryDate'] ?? '',
    totalDebit: (j['totalDebit'] ?? 0).toDouble(),
    totalCredit: (j['totalCredit'] ?? 0).toDouble(),
  );
}

class AccountingAnomalyReport {
  final int totalEntriesAnalyzed, anomalyCount;
  final List<AccountingAnomaly> anomalies;
  AccountingAnomalyReport({required this.totalEntriesAnalyzed, required this.anomalyCount, required this.anomalies});
  factory AccountingAnomalyReport.fromJson(Map<String, dynamic> j) => AccountingAnomalyReport(
    totalEntriesAnalyzed: j['totalEntriesAnalyzed'] ?? 0,
    anomalyCount: j['anomalyCount'] ?? 0,
    anomalies: (j['anomalies'] as List?)?.map((e) => AccountingAnomaly.fromJson(e)).toList() ?? [],
  );
}
