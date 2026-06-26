import 'package:flutter/material.dart';
import '../ds.dart';

/// A reusable company/tenant selector dropdown with built-in dark/light mode styling.
///
/// Shows a compact pill-shaped selector with brand-primary colors.
/// Returns empty widget when [tenants] has 0 or 1 items (no selection needed).
///
/// Usage:
/// ```dart
/// ZCompanyDropdown(
///   tenants: tenants,
///   currentTenantId: currentTenant,
///   onChanged: (newId) async {
///     await ref.read(authProvider.notifier).switchTenant(newId!);
///   },
/// )
/// ```
class ZCompanyDropdown extends StatelessWidget {
  final List<Map<String, dynamic>> tenants;
  final String? currentTenantId;
  final ValueChanged<String?>? onChanged;
  final IconData icon;
  final EdgeInsetsGeometry? padding;

  const ZCompanyDropdown({
    super.key,
    required this.tenants,
    this.currentTenantId,
    this.onChanged,
    this.icon = Icons.business,
    this.padding,
  });

  @override
  Widget build(BuildContext context) {
    if (tenants.length <= 1) return const SizedBox.shrink();

    return Container(
      padding: padding ?? const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      decoration: BoxDecoration(
        color: ZColors.brandPrimary.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(ZRadii.full),
        border: Border.all(color: ZColors.brandPrimary.withValues(alpha: 0.2)),
      ),
      child: DropdownButton<String>(
        value: currentTenantId,
        underline: const SizedBox(),
        isDense: true,
        icon: Icon(icon, size: 16, color: ZColors.brandPrimary),
        dropdownColor: Theme.of(context).colorScheme.surface,
        style: ZTypography.labelSmall.copyWith(
          color: ZColors.brandPrimary,
          fontWeight: FontWeight.bold,
        ),
        items: tenants
            .map((t) => DropdownMenuItem(
                  value: t['tenantId'] as String,
                  child: Text(t['name'] as String),
                ))
            .toList(),
        onChanged: onChanged,
      ),
    );
  }
}
