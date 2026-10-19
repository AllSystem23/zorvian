import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/core/navigation/nav_config.dart';
import 'package:zorvian/features/admin/config/subscription_plans_config.dart';

void main() {
  group('SubscriptionPlanConfig.fromApiJson', () {
    test('mapea la fila de la BD (id GUID + planId + price numérico + modules)',
        () {
      final plan = SubscriptionPlanConfig.fromApiJson({
        'id': '9f1c2d3e-4a5b-6c7d-8e9f-0a1b2c3d4e5f',
        'planId': 'professional',
        'name': 'Professional',
        'price': 49,
        'period': '/mes',
        'maxEmployees': 100,
        'isPopular': true,
        'modules': ['Fleet', 'Sales'],
      });

      expect(plan.id, 'professional', reason: 'la clave de negocio es planId');
      expect(plan.planGuid, '9f1c2d3e-4a5b-6c7d-8e9f-0a1b2c3d4e5f');
      expect(plan.isEditable, isTrue);
      expect(plan.price, '\$49');
      expect(plan.displayPrice, '\$49/mes');
      expect(plan.modules, ['Fleet', 'Sales']);
      // Las etiquetas respetan el orden del catálogo, no el del API.
      expect(plan.moduleLabels, ['Ventas', 'Flota y Logística']);
    });

    test('price 0 numérico se muestra como Gratis', () {
      final plan = SubscriptionPlanConfig.fromApiJson({
        'id': 'guid-x',
        'planId': 'starter',
        'name': 'Starter',
        'price': 0,
        'period': 'Para siempre',
        'maxEmployees': 10,
      });

      expect(plan.price, 'Gratis');
      expect(plan.displayPrice, 'Gratis');
    });

    test('sin planId (catálogo estático del backend) no es editable', () {
      final plan = SubscriptionPlanConfig.fromApiJson({
        'id': 'starter',
        'name': 'Starter',
        'price': 'Gratis',
        'period': 'Para siempre',
        'maxEmployees': 10,
        'isPopular': false,
        'shortDescription': 'Hasta 10 empleados, módulos básicos',
      });

      expect(plan.id, 'starter');
      expect(plan.planGuid, isNull);
      expect(plan.isEditable, isFalse);
      expect(plan.price, 'Gratis');
      // Sin `modules` en el payload cae al espejo del frontend por plan.
      expect(plan.modules, NavConfig.modulesByPlan['starter']);
      // Los datos de presentación salen del catálogo local por clave de negocio.
      expect(plan.features, isNotEmpty);
      expect(plan.moduleLabels, contains('Ventas'));
    });

    test('módulos desconocidos se conservan al final de las etiquetas', () {
      final plan = SubscriptionPlanConfig.fromApiJson({
        'id': 'guid-x',
        'planId': 'enterprise',
        'name': 'Enterprise',
        'price': 199,
        'period': '/mes',
        'maxEmployees': 9999,
        'modules': ['Sales', 'ZzzNuevo'],
      });

      expect(plan.moduleLabels, ['Ventas', 'ZzzNuevo']);
    });
  });

  group('PlanModuleOption (catálogo)', () {
    test('cubre los 11 ModuleType del backend', () {
      final keys = SubscriptionPlanConfig.availableModules.map((o) => o.key).toSet();
      expect(keys, {
        'Sales',
        'Inventory',
        'Purchases',
        'Fleet',
        'PalmTrack',
        'Finance',
        'Hr',
        'Bi',
        'Communication',
        'Administration',
        'Tax',
      });
      expect(keys.length, SubscriptionPlanConfig.availableModules.length,
          reason: 'sin claves duplicadas');
    });

    test('los módulos del mapa por plan existen en el catálogo', () {
      final keys = SubscriptionPlanConfig.availableModules.map((o) => o.key).toSet();
      for (final entry in NavConfig.modulesByPlan.entries) {
        for (final module in entry.value) {
          expect(keys, contains(module),
              reason: '${entry.key} referencia "$module" sin entrada en el catálogo');
        }
      }
    });

    test('moduleLabel traduce y hace passthrough de claves desconocidas', () {
      expect(SubscriptionPlanConfig.moduleLabel('Hr'), 'Talento Humano');
      expect(SubscriptionPlanConfig.moduleLabel('NoExiste'), 'NoExiste');
    });
  });

  group('SubscriptionPlanEndpoints', () {
    test('arma el path del PUT de módulos y el cuerpo del request', () {
      expect(
        SubscriptionPlanEndpoints.modulesPath('9f1c2d3e'),
        'subscription-plans/9f1c2d3e/modules',
      );
      expect(
        SubscriptionPlanEndpoints.modulesBody(['Sales', 'Hr']),
        {
          'modules': ['Sales', 'Hr']
        },
      );
    });
  });

  group('fallbackAll', () {
    test('expone los 3 planes sin GUID y con módulos para mostrar', () {
      expect(SubscriptionPlanConfig.fallbackAll.length, 3);
      for (final plan in SubscriptionPlanConfig.fallbackAll) {
        expect(plan.isEditable, isFalse);
        expect(plan.modules, isNotEmpty);
        expect(plan.moduleLabels, isNotEmpty);
      }
      expect(SubscriptionPlanConfig.fallbackAll.first.id, 'starter');
    });
  });
}
