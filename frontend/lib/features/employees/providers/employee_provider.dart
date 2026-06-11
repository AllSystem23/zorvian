import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

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
    return _fetch(page: 1);
  }

  Future<void> load({String? search, String? status, String? departmentId, int page = 1}) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => _fetch(search: search, status: status, departmentId: departmentId, page: page));
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
