import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/features/login/reset_password_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/fake_asset_bundle.dart';

Widget _wrap(Widget child, FakeApi api) {
  return DefaultAssetBundle(
    bundle: FakeAssetBundle(),
    child: ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(api.client)],
      child: MaterialApp(home: child),
    ),
  );
}

/// Monta la página con una ventana alta (el botón queda fuera del viewport
/// de 600 px por defecto) y descarta el error de carga del logo.
Future<void> _pumpPage(WidgetTester tester, Widget page, FakeApi api) async {
  tester.view.physicalSize = const Size(1400, 2400);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);

  await tester.pumpWidget(_wrap(page, api));
  await tester.pumpAndSettle();
  tester.takeException();
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('ResetPasswordPage', () {
    testWidgets('sin token muestra el estado de enlace inválido', (tester) async {
      final api = FakeApi((_) => jsonResponse(const {}));

      await _pumpPage(tester, const ResetPasswordPage(), api);

      expect(find.text('Enlace inválido'), findsOneWidget);
      expect(find.text('SOLICITAR NUEVO ENLACE'), findsOneWidget);
      expect(find.byType(TextFormField), findsNothing);
    });

    testWidgets('valida la política de contraseña y la confirmación', (tester) async {
      final api = FakeApi((_) => jsonResponse(const {}));

      await _pumpPage(tester, const ResetPasswordPage(token: 'abc123'), api);

      await tester.enterText(find.byType(TextFormField).at(0), 'corta');
      await tester.enterText(find.byType(TextFormField).at(1), 'otra');
      await tester.tap(find.text('GUARDAR NUEVA CONTRASEÑA'));
      await tester.pumpAndSettle();
      tester.takeException();

      expect(find.text('Mínimo 8 caracteres'), findsOneWidget);
      expect(find.text('Las contraseñas no coinciden'), findsOneWidget);
      // No se envió nada al backend.
      expect(api.requestTo('auth/reset-password'), isNull);
    });

    testWidgets('envía token, newPassword y confirmPassword y muestra el éxito',
        (tester) async {
      final api = FakeApi((_) => jsonResponse({'message': 'ok'}));

      await _pumpPage(tester, const ResetPasswordPage(token: 'abc123'), api);

      await tester.enterText(find.byType(TextFormField).at(0), 'Zorvian1');
      await tester.enterText(find.byType(TextFormField).at(1), 'Zorvian1');
      await tester.tap(find.text('GUARDAR NUEVA CONTRASEÑA'));
      await tester.pumpAndSettle();
      tester.takeException();

      final request = api.requestTo('auth/reset-password');
      expect(request, isNotNull);
      final data = request!.data as Map;
      expect(data['token'], 'abc123');
      expect(data['newPassword'], 'Zorvian1');
      expect(data['confirmPassword'], 'Zorvian1');

      expect(find.text('Contraseña actualizada'), findsOneWidget);
      expect(find.text('IR A INICIAR SESIÓN'), findsOneWidget);
    });

    testWidgets('muestra el error del backend sin perder el formulario',
        (tester) async {
      final api = FakeApi(
        (_) => jsonResponse({'error': 'El enlace expiró. Solicita uno nuevo.'}, status: 400),
      );

      await _pumpPage(tester, const ResetPasswordPage(token: 'abc123'), api);

      await tester.enterText(find.byType(TextFormField).at(0), 'Zorvian1');
      await tester.enterText(find.byType(TextFormField).at(1), 'Zorvian1');
      await tester.tap(find.text('GUARDAR NUEVA CONTRASEÑA'));
      await tester.pumpAndSettle();
      tester.takeException();

      expect(find.text('El enlace expiró. Solicita uno nuevo.'), findsOneWidget);
      expect(find.text('Contraseña actualizada'), findsNothing);
    });
  });
}
