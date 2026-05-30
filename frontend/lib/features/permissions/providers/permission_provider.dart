import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class PermissionType {
  final String id;
  final String code;
  final String name;
  final bool isPaid;
  final bool requiresAttachment;
  final bool requiresApproval;
  final int? maxDaysPerRequest;
  final int? maxDaysPerMonth;
  final int? maxDaysPerYear;
  final String? description;

  const PermissionType({
    required this.id,
    required this.code,
    required this.name,
    required this.isPaid,
    required this.requiresAttachment,
    required this.requiresApproval,
    this.maxDaysPerRequest,
    this.maxDaysPerMonth,
    this.maxDaysPerYear,
    this.description,
  });

  factory PermissionType.fromJson(Map<String, dynamic> json) => PermissionType(
    id: json['id'] as String,
    code: json['code'] as String? ?? '',
    name: json['name'] as String? ?? '',
    isPaid: json['isPaid'] as bool? ?? false,
    requiresAttachment: json['requiresAttachment'] as bool? ?? false,
    requiresApproval: json['requiresApproval'] as bool? ?? false,
    maxDaysPerRequest: json['maxDaysPerRequest'] as int?,
    maxDaysPerMonth: json['maxDaysPerMonth'] as int?,
    maxDaysPerYear: json['maxDaysPerYear'] as int?,
    description: json['description'] as String?,
  );
}

class PermissionItem {
  final String id;
  final String employeeName;
  final String employeeCode;
  final String leaveTypeCode;
  final String leaveTypeName;
  final String startDate;
  final String endDate;
  final double totalDays;
  final double businessDays;
  final String? reason;
  final String status;
  final String? supportingDocumentUrl;
  final String? supportingDocumentFileName;
  final bool isPaid;

  const PermissionItem({
    required this.id,
    required this.employeeName,
    required this.employeeCode,
    required this.leaveTypeCode,
    required this.leaveTypeName,
    required this.startDate,
    required this.endDate,
    required this.totalDays,
    required this.businessDays,
    this.reason,
    required this.status,
    this.supportingDocumentUrl,
    this.supportingDocumentFileName,
    required this.isPaid,
  });

  factory PermissionItem.fromJson(Map<String, dynamic> json) => PermissionItem(
    id: json['id'] as String,
    employeeName: json['employeeName'] as String? ?? '',
    employeeCode: json['employeeCode'] as String? ?? '',
    leaveTypeCode: json['leaveTypeCode'] as String? ?? '',
    leaveTypeName: json['leaveTypeName'] as String? ?? '',
    startDate: json['startDate'] as String? ?? '',
    endDate: json['endDate'] as String? ?? '',
    totalDays: (json['totalDays'] as num?)?.toDouble() ?? 0,
    businessDays: (json['businessDays'] as num?)?.toDouble() ?? 0,
    reason: json['reason'] as String?,
    status: json['status'] as String? ?? '',
    supportingDocumentUrl: json['supportingDocumentUrl'] as String?,
    supportingDocumentFileName: json['supportingDocumentFileName'] as String?,
    isPaid: json['isPaid'] as bool? ?? false,
  );
}

class PermissionListState {
  final List<PermissionItem> items;
  final int total;
  final bool loading;
  final String? error;
  final List<PermissionType> types;
  final bool typesLoading;

  const PermissionListState({
    this.items = const [],
    this.total = 0,
    this.loading = false,
    this.error,
    this.types = const [],
    this.typesLoading = false,
  });

  PermissionListState copyWith({
    List<PermissionItem>? items,
    int? total,
    bool? loading,
    String? error,
    List<PermissionType>? types,
    bool? typesLoading,
  }) => PermissionListState(
    items: items ?? this.items,
    total: total ?? this.total,
    loading: loading ?? this.loading,
    error: error ?? this.error,
    types: types ?? this.types,
    typesLoading: typesLoading ?? this.typesLoading,
  );
}

class PermissionNotifier extends Notifier<PermissionListState> {
  @override
  PermissionListState build() => const PermissionListState();

  Future<void> load({String? status, String? employeeId}) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final params = <String, dynamic>{
        'page': 1,
        'pageSize': 20,
        if (status != null) 'status': status,
        if (employeeId != null) 'employeeId': employeeId,
      };
      final r = await dio.get('/permissions', params: params);
      final data = r.data;
      state = PermissionListState(
        items: (data['items'] as List).map((e) => PermissionItem.fromJson(e)).toList(),
        total: data['total'] as int,
        types: state.types,
      );
    } catch (e) {
      state = state.copyWith(error: 'Error al cargar permisos', loading: false);
    }
  }

  Future<void> loadTypes() async {
    state = state.copyWith(typesLoading: true);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('/permissions/types');
      final data = r.data as List;
      state = state.copyWith(
        types: data.map((e) => PermissionType.fromJson(e)).toList(),
        typesLoading: false,
      );
    } catch (e) {
      state = state.copyWith(typesLoading: false, error: 'Error al cargar tipos de permiso');
    }
  }
}

final permissionProvider = NotifierProvider<PermissionNotifier, PermissionListState>(
  PermissionNotifier.new,
);
