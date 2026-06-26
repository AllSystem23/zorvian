import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/error/error_notifier.dart';
import '../../../core/widgets/responsive_layout.dart';
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
    final isDark = theme.brightness == Brightness.dark;
    final details = (_run?['details'] as List<dynamic>? ?? []);

    return Scaffold(
      backgroundColor: isDark ? ZColors.darkBackground : ZColors.neutral50,
      appBar: AppBar(title: Text(_run?['periodName'] ?? 'Corrida de Nómina')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _run == null
              ? const Center(child: Text('Corrida no encontrada'))
              : SingleChildScrollView(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      _buildSummary(),
                      // ── Actions ──
                      if (_run!['status'] == 'draft' || _run!['status'] == 'approved') ...[
                        const SizedBox(height: 16),
                        ZCard(
                          padding: const EdgeInsets.all(16),
                          child: Row(
                            children: [
                              if (_run!['status'] == 'draft')
                                Expanded(child: ZButton(text: 'Aprobar Nómina', onPressed: _approve, icon: Icons.check)),
                              if (_isAdmin && _run!['status'] == 'draft') ...[
                                const SizedBox(width: 8),
                                Expanded(child: ZButton(text: 'Eliminar', onPressed: () => _confirmDelete(context), type: ZButtonType.danger, icon: Icons.delete_outline)),
                              ],
                              if (_run!['status'] == 'approved') ...[
                                Expanded(child: ZButton(text: 'Marcar Pagado', onPressed: () => _confirmMarkAsPaid(context), icon: Icons.check_circle_outline, type: ZButtonType.secondary)),
                                const SizedBox(width: 8),
                                Expanded(child: ZButton(text: 'Anular', onPressed: () => _confirmCancel(context), icon: Icons.cancel, type: ZButtonType.danger)),
                              ],
                            ],
                          ),
                        ),
                      ],
                      const SizedBox(height: 32),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text('Detalle por Trabajador', style: ZTypography.titleLarge),
                          if (_run!['status'] == 'approved')
                            ZButton(
                              text: 'Exportar ACH',
                              onPressed: _exportAch,
                              icon: Icons.download,
                              type: ZButtonType.secondary,
                              fullWidth: false,
                            ),
                        ],
                      ),
                      const SizedBox(height: 16),
                      SizedBox(
                        height: 500,
                        child: ZDataTable<dynamic>(
                          columns: const [
                            ZColumn(id: 'name', label: 'Trabajador'),
                            ZColumn(id: 'salary', label: 'Salario Base', numeric: true),
                            ZColumn(id: 'inss', label: 'INSS', numeric: true),
                            ZColumn(id: 'ir', label: 'IR', numeric: true),
                            ZColumn(id: 'net', label: 'Neto', numeric: true),
                          ],
                          rows: details,
                          rowMapper: (d) => DataRow(cells: [
                            DataCell(Text(d['employeeName'] ?? '', style: const TextStyle(fontWeight: FontWeight.w600))),
                            DataCell(Text('C\$ ${(d['baseSalary'] as num?)?.toStringAsFixed(2) ?? '0.00'}')),
                            DataCell(Text('C\$ ${(d['inssDeduction'] as num?)?.toStringAsFixed(2) ?? '0.00'}')),
                            DataCell(Text('C\$ ${(d['irDeduction'] as num?)?.toStringAsFixed(2) ?? '0.00'}')),
                            DataCell(Row(
                              children: [
                                Text('C\$ ${(d['netPay'] as num?)?.toStringAsFixed(2) ?? '0.00'}', style: const TextStyle(fontWeight: FontWeight.bold, color: ZColors.brandPrimary)),
                                if (_run!['status'] == 'draft')
                                  IconButton(
                                    icon: const Icon(Icons.edit, size: 16),
                                    onPressed: () => _editDetail(d),
                                  ),
                              ],
                            )),
                          ]),
                        ),
                      ),
                    ],
                  ),
                ),
    );
  }

  bool get _isAdmin {
    final role = ref.watch(authProvider).role;
    return role == 'SuperAdmin' || role == 'CompanyAdmin';
  }

  Widget _buildSummary() {
    final status = _run!['status'] as String? ?? 'draft';
    final (label, variant) = switch (status) {
      'draft' => ('Borrador', ZStatVariant.neutral),
      'approved' => ('Aprobado', ZStatVariant.info),
      'paid' => ('Pagado', ZStatVariant.success),
      'cancelled' => ('Cancelado', ZStatVariant.danger),
      _ => (status, ZStatVariant.neutral),
    };

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text('Resumen de Nómina', style: ZTypography.titleLarge),
            ZBadge(text: label.toUpperCase(), type: ZBadgeType.neutral),
          ],
        ),
        const SizedBox(height: 16),
        ResponsiveGrid(
          mobileColumns: 1,
          tabletColumns: 2,
          desktopColumns: 4,
          children: [
            ZStatCard(title: 'Salarios', value: 'C\$ ${_run!["totalSalaries"]?.toStringAsFixed(2) ?? "0.00"}', icon: Icons.attach_money_outlined, variant: ZStatVariant.neutral),
            ZStatCard(title: 'Deducciones', value: '- C\$ ${_run!["totalDeductions"]?.toStringAsFixed(2) ?? "0.00"}', icon: Icons.money_off_outlined, variant: ZStatVariant.danger),
            ZStatCard(title: 'Neto a Pagar', value: 'C\$ ${_run!["totalNetPay"]?.toStringAsFixed(2) ?? "0.00"}', icon: Icons.payments_outlined, variant: ZStatVariant.success),
            ZStatCard(title: 'Trabajadores', value: '${_run!["employeeCount"] ?? 0}', icon: Icons.people_outline, variant: ZStatVariant.info),
          ],
        ),
      ],
    );
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
}
