import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/app/router.dart';
import 'package:zorvian/core/navigation/nav_config.dart';
import 'package:zorvian/features/admin/providers/roles_admin_provider.dart';

/// Roles PERSONALIZADOS (CRUD de roles): un rol creado por la compañía no
/// figura en las listas de roles por ruta ni en las listas `roles` del menú.
/// Su acceso lo deciden EXCLUSIVAMENTE sus permisos granulares; sin permisos
/// cargados se falla cerrado (a diferencia de los roles del sistema, que
/// conservan su lista de roles).
void main() {
  group('RolePermissionModel (contrato del API de roles)', () {
    test('parsea el nombre y distingue roles personalizados', () {
      final custom = RolePermissionModel.fromJson({
        'id': 'r1',
        'name': 'Custom',
        'displayName': 'Auditor',
        'description': 'Solo lectura',
        'isSystem': false,
        'permissions': ['sale.read'],
      });
      expect(custom.name, 'Custom');
      expect(custom.isCustom, isTrue);
      expect(custom.description, 'Solo lectura');

      final system = RolePermissionModel.fromJson({
        'id': 'r2',
        'name': 'CompanyAdmin',
        'displayName': 'Admin',
        'isSystem': true,
        'permissions': <String>[],
      });
      expect(system.isCustom, isFalse);
      expect(system.description, isNull);
    });

    test('tolera un `name` numérico (enum serializado como entero)', () {
      // Regresión: `json['name'] as String?` reventaba con un enum numérico.
      final role = RolePermissionModel.fromJson({
        'id': 'r3',
        'name': 6,
        'displayName': 'Auditor',
        'isSystem': false,
        'permissions': <String>[],
      });
      expect(role.name, '6');
    });
  });

  group('router: acceso de un rol personalizado', () {
    test('la ruta se decide por permiso de lectura/escritura', () {
      expect(
        hasAccessForRole('Custom', '/sales', permissions: {'sale.read'}),
        isTrue,
      );
      expect(
        hasAccessForRole('Custom', '/sales/new', permissions: {'sale.read'}),
        isFalse,
      );
      expect(
        hasAccessForRole('Custom', '/sales/new',
            permissions: {'sale.read', 'sale.write'}),
        isTrue,
      );
    });

    test('abre una ruta restringida por rol si tiene el permiso', () {
      // '/admin/roles' está reservada a SuperAdmin/CompanyAdmin por rol.
      expect(
        hasAccessForRole('Custom', '/admin/roles', permissions: <String>{}),
        isFalse,
      );
      expect(
        hasAccessForRole('Custom', '/admin/roles',
            permissions: {'role.manage'}),
        isTrue,
      );
    });

    test('sin permisos cargados no pasa (fail-closed)', () {
      expect(hasAccessForRole('Custom', '/sales'), isFalse);
    });

    test('los roles del sistema conservan su lista de roles', () {
      expect(
        hasAccessForRole('Employee', '/admin/roles',
            permissions: {'role.manage'}),
        isFalse,
      );
      expect(
        hasAccessForRole('CompanyAdmin', '/admin/roles',
            permissions: {'role.manage'}),
        isTrue,
      );
    });
  });

  group('sidebar: un rol personalizado se filtra solo por permisos', () {
    List<String> itemIds(String role, {Set<String>? permissions}) =>
        NavConfig.getModulesForRole(role, permissions: permissions)
            .expand((m) => m.children)
            .map((i) => i.id)
            .toList();

    test('entra a un módulo restringido por rol cuando tiene el permiso', () {
      // El módulo de Administración declara roles [SuperAdmin, CompanyAdmin]:
      // un rol del sistema ajeno queda fuera, un rol personalizado entra si
      // tiene el permiso del ítem.
      expect(
        itemIds('Custom', permissions: {'user.read'}),
        contains('usuarios'),
      );
    });

    test('un rol del sistema NO entra a ese módulo aunque tenga el permiso', () {
      expect(
        itemIds('Employee', permissions: {'user.read'}),
        isNot(contains('usuarios')),
      );
    });

    test('el permiso sigue siendo obligatorio al saltarse el filtro de rol', () {
      // 'empresas' exige company.manage: sin él no aparece aunque el rol
      // personalizado pase el filtro de roles del módulo.
      expect(
        itemIds('Custom', permissions: {'user.read'}),
        isNot(contains('empresas')),
      );
      expect(
        itemIds('Custom', permissions: {'user.read', 'company.manage'}),
        contains('empresas'),
      );
    });

    test('sin permisos cargados no ve nada', () {
      expect(itemIds('Custom'), isEmpty);
    });
  });
}
