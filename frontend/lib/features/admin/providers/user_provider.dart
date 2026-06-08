import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:nexora/auth/auth_provider.dart';
import 'package:nexora/features/admin/models/user_model.dart';

class UserState {
  final List<UserModel> users;
  final bool loading;
  final String? error;
  const UserState({this.users = const [], this.loading = false, this.error});
  UserState copyWith({List<UserModel>? users, bool? loading, String? error}) =>
    UserState(users: users ?? this.users, loading: loading ?? this.loading, error: error ?? this.error);
}

class UserNotifier extends Notifier<UserState> {
  @override
  UserState build() => const UserState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('users');
      final items = (r.data as List).map((e) => UserModel.fromJson(e)).toList();
      state = state.copyWith(users: items, loading: false);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar usuarios', loading: false);
    }
  }

  Future<void> inviteUser(String email, String role) async {
    state = state.copyWith(loading: true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('invitations', data: {'email': email, 'role': role});
      await load(); // Reload list after invitation
    } catch (e) {
      state = state.copyWith(error: 'Error al invitar usuario', loading: false);
    }
  }

  Future<void> updateUserRole(String userId, String roleId) async {
    state = state.copyWith(loading: true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('users/$userId/role', data: {'roleId': roleId});
      await load(); // Reload list after update
    } catch (e) {
      state = state.copyWith(error: 'Error al actualizar rol', loading: false);
    }
  }
}

final userProvider = NotifierProvider<UserNotifier, UserState>(UserNotifier.new);
