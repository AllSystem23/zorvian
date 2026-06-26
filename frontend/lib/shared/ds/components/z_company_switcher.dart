import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
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
        if (!snapshot.hasData || snapshot.data!.length <= 1) {
          return const SizedBox.shrink();
        }

        final tenants = snapshot.data!;
        final currentTenant = ref.watch(authProvider).tenantId;

        return ZCompanyDropdown(
          tenants: tenants,
          currentTenantId: currentTenant,
          icon: icon,
          padding: padding,
          onChanged: (newId) async {
            if (newId != null && newId != currentTenant) {
              final tenantName = tenants.firstWhere(
                (t) => t['tenantId'] == newId,
                orElse: () => <String, dynamic>{'name': 'empresa'},
              )['name'];
              final messenger = ScaffoldMessenger.of(context);
              final success = await ref
                  .read(authProvider.notifier)
                  .switchTenant(newId);
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
}
