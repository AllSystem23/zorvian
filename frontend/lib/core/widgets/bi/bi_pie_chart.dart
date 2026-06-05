import 'package:flutter/material.dart';
import 'package:fl_chart/fl_chart.dart';

class BiPieChart extends StatelessWidget {
  final List<PieChartItem> items;
  final String? title;
  final double size;

  const BiPieChart({super.key, required this.items, this.title, this.size = 160});

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) return const SizedBox.shrink();
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (title != null) Text(title!, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            Row(
              children: [
                SizedBox(width: size, height: size, child: PieChart(PieChartData(sections: items.map((e) => PieChartSectionData(value: e.value, title: '${e.value.toStringAsFixed(0)}%', color: e.color, radius: size / 2 - 10, titleStyle: const TextStyle(fontSize: 10, color: Colors.white))).toList(), centerSpaceRadius: 20, sectionsSpace: 2))),
                const SizedBox(width: 12),
                Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, mainAxisSize: MainAxisSize.min, children: items.map((e) => Padding(padding: const EdgeInsets.symmetric(vertical: 2), child: Row(children: [Container(width: 10, height: 10, decoration: BoxDecoration(color: e.color, borderRadius: BorderRadius.circular(2))), const SizedBox(width: 6), Expanded(child: Text(e.label, style: const TextStyle(fontSize: 11))), Text('${e.value.toStringAsFixed(1)}%', style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w500))]))).toList())),
              ],
            ),
          ],
        ),
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
