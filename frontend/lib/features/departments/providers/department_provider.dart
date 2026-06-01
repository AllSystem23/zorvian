import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class DepartmentState {
  final List<DepartmentItem> items;
  final bool loading;
  final String? error;

  const DepartmentState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  DepartmentState copyWith({
    List<DepartmentItem>? items,
    bool? loading,
    String? error,
  }) => DepartmentState(
    items: items ?? this.items,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

class DepartmentItem {
  final String id;
  final String name;
  final String code;
  final String description;
  final String managerName;
  final bool isActive;
  final int employeeCount;

  const DepartmentItem({
    required this.id,
    required this.name,
    required this.code,
    required this.description,
    required this.managerName,
    required this.isActive,
    required this.employeeCount,
  });

  factory DepartmentItem.fromJson(Map<String, dynamic> json) => DepartmentItem(
    id: json['id'] as String,
    name: json['name'] as String? ?? '',
    code: json['code'] as String? ?? '',
    description: json['description'] as String? ?? '',
    managerName: json['managerName'] as String? ?? '',
    isActive: json['isActive'] as bool? ?? true,
    employeeCount: json['employeeCount'] as int? ?? 0,
  );
}

class DepartmentNotifier extends Notifier<DepartmentState> {
  @override
  DepartmentState build() => const DepartmentState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('departments');
      final data = response.data as List;
      state = DepartmentState(
        items: data.map((e) => DepartmentItem.fromJson(e)).toList(),
      );
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar departamentos', loading: false);
    }
  }
}

final departmentProvider = NotifierProvider<DepartmentNotifier, DepartmentState>(
  DepartmentNotifier.new,
);
