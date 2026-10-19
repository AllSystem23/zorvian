import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/app/router.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/navigation/nav_config.dart';

/// Gating por permisos granulares (feature: permisos asignables desde el
/// frontend). Cubre las 3 capas: AuthState.hasPermission, sidebar
/// (NavConfig.getModulesForRole) y router (hasAccessForRole con permissions).
void main() {
  group('AuthState.hasPermission', () {
    test('SuperAdmin always passes', () {
      const state = AuthState(role: 'SuperAdmin', permissions: <String>[]);
      expect(state.hasPermission('sale.write'), isTrue);
      expect(state.canWrite('payroll'), isTrue);
    });

    test('permissions == null disables permission gating (SSO/compat)', () {
      const state = AuthState(role: 'Employee', permissions: null);
      expect(state.hasPermission('sale.write'), isTrue);
    });

    test('membership check + canWrite module shortcut', () {
      const state = AuthState(
        role: 'Supervisor',
        permissions: ['sale.read', 'client.read'],
      );
      expect(state.hasPermission('sale.read'), isTrue);
      expect(state.hasPermission('sale.write'), isFalse);
      expect(state.canWrite('sale'), isFalse);
      expect(state.canWrite('client'), isFalse); // client.write no está
    });
  });

  group('NavConfig.getModulesForRole con permisos', () {
    const salesRead = {
      'sale.read', 'client.read', 'credit.read', 'inventory.read',
      'cash.read', 'employee.read', 'goal.read', 'fleet.read',
      'document.read',
    };

    test('item sin permiso requerido se oculta', () {
      // Employee ve Cotizaciones (sale.read ✓) pero NO Nómina (payroll.read ✗).
      final modules = NavConfig.getModulesForRole(
        'Employee',
        permissions: salesRead,
      );
      final items = modules.expand((m) => m.children).toList();
      expect(items.any((i) => i.id == 'cotizaciones'), isTrue);
      expect(items.any((i) => i.id == 'nomina'), isFalse);
      expect(items.any((i) => i.id == 'facturas-compra'), isFalse);
    });

    test('módulo sin ningún hijo permitido desaparece', () {
      final modules = NavConfig.getModulesForRole(
        'Employee',
        permissions: salesRead,
      );
      // Compras completo requiere purchase.read → módulo entero ausente.
      expect(modules.any((m) => m.id == 'compras'), isFalse);
    });

    test('permissions == null conserva el filtrado por rol actual', () {
      final modules = NavConfig.getModulesForRole('Employee');
      final items = modules.expand((m) => m.children).toList();
      // Sin gating por permiso, Compras es visible por rol (comportamiento previo).
      expect(items.any((i) => i.id == 'facturas-compra'), isTrue);
    });

    test('SuperAdmin bypass total', () {
      final modules = NavConfig.getModulesForRole(
        'SuperAdmin',
        permissions: <String>{},
      );
      final items = modules.expand((m) => m.children).toList();
      expect(items.any((i) => i.id == 'nomina'), isTrue);
      expect(items.any((i) => i.id == 'facturas-compra'), isTrue);
    });

    test('búsqueda respeta el gating por permiso', () {
      final modules = NavConfig.getModulesForRole(
        'Employee',
        permissions: salesRead,
        searchQuery: 'nómina',
      );
      expect(modules.expand((m) => m.children), isEmpty);
    });
  });

  group('hasAccessForRole con permisos', () {
    const employeePerms = {'sale.read', 'client.read', 'inventory.read'};

    test('rol permitido pero sin permiso → denegado', () {
      expect(
        hasAccessForRole('Employee', '/payroll', permissions: employeePerms),
        isFalse,
      );
      expect(
        hasAccessForRole('Employee', '/sales', permissions: employeePerms),
        isTrue,
      );
    });

    test('permissions == null no gatea (compat)', () {
      expect(
        hasAccessForRole('Employee', '/clients'),
        isTrue,
        reason: 'sin lista de permisos se conserva el guard por rol',
      );
      expect(
        hasAccessForRole('Employee', '/clients', permissions: {'sale.read'}),
        isFalse,
        reason: 'con lista, /clients exige client.read',
      );
    });

    test('SuperAdmin ignora el gating por permiso', () {
      expect(
        hasAccessForRole('SuperAdmin', '/payroll', permissions: <String>{}),
        isTrue,
      );
    });

    test('guard por rol sigue aplicando primero', () {
      // Employee no está en los roles de /payroll aunque tenga permiso.
      expect(
        hasAccessForRole(
          'Employee',
          '/payroll',
          permissions: {'payroll.read'},
        ),
        isFalse,
      );
    });

    test('prefijo más específico gana (admin/users vs admin)', () {
      const adminPerms = {'company.manage'};
      expect(
        hasAccessForRole('CompanyAdmin', '/admin/companies',
            permissions: adminPerms),
        isFalse,
        reason: '/admin/companies es SuperAdmin-only por rol → denegado antes',
      );
      expect(
        hasAccessForRole('SuperAdmin', '/admin/users',
            permissions: adminPerms),
        isTrue,
        reason: 'SuperAdmin salta el gating por permiso (bypass total)',
      );
      expect(
        hasAccessForRole('CompanyAdmin', '/admin/users',
            permissions: adminPerms),
        isFalse,
        reason: 'CompanyAdmin carece de user.read en el set → denegado',
      );
    });
  });
}
