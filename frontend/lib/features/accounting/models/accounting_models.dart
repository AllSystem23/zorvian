// Move models from accounting_provider.dart
final class AccountItem {
  final String id;
  final String code;
  final String name;
  final String? description;
  final String type;
  final String normalSide;
  final String? parentId;
  final String? parentName;
  final int level;
  final bool isActive;
  final bool isSystem;
  final double openingBalance;
  final double currentBalance;
  final List<AccountItem> children;

  const AccountItem({
    required this.id, required this.code, required this.name, this.description,
    required this.type, required this.normalSide, this.parentId, this.parentName,
    required this.level, required this.isActive, this.isSystem = false, required this.openingBalance,
    required this.currentBalance, this.children = const [],
  });

  factory AccountItem.fromJson(Map<String, dynamic> j) => AccountItem(
    id: j['id'] as String? ?? '',
    code: j['code'] as String? ?? '',
    name: j['name'] as String? ?? '',
    description: j['description'] as String?,
    type: j['type'] as String? ?? '',
    normalSide: j['normalSide'] as String? ?? '',
    parentId: j['parentId'] as String?,
    parentName: j['parentName'] as String?,
    level: (j['level'] as num?)?.toInt() ?? 0,
    isActive: j['isActive'] as bool? ?? true,
    isSystem: j['isSystem'] as bool? ?? false,
    openingBalance: (j['openingBalance'] as num?)?.toDouble() ?? 0,
    currentBalance: (j['currentBalance'] as num?)?.toDouble() ?? 0,
    children: (j['children'] as List?)?.map((e) => AccountItem.fromJson(e)).toList() ?? [],
  );
}

final class AccountingEntryItem {
  final String id;
  final String entryNumber;
  final String entryDate;
  final String description;
  final String referenceType;
  final String status;
  final double totalDebit;
  final double totalCredit;
  final String? periodName;

  const AccountingEntryItem({
    required this.id, required this.entryNumber, required this.entryDate,
    required this.description, required this.referenceType, required this.status,
    required this.totalDebit, required this.totalCredit, this.periodName,
  });

  factory AccountingEntryItem.fromJson(Map<String, dynamic> j) => AccountingEntryItem(
    id: j['id'] as String? ?? '',
    entryNumber: j['entryNumber'] as String? ?? '',
    entryDate: j['entryDate'] as String? ?? '',
    description: j['description'] as String? ?? '',
    referenceType: j['referenceType'] as String? ?? '',
    status: j['status'] as String? ?? '',
    totalDebit: (j['totalDebit'] as num?)?.toDouble() ?? 0,
    totalCredit: (j['totalCredit'] as num?)?.toDouble() ?? 0,
    periodName: j['periodName'] as String?,
  );
}

final class TrialBalanceItem {
  final String accountCode;
  final String accountName;
  final String accountType;
  final double openingBalance;
  final double debitMovements;
  final double creditMovements;
  final double endingBalance;

  const TrialBalanceItem({
    required this.accountCode, required this.accountName, required this.accountType,
    required this.openingBalance, required this.debitMovements, required this.creditMovements,
    required this.endingBalance,
  });

  factory TrialBalanceItem.fromJson(Map<String, dynamic> j) => TrialBalanceItem(
    accountCode: j['accountCode'] as String? ?? '',
    accountName: j['accountName'] as String? ?? '',
    accountType: j['accountType'] as String? ?? '',
    openingBalance: (j['openingBalance'] as num?)?.toDouble() ?? 0,
    debitMovements: (j['debitMovements'] as num?)?.toDouble() ?? 0,
    creditMovements: (j['creditMovements'] as num?)?.toDouble() ?? 0,
    endingBalance: (j['endingBalance'] as num?)?.toDouble() ?? 0,
  );
}

final class TrialBalanceData {
  final String periodName;
  final List<TrialBalanceItem> items;
  const TrialBalanceData({required this.periodName, required this.items});
  factory TrialBalanceData.fromJson(Map<String, dynamic> j) => TrialBalanceData(
    periodName: j['periodName'] as String? ?? '',
    items: (j['items'] as List?)?.map((e) => TrialBalanceItem.fromJson(e)).toList() ?? [],
  );
}

final class IncomeStatementData {
  final String periodName;
  final double totalIncome;
  final double totalCost;
  final double grossProfit;
  final double totalExpenses;
  final double netIncome;

  const IncomeStatementData({
    required this.periodName, required this.totalIncome, required this.totalCost,
    required this.grossProfit, required this.totalExpenses, required this.netIncome,
  });

  factory IncomeStatementData.fromJson(Map<String, dynamic> j) => IncomeStatementData(
    periodName: j['periodName'] as String? ?? '',
    totalIncome: (j['totalIncome'] as num?)?.toDouble() ?? 0,
    totalCost: (j['totalCost'] as num?)?.toDouble() ?? 0,
    grossProfit: (j['grossProfit'] as num?)?.toDouble() ?? 0,
    totalExpenses: (j['totalExpenses'] as num?)?.toDouble() ?? 0,
    netIncome: (j['netIncome'] as num?)?.toDouble() ?? 0,
  );
}

final class AccountLinkItem {
  final String id;
  final String transactionType;
  final String role;
  final String accountId;
  final String accountCode;
  final String accountName;

  const AccountLinkItem({
    required this.id, required this.transactionType, required this.role,
    required this.accountId, required this.accountCode, required this.accountName,
  });

  factory AccountLinkItem.fromJson(Map<String, dynamic> j) => AccountLinkItem(
    id: j['id'] as String? ?? '',
    transactionType: j['transactionType'] as String? ?? '',
    role: j['role'] as String? ?? '',
    accountId: j['accountId'] as String? ?? '',
    accountCode: j['accountCode'] as String? ?? '',
    accountName: j['accountName'] as String? ?? '',
  );
}

final class AccountingPeriodItem {
  final String id;
  final int year;
  final int month;
  final String name;
  final String status;

  const AccountingPeriodItem({
    required this.id, required this.year, required this.month,
    required this.name, required this.status,
  });

  factory AccountingPeriodItem.fromJson(Map<String, dynamic> j) => AccountingPeriodItem(
    id: j['id'] as String? ?? '',
    year: (j['year'] as num?)?.toInt() ?? 0,
    month: (j['month'] as num?)?.toInt() ?? 0,
    name: j['name'] as String? ?? '',
    status: j['status'] as String? ?? '',
  );
}
