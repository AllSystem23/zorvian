import 'dart:async';
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class DashboardKpis {
  final int totalEmployees;
  final int activeEmployees;
  final int inactiveEmployees;
  final double? activeEmployeesTrend;
  final double? pendingRequestsTrend;
  final double? birthdaysTrend;
  final double? anniversariesTrend;
  final List<DepartmentCount> employeesByDepartment;
  final int pendingVacationRequests;
  final int pendingPermissionRequests;
  final int birthdaysThisMonth;
  final int workAnniversariesThisMonth;

  const DashboardKpis({
    required this.totalEmployees,
    required this.activeEmployees,
    required this.inactiveEmployees,
    this.activeEmployeesTrend,
    this.pendingRequestsTrend,
    this.birthdaysTrend,
    this.anniversariesTrend,
    required this.employeesByDepartment,
    required this.pendingVacationRequests,
    required this.pendingPermissionRequests,
    required this.birthdaysThisMonth,
    required this.workAnniversariesThisMonth,
  });

  factory DashboardKpis.empty() => const DashboardKpis(
    totalEmployees: 0,
    activeEmployees: 0,
    inactiveEmployees: 0,
    employeesByDepartment: [],
    pendingVacationRequests: 0,
    pendingPermissionRequests: 0,
    birthdaysThisMonth: 0,
    workAnniversariesThisMonth: 0,
  );

  factory DashboardKpis.fromJson(Map<String, dynamic> json) => DashboardKpis(
    totalEmployees: _readInt(json, 'totalEmployees'),
    activeEmployees: _readInt(json, 'activeEmployees'),
    inactiveEmployees: _readInt(json, 'inactiveEmployees'),
    activeEmployeesTrend: _readDouble(json, 'activeEmployeesTrend'),
    pendingRequestsTrend: _readDouble(json, 'pendingRequestsTrend'),
    birthdaysTrend: _readDouble(json, 'birthdaysTrend'),
    anniversariesTrend: _readDouble(json, 'anniversariesTrend'),
    employeesByDepartment: _readList(
      json,
      'employeesByDepartment',
    ).map((e) => DepartmentCount.fromJson(e)).toList(),
    pendingVacationRequests: _readInt(json, 'pendingVacationRequests'),
    pendingPermissionRequests: _readInt(json, 'pendingPermissionRequests'),
    birthdaysThisMonth: _readInt(json, 'birthdaysThisMonth'),
    workAnniversariesThisMonth: _readInt(json, 'workAnniversariesThisMonth'),
  );

  static int _readInt(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value is int) return value;
    if (value is num) return value.toInt();
    return 0;
  }

  static double? _readDouble(Map<String, dynamic> json, String key) {
    final value = json[key];
    if (value is num) return value.toDouble();
    return null;
  }

  static List<Map<String, dynamic>> _readList(
    Map<String, dynamic> json,
    String key,
  ) {
    final value = json[key];
    if (value is List) {
      return value.whereType<Map<String, dynamic>>().toList();
    }
    return const [];
  }
}

class DepartmentCount {
  final String departmentName;
  final int count;

  const DepartmentCount({required this.departmentName, required this.count});

  factory DepartmentCount.fromJson(Map<String, dynamic> json) =>
      DepartmentCount(
        departmentName: (json['departmentName'] as String?)?.isNotEmpty == true
            ? json['departmentName'] as String
            : 'Sin departamento',
        count: json['count'] is num ? (json['count'] as num).toInt() : 0,
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
    employeeId: (json['employeeId'] as String?)?.isNotEmpty == true
        ? json['employeeId'] as String
        : '',
    employeeName: (json['employeeName'] as String?)?.isNotEmpty == true
        ? json['employeeName'] as String
        : 'Empleado',
    employeeCode: (json['employeeCode'] as String?) ?? '',
    type: (json['type'] as String?)?.isNotEmpty == true
        ? json['type'] as String
        : 'vacation',
    startDate: (json['startDate'] as String?)?.isNotEmpty == true
        ? json['startDate'] as String
        : '',
    endDate: (json['endDate'] as String?)?.isNotEmpty == true
        ? json['endDate'] as String
        : '',
    status: (json['status'] as String?)?.isNotEmpty == true
        ? json['status'] as String
        : 'pending',
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

  factory RecentRequestItem.fromJson(Map<String, dynamic> json) =>
      RecentRequestItem(
        id: (json['id'] as String?)?.isNotEmpty == true
            ? json['id'] as String
            : '',
        requestType: (json['requestType'] as String?)?.isNotEmpty == true
            ? json['requestType'] as String
            : 'permission',
        employeeName: (json['employeeName'] as String?)?.isNotEmpty == true
            ? json['employeeName'] as String
            : 'Empleado',
        status: (json['status'] as String?)?.isNotEmpty == true
            ? json['status'] as String
            : 'pending',
        description: json['description'] as String?,
        createdAt: (json['createdAt'] as String?)?.isNotEmpty == true
            ? json['createdAt'] as String
            : '',
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
      final timeout = const Duration(seconds: 45);

      final response = await dio.get('dashboard/summary').timeout(timeout);
      final data = response.data;

      if (data is! Map<String, dynamic>) {
        throw Exception('Formato de datos inválido en Dashboard');
      }

      final kpisJson = _readMap(data, 'kpis');
      final calendarJson = _readList(data, 'calendarEvents');
      final requestsJson = _readList(data, 'recentRequests');

      state = DashboardState(
        kpis: kpisJson.isEmpty
            ? DashboardKpis.empty()
            : DashboardKpis.fromJson(kpisJson),
        calendarEvents: calendarJson
            .map((e) => CalendarEvent.fromJson(e))
            .toList(),
        recentRequests: requestsJson
            .map((e) => RecentRequestItem.fromJson(e))
            .toList(),
      );
    } on TimeoutException {
      state = state.copyWith(
        error: 'Tiempo de espera agotado al cargar dashboard',
        loading: false,
      );
    } on DioException catch (error) {
      state = state.copyWith(
        error: _friendlyDashboardError(error),
        loading: false,
      );
    } catch (e) {
      state = state.copyWith(
        error: 'Error al cargar dashboard: $e',
        loading: false,
      );
    }
  }

  Future<void> refresh() => loadAll();
}

Map<String, dynamic> _readMap(Map<String, dynamic> json, String key) {
  final value = json[key];
  return value is Map<String, dynamic> ? value : <String, dynamic>{};
}

List<Map<String, dynamic>> _readList(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value is List) {
    return value.whereType<Map<String, dynamic>>().toList();
  }
  return const [];
}

String _friendlyDashboardError(DioException error) {
  final statusCode = error.response?.statusCode;
  final detail = _extractDioMessage(error).toLowerCase();

  if (detail.contains('no data') ||
      detail.contains('no hay datos') ||
      detail.contains('empty')) {
    return 'No hay datos disponibles aún. El dashboard se mostrará vacío hasta que registres información.';
  }

  return switch (statusCode) {
    401 => 'Tu sesión expiró. Inicia sesión nuevamente.',
    403 => 'No tienes permiso para ver el dashboard.',
    404 => 'El endpoint del dashboard no existe.',
    500 =>
      'El servidor respondió con error. Si la base está vacía, el dashboard debería verse sin datos.',
    502 => 'El servicio API no está disponible temporalmente.',
    503 => 'El servicio API está temporalmente ocupado.',
    _ =>
      'No se pudo cargar el dashboard. Verifica la conexión o reintenta más tarde.',
  };
}

String _extractDioMessage(DioException error) {
  final data = error.response?.data;
  if (data is Map<String, dynamic>) {
    return (data['message']?.toString() ??
        data['title']?.toString() ??
        data['error']?.toString() ??
        error.message ??
        'Error de conexión');
  }
  return data?.toString() ?? error.message ?? 'Error de conexión';
}

final dashboardProvider = NotifierProvider<DashboardNotifier, DashboardState>(
  DashboardNotifier.new,
);
