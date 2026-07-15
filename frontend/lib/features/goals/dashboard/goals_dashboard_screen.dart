import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/goal_provider.dart';

class GoalsDashboardScreen extends ConsumerWidget {
  const GoalsDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dashboardAsync = ref.watch(goalDashboardProvider);

    return Scaffold(
      body: dashboardAsync.when(
        data: (data) {
          final globalCompliance = (data['globalCompliance'] as num?)?.toDouble() ?? 0.0;
          final incentiveBudget = (data['incentiveBudget'] as num?)?.toDouble() ?? 0.0;
          final goalStats = (data['goalStats'] as List?)?.cast<Map<String, dynamic>>() ?? [];
          final lowPerformers = (data['lowPerformers'] as List?)?.cast<Map<String, dynamic>>() ?? [];

          return SingleChildScrollView(
            padding: const EdgeInsets.all(ZSpacing.lg),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // KPIs de alto nivel
                Row(
                  children: [
                    Expanded(
                      child: ZStatCard(
                        title: 'Cumplimiento Global',
                        value: '${globalCompliance.toStringAsFixed(1)}%',
                      ),
                    ),
                    const SizedBox(width: ZSpacing.md),
                    Expanded(
                      child: ZStatCard(
                        title: 'Presupuesto Incentivos',
                        value: '\$${incentiveBudget.toStringAsFixed(0)}',
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: ZSpacing.xl),

                Text('Desempeño por Meta', style: ZTypography.titleLarge),
                const SizedBox(height: ZSpacing.md),

                if (goalStats.isEmpty)
                  const Center(
                    child: Padding(
                      padding: EdgeInsets.all(ZSpacing.xl),
                      child: Text('Sin datos para mostrar'),
                    ),
                  )
                else
                  ZDataTable<Map<String, dynamic>>(
                    columns: const [
                      ZColumn(id: 'goal', label: 'Meta'),
                      ZColumn(id: 'participants', label: 'Participantes'),
                      ZColumn(id: 'average', label: 'Promedio', numeric: true),
                      ZColumn(id: 'status', label: 'Estado'),
                    ],
                    rows: goalStats,
                    rowMapper: (s) {
                      return DataRow(cells: [
                        DataCell(Text(s['goalName'] as String? ?? '')),
                        DataCell(Text('${s['participants'] ?? 0}')),
                        DataCell(Text('${(s['averageCompliance'] as num?)?.toDouble().toStringAsFixed(1)}%')),
                        DataCell(ZBadge(
                          text: s['status'] as String? ?? '',
                          type: (s['status'] as String?) == 'active' ? ZBadgeType.success : ZBadgeType.neutral,
                        )),
                      ]);
                    },
                  ),

                const SizedBox(height: ZSpacing.xl),

                Text('Alertas de Bajo Rendimiento', style: ZTypography.titleLarge),
                const SizedBox(height: ZSpacing.md),

                if (lowPerformers.isEmpty)
                  const Center(
                    child: Padding(
                      padding: EdgeInsets.all(ZSpacing.xl),
                      child: Text('Sin alertas de bajo rendimiento'),
                    ),
                  )
                else
                  ...lowPerformers.map((lp) => Padding(
                    padding: const EdgeInsets.only(bottom: ZSpacing.sm),
                    child: ZAlertCard(
                      message: "${lp['employeeName']} está por debajo del 50% de cumplimiento en la meta de \"${lp['goalName']}\".",
                      severity: 'medium',
                    ),
                  )),
              ],
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (err, _) => Center(
          child: Padding(
            padding: const EdgeInsets.all(ZSpacing.xl),
            child: ZAlertCard(
              message: 'Error al cargar datos del dashboard: $err',
              severity: 'high',
            ),
          ),
        ),
      ),
    );
  }
}
