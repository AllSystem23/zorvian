import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';

final companySettingsProvider = FutureProvider.autoDispose<Map<String, dynamic>>((ref) async {
  final dio = ref.read(dioClientProvider);
  try {
    final response = await dio.get('companies/settings').timeout(const Duration(seconds: 5));
    final raw = response.data;
    if (raw is Map<String, dynamic>) return raw;
    throw Exception('Formato de datos inválido');
  } catch (e) {
    throw Exception('Error cargando configuración: $e');
  }
});

final companyInfoProvider = FutureProvider.autoDispose<Map<String, dynamic>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final auth = ref.watch(authProvider);
  final companyBranch = ref.watch(companyBranchProvider);
  try {
    // For SuperAdmin, try companies/current first; if empty, use selected company from list
    final response = await dio.get('companies/current').timeout(const Duration(seconds: 5));
    if (response.data is Map) {
      final raw = response.data;
      if (raw is Map<String, dynamic>) {
        // If response is a default/empty company (SuperAdmin without tenant)
        if (auth.role == 'SuperAdmin' && _isEmptyCompany(raw)) {
          return await _loadCompanyInfo(dio, companyBranch.companyId);
        }
        return raw;
      }
      throw Exception('Formato de datos inválido');
    }
    throw Exception('Formato de datos inválido');
  } catch (e) {
    if (auth.role == 'SuperAdmin' && companyBranch.companyId != null) {
      return await _loadCompanyInfo(dio, companyBranch.companyId);
    }
    throw Exception('Error cargando empresa: $e');
  }
});

bool _isEmptyCompany(Map<String, dynamic> c) {
  return (c['name'] == null || c['name'] == '') &&
         (c['id'] == null || c['id'] == '00000000-0000-0000-0000-000000000000');
}


Future<Map<String, dynamic>> _loadCompanyInfo(dynamic dio, String? companyId) async {
  if (companyId == null) return {};
  try {
    final response = await dio.get('companies/all').timeout(const Duration(seconds: 5));
    final list = response.data;
    if (list is List) {
      for (final c in list) {
        if (c is Map && c['id']?.toString() == companyId) {
          return Map<String, dynamic>.from(c);
        }
      }
    }
  } catch (_) {}
  return {};
}
