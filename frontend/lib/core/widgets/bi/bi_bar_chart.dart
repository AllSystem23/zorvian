import 'package:flutter/material.dart';
import 'package:fl_chart/fl_chart.dart';

class BiBarChart extends StatelessWidget {
  final List<BarChartItem> items;
  final String? title;
  final bool horizontal;
  final double height;

  const BiBarChart({super.key, required this.items, this.title, this.horizontal = false, this.height = 200});

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) return const SizedBox.shrink();
    final maxVal = items.map((e) => e.value).reduce((a, b) => a > b ? a : b);
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (title != null) Text(title!, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            SizedBox(
              height: height,
              child: horizontal ? _HorizontalBar(items, maxVal) : _VerticalBar(items, maxVal),
            ),
          ],
        ),
      ),
    );
  }
}

class BarChartItem {
  final String label;
  final double value;
  final Color color;
  BarChartItem(this.label, this.value, {this.color = Colors.blue});
}

class _VerticalBar extends StatelessWidget {
  final List<BarChartItem> items;
  final double maxVal;
  const _VerticalBar(this.items, this.maxVal);

  @override
  Widget build(BuildContext context) => BarChart(
        BarChartData(
          alignment: BarChartAlignment.center,
          maxY: maxVal * 1.15,
          barTouchData: BarTouchData(
            enabled: true,
            touchTooltipData: BarTouchTooltipData(
              getTooltipItem: (group, groupIndex, rod, rodIndex) => BarTooltipItem(
                '${items[group.x].label}: \$${rod.toY.toStringAsFixed(0)}',
                const TextStyle(fontSize: 10),
              ),
            ),
          ),
          gridData: const FlGridData(show: false),
          titlesData: FlTitlesData(
            show: true,
            bottomTitles: AxisTitles(sideTitles: SideTitles(showTitles: true, getTitlesWidget: (v, _) => v.toInt() < items.length ? Padding(padding: const EdgeInsets.only(top: 4), child: Text(items[v.toInt()].label, style: const TextStyle(fontSize: 9))) : const SizedBox(), reservedSize: 28)),
            leftTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
          ),
          borderData: FlBorderData(show: false),
          barGroups: items.asMap().entries.map((e) => BarChartGroupData(x: e.key, barRods: [BarChartRodData(toY: e.value.value, color: e.value.color, width: 16, borderRadius: const BorderRadius.only(topLeft: Radius.circular(4), topRight: Radius.circular(4)))])).toList(),
        ),
      );
}

class _HorizontalBar extends StatelessWidget {
  final List<BarChartItem> items;
  final double maxVal;
  const _HorizontalBar(this.items, this.maxVal);

  @override
  Widget build(BuildContext context) => BarChart(
        BarChartData(
          alignment: BarChartAlignment.center,
          maxY: maxVal * 1.15,
          barTouchData: BarTouchData(
            enabled: true,
            touchTooltipData: BarTouchTooltipData(
              getTooltipItem: (group, groupIndex, rod, rodIndex) => BarTooltipItem(
                '${items[group.x].label}: \$${rod.toY.toStringAsFixed(0)}',
                const TextStyle(fontSize: 10),
              ),
            ),
          ),
          gridData: const FlGridData(show: false),
          titlesData: FlTitlesData(
            show: true,
            bottomTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            leftTitles: AxisTitles(sideTitles: SideTitles(showTitles: true, getTitlesWidget: (v, _) => v.toInt() < items.length ? Padding(padding: const EdgeInsets.only(right: 4), child: Text(items[v.toInt()].label, style: const TextStyle(fontSize: 9))) : const SizedBox(), reservedSize: 80)),
            topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
          ),
          borderData: FlBorderData(show: false),
          barGroups: items.asMap().entries.map((e) => BarChartGroupData(x: e.key, barRods: [BarChartRodData(toY: e.value.value, color: e.value.color, width: 16, borderRadius: const BorderRadius.only(topLeft: Radius.circular(4), topRight: Radius.circular(4)))])).toList(),
        ),
      );
}
