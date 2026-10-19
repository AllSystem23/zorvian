import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/auth/auth_provider.dart';

/// Rol de la compañía con sus permisos granulares asignados.
class RolePermissionModel {
  final String id;
  final String name;
  final String displayName;
  final String? description;
  final bool isSystem;
  final List<String> permissions;

  const RolePermissionModel({
    required this.id,
    required this.name,
    required this.displayName,
    this.description,
    required this.isSystem,
    required this.permissions,
  });

  factory RolePermissionModel.fromJson(Map<String, dynamic> json) =>
      RolePermissionModel(
        id: json['id'] as String? ?? '',
        // Tolerante: el backend serializa el nombre del rol como string; si por
        // una versión antigua llegara un número (enum), no reventamos el parseo.
        name: '${json['name'] ?? ''}',
        displayName: json['displayName'] as String? ?? '',
        description: json['description'] as String?,
        isSystem: json['isSystem'] as bool? ?? false,
        permissions: (json['permissions'] as List<dynamic>? ?? const [])
            .map((e) => '$e')
            .toList(),
      );

  /// Rol PERSONALIZADO de la compañía (creado desde esta pantalla): se puede
  /// renombrar y eliminar. Los roles del sistema solo permiten editar permisos.
  bool get isCustom => !isSystem;
}

/// Entrada del catálogo de permisos (código, etiqueta, grupo).
class PermissionCatalogEntry {
  final String code;
  final String label;
  final String group;
  final bool isSuperAdminOnly;

  const PermissionCatalogEntry({
    required this.code,
    required this.label,
    required this.group,
    required this.isSuperAdminOnly,
  });

  factory PermissionCatalogEntry.fromJson(Map<String, dynamic> json) =>
      PermissionCatalogEntry(
        code: json['code'] as String? ?? '',
        label: json['label'] as String? ?? '',
        group: json['group'] as String? ?? '',
        isSuperAdminOnly: json['isSuperAdminOnly'] as bool? ?? false,
      );
}

/// Estado de la pantalla de matriz: roles + catálogo.
class RolesAdminState {
  final List<RolePermissionModel> roles;
  final List<PermissionCatalogEntry> catalog;

  const RolesAdminState({required this.roles, required this.catalog});

  /// Catálogo agrupado por grupo (orden de aparición).
  Map<String, List<PermissionCatalogEntry>> get groupedCatalog {
    final grouped = <String, List<PermissionCatalogEntry>>{};
    for (final entry in catalog) {
      grouped.putIfAbsent(entry.group, () => []).add(entry);
    }
    return grouped;
  }
}

class RolesAdminNotifier extends AsyncNotifier<RolesAdminState> {
  @override
  Future<RolesAdminState> build() => _fetch();

  Future<void> load() async {
    state = const AsyncValue.loading();
    state = await AsyncValue.guard(_fetch);
  }

  Future<RolesAdminState> _fetch() async {
    final dio = ref.read(dioClientProvider);
    final results = await Future.wait([
      dio.get('roles'),
      dio.get('roles/permissions-catalog'),
    ]);
    final roles = (results[0].data as List)
        .map((e) => RolePermissionModel.fromJson(Map<String, dynamic>.from(e as Map)))
        .toList();
    final catalog = (results[1].data as List)
        .map((e) => PermissionCatalogEntry.fromJson(Map<String, dynamic>.from(e as Map)))
        .toList();
    return RolesAdminState(roles: roles, catalog: catalog);
  }

  /// Reemplaza el conjunto de permisos de un rol. Lanza [Exception] con el
  /// mensaje del backend si la validación falla (permisos SuperAdmin-only, etc).
  Future<void> updatePermissions(String roleId, List<String> permissions) async {
    final dio = ref.read(dioClientProvider);
    await dio.put(
      'roles/$roleId/permissions',
      data: {'permissions': permissions},
    );
    state = await AsyncValue.guard(_fetch);
  }

  /// Crea un rol personalizado de la compañía con sus permisos iniciales.
  /// Lanza [DioException] si el nombre está repetido o un permiso no es válido.
  Future<void> createRole({
    required String displayName,
    String? description,
    List<String> permissions = const [],
  }) async {
    final dio = ref.read(dioClientProvider);
    await dio.post('roles', data: {
      'displayName': displayName,
      'description': description,
      'permissions': permissions,
    });
    state = await AsyncValue.guard(_fetch);
  }

  /// Renombra / describe un rol personalizado.
  Future<void> updateRole(
    String roleId, {
    String? displayName,
    String? description,
  }) async {
    final dio = ref.read(dioClientProvider);
    await dio.put('roles/$roleId', data: {
      if (displayName != null) 'displayName': displayName,
      if (description != null) 'description': description,
    });
    state = await AsyncValue.guard(_fetch);
  }

  /// Elimina un rol personalizado. El backend rechaza los roles de sistema y
  /// los que tengan usuarios asignados.
  Future<void> deleteRole(String roleId) async {
    final dio = ref.read(dioClientProvider);
    await dio.delete('roles/$roleId');
    state = await AsyncValue.guard(_fetch);
  }
}

final rolesAdminProvider =
    AsyncNotifierProvider<RolesAdminNotifier, RolesAdminState>(
        RolesAdminNotifier.new);
