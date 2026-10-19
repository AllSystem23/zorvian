import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/app/router.dart';
import 'package:zorvian/core/navigation/write_permissions.dart';
import 'package:zorvian/shared/ds/components/z_quick_actions_fab.dart';

/// Gating de ESCRITURA (permisos granulares, capa UI):
/// `write_permissions.dart` es la fuente única; la consumen el guard del router
/// (`hasAccessForRole` → `_hasAccess`) y el FAB global (`ZQuickActionsFAB`).
void main() {
  group('writePermissionFor', () {
    test('pantallas /new exigen el permiso de escritura del módulo', () {
      expect(writePermissionFor('/sales/new'), 'sale.write');
      expect(writePermissionFor('/quotes/new'), 'sale.write');
      expect(writePermissionFor('/products/new'), 'inventory.write');
      expect(writePermissionFor('/fleet/vehicles/new'), 'fleet.write');
      expect(writePermissionFor('/providers/contracts/new'), 'provider.write');
      expect(writePermissionFor('/stock-transfers/new'), 'inventory.write');
      expect(writePermissionFor('/documents/templates/new'), 'document.write');
    });

    test('pantallas /edit (id dinámico) resuelven al mismo permiso', () {
      expect(
        writePermissionFor('/sales/3f2a1b00-0000-0000-0000-000000000000/edit'),
        'sale.write',
      );
      expect(
        writePermissionFor('/employees/abc/edit'),
        'employee.write',
      );
    });

    test('marcador de acción también en el penúltimo segmento', () {
      // El router usa las dos variantes: `/x/new/:id` y `/x/edit/:id`.
      expect(writePermissionFor('/credit-notes/new/abc'), 'credit.write');
      expect(
        writePermissionFor('/service-payments/edit/abc'),
        'payroll.write',
      );
      expect(writePermissionFor('/payroll/periods/new'), 'payroll.write');
      expect(writePermissionFor('/crm/leads/abc/edit'), 'sale.write');
      expect(
        writePermissionFor('/crm/opportunities/abc/edit'),
        'sale.write',
      );
    });

    test('rutas de escritura explícitas (sin /new ni /edit)', () {
      expect(writePermissionFor('/goals/configurator'), 'goal.write');
      // El form de bodegas recibe el id por `extra`, no por la URL.
      expect(writePermissionFor('/warehouses/form'), 'inventory.write');
    });

    test('espeja permisos del backend que NO son <modulo>.write', () {
      // BranchesController exige `settings.write` para crear/editar/borrar.
      expect(writePermissionFor('/branches/new'), 'settings.write');
      // CustomReportsController exige `report.read` incluso para escribir.
      expect(writePermissionFor('/custom-reports/new'), 'report.read');
    });

    test('el prefijo /branches lo cubre el gate de LECTURA del router', () {
      // `/branches/:id` es el form de edición pero no lleva marcador: lo acota
      // `_routePermissions['/branches'] = 'settings.write'` en el router.
      expect(writePermissionFor('/branches/abc'), isNull);
    });

    test('páginas de lectura y autoservicio NO se gatean', () {
      expect(writePermissionFor('/sales'), isNull);
      expect(writePermissionFor('/dashboard'), isNull);
      // Autoservicio: el empleado crea su permiso/vacación (no hay *.write).
      expect(writePermissionFor('/permissions/new'), isNull);
      expect(writePermissionFor('/vacations/new'), isNull);
      expect(writePermissionFor('/goals'), isNull);
    });
  });

  group('canWriteRoute', () {
    test('sin permisos cargados no gatea (SSO/compat)', () {
      expect(canWriteRoute('/sales/new', null, 'Employee'), isTrue);
    });

    test('SuperAdmin siempre pasa', () {
      expect(canWriteRoute('/sales/new', <String>[], 'SuperAdmin'), isTrue);
    });

    test('lectura sin escritura bloquea la pantalla de escritura', () {
      expect(canWriteRoute('/sales/new', ['sale.read'], 'Employee'), isFalse);
      expect(
        canWriteRoute('/sales/new', ['sale.read', 'sale.write'], 'Employee'),
        isTrue,
      );
    });
  });

  group('router: hasAccessForRole con permiso de escritura', () {
    test('/sales readable pero /sales/new bloqueado sin sale.write', () {
      const readOnly = {'sale.read'};
      expect(
        hasAccessForRole('Employee', '/sales', permissions: readOnly),
        isTrue,
      );
      expect(
        hasAccessForRole('Employee', '/sales/new', permissions: readOnly),
        isFalse,
      );
    });

    test('con sale.write la pantalla de escritura pasa', () {
      expect(
        hasAccessForRole('Employee', '/sales/new',
            permissions: {'sale.read', 'sale.write'}),
        isTrue,
      );
    });

    test('el permiso es del módulo (client vs sale)', () {
      const perms = {'client.read', 'sale.read', 'sale.write'};
      expect(
        hasAccessForRole('Supervisor', '/clients/new', permissions: perms),
        isFalse,
        reason: 'client.write sigue faltando',
      );
      expect(
        hasAccessForRole('Supervisor', '/clients/new',
            permissions: {...perms, 'client.write'}),
        isTrue,
      );
    });

    test('rutas /edit también se gatean', () {
      expect(
        hasAccessForRole('Employee', '/products/abc/edit',
            permissions: {'inventory.read'}),
        isFalse,
      );
      expect(
        hasAccessForRole('Employee', '/products/abc/edit',
            permissions: {'inventory.read', 'inventory.write'}),
        isTrue,
      );
    });

    test('sin permisos (compat) el guard de escritura no aplica', () {
      expect(hasAccessForRole('Employee', '/sales/new'), isTrue);
    });
  });

  group('ZQuickActionsFAB: acciones ocultas por permiso', () {
    test('Employee lector pierde TODAS las acciones de ventas', () {
      // /sales ofrece /sales/new, /quotes/new y /employees/new: las 3 son
      // pantallas de escritura → sin los permisos el FAB desaparece.
      expect(
        ZQuickActionsFAB.routeHasActions('/sales',
            permissions: ['sale.read'], role: 'Employee'),
        isFalse,
      );
      expect(
        ZQuickActionsFAB.routeHasActions('/sales',
            permissions: ['sale.read', 'sale.write', 'employee.write'],
            role: 'Employee'),
        isTrue,
      );
    });

    test('acciones de autoservicio se conservan sin permisos de escritura', () {
      expect(
        ZQuickActionsFAB.routeHasActions('/permissions',
            permissions: ['employee.read'], role: 'Employee'),
        isTrue,
        reason: '/permissions/new no exige escritura (autoservicio)',
      );
    });

    test('SuperAdmin y sin permisos conservan todo', () {
      expect(
        ZQuickActionsFAB.routeHasActions('/sales',
            permissions: <String>[], role: 'SuperAdmin'),
        isTrue,
      );
      expect(ZQuickActionsFAB.routeHasActions('/sales'), isTrue);
    });
  });
}
