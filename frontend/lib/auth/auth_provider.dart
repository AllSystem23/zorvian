import 'package:dio/dio.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../core/error/error_notifier.dart';
import '../core/network/dio_client.dart';
import '../core/storage/secure_storage.dart';
import '../features/dashboard/providers/dashboard_provider.dart';

final secureStorageProvider = Provider<SecureStorage>((_) => SecureStorage());

final dioClientProvider = Provider<DioClient>((ref) {
  final storage = ref.watch(secureStorageProvider);
  return DioClient(storage, onError: (statusCode, message) {
    final notifier = ref.read(errorNotifierProvider.notifier);
    // Show actual backend error when available; fallback to generic friendly message
    final displayMsg = (message.isNotEmpty && message != 'Error de conexión')
        ? message
        : notifier.friendlyHttpError(statusCode);
    notifier.showError(displayMsg, detail: statusCode != null ? 'HTTP $statusCode' : null);
  }, onUnauthorized: () {
    ref.read(authProvider.notifier).logout();
  });
});

enum AuthStatus { unknown, authenticated, unauthenticated }

class AuthState {
  final AuthStatus status;
  final String? userId;
  final String? email;
  final String? displayName;
  final String? role;
  final String? tenantId;
  final String? employeeId;

  const AuthState({
    this.status = AuthStatus.unknown,
    this.userId,
    this.email,
    this.displayName,
    this.role,
    this.tenantId,
    this.employeeId,
  });

  AuthState copyWith({
    AuthStatus? status,
    String? userId,
    String? email,
    String? displayName,
    String? role,
    String? tenantId,
    String? employeeId,
  }) => AuthState(
    status: status ?? this.status,
    userId: userId ?? this.userId,
    email: email ?? this.email,
    displayName: displayName ?? this.displayName,
    role: role ?? this.role,
    tenantId: tenantId ?? this.tenantId,
    employeeId: employeeId ?? this.employeeId,
  );
}

class AuthNotifier extends Notifier<AuthState> {
  @override
  AuthState build() => const AuthState();

  Future<void> checkAuth() async {
    final storage = ref.read(secureStorageProvider);
    final token = await storage.getAccessToken();
    if (token != null) {
      try {
        final dio = ref.read(dioClientProvider);
        final response = await dio.get('auth/me');
        final user = response.data;
        state = AuthState(
          status: AuthStatus.authenticated,
          userId: user['id'],
          email: user['email'],
          displayName: user['displayName'],
          role: user['role'],
          tenantId: user['tenantId'],
          employeeId: user['employeeId'],
        );
      } catch (_) {
        await storage.clearTokens();
        state = state.copyWith(status: AuthStatus.unauthenticated);
      }
    } else {
      state = state.copyWith(status: AuthStatus.unauthenticated);
    }
  }

  Future<bool> loginWithPassword(String email, String password) async {
    try {
      final dio = ref.read(dioClientProvider);
      final storage = ref.read(secureStorageProvider);
      final response = await dio.post('auth/login-password', data: {
        'email': email,
        'password': password,
      });

      final data = response.data['data'];
      // Guardamos tokens
      await storage.saveTokens(data['accessToken'], data['refreshToken'])
          .catchError((_) => null);

      final user = data['user'];
      state = AuthState(
        status: AuthStatus.authenticated,
        userId: user['id'],
        email: user['email'],
        displayName: user['displayName'],
        role: user['role'],
        tenantId: user['tenantId'],
        employeeId: user['employeeId'],
      );
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<bool> registerWithPassword(String email, String password, String displayName) async {
    try {
      final dio = ref.read(dioClientProvider);
      final storage = ref.read(secureStorageProvider);
      final response = await dio.post('auth/register', data: {
        'email': email,
        'password': password,
        'displayName': displayName,
      });

      final data = response.data['data'];
      await storage.saveTokens(data['accessToken'], data['refreshToken'])
          .catchError((_) => null);

      final user = data['user'];
      state = AuthState(
        status: AuthStatus.authenticated,
        userId: user['id'],
        email: user['email'],
        displayName: user['displayName'],
        role: user['role'],
        tenantId: user['tenantId'],
        employeeId: user['employeeId'],
      );
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<void> logout() async {
    final storage = ref.read(secureStorageProvider);
    await storage.clearTokens();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }
}

final authProvider = NotifierProvider<AuthNotifier, AuthState>(
  AuthNotifier.new,
);
