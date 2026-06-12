import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/app/app.dart';
import 'package:zorvian/auth/auth_provider.dart';

void main() {
  testWidgets('Splash initially shows loading, then navigates to login', (tester) async {
    // 1. Pump the app
    await tester.pumpWidget(
      const ProviderScope(
        child: ZorvianApp(),
      ),
    );

    // 2. Verify we are on Splash initially
    expect(find.text('Cargando...'), findsOneWidget);

    // 3. Trigger authentication state change (simulate checkAuth finishing as unauthenticated)
    // We can access the notifier to force the state update
    final container = ProviderScope.containerOf(tester.element(find.byType(ZorvianApp)));
    
    // Explicitly set state to unauthenticated
    final authNotifier = container.read(authProvider.notifier);
    // Note: Since we cannot easily "set" state directly, we simulate the logic
    // We just need to trigger the redirect.
    // The redirect logic in router.dart depends on authState.status.
    // Let's try triggering checkAuth which should fail as unauthenticated if no token.
    await authNotifier.checkAuth();
    
    // 4. Pump to let router process the redirect
    await tester.pumpAndSettle();

    // 5. Verify we are on Login
    // Note: LoginPage uses 'Iniciar Sesión'
    expect(find.text('Iniciar Sesión'), findsOneWidget);
  });
}
