import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

/// Usuario del endpoint `GET users` (respuesta cruda). Se conserva por
/// compatibilidad: la página usa `userProvider` con modelos tipados.
class UsersNotifier extends AsyncNotifier<List<dynamic>> {
  @override
  Future<List<dynamic>> build() async {
    final dio = ref.read(dioClientProvider);
    final response = await dio.get('users');
    return (response.data as List).cast<dynamic>();
  }
}

final usersProvider =
    AsyncNotifierProvider.autoDispose<UsersNotifier, List<dynamic>>(
        UsersNotifier.new);

/// Rol asignable a un usuario (`GET users/roles`).
class RoleOption {
  final String id;
  final String displayName;

  /// Clave del rol (`RoleType`): 'SuperAdmin', 'CompanyAdmin', ..., o 'Custom'
  /// para los roles personalizados de la compañía.
  final String name;
  final String? description;
  final bool isSystem;

  const RoleOption({
    required this.id,
    required this.displayName,
    required this.name,
    this.description,
    required this.isSystem,
  });

  factory RoleOption.fromJson(Map<String, dynamic> json) => RoleOption(
        id: json['id'] as String? ?? '',
        displayName: json['displayName'] as String? ?? '',
        // Tolerante: versiones previas del API serializaban el enum numérico.
        name: '${json['name'] ?? ''}',
        description: json['description'] as String?,
        isSystem: json['isSystem'] as bool? ?? false,
      );

  /// Rol de PLATAFORMA: solo un SuperAdmin puede asignarlo (el backend lo
  /// rechaza con 403 para el resto).
  bool get isPlatform => name == 'SuperAdmin';

  /// Etiqueta para el desplegable: los roles personalizados se distinguen.
  String get label => isSystem ? displayName : '$displayName (personalizado)';
}

class RolesNotifier extends AsyncNotifier<List<RoleOption>> {
  @override
  Future<List<RoleOption>> build() async {
    final dio = ref.read(dioClientProvider);
    final response = await dio.get('users/roles');
    return (response.data as List)
        .map((e) => RoleOption.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}

final rolesProvider =
    AsyncNotifierProvider.autoDispose<RolesNotifier, List<RoleOption>>(
        RolesNotifier.new);
