import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../../navigation/nav_config.dart';
import '../../navigation/nav_provider.dart';

/// Hub fijo de la barra inferior móvil.
///
/// Cada hub está atado a un [NavModule] (`id`). El hub solo aparece si el
/// usuario ve ese módulo (rol + permisos + plan) y navega al primer ítem
/// permitido del módulo, no a una ruta hardcodeada.
@immutable
final class MobileNavHub {
  final String id;
  final String label;
  final IconData icon;
  final IconData activeIcon;
  final String fallbackRoute;

  /// Rutas que "pertenecen" a este hub para pintar el tab activo.
  final List<String> prefixes;

  const MobileNavHub({
    required this.id,
    required this.label,
    required this.icon,
    required this.activeIcon,
    required this.fallbackRoute,
    required this.prefixes,
  });

  bool matches(String location) => prefixes.any(
        (p) => location == p || location.startsWith('$p/'),
      );
}

/// Destino ya resuelto para la barra (hub, Inicio o "Más").
@immutable
final class MobileDestination {
  final String label;
  final IconData icon;
  final IconData activeIcon;
  final String? route;
  final String? hubId;
  final List<String> prefixes;

  const MobileDestination({
    required this.label,
    required this.icon,
    required this.activeIcon,
    this.route,
    this.hubId,
    this.prefixes = const [],
  });

  bool get isMore => route == null;

  bool matches(String location) => prefixes.any(
        (p) => location == p || location.startsWith('$p/'),
      );
}

/// Bottom navigation bar for phone screens.
///
/// - Material 3 [NavigationBar] (48 dp mínimo por destino).
/// - Hasta 3 hubs contextuales + "Más" (si el usuario no tiene acceso a
///   ningún módulo, solo ve Inicio + Más).
/// - Cuando la ruta actual no pertenece a ningún hub, "Más" queda activo
///   (antes el índice se recortaba con `clamp(0, 3)` y nunca se marcaba).
final class MobileBottomNav extends ConsumerWidget {
  const MobileBottomNav({super.key});

  /// Orden de preferencia de los hubs. Se muestran los 3 primeros módulos
  /// que el usuario realmente puede ver.
  static const List<MobileNavHub> hubCatalog = [
    MobileNavHub(
      id: 'ventas',
      label: 'Ventas',
      icon: Icons.point_of_sale_outlined,
      activeIcon: Icons.point_of_sale,
      fallbackRoute: '/sales',
      prefixes: [
        '/sales',
        '/quotes',
        '/credits',
        '/credit-notes',
        '/clients',
        '/pos',
        '/crm',
      ],
    ),
    MobileNavHub(
      id: 'inventario',
      label: 'Inventario',
      icon: Icons.inventory_2_outlined,
      activeIcon: Icons.inventory_2,
      fallbackRoute: '/products',
      prefixes: [
        '/products',
        '/categories',
        '/brands',
        '/inventory-movements',
        '/stock-transfers',
        '/warehouses',
        '/inventory-adjustment',
        '/warranties',
        '/workshops',
      ],
    ),
    MobileNavHub(
      id: 'talento',
      label: 'Personas',
      icon: Icons.groups_2_outlined,
      activeIcon: Icons.groups_2,
      fallbackRoute: '/employees',
      prefixes: [
        '/employees',
        '/attendance',
        '/payroll',
        '/vacations',
        '/permissions',
        '/departments',
        '/providers',
        '/service-payments',
        '/certificate-requests',
        '/goals',
      ],
    ),
    MobileNavHub(
      id: 'finanzas',
      label: 'Finanzas',
      icon: Icons.account_balance_wallet_outlined,
      activeIcon: Icons.account_balance_wallet,
      fallbackRoute: '/cash-registers',
      prefixes: [
        '/cash-registers',
        '/treasury',
        '/accounting',
        '/budgets',
        '/cost-centers',
        '/exchange-rates',
        '/reconciliations',
      ],
    ),
    MobileNavHub(
      id: 'compras',
      label: 'Compras',
      icon: Icons.shopping_cart_outlined,
      activeIcon: Icons.shopping_cart,
      fallbackRoute: '/purchases',
      prefixes: [
        '/purchases',
        '/purchase-orders',
        '/purchase-requests',
        '/suppliers',
      ],
    ),
    MobileNavHub(
      id: 'flota',
      label: 'Flota',
      icon: Icons.local_shipping_outlined,
      activeIcon: Icons.local_shipping,
      fallbackRoute: '/fleet',
      prefixes: ['/fleet'],
    ),
  ];

  static const MobileDestination homeDestination = MobileDestination(
    label: 'Inicio',
    icon: Icons.dashboard_outlined,
    activeIcon: Icons.dashboard,
    route: '/dashboard',
    prefixes: ['/dashboard', '/executive-dashboard'],
  );

  static const MobileDestination moreDestination = MobileDestination(
    label: 'Más',
    icon: Icons.grid_view_outlined,
    activeIcon: Icons.grid_view,
  );

  /// Resuelve los destinos visibles para el usuario.
  ///
  /// [permissions] y [enabledModules] deben venir del `AuthState` (claims del
  /// backend). `permissions == null` = sesión sin permisos cargados
  /// (SSO/API key): solo los roles del sistema ven módulos, igual que el
  /// sidebar (`NavConfig.getModulesForRole`).
  static List<MobileDestination> buildDestinations({
    required String role,
    Set<String>? permissions,
    Set<String>? enabledModules,
  }) {
    final modules = NavConfig.getModulesForRole(
      role,
      enabledModules: enabledModules,
      permissions: permissions,
    );
    final byId = {for (final m in modules) m.id: m};

    final hubs = <MobileDestination>[];
    for (final hub in hubCatalog) {
      final module = byId[hub.id];
      if (module == null) continue;
      // Preferimos la ruta canónica del hub (p. ej. Ventas → /sales, no
      // Cotizaciones) si el usuario tiene acceso; si no, el primer ítem.
      final hasPreferred =
          module.children.any((c) => c.route == hub.fallbackRoute);
      final route = hasPreferred
          ? hub.fallbackRoute
          : (module.children.isNotEmpty
              ? module.children.first.route
              : hub.fallbackRoute);
      hubs.add(
        MobileDestination(
          label: hub.label,
          icon: hub.icon,
          activeIcon: hub.activeIcon,
          route: route,
          hubId: hub.id,
          prefixes: hub.prefixes,
        ),
      );
      if (hubs.length == 3) break;
    }

    return [homeDestination, ...hubs, moreDestination];
  }

  /// Índice activo para [location]. Cae a "Más" cuando la ruta no pertenece
  /// a ningún hub (nunca recorta el índice).
  static int selectedIndex(
    String location,
    List<MobileDestination> destinations,
  ) {
    for (var i = 0; i < destinations.length; i++) {
      if (destinations[i].isMore) continue;
      if (destinations[i].matches(location)) return i;
    }
    return destinations.length - 1;
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authProvider);
    final role = auth.role ?? 'Employee';
    final destinations = buildDestinations(
      role: role,
      permissions: auth.permissions?.toSet(),
      enabledModules: auth.enabledModules?.toSet(),
    );
    final location = GoRouterState.of(context).matchedLocation;
    final selected = selectedIndex(location, destinations);
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final surface = isDark ? ZColors.darkSurface : ZColors.surface;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: surface,
        border: Border(
          top: BorderSide(
            color: isDark
                ? ZColors.darkBorder.withValues(alpha: 0.6)
                : ZColors.border,
            width: 0.5,
          ),
        ),
      ),
      child: NavigationBar(
        selectedIndex: selected,
        backgroundColor: surface,
        surfaceTintColor: Colors.transparent,
        indicatorColor: ZColors.brandAccent.withValues(alpha: 0.14),
        labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
        onDestinationSelected: (index) {
          if (index == selected) return;
          final destination = destinations[index];
          if (destination.isMore) {
            _showMoreSheet(
              context,
              role: role,
              permissions: auth.permissions?.toSet(),
              enabledModules: auth.enabledModules?.toSet(),
            );
            return;
          }
          ref.read(recentItemsProvider.notifier).add(destination.route!);
          context.go(destination.route!);
        },
        destinations: [
          for (final destination in destinations)
            NavigationDestination(
              icon: Icon(destination.icon, size: 24),
              selectedIcon: Icon(destination.activeIcon, size: 24),
              label: destination.label,
              tooltip: destination.label,
            ),
        ],
      ),
    );
  }

  static void _showMoreSheet(
    BuildContext context, {
    required String role,
    Set<String>? permissions,
    Set<String>? enabledModules,
  }) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      backgroundColor: isDark ? ZColors.darkSurface : ZColors.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(ZRadii.xl)),
      ),
      builder: (_) => _MoreSheet(
        role: role,
        permissions: permissions,
        enabledModules: enabledModules,
      ),
    );
  }
}

/// Hoja "Más": todos los módulos agrupados (igual que el sidebar) con buscador.
final class _MoreSheet extends ConsumerStatefulWidget {
  final String role;
  final Set<String>? permissions;
  final Set<String>? enabledModules;

  const _MoreSheet({
    required this.role,
    this.permissions,
    this.enabledModules,
  });

  @override
  ConsumerState<_MoreSheet> createState() => _MoreSheetState();
}

final class _MoreSheetState extends ConsumerState<_MoreSheet> {
  final TextEditingController _search = TextEditingController();
  String _query = '';

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  void _go(String route) {
    final router = GoRouter.of(context);
    Navigator.of(context).pop();
    ref.read(recentItemsProvider.notifier).add(route);
    router.go(route);
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final modules = NavConfig.getModulesForRole(
      widget.role,
      enabledModules: widget.enabledModules,
      permissions: widget.permissions,
    );
    final q = _query.trim().toLowerCase();
    final rows = _flatten(modules, q);

    return DraggableScrollableSheet(
      expand: false,
      initialChildSize: 0.92,
      minChildSize: 0.5,
      maxChildSize: 0.96,
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
                      'Todos los módulos',
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
            Padding(
              padding: const EdgeInsets.fromLTRB(
                ZSpacing.lg,
                ZSpacing.sm,
                ZSpacing.lg,
                ZSpacing.sm,
              ),
              child: TextField(
                controller: _search,
                autofocus: false,
                textInputAction: TextInputAction.search,
                onChanged: (value) => setState(() => _query = value),
                decoration: InputDecoration(
                  isDense: true,
                  hintText: 'Buscar módulo o pantalla',
                  prefixIcon: const Icon(Icons.search, size: 20),
                  suffixIcon: _query.isEmpty
                      ? null
                      : IconButton(
                          icon: const Icon(Icons.clear, size: 18),
                          tooltip: 'Limpiar',
                          onPressed: () {
                            _search.clear();
                            setState(() => _query = '');
                          },
                        ),
                  filled: true,
                  fillColor: isDark
                      ? ZColors.neutral700.withValues(alpha: 0.3)
                      : ZColors.neutral100,
                  contentPadding:
                      const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(ZRadii.lg),
                    borderSide: BorderSide.none,
                  ),
                ),
              ),
            ),
            Expanded(
              child: rows.isEmpty
                  ? Center(
                      child: Padding(
                        padding: const EdgeInsets.all(ZSpacing.xl),
                        child: Text(
                          'Sin resultados para "$_query"',
                          textAlign: TextAlign.center,
                          style: ZTypography.bodyMedium.copyWith(
                            color: isDark
                                ? ZColors.neutral400
                                : ZColors.neutral500,
                          ),
                        ),
                      ),
                    )
                  : ListView.builder(
                      controller: scrollController,
                      padding: const EdgeInsets.only(bottom: ZSpacing.xl),
                      itemCount: rows.length,
                      itemBuilder: (context, index) {
                        final row = rows[index];
                        return switch (row) {
                          _GroupRow(:final label) => _GroupHeader(label),
                          _ModuleRow(:final module) => _ModuleHeader(module),
                          _ItemRow(:final module, :final item) => _ItemTile(
                              module: module,
                              item: item,
                              onTap: () => _go(item.route),
                            ),
                        };
                      },
                    ),
            ),
          ],
        );
      },
    );
  }

  /// Aplana los módulos visibles en filas para una `ListView.builder`.
  /// Con búsqueda activa solo se muestran los ítems que coinciden (y el
  /// encabezado del módulo que los contiene).
  static List<_MoreRow> _flatten(List<NavModule> modules, String query) {
    final rows = <_MoreRow>[];
    final order = ['operations', 'financial', 'talent', 'intelligence', 'admin'];
    final sorted = [...modules]..sort(
        (a, b) => order.indexOf(a.group).compareTo(order.indexOf(b.group)),
      );

    String? currentGroup;
    for (final module in sorted) {
      final items = query.isEmpty
          ? module.children
          : module.children
              .where((i) => i.label.toLowerCase().contains(query) ||
                  module.label.toLowerCase().contains(query))
              .toList();
      if (items.isEmpty) continue;

      if (module.group != currentGroup) {
        currentGroup = module.group;
        rows.add(_GroupRow(NavConfig.groupLabels[module.group] ?? module.group));
      }
      rows.add(_ModuleRow(module));
      for (final item in items) {
        rows.add(_ItemRow(module, item));
      }
    }
    return rows;
  }
}

sealed class _MoreRow {
  const _MoreRow();
}

final class _GroupRow extends _MoreRow {
  final String label;
  const _GroupRow(this.label);
}

final class _ModuleRow extends _MoreRow {
  final NavModule module;
  const _ModuleRow(this.module);
}

final class _ItemRow extends _MoreRow {
  final NavModule module;
  final NavItem item;
  const _ItemRow(this.module, this.item);
}

final class _GroupHeader extends StatelessWidget {
  final String label;

  const _GroupHeader(this.label);

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        ZSpacing.lg,
        ZSpacing.lg,
        ZSpacing.lg,
        ZSpacing.xs,
      ),
      child: Text(
        label,
        style: ZTypography.labelMedium.copyWith(
          color: isDark ? ZColors.neutral400 : ZColors.neutral500,
        ),
      ),
    );
  }
}

final class _ModuleHeader extends StatelessWidget {
  final NavModule module;

  const _ModuleHeader(this.module);

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        ZSpacing.lg,
        ZSpacing.sm,
        ZSpacing.lg,
        ZSpacing.xxs,
      ),
      child: Row(
        children: [
          Icon(
            module.icon,
            size: 18,
            color: isDark ? module.color : module.textColor,
          ),
          const SizedBox(width: ZSpacing.sm),
          Expanded(
            child: Text(
              module.label,
              style: ZTypography.titleSmall.copyWith(
                color: isDark ? ZColors.neutral200 : ZColors.neutral700,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

final class _ItemTile extends StatelessWidget {
  final NavModule module;
  final NavItem item;
  final VoidCallback onTap;

  const _ItemTile({
    required this.module,
    required this.item,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return ListTile(
      minTileHeight: ZTouchTarget.min,
      contentPadding: const EdgeInsets.only(left: 40, right: ZSpacing.lg),
      leading: Icon(
        item.icon,
        size: 20,
        color: isDark ? ZColors.neutral300 : ZColors.neutral600,
      ),
      title: Text(
        item.label,
        maxLines: 2,
        overflow: TextOverflow.ellipsis,
        style: ZTypography.bodyMedium.copyWith(
          color: isDark ? ZColors.neutral200 : ZColors.neutral700,
        ),
      ),
      trailing: Icon(
        Icons.chevron_right,
        size: 18,
        color: isDark ? ZColors.neutral500 : ZColors.neutral400,
      ),
      onTap: onTap,
    );
  }
}
