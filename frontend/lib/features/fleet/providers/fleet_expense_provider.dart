import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

final class FleetExpenseItem {
  final String id;
  final String expenseDate;
  final String categoryName;
  final String? subcategoryName;
  final String? vehiclePlate;
  final String? vehicleBrandModel;
  final String? driverName;
  final String description;
  final double amount;
  final String currency;
  final double amountBaseCurrency;
  final String paymentMethod;
  final bool reimbursable;
  final bool reimbursed;
  final bool approved;
  final String createdAt;

  const FleetExpenseItem({
    required this.id,
    required this.expenseDate,
    required this.categoryName,
    this.subcategoryName,
    this.vehiclePlate,
    this.vehicleBrandModel,
    this.driverName,
    required this.description,
    required this.amount,
    required this.currency,
    required this.amountBaseCurrency,
    required this.paymentMethod,
    required this.reimbursable,
    required this.reimbursed,
    required this.approved,
    required this.createdAt,
  });

  factory FleetExpenseItem.fromJson(Map<String, dynamic> j) => FleetExpenseItem(
    id: j['id'] as String,
    expenseDate: j['expenseDate'] as String? ?? '',
    categoryName: j['categoryName'] as String? ?? '',
    subcategoryName: j['subcategoryName'] as String?,
    vehiclePlate: j['vehiclePlate'] as String?,
    vehicleBrandModel: j['vehicleBrandModel'] as String?,
    driverName: j['driverName'] as String?,
    description: j['description'] as String? ?? '',
    amount: (j['amount'] as num?)?.toDouble() ?? 0,
    currency: j['currency'] as String? ?? 'NIO',
    amountBaseCurrency: (j['amountBaseCurrency'] as num?)?.toDouble() ?? 0,
    paymentMethod: j['paymentMethod'] as String? ?? 'Cash',
    reimbursable: j['reimbursable'] as bool? ?? false,
    reimbursed: j['reimbursed'] as bool? ?? false,
    approved: j['approved'] as bool? ?? false,
    createdAt: j['createdAt'] as String? ?? '',
  );
}

final class FleetExpenseState {
  final List<FleetExpenseItem> items;
  final bool loading;
  final String? error;

  const FleetExpenseState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  FleetExpenseState copyWith({
    List<FleetExpenseItem>? items,
    bool? loading,
    String? error,
  }) => FleetExpenseState(
    items: items ?? this.items,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

final class FleetExpenseNotifier extends Notifier<FleetExpenseState> {
  @override
  FleetExpenseState build() => const FleetExpenseState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/expenses');
      final items = ((r.data['items'] as List?) ?? [])
          .map((e) => FleetExpenseItem.fromJson(e as Map<String, dynamic>))
          .toList();
      state = FleetExpenseState(items: items);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar gastos', loading: false);
    }
  }

  Future<bool> approve(String id, {String? accountId}) async {
    try {
      final dio = ref.read(dioClientProvider);
      final query = (accountId != null && accountId.isNotEmpty) ? '?accountId=$accountId' : '';
      await dio.post('fleet/expenses/$id/approve$query');
      await load();
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<Map<String, dynamic>?> classify(String description, double amount) async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.post('fleet/expenses/classify',
        data: {'description': description, 'amount': amount});
      return r.data as Map<String, dynamic>;
    } catch (_) {
      return null;
    }
  }

  Future<Map<String, dynamic>?> approveBatch(List<String> ids) async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.post('fleet/expenses/approve-batch',
        data: {'ids': ids});
      await load();
      return r.data as Map<String, dynamic>?;
    } catch (_) {
      return null;
    }
  }

  Future<bool> delete(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('fleet/expenses/$id');
      await load();
      return true;
    } catch (_) {
      return false;
    }
  }
}

final fleetExpenseProvider = NotifierProvider<FleetExpenseNotifier, FleetExpenseState>(FleetExpenseNotifier.new);
