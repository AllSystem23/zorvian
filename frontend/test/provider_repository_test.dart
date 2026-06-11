import 'package:flutter_test/flutter_test.dart';
import 'package:dio/dio.dart';
import 'package:zorvian/features/providers/data/provider_repository.dart';
import 'package:zorvian/core/network/dio_client.dart';
import 'package:zorvian/core/entities/service_provider.dart';

class MockDioClient extends Fake implements DioClient {
  final Map<String, dynamic> responses;
  MockDioClient(this.responses);

  @override
  Future<Response<T>> get<T>(String path, {Map<String, dynamic>? params, Options? options}) async {
    if (responses.containsKey(path)) {
      return Response<T>(
        data: responses[path] as T,
        requestOptions: RequestOptions(path: path),
        statusCode: 200,
      );
    }
    throw DioException(requestOptions: RequestOptions(path: path), type: DioExceptionType.badResponse);
  }

  @override
  Future<Response<T>> post<T>(String path, {dynamic data, Map<String, dynamic>? queryParameters, Options? options}) async {
    return Response<T>(
      data: data as T,
      requestOptions: RequestOptions(path: path),
      statusCode: 200,
    );
  }
}

void main() {
  group('ProviderRepository Tests', () {
    test('getProviders returns list of ServiceProvider', () async {
      final mockData = [
        {
          'id': '1',
          'employeeId': 'emp-1',
          'businessName': 'Test Provider',
          'serviceCategory': 'Tech',
        }
      ];
      final mockDio = MockDioClient({'providers': mockData});
      final repository = ProviderRepository(mockDio);

      final result = await repository.getProviders();

      expect(result, isA<List<ServiceProvider>>());
      expect(result.length, 1);
      expect(result[0].businessName, 'Test Provider');
    });
  });
}
