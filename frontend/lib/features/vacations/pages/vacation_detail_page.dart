import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/vacation_provider.dart';
import '../providers/vacation_receipt_provider.dart';

class VacationDetailPage extends ConsumerStatefulWidget {
  final String vacationId;
  const VacationDetailPage({super.key, required this.vacationId});

  @override
  ConsumerState<VacationDetailPage> createState() => _VacationDetailPageState();
}

class _VacationDetailPageState extends ConsumerState<VacationDetailPage> {
  VacationItem? _request;
  bool _loading = true;
  String? _error;
  bool _actionLoading = false;
  /// true si la solicitud "tomada" tiene recibo RV asociado (pago por recibo).
  bool _receiptFound = false;
  static String? _lastScope;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final request = await ref.read(vacationProvider.notifier).loadDetail(widget.vacationId);
    // Solicitud tomada sin pago por planilla → saber si existe recibo RV para decidir
    // si mostramos "Ver Recibo de Pago" (marcadas como tomadas sin recibo no lo muestran).
    var receiptFound = false;
    if (request != null &&
        request.status == 'taken' &&
        request.paidByPayrollRunId == null) {
      final receipt =
          await ref.read(vacationReceiptProvider.notifier).loadReceiptByRequest(widget.vacationId);
      receiptFound = receipt != null;
    }
    if (!mounted) return;
    setState(() {
      _request = request;
      _receiptFound = receiptFound;
      _loading = false;
      _error = request == null ? 'Error al cargar solicitud' : null;
    });
  }

  Future<void> _approve() async {
    setState(() => _actionLoading = true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('vacations/${widget.vacationId}/approve', data: {'comment': ''});
      if (mounted) _load();
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error al aprobar')));
    } finally {
      if (mounted) setState(() => _actionLoading = false);
    }
  }

  Future<void> _reject() async {
    final ctrl = TextEditingController();
    final reason = await ZModal.show<String>(context,
      title: 'Motivo de rechazo',
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ZTextField(
            controller: ctrl,
            label: '',
            hint: 'Ingrese el motivo...',
            maxLines: 3,
          ),
          const SizedBox(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancelar')),
              const SizedBox(width: 8),
              ZButton(
                onPressed: () => Navigator.pop(context, ctrl.text),
                text: 'Rechazar',
              ),
            ],
          ),
        ],
      ),
    );
    ctrl.dispose();
    if (reason == null) return;

    setState(() => _actionLoading = true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('vacations/${widget.vacationId}/reject', data: {'comment': reason});
      if (mounted) _load();
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error al rechazar')));
    } finally {
      if (mounted) setState(() => _actionLoading = false);
    }
  }

  Future<void> _pay() async {
    final confirmed = await ZConfirmDialog.show(
      context,
      title: 'Procesar Pago',
      message: 'Se generará el recibo de vacaciones y se descontarán los días '
          'del saldo. ¿Continuar?',
      confirmLabel: 'Pagar',
      icon: Icons.payments_outlined,
    );
    if (!confirmed) return;

    setState(() => _actionLoading = true);
    try {
      final success = await ref.read(vacationReceiptProvider.notifier).payVacation(widget.vacationId);
      if (!mounted) return;
      if (success) {
        final receipt = await ref.read(vacationReceiptProvider.notifier).loadReceiptByRequest(widget.vacationId);
        if (!mounted) return;
        if (receipt != null) {
          context.push('/vacations/receipts/${receipt.id}');
        } else {
          await _load();
        }
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error al procesar el pago')),
        );
      }
    } finally {
      if (mounted) setState(() => _actionLoading = false);
    }
  }

  Future<void> _viewReceipt() async {
    final receipt = await ref.read(vacationReceiptProvider.notifier).loadReceiptByRequest(widget.vacationId);
    if (!mounted) return;
    if (receipt != null) {
      context.push('/vacations/receipts/${receipt.id}');
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('No se encontró el recibo')),
      );
    }
  }

  Color _statusColor(String s) {
    switch (s) {
      case 'approved': return Colors.green;
      case 'rejected': return Colors.red;
      case 'pending': return Colors.orange;
      case 'taken': return Colors.teal;
      default: return Colors.grey;
    }
  }

  String _statusLabel(String s) {
    switch (s) {
      case 'approved': return 'Aprobado';
      case 'rejected': return 'Rechazado';
      case 'pending': return 'Pendiente';
      case 'taken': return 'Tomada';
      case 'cancelled': return 'Cancelado';
      default: return s;
    }
  }

  bool _canAct() => _request?.status == 'pending';
  bool _canPay() => _request?.status == 'approved';
  bool _isTaken() => _request?.status == 'taken';
  bool _paidByPayroll() => _request?.paidByPayrollRunId != null;

  /// Marcada como tomada con recibo RV (pago por recibo) — es la única con recibo que ver.
  bool _hasReceiptPath() => _isTaken() && !_paidByPayroll() && _receiptFound;

  Future<void> _markTaken() async {
    final confirmed = await ZConfirmDialog.show(
      context,
      title: 'Marcar como Tomada',
      message: 'Registra la vacación como disfrutada SIN generar recibo de '
          'pago y descuenta los días del saldo. ¿Continuar?',
      confirmLabel: 'Confirmar',
      icon: Icons.event_available_outlined,
    );
    if (!confirmed) return;

    setState(() => _actionLoading = true);
    try {
      final ok = await ref.read(vacationProvider.notifier).markVacationTaken(widget.vacationId);
      if (!mounted) return;
      if (ok) {
        await _load();
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('No se pudo marcar como tomada (solo vacaciones aprobadas)')),
        );
      }
    } finally {
      if (mounted) setState(() => _actionLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scope = ref.watch(branchScopeProvider);
    if (_lastScope != null && _lastScope != scope) {
      _lastScope = scope;
      Future.microtask(() => _load());
    } else {
      _lastScope = scope;
    }

    if (_loading) return Scaffold(body: const Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(body: Center(child: Text(_error!)));
    final r = _request!;

    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ZCard(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      CircleAvatar(
                        backgroundColor: theme.colorScheme.primaryContainer,
                        child: Text(
                          (r.employeeName.isNotEmpty ? r.employeeName[0] : '?'),
                          style: TextStyle(color: theme.colorScheme.onPrimaryContainer),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(child: Text(r.employeeName, style: theme.textTheme.titleMedium)),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                        decoration: BoxDecoration(
                          color: _statusColor(r.status).withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text(
                          _statusLabel(r.status),
                          style: TextStyle(color: _statusColor(r.status), fontWeight: FontWeight.w600),
                        ),
                      ),
                    ],
                  ),
                  const Divider(height: 24),
                  _row('Código', r.employeeCode),
                  _row('Inicio', r.startDate),
                  _row('Fin', r.endDate),
                  _row('Días totales', r.totalDays.toStringAsFixed(r.totalDays % 1 == 0 ? 0 : 1)),
                  _row('Días hábiles', r.businessDays.toStringAsFixed(r.businessDays % 1 == 0 ? 0 : 1)),
                  _row('Comentarios', r.comments ?? '—'),
                  if (r.isAdvanced) _row('Tipo', 'Adelanto de vacaciones'),
                  if (r.rejectionReason != null) _row('Motivo rechazo', r.rejectionReason),
                ],
              ),
            ),
            const SizedBox(height: 16),
            if (r.approvalSteps.isNotEmpty)
              ZCard(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Flujo de aprobación', style: theme.textTheme.titleMedium),
                    const Divider(),
                    ...(r.approvalSteps.map((s) {
                      return ListTile(
                        leading: Icon(
                          s.status == 'approved'
                              ? Icons.check_circle
                              : s.status == 'rejected'
                                  ? Icons.cancel
                                  : Icons.schedule,
                          color: s.status == 'approved'
                              ? Colors.green
                              : s.status == 'rejected'
                                  ? Colors.red
                                  : Colors.grey,
                        ),
                        title: Text('Paso ${s.step}: ${s.approverName ?? 'RRHH'}'),
                        subtitle: Text(_statusLabel(s.status)),
                      );
                    })),
                  ],
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
            if (_canPay())
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    ElevatedButton.icon(
                      onPressed: _actionLoading ? null : _pay,
                      icon: const Icon(Icons.payment),
                      label: const Text('Procesar Pago de Vacaciones'),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFF7C4DFF),
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                      ),
                    ),
                    const SizedBox(height: 8),
                    OutlinedButton.icon(
                      onPressed: _actionLoading ? null : _markTaken,
                      icon: const Icon(Icons.beach_access),
                      label: const Text('Marcar como Tomada (sin pago)'),
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 14),
                      ),
                    ),
                  ],
                ),
              ),
            if (_isTaken() && _paidByPayroll())
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: Card(
                  color: Color(0xFFE8F5E9),
                  child: ListTile(
                    leading: Icon(Icons.account_balance, color: Colors.teal),
                    title: Text('Pagada por Planilla de Pago'),
                    subtitle: Text(
                        'Estas vacaciones se pagaron y descontaron del saldo al aprobar la nómina.'),
                  ),
                ),
              ),
            if (_hasReceiptPath())
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 16),
                child: ElevatedButton.icon(
                  onPressed: _actionLoading ? null : _viewReceipt,
                  icon: const Icon(Icons.receipt_long),
                  label: const Text('Ver Recibo de Pago'),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.teal,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 16),
                  ),
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
