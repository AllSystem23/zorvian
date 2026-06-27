import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../data/goal_repository.dart';
import '../../../core/entities/goal_definition.dart';
import '../../../core/entities/goal_assignment.dart';
import '../../../auth/auth_provider.dart';

final goalDefinitionsProvider = FutureProvider<List<GoalDefinition>>((ref) async {
  final repository = ref.watch(goalRepositoryProvider);
  return repository.getDefinitions();
});

final employeeGoalsProvider = FutureProvider<List<GoalAssignment>>((ref) async {
  final repository = ref.watch(goalRepositoryProvider);
  final auth = ref.watch(authProvider);
  
  // En un escenario real, el employeeId vendría del perfil del usuario logueado
  final employeeId = auth.userId ?? '00000000-0000-0000-0000-000000000000';
  return repository.getAssignments(employeeId);
});

final assignmentsByGoalProvider = FutureProvider.family<List<GoalAssignment>, String>((ref, goalId) async {
  final repository = ref.watch(goalRepositoryProvider);
  return repository.getAssignmentsByGoal(goalId);
});

final goalDashboardProvider = FutureProvider<Map<String, dynamic>>((ref) async {
  final repository = ref.watch(goalRepositoryProvider);
  return repository.getDashboard();
});

final incentivePaymentsProvider = FutureProvider<List<Map<String, dynamic>>>((ref) async {
  final repository = ref.watch(goalRepositoryProvider);
  final auth = ref.watch(authProvider);
  final employeeId = auth.userId ?? '00000000-0000-0000-0000-000000000000';
  return repository.getIncentivePayments(employeeId);
});

final goalStatsProvider = Provider((ref) {
  final dashboardAsync = ref.watch(goalDashboardProvider);
  return dashboardAsync.when(
    data: (data) => {
      'total': data['totalGoals'] ?? 0,
      'active': data['activeGoals'] ?? 0,
      'globalCompliance': data['globalCompliance'] ?? 0.0,
      'incentiveBudget': data['incentiveBudget'] ?? 0.0,
    },
    loading: () => {'total': 0, 'active': 0, 'globalCompliance': 0.0, 'incentiveBudget': 0.0},
    error: (_, _) => {'total': 0, 'active': 0, 'globalCompliance': 0.0, 'incentiveBudget': 0.0},
  );
});
