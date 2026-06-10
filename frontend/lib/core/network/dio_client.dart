import 'package:dio/dio.dart';
import '../storage/secure_storage.dart';

typedef OnErrorCallback = void Function(int? statusCode, String message);
typedef OnUnauthorizedCallback = void Function();

class DioClient {
  late final Dio _dio;
  final SecureStorage _storage;
  final OnErrorCallback? onError;
  final OnUnauthorizedCallback? onUnauthorized;

  static String _normalizeBaseUrl(String url) =>
      url.endsWith('/') ? url : '$url/';

  DioClient(this._storage, {this.onError, this.onUnauthorized}) {
    _dio = Dio(BaseOptions(
      baseUrl: _normalizeBaseUrl(const String.fromEnvironment('API_URL', defaultValue: 'https://nexora-9yal.onrender.com/zorvian/v1')),
      connectTimeout: const Duration(seconds: 30),
      receiveTimeout: const Duration(seconds: 60),
      headers: {'Content-Type': 'application/json'},
    ));

    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        // No necesitamos token para el login
        if (options.path.contains('auth/login')) {
          return handler.next(options);
        }

        try {
          final token = await _storage.getAccessToken();
          if (token != null) {
            options.headers['Authorization'] = 'Bearer $token';
          }
        } catch (_) {
          // Si falla el almacenamiento, continuamos sin token
        }
        handler.next(options);
      },
      onError: (error, handler) async {
        if (error.response?.statusCode == 401) {
          final refreshed = await _tryRefreshToken();
          if (refreshed) {
            final retryResponse = await _dio.fetch(error.requestOptions);
            handler.resolve(retryResponse);
            return;
          }
          await _storage.clearTokens();
          onUnauthorized?.call();
        }
        final data = error.response?.data;
        final msg = data is Map
            ? (data['message'] as String? ??
                data['title'] as String? ??
                error.message ??
                'Error de conexión')
            : data?.toString() ?? error.message ?? 'Error de conexión';
        onError?.call(error.response?.statusCode, msg);
        handler.next(error);
      },
    ));
  }

  Future<bool> _tryRefreshToken() async {
    try {
      final refresh = await _storage.getRefreshToken();
      if (refresh == null) return false;

      print('DEBUG: Attempting refresh with token: $refresh');
      
      // Usamos baseUrl directamente para evitar concatenaciones mal formadas
      final response = await Dio(BaseOptions(
        connectTimeout: const Duration(seconds: 30),
        receiveTimeout: const Duration(seconds: 60),
      )).post(
        '${_dio.options.baseUrl}auth/refresh',
        data: {'refreshToken': refresh},
      );

      print('DEBUG: Refresh response: ${response.statusCode}');

      if (response.statusCode == 200) {
        await _storage.saveTokens(
          response.data['data']['accessToken'],
          response.data['data']['refreshToken'],
        );
        return true;
      }
    } catch (e) {
      if (e is DioException) {
        print('DEBUG: Refresh error: ${e.response?.statusCode} - ${e.response?.data}');
      }
    }
    return false;
  }

  Future<Response<T>> get<T>(String path, {Map<String, dynamic>? params, Options? options}) =>
      _dio.get<T>(path, queryParameters: params, options: options);

  Future<Response<T>> post<T>(String path, {dynamic data, Map<String, dynamic>? queryParameters, Options? options}) =>
      _dio.post<T>(path, data: data, queryParameters: queryParameters, options: options);

  Future<Response<T>> put<T>(String path, {dynamic data, Map<String, dynamic>? queryParameters}) =>
      _dio.put<T>(path, data: data, queryParameters: queryParameters);

  Future<Response<T>> patch<T>(String path, {dynamic data}) =>
      _dio.patch<T>(path, data: data);

  Future<Response<T>> delete<T>(String path) =>
      _dio.delete<T>(path);
}
