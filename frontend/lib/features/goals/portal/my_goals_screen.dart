import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/goal_provider.dart';

class MyGoalsScreen extends ConsumerWidget {
  const MyGoalsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final goalsAsync = ref.watch(employeeGoalsProvider);
    final stats = ref.watch(goalStatsProvider);

    return Scaffold(
      body: goalsAsync.when(
        data: (goals) => SingleChildScrollView(
          padding: const EdgeInsets.all(ZSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Resumen de estadísticas
              Row(
                children: [
                  Expanded(
                    child: ZStatCard(
                      title: 'Progreso Total',
                      value: '${(stats['percentage'] as double) * 100}%',
                    ),
                  ),
                  const SizedBox(width: ZSpacing.md),
                  Expanded(
                    child: ZStatCard(
                      title: 'Metas Activas',
                      value: stats['total'].toString(),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: ZSpacing.xl),
              Text('Tus Objetivos', style: ZTypography.titleLarge),
              const SizedBox(height: ZSpacing.md),
              if (goals.isEmpty)
                const ZEmptyState(
                  icon: Icons.flag_outlined,
                  title: 'Sin metas asignadas',
                  subtitle: 'Aún no tienes objetivos para este período.',
                )
              else
                ListView.separated(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: goals.length,
                  separatorBuilder: (_, _) => const SizedBox(height: ZSpacing.md),
                  itemBuilder: (context, index) {
                    final goal = goals[index];
                    // Calculamos el progreso promedio de las entradas
                    final latestProgress = 0.0;

                    return ZCard(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Expanded(
                                child: Text(
                                  goal.goalDefinition?.name ?? 'Meta sin nombre',
                                  style: ZTypography.titleMedium,
                                ),
                              ),
                              ZBadge(
                                text: goal.status,
                                type: goal.status == 'active' ? ZBadgeType.success : ZBadgeType.neutral,
                              ),
                            ],
                          ),
                          const SizedBox(height: ZSpacing.sm),
                          Text(
                            'Objetivo: ${goal.targetValue} ${goal.goalDefinition?.metricType ?? ''}',
                            style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500),
                          ),
                          const SizedBox(height: ZSpacing.md),
                          ZProgress(label: 'Progreso', value: latestProgress),
                          const SizedBox(height: ZSpacing.xs),
                          Align(
                            alignment: Alignment.centerRight,
                            child: Text(
                              '${(latestProgress * 100).toStringAsFixed(1)}%',
                              style: ZTypography.labelSmall.copyWith(fontWeight: FontWeight.bold),
                            ),
                          ),
                          if (goal.expirationDate != null) ...[
                            const Divider(height: ZSpacing.lg),
                            Row(
                              children: [
                                const Icon(Icons.calendar_today, size: 14, color: ZColors.neutral400),
                                const SizedBox(width: ZSpacing.xs),
                                Text(
                                  'Vence: ${goal.expirationDate.toString().split(' ')[0]}',
                                  style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500),
                                ),
                              ],
                            ),
                          ],
                        ],
                      ),
                    );
                  },
                ),
            ],
          ),
        ),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (err, _) => Center(
          child: ZAlertCard(
            message: 'No pudimos cargar tus metas. $err',
            severity: 'high',
          ),
        ),
      ),
    );
  }
}
