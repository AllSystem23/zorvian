import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/app/router.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/features/login/forgot_password_page.dart';
import 'package:zorvian/features/login/register_page.dart';
import 'package:zorvian/features/login/reset_password_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/fake_asset_bundle.dart';

Widget _wrapApp(Widget child, FakeApi api) {
  return DefaultAssetBundle(
    bundle: FakeAssetBundle(),
    child: ProviderScope(
      overrides: [dioClientProvider.overrideWithValue(api.client)],
      child: MaterialApp(home: child),
    ),
  );
}

/// Monta la página con una ventana alta y descarta el error de carga del logo.
Future<void> _pumpPage(WidgetTester tester, Widget page, FakeApi api) async {
  tester.view.physicalSize = const Size(1400, 2400);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);

  await tester.pumpWidget(_wrapApp(page, api));
  await tester.pumpAndSettle();
  tester.takeException();
}

/// Monta la app con el router real (sin sesión).
///
/// El splash tiene un spinner infinito: `pumpAndSettle` nunca termina, así
/// que se avanza el reloj a mano.
Future<void> _pumpRouter(WidgetTester tester, GoRouter router, FakeApi api) async {
  tester.view.physicalSize = const Size(1400, 2400);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);

  await tester.pumpWidget(
    DefaultAssetBundle(
      bundle: FakeAssetBundle(),
      child: ProviderScope(
        overrides: [dioClientProvider.overrideWithValue(api.client)],
        child: MaterialApp.router(routerConfig: router),
      ),
    ),
  );
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 200));
  tester.takeException();
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('rutas públicas de autenticación', () {
    test('el árbol de rutas expone /reset-password', () {
      final paths = appRoutes
          .whereType<GoRoute>()
          .map((r) => r.path)
          .toList();

      // La ruta cuelga del ShellRoute: se recorre el árbol completo.
      bool found = false;
      void walk(List<RouteBase> routes) {
        for (final r in routes) {
          if (r is GoRoute) {
            if (r.path == '/reset-password') found = true;
            walk(r.routes);
          } else if (r is ShellRoute) {
            walk(r.routes);
          }
        }
      }

      walk(appRoutes);
      expect(found, isTrue, reason: 'paths: $paths');
    });

    testWidgets('sin sesión, /reset-password NO rebota a /login', (tester) async {
      final router = buildGoRouter(const AuthState(status: AuthStatus.unauthenticated));
      final api = FakeApi((_) => jsonResponse(const {}));

      await _pumpRouter(tester, router, api);

      router.go('/reset-password?token=abc123');
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 200));

      expect(find.byType(ResetPasswordPage), findsOneWidget);
      expect(router.state.uri.path, '/reset-password');
    });

    testWidgets('sin sesión, /forgot-password NO rebota a /login', (tester) async {
      final router = buildGoRouter(const AuthState(status: AuthStatus.unauthenticated));
      final api = FakeApi((_) => jsonResponse(const {}));

      await _pumpRouter(tester, router, api);

      router.go('/forgot-password');
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 200));

      expect(find.byType(ForgotPasswordPage), findsOneWidget);
      expect(router.state.uri.path, '/forgot-password');
    });

    testWidgets('sin sesión, /register NO rebota a /login', (tester) async {
      final router = buildGoRouter(const AuthState(status: AuthStatus.unauthenticated));
      final api = FakeApi((_) => jsonResponse(const {}));

      await _pumpRouter(tester, router, api);

      router.go('/register');
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 200));

      expect(find.byType(RegisterPage), findsOneWidget);
      expect(router.state.uri.path, '/register');
    });
  });

  group('solicitud de acceso real', () {
    testWidgets('el modo "Solicitar Acceso" llama a auth/request-access',
        (tester) async {
      final api = FakeApi((_) => jsonResponse({'message': 'ok'}));

      await _pumpPage(tester, const RegisterPage(), api);

      await tester.tap(find.text('Solicitar Acceso').last);
      await tester.pumpAndSettle();
      tester.takeException();

      await tester.enterText(find.byType(TextFormField).at(0), 'Nuevo Usuario');
      await tester.enterText(find.byType(TextFormField).at(1), 'nuevo@empresa.com');
      await tester.enterText(find.byType(TextFormField).at(2), 'Empresa Nueva');

      await tester.tap(find.text('ENVIAR SOLICITUD'));
      await tester.pumpAndSettle();
      tester.takeException();

      final request = api.requestTo('auth/request-access');
      expect(request, isNotNull);
      final data = request!.data as Map;
      expect(data['fullName'], 'Nuevo Usuario');
      expect(data['email'], 'nuevo@empresa.com');
      expect(data['company'], 'Empresa Nueva');
    });

    testWidgets('el correo inválido bloquea el envío', (tester) async {
      final api = FakeApi((_) => jsonResponse({'message': 'ok'}));

      await _pumpPage(tester, const RegisterPage(), api);

      await tester.tap(find.text('Solicitar Acceso').last);
      await tester.pumpAndSettle();
      tester.takeException();

      await tester.enterText(find.byType(TextFormField).at(0), 'Nuevo Usuario');
      await tester.enterText(find.byType(TextFormField).at(1), 'correo-malo');

      await tester.tap(find.text('ENVIAR SOLICITUD'));
      await tester.pumpAndSettle();
      tester.takeException();

      expect(find.text('Ingrese un correo válido'), findsOneWidget);
      expect(api.requestTo('auth/request-access'), isNull);
    });
  });

  group('validación consistente', () {
    testWidgets('forgot-password usa el validador de correo compartido',
        (tester) async {
      final api = FakeApi((_) => jsonResponse(const {}));

      await _pumpPage(tester, const ForgotPasswordPage(), api);

      await tester.enterText(find.byType(TextFormField).first, 'no-es-correo');
      await tester.tap(find.text('ENVIAR ENLACE DE RECUPERACIÓN'));
      await tester.pumpAndSettle();
      tester.takeException();

      expect(find.text('Ingrese un correo válido'), findsOneWidget);
      expect(api.requestTo('auth/forgot-password'), isNull);
    });
  });
}



