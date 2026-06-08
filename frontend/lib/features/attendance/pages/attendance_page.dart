import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/attendance_provider.dart';
import '../../../shared/ds/ds.dart';

class AttendancePage extends ConsumerStatefulWidget {
  const AttendancePage({super.key});

  @override
  ConsumerState<AttendancePage> createState() => _AttendancePageState();
}

class _AttendancePageState extends ConsumerState<AttendancePage> {
  DateTime _now = DateTime.now();
  Timer? _timer;

  @override
  void initState() {
    super.initState();
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      setState(() => _now = DateTime.now());
    });
    Future.microtask(() => ref.read(attendanceProvider.notifier).load());
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final state = ref.watch(attendanceProvider);
    final today = state.todayRecord;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Asistencia'),
        actions: [
          IconButton(
            icon: const Icon(Icons.qr_code_scanner),
            onPressed: () => context.push('/attendance/qr'),
          ),
          IconButton(
            icon: const Icon(Icons.history),
            onPressed: () => context.push('/attendance/history'),
          ),
        ],
      ),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: () => ref.read(attendanceProvider.notifier).load(),
              child: ListView(
                padding: const EdgeInsets.all(24),
                children: [
                  // Live clock
                  Center(
                    child: Text(
                      '${_now.hour.toString().padLeft(2, '0')}:${_now.minute.toString().padLeft(2, '0')}:${_now.second.toString().padLeft(2, '0')}',
                      style: theme.textTheme.displayLarge?.copyWith(fontWeight: FontWeight.bold, color: theme.colorScheme.primary),
                    ),
                  ),
                  const SizedBox(height: 4),
                  Center(
                    child: Text(
                      '${_now.day}/${_now.month}/${_now.year}',
                      style: theme.textTheme.titleMedium?.copyWith(color: Colors.grey),
                    ),
                  ),
                  const SizedBox(height: 48),

                  // Check-in / Check-out status
                  Center(
                    child: today == null
                        ? _buildBigButton(theme, 'Marcar Entrada', Icons.login, Colors.green, () => _doCheckIn())
                        : today.checkOutTime == null
                            ? Column(
                                children: [
                                  _buildStatusCard(theme, today),
                                  const SizedBox(height: 24),
                                  _buildBigButton(theme, 'Marcar Salida', Icons.logout, Colors.red, () => _doCheckOut()),
                                ],
                              )
                            : _buildStatusCard(theme, today),
                  ),
                  const SizedBox(height: 24),

                  // Error display
                  if (state.error != null)
                    Card(
                      color: Colors.red.shade50,
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Row(
                          children: [
                            const Icon(Icons.error_outline, color: Colors.red),
                            const SizedBox(width: 8),
                            Expanded(child: Text(state.error!, style: const TextStyle(color: Colors.red))),
                          ],
                        ),
                      ),
                    ),

                  // Weekly summary mini
                  if (state.summary != null) ...[
                    const SizedBox(height: 32),
                    Text('Resumen del Mes', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                    const SizedBox(height: 12),
                    _buildSummaryRow(state.summary!),
                  ],
                ],
              ),
            ),
    );
  }

  Widget _buildStatusCard(ThemeData theme, AttendanceRecord record) {
    final statusColor = record.status == 'present' ? Colors.green : Colors.orange;
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          Icon(Icons.check_circle, color: statusColor, size: 48),
          const SizedBox(height: 8),
          Text(record.status == 'present' ? 'Presente' : 'Tarde', style: TextStyle(fontSize: 18, color: statusColor, fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          Text('Entrada: ${record.checkInTime ?? '—'}'),
          Text('Salida: ${record.checkOutTime ?? 'Pendiente'}'),
        ],
      ),
    );
  }

  Widget _buildBigButton(ThemeData theme, String label, IconData icon, Color color, VoidCallback onPressed) {
    final checking = ref.watch(attendanceProvider).checking;
    return SizedBox(
      width: 200,
      height: 200,
      child: Material(
        elevation: 4,
        shape: const CircleBorder(),
        color: color,
        child: Semantics(
          label: label,
          button: !checking,
          child: InkWell(
            customBorder: const CircleBorder(),
            onTap: checking ? null : onPressed,
          child: Center(
            child: checking
                ? const CircularProgressIndicator(color: Colors.white)
                : Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(icon, size: 48, color: Colors.white),
                      const SizedBox(height: 8),
                      Text(label, style: const TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold)),
                    ],
                  ),
          ),
        ),
        ),
      ),
    );
  }

  Widget _buildSummaryRow(AttendanceSummary summary) {
    return Row(
      children: [
        Expanded(child: _SummaryCard(label: 'Presentes', value: '${summary.presentDays}', color: Colors.green)),
        const SizedBox(width: 8),
        Expanded(child: _SummaryCard(label: 'Tardes', value: '${summary.lateDays}', color: Colors.orange)),
        const SizedBox(width: 8),
        Expanded(child: _SummaryCard(label: 'Horas', value: summary.totalHours.toStringAsFixed(1), color: Colors.blue)),
      ],
    );
  }

  Future<void> _doCheckIn() async {
    final ok = await ref.read(attendanceProvider.notifier).checkIn();
    if (ok) {
      ref.read(attendanceProvider.notifier).load();
    }
  }

  Future<void> _doCheckOut() async {
    final ok = await ref.read(attendanceProvider.notifier).checkOut();
    if (ok) {
      ref.read(attendanceProvider.notifier).load();
    }
  }
}

class _SummaryCard extends StatelessWidget {
  final String label;
  final String value;
  final Color color;

  const _SummaryCard({required this.label, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return ZCard(
      padding: const EdgeInsets.all(12),
      child: Column(
        children: [
          Text(value, style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: color)),
          Text(label, style: const TextStyle(fontSize: 12, color: Colors.grey)),
        ],
      ),
    );
  }
}
