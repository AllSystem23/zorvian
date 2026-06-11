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

final goalDashboardStatsProvider = FutureProvider<List<Map<String, dynamic>>>((ref) async {
  final definitions = await ref.watch(goalDefinitionsProvider.future);
  final repository = ref.watch(goalRepositoryProvider);
  
  final stats = <Map<String, dynamic>>[];
  for (final def in definitions) {
    final assignments = await repository.getAssignmentsByGoal(def.id);
    final participants = assignments.length;
    final avgCompliance = assignments.isEmpty 
        ? 0.0 
        : assignments.map((a) => a.progressEntries.isNotEmpty ? a.progressEntries.last.compliancePercentage : 0.0).reduce((a, b) => a + b) / participants;
    
    stats.add({
      'definition': def,
      'participants': participants,
      'average': avgCompliance,
    });
  }
  return stats;
});

final incentivePaymentsProvider = FutureProvider<List<Map<String, dynamic>>>((ref) async {
  final repository = ref.watch(goalRepositoryProvider);
  final auth = ref.watch(authProvider);
  final employeeId = auth.userId ?? '00000000-0000-0000-0000-000000000000';
  return repository.getIncentivePayments(employeeId);
});

final goalStatsProvider = Provider((ref) {
  final assignments = ref.watch(employeeGoalsProvider).value ?? [];
  if (assignments.isEmpty) return {'total': 0, 'completed': 0, 'percentage': 0.0};
  
  final total = assignments.length;
  // Simulación de cálculo de cumplimiento
  final completed = assignments.where((a) => a.status == 'completed').length;
  final percentage = total > 0 ? (completed / total) : 0.0;
  
  return {
    'total': total,
    'completed': completed,
    'percentage': percentage,
  };
});
