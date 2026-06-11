import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/goal_provider.dart';
import '../../../core/entities/goal_definition.dart';

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
              Row(
                children: [
                  Expanded(
                    child: ZStatCard(
                      title: 'Cumplimiento Global',
                      value: '78.4%',
                    ),
                  ),
                  const SizedBox(width: ZSpacing.md),
                  Expanded(
                    child: ZStatCard(
                      title: 'Presupuesto Incentivos',
                      value: '\$45,200',
                    ),
                  ),
                ],
              ),
            const SizedBox(height: ZSpacing.xl),
            
            Text('Desempeño por Meta', style: ZTypography.titleLarge),
            const SizedBox(height: ZSpacing.md),
            
            statsAsync.when(
              data: (stats) => ZDataTable<Map<String, dynamic>>(
                columns: const [
                  DataColumn(label: Text('Meta')),
                  DataColumn(label: Text('Participantes')),
                  DataColumn(label: Text('Promedio')),
                  DataColumn(label: Text('Estado')),
                ],
                rows: stats,
                rowMapper: (s) {
                  final def = s['definition'] as GoalDefinition;
                  return DataRow(cells: [
                    DataCell(Text(def.name)),
                    DataCell(Text(s['participants'].toString())),
                    DataCell(Text('${(s['average'] as double).toStringAsFixed(1)}%')),
                    DataCell(ZBadge(text: def.status, type: def.status == 'active' ? ZBadgeType.success : ZBadgeType.neutral)),
                  ]);
                },
              ),
              loading: () => const ZSkeleton(height: 300),
              error: (err, _) => ZAlertCard(
                message: 'Error al cargar datos del dashboard: $err',
                severity: 'high',
              ),
            ),
            
            const SizedBox(height: ZSpacing.xl),
            
            Text('Alertas de Bajo Rendimiento', style: ZTypography.titleLarge),
            const SizedBox(height: ZSpacing.md),
            const ZAlertCard(
              message: '3 empleados están por debajo del 50% de cumplimiento en la meta de "Nuevos Clientes".',
              severity: 'medium',
            ),
          ],
        ),
      ),
    );
  }
}
