import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class DashboardKpis {
  final int totalEmployees;
  final int activeEmployees;
  final int inactiveEmployees;
  final List<DepartmentCount> employeesByDepartment;
  final int pendingVacationRequests;
  final int pendingPermissionRequests;
  final int birthdaysThisMonth;
  final int workAnniversariesThisMonth;

  const DashboardKpis({
    required this.totalEmployees,
    required this.activeEmployees,
    required this.inactiveEmployees,
    required this.employeesByDepartment,
    required this.pendingVacationRequests,
    required this.pendingPermissionRequests,
    required this.birthdaysThisMonth,
    required this.workAnniversariesThisMonth,
  });

  factory DashboardKpis.fromJson(Map<String, dynamic> json) => DashboardKpis(
    totalEmployees: json['totalEmployees'] as int,
    activeEmployees: json['activeEmployees'] as int,
    inactiveEmployees: json['inactiveEmployees'] as int,
    employeesByDepartment: (json['employeesByDepartment'] as List)
        .map((e) => DepartmentCount.fromJson(e))
        .toList(),
    pendingVacationRequests: json['pendingVacationRequests'] as int,
    pendingPermissionRequests: json['pendingPermissionRequests'] as int,
    birthdaysThisMonth: json['birthdaysThisMonth'] as int,
    workAnniversariesThisMonth: json['workAnniversariesThisMonth'] as int,
  );
}

class DepartmentCount {
  final String departmentName;
  final int count;

  const DepartmentCount({required this.departmentName, required this.count});

  factory DepartmentCount.fromJson(Map<String, dynamic> json) => DepartmentCount(
    departmentName: json['departmentName'] as String,
    count: json['count'] as int,
  );
}

class CalendarEvent {
  final String employeeId;
  final String employeeName;
  final String employeeCode;
  final String type;
  final String startDate;
  final String endDate;
  final String status;

  const CalendarEvent({
    required this.employeeId,
    required this.employeeName,
    required this.employeeCode,
    required this.type,
    required this.startDate,
    required this.endDate,
    required this.status,
  });

  factory CalendarEvent.fromJson(Map<String, dynamic> json) => CalendarEvent(
    employeeId: json['employeeId'] as String,
    employeeName: json['employeeName'] as String,
    employeeCode: json['employeeCode'] as String,
    type: json['type'] as String,
    startDate: json['startDate'] as String,
    endDate: json['endDate'] as String,
    status: json['status'] as String,
  );
}

class RecentRequestItem {
  final String id;
  final String requestType;
  final String employeeName;
  final String status;
  final String? description;
  final String createdAt;

  const RecentRequestItem({
    required this.id,
    required this.requestType,
    required this.employeeName,
    required this.status,
    this.description,
    required this.createdAt,
  });

  factory RecentRequestItem.fromJson(Map<String, dynamic> json) => RecentRequestItem(
    id: json['id'] as String,
    requestType: json['requestType'] as String,
    employeeName: json['employeeName'] as String,
    status: json['status'] as String,
    description: json['description'] as String?,
    createdAt: json['createdAt'] as String,
  );
}

class DashboardState {
  final DashboardKpis? kpis;
  final List<CalendarEvent> calendarEvents;
  final List<RecentRequestItem> recentRequests;
  final bool loading;
  final String? error;

  const DashboardState({
    this.kpis,
    this.calendarEvents = const [],
    this.recentRequests = const [],
    this.loading = false,
    this.error,
  });

  DashboardState copyWith({
    DashboardKpis? kpis,
    List<CalendarEvent>? calendarEvents,
    List<RecentRequestItem>? recentRequests,
    bool? loading,
    String? error,
  }) => DashboardState(
    kpis: kpis ?? this.kpis,
    calendarEvents: calendarEvents ?? this.calendarEvents,
    recentRequests: recentRequests ?? this.recentRequests,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

class DashboardNotifier extends Notifier<DashboardState> {
  @override
  DashboardState build() => const DashboardState();

  Future<void> loadAll() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final timeout = const Duration(seconds: 25);
      
      final response = await dio.get('dashboard/summary').timeout(timeout);
      final data = response.data;

      if (data is! Map<String, dynamic>) {
         throw Exception('Formato de datos inválido en Dashboard');
      }

      state = DashboardState(
        kpis: DashboardKpis.fromJson(data['kpis'] as Map<String, dynamic>),
        calendarEvents: (data['calendarEvents'] as List)
            .map((e) => CalendarEvent.fromJson(e as Map<String, dynamic>))
            .toList(),
        recentRequests: (data['recentRequests'] as List)
            .map((e) => RecentRequestItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
    } on TimeoutException {
      state = state.copyWith(error: 'Tiempo de espera agotado al cargar dashboard', loading: false);
    } catch (e) {
      state = state.copyWith(error: 'Error al cargar dashboard: $e', loading: false);
    }
  }

  Future<void> refresh() => loadAll();
}

final dashboardProvider = NotifierProvider<DashboardNotifier, DashboardState>(
  DashboardNotifier.new,
);
