class EquityChangeItem {
  final String concept;
  final double openingBalance;
  final double additions;
  final double deductions;
  final double endingBalance;

  const EquityChangeItem({
    required this.concept, required this.openingBalance,
    required this.additions, required this.deductions, required this.endingBalance,
  });

  factory EquityChangeItem.fromJson(Map<String, dynamic> j) => EquityChangeItem(
    concept: j['concept'] as String? ?? '',
    openingBalance: (j['openingBalance'] as num?)?.toDouble() ?? 0,
    additions: (j['additions'] as num?)?.toDouble() ?? 0,
    deductions: (j['deductions'] as num?)?.toDouble() ?? 0,
    endingBalance: (j['endingBalance'] as num?)?.toDouble() ?? 0,
  );
}

class EquityChangeResponse {
  final String periodName;
  final String generatedAt;
  final double totalOpeningEquity;
  final double totalAdditions;
  final double totalDeductions;
  final double totalEndingEquity;
  final List<EquityChangeItem> items;

  const EquityChangeResponse({
    required this.periodName, required this.generatedAt,
    required this.totalOpeningEquity, required this.totalAdditions,
    required this.totalDeductions, required this.totalEndingEquity,
    required this.items,
  });

  factory EquityChangeResponse.fromJson(Map<String, dynamic> j) => EquityChangeResponse(
    periodName: j['periodName'] as String? ?? '',
    generatedAt: j['generatedAt'] as String? ?? '',
    totalOpeningEquity: (j['totalOpeningEquity'] as num?)?.toDouble() ?? 0,
    totalAdditions: (j['totalAdditions'] as num?)?.toDouble() ?? 0,
    totalDeductions: (j['totalDeductions'] as num?)?.toDouble() ?? 0,
    totalEndingEquity: (j['totalEndingEquity'] as num?)?.toDouble() ?? 0,
    items: (j['items'] as List?)?.map((e) => EquityChangeItem.fromJson(e)).toList() ?? [],
  );
}

class CashFlowStatementItem {
  final String concept;
  final double amount;
  final String category;

  const CashFlowStatementItem({
    required this.concept, required this.amount, required this.category,
  });

  factory CashFlowStatementItem.fromJson(Map<String, dynamic> j) => CashFlowStatementItem(
    concept: j['concept'] as String? ?? '',
    amount: (j['amount'] as num?)?.toDouble() ?? 0,
    category: j['category'] as String? ?? '',
  );
}

class CashFlowStatementResponse {
  final String periodName;
  final String generatedAt;
  final List<CashFlowStatementItem> operatingActivities;
  final double netOperatingCashFlow;
  final List<CashFlowStatementItem> investingActivities;
  final double netInvestingCashFlow;
  final List<CashFlowStatementItem> financingActivities;
  final double netFinancingCashFlow;
  final double netCashIncrease;
  final double beginningCash;
  final double endingCash;

  const CashFlowStatementResponse({
    required this.periodName, required this.generatedAt,
    required this.operatingActivities, required this.netOperatingCashFlow,
    required this.investingActivities, required this.netInvestingCashFlow,
    required this.financingActivities, required this.netFinancingCashFlow,
    required this.netCashIncrease, required this.beginningCash, required this.endingCash,
  });

  factory CashFlowStatementResponse.fromJson(Map<String, dynamic> j) => CashFlowStatementResponse(
    periodName: j['periodName'] as String? ?? '',
    generatedAt: j['generatedAt'] as String? ?? '',
    operatingActivities: (j['operatingActivities'] as List?)?.map((e) => CashFlowStatementItem.fromJson(e)).toList() ?? [],
    netOperatingCashFlow: (j['netOperatingCashFlow'] as num?)?.toDouble() ?? 0,
    investingActivities: (j['investingActivities'] as List?)?.map((e) => CashFlowStatementItem.fromJson(e)).toList() ?? [],
    netInvestingCashFlow: (j['netInvestingCashFlow'] as num?)?.toDouble() ?? 0,
    financingActivities: (j['financingActivities'] as List?)?.map((e) => CashFlowStatementItem.fromJson(e)).toList() ?? [],
    netFinancingCashFlow: (j['netFinancingCashFlow'] as num?)?.toDouble() ?? 0,
    netCashIncrease: (j['netCashIncrease'] as num?)?.toDouble() ?? 0,
    beginningCash: (j['beginningCash'] as num?)?.toDouble() ?? 0,
    endingCash: (j['endingCash'] as num?)?.toDouble() ?? 0,
  );
}

class ComparativePeriod {
  final String periodName;
  final double amount;
  final double percentageOfTotal;

  const ComparativePeriod({
    required this.periodName, required this.amount, required this.percentageOfTotal,
  });

  factory ComparativePeriod.fromJson(Map<String, dynamic> j) => ComparativePeriod(
    periodName: j['periodName'] as String? ?? '',
    amount: (j['amount'] as num?)?.toDouble() ?? 0,
    percentageOfTotal: (j['percentageOfTotal'] as num?)?.toDouble() ?? 0,
  );
}

class ComparativeLine {
  final String concept;
  final String accountType;
  final List<ComparativePeriod> periods;
  final double variance;
  final double variancePercent;

  const ComparativeLine({
    required this.concept, required this.accountType,
    required this.periods, required this.variance, required this.variancePercent,
  });

  factory ComparativeLine.fromJson(Map<String, dynamic> j) => ComparativeLine(
    concept: j['concept'] as String? ?? '',
    accountType: j['accountType'] as String? ?? '',
    periods: (j['periods'] as List?)?.map((e) => ComparativePeriod.fromJson(e)).toList() ?? [],
    variance: (j['variance'] as num?)?.toDouble() ?? 0,
    variancePercent: (j['variancePercent'] as num?)?.toDouble() ?? 0,
  );
}

class ComparativeReportResponse {
  final String reportType;
  final String generatedAt;
  final List<ComparativeLine> lines;
  final double totalCurrent;
  final double totalPrevious;
  final double totalVariance;
  final double totalVariancePercent;

  const ComparativeReportResponse({
    required this.reportType, required this.generatedAt,
    required this.lines, required this.totalCurrent, required this.totalPrevious,
    required this.totalVariance, required this.totalVariancePercent,
  });

  factory ComparativeReportResponse.fromJson(Map<String, dynamic> j) => ComparativeReportResponse(
    reportType: j['reportType'] as String? ?? '',
    generatedAt: j['generatedAt'] as String? ?? '',
    lines: (j['lines'] as List?)?.map((e) => ComparativeLine.fromJson(e)).toList() ?? [],
    totalCurrent: (j['totalCurrent'] as num?)?.toDouble() ?? 0,
    totalPrevious: (j['totalPrevious'] as num?)?.toDouble() ?? 0,
    totalVariance: (j['totalVariance'] as num?)?.toDouble() ?? 0,
    totalVariancePercent: (j['totalVariancePercent'] as num?)?.toDouble() ?? 0,
  );
}

class FinancialAssistantResponse {
  final String answer;
  final String? confidence;
  final List<FinancialAssistantDataPoint>? supportingData;

  const FinancialAssistantResponse({
    required this.answer, this.confidence, this.supportingData,
  });

  factory FinancialAssistantResponse.fromJson(Map<String, dynamic> j) => FinancialAssistantResponse(
    answer: j['answer'] as String? ?? '',
    confidence: j['confidence'] as String?,
    supportingData: (j['supportingData'] as List?)?.map((e) => FinancialAssistantDataPoint.fromJson(e)).toList(),
  );
}

class FinancialAssistantDataPoint {
  final String label;
  final double value;
  final String? format;

  const FinancialAssistantDataPoint({
    required this.label, required this.value, this.format,
  });

  factory FinancialAssistantDataPoint.fromJson(Map<String, dynamic> j) => FinancialAssistantDataPoint(
    label: j['label'] as String? ?? '',
    value: (j['value'] as num?)?.toDouble() ?? 0,
    format: j['format'] as String?,
  );
}
