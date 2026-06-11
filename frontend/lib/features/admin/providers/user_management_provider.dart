import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class UsersNotifier extends AsyncNotifier<List<dynamic>> {
  @override
  Future<List<dynamic>> build() async {
    final dio = ref.read(dioClientProvider);
    final response = await dio.get('users');
    return (response.data as List).cast<dynamic>();
  }
}

final usersProvider = AsyncNotifierProvider.autoDispose<UsersNotifier, List<dynamic>>(UsersNotifier.new);

class RolesNotifier extends AsyncNotifier<List<dynamic>> {
  @override
  Future<List<dynamic>> build() async {
    final dio = ref.read(dioClientProvider);
    final response = await dio.get('users/roles');
    return (response.data as List).cast<dynamic>();
  }
}

final rolesProvider = AsyncNotifierProvider.autoDispose<RolesNotifier, List<dynamic>>(RolesNotifier.new);
