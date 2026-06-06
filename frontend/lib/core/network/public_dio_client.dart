import 'package:dio/dio.dart';

class PublicDioClient {
  final Dio _dio;

  PublicDioClient() : _dio = Dio(BaseOptions(baseUrl: 'http://localhost:5000')); // Adjust base URL as needed

  Future<Response> get(String path) async {
    return await _dio.get(path);
  }
}
