import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../theme/theme_provider.dart';

final class AppShell extends ConsumerWidget {
  final Widget child;

  const AppShell({super.key, required this.child});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authProvider);
    final role = auth.role ?? 'Employee';
    final theme = Theme.of(context);

    return Row(
      children: [
        NavigationRail(
          selectedIndex: _selectedIndex(context, role),
          onDestinationSelected: (i) => _onNavigate(context, i, role),
          labelType: NavigationRailLabelType.all,
          leading: Padding(
            padding: const EdgeInsets.symmetric(vertical: 8),
            child: Icon(Icons.diamond, color: theme.colorScheme.primary, size: 32),
          ),
          destinations: _navDestinations(role),
          trailing: Expanded(
            child: Align(
              alignment: Alignment.bottomCenter,
              child: Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
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
              ),
            ),
          ),
        ),
        const VerticalDivider(width: 1),
        Expanded(child: child),
      ],
    );
  }

  int _selectedIndex(BuildContext context, String role) {
    final location = GoRouterState.of(context).matchedLocation;
    final entries = _navItems(role);
    for (var i = 0; i < entries.length; i++) {
      if (location.startsWith(entries[i].route)) return i;
    }
    return 0;
  }

  void _onNavigate(BuildContext context, int index, String role) {
    final entries = _navItems(role);
    if (index >= 0 && index < entries.length) {
      context.go(entries[index].route);
    }
  }

  List<_NavItem> _navItems(String role) {
    final all = <_NavItem>[
      _NavItem('Dashboard', Icons.dashboard, '/dashboard'),
      _NavItem('Clientes', Icons.people, '/clients'),
      _NavItem('Ventas', Icons.point_of_sale, '/sales'),
      _NavItem('Cotizaciones', Icons.description, '/quotes'),
      _NavItem('Productos', Icons.inventory_2, '/products'),
      _NavItem('Kardex', Icons.inventory, '/inventory-movements'),
      _NavItem('Créditos', Icons.credit_card, '/credits'),
      _NavItem('Caja', Icons.monetization_on, '/cash-registers'),
      _NavItem('Garantías', Icons.verified, '/warranties'),
    ];

    if (role == 'SuperAdmin' || role == 'CompanyAdmin' || role == 'Rrhh') {
      all.addAll([
        _NavItem('Ejecutivo', Icons.analytics, '/executive-dashboard'),
        _NavItem('RH', Icons.badge, '/employees'),
        _NavItem('Categorías', Icons.category, '/categories'),
        _NavItem('Marcas', Icons.branding_watermark, '/brands'),
        _NavItem('Proveedores', Icons.local_shipping, '/suppliers'),
        _NavItem('Vacaciones', Icons.beach_access, '/vacations'),
        _NavItem('Permisos', Icons.description, '/permissions'),
        _NavItem('Asistencia', Icons.schedule, '/attendance'),
        _NavItem('Nómina', Icons.receipt_long, '/payroll'),
        _NavItem('Reportes', Icons.assessment, '/reports'),
        _NavItem('Config.', Icons.settings, '/settings'),
      ]);
    }

    return all;
  }

  List<NavigationRailDestination> _navDestinations(String role) {
    return _navItems(role).map((item) => NavigationRailDestination(
      icon: Icon(item.icon, size: 20),
      selectedIcon: Icon(item.icon, size: 20),
      label: Text(item.label, style: const TextStyle(fontSize: 11)),
    )).toList();
  }
}

final class _NavItem {
  final String label;
  final IconData icon;
  final String route;
  const _NavItem(this.label, this.icon, this.route);
}
