import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/attendance_provider.dart';
import '../../../shared/ds/ds.dart';

class AttendanceHistoryPage extends ConsumerWidget {
  const AttendanceHistoryPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final asyncState = ref.watch(attendanceProvider);

    return Scaffold(
      body: ZAsyncRenderer<AttendanceSummary>(
        value: asyncState,
        builder: (summary) => ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Row(
              children: [
                Expanded(child: _StatCard(label: 'Presentes', value: summary.presentDays.toString(), icon: Icons.check_circle, color: Colors.green)),
                const SizedBox(width: 8),
                Expanded(child: _StatCard(label: 'Tardes', value: summary.lateDays.toString(), icon: Icons.warning, color: Colors.orange)),
                const SizedBox(width: 8),
                Expanded(child: _StatCard(label: 'Ausentes', value: summary.absentDays.toString(), icon: Icons.cancel, color: Colors.red)),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Expanded(child: _StatCard(label: 'Total Horas', value: summary.totalHours.toStringAsFixed(1), icon: Icons.access_time, color: Colors.blue)),
                const SizedBox(width: 8),
                Expanded(child: _StatCard(label: 'Promedio/día', value: summary.averageHoursPerDay.toStringAsFixed(1), icon: Icons.trending_up, color: Colors.purple)),
                const SizedBox(width: 8),
                Expanded(child: _StatCard(label: 'Registros', value: summary.records.length.toString(), icon: Icons.list, color: Colors.teal)),
              ],
            ),
            const SizedBox(height: 24),

            Text('Registros Diarios', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            ...summary.records.reversed.take(31).map((r) {
              final statusColor = r.status == 'present' ? Colors.green : Colors.orange;
              final statusText = r.status == 'present' ? 'A tiempo' : 'Tarde';
              return ZCard(
                child: ListTile(
                  leading: Icon(r.checkInTime != null ? Icons.access_time : Icons.cancel, color: statusColor),
                  title: Text(r.date),
                  subtitle: Text('${r.checkInTime ?? '—'} → ${r.checkOutTime ?? '—'}'),
                  trailing: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(statusText, style: TextStyle(fontSize: 12, color: statusColor)),
                      if (r.totalHours != null) Text('${r.totalHours!.toStringAsFixed(1)}h', style: const TextStyle(fontSize: 11, color: Colors.grey)),
                    ],
                  ),
                ),
              );
            }),
          ],
        ),
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color color;

  const _StatCard({required this.label, required this.value, required this.icon, required this.color});

  @override
  Widget build(BuildContext context) {
    return ZCard(
      padding: const EdgeInsets.all(10),
      child: Column(
        children: [
          Icon(icon, color: color, size: 22),
          const SizedBox(height: 4),
          Text(value, style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: color)),
          Text(label, style: const TextStyle(fontSize: 10, color: Colors.grey)),
        ],
      ),
    );
  }
}
