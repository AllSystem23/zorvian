import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../core/error/error_notifier.dart';
import '../core/network/dio_client.dart';
import '../core/providers/company_currency_provider.dart';
import '../core/storage/secure_storage.dart';

final secureStorageProvider = Provider<SecureStorage>((_) => SecureStorage());

final dioClientProvider = Provider<DioClient>((ref) {
  final storage = ref.watch(secureStorageProvider);
  return DioClient(
    storage,
    onError: (statusCode, message) {
      final notifier = ref.read(errorNotifierProvider.notifier);
      // Show actual backend error when available; fallback to generic friendly message
      final displayMsg = (message.isNotEmpty && message != 'Error de conexión')
          ? message
          : notifier.friendlyHttpError(statusCode);
      notifier.showError(
        displayMsg,
        detail: statusCode != null ? 'HTTP $statusCode' : null,
      );
    },
    onUnauthorized: () {
      ref.read(authProvider.notifier).logout();
    },
  );
});

enum AuthStatus { unknown, authenticated, unauthenticated, mfaRequired }

class AuthState {
  final AuthStatus status;
  final String? userId;
  final String? email;
  final String? displayName;
  final String? role;
  final String? tenantId;
  final String? employeeId;
  final String currencyCode;
  final String? mfaToken;

  const AuthState({
    this.status = AuthStatus.unknown,
    this.userId,
    this.email,
    this.displayName,
    this.role,
    this.tenantId,
    this.employeeId,
    this.currencyCode = 'NIO',
    this.mfaToken,
  });

  AuthState copyWith({
    AuthStatus? status,
    String? userId,
    String? email,
    String? displayName,
    String? role,
    String? tenantId,
    String? employeeId,
    String? currencyCode,
    String? mfaToken,
  }) => AuthState(
    status: status ?? this.status,
    userId: userId ?? this.userId,
    email: email ?? this.email,
    displayName: displayName ?? this.displayName,
    role: role ?? this.role,
    tenantId: tenantId ?? this.tenantId,
    employeeId: employeeId ?? this.employeeId,
    currencyCode: currencyCode ?? this.currencyCode,
    mfaToken: mfaToken ?? this.mfaToken,
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
        final response = await dio.get('auth/me', options: Options(extra: {'suppressGlobalError': true}));
        final user = response.data;
        final currencyCode = user['currencyCode'] ?? 'NIO';
        await storage.saveCurrencyCode(currencyCode);
        state = AuthState(
          status: AuthStatus.authenticated,
          userId: user['id'],
          email: user['email'],
          displayName: user['displayName'],
          role: user['role'],
          tenantId: user['tenantId'],
          employeeId: user['employeeId'],
          currencyCode: currencyCode,
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
      final response = await dio.post(
        'auth/login-password',
        data: {'email': email, 'password': password},
      );

      final body = response.data;

      // Check if MFA is required (API returns at top level: { mfa_required: true, mfa_token: "..." })
      if (body['mfa_required'] == true) {
        state = state.copyWith(
          status: AuthStatus.mfaRequired,
          mfaToken: body['mfa_token'] as String?,
        );
        return false; // Login not complete yet
      }

      // Normal login — tokens and user are under 'data'
      final data = body['data'] as Map<String, dynamic>?;
      if (data == null) return false;

      await storage
          .saveTokens(data['accessToken'] as String, data['refreshToken'] as String)
          .catchError((_) => null);

      final user = data['user'] as Map<String, dynamic>?;
      if (user == null) return false;

      final currencyCode = user['currencyCode'] as String? ?? 'NIO';
      await storage.saveCurrencyCode(currencyCode);
      state = AuthState(
        status: AuthStatus.authenticated,
        userId: user['id'] as String?,
        email: user['email'] as String?,
        displayName: user['displayName'] as String?,
        role: user['role'] as String?,
        tenantId: user['tenantId'] as String?,
        employeeId: user['employeeId'] as String?,
        currencyCode: currencyCode,
      );
      return true;
    } catch (_) {
      return false;
    }
  }

  /// Complete MFA verification after password login.
  Future<bool> completeMfaLogin(String mfaToken, String code) async {
    try {
      final dio = ref.read(dioClientProvider);
      final storage = ref.read(secureStorageProvider);
      final response = await dio.post(
        'auth/mfa/login',
        data: {'mfaToken': mfaToken, 'code': code},
      );

      final data = response.data['data'];
      await storage
          .saveTokens(data['accessToken'], data['refreshToken'])
          .catchError((_) => null);

      final user = data['user'];
      final currencyCode = user['currencyCode'] ?? 'NIO';
      await storage.saveCurrencyCode(currencyCode);
      state = AuthState(
        status: AuthStatus.authenticated,
        userId: user['id'],
        email: user['email'],
        displayName: user['displayName'],
        role: user['role'],
        tenantId: user['tenantId'],
        employeeId: user['employeeId'],
        currencyCode: currencyCode,
      );
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<bool> registerWithPassword(
    String email,
    String password,
    String displayName,
  ) async {
    try {
      final dio = ref.read(dioClientProvider);
      final storage = ref.read(secureStorageProvider);
      final response = await dio.post(
        'auth/register',
        data: {
          'email': email,
          'password': password,
          'displayName': displayName,
        },
      );

      final data = response.data['data'];
      await storage
          .saveTokens(data['accessToken'], data['refreshToken'])
          .catchError((_) => null);

      final user = data['user'];
      final currencyCode = user['currencyCode'] ?? 'NIO';
      await storage.saveCurrencyCode(currencyCode);
      state = AuthState(
        status: AuthStatus.authenticated,
        userId: user['id'],
        email: user['email'],
        displayName: user['displayName'],
        role: user['role'],
        tenantId: user['tenantId'],
        employeeId: user['employeeId'],
        currencyCode: currencyCode,
      );
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<void> logout() async {
    final storage = ref.read(secureStorageProvider);
    await storage.clearTokens();
    clearCachedCurrencyCode();
    await storage.saveCurrencyCode('NIO');
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  /// Cancel MFA flow and go back to login.
  void cancelMfa() {
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  /// Fetches the list of tenants the current user can access.
  Future<List<Map<String, dynamic>>> getMyTenants() async {
    final dio = ref.read(dioClientProvider);
    final response = await dio.get('auth/tenants', params: {'pageSize': 100});
    final data = response.data;
    final Iterable list = data is List
        ? data
        : (data['items'] as List<dynamic>);
    return list.map((e) => Map<String, dynamic>.from(e as Map)).toList();
  }

  /// Switches the current user's active tenant. Returns true on success.
  Future<bool> switchTenant(String tenantId) async {
    try {
      final dio = ref.read(dioClientProvider);
      final storage = ref.read(secureStorageProvider);
      final response = await dio.post(
        'auth/switch-tenant',
        data: {'tenantId': tenantId},
      );

      final data = response.data['data'];
      // Save new tokens issued for the switched tenant
      if (data['accessToken'] != null) {
        await storage
            .saveTokens(data['accessToken'], data['refreshToken'])
            .catchError((_) => null);
      }

      final user = data['user'];
      final currencyCode = user['currencyCode'] ?? 'NIO';
      await storage.saveCurrencyCode(currencyCode);
      state = AuthState(
        status: AuthStatus.authenticated,
        userId: user['id'],
        email: user['email'],
        displayName: user['displayName'],
        role: user['role'],
        tenantId: user['tenantId'],
        employeeId: user['employeeId'],
        currencyCode: currencyCode,
      );
      return true;
    } catch (_) {
      return false;
    }
  }
}

final authProvider = NotifierProvider<AuthNotifier, AuthState>(
  AuthNotifier.new,
);
