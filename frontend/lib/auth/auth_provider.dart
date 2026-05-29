import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../core/network/dio_client.dart';
import '../core/storage/secure_storage.dart';

final secureStorageProvider = Provider<SecureStorage>((_) => SecureStorage());

final dioClientProvider = Provider<DioClient>((ref) {
  final storage = ref.watch(secureStorageProvider);
  return DioClient(storage);
});

enum AuthStatus { unknown, authenticated, unauthenticated }

class AuthState {
  final AuthStatus status;
  final String? userId;
  final String? email;
  final String? displayName;
  final String? role;
  final String? tenantId;

  const AuthState({
    this.status = AuthStatus.unknown,
    this.userId,
    this.email,
    this.displayName,
    this.role,
    this.tenantId,
  });

  AuthState copyWith({
    AuthStatus? status,
    String? userId,
    String? email,
    String? displayName,
    String? role,
    String? tenantId,
  }) => AuthState(
    status: status ?? this.status,
    userId: userId ?? this.userId,
    email: email ?? this.email,
    displayName: displayName ?? this.displayName,
    role: role ?? this.role,
    tenantId: tenantId ?? this.tenantId,
  );
}

class AuthNotifier extends Notifier<AuthState> {
  @override
  AuthState build() => const AuthState();

  Future<void> checkAuth() async {
    final storage = ref.read(secureStorageProvider);
    final token = await storage.getAccessToken();
    if (token != null) {
      state = state.copyWith(status: AuthStatus.authenticated);
    } else {
      state = state.copyWith(status: AuthStatus.unauthenticated);
    }
  }

  Future<bool> loginWithFirebase(String idToken) async {
    try {
      final dio = ref.read(dioClientProvider);
      final storage = ref.read(secureStorageProvider);
      final response = await dio.post('/auth/login', data: {
        'idToken': idToken,
      });

      final data = response.data['data'];
      await storage.saveTokens(data['accessToken'], data['refreshToken']);

      final user = data['user'];
      state = AuthState(
        status: AuthStatus.authenticated,
        userId: user['id'],
        email: user['email'],
        displayName: user['displayName'],
        role: user['role'],
        tenantId: user['tenantId'],
      );
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<void> logout() async {
    final dio = ref.read(dioClientProvider);
    final storage = ref.read(secureStorageProvider);
    final refresh = await storage.getRefreshToken();
    if (refresh != null) {
      try {
        await dio.post('/auth/logout', data: {'refreshToken': refresh});
      } catch (_) {}
    }
    await storage.clearTokens();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }
}

final authProvider = NotifierProvider<AuthNotifier, AuthState>(
  AuthNotifier.new,
);
