import 'package:dio/dio.dart';
import 'api_config.dart';

class PublicDioClient {
  final Dio _dio;

  PublicDioClient() : _dio = Dio(BaseOptions(baseUrl: '${ApiConfig.baseUrl}/'));

  Future<Response> get(String path) async {
    return await _dio.get(path);
  }
}
