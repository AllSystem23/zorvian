import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../features/bi/providers/bi_provider.dart';
import '../../../../shared/ds/ds.dart';

class PurchaseRecommendationSection extends ConsumerWidget {
  const PurchaseRecommendationSection({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final asyncRecs = ref.watch(purchaseRecommendationProvider);
    final palette = Theme.of(context).colorScheme;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Recomendaciones de Compra', style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 8),
        asyncRecs.when(
          loading: () => const SizedBox(height: 120, child: Center(child: CircularProgressIndicator())),
          error: (e, _) => ZCard(padding: const EdgeInsets.all(16), child: Text('Error: $e')),
          data: (summary) {
            final items = summary.recommendations;
            return Column(
              children: [
                LayoutBuilder(
                  builder: (_, constraints) => Wrap(
                    spacing: 12, runSpacing: 12,
                    children: [
                      _badge(context, 'Críticos', '${summary.criticalCount}', palette.error, (constraints.maxWidth - 36) / 3),
                      _badge(context, 'Advertencia', '${summary.warningCount}', palette.tertiary, (constraints.maxWidth - 36) / 3),
                      _badge(context, 'Saludables', '${summary.healthyCount}', palette.primary, (constraints.maxWidth - 36) / 3),
                    ],
                  ),
                ),
                const SizedBox(height: 8),
                if (summary.totalRecommendedCost > 0)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: Text('Costo total recomendado: \$${summary.totalRecommendedCost.toStringAsFixed(0)}',
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(color: palette.onSurfaceVariant)),
                  ),
                if (items.isEmpty)
                  ZCard(padding: const EdgeInsets.all(16), child: Text('No hay productos por recomendar.', style: Theme.of(context).textTheme.bodyMedium)),
                ...items.take(10).map((item) => _recommendationTile(context, item)),
                if (items.length > 10)
                  Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: Text('+${items.length - 10} más...', style: Theme.of(context).textTheme.bodySmall?.copyWith(color: palette.onSurfaceVariant)),
                  ),
              ],
            );
          },
        ),
      ],
    );
  }

  Widget _badge(BuildContext context, String label, String value, Color color, double width) {
    return SizedBox(
      width: width,
      child: ZCard(
        padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 8),
        child: Column(
          children: [
            Text(value, style: Theme.of(context).textTheme.titleLarge?.copyWith(color: color, fontWeight: FontWeight.bold)),
            Text(label, style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
      ),
    );
  }

  Widget _recommendationTile(BuildContext context, dynamic item) {
    final palette = Theme.of(context).colorScheme;
    final isCritical = item.priority == 'critical';
    final color = isCritical ? palette.error : palette.tertiary;

    return ZCard(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(item.productName, style: Theme.of(context).textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
                Text('${item.productCode}  •  Stock: ${item.currentStock}  •  Mín: ${item.minStock}',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(color: palette.onSurfaceVariant)),
                Text('Demanda diaria: ${item.averageDailyDemand}  •  Días hasta agotar: ${item.daysUntilStockout == -1 ? "N/A" : item.daysUntilStockout.toStringAsFixed(1)}',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(color: palette.onSurfaceVariant)),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text('Recomendado:', style: Theme.of(context).textTheme.bodySmall?.copyWith(color: palette.onSurfaceVariant)),
              Text('${item.recommendedQuantity} uds', style: Theme.of(context).textTheme.titleMedium?.copyWith(color: color, fontWeight: FontWeight.bold)),
              Text('\$${(item.recommendedQuantity * item.costPrice).toStringAsFixed(0)}',
                style: Theme.of(context).textTheme.bodySmall?.copyWith(color: palette.onSurfaceVariant)),
            ],
          ),
        ],
      ),
    );
  }
}
