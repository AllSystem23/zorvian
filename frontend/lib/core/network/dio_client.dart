import 'package:dio/dio.dart';
import '../storage/secure_storage.dart';

typedef OnErrorCallback = void Function(int? statusCode, String message);

class DioClient {
  late final Dio _dio;
  final SecureStorage _storage;
  final OnErrorCallback? onError;

  DioClient(this._storage, {this.onError}) {
    _dio = Dio(BaseOptions(
      baseUrl: const String.fromEnvironment(
        'API_URL',
        defaultValue: 'https://nexora-9yal.onrender.com/zorvian/v1/',
      ),
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 10),
      headers: {'Content-Type': 'application/json'},
    ));

    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        // No necesitamos token para el login
        if (options.path.contains('auth/login')) {
          return handler.next(options);
        }

        try {
          // Añadimos un timeout corto para evitar bloqueos en web
          final token = await _storage.getAccessToken().timeout(
            const Duration(milliseconds: 500),
            onTimeout: () => null,
          );
          print('DEBUG: Request path: ${options.path}');
          print('DEBUG: Headers: ${options.headers}');
          print('DEBUG: Token retrieved: ${token != null ? 'Yes' : 'No'}');
          if (token != null) {
            options.headers['Authorization'] = 'Bearer $token';
            print('DEBUG: Authorization header added.');
          } else {
            print('DEBUG: No token found to add.');
          }
        } catch (e) {
          print('DEBUG: Token retrieval error: $e');
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
        }
        final msg = error.response?.data?['message'] as String? ??
            error.response?.data?['title'] as String? ??
            error.message ??
            'Error de conexión';
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
        connectTimeout: const Duration(seconds: 10),
        receiveTimeout: const Duration(seconds: 10),
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

  Future<Response<T>> get<T>(String path, {Map<String, dynamic>? params}) =>
      _dio.get<T>(path, queryParameters: params);

  Future<Response<T>> post<T>(String path, {dynamic data, Options? options}) =>
      _dio.post<T>(path, data: data, options: options);

  Future<Response<T>> put<T>(String path, {dynamic data}) =>
      _dio.put<T>(path, data: data);

  Future<Response<T>> delete<T>(String path) =>
      _dio.delete<T>(path);
}
