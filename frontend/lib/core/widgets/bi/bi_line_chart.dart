import 'package:flutter/material.dart';
import 'package:fl_chart/fl_chart.dart';

class BiLineChart extends StatelessWidget {
  final List<LineChartSeries> series;
  final String? title;
  final double height;

  const BiLineChart({super.key, required this.series, this.title, this.height = 200});

  @override
  Widget build(BuildContext context) {
    if (series.isEmpty || series.every((s) => s.spots.isEmpty)) return const SizedBox.shrink();
    final allSpots = series.expand((s) => s.spots).toList();
    final minY = allSpots.map((e) => e.y).reduce((a, b) => a < b ? a : b);
    final maxY = allSpots.map((e) => e.y).reduce((a, b) => a > b ? a : b);
    final range = maxY - minY;

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
              child: LineChart(
                LineChartData(
                  minY: minY - range * 0.1,
                  maxY: maxY + range * 0.1,
                  gridData: FlGridData(show: true, drawVerticalLine: false, horizontalInterval: range > 0 ? range / 4 : 1),
                  titlesData: FlTitlesData(
                    show: true,
                    bottomTitles: AxisTitles(sideTitles: SideTitles(showTitles: true, getTitlesWidget: (v, _) {
                      if (v.toInt() < 0 || v.toInt() >= (series.isNotEmpty ? series.first.spots.length : 0)) return const SizedBox();
                      final s = series.first.spots[v.toInt()];
                      return Padding(padding: const EdgeInsets.only(top: 4), child: Text(s.x.toStringAsFixed(0), style: const TextStyle(fontSize: 9)));
                    }, reservedSize: 20)),
                    leftTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                    topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                    rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                  ),
                  borderData: FlBorderData(show: false),
                  lineBarsData: series.map((s) => LineChartBarData(
                    spots: s.spots,
                    isCurved: true,
                    color: s.color,
                    barWidth: 2,
                    dotData: const FlDotData(show: true),
                    belowBarData: BarAreaData(show: s.showArea, color: s.color.withAlpha(20)),
                  )).toList(),
                  lineTouchData: LineTouchData(
                    enabled: true,
                    touchTooltipData: LineTouchTooltipData(
                      getTooltipItems: (touchedSpots) => touchedSpots.map((s) => LineTooltipItem(
                        '\$${s.y.toStringAsFixed(0)}',
                        TextStyle(color: s.bar.color ?? Colors.black, fontSize: 10),
                      )).toList(),
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class LineChartSeries {
  final List<FlSpot> spots;
  final Color color;
  final bool showArea;
  LineChartSeries(this.spots, {this.color = Colors.blue, this.showArea = false});
}
