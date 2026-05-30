import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/attendance_provider.dart';

class AttendanceHistoryPage extends ConsumerStatefulWidget {
  const AttendanceHistoryPage({super.key});

  @override
  ConsumerState<AttendanceHistoryPage> createState() => _AttendanceHistoryPageState();
}

class _AttendanceHistoryPageState extends ConsumerState<AttendanceHistoryPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(attendanceProvider.notifier).load());
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final state = ref.watch(attendanceProvider);
    final summary = state.summary;

    return Scaffold(
      appBar: AppBar(title: const Text('Historial de Asistencia')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : summary == null
              ? const Center(child: Text('No hay datos'))
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    // Summary cards
                    Row(
                      children: [
                        Expanded(child: _StatCard(label: 'Presentes', value: '${summary.presentDays}', icon: Icons.check_circle, color: Colors.green)),
                        const SizedBox(width: 8),
                        Expanded(child: _StatCard(label: 'Tardes', value: '${summary.lateDays}', icon: Icons.warning, color: Colors.orange)),
                        const SizedBox(width: 8),
                        Expanded(child: _StatCard(label: 'Ausentes', value: '${summary.absentDays}', icon: Icons.cancel, color: Colors.red)),
                      ],
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        Expanded(child: _StatCard(label: 'Total Horas', value: '${summary.totalHours.toStringAsFixed(1)}', icon: Icons.access_time, color: Colors.blue)),
                        const SizedBox(width: 8),
                        Expanded(child: _StatCard(label: 'Promedio/día', value: '${summary.averageHoursPerDay.toStringAsFixed(1)}', icon: Icons.trending_up, color: Colors.purple)),
                        const SizedBox(width: 8),
                        Expanded(child: _StatCard(label: 'Registros', value: '${summary.records.length}', icon: Icons.list, color: Colors.teal)),
                      ],
                    ),
                    const SizedBox(height: 24),

                    // Daily records
                    Text('Registros Diarios', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                    const SizedBox(height: 8),
                    ...summary.records.reversed.take(31).map((r) {
                      final statusColor = r.status == 'present' ? Colors.green : Colors.orange;
                      final statusText = r.status == 'present' ? 'A tiempo' : 'Tarde';
                      return Card(
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
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(10),
        child: Column(
          children: [
            Icon(icon, color: color, size: 22),
            const SizedBox(height: 4),
            Text(value, style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: color)),
            Text(label, style: const TextStyle(fontSize: 10, color: Colors.grey)),
          ],
        ),
      ),
    );
  }
}
