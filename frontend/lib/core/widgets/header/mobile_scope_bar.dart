import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../../providers/company_branch_provider.dart';

/// Franja de contexto multi-tenant para teléfonos.
///
/// El [GlobalHeader] no se monta en el layout móvil (solo tablet/desktop), así
/// que sin esta franja el usuario de teléfono no puede ver ni cambiar la
/// compañía, la sucursal o la bodega activa: escribiría en el ámbito que
/// quedara del último login.
///
/// Semántica idéntica al header de escritorio:
/// - cambiar de compañía resetea sucursal y bodega + invalida sus listas;
/// - cambiar de sucursal resetea la bodega;
/// - "Todas las sucursales"/"Todas las bodegas" son elecciones explícitas;
/// - SuperAdmin dispara `switchTenant` al cambiar de compañía.
final class MobileScopeBar extends ConsumerWidget {
  const MobileScopeBar({super.key});

  static String scopeLabel(CompanyBranchState state) {
    final company = state.companyName ?? 'Empresa';
    final branch = state.allBranches
        ? 'Todas las sucursales'
        : (state.branchName ?? 'Sucursal');
    final parts = <String>[
      company,
      branch,
      if (state.warehouseName != null && state.warehouseName!.isNotEmpty)
        state.warehouseName!,
    ];
    return parts.join(' · ');
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final scope = ref.watch(companyBranchProvider);
    final branchesAsync = ref.watch(headerBranchListProvider);

    // El header de escritorio auto-selecciona la primera sucursal; en móvil
    // ese widget no existe, así que la auto-selección vive aquí.
    branchesAsync.whenData((branches) {
      if (scope.companyId == null ||
          scope.branchId != null ||
          scope.allBranches ||
          branches.isEmpty) {
        return;
      }
      WidgetsBinding.instance.addPostFrameCallback((_) {
        final current = ref.read(companyBranchProvider);
        if (current.companyId == null ||
            current.branchId != null ||
            current.allBranches) {
          return;
        }
        final first = branches.first;
        final id = first['id'] as String? ?? '';
        if (id.isEmpty) return;
        ref.read(companyBranchProvider.notifier).selectBranch(
              id,
              first['name'] as String? ?? 'Sucursal',
            );
      });
    });

    return Material(
      color: isDark ? ZColors.darkSurface : ZColors.surface,
      child: InkWell(
        onTap: () => _showScopeSheet(context),
        child: Container(
          height: ZTouchTarget.compact,
          padding: const EdgeInsets.symmetric(horizontal: ZSpacing.lg),
          decoration: BoxDecoration(
            border: Border(
              top: BorderSide(
                color: isDark
                    ? ZColors.darkBorder.withValues(alpha: 0.6)
                    : ZColors.border,
                width: 0.5,
              ),
            ),
          ),
          child: Row(
            children: [
              Icon(
                Icons.apartment_outlined,
                size: 16,
                color: isDark ? ZColors.neutral400 : ZColors.neutral500,
              ),
              const SizedBox(width: ZSpacing.sm),
              Expanded(
                child: Text(
                  scopeLabel(scope),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: ZTypography.bodySmall.copyWith(
                    fontWeight: FontWeight.w500,
                    color: isDark ? ZColors.neutral200 : ZColors.neutral700,
                  ),
                ),
              ),
              Icon(
                Icons.expand_more,
                size: 18,
                color: isDark ? ZColors.neutral400 : ZColors.neutral500,
              ),
            ],
          ),
        ),
      ),
    );
  }

  static void _showScopeSheet(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      backgroundColor: isDark ? ZColors.darkSurface : ZColors.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(ZRadii.xl)),
      ),
      builder: (_) => const _ScopeSheet(),
    );
  }
}

/// Hoja con las 3 secciones de contexto: empresa, sucursal y bodega.
final class _ScopeSheet extends ConsumerWidget {
  const _ScopeSheet();

  Future<void> _selectCompany(
    WidgetRef ref,
    String id,
    String name,
    String tenantId,
    bool isSuperAdmin,
  ) async {
    final notifier = ref.read(companyBranchProvider.notifier);
    notifier.selectCompany(id, name);
    ref.invalidate(headerBranchListProvider);
    ref.invalidate(headerWarehouseListProvider);
    if (isSuperAdmin && tenantId.isNotEmpty) {
      await ref.read(authProvider.notifier).switchTenant(tenantId);
      ref.invalidate(headerBranchListProvider);
      ref.invalidate(headerWarehouseListProvider);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final scope = ref.watch(companyBranchProvider);
    final companiesAsync = ref.watch(companyListProvider);
    final branchesAsync = ref.watch(headerBranchListProvider);
    final warehousesAsync = ref.watch(headerWarehouseListProvider);
    final isSuperAdmin = ref.watch(authProvider).role == 'SuperAdmin';

    final companies = companiesAsync.value ?? const <Map<String, dynamic>>[];
    final branches = branchesAsync.value ?? const <Map<String, dynamic>>[];
    final warehouses =
        warehousesAsync.value ?? const <Map<String, dynamic>>[];

    final mutedStyle = ZTypography.bodySmall.copyWith(
      color: isDark ? ZColors.neutral400 : ZColors.neutral500,
    );

    return DraggableScrollableSheet(
      expand: false,
      initialChildSize: 0.7,
      minChildSize: 0.4,
      maxChildSize: 0.92,
      builder: (context, scrollController) {
        return Column(
          children: [
            const SizedBox(height: ZSpacing.sm),
            Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: isDark ? ZColors.neutral600 : ZColors.neutral300,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(
                ZSpacing.lg,
                ZSpacing.md,
                ZSpacing.sm,
                0,
              ),
              child: Row(
                children: [
                  Expanded(
                    child: Text(
                      'Contexto de trabajo',
                      style: ZTypography.titleLarge.copyWith(
                        color: isDark ? ZColors.neutral100 : ZColors.neutral700,
                      ),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close),
                    tooltip: 'Cerrar',
                    onPressed: () => Navigator.of(context).pop(),
                  ),
                ],
              ),
            ),
            Expanded(
              child: ListView(
                controller: scrollController,
                padding: const EdgeInsets.only(bottom: ZSpacing.xl),
                children: [
                  if (companies.length > 1) ...[
                    _SectionHeader('Empresa', style: mutedStyle),
                    for (final company in companies)
                      _ScopeOption(
                        label: company['name'] as String? ??
                            company['legalName'] as String? ??
                            'Sin nombre',
                        icon: Icons.business_outlined,
                        selected: scope.companyId == company['id'],
                        onTap: () => _selectCompany(
                          ref,
                          company['id'] as String? ?? '',
                          company['name'] as String? ??
                              company['legalName'] as String? ??
                              'Empresa',
                          company['tenantId'] as String? ?? '',
                          isSuperAdmin,
                        ),
                      ),
                  ],
                  _SectionHeader('Sucursal', style: mutedStyle),
                  if (branchesAsync.isLoading)
                    const Padding(
                      padding: EdgeInsets.all(ZSpacing.lg),
                      child: LinearProgressIndicator(),
                    )
                  else ...[
                    _ScopeOption(
                      label: 'Todas las sucursales',
                      icon: Icons.all_inclusive_outlined,
                      selected: scope.allBranches,
                      onTap: () =>
                          ref.read(companyBranchProvider.notifier).selectAllBranches(),
                    ),
                    if (branches.isEmpty)
                      Padding(
                        padding: const EdgeInsets.symmetric(
                          horizontal: ZSpacing.lg,
                          vertical: ZSpacing.sm,
                        ),
                        child: Text('Sin sucursales en esta compañía',
                            style: mutedStyle),
                      ),
                    for (final branch in branches)
                      _ScopeOption(
                        label: branch['name'] as String? ?? 'Sin nombre',
                        icon: Icons.storefront_outlined,
                        selected: scope.branchId == branch['id'],
                        onTap: () => ref
                            .read(companyBranchProvider.notifier)
                            .selectBranch(
                              branch['id'] as String? ?? '',
                              branch['name'] as String? ?? 'Sucursal',
                            ),
                      ),
                  ],
                  _SectionHeader('Bodega', style: mutedStyle),
                  if (warehousesAsync.isLoading)
                    const Padding(
                      padding: EdgeInsets.all(ZSpacing.lg),
                      child: LinearProgressIndicator(),
                    )
                  else ...[
                    _ScopeOption(
                      label: 'Todas las bodegas',
                      icon: Icons.all_inclusive_outlined,
                      selected: scope.warehouseId == null,
                      onTap: () => ref
                          .read(companyBranchProvider.notifier)
                          .selectWarehouse(null, null),
                    ),
                    if (warehouses.isEmpty)
                      Padding(
                        padding: const EdgeInsets.symmetric(
                          horizontal: ZSpacing.lg,
                          vertical: ZSpacing.sm,
                        ),
                        child: Text('Sin bodegas en el ámbito actual',
                            style: mutedStyle),
                      ),
                    for (final warehouse in warehouses)
                      _ScopeOption(
                        label: (warehouse['isDefault'] as bool? ?? false)
                            ? '${warehouse['name']} (principal)'
                            : warehouse['name'] as String? ?? 'Sin nombre',
                        icon: Icons.warehouse_outlined,
                        selected: scope.warehouseId == warehouse['id'],
                        onTap: () => ref
                            .read(companyBranchProvider.notifier)
                            .selectWarehouse(
                              warehouse['id'] as String? ?? '',
                              warehouse['name'] as String? ?? 'Bodega',
                            ),
                      ),
                  ],
                ],
              ),
            ),
            SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(
                  ZSpacing.lg,
                  ZSpacing.sm,
                  ZSpacing.lg,
                  ZSpacing.sm,
                ),
                child: SizedBox(
                  width: double.infinity,
                  child: ZButton(
                    text: 'Listo',
                    onPressed: () => Navigator.of(context).pop(),
                  ),
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}

final class _SectionHeader extends StatelessWidget {
  final String label;
  final TextStyle style;

  const _SectionHeader(this.label, {required this.style});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        ZSpacing.lg,
        ZSpacing.lg,
        ZSpacing.lg,
        ZSpacing.xs,
      ),
      child: Text(label, style: style),
    );
  }
}

final class _ScopeOption extends StatelessWidget {
  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onTap;

  const _ScopeOption({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return ListTile(
      minTileHeight: ZTouchTarget.min,
      leading: Icon(
        selected ? Icons.check_circle : icon,
        size: 20,
        color: selected
            ? ZColors.brandAccent
            : (isDark ? ZColors.neutral400 : ZColors.neutral500),
      ),
      title: Text(
        label,
        maxLines: 2,
        overflow: TextOverflow.ellipsis,
        style: ZTypography.bodyMedium.copyWith(
          fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
          color: isDark ? ZColors.neutral200 : ZColors.neutral700,
        ),
      ),
      onTap: onTap,
    );
  }
}
