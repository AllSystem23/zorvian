import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/navigation/nav_config.dart';
import '../../../shared/ds/ds.dart';

/// Módulo licenciable del ERP (espejo del enum `ModuleType` del backend y del
/// catálogo `PlanModule`). `key` es el nombre que viaja por el API.
class PlanModuleOption {
  final String key;
  final String label;
  final IconData icon;

  const PlanModuleOption({
    required this.key,
    required this.label,
    required this.icon,
  });
}

/// Subscription plan configuration.
/// Business data (id, name, price, maxEmployees, isPopular, modules) comes from
/// the API. Display data (color, icon, features, limitations) is defined locally.
class SubscriptionPlanConfig {
  /// Clave de negocio del plan (`Company.SubscriptionPlan`), p. ej. 'starter'.
  final String id;

  /// Id GUID del plan en la tabla `SubscriptionPlans`, presente solo cuando el
  /// API lo devolvió desde la BD (el catálogo estático no lo expone).
  /// `null` = plan no editable (la tabla aún no está sembrada / API caída).
  final String? planGuid;

  final String name;
  final String price;
  final String period;
  final Color color;
  final IconData icon;
  final int maxEmployees;
  final bool isPopular;
  final String shortDescription;
  final List<String> features;
  final List<String> limitations;

  /// Módulos (`ModuleType` names) habilitados para el plan.
  final List<String> modules;

  const SubscriptionPlanConfig({
    required this.id,
    this.planGuid,
    required this.name,
    required this.price,
    required this.period,
    required this.color,
    required this.icon,
    required this.maxEmployees,
    this.isPopular = false,
    required this.shortDescription,
    required this.features,
    required this.limitations,
    this.modules = const [],
  });

  /// Create from API JSON (business data only), enriched with local display data.
  factory SubscriptionPlanConfig.fromApiJson(Map<String, dynamic> json) {
    // `SubscriptionPlanResponse` del backend expone `id` (GUID de la fila) y
    // `planId` (clave de negocio). El catálogo estático solo expone `id` ya
    // siendo la clave — de ahí el fallback.
    final apiId = (json['id'] as String?) ?? '';
    final planId = (json['planId'] as String?) ?? apiId;
    final display = _displayData[planId];
    return SubscriptionPlanConfig(
      id: planId,
      planGuid: json['planId'] != null ? apiId : null,
      name: json['name'] as String? ?? planId,
      price: _formatPrice(json['price']),
      period: json['period'] as String? ?? '',
      maxEmployees: json['maxEmployees'] as int? ?? 50,
      isPopular: json['isPopular'] as bool? ?? false,
      shortDescription: json['shortDescription'] as String? ??
          (display?['shortDescription'] as String?) ?? '',
      color: (display?['color'] as Color?) ?? ZColors.neutral400,
      icon: (display?['icon'] as IconData?) ?? Icons.help_outline,
      features: (display?['features'] as List<dynamic>?)?.cast<String>() ?? [],
      limitations: (display?['limitations'] as List<dynamic>?)?.cast<String>() ?? [],
      modules: _stringList(json['modules']) ??
          (NavConfig.modulesByPlan[planId]?.toList() ?? const []),
    );
  }

  /// Formatted price with period for display.
  String get displayPrice => price == 'Gratis' ? 'Gratis' : '$price$period';

  /// `true` cuando el plan vino del API con su GUID → se pueden editar módulos.
  bool get isEditable => planGuid != null;

  /// Catálogo de módulos licenciables (orden de presentación en la UI).
  static const List<PlanModuleOption> availableModules = [
    PlanModuleOption(key: 'Sales', label: 'Ventas', icon: Icons.point_of_sale_outlined),
    PlanModuleOption(key: 'Inventory', label: 'Inventario', icon: Icons.inventory_2_outlined),
    PlanModuleOption(key: 'Purchases', label: 'Compras', icon: Icons.shopping_cart_outlined),
    PlanModuleOption(key: 'Fleet', label: 'Flota y Logística', icon: Icons.local_shipping_outlined),
    PlanModuleOption(key: 'PalmTrack', label: 'PalmTrack', icon: Icons.agriculture_outlined),
    PlanModuleOption(key: 'Finance', label: 'Finanzas', icon: Icons.account_balance_outlined),
    PlanModuleOption(key: 'Hr', label: 'Talento Humano', icon: Icons.diversity_3_outlined),
    PlanModuleOption(key: 'Bi', label: 'BI e Inteligencia', icon: Icons.insights_outlined),
    PlanModuleOption(key: 'Communication', label: 'Comunicación', icon: Icons.forum_outlined),
    PlanModuleOption(key: 'Administration', label: 'Administración', icon: Icons.admin_panel_settings_outlined),
    PlanModuleOption(key: 'Tax', label: 'Configuración Fiscal', icon: Icons.account_balance_outlined),
  ];

  /// Etiqueta legible de un módulo (`ModuleType` name → texto de UI).
  static String moduleLabel(String key) {
    for (final option in availableModules) {
      if (option.key == key) return option.label;
    }
    return key;
  }

  /// Módulos del plan como etiquetas legibles, en el orden del catálogo.
  List<String> get moduleLabels {
    final known = availableModules
        .where((o) => modules.contains(o.key))
        .map((o) => o.label)
        .toList();
    final unknown = modules
        .where((m) => !availableModules.any((o) => o.key == m))
        .toList()
      ..sort();
    return [...known, ...unknown];
  }

  static String _formatPrice(dynamic value) {
    if (value is String) return value.isEmpty ? 'Gratis' : value;
    if (value is num) {
      if (value == 0) return 'Gratis';
      final text = value == value.roundToDouble()
          ? value.toStringAsFixed(0)
          : value.toStringAsFixed(2);
      return '\$$text';
    }
    return 'Gratis';
  }

  static List<String>? _stringList(dynamic value) {
    if (value is! List) return null;
    return value.map((e) => '$e').where((e) => e.isNotEmpty).toList();
  }

  /// Lookup a plan config by its string id from a list.
  static SubscriptionPlanConfig? findById(String id, List<SubscriptionPlanConfig> plans) {
    for (final plan in plans) {
      if (plan.id == id) return plan;
    }
    return null;
  }

  // ── Local display-only data (color, icon, features, limitations, shortDescription) ──

  static const Map<String, Map<String, dynamic>> _displayData = {
    'starter': {
      'color': ZColors.neutral400,
      'icon': Icons.rocket_launch_outlined,
      'shortDescription': 'Hasta 10 trabajadores, módulos básicos',
      'features': [
        'Hasta 10 trabajadores',
        'Módulos básicos (RRHH, Ventas)',
        'Soporte por email',
        '1GB almacenamiento',
        'Reportes estándar',
      ],
      'limitations': [
        'Sin flota/logística',
        'Sin IA asistente',
        'Sin webhooks personalizados',
      ],
    },
    'professional': {
      'color': ZColors.brandAccent,
      'icon': Icons.star_outline,
      'shortDescription': 'Hasta 100 trabajadores, todos los módulos',
      'features': [
        'Hasta 100 trabajadores',
        'Todos los módulos ERP',
        'Soporte prioritario',
        '10GB almacenamiento',
        'Reportes avanzados + BI',
        'Flota y logística',
        'Webhooks personalizados',
      ],
      'limitations': [
        'Sin IA asistente',
        'Sin multi-sucursal avanzado',
      ],
    },
    'enterprise': {
      'color': ZColors.brandGold,
      'icon': Icons.diamond_outlined,
      'shortDescription': 'Trabajadores ilimitados, IA + API',
      'features': [
        'Trabajadores ilimitados',
        'Todos los módulos ERP + IA',
        'Soporte 24/7 dedicado',
        'Almacenamiento ilimitado',
        'Reportes + BI + predictivo',
        'Multi-sucursal avanzado',
        'API pública personalizada',
        'Integraciones premium',
        'SLA garantizado 99.9%',
      ],
      'limitations': <String>[],
    },
  };

  /// Static fallback list (used if API is unavailable).
  /// Generated from [_displayData] to stay in sync with display definitions.
  static final List<SubscriptionPlanConfig> fallbackAll = [
    _FallbackPlan('starter', 'Starter', 'Gratis', 'Para siempre', 10),
    _FallbackPlan('professional', 'Professional', '\$49', '/mes', 100, isPopular: true),
    _FallbackPlan('enterprise', 'Enterprise', '\$199', '/mes', 9999),
  ];

  static const List<(String, String, String, String)> comparisonRows = [
    ('Trabajadores', '10', '100', 'Ilimitados'),
    ('Módulos ERP', 'Básicos', 'Todos', 'Todos + IA'),
    ('Almacenamiento', '1GB', '10GB', 'Ilimitado'),
    ('Soporte', 'Email', 'Prioritario', '24/7 Dedicado'),
    ('Reportes', 'Estándar', 'Avanzados + BI', 'Predictivo'),
    ('Flota/Logística', '—', '✓', '✓'),
    ('IA Asistente', '—', '—', '✓'),
    ('Webhooks', '—', 'Personalizados', 'Personalizados'),
    ('Multi-sucursal', '—', '—', 'Avanzado'),
    ('API Pública', '—', '—', '✓'),
    ('SLA', '—', '—', '99.9%'),
  ];
}

/// Helper that pulls display data from [_displayData] automatically.
class _FallbackPlan extends SubscriptionPlanConfig {
  _FallbackPlan(
    String id,
    String name,
    String price,
    String period,
    int maxEmployees, {
    super.isPopular = false,
  }) : super(
          id: id,
          name: name,
          price: price,
          period: period,
          maxEmployees: maxEmployees,
          // El catálogo estático no expone módulos: se espeja el mapa por plan
          // del frontend (solo para mostrar; sin GUID no se pueden editar).
          modules: NavConfig.modulesByPlan[id]?.toList() ?? const [],
          color: (SubscriptionPlanConfig._displayData[id]?['color'] as Color?) ?? ZColors.neutral400,
          icon: (SubscriptionPlanConfig._displayData[id]?['icon'] as IconData?) ?? Icons.help_outline,
          shortDescription: (SubscriptionPlanConfig._displayData[id]?['shortDescription'] as String?) ?? '',
          features: (SubscriptionPlanConfig._displayData[id]?['features'] as List<dynamic>?)?.cast<String>() ?? [],
          limitations: (SubscriptionPlanConfig._displayData[id]?['limitations'] as List<dynamic>?)?.cast<String>() ?? [],
        );
}

/// Contrato HTTP del catálogo de módulos por plan. Lo consume el editor de
/// módulos de la página de Planes de Suscripción (SuperAdmin).
class SubscriptionPlanEndpoints {
  SubscriptionPlanEndpoints._();

  /// `PUT` que reemplaza el conjunto de módulos habilitados de un plan.
  static String modulesPath(String planGuid) =>
      'subscription-plans/$planGuid/modules';

  /// Cuerpo del request anterior (`UpdatePlanModulesRequest` del backend).
  static Map<String, dynamic> modulesBody(Iterable<String> modules) =>
      {'modules': modules.toList()};
}

/// Riverpod provider that fetches subscription plans from the API.
/// Falls back to [SubscriptionPlanConfig.fallbackAll] if the API is unavailable.
final subscriptionPlansProvider = FutureProvider<List<SubscriptionPlanConfig>>((ref) async {
  try {
    final dio = ref.read(dioClientProvider);
    final response = await dio.get('companies/subscription-plans');
    final data = response.data as List<dynamic>;
    return data
        .map((e) => SubscriptionPlanConfig.fromApiJson(Map<String, dynamic>.from(e as Map)))
        .toList();
  } catch (_) {
    return SubscriptionPlanConfig.fallbackAll;
  }
});
