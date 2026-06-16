import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'auth_provider.dart';

class TenantInfo {
  final String tenantId;
  final String companyName;
  final bool isCurrent;

  const TenantInfo({
    required this.tenantId,
    required this.companyName,
    this.isCurrent = false,
  });

  factory TenantInfo.fromJson(Map<String, dynamic> json) => TenantInfo(
    tenantId: json['tenantId'] as String,
    companyName: json['companyName'] as String,
    isCurrent: json['isCurrent'] as bool? ?? false,
  );
}

final tenantsListProvider = FutureProvider.autoDispose<List<TenantInfo>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final response = await dio.get('auth/tenants', params: {'pageSize': 100});
  final data = response.data;
  // Soporta formato plano (List) y paginado (PagedResult con .items)
  final Iterable list = data is List ? data : (data['items'] as List<dynamic>);
  return list.map((e) => TenantInfo.fromJson(e as Map<String, dynamic>)).toList();
});
