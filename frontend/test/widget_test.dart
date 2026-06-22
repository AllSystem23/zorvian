import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/app/app.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/storage/secure_storage.dart';
import 'package:zorvian/features/biometrics/providers/biometric_provider.dart';

void main() {
  testWidgets('App renders login page', (WidgetTester tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authProvider.overrideWith(() => AuthNotifierMock()),
          biometricProvider.overrideWith(() => BiometricNotifierMock()),
          secureStorageProvider.overrideWith((ref) => SecureStorageMock()),
        ],
        child: const ZorvianApp(),
      ),
    );
    
    // We use pump() instead of pumpAndSettle() because ParticleBackground 
    // has an infinite animation that prevents pumpAndSettle from finishing.
    await tester.pump(const Duration(seconds: 1));
    await tester.pump(const Duration(seconds: 1));
    
    expect(find.text('Iniciar Sesión'), findsOneWidget);
  });
}

class AuthNotifierMock extends AuthNotifier {
  @override
  AuthState build() => const AuthState(status: AuthStatus.unauthenticated);
}

class BiometricNotifierMock extends BiometricNotifier {
  @override
  BiometricState build() => const BiometricState(isEnabled: false, isUnlocked: true);
}

class SecureStorageMock extends SecureStorage {
  @override
  Future<(String?, String?)> getRememberedCredentials() async => (null, null);
  @override
  Future<String?> getAccessToken() async => null;
  @override
  Future<bool> isBiometricEnabled() async => false;
}
