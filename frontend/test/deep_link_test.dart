import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/app/router.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/services/deep_link_service.dart';

import 'helpers/fake_api.dart';
import 'helpers/fake_asset_bundle.dart';

/// Origen de enlaces falso: permite simular el enlace de arranque y los
/// enlaces en caliente sin depender del plugin nativo.
class _FakeLinkSource implements DeepLinkSource {
  _FakeLinkSource({this.initial});

  Uri? initial;
  int initialCalls = 0;
  final StreamController<Uri> _controller = StreamController<Uri>.broadcast();

  @override
  Future<Uri?> getInitialLink() async {
    initialCalls++;
    return initial;
  }

  @override
  Stream<Uri> get uriLinkStream => _controller.stream;

  void emit(Uri uri) => _controller.add(uri);

  void dispose() => _controller.close();
}

/// Notificador de sesión fijo: la prueba no habla con la red.
class _FakeAuthNotifier extends AuthNotifier {
  _FakeAuthNotifier(this.initial);

  final AuthState initial;

  @override
  AuthState build() => initial;
}

ProviderContainer _container(
  _FakeLinkSource source, {
  AuthStatus status = AuthStatus.unauthenticated,
}) {
  return ProviderContainer(
    overrides: [
      authProvider.overrideWith(
        () => _FakeAuthNotifier(AuthState(status: status)),
      ),
      deepLinkSourceProvider.overrideWithValue(source),
    ],
  );
}

/// Monta el router real. El splash tiene un spinner infinito, así que
/// `pumpAndSettle` nunca termina: el reloj se avanza a mano.
Future<void> _pumpRouter(WidgetTester tester, GoRouter router) async {
  tester.view.physicalSize = const Size(1400, 2400);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);

  final api = FakeApi((_) => jsonResponse(const {}));

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
  const resolver = DeepLinkResolver();

  group('resolución de deep links', () {
    test('el esquema propio con token apunta a /reset-password', () {
      final route = resolver.resolve(
        Uri.parse('zorvian://reset-password?token=abc123'),
      );

      expect(route, isNotNull);
      expect(route!.path, '/reset-password');
      expect(route.query, {'token': 'abc123'});
      expect(route.location, '/reset-password?token=abc123');
    });

    test('el esquema propio con código apunta a /register', () {
      final route = resolver.resolve(
        Uri.parse('zorvian://register?code=INV-77'),
      );

      expect(route, isNotNull);
      expect(route!.path, '/register');
      expect(route.location, '/register?code=INV-77');
    });

    test('el esquema propio con triple barra también resuelve', () {
      final route = resolver.resolve(
        Uri.parse('zorvian:///reset-password?token=abc123'),
      );

      expect(route?.path, '/reset-password');
      expect(route?.location, '/reset-password?token=abc123');
    });

    test('el Universal Link https usa el mismo contrato', () {
      final route = resolver.resolve(
        Uri.parse('https://zorvian-erp.web.app/reset-password?token=abc123'),
      );

      expect(route?.path, '/reset-password');
      expect(route?.location, '/reset-password?token=abc123');
    });

    test('un host ajeno se descarta', () {
      expect(
        resolver.resolve(
          Uri.parse('https://otro-sitio.com/reset-password?token=abc123'),
        ),
        isNull,
      );
    });

    test('una ruta que no es pública se descarta', () {
      expect(resolver.resolve(Uri.parse('zorvian://dashboard')), isNull);
      expect(
        resolver.resolve(Uri.parse('zorvian://sales?token=abc123')),
        isNull,
      );
    });

    test('un esquema desconocido se descarta', () {
      expect(
        resolver.resolve(Uri.parse('ftp://zorvian-erp.web.app/reset-password')),
        isNull,
      );
    });

    test('los query params no permitidos se descartan', () {
      final route = resolver.resolve(
        Uri.parse('zorvian://reset-password?token=abc123&admin=1'),
      );

      expect(route?.query, {'token': 'abc123'});
      expect(route?.location, isNot(contains('admin')));
    });

    test('un token con caracteres especiales se codifica', () {
      final route = resolver.resolve(
        Uri.parse('zorvian://reset-password?token=a%20b%26c'),
      );

      expect(route?.query['token'], 'a b&c');
      expect(route?.location, '/reset-password?token=a+b%26c');
    });
  });

  group('servicio de deep links', () {
    testWidgets('el enlace de arranque navega cuando la sesión ya está resuelta',
        (tester) async {
      final source = _FakeLinkSource(
        initial: Uri.parse('zorvian://reset-password?token=abc123'),
      );
      final container = _container(source);
      addTearDown(container.dispose);
      addTearDown(source.dispose);

      final router = container.read(routerProvider);
      await _pumpRouter(tester, router);

      await container.read(deepLinkServiceProvider.notifier).start();
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 200));

      expect(router.state.uri.path, '/reset-password');
      expect(router.state.uri.queryParameters['token'], 'abc123');
    });

    test('sin sesión resuelta, el enlace queda pendiente y se consume una vez',
        () async {
      final source = _FakeLinkSource(
        initial: Uri.parse('zorvian://reset-password?token=abc123'),
      );
      final container = _container(source, status: AuthStatus.unknown);
      addTearDown(container.dispose);
      addTearDown(source.dispose);

      await container.read(deepLinkServiceProvider.notifier).start();
      await Future<void>.delayed(const Duration(milliseconds: 20));

      final service = container.read(deepLinkServiceProvider.notifier);
      expect(service.takePendingLocation(), '/reset-password?token=abc123');
      expect(service.takePendingLocation(), isNull);
    });

    testWidgets('el enlace duplicado (arranque + stream) navega una sola vez',
        (tester) async {
      final source = _FakeLinkSource(
        initial: Uri.parse('zorvian://reset-password?token=abc123'),
      );
      final container = _container(source);
      addTearDown(container.dispose);
      addTearDown(source.dispose);

      final router = container.read(routerProvider);
      await _pumpRouter(tester, router);

      await container.read(deepLinkServiceProvider.notifier).start();
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 200));

      final before = router.state.uri;

      source.emit(Uri.parse('zorvian://reset-password?token=abc123'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 200));

      expect(router.state.uri, before);
    });

    testWidgets('un enlace en caliente navega a la ruta', (tester) async {
      final source = _FakeLinkSource();
      final container = _container(source);
      addTearDown(container.dispose);
      addTearDown(source.dispose);

      final router = container.read(routerProvider);
      await _pumpRouter(tester, router);

      await container.read(deepLinkServiceProvider.notifier).start();
      await tester.pump();

      source.emit(Uri.parse('zorvian://register?code=INV-77'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 200));

      expect(router.state.uri.path, '/register');
      expect(router.state.uri.queryParameters['code'], 'INV-77');
    });

    testWidgets('un enlace a una ruta no permitida no navega', (tester) async {
      final source = _FakeLinkSource();
      final container = _container(source);
      addTearDown(container.dispose);
      addTearDown(source.dispose);

      final router = container.read(routerProvider);
      await _pumpRouter(tester, router);

      await container.read(deepLinkServiceProvider.notifier).start();
      await tester.pump();

      final before = router.state.uri;

      source.emit(Uri.parse('zorvian://dashboard'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 200));

      expect(router.state.uri, before);
    });
  });
}
