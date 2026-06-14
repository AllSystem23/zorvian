import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/network/dio_client.dart';
import '../../../core/entities/goal_definition.dart';
import '../../../core/entities/goal_assignment.dart';
import '../../../core/entities/goal_progress.dart';

class GoalRepository {
  final DioClient _apiClient;

  GoalRepository(this._apiClient);

  Future<List<GoalDefinition>> getDefinitions() async {
    final response = await _apiClient.get('goals/definitions');
    return (response.data as List).map((e) => GoalDefinition.fromJson(e)).toList();
  }

  Future<List<GoalAssignment>> getAssignments(String employeeId) async {
    final response = await _apiClient.get('goals/assignments', params: {'employeeId': employeeId});
    return (response.data as List).map((e) => GoalAssignment.fromJson(e)).toList();
  }

  Future<List<GoalAssignment>> getAssignmentsByGoal(String goalId) async {
    final response = await _apiClient.get('goals/assignments', params: {'goalId': goalId});
    return (response.data as List).map((e) => GoalAssignment.fromJson(e)).toList();
  }

  Future<void> recordProgress(GoalProgress progress) async {
    await _apiClient.post('goals/progress', data: progress.toJson());
  }

  Future<GoalDefinition> createDefinition(GoalDefinition definition) async {
    final response = await _apiClient.post('goals/definitions', data: definition.toJson());
    return GoalDefinition.fromJson(response.data);
  }

  Future<List<Map<String, dynamic>>> getIncentives() async {
    final response = await _apiClient.get('goals/incentives');
    return (response.data as List).cast<Map<String, dynamic>>();
  }

  Future<List<Map<String, dynamic>>> getIncentivePayments(String employeeId) async {
    final response = await _apiClient.get('goals/incentive-payments', params: {'employeeId': employeeId});
    return (response.data as List).cast<Map<String, dynamic>>();
  }
}

final goalRepositoryProvider = Provider<GoalRepository>((ref) {
  final dio = ref.watch(dioClientProvider);
  return GoalRepository(dio);
});
