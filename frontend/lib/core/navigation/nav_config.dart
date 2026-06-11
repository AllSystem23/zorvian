import 'package:flutter/material.dart';

final class NavItem {
  final String id;
  final String label;
  final IconData icon;
  final String route;
  final List<String> roles;
  final String badgeRef;

  const NavItem({
    required this.id,
    required this.label,
    required this.icon,
    required this.route,
    this.roles = const [],
    this.badgeRef = '',
  });
}

final class NavModule {
  final String id;
  final String label;
  final IconData icon;
  final List<NavItem> children;
  final List<String> roles;

  const NavModule({
    required this.id,
    required this.label,
    required this.icon,
    required this.children,
    this.roles = const [],
  });
}

final class NavConfig {
  NavConfig._();

  static final List<NavModule> allModules = [
    // ── GESTIÓN INSTITUCIONAL ──
    const NavModule(
      id: 'institucional',
      label: 'Gestión Institucional',
      icon: Icons.hub_outlined,
      children: [
        NavItem(id: 'dashboard', label: 'Dashboard General', icon: Icons.dashboard, route: '/dashboard'),
        NavItem(id: 'asistente-ia', label: 'Asistente Z-IA', icon: Icons.smart_toy_outlined, route: '/accounting/ai-assistant'),
        NavItem(id: 'ejecutivo', label: 'Panel Ejecutivo', icon: Icons.analytics, route: '/executive-dashboard', roles: ['SuperAdmin', 'CompanyAdmin']),
        NavItem(id: 'bi-central', label: 'Inteligencia de Negocios', icon: Icons.insights, route: '/bi/executive', roles: ['SuperAdmin', 'CompanyAdmin']),
        NavItem(id: 'chat', label: 'Centro de Comunicación', icon: Icons.chat_bubble_outline, route: '/chat'),
        NavItem(id: 'perfil', label: 'Mi Perfil', icon: Icons.person_outline, route: '/profile'),
      ],
    ),

    // ── GESTIÓN DE TALENTO ──
    const NavModule(
      id: 'talento',
      label: 'Gestión de Talento',
      icon: Icons.diversity_3_outlined,
      children: [
        NavItem(id: 'empleados', label: 'Capital Humano', icon: Icons.people_outline, route: '/employees'),
        NavItem(id: 'asistencia', label: 'Reloj y Asistencia', icon: Icons.schedule_outlined, route: '/attendance'),
        NavItem(id: 'nomina', label: 'Gestión de Nómina', icon: Icons.receipt_long_outlined, route: '/payroll', roles: ['SuperAdmin', 'CompanyAdmin', 'Rrhh']),
        NavItem(id: 'prestadores', label: 'Prestadores Externos', icon: Icons.business_center_outlined, route: '/providers'),
        NavItem(id: 'vacaciones', label: 'Ausencias y Vacaciones', icon: Icons.beach_access_outlined, route: '/vacations'),
        NavItem(id: 'metas', label: 'Metas e Incentivos', icon: Icons.emoji_events_outlined, route: '/goals/dashboard'),
      ],
    ),

    // ── LOGÍSTICA Y SUMINISTROS ──
    const NavModule(
      id: 'logistica',
      label: 'Logística y Suministros',
      icon: Icons.local_shipping_outlined,
      children: [
        NavItem(id: 'inventario', label: 'Control de Inventario', icon: Icons.inventory_2_outlined, route: '/products'),
        NavItem(id: 'activos-fijos', label: 'Activos Fijos', icon: Icons.account_balance_wallet_outlined, route: '/fixed-assets', roles: ['SuperAdmin', 'CompanyAdmin']),
        NavItem(id: 'compras', label: 'Órdenes de Compra', icon: Icons.shopping_bag_outlined, route: '/purchases'),
        NavItem(id: 'proveedores', label: 'Gestión de Proveedores', icon: Icons.local_shipping_outlined, route: '/suppliers'),
        NavItem(id: 'garantias', label: 'Gestión de Garantías', icon: Icons.verified_user_outlined, route: '/warranties'),
        NavItem(id: 'sucursales', label: 'Red de Sucursales', icon: Icons.store_mall_directory_outlined, route: '/branches'),
      ],
    ),

    // ── OPERACIONES Y FINANZAS ──
    const NavModule(
      id: 'finanzas',
      label: 'Operaciones y Finanzas',
      icon: Icons.account_balance_outlined,
      children: [
        NavItem(id: 'comercial', label: 'Ventas y Facturación', icon: Icons.point_of_sale_outlined, route: '/sales'),
        NavItem(id: 'clientes', label: 'Cartera de Clientes', icon: Icons.people_alt_outlined, route: '/clients'),
        NavItem(id: 'creditos', label: 'Créditos y Cobros', icon: Icons.credit_card_outlined, route: '/credits'),
        NavItem(id: 'tesoreria', label: 'Tesorería y Bancos', icon: Icons.savings_outlined, route: '/treasury/checks', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
        NavItem(id: 'contabilidad', label: 'Contabilidad Central', icon: Icons.balance_outlined, route: '/accounting/trial-balance', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
        NavItem(id: 'caja', label: 'Movimientos de Caja', icon: Icons.monetization_on_outlined, route: '/cash-registers'),
      ],
    ),

    // ── SOPORTE Y CONTROL ──
    const NavModule(
      id: 'soporte',
      label: 'Soporte y Control',
      icon: Icons.settings_suggest_outlined,
      roles: ['SuperAdmin', 'CompanyAdmin'],
      children: [
        NavItem(id: 'documental', label: 'Motor Documental', icon: Icons.description_outlined, route: '/documents'),
        NavItem(id: 'aprobaciones', label: 'Flujos de Aprobación', icon: Icons.fact_check_outlined, route: '/approval-pending'),
        NavItem(id: 'administracion', label: 'Usuarios y Seguridad', icon: Icons.admin_panel_settings_outlined, route: '/admin/users'),
        NavItem(id: 'configuracion', label: 'Ajustes del Sistema', icon: Icons.settings_outlined, route: '/settings'),
        NavItem(id: 'auditoria', label: 'Logs de Auditoría', icon: Icons.history_outlined, route: '/audit-logs'),
        NavItem(id: 'herramientas', label: 'Herramientas Pro', icon: Icons.build_outlined, route: '/exchange-rates'),
      ],
    ),
  ];

  static List<NavModule> getModulesForRole(String role, {String? searchQuery}) {
    final bypass = role == 'SuperAdmin';

    final filtered = allModules.where((module) {
      if (!bypass && module.roles.isNotEmpty && !module.roles.contains(role)) {
        return false;
      }
      if (searchQuery != null && searchQuery.isNotEmpty) {
        final q = searchQuery.toLowerCase();
        final matchesModule = module.label.toLowerCase().contains(q);
        final matchesChild = module.children.any((item) =>
            item.label.toLowerCase().contains(q) &&
            (bypass || item.roles.isEmpty || item.roles.contains(role)));
        return matchesModule || matchesChild;
      }
      return true;
    }).map((module) {
      if (searchQuery != null && searchQuery.isNotEmpty) {
        final filteredItems = module.children.where((item) {
          final roleOk = bypass || item.roles.isEmpty || item.roles.contains(role);
          final searchOk = item.label.toLowerCase().contains(searchQuery.toLowerCase());
          return roleOk && searchOk;
        }).toList();
        return NavModule(id: module.id, label: module.label, icon: module.icon, children: filteredItems, roles: module.roles);
      }
      return module;
    }).where((m) => m.children.isNotEmpty).toList();

    return filtered;
  }
}
