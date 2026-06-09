import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

/// Custom Dio interceptors for Zorvian ERP API
class ZorvianInterceptors extends InterceptorsWrapper {
  ZorvianInterceptors({required this.onUnauthorized, required this.onErrorCallback});

  final VoidCallback onUnauthorized;
  final void Function(int statusCode, String message) onErrorCallback;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    // Add request timestamp for performance logging
    options.headers['X-Request-Timestamp'] = DateTime.now().toIso8601String();

    // Add correlation ID for tracing
    options.headers['X-Correlation-Id'] = _generateCorrelationId();

    if (kDebugMode) {
      print('[API] ${options.method} ${options.uri}');
    }

    handler.next(options);
  }

  @override
  void onResponse(Response response, ResponseInterceptorHandler handler) {
    if (kDebugMode) {
      print('[API] ${response.statusCode} ${response.requestOptions.method} ${response.requestOptions.uri}');
    }

    handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    final statusCode = err.response?.statusCode ?? 0;
    final message = _extractErrorMessage(err);

    if (kDebugMode) {
      print('[API ERROR] $statusCode $message');
    }

    // Handle specific error codes
    switch (statusCode) {
      case 401:
        onUnauthorized();
        break;
      case 403:
        onErrorCallback(403, 'No tienes permiso para realizar esta acción.');
        break;
      case 404:
        onErrorCallback(404, 'Recurso no encontrado.');
        break;
      case 429:
        final retryAfter = err.response?.headers['Retry-After']?.first;
        onErrorCallback(429, 'Demasiadas solicitudes. ${retryAfter != null ? "Intenta en ${retryAfter}s" : "Intenta más tarde"}.');
        break;
      case 500:
        onErrorCallback(500, 'Error del servidor. Intenta más tarde.');
        break;
      case 502:
      case 503:
        onErrorCallback(statusCode, 'Servicio temporalmente no disponible.');
        break;
      default:
        onErrorCallback(statusCode, message);
    }

    handler.next(err);
  }

  String _extractErrorMessage(DioException err) {
    try {
      final data = err.response?.data;
      if (data is Map<String, dynamic>) {
        return data['error']?['message']?.toString()
            ?? data['message']?.toString()
            ?? 'Error desconocido';
      }
    } catch (_) {}
    return err.message ?? 'Error de conexión';
  }

  String _generateCorrelationId() {
    final now = DateTime.now().millisecondsSinceEpoch;
    final random = (now * 1000 + (now % 10000)).toRadixString(36);
    return 'req_$random';
  }
}