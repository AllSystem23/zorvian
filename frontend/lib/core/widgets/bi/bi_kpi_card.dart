import 'package:flutter/material.dart';
import 'package:fl_chart/fl_chart.dart';

class BiKpiCard extends StatelessWidget {
  final String label;
  final String value;
  final double? changePercent;
  final IconData icon;
  final Color color;
  final List<double>? sparklineData;
  final VoidCallback? onTap;

  const BiKpiCard({
    super.key,
    required this.label,
    required this.value,
    this.changePercent,
    required this.icon,
    required this.color,
    this.sparklineData,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isPositive = changePercent != null && changePercent! >= 0;
    return Card(
      margin: const EdgeInsets.all(4),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                children: [
                  Icon(icon, size: 20, color: color),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(label, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey[600]), overflow: TextOverflow.ellipsis),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              Text(value, style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)),
              if (changePercent != null)
                Row(
                  children: [
                    Icon(isPositive ? Icons.trending_up : Icons.trending_down, size: 14, color: isPositive ? Colors.green : Colors.red),
                    const SizedBox(width: 2),
                    Text('${isPositive ? '+' : ''}${changePercent!.toStringAsFixed(1)}%',
                        style: TextStyle(fontSize: 11, color: isPositive ? Colors.green : Colors.red)),
                  ],
                ),
              if (sparklineData != null && sparklineData!.length > 1) ...[
                const SizedBox(height: 6),
                SizedBox(height: 28, child: _Sparkline(data: sparklineData!, color: color)),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _Sparkline extends StatelessWidget {
  final List<double> data;
  final Color color;
  const _Sparkline({required this.data, required this.color});

  @override
  Widget build(BuildContext context) {
    return LineChart(
      LineChartData(
        gridData: const FlGridData(show: false),
        titlesData: const FlTitlesData(show: false),
        borderData: FlBorderData(show: false),
        lineBarsData: [
          LineChartBarData(
            spots: data.asMap().entries.map((e) => FlSpot(e.key.toDouble(), e.value)).toList(),
            isCurved: true,
            color: color,
            barWidth: 1.5,
            dotData: const FlDotData(show: false),
            belowBarData: BarAreaData(show: true, color: color.withAlpha(30)),
          ),
        ],
      ),
      duration: Duration.zero,
    );
  }
}
