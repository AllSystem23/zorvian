import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:fl_chart/fl_chart.dart';

class BiPieChart extends StatelessWidget {
  final List<PieChartItem> items;
  final String? title;
  final double size;

  const BiPieChart({super.key, required this.items, this.title, this.size = 160});

  @override
  Widget build(BuildContext context) {
    // fl_chart lanza excepción cuando todas las secciones valen 0 (total = 0).
    final hasValue = items.any((e) => e.value > 0);
    if (items.isEmpty || !hasValue) return const SizedBox.shrink();
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: LayoutBuilder(builder: (context, constraints) {
          // La dona nunca puede ser más ancha que la tarjeta que la contiene.
          final chartSize = math.min(size, constraints.maxWidth);
          final pie = SizedBox(
            width: chartSize,
            height: chartSize,
            child: PieChart(PieChartData(
              sections: items
                  .map((e) => PieChartSectionData(
                        value: e.value,
                        title: '${e.value.toStringAsFixed(0)}%',
                        color: e.color,
                        radius: chartSize / 2 - 10,
                        titleStyle: const TextStyle(fontSize: 10, color: Colors.white),
                      ))
                  .toList(),
              centerSpaceRadius: 20,
              sectionsSpace: 2,
            )),
          );

          final legend = Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: items
                .map((e) => Padding(
                      padding: const EdgeInsets.symmetric(vertical: 2),
                      child: Row(children: [
                        Container(
                          width: 10,
                          height: 10,
                          decoration: BoxDecoration(
                            color: e.color,
                            borderRadius: BorderRadius.circular(2),
                          ),
                        ),
                        const SizedBox(width: 6),
                        Expanded(
                          child: Text(
                            e.label,
                            style: const TextStyle(fontSize: 11),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        const SizedBox(width: 6),
                        Text(
                          '${e.value.toStringAsFixed(1)}%',
                          style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w500),
                        ),
                      ]),
                    ))
                .toList(),
          );

          // En tarjetas angostas (teléfono, o un dashboard a 2 columnas) la
          // leyenda baja debajo del gráfico: junto a una dona de 160 px no
          // quedan más de ~30 px para el texto y desborda.
          final stacked = constraints.maxWidth < size + 140;
          return Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              if (title != null)
                Text(title!,
                    style: Theme.of(context)
                        .textTheme
                        .titleSmall
                        ?.copyWith(fontWeight: FontWeight.w600)),
              const SizedBox(height: 8),
              if (stacked) ...[
                Center(child: pie),
                const SizedBox(height: 12),
                legend,
              ] else
                Row(
                  children: [
                    pie,
                    const SizedBox(width: 12),
                    Expanded(child: legend),
                  ],
                ),
            ],
          );
        }),
      ),
    );
  }
}

class PieChartItem {
  final String label;
  final double value;
  final Color color;
  PieChartItem(this.label, this.value, {this.color = Colors.blue});
}
