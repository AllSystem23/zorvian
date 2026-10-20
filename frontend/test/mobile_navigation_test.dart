import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/core/widgets/header/mobile_bottom_nav.dart';

void main() {
  group('MobileBottomNav.buildDestinations', () {
    test('SuperAdmin ve Inicio + 3 hubs + Más', () {
      final destinations = MobileBottomNav.buildDestinations(role: 'SuperAdmin');

      expect(
        destinations.map((d) => d.label).toList(),
        ['Inicio', 'Ventas', 'Inventario', 'Personas', 'Más'],
      );
      expect(destinations.first.route, '/dashboard');
      expect(destinations[1].route, '/sales', reason: 'hub canónico, no el primer ítem');
      expect(destinations[3].route, '/employees');
      expect(destinations.last.isMore, isTrue);
      expect(destinations.first.isMore, isFalse);
    });

    test('los hubs sin ítems permitidos no aparecen', () {
      final destinations = MobileBottomNav.buildDestinations(
        role: 'Employee',
        permissions: {'sale.read'},
      );

      final labels = destinations.map((d) => d.label).toList();
      expect(labels.first, 'Inicio');
      expect(labels.last, 'Más');
      expect(labels, contains('Ventas'));
      expect(labels, isNot(contains('Inventario')));
      expect(labels, isNot(contains('Finanzas')));
      expect(destinations[1].route, '/sales');
    });

    test('con permisos de RRHH el hub de personas se suma', () {
      final destinations = MobileBottomNav.buildDestinations(
        role: 'Employee',
        permissions: {'sale.read', 'employee.read'},
      );

      expect(
        destinations.map((d) => d.label).toList(),
        ['Inicio', 'Ventas', 'Personas', 'Más'],
      );
    });

    test('un rol personalizado sin permisos cargados ve solo Inicio + Más', () {
      final destinations = MobileBottomNav.buildDestinations(role: 'Custom');

      expect(destinations.length, 2);
      expect(destinations.first.route, '/dashboard');
      expect(destinations.last.isMore, isTrue);
    });

    test('el plan de la compañía limita los hubs', () {
      final destinations = MobileBottomNav.buildDestinations(
        role: 'CompanyAdmin',
        enabledModules: {'Sales'},
      );

      expect(
        destinations.map((d) => d.label).toList(),
        ['Inicio', 'Ventas', 'Más'],
      );
    });

    test('nunca hay más de 3 hubs', () {
      final destinations = MobileBottomNav.buildDestinations(role: 'SuperAdmin');

      final hubs = destinations.where((d) => !d.isMore && d.route != '/dashboard');
      expect(hubs.length, lessThanOrEqualTo(3));
      expect(destinations.length, lessThanOrEqualTo(5));
    });
  });

  group('MobileBottomNav.selectedIndex', () {
    final destinations = MobileBottomNav.buildDestinations(role: 'SuperAdmin');

    test('Inicio para dashboard y panel ejecutivo', () {
      expect(MobileBottomNav.selectedIndex('/dashboard', destinations), 0);
      expect(
        MobileBottomNav.selectedIndex('/executive-dashboard', destinations),
        0,
      );
    });

    test('resuelve el hub de rutas anidadas', () {
      expect(
        MobileBottomNav.selectedIndex('/credits/overdue-dashboard', destinations),
        1,
      );
      expect(
        MobileBottomNav.selectedIndex('/stock-transfers', destinations),
        2,
      );
      expect(
        MobileBottomNav.selectedIndex('/payroll/periods/new', destinations),
        3,
      );
    });

    test('"Más" queda activo en rutas fuera de los hubs (regresión clamp 0..3)',
        () {
      final lastIndex = destinations.length - 1;
      for (final location in [
        '/documents',
        '/settings',
        '/admin/users',
        '/chat',
        '/bi/executive',
      ]) {
        expect(
          MobileBottomNav.selectedIndex(location, destinations),
          lastIndex,
          reason: '$location pertenece a "Más"',
        );
      }
    });

    test('el índice siempre cae dentro de los destinos', () {
      for (final location in [
        '/dashboard',
        '/sales',
        '/products',
        '/employees',
        '/documents',
        '/unknown/route',
      ]) {
        final index = MobileBottomNav.selectedIndex(location, destinations);
        expect(
          index,
          inInclusiveRange(0, destinations.length - 1),
          reason: '$location no debe producir un índice fuera de rango',
        );
      }
    });
  });
}
