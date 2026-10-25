import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';

class EmployeeListState {
  final List<EmployeeListItem> items;
  final int total;
  final int page;
  final int pageSize;
  final bool loading;
  final String? error;

  const EmployeeListState({
    this.items = const [],
    this.total = 0,
    this.page = 1,
    this.pageSize = 20,
    this.loading = false,
    this.error,
  });

  EmployeeListState copyWith({
    List<EmployeeListItem>? items,
    int? total,
    int? page,
    int? pageSize,
    bool? loading,
    String? error,
  }) => EmployeeListState(
    items: items ?? this.items,
    total: total ?? this.total,
    page: page ?? this.page,
    pageSize: pageSize ?? this.pageSize,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

class EmployeeListItem {
  final String id;
  final String employeeCode;
  final String fullName;
  final String email;
  final String departmentName;
  final String position;
  final String status;
  final String hireDate;

  const EmployeeListItem({
    required this.id,
    required this.employeeCode,
    required this.fullName,
    required this.email,
    required this.departmentName,
    required this.position,
    required this.status,
    required this.hireDate,
  });

  factory EmployeeListItem.fromJson(Map<String, dynamic> json) => EmployeeListItem(
    id: json['id'] as String,
    employeeCode: json['employeeCode'] as String? ?? '',
    fullName: json['fullName'] as String? ?? '',
    email: json['email'] as String? ?? '',
    departmentName: json['departmentName'] as String? ?? '',
    position: json['position'] as String? ?? '',
    status: json['status'] as String? ?? '',
    hireDate: json['hireDate'] as String? ?? '',
  );
}

class EmployeeNotifier extends AsyncNotifier<EmployeeListState> {
  @override
  Future<EmployeeListState> build() async {
    ref.watch(branchScopeProvider);
    return _fetch(page: 1);
  }

  /// Recarga la lista (búsqueda, filtro, paginación o pull-to-refresh)
  /// CONSERVANDO las filas que ya están en pantalla. Antes se hacía
  /// `state = const AsyncLoading()` a ciegas: cada tecla del buscador
  /// vaciaba la lista y la cambiaba por el esqueleto de carga.
  ///
  /// El cambio de SUCURSAL/COMPAÑÍA no pasa por aquí: va por `build()`
  /// (`ref.watch(branchScopeProvider)`), que arranca sin datos previos →
  /// pinta el esqueleto y refetchea con el nuevo `X-Branch-Id`. Los datos de
  /// la sucursal anterior no aplican en la nueva.
  Future<void> load({String? search, String? status, String? departmentId, int page = 1}) async {
    final scope = ref.read(branchScopeProvider);
    final previous = state.asData?.value;
    state = previous != null
        ? AsyncData(previous.copyWith(loading: true))
        : const AsyncLoading();
    final next = await AsyncValue.guard(
      () => _fetch(search: search, status: status, departmentId: departmentId, page: page),
    );
    // Si el alcance cambió en plena recarga, el resultado ya no corresponde
    // a la sucursal activa: se descarta y se deja que `build()` publique los
    // datos nuevos (si no, esta respuesta vieja pisaría ese refetch).
    if (scope != ref.read(branchScopeProvider)) return;
    state = next;
  }

  Future<EmployeeListState> _fetch({String? search, String? status, String? departmentId, int page = 1}) async {
    final dio = ref.read(dioClientProvider);
    final params = <String, dynamic>{
      'page': page,
      'pageSize': 20,
      if (search != null && search.isNotEmpty) 'search': search,
      if (status != null && status.isNotEmpty) 'status': status,
      if (departmentId != null) 'departmentId': departmentId,
    };
    final response = await dio.get('employees', params: params);
    final data = response.data;
    return EmployeeListState(
      items: (data['items'] as List).map((e) => EmployeeListItem.fromJson(e)).toList(),
      total: data['total'] as int,
      page: data['page'] as int,
      pageSize: data['pageSize'] as int,
    );
  }
}

final employeeProvider = AsyncNotifierProvider<EmployeeNotifier, EmployeeListState>(
  EmployeeNotifier.new,
);
