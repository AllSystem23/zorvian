import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../features/bi/providers/bi_provider.dart';
import '../../../../shared/ds/ds.dart';

class AnomalyDetectionSection extends ConsumerWidget {
  const AnomalyDetectionSection({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final asyncAnomalies = ref.watch(accountingAnomaliesProvider);
    final palette = Theme.of(context).colorScheme;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Detección de Anomalías Contables', style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 8),
        asyncAnomalies.when(
          loading: () => const SizedBox(height: 80, child: Center(child: CircularProgressIndicator())),
          error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
          data: (report) {
            if (report.anomalies.isEmpty) {
              return ZCard(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    Icon(Icons.check_circle, color: palette.primary),
                    const SizedBox(width: 12),
                    Text('No se detectaron anomalías en los últimos ${report.totalEntriesAnalyzed} asientos contables.'),
                  ],
                ),
              );
            }

            return Column(
              children: [
                ZCard(
                  padding: const EdgeInsets.all(12),
                  child: Row(
                    children: [
                      Icon(Icons.warning_amber_rounded, color: palette.error, size: 28),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('${report.anomalyCount} anomalía(s) detectada(s)', style: Theme.of(context).textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
                            Text('${report.totalEntriesAnalyzed} asientos analizados', style: Theme.of(context).textTheme.bodySmall?.copyWith(color: palette.onSurfaceVariant)),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 8),
                ...report.anomalies.take(10).map((a) {
                  final isCritical = a.severity == 'critical';
                  return ZCard(
                    margin: const EdgeInsets.only(bottom: 6),
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Icon(isCritical ? Icons.error : Icons.warning, size: 18, color: isCritical ? palette.error : palette.tertiary),
                            const SizedBox(width: 8),
                            Text(a.entryNumber, style: Theme.of(context).textTheme.bodySmall?.copyWith(fontWeight: FontWeight.w600)),
                            const Spacer(),
                            Text(a.entryDate.substring(0, 10), style: Theme.of(context).textTheme.bodySmall),
                          ],
                        ),
                        const SizedBox(height: 4),
                        Text(a.detail, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: palette.onSurfaceVariant)),
                      ],
                    ),
                  );
                }),
                if (report.anomalies.length > 10)
                  Padding(
                    padding: const EdgeInsets.only(top: 4),
                    child: Text('+${report.anomalies.length - 10} más...', style: Theme.of(context).textTheme.bodySmall?.copyWith(color: palette.onSurfaceVariant)),
                  ),
              ],
            );
          },
        ),
      ],
    );
  }
}
