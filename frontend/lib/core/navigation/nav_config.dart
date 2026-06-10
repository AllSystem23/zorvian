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
    // ── Inicio ──
    const NavModule(
      id: 'inicio',
      label: 'Inicio',
      icon: Icons.home_outlined,
      children: [
        NavItem(id: 'dashboard', label: 'Dashboard', icon: Icons.dashboard, route: '/dashboard'),
        NavItem(id: 'dashboard-v2', label: 'Dashboard V2', icon: Icons.dashboard_customize, route: '/dashboard-v2'),
        NavItem(id: 'ejecutivo', label: 'Panel Ejecutivo', icon: Icons.analytics, route: '/executive-dashboard', roles: ['SuperAdmin', 'CompanyAdmin']),
        NavItem(id: 'perfil', label: 'Perfil', icon: Icons.person, route: '/profile'),
        NavItem(id: 'calendario-ausencias', label: 'Calendario Ausencias', icon: Icons.calendar_month, route: '/absence-calendar'),
      ],
    ),

    // ── Asistencia ──
    const NavModule(
      id: 'asistencia',
      label: 'Asistencia',
      icon: Icons.schedule_outlined,
      children: [
        NavItem(id: 'marcaciones', label: 'Marcaciones', icon: Icons.access_time, route: '/attendance'),
        NavItem(id: 'historial-asistencia', label: 'Historial', icon: Icons.history, route: '/attendance/history'),
        NavItem(id: 'kiosko', label: 'Kiosko', icon: Icons.qr_code_scanner, route: '/attendance/kiosk'),
        NavItem(id: 'qr-checkin', label: 'QR Check-in', icon: Icons.qr_code, route: '/attendance/qr'),
      ],
    ),

    // ── Recursos Humanos ──
    const NavModule(
      id: 'rrhh',
      label: 'Recursos Humanos',
      icon: Icons.badge_outlined,
      children: [
        NavItem(id: 'empleados', label: 'Empleados', icon: Icons.people, route: '/employees'),
        NavItem(id: 'departamentos', label: 'Departamentos', icon: Icons.business, route: '/departments'),
        NavItem(id: 'vacaciones', label: 'Vacaciones', icon: Icons.beach_access, route: '/vacations'),
        NavItem(id: 'permisos', label: 'Permisos', icon: Icons.description, route: '/permissions'),
        NavItem(id: 'tipos-ausencia', label: 'Tipos de Ausencia', icon: Icons.bookmark, route: '/leave-types'),
      ],
    ),

    // ── Nómina ──
    const NavModule(
      id: 'nomina',
      label: 'Nómina',
      icon: Icons.receipt_long_outlined,
      roles: ['SuperAdmin', 'CompanyAdmin', 'Rrhh'],
      children: [
        NavItem(id: 'nomina-dashboard', label: 'Dashboard', icon: Icons.dashboard, route: '/payroll'),
        NavItem(id: 'periodos', label: 'Períodos', icon: Icons.date_range, route: '/payroll/periods'),
        NavItem(id: 'ejecuciones', label: 'Ejecuciones', icon: Icons.play_circle, route: '/payroll/runs'),
        NavItem(id: 'salarios', label: 'Salarios', icon: Icons.attach_money, route: '/payroll/salaries'),
        NavItem(id: 'deducciones', label: 'Tipos Deducción', icon: Icons.remove_circle, route: '/payroll/deduction-types'),
        NavItem(id: 'liquidaciones', label: 'Liquidaciones', icon: Icons.money_off, route: '/payroll/settlement'),
      ],
    ),

    // ── Comercial ──
    const NavModule(
      id: 'comercial',
      label: 'Comercial',
      icon: Icons.shopping_cart_outlined,
      children: [
        NavItem(id: 'clientes', label: 'Clientes', icon: Icons.people, route: '/clients'),
        NavItem(id: 'ventas', label: 'Ventas', icon: Icons.point_of_sale, route: '/sales'),
        NavItem(id: 'cotizaciones', label: 'Cotizaciones', icon: Icons.description, route: '/quotes'),
        NavItem(id: 'notas-credito', label: 'Notas de Crédito', icon: Icons.post_add, route: '/credit-notes'),
      ],
    ),

    // ── Inventario ──
    const NavModule(
      id: 'inventario',
      label: 'Inventario',
      icon: Icons.inventory_2_outlined,
      children: [
        NavItem(id: 'productos', label: 'Productos', icon: Icons.inventory_2, route: '/products'),
        NavItem(id: 'categorias', label: 'Categorías', icon: Icons.category, route: '/categories'),
        NavItem(id: 'marcas', label: 'Marcas', icon: Icons.branding_watermark, route: '/brands'),
        NavItem(id: 'movimientos', label: 'Movimientos', icon: Icons.swap_horiz, route: '/inventory-movements'),
        NavItem(id: 'ajuste-inventario', label: 'Ajuste Inventario', icon: Icons.tune, route: '/inventory-adjustment'),
      ],
    ),

    // ── Compras ──
    const NavModule(
      id: 'compras',
      label: 'Compras',
      icon: Icons.shopping_bag_outlined,
      children: [
        NavItem(id: 'proveedores', label: 'Proveedores', icon: Icons.local_shipping, route: '/suppliers'),
        NavItem(id: 'ordenes-compra', label: 'Órdenes de Compra', icon: Icons.receipt, route: '/purchases'),
      ],
    ),

    // ── Créditos ──
    const NavModule(
      id: 'creditos',
      label: 'Créditos',
      icon: Icons.credit_card_outlined,
      children: [
        NavItem(id: 'cartera-creditos', label: 'Cartera de Créditos', icon: Icons.account_balance_wallet, route: '/credits', badgeRef: 'credits-pending'),
        NavItem(id: 'mora', label: 'Dashboard de Mora', icon: Icons.warning_amber, route: '/credits/overdue-dashboard', badgeRef: 'overdue-credits'),
      ],
    ),

    // ── Caja ──
    const NavModule(
      id: 'caja',
      label: 'Caja',
      icon: Icons.monetization_on_outlined,
      children: [
        NavItem(id: 'movimientos-caja', label: 'Movimientos de Caja', icon: Icons.account_balance, route: '/cash-registers'),
      ],
    ),

    // ── Tesorería ──
    const NavModule(
      id: 'tesoreria',
      label: 'Tesorería',
      icon: Icons.account_balance_outlined,
      roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
      children: [
        NavItem(id: 'cheques', label: 'Emitir Cheque', icon: Icons.payments, route: '/treasury/checks'),
        NavItem(id: 'transferencias', label: 'Transferencias', icon: Icons.swap_horiz, route: '/treasury/transfers'),
        NavItem(id: 'depositos', label: 'Depósitos', icon: Icons.account_balance, route: '/treasury/deposits'),
        NavItem(id: 'comisiones', label: 'Comisiones Bancarias', icon: Icons.monetization_on, route: '/treasury/commissions'),
        NavItem(id: 'cobranza-bancaria', label: 'Cobranza Bancaria', icon: Icons.payments, route: '/treasury/collections'),
      ],
    ),

    // ── Garantías ──
    const NavModule(
      id: 'garantias',
      label: 'Garantías',
      icon: Icons.verified_outlined,
      children: [
        NavItem(id: 'gestion-garantias', label: 'Gestión de Garantías', icon: Icons.assignment, route: '/warranties', badgeRef: 'warranties-pending'),
      ],
    ),

    // ── Sucursales ──
    const NavModule(
      id: 'sucursales',
      label: 'Sucursales',
      icon: Icons.store_mall_directory_outlined,
      children: [
        NavItem(id: 'sucursales-lista', label: 'Sucursales', icon: Icons.store_mall_directory, route: '/branches'),
      ],
    ),

    // ── Finanzas ──
    const NavModule(
      id: 'finanzas',
      label: 'Finanzas',
      icon: Icons.account_balance_outlined,
      roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
      children: [
        NavItem(id: 'centros-costos', label: 'Centros de Costo', icon: Icons.account_tree, route: '/cost-centers'),
        NavItem(id: 'presupuestos', label: 'Presupuestos', icon: Icons.account_balance, route: '/budgets'),
        NavItem(id: 'presupuesto-vs-real', label: 'Presupuesto vs Real', icon: Icons.compare_arrows, route: '/budgets/vs-actual'),
        NavItem(id: 'informe-equity', label: 'Cambios en Patrimonio', icon: Icons.show_chart, route: '/accounting/reports/equity'),
        NavItem(id: 'informe-comparativo', label: 'Informes Comparativos', icon: Icons.bar_chart, route: '/accounting/reports/comparative'),
        NavItem(id: 'asistente-contable', label: 'Asistente Contable IA', icon: Icons.smart_toy, route: '/accounting/ai-assistant'),
        NavItem(id: 'balance-prueba', label: 'Balance de Prueba', icon: Icons.balance, route: '/accounting/trial-balance'),
        NavItem(id: 'estado-resultados', label: 'Estado de Resultados', icon: Icons.trending_up, route: '/accounting/income-statement'),
        NavItem(id: 'catalogo-cuentas', label: 'Catálogo de Cuentas', icon: Icons.list_alt, route: '/accounting/chart-of-accounts'),
        NavItem(id: 'periodos-contables', label: 'Períodos Contables', icon: Icons.date_range, route: '/accounting/periods'),
        NavItem(id: 'asientos-contables', label: 'Asientos Contables', icon: Icons.description, route: '/accounting/entries'),
        NavItem(id: 'enlace-cuentas', label: 'Enlace de Cuentas', icon: Icons.link, route: '/accounting/account-links'),
      ],
    ),

    // ── Inteligencia de Negocios ──
    const NavModule(
      id: 'bi',
      label: 'Inteligencia de Negocios',
      icon: Icons.insights_outlined,
      roles: ['SuperAdmin', 'CompanyAdmin'],
      children: [
        NavItem(id: 'bi-ejecutivo', label: 'Panel Ejecutivo', icon: Icons.dashboard, route: '/bi/executive'),
        NavItem(id: 'bi-financiero', label: 'Panel Financiero', icon: Icons.account_balance, route: '/bi/financial'),
        NavItem(id: 'bi-comercial', label: 'Panel Comercial', icon: Icons.trending_up, route: '/bi/commercial'),
        NavItem(id: 'bi-operativo', label: 'Panel Operativo', icon: Icons.precision_manufacturing, route: '/bi/operational'),
      ],
    ),

    // ── Administración ──
    const NavModule(
      id: 'admin',
      label: 'Administración',
      icon: Icons.admin_panel_settings_outlined,
      roles: ['SuperAdmin', 'CompanyAdmin'],
      children: [
        NavItem(id: 'usuarios', label: 'Usuarios', icon: Icons.person_add, route: '/admin/users'),
        NavItem(id: 'invitar-usuario', label: 'Invitar Usuario', icon: Icons.mail, route: '/admin/invite'),
        NavItem(id: 'flujos-aprobacion', label: 'Flujos Aprobación', icon: Icons.approval, route: '/approval-flows'),
        NavItem(id: 'pendientes-aprob', label: 'Pendientes Aprob.', icon: Icons.pending_actions, route: '/approval-pending', badgeRef: 'approvals-pending'),
        NavItem(id: 'reportes', label: 'Reportes', icon: Icons.assessment, route: '/reports'),
        NavItem(id: 'auditoria', label: 'Auditoría', icon: Icons.history, route: '/audit-logs'),
        NavItem(id: 'configuracion', label: 'Configuración', icon: Icons.settings, route: '/settings'),
      ],
    ),

    // ── Herramientas ──
    const NavModule(
      id: 'herramientas',
      label: 'Herramientas',
      icon: Icons.build_outlined,
      roles: ['SuperAdmin', 'CompanyAdmin'],
      children: [
        NavItem(id: 'tasas-cambio', label: 'Tasas de Cambio', icon: Icons.currency_exchange, route: '/exchange-rates'),
        NavItem(id: 'reportes-personalizados', label: 'Reportes Personalizados', icon: Icons.description, route: '/custom-reports'),
        NavItem(id: 'webhooks', label: 'Webhooks', icon: Icons.webhook, route: '/webhooks'),
      ],
    ),

    // ── Comunicación ──
    const NavModule(
      id: 'comunicacion',
      label: 'Comunicación',
      icon: Icons.forum_outlined,
      children: [
        NavItem(id: 'chat', label: 'Chat', icon: Icons.chat, route: '/chat'),
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
