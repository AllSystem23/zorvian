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

  // SuperAdmin: go directly to companies/all filtered by selected companyId
  if (auth.role == 'SuperAdmin' && companyBranch.companyId != null) {
    try {
      final response = await dio.get('companies/all').timeout(const Duration(seconds: 5));
      final list = response.data;
      if (list is List) {
        for (final c in list) {
          if (c is Map && c['id']?.toString() == companyBranch.companyId) {
            return Map<String, dynamic>.from(c);
          }
        }
      }
    } catch (_) {}
    return {};
  }

  // Regular users: use companies/current with tenant context
  try {
    final response = await dio.get('companies/current').timeout(const Duration(seconds: 5));
    final raw = response.data;
    if (raw is Map<String, dynamic>) return raw;
    throw Exception('Formato de datos inválido');
  } catch (e) {
    throw Exception('Error cargando empresa: $e');
  }
});
