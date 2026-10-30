import 'package:flutter/material.dart';
import '../../shared/ds/ds.dart';

final class NavItem {
  final String id;
  final String label;
  final IconData icon;
  final String route;
  final List<String> roles;
  final String badgeRef;
  final bool selectedExact;

  const NavItem({
    required this.id,
    required this.label,
    required this.icon,
    required this.route,
    this.roles = const [],
    this.badgeRef = '',
    this.selectedExact = false,
  });
}

final class NavModule {
  final String id;
  final String label;
  final IconData icon;
  final Color color;
  final Color textColor; // WCAG AA compliant text variant for light mode
  final String group; // Operations, Financial, Talent, Intelligence, Admin
  final List<NavItem> children;
  final List<String> roles;
  final String module; // module entitlement key (ModuleType name del backend)

  const NavModule({
    required this.id,
    required this.label,
    required this.icon,
    required this.color,
    this.textColor = const Color(0xFF484854), // default: neutral600
    required this.group,
    required this.children,
    this.roles = const [],
    this.module = '',
  });
}

final class NavConfig {
  NavConfig._();

  static const Map<String, String> groupLabels = {
    'operations': 'OPERACIONES',
    'financial': 'FINANCIERO',
    'talent': 'TALENTO',
    'intelligence': 'INTELIGENCIA',
    'admin': 'CONFIGURACIÓN',
  };

  static const Map<String, Color> groupColors = {
    'operations': ZColors.moduleSales,
    'financial': ZColors.moduleFinance,
    'talent': ZColors.moduleHr,
    'intelligence': ZColors.moduleIa,
    'admin': ZColors.moduleAdmin,
  };

  /// Darkened group colors for light-mode text (WCAG AA ≥4.5:1 on white)
  static const Map<String, Color> groupTextColors = {
    'operations': ZColors.moduleSalesText,
    'financial': ZColors.moduleFinanceText,
    'talent': ZColors.moduleHrText,
    'intelligence': ZColors.moduleIaText,
    'admin': ZColors.moduleAdminText,
  };

  static Color colorForModule(String moduleId) {
    return allModules.firstWhere((m) => m.id == moduleId, orElse: () => allModules.first).color;
  }

  static final List<NavModule> allModules = [
    // ── OPERACIONES ──
    const NavModule(
      id: 'ventas',
      module: 'Sales',
      label: 'Ventas',
      icon: Icons.point_of_sale_outlined,
      color: ZColors.moduleSales,
      textColor: ZColors.moduleSalesText,
      group: 'operations',
      children: [
        NavItem(id: 'cotizaciones', label: 'Cotizaciones', icon: Icons.request_quote_outlined, route: '/quotes'),
        NavItem(id: 'comercial', label: 'Facturación / Ventas', icon: Icons.receipt_long_outlined, route: '/sales'),
        NavItem(id: 'creditos', label: 'Créditos y Cobros', icon: Icons.credit_card_outlined, route: '/credits'),
        NavItem(id: 'creditos-vencidos', label: 'Dashboard Vencimientos', icon: Icons.warning_amber_outlined, route: '/credits/overdue-dashboard', roles: ['SuperAdmin', 'CompanyAdmin', 'Supervisor']),
        NavItem(id: 'clientes', label: 'Cartera de Clientes', icon: Icons.people_alt_outlined, route: '/clients'),
        NavItem(id: 'notas-credito', label: 'Notas de Crédito', icon: Icons.request_quote_outlined, route: '/credit-notes'),
        NavItem(id: 'pos', label: 'Punto de Venta', icon: Icons.storefront_outlined, route: '/pos'),
        NavItem(id: 'crm', label: 'CRM', icon: Icons.handshake_outlined, route: '/crm'),
      ],
    ),

    const NavModule(
      id: 'inventario',
      module: 'Inventory',
      label: 'Inventario',
      icon: Icons.inventory_2_outlined,
      color: ZColors.moduleInventory,
      textColor: ZColors.moduleInventoryText,
      group: 'operations',
      children: [
        NavItem(id: 'productos', label: 'Productos', icon: Icons.shopping_bag_outlined, route: '/products'),
        NavItem(id: 'categorias', label: 'Categorías', icon: Icons.category_outlined, route: '/categories'),
        NavItem(id: 'marcas', label: 'Marcas', icon: Icons.branding_watermark_outlined, route: '/brands'),
        NavItem(id: 'movimientos-inv', label: 'Movimientos de Inventario', icon: Icons.swap_horiz_outlined, route: '/inventory-movements'),
        NavItem(id: 'transferencias-inv', label: 'Transferencias', icon: Icons.swap_horizontal_circle_outlined, route: '/stock-transfers'),
        NavItem(id: 'bodegas-inv', label: 'Bodegas', icon: Icons.warehouse_outlined, route: '/warehouses'),
        NavItem(id: 'ajustes-inv', label: 'Ajustes de Inventario', icon: Icons.tune_outlined, route: '/inventory-adjustment'),
        NavItem(id: 'garantias', label: 'Garantías', icon: Icons.shield_outlined, route: '/warranties'),
        NavItem(id: 'talleres', label: 'Talleres', icon: Icons.build_outlined, route: '/workshops'),
      ],
    ),

    const NavModule(
      id: 'compras',
      module: 'Purchases',
      label: 'Compras',
      icon: Icons.shopping_cart_outlined,
      color: ZColors.modulePurchases,
      textColor: ZColors.modulePurchasesText,
      group: 'operations',
      children: [
        NavItem(id: 'facturas-compra', label: 'Facturas de Compra', icon: Icons.receipt_long_outlined, route: '/purchases'),
        NavItem(id: 'compras-ordenes', label: 'Órdenes de Compra', icon: Icons.receipt_outlined, route: '/purchase-orders'),
        NavItem(id: 'compras-solicitudes', label: 'Solicitudes de Cotización', icon: Icons.difference_outlined, route: '/purchase-requests'),
        NavItem(id: 'proveedores', label: 'Proveedores', icon: Icons.factory_outlined, route: '/suppliers'),
        NavItem(id: 'pagos-proveedores', label: 'Pagos a Proveedores', icon: Icons.payments_outlined, route: '/purchases/payments'),
        NavItem(id: 'notas-credito-proveedores', label: 'Notas de Crédito a Proveedores', icon: Icons.request_quote_outlined, route: '/purchases/credit-notes'),
      ],
    ),

    const NavModule(
      id: 'flota',
      module: 'Fleet',
      label: 'Flota y Logística',
      icon: Icons.local_shipping_outlined,
      color: ZColors.moduleFleet,
      textColor: ZColors.moduleFleetText,
      group: 'operations',
      children: [
        NavItem(id: 'flota-dashboard', label: 'Dashboard', icon: Icons.dashboard_outlined, route: '/fleet', selectedExact: true),
        NavItem(id: 'flota-vehiculos', label: 'Vehículos', icon: Icons.directions_car_outlined, route: '/fleet/vehicles'),
        NavItem(id: 'flota-conductores', label: 'Conductores', icon: Icons.person_outline, route: '/fleet/drivers'),
        NavItem(id: 'flota-rutas', label: 'Rutas', icon: Icons.alt_route_outlined, route: '/fleet/routes'),
        NavItem(id: 'flota-entregas', label: 'Entregas', icon: Icons.local_shipping_outlined, route: '/fleet/deliveries'),
        NavItem(id: 'flota-viajes', label: 'Viajes', icon: Icons.flight_takeoff_outlined, route: '/fleet/trips'),
        NavItem(id: 'flota-combustible', label: 'Combustible', icon: Icons.local_gas_station_outlined, route: '/fleet/fuel'),
        NavItem(id: 'flota-mantenimientos', label: 'Mantenimientos', icon: Icons.build_outlined, route: '/fleet/maintenance'),
        NavItem(id: 'flota-taller', label: 'Taller', icon: Icons.precision_manufacturing_outlined, route: '/fleet/workshop'),
        NavItem(id: 'flota-documentos', label: 'Documentos', icon: Icons.folder_outlined, route: '/fleet/documents'),
        NavItem(id: 'flota-gastos', label: 'Gastos', icon: Icons.account_balance_wallet_outlined, route: '/fleet/expenses'),
        NavItem(id: 'flota-gps', label: 'GPS en Tiempo Real', icon: Icons.gps_fixed_outlined, route: '/fleet/gps'),
        NavItem(id: 'flota-alertas', label: 'Alertas', icon: Icons.notifications_active_outlined, route: '/fleet/alerts'),
        NavItem(id: 'flota-tracking', label: 'Tracking Entregas', icon: Icons.route_outlined, route: '/fleet/tracking'),
        NavItem(id: 'flota-predictive', label: 'IA Predictiva', icon: Icons.psychology_outlined, route: '/fleet/predictive'),
        NavItem(id: 'flota-catalogos', label: 'Catálogos', icon: Icons.list_alt_outlined, route: '/fleet/catalogs', roles: ['SuperAdmin', 'CompanyAdmin']),
        NavItem(id: 'flota-reportes', label: 'Reportes', icon: Icons.summarize_outlined, route: '/fleet/reports'),
      ],
    ),

    // ── PalmTrack: módulo independiente de integración externa ──
    const NavModule(
      id: 'palmtrack',
      module: 'PalmTrack',
      label: 'PalmTrack',
      icon: Icons.agriculture_outlined,
      color: ZColors.moduleFleet,
      textColor: ZColors.moduleFleetText,
      group: 'operations',
      roles: ['SuperAdmin', 'CompanyAdmin'],
      children: [
        NavItem(id: 'palmtrack-production', label: 'Producción', icon: Icons.agriculture_outlined, route: '/palmtrack/production', selectedExact: true),
        NavItem(id: 'palmtrack-farms', label: 'Fincas', icon: Icons.landscape_outlined, route: '/palmtrack/farms'),
        NavItem(id: 'palmtrack-lots', label: 'Lotes', icon: Icons.grid_view_outlined, route: '/palmtrack/lots'),
        NavItem(id: 'palmtrack-producers', label: 'Productores', icon: Icons.people_outline, route: '/palmtrack/producers'),
        NavItem(id: 'palmtrack-sales', label: 'Ventas', icon: Icons.receipt_long_outlined, route: '/palmtrack/sales'),
        NavItem(id: 'palmtrack-inventory', label: 'Inventario', icon: Icons.inventory_2_outlined, route: '/palmtrack/inventory'),
        NavItem(id: 'palmtrack-sync', label: 'Sincronización', icon: Icons.sync_outlined, route: '/palmtrack/sync'),
        NavItem(id: 'palmtrack-conflicts', label: 'Conflictos', icon: Icons.sync_problem_outlined, route: '/palmtrack/conflicts'),
        NavItem(id: 'palmtrack-settings', label: 'Configuración', icon: Icons.settings_outlined, route: '/palmtrack/settings', roles: ['SuperAdmin']),
      ],
    ),

    // ── FINANCIERO ──
    const NavModule(
      id: 'finanzas',
      module: 'Finance',
      label: 'Finanzas',
      icon: Icons.account_balance_outlined,
      color: ZColors.moduleFinance,
      textColor: ZColors.moduleFinanceText,
      group: 'financial',
      children: [
        NavItem(id: 'caja', label: 'Caja', icon: Icons.monetization_on_outlined, route: '/cash-registers'),
        NavItem(id: 'tesoreria', label: 'Tesorería y Bancos', icon: Icons.savings_outlined, route: '/treasury/checks', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
        NavItem(id: 'contabilidad', label: 'Contabilidad', icon: Icons.balance_outlined, route: '/accounting/trial-balance', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
        NavItem(id: 'catalogo-cuentas', label: 'Catálogo de Cuentas', icon: Icons.account_tree_outlined, route: '/accounting/chart-of-accounts', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
        NavItem(id: 'tipo-cambio', label: 'Tipo de Cambio', icon: Icons.currency_exchange_outlined, route: '/exchange-rates'),
        NavItem(id: 'presupuestos', label: 'Presupuestos', icon: Icons.account_balance_wallet_outlined, route: '/budgets', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
        NavItem(id: 'presupuesto-vs-real', label: 'Presupuesto vs Real', icon: Icons.compare_arrows_outlined, route: '/budgets/vs-actual', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
        NavItem(id: 'conciliaciones', label: 'Conciliaciones Bancarias', icon: Icons.compare_arrows_outlined, route: '/reconciliations', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
        NavItem(id: 'centros-costo', label: 'Centros de Costo', icon: Icons.pie_chart_outline, route: '/cost-centers', roles: ['SuperAdmin', 'CompanyAdmin', 'Accountant']),
      ],
    ),

    // ── TALENTO ──
    const NavModule(
      id: 'talento',
      module: 'Hr',
      label: 'Talento Humano',
      icon: Icons.diversity_3_outlined,
      color: ZColors.moduleHr,
      textColor: ZColors.moduleHrText,
      group: 'talent',
      children: [
        NavItem(id: 'empleados', label: 'Capital Humano', icon: Icons.people_outline, route: '/employees'),
        NavItem(id: 'departamentos', label: 'Departamentos', icon: Icons.business_outlined, route: '/departments', roles: ['SuperAdmin', 'CompanyAdmin', 'Rrhh']),
        NavItem(id: 'asistencia', label: 'Reloj y Asistencia', icon: Icons.schedule_outlined, route: '/attendance'),
        NavItem(id: 'nomina', label: 'Gestión de Nómina', icon: Icons.receipt_long_outlined, route: '/payroll', roles: ['SuperAdmin', 'CompanyAdmin', 'Rrhh']),
        NavItem(id: 'prestadores', label: 'Prestadores Externos', icon: Icons.business_center_outlined, route: '/providers'),
        NavItem(id: 'prestadores-dashboard', label: 'Dashboard Prestadores', icon: Icons.dashboard_outlined, route: '/providers/dashboard'),
        NavItem(id: 'vacaciones', label: 'Ausencias y Vacaciones', icon: Icons.event_busy_outlined, route: '/vacations'),
        NavItem(id: 'recibos-vacaciones', label: 'Recibos de Vacaciones', icon: Icons.receipt_long_outlined, route: '/vacations/receipts', roles: ['SuperAdmin', 'CompanyAdmin', 'Rrhh']),
        NavItem(id: 'pagos-prestadores', label: 'Pagos a Prestadores', icon: Icons.handshake_outlined, route: '/service-payments', roles: ['SuperAdmin', 'CompanyAdmin', 'Rrhh']),
        NavItem(id: 'constancias', label: 'Solicitudes de Constancia', icon: Icons.description_outlined, route: '/certificate-requests', roles: ['SuperAdmin', 'CompanyAdmin', 'Rrhh']),
        NavItem(id: 'permisos', label: 'Permisos', icon: Icons.event_available_outlined, route: '/permissions'),
        NavItem(id: 'metas', label: 'Metas e Incentivos', icon: Icons.emoji_events_outlined, route: '/goals/dashboard'),
      ],
    ),

    // ── INTELIGENCIA ──
    const NavModule(
      id: 'bi',
      module: 'Bi',
      label: 'BI e Inteligencia',
      icon: Icons.insights_outlined,
      color: ZColors.moduleIa,
      textColor: ZColors.moduleIaText,
      group: 'intelligence',
      roles: ['SuperAdmin', 'CompanyAdmin'],
      children: [
        NavItem(id: 'asistente-ia', label: 'Asistente Z-IA', icon: Icons.smart_toy_outlined, route: '/accounting/ai-assistant'),
        NavItem(id: 'bi-financiero', label: 'Dashboard Financiero', icon: Icons.candlestick_chart_outlined, route: '/bi/financial'),
        NavItem(id: 'bi-comercial', label: 'Dashboard Comercial', icon: Icons.trending_up_outlined, route: '/bi/commercial'),
        NavItem(id: 'bi-operacional', label: 'Dashboard Operacional', icon: Icons.speed_outlined, route: '/bi/operational'),
        NavItem(id: 'reportes-custom', label: 'Reportes Personalizados', icon: Icons.analytics_outlined, route: '/custom-reports'),
      ],
    ),

    const NavModule(
      id: 'comunicacion',
      module: 'Communication',
      label: 'Comunicación',
      icon: Icons.forum_outlined,
      color: ZColors.moduleCrm,
      textColor: ZColors.moduleCrmText,
      group: 'intelligence',
      children: [
        NavItem(id: 'chat', label: 'Centro de Comunicación', icon: Icons.chat_bubble_outline, route: '/chat'),
      ],
    ),

    // ── CONFIGURACIÓN ──
    const NavModule(
      id: 'administracion',
      module: 'Administration',
      label: 'Administración',
      icon: Icons.admin_panel_settings_outlined,
      color: ZColors.moduleAdmin,
      textColor: ZColors.moduleAdminText,
      group: 'admin',
      roles: ['SuperAdmin', 'CompanyAdmin'],
      children: [
        NavItem(id: 'empresas', label: 'Gestión de Empresas', icon: Icons.business_outlined, route: '/admin/companies', roles: ['SuperAdmin']),
        NavItem(id: 'planes', label: 'Planes de Suscripción', icon: Icons.workspace_premium_outlined, route: '/admin/subscription-plans', roles: ['SuperAdmin']),
        NavItem(id: 'usuarios', label: 'Usuarios y Seguridad', icon: Icons.manage_accounts_outlined, route: '/admin/users'),
        NavItem(id: 'roles-permisos', label: 'Roles y Permisos', icon: Icons.security_outlined, route: '/admin/roles', roles: ['SuperAdmin', 'CompanyAdmin']),
        NavItem(id: 'invitar', label: 'Invitar Usuario', icon: Icons.person_add_outlined, route: '/admin/invite', roles: ['SuperAdmin']),
        NavItem(id: 'invitaciones-historial', label: 'Historial Invitaciones', icon: Icons.history_outlined, route: '/admin/invitations-history', roles: ['SuperAdmin']),
        NavItem(id: 'solicitudes-acceso', label: 'Solicitudes de Acceso', icon: Icons.how_to_reg_outlined, route: '/admin/access-requests', roles: ['SuperAdmin']),
        NavItem(id: 'db-storage', label: 'Almacenamiento BD', icon: Icons.storage_outlined, route: '/admin/db-storage', roles: ['SuperAdmin']),
        NavItem(id: 'sucursales', label: 'Sucursales', icon: Icons.store_outlined, route: '/branches'),
        NavItem(id: 'documental', label: 'Motor Documental', icon: Icons.auto_stories_outlined, route: '/documents'),
        NavItem(id: 'aprobaciones', label: 'Flujos de Aprobación', icon: Icons.fact_check_outlined, route: '/approval-pending'),
        NavItem(id: 'webhooks', label: 'Webhooks', icon: Icons.webhook_outlined, route: '/webhooks'),
        NavItem(id: 'configuracion', label: 'Ajustes del Sistema', icon: Icons.settings_outlined, route: '/settings'),
        NavItem(id: 'auditoria', label: 'Logs de Auditoría', icon: Icons.policy_outlined, route: '/audit-logs'),
      ],
    ),

    const NavModule(
      id: 'fiscal',
      module: 'Tax',
      label: 'Configuración Fiscal',
      icon: Icons.receipt_long_outlined,
      color: ZColors.moduleFinance,
      textColor: ZColors.moduleFinanceText,
      group: 'admin',
      roles: ['SuperAdmin', 'CompanyAdmin'],
      children: [
        NavItem(id: 'country-tax', label: 'Config Fiscal por País', icon: Icons.policy_outlined, route: '/admin/country-tax-configs', roles: ['SuperAdmin', 'CompanyAdmin']),
        NavItem(id: 'regional-tax', label: 'Tasas Fiscales Regionales', icon: Icons.receipt_long_outlined, route: '/admin/regional-tax-configs', roles: ['SuperAdmin', 'CompanyAdmin']),
      ],
    ),
  ];

  /// Permiso granular de lectura requerido por ítem de navegación (id → código
  /// del catálogo `PermissionCatalog`). Los ítems no listados NO se filtran por
  /// permiso (solo rol + plan), lo que permite migrar incrementalmente.
  /// Mapeo derivado de los `RequirePermission` reales de cada controller.
  static const Map<String, String> itemPermissions = {
    // Ventas
    'cotizaciones': 'sale.read',
    'comercial': 'sale.read',
    'creditos': 'credit.read',
    'creditos-vencidos': 'credit.read',
    'clientes': 'client.read',
    'notas-credito': 'credit.read',
    'pos': 'sale.read',
    'crm': 'sale.read',
    // Inventario
    'productos': 'inventory.read',
    'categorias': 'inventory.read',
    'marcas': 'inventory.read',
    'movimientos-inv': 'inventory.read',
    'transferencias-inv': 'inventory.read',
    'bodegas-inv': 'inventory.read',
    'ajustes-inv': 'inventory.read',
    'garantias': 'warranty.read',
    'talleres': 'fleet.read',
    // Compras
    'facturas-compra': 'purchase.read',
    'compras-ordenes': 'purchase.read',
    'compras-solicitudes': 'purchase.read',
    'proveedores': 'purchase.read', // /suppliers → SuppliersController (PurchaseRead)
    'pagos-proveedores': 'purchase.read',
    'notas-credito-proveedores': 'purchase.read',
    // Flota
    'flota-dashboard': 'fleet.read',
    'flota-vehiculos': 'fleet.read',
    'flota-conductores': 'fleet.read',
    'flota-rutas': 'fleet.read',
    'flota-entregas': 'fleet.read',
    'flota-viajes': 'fleet.read',
    'flota-combustible': 'fleet.read',
    'flota-mantenimientos': 'fleet.read',
    'flota-taller': 'fleet.read',
    'flota-documentos': 'fleet.read',
    'flota-gastos': 'fleet.read',
    'flota-gps': 'fleet.read',
    'flota-alertas': 'fleet.read',
    'flota-tracking': 'fleet.read',
    'flota-predictive': 'fleet.read',
    'flota-catalogos': 'fleet.config',
    'flota-reportes': 'fleet.read',
    // Finanzas
    'caja': 'cash.read',
    'tesoreria': 'accounting.read',
    'contabilidad': 'accounting.read',
    'catalogo-cuentas': 'accounting.read',
    'tipo-cambio': 'accounting.read',
    'presupuestos': 'accounting.read',
    'presupuesto-vs-real': 'accounting.read',
    'conciliaciones': 'accounting.read',
    'centros-costo': 'accounting.read',
    // Talento
    'empleados': 'employee.read',
    'departamentos': 'employee.read',
    'asistencia': 'employee.read',
    'nomina': 'payroll.read',
    'prestadores': 'provider.read',
    'prestadores-dashboard': 'provider.read',
    'vacaciones': 'employee.read',
    'recibos-vacaciones': 'employee.read',
    'pagos-prestadores': 'payroll.read',
    'constancias': 'employee.read',
    'metas': 'goal.read',
    // BI
    'bi-financiero': 'kpi.read',
    'bi-comercial': 'kpi.read',
    'bi-operacional': 'kpi.read',
    'reportes-custom': 'report.read',
    // Administración
    'empresas': 'company.manage',
    'usuarios': 'user.read',
    'roles-permisos': 'role.manage',
    'invitar': 'user.write',
    'invitaciones-historial': 'user.write',
    'documental': 'document.read',
    'auditoria': 'audit.read',
    'db-storage': 'company.manage',
    // Fiscal
    'country-tax': 'accounting.read',
    'regional-tax': 'accounting.read',
  };

  /// Módulos habilitados por plan (espejo del catálogo `PlanModule` del backend).
  /// `enterprise` habilita todos los módulos del catálogo.
  /// Roles del SISTEMA (los 7 sembrados por el backend). Un rol personalizado
  /// (`RoleType.Custom`) NO está aquí: su acceso lo deciden sus permisos.
  static const Set<String> systemRoles = {
    'SuperAdmin',
    'CompanyAdmin',
    'Rrhh',
    'Supervisor',
    'Employee',
    'Accountant',
    'Dispatcher',
  };

  static const Map<String, List<String>> modulesByPlan = {
    'starter': ['Sales', 'Inventory', 'Hr', 'Administration'],
    'professional': [
      'Sales', 'Inventory', 'Hr', 'Administration',
      'Purchases', 'Fleet', 'Finance', 'Bi', 'Communication',
    ],
    'enterprise': [
      'Sales', 'Inventory', 'Hr', 'Administration',
      'Purchases', 'Fleet', 'Finance', 'Bi', 'Communication',
      'PalmTrack', 'Tax',
    ],
  };

  /// Conjunto de módulos habilitados para un plan. Un plan desconocido cae a
  /// `starter` (least-privilege). SuperAdmin verá todos los módulos.
  static Set<String> enabledModulesForPlan(String? plan) {
    return (modulesByPlan[plan] ?? modulesByPlan['starter']!).toSet();
  }

  static List<NavModule> getModulesForRole(
    String role, {
    String? searchQuery,
    Set<String>? enabledModules,
    Set<String>? permissions,
  }) {
    final bypass = role == 'SuperAdmin';
    // Un rol PERSONALIZADO (RoleType.Custom en el backend) no aparece en las
    // listas `roles` de módulos/ítems: su visibilidad la deciden sus permisos
    // granulares, así que para él se omite el filtro por rol.
    final isSystemRole = systemRoles.contains(role);

    bool roleAllowedOnModule(NavModule m) =>
        bypass || !isSystemRole || m.roles.isEmpty || m.roles.contains(role);

    bool roleAllowedOnItem(NavItem item) =>
        bypass || !isSystemRole || item.roles.isEmpty || item.roles.contains(role);

    bool moduleEnabled(NavModule m) {
      if (bypass || enabledModules == null) return true;
      if (m.module.isEmpty) return true;
      return enabledModules.contains(m.module);
    }

    /// Gating por permiso granular: solo aplica si el backend envió la lista
    /// (`permissions != null`) y el ítem declara permiso requerido. Un rol
    /// personalizado SIN permisos cargados no ve nada (fail-closed, igual que
    /// el guard del router).
    bool itemAllowed(NavItem item) {
      if (bypass) return true;
      if (permissions == null) return isSystemRole;
      final required = itemPermissions[item.id];
      if (required == null) return true;
      return permissions.contains(required);
    }

    final filtered = allModules.where((module) {
      if (!moduleEnabled(module)) return false;
      if (!roleAllowedOnModule(module)) return false;
      if (searchQuery != null && searchQuery.isNotEmpty) {
        final q = searchQuery.toLowerCase();
        final matchesModule = module.label.toLowerCase().contains(q);
        final matchesChild = module.children.any((item) =>
            item.label.toLowerCase().contains(q) &&
            itemAllowed(item) &&
            roleAllowedOnItem(item));
        return matchesModule || matchesChild;
      }
      return module.children.any(itemAllowed);
    }).map((module) {
      final allowedChildren = module.children.where(itemAllowed).toList();
      if (searchQuery != null && searchQuery.isNotEmpty) {
        final filteredItems = allowedChildren.where((item) {
          final roleOk = roleAllowedOnItem(item);
          final searchOk = item.label.toLowerCase().contains(searchQuery.toLowerCase());
          return roleOk && searchOk;
        }).toList();
        return NavModule(id: module.id, label: module.label, icon: module.icon, color: module.color, textColor: module.textColor, group: module.group, children: filteredItems, roles: module.roles, module: module.module);
      }
      if (identical(allowedChildren, module.children)) return module;
      return NavModule(id: module.id, label: module.label, icon: module.icon, color: module.color, textColor: module.textColor, group: module.group, children: allowedChildren, roles: module.roles, module: module.module);
    }).where((m) => m.children.isNotEmpty).toList();

    return filtered;
  }

  static List<NavModule> getModulesByGroup(List<NavModule> modules) {
    final grouped = <String, List<NavModule>>{};
    for (final m in modules) {
      grouped.putIfAbsent(m.group, () => []).add(m);
    }
    final order = ['operations', 'financial', 'talent', 'intelligence', 'admin'];
    final result = <NavModule>[];
    for (final key in order) {
      if (grouped.containsKey(key)) result.addAll(grouped[key]!);
    }
    return result;
  }
}
