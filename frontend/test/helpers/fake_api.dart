import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:zorvian/core/network/dio_client.dart';
import 'package:zorvian/core/storage/secure_storage.dart';

/// `SecureStorage` en memoria.
///
/// El `SecureStorage` real depende de un canal de plugin que no existe en los
/// tests: el interceptor de `DioClient` se queda esperando `getAccessToken()`
/// para siempre y `pumpAndSettle` expira.
class FakeApiStorage extends SecureStorage {
  @override
  Future<String?> getAccessToken() async => 'test-token';

  @override
  Future<String?> getRefreshToken() async => null;
}

/// Red falsa para montar páginas que piden datos en `initState`.
///
/// Uso:
/// ```dart
/// final api = FakeApi((options) => jsonResponse(const []));
/// ... dioClientProvider.overrideWithValue(api.client)
/// ```
class FakeApi {
  FakeApi(this._handle);

  /// Responde lo mismo a cualquier ruta. Las rutas pedidas quedan en
  /// [requests] para poder assertearlas.
  final Future<ResponseBody> Function(RequestOptions options) _handle;

  final List<RequestOptions> requests = [];

  late final DioClient client = DioClient(FakeApiStorage(), adapter: _adapter);

  late final HttpClientAdapter _adapter = _CallbackAdapter((options) async {
    requests.add(options);
    return _handle(options);
  });

  /// Respuestas por ruta (prefijo más largo primero); lo no listado → `[]`.
  factory FakeApi.routes(Map<String, Object?> routes) {
    final sorted = routes.keys.toList()
      ..sort((a, b) => b.length.compareTo(a.length));
    return FakeApi((options) {
      final path = options.path;
      for (final key in sorted) {
        if (path.contains(key)) return jsonResponse(routes[key]);
      }
      return jsonResponse(const []);
    });
  }

  RequestOptions? requestTo(String fragment) => requests
      .where((r) => r.path.contains(fragment))
      .cast<RequestOptions?>()
      .firstOrNull;
}

/// `ResponseBody` JSON listo para un adapter falso.
Future<ResponseBody> jsonResponse(Object? data, {int status = 200}) async {
  return ResponseBody.fromString(
    jsonEncode(data),
    status,
    headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    },
  );
}

/// `ResponseBody` vacío (por ejemplo un DELETE).
Future<ResponseBody> emptyResponse({int status = 200}) async =>
    ResponseBody.fromBytes(Uint8List(0), status);

class _CallbackAdapter implements HttpClientAdapter {
  _CallbackAdapter(this._handle);

  final Future<ResponseBody> Function(RequestOptions options) _handle;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) =>
      _handle(options);

  @override
  void close({bool force = false}) {}
}
