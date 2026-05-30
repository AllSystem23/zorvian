import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class VacationDetailPage extends ConsumerStatefulWidget {
  final String vacationId;
  const VacationDetailPage({super.key, required this.vacationId});

  @override
  ConsumerState<VacationDetailPage> createState() => _VacationDetailPageState();
}

class _VacationDetailPageState extends ConsumerState<VacationDetailPage> {
  Map<String, dynamic>? _request;
  bool _loading = true;
  String? _error;
  bool _actionLoading = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('/vacations/${widget.vacationId}');
      setState(() { _request = r.data; _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar solicitud'; _loading = false; });
    }
  }

  Future<void> _approve() async {
    setState(() => _actionLoading = true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('/vacations/${widget.vacationId}/approve', data: {'comment': ''});
      if (mounted) _load();
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error al aprobar')));
    } finally {
      if (mounted) setState(() => _actionLoading = false);
    }
  }

  Future<void> _reject() async {
    final reason = await showDialog<String>(
      context: context,
      builder: (_) => _RejectDialog(),
    );
    if (reason == null) return;

    setState(() => _actionLoading = true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('/vacations/${widget.vacationId}/reject', data: {'comment': reason});
      if (mounted) _load();
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error al rechazar')));
    } finally {
      if (mounted) setState(() => _actionLoading = false);
    }
  }

  Color _statusColor(String s) {
    switch (s) {
      case 'approved': return Colors.green;
      case 'rejected': return Colors.red;
      case 'pending': return Colors.orange;
      default: return Colors.grey;
    }
  }

  String _statusLabel(String s) {
    switch (s) {
      case 'approved': return 'Aprobado';
      case 'rejected': return 'Rechazado';
      case 'pending': return 'Pendiente';
      case 'cancelled': return 'Cancelado';
      default: return s;
    }
  }

  bool _canAct() {
    final status = _request?['status'] as String?;
    return status == 'pending';
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (_loading) return Scaffold(appBar: AppBar(title: const Text('Vacaciones')), body: const Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(appBar: AppBar(title: const Text('Vacaciones')), body: Center(child: Text(_error!)));
    final r = _request!;

    return Scaffold(
      appBar: AppBar(title: const Text('Solicitud de vacaciones')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        CircleAvatar(
                          backgroundColor: theme.colorScheme.primaryContainer,
                          child: Text(
                            ((r['employeeName'] as String?)?[0] ?? '?'),
                            style: TextStyle(color: theme.colorScheme.onPrimaryContainer),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(child: Text(r['employeeName'] ?? '', style: theme.textTheme.titleMedium)),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                          decoration: BoxDecoration(
                            color: _statusColor(r['status'] as String? ?? '').withValues(alpha: 0.15),
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: Text(
                            _statusLabel(r['status'] as String? ?? ''),
                            style: TextStyle(color: _statusColor(r['status'] as String? ?? ''), fontWeight: FontWeight.w600),
                          ),
                        ),
                      ],
                    ),
                    const Divider(height: 24),
                    _row('Código', r['employeeCode']),
                    _row('Inicio', r['startDate']),
                    _row('Fin', r['endDate']),
                    _row('Días totales', '${r['totalDays']}'),
                    _row('Días hábiles', '${r['businessDays']}'),
                    _row('Comentarios', r['comments'] ?? '—'),
                    if (r['rejectionReason'] != null)
                      _row('Motivo rechazo', r['rejectionReason']),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            if (r['approvalSteps'] is List && (r['approvalSteps'] as List).isNotEmpty)
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Flujo de aprobación', style: theme.textTheme.titleMedium),
                      const Divider(),
                      ...((r['approvalSteps'] as List).map((s) {
                        final approver = s['approverName'] as String? ?? 'RRHH';
                        final stepStatus = s['status'] as String? ?? 'pending';
                        return ListTile(
                          leading: Icon(
                            stepStatus == 'approved'
                                ? Icons.check_circle
                                : stepStatus == 'rejected'
                                    ? Icons.cancel
                                    : Icons.schedule,
                            color: stepStatus == 'approved'
                                ? Colors.green
                                : stepStatus == 'rejected'
                                    ? Colors.red
                                    : Colors.grey,
                          ),
                          title: Text('Paso ${s['step']}: $approver'),
                          subtitle: Text(_statusLabel(stepStatus)),
                        );
                      })),
                    ],
                  ),
                ),
              ),
            if (_canAct())
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 16),
                child: Row(
                  children: [
                    Expanded(
                      child: ElevatedButton.icon(
                        onPressed: _actionLoading ? null : _approve,
                        icon: const Icon(Icons.check),
                        label: const Text('Aprobar'),
                        style: ElevatedButton.styleFrom(backgroundColor: Colors.green, foregroundColor: Colors.white),
                      ),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: ElevatedButton.icon(
                        onPressed: _actionLoading ? null : _reject,
                        icon: const Icon(Icons.close),
                        label: const Text('Rechazar'),
                        style: ElevatedButton.styleFrom(backgroundColor: Colors.red, foregroundColor: Colors.white),
                      ),
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _row(String label, String? value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          SizedBox(width: 120, child: Text(label, style: const TextStyle(color: Colors.grey))),
          Expanded(child: Text(value ?? '—')),
        ],
      ),
    );
  }
}

class _RejectDialog extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final ctrl = TextEditingController();
    return AlertDialog(
      title: const Text('Motivo de rechazo'),
      content: TextField(
        controller: ctrl,
        decoration: const InputDecoration(hintText: 'Ingrese el motivo...'),
        maxLines: 3,
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancelar')),
        TextButton(onPressed: () => Navigator.pop(context, ctrl.text), child: const Text('Rechazar', style: TextStyle(color: Colors.red))),
      ],
    );
  }
}
