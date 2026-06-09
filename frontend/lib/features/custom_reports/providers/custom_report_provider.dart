import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../models/report_designer_models.dart';

class CustomReportState {
  final List<CustomReportItem> items;
  final bool loading;
  final String? error;

  const CustomReportState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  CustomReportState copyWith({
    List<CustomReportItem>? items,
    bool? loading,
    String? error,
  }) =>
      CustomReportState(
        items: items ?? this.items,
        loading: loading ?? this.loading,
        error: error ?? this.error,
      );
}

class CustomReportItem {
  final String id;
  final String name;
  final String? description;
  final String module;
  final List<ReportField> fields;
  final List<ReportFilter> filters;
  final String? groupByField;
  final String? sortByField;
  final String sortOrder;
  final bool isPublic;
  final String? createdByUserId;
  final String? companyId;

  const CustomReportItem({
    required this.id,
    required this.name,
    this.description,
    required this.module,
    required this.fields,
    this.filters = const [],
    this.groupByField,
    this.sortByField,
    this.sortOrder = 'asc',
    this.isPublic = false,
    this.createdByUserId,
    this.companyId,
  });

  factory CustomReportItem.fromJson(Map<String, dynamic> json) {
    return CustomReportItem(
      id: json['id'] as String,
      name: json['name'] as String? ?? '',
      description: json['description'] as String?,
      module: json['module'] as String? ?? '',
      fields: (json['fields'] as List?)?.map((e) => ReportField.fromJson(e)).toList() ?? [],
      filters: (json['filters'] as List?)?.map((e) => ReportFilter.fromJson(e)).toList() ?? [],
      groupByField: json['groupByField'] as String?,
      sortByField: json['sortByField'] as String?,
      sortOrder: json['sortOrder'] as String? ?? 'asc',
      isPublic: json['isPublic'] as bool? ?? false,
      createdByUserId: json['createdByUserId'] as String?,
      companyId: json['companyId'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'name': name,
    'description': description,
    'module': module,
    'fields': fields.map((f) => f.toJson()).toList(),
    'filters': filters.map((f) => f.toJson()).toList(),
    'groupByField': groupByField,
    'sortByField': sortByField,
    'sortOrder': sortOrder,
    'isPublic': isPublic,
  };
}

class ReportResultState {
  final List<String> columns;
  final List<Map<String, dynamic>> rows;
  final bool loading;
  final String? error;

  const ReportResultState({
    this.columns = const [],
    this.rows = const [],
    this.loading = false,
    this.error,
  });

  ReportResultState copyWith({
    List<String>? columns,
    List<Map<String, dynamic>>? rows,
    bool? loading,
    String? error,
  }) =>
      ReportResultState(
        columns: columns ?? this.columns,
        rows: rows ?? this.rows,
        loading: loading ?? this.loading,
        error: error ?? this.error,
      );
}

class CustomReportNotifier extends Notifier<CustomReportState> {
  @override
  CustomReportState build() => const CustomReportState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('custom-reports');
      final data = response.data as List;
      state = CustomReportState(
        items: data.map((e) => CustomReportItem.fromJson(e)).toList(),
      );
    } catch (_) {
      state = state.copyWith(
        error: 'Error al cargar reportes personalizados',
        loading: false,
      );
    }
  }

  Future<String?> delete(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('custom-reports/$id');
      await load();
      return null;
    } catch (e) {
      return 'Error al eliminar reporte';
    }
  }
}

final customReportProvider =
    NotifierProvider<CustomReportNotifier, CustomReportState>(
  CustomReportNotifier.new,
);
