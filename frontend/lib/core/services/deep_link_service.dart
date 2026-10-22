import 'dart:async';

import 'package:app_links/app_links.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../app/router.dart';
import '../../auth/auth_provider.dart';

/// Rutas internas a las que puede apuntar un deep link externo.
///
/// Lista blanca: un enlace que llega por correo (o desde cualquier otra app)
/// NUNCA debe poder navegar a una pantalla interna arbitraria. Solo las rutas
/// públicas de autenticación son alcanzables de esta forma.
const Set<String> kDeepLinkAllowedRoutes = {
  '/reset-password',
  '/register',
};

/// Parámetros de query admitidos por ruta. Cualquier otro se descarta.
const Map<String, Set<String>> kDeepLinkAllowedQuery = {
  '/reset-password': {'token'},
  '/register': {'code'},
};

/// Ruta interna derivada de un enlace externo.
@immutable
class DeepLinkRoute {
  final String path;
  final Map<String, String> query;

  const DeepLinkRoute({required this.path, this.query = const {}});

  /// Ubicación lista para `GoRouter.go`.
  String get location =>
      query.isEmpty ? path : Uri(path: path, queryParameters: query).toString();

  @override
  bool operator ==(Object other) =>
      other is DeepLinkRoute &&
      other.path == path &&
      mapEquals(other.query, query);

  @override
  int get hashCode =>
      Object.hash(path, Object.hashAll(query.entries.map((e) => '${e.key}=${e.value}')));

  @override
  String toString() => 'DeepLinkRoute($location)';
}

/// Convierte enlaces externos (esquema propio `zorvian://` o Universal Links
/// `https://zorvian-erp.web.app/...`) en rutas internas de la app.
///
/// Contrato de los enlaces que genera el backend (`AppLinks`):
///   - `zorvian://reset-password?token=…` → `/reset-password?token=…`
///   - `zorvian://register?code=…`        → `/register?code=…`
///   - `https://zorvian-erp.web.app/reset-password?token=…` → igual que el
///     esquema propio (mismo contrato de query).
class DeepLinkResolver {
  /// Esquema del deep link propio de la app.
  final String scheme;

  /// Host web que sirve los Universal Links.
  final String webHost;

  const DeepLinkResolver({
    this.scheme = 'zorvian',
    this.webHost = 'zorvian-erp.web.app',
  });

  /// Devuelve la ruta interna equivalente o `null` si el enlace no apunta a
  /// una ruta permitida.
  DeepLinkRoute? resolve(Uri uri) {
    final uriScheme = uri.scheme.toLowerCase();
    final String path;

    if (uriScheme == scheme.toLowerCase()) {
      // zorvian://reset-password?token=… → el host ES el primer segmento.
      path = _pathFromCustomScheme(uri);
    } else if (uriScheme == 'http' || uriScheme == 'https') {
      if (uri.host.toLowerCase() != webHost.toLowerCase()) return null;
      path = uri.path;
    } else {
      return null;
    }

    if (!kDeepLinkAllowedRoutes.contains(path)) return null;

    final allowed = kDeepLinkAllowedQuery[path] ?? const <String>{};
    final query = <String, String>{};
    for (final key in allowed) {
      final value = uri.queryParameters[key];
      if (value != null && value.isNotEmpty) query[key] = value;
    }

    return DeepLinkRoute(path: path, query: query);
  }

  static String _pathFromCustomScheme(Uri uri) {
    final segments = <String>[];
    if (uri.host.isNotEmpty) segments.add(uri.host);
    for (final segment in uri.path.split('/')) {
      if (segment.isNotEmpty) segments.add(segment);
    }
    return '/${segments.join('/')}';
  }
}

/// Estado del servicio de deep links.
@immutable
class DeepLinkState {
  /// Ruta interna pendiente de navegar: el guard de autenticación todavía no
  /// ha resuelto, así que navegarla ahora la rebotaría a `/splash`.
  ///
  /// Se entrega una sola vez (ver `DeepLinkService.takePendingLocation`).
  final String? pendingLocation;

  const DeepLinkState({this.pendingLocation});

  DeepLinkState copyWith({
    String? pendingLocation,
    bool clearPending = false,
  }) =>
      DeepLinkState(
        pendingLocation:
            clearPending ? null : (pendingLocation ?? this.pendingLocation),
      );
}

final deepLinkResolverProvider = Provider<DeepLinkResolver>(
  (_) => const DeepLinkResolver(),
);

/// Origen de los deep links. Abstrae el plugin `app_links` para poder
/// inyectarlo en los tests.
abstract class DeepLinkSource {
  /// Enlace con el que arrancó la app (null si no arrancó con un enlace).
  Future<Uri?> getInitialLink();

  /// Enlaces posteriores (app en caliente).
  Stream<Uri> get uriLinkStream;
}

/// Implementación sobre el plugin `app_links`.
class AppLinksDeepLinkSource implements DeepLinkSource {
  final AppLinks _appLinks;

  AppLinksDeepLinkSource([AppLinks? appLinks]) : _appLinks = appLinks ?? AppLinks();

  @override
  Future<Uri?> getInitialLink() => _appLinks.getInitialLink();

  @override
  Stream<Uri> get uriLinkStream => _appLinks.uriLinkStream;
}

final deepLinkSourceProvider = Provider<DeepLinkSource>(
  (_) => AppLinksDeepLinkSource(),
);

final deepLinkServiceProvider =
    NotifierProvider<DeepLinkService, DeepLinkState>(DeepLinkService.new);

/// Escucha los deep links de la app y los enruta al router.
///
/// Flujo: el backend envía por correo un enlace `zorvian://reset-password?token=…`
/// (o el equivalente `https://…/reset-password?token=…`). El sistema abre la
/// app con ese enlace y el plugin `app_links` lo entrega aquí.
///
/// - Enlace de arranque (app en frío): `AppLinks().getInitialLink()`.
/// - Enlaces posteriores (app en caliente): `AppLinks().uriLinkStream`.
class DeepLinkService extends Notifier<DeepLinkState> {
  StreamSubscription<Uri>? _subscription;
  String? _lastHandledLink;
  bool _started = false;

  @override
  DeepLinkState build() {
    ref.onDispose(() {
      _subscription?.cancel();
      _subscription = null;
    });
    return const DeepLinkState();
  }

  /// Arranca la escucha. Es idempotente.
  Future<void> start() async {
    if (_started) return;
    _started = true;

    final source = ref.read(deepLinkSourceProvider);

    try {
      final initial = await source.getInitialLink();
      if (initial != null) _handle(initial);
    } catch (_) {
      // Sin plugin disponible (web o escritorio): no hay enlace inicial.
    }

    try {
      _subscription = source.uriLinkStream.listen(_handle);
    } catch (_) {
      // El stream no está disponible: se queda solo el enlace inicial.
    }
  }

  /// Devuelve (y consume) la ruta pendiente, si la hay.
  String? takePendingLocation() {
    final pending = state.pendingLocation;
    if (pending == null) return null;
    state = state.copyWith(clearPending: true);
    return pending;
  }

  void _handle(Uri uri) {
    final link = uri.toString();

    // El plugin entrega el enlace de arranque DOS veces (por `getInitialLink`
    // y por el stream). Navegar dos veces la misma ruta es inocuo, pero
    // evita trabajo de más y pantallas duplicadas.
    if (link == _lastHandledLink) return;
    _lastHandledLink = link;

    final route = ref.read(deepLinkResolverProvider).resolve(uri);
    if (route == null) return;

    final status = ref.read(authProvider).status;
    if (status == AuthStatus.unknown) {
      // El guard de autenticación aún no resolvió: el rebote a `/splash`
      // tiraría el enlace. Se entrega al listener de auth (app.dart), que
      // navega a la ruta pendiente en vez de a `/login` o `/dashboard`.
      state = state.copyWith(pendingLocation: route.location);
      return;
    }

    _navigate(route.location);
  }

  void _navigate(String location) {
    try {
      ref.read(routerProvider).go(location);
    } catch (_) {
      // El router puede no existir todavía (la app acaba de arrancar).
    }
  }
}
