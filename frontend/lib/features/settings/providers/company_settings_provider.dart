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

/// Datos mínimos para editar el logo de la compañía en contexto.
class CompanyLogoInfo {
  /// Id de la compañía (el endpoint de subida/borrado es por id).
  final String id;

  /// URL firmada (caduca ~15 min) o `null` si la compañía no tiene logo.
  final String? logoUrl;

  const CompanyLogoInfo({required this.id, this.logoUrl});
}

/// `GET companies/logo`.
///
/// Endpoint propio (no `companies/current`) porque este último exige
/// `company.manage` — permiso de plataforma — y la pantalla de configuración la
/// usa una CompanyAdmin para el logo de SU empresa. La URL viene firmada: se
/// vuelve a pedir al abrir la pantalla, nunca se cachea.
final companyLogoProvider = FutureProvider.autoDispose<CompanyLogoInfo>((ref) async {
  final dio = ref.read(dioClientProvider);
  // Cambiar de compañía en el cabecero recarga el logo.
  ref.watch(branchScopeProvider);

  final response = await dio.get('companies/logo').timeout(const Duration(seconds: 10));
  final raw = response.data;
  if (raw is! Map) throw Exception('Formato de datos inválido');

  final logo = (raw['logoUrl'] as String?)?.trim();
  return CompanyLogoInfo(
    id: raw['id']?.toString() ?? '',
    logoUrl: (logo == null || logo.isEmpty) ? null : logo,
  );
});

final companyInfoProvider = FutureProvider.autoDispose<Map<String, dynamic>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final auth = ref.watch(authProvider);
  final companyBranch = ref.watch(companyBranchProvider);
  ref.watch(branchScopeProvider);

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
