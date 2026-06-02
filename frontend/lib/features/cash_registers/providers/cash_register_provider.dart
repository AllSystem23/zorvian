import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class CashRegisterItem {
  final String id;
  final String code;
  final double openingBalance;
  final double closingBalance;
  final double totalIncome;
  final double totalExpense;
  final double expectedBalance;
  final double difference;
  final String status;
  final String? employeeName;
  final String openedAt;
  final String? closedAt;

  const CashRegisterItem({
    required this.id, required this.code, required this.openingBalance,
    required this.closingBalance, required this.totalIncome, required this.totalExpense,
    required this.expectedBalance, required this.difference, required this.status,
    this.employeeName, required this.openedAt, this.closedAt,
  });

  factory CashRegisterItem.fromJson(Map<String, dynamic> j) => CashRegisterItem(
    id: j['id'] as String? ?? '',
    code: j['code'] as String? ?? '',
    openingBalance: (j['openingBalance'] as num?)?.toDouble() ?? 0,
    closingBalance: (j['closingBalance'] as num?)?.toDouble() ?? 0,
    totalIncome: (j['totalIncome'] as num?)?.toDouble() ?? 0,
    totalExpense: (j['totalExpense'] as num?)?.toDouble() ?? 0,
    expectedBalance: (j['expectedBalance'] as num?)?.toDouble() ?? 0,
    difference: (j['difference'] as num?)?.toDouble() ?? 0,
    status: j['status'] as String? ?? '',
    employeeName: j['employeeName'] as String?,
    openedAt: j['openedAt'] as String? ?? '',
    closedAt: j['closedAt'] as String?,
  );

  bool get isOpen => status == 'open';
}

final class CashMovement {
  final String id;
  final String movementType;
  final double amount;
  final String? concept;
  final String? referenceNumber;
  final String? employeeName;
  final String createdAt;

  const CashMovement({
    required this.id, required this.movementType, required this.amount,
    this.concept, this.referenceNumber, this.employeeName, required this.createdAt,
  });

  factory CashMovement.fromJson(Map<String, dynamic> j) => CashMovement(
    id: j['id'] as String? ?? '',
    movementType: j['movementType'] as String? ?? '',
    amount: (j['amount'] as num?)?.toDouble() ?? 0,
    concept: j['concept'] as String?,
    referenceNumber: j['referenceNumber'] as String?,
    employeeName: j['employeeName'] as String?,
    createdAt: j['createdAt'] as String? ?? '',
  );
}

final class CashRegisterState {
  final List<CashRegisterItem> items;
  final bool loading;
  final String? error;
  const CashRegisterState({this.items = const [], this.loading = false, this.error});
  CashRegisterState copyWith({List<CashRegisterItem>? items, bool? loading, String? error}) =>
    CashRegisterState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class CashRegisterNotifier extends Notifier<CashRegisterState> {
  @override
  CashRegisterState build() => const CashRegisterState();
  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('cash-registers');
      final body = r.data;
      final list = body is Map ? (body['items'] ?? []) : (body is List ? body : []);
      state = CashRegisterState(items: (list as List).map((e) => CashRegisterItem.fromJson(e as Map<String, dynamic>)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar cajas', loading: false);
    }
  }
}

final cashRegisterProvider = NotifierProvider<CashRegisterNotifier, CashRegisterState>(CashRegisterNotifier.new);
