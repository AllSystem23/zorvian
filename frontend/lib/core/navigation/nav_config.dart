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
    // ── INICIO ──
    const NavModule(
      id: 'inicio',
      label: 'Inicio',
      icon: Icons.home_outlined,
      children: [
        NavItem(id: 'dashboard', label: 'Dashboard General', icon: Icons.dashboard_outlined, route: '/dashboard'),
        NavItem(id: 'ejecutivo', label: 'Panel Ejecutivo', icon: Icons.analytics_outlined, route: '/executive-dashboard', roles: ['SuperAdmin', 'CompanyAdmin']),
        NavItem(id: 'bi-central', label: 'Inteligencia de Negocios', icon: Icons.insights_outlined, route: '/bi/executive', roles: ['SuperAdmin', 'CompanyAdmin']),
        NavItem(id: 'calendario', label: 'Calendario de Ausencias', icon: Icons.calendar_month_outlined, route: '/absence-calendar'),
        NavItem(id: 'perfil', label: 'Mi Perfil', icon: Icons.person_outline, route: '/profile'),
      ],
    ),

    // ── VENTAS ──
    const NavModule(
      id: 'ventas',
      label: 'Ventas',
      icon: Icons.point_of_sale_outlined,
      children: [
        NavItem(id: 'cotizaciones', label: 'Cotizaciones', icon: Icons.request_quote_outlined, route: '/quotes'),
        NavItem(id: 'comercial', label: 'Facturación / Ventas', icon: Icons.receipt_long_outlined, route: '/sales'),
        NavItem(id: 'creditos', label: 'Créditos y Cobros', icon: Icons.credit_card_outlined, route: '/credits'),
        NavItem(id: 'creditos-vencidos', label: 'Dashboard Vencimientos', icon: Icons.warning_amber_outlined, route: '/credits/overdue-dashboard', roles: ['SuperAdmin', 'CompanyAdmin', 'Supervisor']),
        NavItem(id: 'clientes', label: 'Cartera de Clientes', icon: Icons.people_alt_outlined, route: '/clients'),
        NavItem(id: 'notas-credito', label: 'Notas de Crédito', icon: Icons.description_outlined, route: '/credit-notes'),
        NavItem(id: 'pos', label: 'Punto de Venta', icon: Icons.point_of_sale_outlined, route: '/pos'),
        NavItem(id: 'crm', label: 'CRM', icon: Icons.handshake_outlined, route: '/crm'),
      ],
    ),

    // ── INVENTARIO ──
    const NavModule(
      id: 'inventario',
      label: 'Inventario',
      icon: Icons.inventory_2_outlined,
      children: [
        NavItem(id: 'productos', label: 'Productos', icon: Icons.shopping_bag_outlined, route: '/products'),
        NavItem(id: 'categorias', label: 'Categorías', icon: Icons.category_outlined, route: '/categories'),
        NavItem(id: 'marcas', label: 'Marcas', icon: Icons.branding_watermark_outlined, route: '/brands'),
        NavItem(id: 'movimientos-inv', label: 'Movimientos de Inventario', icon: Icons.swap_horiz_outlined, route: '/inventory-movements'),
        NavItem(id: 'ajustes-inv', label: 'Ajustes de Inventario', icon: Icons.tune_outlined, route: '/inventory-adjustment'),
        NavItem(id: 'garantias', label: 'Garantías', icon: Icons.verified_user_outlined, route: '/warranties'),
      ],
    ),

    // ── COMPRAS ──
    const NavModule(
      id: 'compras',
      label: 'Compras',
      icon: Icons.shopping_cart_outlined,
      children: [
        NavItem(id: 'compras-ordenes', label: 'Órdenes de Compra', icon: Icons.shopping_bag_outlined, route: '/purchases'),
        NavItem(id: 'proveedores', label: 'Proveedores', icon: Icons.local_shipping_outlined, route: '/suppliers'),
      ],
    ),

    // ── FINANZAS ──
    const NavModule(
      id: 'finanzas',
      label: 'Finanzas',
      icon: Icons.account_balance_outlined,
      children: [
        NavItem(id: 'caja', label: 'Caja', icon: Icons.monetization_on_outlined, route: '/cash-registers'),
        NavItem(id: 'tesoreria', label: 'Tesorería y Bancos', icon: Icons.savings_outlined, route: '/treasury/checks', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
        NavItem(id: 'contabilidad', label: 'Contabilidad', icon: Icons.balance_outlined, route: '/accounting/trial-balance', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
        NavItem(id: 'catalogo-cuentas', label: 'Catálogo de Cuentas', icon: Icons.account_tree_outlined, route: '/accounting/chart-of-accounts', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
        NavItem(id: 'tipo-cambio', label: 'Tipo de Cambio', icon: Icons.currency_exchange_outlined, route: '/exchange-rates'),
        NavItem(id: 'presupuestos', label: 'Presupuestos', icon: Icons.savings_outlined, route: '/budgets', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
        NavItem(id: 'centros-costo', label: 'Centros de Costo', icon: Icons.pie_chart_outline, route: '/cost-centers', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
      ],
    ),

    // ── TALENTO HUMANO ──
    const NavModule(
      id: 'talento',
      label: 'Talento Humano',
      icon: Icons.diversity_3_outlined,
      children: [
        NavItem(id: 'empleados', label: 'Capital Humano', icon: Icons.people_outline, route: '/employees'),
        NavItem(id: 'asistencia', label: 'Reloj y Asistencia', icon: Icons.schedule_outlined, route: '/attendance'),
        NavItem(id: 'nomina', label: 'Gestión de Nómina', icon: Icons.receipt_long_outlined, route: '/payroll', roles: ['SuperAdmin', 'CompanyAdmin', 'Rrhh']),
        NavItem(id: 'prestadores', label: 'Prestadores Externos', icon: Icons.business_center_outlined, route: '/providers'),
        NavItem(id: 'vacaciones', label: 'Ausencias y Vacaciones', icon: Icons.beach_access_outlined, route: '/vacations'),
        NavItem(id: 'permisos', label: 'Permisos', icon: Icons.event_available_outlined, route: '/permissions'),
        NavItem(id: 'metas', label: 'Metas e Incentivos', icon: Icons.emoji_events_outlined, route: '/goals/dashboard'),
      ],
    ),

    // ── BI E INTELIGENCIA ──
    const NavModule(
      id: 'bi',
      label: 'BI e Inteligencia',
      icon: Icons.insights_outlined,
      roles: ['SuperAdmin', 'CompanyAdmin'],
      children: [
        NavItem(id: 'asistente-ia', label: 'Asistente Z-IA', icon: Icons.smart_toy_outlined, route: '/accounting/ai-assistant'),
        NavItem(id: 'bi-financiero', label: 'Dashboard Financiero', icon: Icons.candlestick_chart_outlined, route: '/bi/financial'),
        NavItem(id: 'bi-comercial', label: 'Dashboard Comercial', icon: Icons.trending_up_outlined, route: '/bi/commercial'),
        NavItem(id: 'bi-operacional', label: 'Dashboard Operacional', icon: Icons.speed_outlined, route: '/bi/operational'),
        NavItem(id: 'reportes-custom', label: 'Reportes Personalizados', icon: Icons.analytics_outlined, route: '/custom-reports'),
      ],
    ),

    // ── COMUNICACIÓN ──
    const NavModule(
      id: 'comunicacion',
      label: 'Comunicación',
      icon: Icons.forum_outlined,
      children: [
        NavItem(id: 'chat', label: 'Centro de Comunicación', icon: Icons.chat_bubble_outline, route: '/chat'),
      ],
    ),

    // ── ADMINISTRACIÓN ──
    const NavModule(
      id: 'administracion',
      label: 'Administración',
      icon: Icons.admin_panel_settings_outlined,
      roles: ['SuperAdmin', 'CompanyAdmin'],
      children: [
        NavItem(id: 'usuarios', label: 'Usuarios y Seguridad', icon: Icons.manage_accounts_outlined, route: '/admin/users'),
        NavItem(id: 'sucursales', label: 'Sucursales', icon: Icons.store_mall_directory_outlined, route: '/branches'),
        NavItem(id: 'documental', label: 'Motor Documental', icon: Icons.description_outlined, route: '/documents'),
        NavItem(id: 'aprobaciones', label: 'Flujos de Aprobación', icon: Icons.fact_check_outlined, route: '/approval-pending'),
        NavItem(id: 'webhooks', label: 'Webhooks', icon: Icons.webhook_outlined, route: '/webhooks'),
        NavItem(id: 'configuracion', label: 'Ajustes del Sistema', icon: Icons.settings_outlined, route: '/settings'),
        NavItem(id: 'auditoria', label: 'Logs de Auditoría', icon: Icons.history_outlined, route: '/audit-logs'),
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
