import 'package:flutter_test/flutter_test.dart';
import 'package:dio/dio.dart';
import 'package:zorvian/features/goals/data/goal_repository.dart';
import 'package:zorvian/core/network/dio_client.dart';
import 'package:zorvian/core/entities/goal_definition.dart';

// Mock manual de DioClient
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
  group('GoalRepository Tests', () {
    test('getDefinitions returns list of GoalDefinition', () async {
      final mockData = [
        {
          'id': '1',
          'name': 'Test Goal',
          'goalType': 'Sales',
          'dataSource': 'Manual',
        }
      ];
      final mockDio = MockDioClient({'goals/definitions': mockData});
      final repository = GoalRepository(mockDio);

      final result = await repository.getDefinitions();

      expect(result, isA<List<GoalDefinition>>());
      expect(result.length, 1);
      expect(result[0].name, 'Test Goal');
    });

    test('createDefinition calls post and returns GoalDefinition', () async {
      final mockDio = MockDioClient({});
      final repository = GoalRepository(mockDio);
      final newGoal = GoalDefinition(
        id: '',
        name: 'New Goal',
        goalType: 'Service',
        dataSource: 'System',
      );

      final result = await repository.createDefinition(newGoal);

      expect(result.name, 'New Goal');
      expect(result.goalType, 'Service');
    });
  });
}
