import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../theme/theme_provider.dart';
import 'responsive_layout.dart';

final class _ModuleInfo {
  final String label;
  final IconData icon;
  final List<_NavItem> items;
  const _ModuleInfo(this.label, this.icon, this.items);
}

final class AppShell extends ConsumerWidget {
  final Widget child;

  const AppShell({super.key, required this.child});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authProvider);
    final role = auth.role ?? 'Employee';
    final theme = Theme.of(context);
    final modules = _buildModules(role);
    final location = GoRouterState.of(context).matchedLocation;

    return ResponsiveBuilder(
      builder: (context, size) {
        if (size == ScreenSize.desktop) {
          return _DesktopLayout(modules: modules, location: location, theme: theme, ref: ref, child: child);
        }
        return _MobileLayout(modules: modules, location: location, theme: theme, ref: ref, child: child);
      },
    );
  }

  List<_ModuleInfo> _buildModules(String role) {
    final isAdmin = role == 'SuperAdmin' || role == 'CompanyAdmin' || role == 'Rrhh';

    return [
      _ModuleInfo('Panel General', Icons.dashboard, [
        _NavItem('Dashboard', Icons.dashboard, '/dashboard'),
        if (isAdmin) _NavItem('Ejecutivo', Icons.analytics, '/executive-dashboard'),
      ]),
      _ModuleInfo('Multisucursal', Icons.business, [
        _NavItem('Sucursales', Icons.store_mall_directory, '/branches'),
      ]),
      _ModuleInfo('Zorvian Comercial', Icons.shopping_cart, [
        _NavItem('Clientes', Icons.people, '/clients'),
        _NavItem('Ventas', Icons.point_of_sale, '/sales'),
        _NavItem('Cotizaciones', Icons.description, '/quotes'),
      ]),
      _ModuleInfo('Zorvian Inventario', Icons.inventory, [
        _NavItem('Productos', Icons.inventory_2, '/products'),
        _NavItem('Marcas', Icons.branding_watermark, '/brands'),
        _NavItem('Categorías', Icons.category, '/categories'),
        _NavItem('Compras', Icons.shopping_bag, '/purchases'),
        _NavItem('Kardex', Icons.swap_horiz, '/inventory-movements'),
        _NavItem('Ajuste Inventario', Icons.tune, '/inventory-adjustment'),
        _NavItem('Proveedores', Icons.local_shipping, '/suppliers'),
      ]),
      _ModuleInfo('Zorvian Créditos', Icons.credit_card, [
        _NavItem('Cartera de Créditos', Icons.account_balance_wallet, '/credits'),
      ]),
      _ModuleInfo('Zorvian Caja', Icons.monetization_on, [
        _NavItem('Movimientos de Caja', Icons.account_balance, '/cash-registers'),
      ]),
      _ModuleInfo('Garantías', Icons.verified, [
        _NavItem('Gestión de Garantías', Icons.assignment_turned_in, '/warranties'),
      ]),
      _ModuleInfo('Zorvian HR', Icons.badge, [
        _NavItem('Empleados', Icons.badge, '/employees'),
        _NavItem('Nómina', Icons.receipt_long, '/payroll'),
        _NavItem('Asistencia', Icons.schedule, '/attendance'),
        _NavItem('Vacaciones', Icons.beach_access, '/vacations'),
        _NavItem('Permisos', Icons.description, '/permissions'),
      ]),
      if (isAdmin)
        _ModuleInfo('Zorvian BI', Icons.insights, [
          _NavItem('Panel Ejecutivo', Icons.dashboard, '/bi/executive'),
          _NavItem('Panel Financiero', Icons.account_balance, '/bi/financial'),
          _NavItem('Panel Comercial', Icons.trending_up, '/bi/commercial'),
          _NavItem('Panel Operativo', Icons.precision_manufacturing, '/bi/operational'),
        ]),
      if (isAdmin)
        _ModuleInfo('Administración', Icons.admin_panel_settings, [
          _NavItem('Usuarios', Icons.person_add, '/admin/users'),
          _NavItem('Reportes', Icons.assessment, '/reports'),
          _NavItem('Auditoría', Icons.history, '/audit-logs'),
          _NavItem('Configuración', Icons.settings, '/settings'),
        ]),
    ];
  }
}

final class _DesktopLayout extends StatelessWidget {
  final List<_ModuleInfo> modules;
  final String location;
  final ThemeData theme;
  final WidgetRef ref;
  final Widget child;

  const _DesktopLayout({
    required this.modules,
    required this.location,
    required this.theme,
    required this.ref,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      child: Row(
        children: [
        Container(
          width: 260, // Increased width slightly for longer labels
          color: theme.colorScheme.surfaceContainerLow,
          child: Column(
            children: [
              _buildHeader(),
              const Divider(height: 1),
              Expanded(
                child: ListView(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  children: [
                    for (final mod in modules) ...[
                      _ModuleHeaderWidget(label: mod.label),
                      for (final item in mod.items)
                        _NavItemWidget(
                          item: item,
                          selected: location.startsWith(item.route),
                          onTap: () => context.go(item.route),
                        ),
                      if (mod != modules.last) const SizedBox(height: 8),
                    ],
                  ],
                ),
              ),
              const Divider(height: 1),
              _buildFooter(),
            ],
          ),
        ),
        const VerticalDivider(width: 1),
        Expanded(child: child),
      ],
    ),
    );
  }

  Widget _buildHeader() {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 16),
      child: Row(
        children: [
          Icon(Icons.diamond, color: theme.colorScheme.primary, size: 28),
          const SizedBox(width: 10),
          Text('Zorvian ERP', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }

  Widget _buildFooter() {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 8),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceEvenly,
        children: [
          Consumer(
            builder: (_, ref, _) {
              final mode = ref.watch(themeModeProvider);
              return IconButton(
                icon: Icon(mode == ThemeMode.dark ? Icons.light_mode : Icons.dark_mode, size: 20),
                onPressed: () => ref.read(themeModeProvider.notifier).toggle(),
              );
            },
          ),
          IconButton(
            icon: const Icon(Icons.logout, size: 20),
            onPressed: () => ref.read(authProvider.notifier).logout(),
          ),
        ],
      ),
    );
  }
}

final class _MobileLayout extends StatelessWidget {
  final List<_ModuleInfo> modules;
  final String location;
  final ThemeData theme;
  final WidgetRef ref;
  final Widget child;

  const _MobileLayout({
    required this.modules,
    required this.location,
    required this.theme,
    required this.ref,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Zorvian ERP'),
        leading: Builder(
          builder: (ctx) => IconButton(
            icon: const Icon(Icons.menu),
            onPressed: () => Scaffold.of(ctx).openDrawer(),
          ),
        ),
        actions: [
          Consumer(
            builder: (_, ref, _) {
              final mode = ref.watch(themeModeProvider);
              return IconButton(
                icon: Icon(mode == ThemeMode.dark ? Icons.light_mode : Icons.dark_mode),
                onPressed: () => ref.read(themeModeProvider.notifier).toggle(),
              );
            },
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () => ref.read(authProvider.notifier).logout(),
          ),
        ],
      ),
      drawer: Drawer(
        child: ListView(
          padding: EdgeInsets.zero,
          children: [
            DrawerHeader(
              decoration: BoxDecoration(color: theme.colorScheme.primaryContainer),
              child: Row(
                children: [
                  Icon(Icons.diamond, color: theme.colorScheme.onPrimaryContainer, size: 32),
                  const SizedBox(width: 12),
                  Text('Zorvian ERP', style: theme.textTheme.titleLarge?.copyWith(color: theme.colorScheme.onPrimaryContainer)),
                ],
              ),
            ),
            for (final mod in modules) ...[
              Padding(
                padding: const EdgeInsets.only(left: 16, top: 16, bottom: 4),
                child: Text(
                  mod.label.toUpperCase(),
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 1.2,
                    color: theme.colorScheme.primary,
                  ),
                ),
              ),
              for (final item in mod.items)
                ListTile(
                  leading: Icon(item.icon, color: location.startsWith(item.route) ? theme.colorScheme.primary : null),
                  title: Text(item.label, style: TextStyle(fontWeight: location.startsWith(item.route) ? FontWeight.w600 : FontWeight.normal)),
                  selected: location.startsWith(item.route),
                  selectedTileColor: theme.colorScheme.primaryContainer.withValues(alpha: 0.3),
                  onTap: () { Navigator.pop(context); context.go(item.route); },
                ),
            ],
          ],
        ),
      ),
      body: child,
    );
  }
}

final class _NavItem {
  final String label;
  final IconData icon;
  final String route;
  const _NavItem(this.label, this.icon, this.route);
}

final class _ModuleHeaderWidget extends StatelessWidget {
  final String label;
  const _ModuleHeaderWidget({required this.label});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(left: 16, top: 12, bottom: 4, right: 16),
      child: Text(
        label.toUpperCase(),
        style: TextStyle(
          fontSize: 10,
          fontWeight: FontWeight.w700,
          letterSpacing: 1.2,
          color: theme.colorScheme.primary,
        ),
      ),
    );
  }
}

final class _NavItemWidget extends StatelessWidget {
  final _NavItem item;
  final bool selected;
  final VoidCallback onTap;

  const _NavItemWidget({
    required this.item,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final color = selected
        ? theme.colorScheme.onSecondaryContainer
        : theme.colorScheme.onSurfaceVariant;
    final bg = selected ? theme.colorScheme.secondaryContainer : Colors.transparent;

    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 1),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(8),
      ),
      child: InkWell(
        borderRadius: BorderRadius.circular(8),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 12),
          child: Row(
            children: [
              Icon(item.icon, size: 20, color: color),
              const SizedBox(width: 12),
              Text(item.label, style: TextStyle(fontSize: 13, color: color, fontWeight: selected ? FontWeight.w600 : FontWeight.normal)),
            ],
          ),
        ),
      ),
    );
  }
}
