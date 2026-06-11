import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/features/admin/models/user_model.dart';

class UserNotifier extends AsyncNotifier<List<UserModel>> {
  @override
  Future<List<UserModel>> build() async {
    return _fetchUsers();
  }

  Future<List<UserModel>> _fetchUsers() async {
    final dio = ref.read(dioClientProvider);
    final r = await dio.get('users');
    return (r.data as List).map((e) => UserModel.fromJson(e)).toList();
  }

  Future<void> load() async {
    state = const AsyncValue.loading();
    state = await AsyncValue.guard(_fetchUsers);
  }

  Future<void> inviteUser(String email, String role) async {
    final dio = ref.read(dioClientProvider);
    await AsyncValue.guard(() async {
      await dio.post('invitations', data: {'email': email, 'role': role});
      return _fetchUsers();
    });
    // Assuming we want to refresh after action, but guard handles state.
    // If _fetchUsers fails, state becomes error.
    final result = await AsyncValue.guard(_fetchUsers);
    state = result;
  }

  Future<void> updateUserRole(String userId, String roleId) async {
    final dio = ref.read(dioClientProvider);
    await dio.put('users/$userId/role', data: {'roleId': roleId});
    final result = await AsyncValue.guard(_fetchUsers);
    state = result;
  }
}

final userProvider = AsyncNotifierProvider<UserNotifier, List<UserModel>>(UserNotifier.new);
