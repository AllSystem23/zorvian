import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class VacationItem {
  final String id;
  final String employeeName;
  final String employeeCode;
  final String startDate;
  final String endDate;
  final double totalDays;
  final String status;
  final String? comments;

  const VacationItem({
    required this.id,
    required this.employeeName,
    required this.employeeCode,
    required this.startDate,
    required this.endDate,
    required this.totalDays,
    required this.status,
    this.comments,
  });

  factory VacationItem.fromJson(Map<String, dynamic> json) => VacationItem(
    id: json['id'] as String,
    employeeName: json['employeeName'] as String? ?? '',
    employeeCode: json['employeeCode'] as String? ?? '',
    startDate: json['startDate'] as String? ?? '',
    endDate: json['endDate'] as String? ?? '',
    totalDays: (json['totalDays'] as num?)?.toDouble() ?? 0,
    status: json['status'] as String? ?? '',
    comments: json['comments'] as String?,
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
  @override
  VacationListState build() => const VacationListState();

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
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('vacations/balance');
      state = state.copyWith(balance: VacationBalance.fromJson(r.data));
    } catch (e) {
      state = state.copyWith(error: 'Error al cargar saldo');
    }
  }
}

final vacationProvider = NotifierProvider<VacationNotifier, VacationListState>(
  VacationNotifier.new,
);
