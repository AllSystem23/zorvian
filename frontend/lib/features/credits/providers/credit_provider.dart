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

final class CreditState {
  final List<CreditItem> items;
  final bool loading;
  final String? error;
  const CreditState({this.items = const [], this.loading = false, this.error});
  CreditState copyWith({List<CreditItem>? items, bool? loading, String? error}) =>
    CreditState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class CreditNotifier extends Notifier<CreditState> {
  @override
  CreditState build() => const CreditState();
  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('credits');
      final data = r.data;
      final list = data is List ? data : (data is Map && data['items'] is List ? data['items'] : []);
      state = CreditState(items: (list as List).map((e) => CreditItem.fromJson(e as Map<String, dynamic>)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar créditos', loading: false);
    }
  }
}

final creditProvider = NotifierProvider<CreditNotifier, CreditState>(CreditNotifier.new);
