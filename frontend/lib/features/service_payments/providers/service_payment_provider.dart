import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';

// ── Modelos del Pago a Prestadores de Servicios ──

final class ServiceProviderPaymentItem {
  final String id;
  final String paymentNumber;
  final String employeeId;
  final String? employeeName;
  final String? employeeCode;
  final String? branchId;
  final String? branchName;
  final int periodYear;
  final int periodMonth;
  final int periodHalf;
  final String periodLabel;
  final String paymentDate;
  final String status;
  final bool goalMet;
  final double totalAccumulated;
  final double totalDeductions;
  final double netAmount;
  final String createdAt;

  const ServiceProviderPaymentItem({
    required this.id, required this.paymentNumber, required this.employeeId,
    this.employeeName, this.employeeCode, this.branchId, this.branchName,
    required this.periodYear, required this.periodMonth, required this.periodHalf,
    required this.periodLabel, required this.paymentDate, required this.status,
    required this.goalMet, required this.totalAccumulated,
    required this.totalDeductions, required this.netAmount, required this.createdAt,
  });

  factory ServiceProviderPaymentItem.fromJson(Map<String, dynamic> j) => ServiceProviderPaymentItem(
    id: j['id'] as String,
    paymentNumber: j['paymentNumber'] as String? ?? '',
    employeeId: j['employeeId'] as String? ?? '',
    employeeName: j['employeeName'] as String?,
    employeeCode: j['employeeCode'] as String?,
    branchId: j['branchId'] as String?,
    branchName: j['branchName'] as String?,
    periodYear: (j['periodYear'] as num?)?.toInt() ?? 0,
    periodMonth: (j['periodMonth'] as num?)?.toInt() ?? 0,
    periodHalf: (j['periodHalf'] as num?)?.toInt() ?? 0,
    periodLabel: j['periodLabel'] as String? ?? '',
    paymentDate: j['paymentDate'] as String? ?? '',
    status: j['status'] as String? ?? '',
    goalMet: j['goalMet'] as bool? ?? false,
    totalAccumulated: (j['totalAccumulated'] as num?)?.toDouble() ?? 0,
    totalDeductions: (j['totalDeductions'] as num?)?.toDouble() ?? 0,
    netAmount: (j['netAmount'] as num?)?.toDouble() ?? 0,
    createdAt: j['createdAt'] as String? ?? '',
  );
}

final class ServiceProviderPaymentLineItem {
  final String id;
  final String lineName;
  final double collectedAmount;
  final double contractedAmount;
  final double commissionRate;
  final double commissionAmount;
  final bool lineGoalMet;

  const ServiceProviderPaymentLineItem({
    required this.id, required this.lineName, required this.collectedAmount,
    required this.contractedAmount, required this.commissionRate,
    required this.commissionAmount, required this.lineGoalMet,
  });

  factory ServiceProviderPaymentLineItem.fromJson(Map<String, dynamic> j) => ServiceProviderPaymentLineItem(
    id: j['id'] as String? ?? '',
    lineName: j['lineName'] as String? ?? '',
    collectedAmount: (j['collectedAmount'] as num?)?.toDouble() ?? 0,
    contractedAmount: (j['contractedAmount'] as num?)?.toDouble() ?? 0,
    commissionRate: (j['commissionRate'] as num?)?.toDouble() ?? 0,
    commissionAmount: (j['commissionAmount'] as num?)?.toDouble() ?? 0,
    lineGoalMet: j['lineGoalMet'] as bool? ?? false,
  );
}

final class ServiceProviderPaymentDeductionItem {
  final String id;
  final String deductionType;
  final String? description;
  final double amount;
  final String? agreementReference;
  final String? agreementDate;

  const ServiceProviderPaymentDeductionItem({
    required this.id, required this.deductionType, this.description,
    required this.amount, this.agreementReference, this.agreementDate,
  });

  factory ServiceProviderPaymentDeductionItem.fromJson(Map<String, dynamic> j) => ServiceProviderPaymentDeductionItem(
    id: j['id'] as String? ?? '',
    deductionType: j['deductionType'] as String? ?? '',
    description: j['description'] as String?,
    amount: (j['amount'] as num?)?.toDouble() ?? 0,
    agreementReference: j['agreementReference'] as String?,
    agreementDate: j['agreementDate'] as String?,
  );
}

final class ServiceProviderPaymentDetail {
  final String id;
  final String paymentNumber;
  final String employeeId;
  final String? employeeName;
  final String? employeeCode;
  final String? branchId;
  final String? branchName;
  final int periodYear;
  final int periodMonth;
  final int periodHalf;
  final String periodLabel;
  final String paymentDate;
  final String status;
  final String goalEvaluationMode;
  final bool goalMet;
  final double goalBonusAmount;
  final double otherIncome;
  final double totalCollected;
  final double totalContracted;
  final double totalCommission;
  final double totalAccumulated;
  final double totalDeductions;
  final double netAmount;
  final String? notes;
  final String createdAt;
  final String? paidAt;
  final String? paymentReference;
  final List<ServiceProviderPaymentLineItem> lines;
  final List<ServiceProviderPaymentDeductionItem> deductions;

  const ServiceProviderPaymentDetail({
    required this.id, required this.paymentNumber, required this.employeeId,
    this.employeeName, this.employeeCode, this.branchId, this.branchName,
    required this.periodYear, required this.periodMonth, required this.periodHalf,
    required this.periodLabel, required this.paymentDate, required this.status,
    required this.goalEvaluationMode, required this.goalMet,
    required this.goalBonusAmount, required this.otherIncome,
    required this.totalCollected, required this.totalContracted,
    required this.totalCommission, required this.totalAccumulated,
    required this.totalDeductions, required this.netAmount, this.notes,
    required this.createdAt, this.paidAt, this.paymentReference,
    required this.lines, required this.deductions,
  });

  factory ServiceProviderPaymentDetail.fromJson(Map<String, dynamic> j) => ServiceProviderPaymentDetail(
    id: j['id'] as String,
    paymentNumber: j['paymentNumber'] as String? ?? '',
    employeeId: j['employeeId'] as String? ?? '',
    employeeName: j['employeeName'] as String?,
    employeeCode: j['employeeCode'] as String?,
    branchId: j['branchId'] as String?,
    branchName: j['branchName'] as String?,
    periodYear: (j['periodYear'] as num?)?.toInt() ?? 0,
    periodMonth: (j['periodMonth'] as num?)?.toInt() ?? 0,
    periodHalf: (j['periodHalf'] as num?)?.toInt() ?? 0,
    periodLabel: j['periodLabel'] as String? ?? '',
    paymentDate: j['paymentDate'] as String? ?? '',
    status: j['status'] as String? ?? '',
    goalEvaluationMode: j['goalEvaluationMode'] as String? ?? 'total',
    goalMet: j['goalMet'] as bool? ?? false,
    goalBonusAmount: (j['goalBonusAmount'] as num?)?.toDouble() ?? 0,
    otherIncome: (j['otherIncome'] as num?)?.toDouble() ?? 0,
    totalCollected: (j['totalCollected'] as num?)?.toDouble() ?? 0,
    totalContracted: (j['totalContracted'] as num?)?.toDouble() ?? 0,
    totalCommission: (j['totalCommission'] as num?)?.toDouble() ?? 0,
    totalAccumulated: (j['totalAccumulated'] as num?)?.toDouble() ?? 0,
    totalDeductions: (j['totalDeductions'] as num?)?.toDouble() ?? 0,
    netAmount: (j['netAmount'] as num?)?.toDouble() ?? 0,
    notes: j['notes'] as String?,
    createdAt: j['createdAt'] as String? ?? '',
    paidAt: j['paidAt'] as String?,
    paymentReference: j['paymentReference'] as String?,
    lines: (j['lines'] as List?)?.map((e) => ServiceProviderPaymentLineItem.fromJson(e)).toList() ?? const [],
    deductions: (j['deductions'] as List?)?.map((e) => ServiceProviderPaymentDeductionItem.fromJson(e)).toList() ?? const [],
  );
}

// ── Payloads de creación/edición ──

final class ServicePaymentLineInput {
  final String lineName;
  final double collectedAmount;
  final double contractedAmount;
  final double commissionRate;

  const ServicePaymentLineInput({
    required this.lineName, required this.collectedAmount,
    required this.contractedAmount, required this.commissionRate,
  });

  Map<String, dynamic> toJson() => {
    'lineName': lineName,
    'collectedAmount': collectedAmount,
    'contractedAmount': contractedAmount,
    'commissionRate': commissionRate,
  };
}

final class ServicePaymentDeductionInput {
  final String deductionType;
  final String? description;
  final double amount;
  final String? agreementReference;
  final String? agreementDate;

  const ServicePaymentDeductionInput({
    required this.deductionType, this.description, required this.amount,
    this.agreementReference, this.agreementDate,
  });

  Map<String, dynamic> toJson() => {
    'deductionType': deductionType,
    'description': description,
    'amount': amount,
    'agreementReference': agreementReference,
    'agreementDate': agreementDate,
  };
}

// ── Listado (historial por agente/período/estado) ──

final class ServicePaymentNotifier extends AsyncNotifier<List<ServiceProviderPaymentItem>> {
  @override
  Future<List<ServiceProviderPaymentItem>> build() async {
    ref.watch(branchScopeProvider);
    return _fetch();
  }

  Future<void> load({String? search, String? status, int? year}) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => _fetch(search: search, status: status, year: year));
  }

  Future<List<ServiceProviderPaymentItem>> _fetch({String? search, String? status, int? year}) async {
    final dio = ref.read(dioClientProvider);
    final params = <String, dynamic>{'pageSize': 100};
    if (search != null && search.isNotEmpty) params['search'] = search;
    if (status != null && status.isNotEmpty) params['status'] = status;
    if (year != null) params['year'] = year;
    final r = await dio.get('service-provider-payments', params: params);
    final data = r.data;
    return (data['items'] as List).map((e) => ServiceProviderPaymentItem.fromJson(e)).toList();
  }
}

final servicePaymentProvider =
    AsyncNotifierProvider<ServicePaymentNotifier, List<ServiceProviderPaymentItem>>(ServicePaymentNotifier.new);
