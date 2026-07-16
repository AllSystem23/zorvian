import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';

/// Subscription plan configuration.
/// Business data (id, name, price, maxEmployees, isPopular) comes from the API.
/// Display data (color, icon, features, limitations) is defined locally.
class SubscriptionPlanConfig {
  final String id;
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

  const SubscriptionPlanConfig({
    required this.id,
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
  });

  /// Create from API JSON (business data only), enriched with local display data.
  factory SubscriptionPlanConfig.fromApiJson(Map<String, dynamic> json) {
    final id = json['id'] as String? ?? '';
    final display = _displayData[id];
    return SubscriptionPlanConfig(
      id: id,
      name: json['name'] as String? ?? id,
      price: json['price'] as String? ?? 'Gratis',
      period: json['period'] as String? ?? '',
      maxEmployees: json['maxEmployees'] as int? ?? 50,
      isPopular: json['isPopular'] as bool? ?? false,
      shortDescription: json['shortDescription'] as String? ??
          (display?['shortDescription'] as String?) ?? '',
      color: (display?['color'] as Color?) ?? ZColors.neutral400,
      icon: (display?['icon'] as IconData?) ?? Icons.help_outline,
      features: (display?['features'] as List<dynamic>?)?.cast<String>() ?? [],
      limitations: (display?['limitations'] as List<dynamic>?)?.cast<String>() ?? [],
    );
  }

  /// Formatted price with period for display.
  String get displayPrice => price == 'Gratis' ? 'Gratis' : '$price$period';

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
          color: (SubscriptionPlanConfig._displayData[id]?['color'] as Color?) ?? ZColors.neutral400,
          icon: (SubscriptionPlanConfig._displayData[id]?['icon'] as IconData?) ?? Icons.help_outline,
          shortDescription: (SubscriptionPlanConfig._displayData[id]?['shortDescription'] as String?) ?? '',
          features: (SubscriptionPlanConfig._displayData[id]?['features'] as List<dynamic>?)?.cast<String>() ?? [],
          limitations: (SubscriptionPlanConfig._displayData[id]?['limitations'] as List<dynamic>?)?.cast<String>() ?? [],
        );
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
