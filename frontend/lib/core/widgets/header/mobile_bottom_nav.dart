import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';

/// Bottom navigation bar for mobile screens.
/// Shows 5 tabs: Home, People, Operations, Finance, More
final class MobileBottomNav extends StatelessWidget {
  const MobileBottomNav({super.key});

  /// Returns the current selected index based on the route
  static int _getSelectedIndex(String location) {
    if (location.startsWith('/dashboard') ||
        location.startsWith('/executive') ||
        location.startsWith('/profile') ||
        location.startsWith('/absence-calendar')) {
      return 0; // Home
    }
    if (location.startsWith('/employees') ||
        location.startsWith('/attendance') ||
        location.startsWith('/payroll') ||
        location.startsWith('/vacations') ||
        location.startsWith('/permissions') ||
        location.startsWith('/departments') ||
        location.startsWith('/providers')) {
      return 1; // People
    }
    if (location.startsWith('/sales') ||
        location.startsWith('/quotes') ||
        location.startsWith('/products') ||
        location.startsWith('/categories') ||
        location.startsWith('/brands') ||
        location.startsWith('/purchases') ||
        location.startsWith('/suppliers') ||
        location.startsWith('/inventory') ||
        location.startsWith('/warranties') ||
        location.startsWith('/clients') ||
        location.startsWith('/credits')) {
      return 2; // Operations
    }
    if (location.startsWith('/cash') ||
        location.startsWith('/treasury') ||
        location.startsWith('/accounting') ||
        location.startsWith('/budgets') ||
        location.startsWith('/cost-centers') ||
        location.startsWith('/exchange')) {
      return 3; // Finance
    }
    return 4; // More
  }

  /// Navigate to the appropriate route based on tab index
  void _onTap(BuildContext context, int index) {
    final location = GoRouterState.of(context).matchedLocation;
    final currentIndex = _getSelectedIndex(location);

    // If tapping the same tab, don't navigate
    if (index == currentIndex) return;

    switch (index) {
      case 0:
        context.go('/dashboard');
        break;
      case 1:
        context.go('/employees');
        break;
      case 2:
        context.go('/sales');
        break;
      case 3:
        context.go('/cash-registers');
        break;
      case 4:
        // Show more menu as bottom sheet
        _showMoreMenu(context);
        break;
    }
  }

  static void _showMoreMenu(BuildContext context) {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Handle bar
            Container(
              margin: const EdgeInsets.only(top: 8),
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: ZColors.neutral300,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(16),
              child: Text(
                'Más opciones',
                style: ZTypography.titleMedium.copyWith(fontWeight: FontWeight.w600),
              ),
            ),
            const Divider(height: 1),
            _MoreItem(
              icon: Icons.chat_bubble_outline,
              label: 'Comunicación',
              route: '/chat',
            ),
            _MoreItem(
              icon: Icons.insights_outlined,
              label: 'BI e Inteligencia',
              route: '/bi/executive',
            ),
            _MoreItem(
              icon: Icons.description_outlined,
              label: 'Documentos',
              route: '/documents',
            ),
            _MoreItem(
              icon: Icons.admin_panel_settings_outlined,
              label: 'Administración',
              route: '/admin/users',
            ),
            _MoreItem(
              icon: Icons.settings_outlined,
              label: 'Configuración',
              route: '/settings',
            ),
            const SizedBox(height: 8),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final location = GoRouterState.of(context).matchedLocation;
    final selectedIndex = _getSelectedIndex(location);
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      decoration: BoxDecoration(
        color: isDark ? ZColors.darkSurface : ZColors.surface,
        border: Border(
          top: BorderSide(
            color: isDark
                ? ZColors.darkBorder.withValues(alpha: 0.5)
                : ZColors.border,
            width: 0.5,
          ),
        ),
      ),
      child: BottomNavigationBar(
        currentIndex: selectedIndex.clamp(0, 3),
        onTap: (index) => _onTap(context, index),
        type: BottomNavigationBarType.fixed,
        backgroundColor: isDark ? ZColors.darkSurface : ZColors.surface,
        selectedItemColor: isDark ? ZColors.brandAccent : ZColors.brandPrimary,
        unselectedItemColor: isDark ? ZColors.neutral500 : ZColors.neutral400,
        selectedFontSize: 10,
        unselectedFontSize: 10,
        elevation: 0,
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.dashboard_outlined, size: 22),
            activeIcon: Icon(Icons.dashboard, size: 22),
            label: 'Inicio',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.groups_2_outlined, size: 22),
            activeIcon: Icon(Icons.groups_2, size: 22),
            label: 'Personas',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.point_of_sale_outlined, size: 22),
            activeIcon: Icon(Icons.point_of_sale, size: 22),
            label: 'Operaciones',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.account_balance_wallet_outlined, size: 22),
            activeIcon: Icon(Icons.account_balance_wallet, size: 22),
            label: 'Finanzas',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.grid_view_outlined, size: 22),
            activeIcon: Icon(Icons.grid_view, size: 22),
            label: 'Más',
          ),
        ],
      ),
    );
  }
}

/// Individual item in the "More" menu
class _MoreItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final String route;

  const _MoreItem({
    required this.icon,
    required this.label,
    required this.route,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return ListTile(
      leading: Icon(
        icon,
        size: 20,
        color: isDark ? ZColors.neutral300 : ZColors.neutral600,
      ),
      title: Text(
        label,
        style: ZTypography.bodyMedium.copyWith(
          color: isDark ? ZColors.neutral200 : ZColors.neutral700,
        ),
      ),
      trailing: Icon(
        Icons.chevron_right,
        size: 18,
        color: isDark ? ZColors.neutral500 : ZColors.neutral400,
      ),
      onTap: () {
        Navigator.pop(context);
        context.go(route);
      },
    );
  }
}