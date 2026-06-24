import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/app/app.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/storage/secure_storage.dart';

/// In-memory storage for tests — no platform channels needed.
class TestSecureStorage extends SecureStorage {
  @override
  Future<String?> getAccessToken() async => null;

  @override
  Future<void> clearTokens() async {}
}

void main() {
  testWidgets('Splash initially shows loading, then navigates to login', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [secureStorageProvider.overrideWith((_) => TestSecureStorage())],
        child: const ZorvianApp(),
      ),
    );

    expect(find.text('Cargando...'), findsOneWidget);

    final container = ProviderScope.containerOf(tester.element(find.byType(ZorvianApp)));
    final authNotifier = container.read(authProvider.notifier);
    await authNotifier.logout();

    for (int i = 0; i < 10; i++) {
      await tester.pump(const Duration(milliseconds: 50));
    }

    expect(find.text('Iniciar Sesión'), findsOneWidget);
  });
}
