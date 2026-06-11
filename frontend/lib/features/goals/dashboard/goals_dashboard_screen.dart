import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/goal_provider.dart';

class GoalsDashboardScreen extends ConsumerWidget {
  const GoalsDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final statsAsync = ref.watch(goalDashboardStatsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Dashboard de Cumplimiento')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // KPIs de alto nivel
            const Row(
              children: [
                Expanded(
                  child: ZStatCard(
                    label: 'Cumplimiento Global',
                    value: '78.4%',
                  ),
                ),
                const SizedBox(width: ZSpacing.md),
                Expanded(
                  child: ZStatCard(
                    label: 'Presupuesto Incentivos',
                    value: '\$45,200',
                  ),
                ),
              ],
            ),
            const SizedBox(height: ZSpacing.xl),
            
            Text('Desempeño por Meta', style: ZTypography.titleLarge),
            const SizedBox(height: ZSpacing.md),
            
            statsAsync.when(
              data: (stats) => ZDataTable(
                columns: const ['Meta', 'Participantes', 'Promedio', 'Estado'],
                rows: stats.map((s) {
                  final def = s['definition'] as GoalDefinition;
                  return {
                    'Meta': def.name,
                    'Participantes': s['participants'].toString(),
                    'Promedio': '${(s['average'] as double).toStringAsFixed(1)}%',
                    'Estado': ZBadge(label: def.status, isSuccess: def.status == 'active'),
                  };
                }).toList(),
              ),
              loading: () => const ZSkeleton(height: 300),
              error: (err, _) => ZAlertCard(
                title: 'Error',
                message: 'Error al cargar datos del dashboard: $err',
                isError: true,
              ),
            ),
            
            const SizedBox(height: ZSpacing.xl),
            
            Text('Alertas de Bajo Rendimiento', style: ZTypography.titleLarge),
            const SizedBox(height: ZSpacing.md),
            const ZAlertCard(
              title: 'Atención Requerida',
              message: '3 empleados están por debajo del 50% de cumplimiento en la meta de "Nuevos Clientes".',
              isWarning: true,
            ),
          ],
        ),
      ),
    );
  }
}
