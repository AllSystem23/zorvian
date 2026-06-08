import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/error/error_notifier.dart';
import '../providers/payroll_provider.dart';
import '../../../shared/ds/ds.dart';

class PayrollRunDetailPage extends ConsumerStatefulWidget {
  final String runId;
  const PayrollRunDetailPage({super.key, required this.runId});

  @override
  ConsumerState<PayrollRunDetailPage> createState() => _PayrollRunDetailPageState();
}

class _PayrollRunDetailPageState extends ConsumerState<PayrollRunDetailPage> {
  Map<String, dynamic>? _run;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final svc = ref.read(payrollServiceProvider);
      _run = await svc.getRunById(widget.runId);
    } catch (_) {
      if (mounted) ref.read(errorNotifierProvider.notifier).showError('Error al cargar la corrida');
    }
    setState(() => _loading = false);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(_run?['periodName'] ?? 'Corrida de Nómina')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _run == null
              ? const Center(child: Text('Corrida no encontrada'))
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    _buildSummary(theme),
                    const SizedBox(height: 24),
                    Text('Detalle por Empleado', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                    const SizedBox(height: 12),
                    ...(_run!['details'] as List<dynamic>? ?? []).map((d) => _buildDetailCard(d, theme)),
                  ],
                ),
    );
  }

  bool get _isAdmin {
    final role = ref.watch(authProvider).role;
    return role == 'SuperAdmin' || role == 'CompanyAdmin';
  }

  Widget _buildSummary(ThemeData theme) {
    final status = _run!['status'] as String? ?? 'draft';
    final (label, color) = switch (status) {
      'draft' => ('Borrador', Colors.grey),
      'approved' => ('Aprobado', Colors.green),
      'paid' => ('Pagado', Colors.blue),
      'cancelled' => ('Cancelado', Colors.red),
      _ => (status, Colors.grey),
    };

    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Resumen', style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                Chip(label: Text(label, style: TextStyle(fontSize: 11, color: color)), materialTapTargetSize: MaterialTapTargetSize.shrinkWrap),
              ],
            ),
            const SizedBox(height: 16),
            _buildSummaryRow('Salarios', 'C\$ ${_run!["totalSalaries"]?.toStringAsFixed(2) ?? "0.00"}'),
            _buildSummaryRow('Deducciones', '- C\$ ${_run!["totalDeductions"]?.toStringAsFixed(2) ?? "0.00"}'),
            const Divider(),
            _buildSummaryRow('Neto a Pagar', 'C\$ ${_run!["totalNetPay"]?.toStringAsFixed(2) ?? "0.00"}', bold: true),
            const SizedBox(height: 8),
            _buildSummaryRow('Empleados', '${_run!["employeeCount"] ?? 0}'),
            if (_run!['status'] == 'draft')
              Padding(
                padding: const EdgeInsets.only(top: 16),
                child: SizedBox(
                  width: double.infinity,
                  child: ZButton(
                    text: 'Aprobar Nómina',
                    onPressed: _approve,
                    icon: Icons.check,
                  ),
                ),
              ),
            if (_isAdmin && _run!['status'] != 'cancelled')
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Row(
                  children: [
                    if (_run!['status'] == 'draft')
                      Expanded(
                        child: ZButton(
                          text: 'Eliminar',
                          onPressed: () => _confirmDelete(context),
                          icon: Icons.delete_outline,
                          type: ZButtonType.secondary,
                          fullWidth: false,
                        ),
                      ),
                    if (_run!['status'] == 'approved')
                      Expanded(
                        child: ZButton(
                          text: 'Marcar Pagado',
                          onPressed: () => _confirmMarkAsPaid(context),
                          icon: Icons.check_circle_outline,
                          type: ZButtonType.secondary,
                          fullWidth: false,
                        ),
                      ),
                    if (_run!['status'] != 'draft' && _run!['status'] != 'cancelled')
                      Expanded(
                        child: ZButton(
                          text: 'Anular',
                          onPressed: () => _confirmCancel(context),
                          icon: Icons.cancel,
                          type: ZButtonType.secondary,
                          fullWidth: false,
                        ),
                      ),
                  ],
                ),
              ),
            if (_run!['status'] == 'approved')
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Row(
                  children: [
                    Expanded(
                      child: ZButton(
                        text: 'Marcar Pagado',
                        onPressed: () => _confirmMarkAsPaid(context),
                        icon: Icons.check_circle_outline,
                        type: ZButtonType.secondary,
                        fullWidth: false,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: ZButton(
                        text: 'Exportar ACH',
                        onPressed: _exportAch,
                        icon: Icons.download,
                        type: ZButtonType.secondary,
                        fullWidth: false,
                      ),
                    ),
                  ],
                ),
              ),
          ],
        ),
    );
  }

  Widget _buildSummaryRow(String label, String value, {bool bold = false}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.normal, fontSize: 14)),
          Text(value, style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.normal, fontSize: 14)),
        ],
      ),
    );
  }

  Widget _buildDetailCard(dynamic d, ThemeData theme) {
    final isDraft = _run!['status'] == 'draft';
    return ZCard(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.person, size: 18, color: theme.colorScheme.primary),
                const SizedBox(width: 8),
                Text(d['employeeName'] ?? '', style: const TextStyle(fontWeight: FontWeight.bold)),
                const Spacer(),
                if (_isAdmin && isDraft)
                  IconButton(
                    icon: Icon(Icons.edit, size: 16, color: theme.colorScheme.primary),
                    onPressed: () => _editDetail(d),
                    padding: EdgeInsets.zero,
                    constraints: const BoxConstraints(),
                  ),
                const SizedBox(width: 8),
                Text('C\$ ${(d['netPay'] as num?)?.toStringAsFixed(2) ?? '0.00'}', style: TextStyle(fontWeight: FontWeight.bold, color: theme.colorScheme.primary)),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Expanded(child: Text('Salario: C\$ ${(d['baseSalary'] as num?)?.toStringAsFixed(2) ?? '0.00'}', style: const TextStyle(fontSize: 12))),
                Expanded(child: Text('INSS: C\$ ${(d['inssDeduction'] as num?)?.toStringAsFixed(2) ?? '0.00'}', style: const TextStyle(fontSize: 12))),
              ],
            ),
            Row(
              children: [
                Expanded(child: Text('IR: C\$ ${(d['irDeduction'] as num?)?.toStringAsFixed(2) ?? '0.00'}', style: const TextStyle(fontSize: 12))),
                Expanded(child: Text('Ded. total: C\$ ${(d['totalDeductions'] as num?)?.toStringAsFixed(2) ?? '0.00'}', style: const TextStyle(fontSize: 12))),
              ],
            ),
          ],
        ),
    );
  }

  Future<void> _editDetail(dynamic d) async {
    final grossCtrl = TextEditingController(text: (d['grossPay'] as num?)?.toStringAsFixed(2) ?? '');
    final inssCtrl = TextEditingController(text: (d['inssDeduction'] as num?)?.toStringAsFixed(2) ?? '');
    final irCtrl = TextEditingController(text: (d['irDeduction'] as num?)?.toStringAsFixed(2) ?? '');
    final otherCtrl = TextEditingController(text: (d['otherDeductions'] as num?)?.toStringAsFixed(2) ?? '');

    final result = await ZModal.show<bool>(context,
      title: d['employeeName'] ?? '',
      confirmText: 'Guardar',
      cancelText: 'Cancelar',
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ZTextField(controller: grossCtrl, label: 'Sueldo bruto', keyboardType: TextInputType.number),
            const SizedBox(height: 8),
            ZTextField(controller: inssCtrl, label: 'INSS', keyboardType: TextInputType.number),
            const SizedBox(height: 8),
            ZTextField(controller: irCtrl, label: 'IR', keyboardType: TextInputType.number),
            const SizedBox(height: 8),
            ZTextField(controller: otherCtrl, label: 'Otras deducciones', keyboardType: TextInputType.number),
          ],
        ),
      ),
    );
    if (result != true) return;

    try {
      final svc = ref.read(payrollServiceProvider);
      await svc.updateDetail(d['id'], {
        'grossPay': double.tryParse(grossCtrl.text),
        'inssDeduction': double.tryParse(inssCtrl.text),
        'irDeduction': double.tryParse(irCtrl.text),
        'otherDeductions': double.tryParse(otherCtrl.text),
      });
      if (mounted) {
        ref.read(errorNotifierProvider.notifier).showInfo('Detalle actualizado');
        _load();
      }
    } catch (e) {
      if (mounted) ref.read(errorNotifierProvider.notifier).showError('Error al actualizar');
    }
  }

  Future<void> _approve() async {
    try {
      final svc = ref.read(payrollServiceProvider);
      await svc.approveRun(widget.runId);
      if (mounted) {
        ref.read(errorNotifierProvider.notifier).showInfo('Nómina aprobada');
        _load();
      }
    } catch (e) {
      if (mounted) ref.read(errorNotifierProvider.notifier).showError('Error al aprobar');
    }
  }

  Future<void> _confirmDelete(BuildContext context) async {
    final confirmed = await ZModal.confirm(context,
      title: 'Eliminar corrida',
      message: '¿Está seguro de eliminar esta corrida de nómina? Esta acción no se puede deshacer.',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
      confirmColor: Colors.red,
    );
    if (confirmed != true) return;

    try {
      final svc = ref.read(payrollServiceProvider);
      await svc.deleteRun(widget.runId);
      if (context.mounted) {
        ref.read(errorNotifierProvider.notifier).showInfo('Corrida eliminada');
        context.pop(true);
      }
    } catch (e) {
      if (mounted) ref.read(errorNotifierProvider.notifier).showError('Error al eliminar corrida');
    }
  }

  Future<void> _confirmCancel(BuildContext context) async {
    final confirmed = await ZModal.confirm(context,
      title: 'Anular corrida',
      message: '¿Está seguro de anular esta corrida de nómina? El periodo volverá a estar abierto.',
      confirmText: 'Anular',
      cancelText: 'Cancelar',
      confirmColor: Colors.orange,
    );
    if (confirmed != true) return;

    try {
      final svc = ref.read(payrollServiceProvider);
      await svc.cancelRun(widget.runId);
      if (mounted) {
        ref.read(errorNotifierProvider.notifier).showInfo('Corrida anulada');
        _load();
      }
    } catch (e) {
      if (mounted) ref.read(errorNotifierProvider.notifier).showError('Error al anular corrida');
    }
  }

  Future<void> _confirmMarkAsPaid(BuildContext context) async {
    final confirmed = await ZModal.confirm(context,
      title: 'Marcar como pagada',
      message: '¿Confirma que esta nómina ha sido pagada?',
      confirmText: 'Confirmar pago',
      cancelText: 'Cancelar',
    );
    if (confirmed != true) return;

    try {
      final svc = ref.read(payrollServiceProvider);
      await svc.markAsPaid(widget.runId);
      if (mounted) {
        ref.read(errorNotifierProvider.notifier).showInfo('Nómina marcada como pagada');
        _load();
      }
    } catch (e) {
      if (mounted) ref.read(errorNotifierProvider.notifier).showError('Error al marcar como pagada');
    }
  }

  Future<void> _exportAch() async {
    try {
      final svc = ref.read(payrollServiceProvider);
      await svc.exportAchFile(widget.runId);
      if (mounted) ref.read(errorNotifierProvider.notifier).showInfo('Archivo ACH descargado');
    } catch (e) {
      if (mounted) ref.read(errorNotifierProvider.notifier).showError('Error al exportar ACH');
    }
  }
}
