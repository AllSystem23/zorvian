import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';

class VacationApprovalStep {
  final int step;
  final String? approverName;
  final String status;
  final String? comments;
  final String? approvedAt;

  const VacationApprovalStep({
    required this.step,
    required this.status,
    this.approverName,
    this.comments,
    this.approvedAt,
  });

  factory VacationApprovalStep.fromJson(Map<String, dynamic> json) => VacationApprovalStep(
    step: (json['step'] as num?)?.toInt() ?? 0,
    approverName: json['approverName'] as String?,
    status: json['status'] as String? ?? 'pending',
    comments: json['comments'] as String?,
    approvedAt: json['approvedAt']?.toString(),
  );
}

class VacationItem {
  final String id;
  final String employeeId;
  final String employeeName;
  final String employeeCode;
  final String startDate;
  final String endDate;
  final double totalDays;
  final double businessDays;
  final String status;
  final String? comments;
  final String? rejectionReason;
  final bool isAdvanced;
  /// Corrida de nómina que pagó estas vacaciones (null = no pagadas por planilla).
  final String? paidByPayrollRunId;
  final List<VacationApprovalStep> approvalSteps;
  final String? createdAt;

  const VacationItem({
    required this.id,
    required this.employeeId,
    required this.employeeName,
    required this.employeeCode,
    required this.startDate,
    required this.endDate,
    required this.totalDays,
    required this.businessDays,
    required this.status,
    this.comments,
    this.rejectionReason,
    this.isAdvanced = false,
    this.paidByPayrollRunId,
    this.approvalSteps = const [],
    this.createdAt,
  });

  factory VacationItem.fromJson(Map<String, dynamic> json) => VacationItem(
    id: json['id'] as String,
    employeeId: json['employeeId'] as String? ?? '',
    employeeName: json['employeeName'] as String? ?? '',
    employeeCode: json['employeeCode'] as String? ?? '',
    startDate: json['startDate'] as String? ?? '',
    endDate: json['endDate'] as String? ?? '',
    totalDays: (json['totalDays'] as num?)?.toDouble() ?? 0,
    businessDays: (json['businessDays'] as num?)?.toDouble() ?? 0,
    status: json['status'] as String? ?? '',
    comments: json['comments'] as String?,
    rejectionReason: json['rejectionReason'] as String?,
    isAdvanced: json['isAdvanced'] as bool? ?? false,
    paidByPayrollRunId: json['paidByPayrollRunId'] as String?,
    approvalSteps: json['approvalSteps'] is List
        ? (json['approvalSteps'] as List)
            .whereType<Map<String, dynamic>>()
            .map(VacationApprovalStep.fromJson)
            .toList()
        : const [],
    createdAt: json['createdAt']?.toString(),
  );
}

class VacationBalance {
  final double totalDaysPerYear;
  final double accruedDays;
  final double takenDays;
  final double pendingDays;
  final double availableDays;

  const VacationBalance({
    required this.totalDaysPerYear,
    required this.accruedDays,
    required this.takenDays,
    required this.pendingDays,
    required this.availableDays,
  });

  factory VacationBalance.fromJson(Map<String, dynamic> json) => VacationBalance(
    totalDaysPerYear: (json['totalDaysPerYear'] as num?)?.toDouble() ?? 0,
    accruedDays: (json['accruedDays'] as num?)?.toDouble() ?? 0,
    takenDays: (json['takenDays'] as num?)?.toDouble() ?? 0,
    pendingDays: (json['pendingDays'] as num?)?.toDouble() ?? 0,
    availableDays: (json['availableDays'] as num?)?.toDouble() ?? 0,
  );
}

class VacationListState {
  final List<VacationItem> items;
  final int total;
  final bool loading;
  final String? error;
  final VacationBalance? balance;

  const VacationListState({
    this.items = const [],
    this.total = 0,
    this.loading = false,
    this.error,
    this.balance,
  });

  VacationListState copyWith({
    List<VacationItem>? items,
    int? total,
    bool? loading,
    String? error,
    VacationBalance? balance,
  }) => VacationListState(
    items: items ?? this.items,
    total: total ?? this.total,
    loading: loading ?? this.loading,
    error: error ?? this.error,
    balance: balance ?? this.balance,
  );
}

class VacationNotifier extends Notifier<VacationListState> {
    static String? _lastScope;

@override
  VacationListState build() {
    final scope = ref.watch(branchScopeProvider);
    if (_lastScope != null && _lastScope != scope) {
      Future.microtask(() => load());
    }
    _lastScope = scope;
    return const VacationListState();
  }

  Future<void> load({String? status, String? employeeId}) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final params = <String, dynamic>{
        'page': 1,
        'pageSize': 20,
        if (status case final s?) 'status': s,
        if (employeeId case final e?) 'employeeId': e,
      };
      final r = await dio.get('vacations', params: params);
      final data = r.data;
      state = VacationListState(
        items: (data['items'] as List).map((e) => VacationItem.fromJson(e)).toList(),
        total: data['total'] as int,
      );
    } catch (e) {
      state = state.copyWith(error: 'Error al cargar vacaciones', loading: false);
    }
  }

  Future<void> loadBalance() async {
    state = state.copyWith(error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('vacations/balance');
      state = state.copyWith(balance: VacationBalance.fromJson(r.data));
    } catch (e) {
      state = state.copyWith(error: 'Error al cargar saldo');
    }
  }

  /// Loads a single request with its full detail (approval steps, business days).
  Future<VacationItem?> loadDetail(String vacationId) async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('vacations/$vacationId');
      return VacationItem.fromJson(r.data);
    } catch (_) {
      return null;
    }
  }

  /// Marca una vacación aprobada como tomada SIN recibo de pago (vacación descansada):
  /// registra el disfrute y descuenta el saldo en el servidor.
  Future<bool> markVacationTaken(String vacationId) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('vacations/$vacationId/mark-taken');
      return true;
    } catch (_) {
      return false;
    }
  }
}

final vacationProvider = NotifierProvider<VacationNotifier, VacationListState>(
  VacationNotifier.new,
);
