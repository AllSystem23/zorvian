import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class CreditItem {
  final String id;
  final String creditNumber;
  final String clientName;
  final double financedAmount;
  final double balance;
  final double paidAmount;
  final String startDate;
  final String endDate;
  final String status;

  const CreditItem({
    required this.id, required this.creditNumber, required this.clientName,
    required this.financedAmount, required this.balance, required this.paidAmount,
    required this.startDate, required this.endDate, required this.status,
  });

  factory CreditItem.fromJson(Map<String, dynamic> j) => CreditItem(
    id: j['id'] as String? ?? '',
    creditNumber: j['creditNumber'] as String? ?? '',
    clientName: j['clientName'] as String? ?? '',
    financedAmount: (j['financedAmount'] as num?)?.toDouble() ?? 0,
    balance: (j['balance'] as num?)?.toDouble() ?? 0,
    paidAmount: (j['paidAmount'] as num?)?.toDouble() ?? 0,
    startDate: j['startDate'] as String? ?? '',
    endDate: j['endDate'] as String? ?? '',
    status: j['status'] as String? ?? '',
  );
}

final class CreditDetail {
  final String id;
  final String creditNumber;
  final String clientName;
  final double financedAmount;
  final double interestRate;
  final int installmentCount;
  final double installmentAmount;
  final double totalAmount;
  final double paidAmount;
  final double balance;
  final double interestAmount;
  final String startDate;
  final String endDate;
  final String? nextDueDate;
  final String status;
  final String? notes;
  final List<CreditInstallment> installments;

  const CreditDetail({
    required this.id, required this.creditNumber, required this.clientName,
    required this.financedAmount, required this.interestRate, required this.installmentCount,
    required this.installmentAmount, required this.totalAmount, required this.paidAmount,
    required this.balance, required this.interestAmount, required this.startDate,
    required this.endDate, this.nextDueDate, required this.status, this.notes,
    required this.installments,
  });

  factory CreditDetail.fromJson(Map<String, dynamic> j) => CreditDetail(
    id: j['id'] as String? ?? '',
    creditNumber: j['creditNumber'] as String? ?? '',
    clientName: j['clientName'] as String? ?? '',
    financedAmount: (j['financedAmount'] as num?)?.toDouble() ?? 0,
    interestRate: (j['interestRate'] as num?)?.toDouble() ?? 0,
    installmentCount: j['installmentCount'] as int? ?? 0,
    installmentAmount: (j['installmentAmount'] as num?)?.toDouble() ?? 0,
    totalAmount: (j['totalAmount'] as num?)?.toDouble() ?? 0,
    paidAmount: (j['paidAmount'] as num?)?.toDouble() ?? 0,
    balance: (j['balance'] as num?)?.toDouble() ?? 0,
    interestAmount: (j['interestAmount'] as num?)?.toDouble() ?? 0,
    startDate: j['startDate'] as String? ?? '',
    endDate: j['endDate'] as String? ?? '',
    nextDueDate: j['nextDueDate'] as String?,
    status: j['status'] as String? ?? '',
    notes: j['notes'] as String?,
    installments: (j['installments'] as List?)?.map((e) => CreditInstallment.fromJson(e)).toList() ?? [],
  );
}

final class CreditInstallment {
  final String id;
  final int installmentNumber;
  final String dueDate;
  final double amount;
  final double principalAmount;
  final double interestAmount;
  final double paidAmount;
  final double balance;
  final String status;

  const CreditInstallment({
    required this.id, required this.installmentNumber, required this.dueDate,
    required this.amount, required this.principalAmount, required this.interestAmount,
    required this.paidAmount, required this.balance, required this.status,
  });

  factory CreditInstallment.fromJson(Map<String, dynamic> j) => CreditInstallment(
    id: j['id'] as String? ?? '',
    installmentNumber: j['installmentNumber'] as int? ?? 0,
    dueDate: j['dueDate'] as String? ?? '',
    amount: (j['amount'] as num?)?.toDouble() ?? 0,
    principalAmount: (j['principalAmount'] as num?)?.toDouble() ?? 0,
    interestAmount: (j['interestAmount'] as num?)?.toDouble() ?? 0,
    paidAmount: (j['paidAmount'] as num?)?.toDouble() ?? 0,
    balance: (j['balance'] as num?)?.toDouble() ?? 0,
    status: j['status'] as String? ?? '',
  );

  bool get isOverdue => status == 'late';
  bool get isPaid => status == 'paid';
}

final class OverdueInstallment {
  final String id;
  final int installmentNumber;
  final String dueDate;
  final double amount;
  final double balance;
  final int daysOverdue;
  final String status;

  const OverdueInstallment({
    required this.id, required this.installmentNumber, required this.dueDate,
    required this.amount, required this.balance, required this.daysOverdue,
    required this.status,
  });

  factory OverdueInstallment.fromJson(Map<String, dynamic> j) => OverdueInstallment(
    id: j['id'] as String? ?? '',
    installmentNumber: j['installmentNumber'] as int? ?? 0,
    dueDate: j['dueDate'] as String? ?? '',
    amount: (j['amount'] as num?)?.toDouble() ?? 0,
    balance: (j['balance'] as num?)?.toDouble() ?? 0,
    daysOverdue: j['daysOverdue'] as int? ?? 0,
    status: j['status'] as String? ?? '',
  );
}

final class LateFee {
  final String id;
  final String creditId;
  final int daysOverdue;
  final double feeAmount;
  final double interestAmount;
  final double totalAmount;
  final double paidAmount;
  final double balance;
  final String status;
  final String calculatedAt;
  final String? notes;

  const LateFee({
    required this.id, required this.creditId, required this.daysOverdue,
    required this.feeAmount, required this.interestAmount, required this.totalAmount,
    required this.paidAmount, required this.balance, required this.status,
    required this.calculatedAt, this.notes,
  });

  factory LateFee.fromJson(Map<String, dynamic> j) => LateFee(
    id: j['id'] as String? ?? '',
    creditId: j['creditId'] as String? ?? '',
    daysOverdue: j['daysOverdue'] as int? ?? 0,
    feeAmount: (j['feeAmount'] as num?)?.toDouble() ?? 0,
    interestAmount: (j['interestAmount'] as num?)?.toDouble() ?? 0,
    totalAmount: (j['totalAmount'] as num?)?.toDouble() ?? 0,
    paidAmount: (j['paidAmount'] as num?)?.toDouble() ?? 0,
    balance: (j['balance'] as num?)?.toDouble() ?? 0,
    status: j['status'] as String? ?? '',
    calculatedAt: j['calculatedAt'] as String? ?? '',
    notes: j['notes'] as String?,
  );
}

final class CollectionAction {
  final String id;
  final String creditId;
  final String employeeName;
  final String actionType;
  final String? description;
  final String actionDate;
  final String? followUpDate;
  final String? contactPerson;
  final String? contactPhone;
  final String? promiseAmount;
  final String? promiseDate;
  final String status;
  final String? result;

  const CollectionAction({
    required this.id, required this.creditId, required this.employeeName,
    required this.actionType, this.description, required this.actionDate,
    this.followUpDate, this.contactPerson, this.contactPhone,
    this.promiseAmount, this.promiseDate, required this.status, this.result,
  });

  factory CollectionAction.fromJson(Map<String, dynamic> j) => CollectionAction(
    id: j['id'] as String? ?? '',
    creditId: j['creditId'] as String? ?? '',
    employeeName: j['employeeName'] as String? ?? '',
    actionType: j['actionType'] as String? ?? '',
    description: j['description'] as String?,
    actionDate: j['actionDate'] as String? ?? '',
    followUpDate: j['followUpDate'] as String?,
    contactPerson: j['contactPerson'] as String?,
    contactPhone: j['contactPhone'] as String?,
    promiseAmount: j['promiseAmount'] as String?,
    promiseDate: j['promiseDate'] as String?,
    status: j['status'] as String? ?? '',
    result: j['result'] as String?,
  );
}

final class CreditRefinancing {
  final String id;
  final double previousBalance;
  final double previousInterestRate;
  final int previousInstallmentCount;
  final double newFinancedAmount;
  final double newInterestRate;
  final int newInstallmentCount;
  final double newInstallmentAmount;
  final double newTotalAmount;
  final String newStartDate;
  final String newEndDate;
  final String reason;

  const CreditRefinancing({
    required this.id, required this.previousBalance, required this.previousInterestRate,
    required this.previousInstallmentCount, required this.newFinancedAmount,
    required this.newInterestRate, required this.newInstallmentCount,
    required this.newInstallmentAmount, required this.newTotalAmount,
    required this.newStartDate, required this.newEndDate, required this.reason,
  });

  factory CreditRefinancing.fromJson(Map<String, dynamic> j) => CreditRefinancing(
    id: j['id'] as String? ?? '',
    previousBalance: (j['previousBalance'] as num?)?.toDouble() ?? 0,
    previousInterestRate: (j['previousInterestRate'] as num?)?.toDouble() ?? 0,
    previousInstallmentCount: j['previousInstallmentCount'] as int? ?? 0,
    newFinancedAmount: (j['newFinancedAmount'] as num?)?.toDouble() ?? 0,
    newInterestRate: (j['newInterestRate'] as num?)?.toDouble() ?? 0,
    newInstallmentCount: j['newInstallmentCount'] as int? ?? 0,
    newInstallmentAmount: (j['newInstallmentAmount'] as num?)?.toDouble() ?? 0,
    newTotalAmount: (j['newTotalAmount'] as num?)?.toDouble() ?? 0,
    newStartDate: j['newStartDate'] as String? ?? '',
    newEndDate: j['newEndDate'] as String? ?? '',
    reason: j['reason'] as String? ?? '',
  );
}

final class OverdueAgingBucket {
  final String label;
  final int creditCount;
  final int installmentCount;
  final double totalBalance;
  final double totalAmount;
  const OverdueAgingBucket({required this.label, required this.creditCount, required this.installmentCount, required this.totalBalance, required this.totalAmount});
  factory OverdueAgingBucket.fromJson(Map<String, dynamic> j) => OverdueAgingBucket(
    label: j['label'] as String? ?? '',
    creditCount: j['creditCount'] as int? ?? 0,
    installmentCount: j['installmentCount'] as int? ?? 0,
    totalBalance: (j['totalBalance'] as num?)?.toDouble() ?? 0,
    totalAmount: (j['totalAmount'] as num?)?.toDouble() ?? 0,
  );
}

final class OverdueDashboard {
  final int totalOverdueCredits;
  final int totalActiveCredits;
  final double totalPortfolio;
  final double totalOverdueBalance;
  final double recoveryRate;
  final List<OverdueAgingBucket> agingBuckets;
  final List<OverdueInstallment> criticalOverdue;

  const OverdueDashboard({
    required this.totalOverdueCredits, required this.totalActiveCredits,
    required this.totalPortfolio, required this.totalOverdueBalance,
    required this.recoveryRate, required this.agingBuckets,
    required this.criticalOverdue,
  });

  factory OverdueDashboard.fromJson(Map<String, dynamic> j) => OverdueDashboard(
    totalOverdueCredits: j['totalOverdueCredits'] as int? ?? 0,
    totalActiveCredits: j['totalActiveCredits'] as int? ?? 0,
    totalPortfolio: (j['totalPortfolio'] as num?)?.toDouble() ?? 0,
    totalOverdueBalance: (j['totalOverdueBalance'] as num?)?.toDouble() ?? 0,
    recoveryRate: (j['recoveryRate'] as num?)?.toDouble() ?? 0,
    agingBuckets: (j['agingBuckets'] as List?)?.map((e) => OverdueAgingBucket.fromJson(e)).toList() ?? [],
    criticalOverdue: (j['criticalOverdue'] as List?)?.map((e) => OverdueInstallment.fromJson(e)).toList() ?? [],
  );
}

final class CreditNotifier extends AsyncNotifier<List<CreditItem>> {
  @override
  FutureOr<List<CreditItem>> build() => _load();

  Future<List<CreditItem>> _load({String? search}) async {
    final dio = ref.read(dioClientProvider);
    final params = <String, dynamic>{};
    if (search != null && search.isNotEmpty) params['search'] = search;
    final r = await dio.get('credits', params: params);
    final data = r.data;
    final list = data is List ? data : (data is Map && data['items'] is List ? data['items'] : []);
    return (list as List).map((e) => CreditItem.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<void> load({String? search}) async {
    state = const AsyncValue.loading();
    state = await AsyncValue.guard(() => _load(search: search));
  }
}

final creditProvider = AsyncNotifierProvider<CreditNotifier, List<CreditItem>>(CreditNotifier.new);
