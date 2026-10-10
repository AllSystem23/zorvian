import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';
import '../ds.dart';

/// A drop-in company/tenant switcher that handles all auth boilerplate.
///
/// Fetches tenants via [getMyTenants], reads the current [tenantId],
/// and calls [switchTenant] on change with a success SnackBar.
///
/// Usage:
/// ```dart
/// const ZCompanySwitcher()
/// ```
class ZCompanySwitcher extends ConsumerWidget {
  final EdgeInsetsGeometry? padding;
  final IconData icon;

  const ZCompanySwitcher({
    super.key,
    this.padding,
    this.icon = Icons.business,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return FutureBuilder<List<Map<String, dynamic>>>(
      future: ref.read(authProvider.notifier).getMyTenants(),
      builder: (context, snapshot) {
        if (!snapshot.hasData) {
          return const SizedBox.shrink();
        }

        // Null-safe: filtra tenants sin tenantId válido antes de renderizar.
        final tenants = snapshot.data!
            .where((t) => t['tenantId'] is String && (t['tenantId'] as String).isNotEmpty)
            .toList();

        if (tenants.length <= 1) {
          return const SizedBox.shrink();
        }
        final currentTenant = ref.watch(authProvider).tenantId;

        return ZCompanyDropdown(
          tenants: tenants,
          currentTenantId: currentTenant,
          icon: icon,
          padding: padding,
          onChanged: (newId) async {
            if (newId != null && newId != currentTenant) {
              final tenantName = (tenants.firstWhere(
                (t) => t['tenantId'] == newId,
                orElse: () => <String, dynamic>{'name': 'empresa'},
              )['name'] ?? 'empresa').toString();
              final messenger = ScaffoldMessenger.of(context);
              final success = await ref
                  .read(authProvider.notifier)
                  .switchTenant(newId);
              if (success) {
                // Sincronizar el alcance visual (compañía + sucursal) con el
                // tenant recién seleccionado: la sucursal anterior pertenecía
                // a la compañía anterior y debe descartarse.
                await _syncScopeToTenant(ref, newId, tenantName);
              }
              if (context.mounted && success) {
                messenger.showSnackBar(
                  SnackBar(content: Text('Cambiando a: $tenantName')),
                );
              }
            }
          },
        );
      },
    );
  }

  /// Aligns the company/branch scope with the tenant just selected, so the
  /// header shows the switched company instead of stale state (and does not
  /// auto-switch back to the first company of the list).
  Future<void> _syncScopeToTenant(
    WidgetRef ref,
    String tenantId,
    String fallbackName,
  ) async {
    String? companyId;
    String companyName = fallbackName;
    try {
      final companies = await ref.read(companyListProvider.future);
      for (final c in companies) {
        if (c['tenantId'] == tenantId) {
          companyId = c['id'] as String?;
          companyName = (c['name'] ?? c['legalName'] ?? fallbackName).toString();
          break;
        }
      }
    } catch (_) {
      // Sin lista de empresas disponible: se limpia el alcance y el header rehace la selección.
    }

    final notifier = ref.read(companyBranchProvider.notifier);
    if (companyId != null && companyId.isNotEmpty) {
      notifier.selectCompany(companyId, companyName);
    } else {
      notifier.clearSelection();
    }
    // Las sucursales y las empresas dependen del tenant activo.
    ref.invalidate(headerBranchListProvider);
    ref.invalidate(companyListProvider);
  }
}
