import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/error/error_notifier.dart';
import '../providers/payroll_provider.dart';

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
      final runs = await svc.getRuns(null);
      _run = runs.cast<Map<String, dynamic>>().firstWhere(
        (r) => r['id'] == widget.runId,
        orElse: () => throw Exception('Run not found'),
      );
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
                    ...(_run!['details'] as List<dynamic>? ?? []).map((d) => _DetailCard(d, theme)),
                  ],
                ),
    );
  }

  Widget _buildSummary(ThemeData theme) {
    final status = _run!['status'] as String? ?? 'draft';
    final (label, color) = switch (status) {
      'draft' => ('Borrador', Colors.grey),
      'approved' => ('Aprobado', Colors.green),
      'paid' => ('Pagado', Colors.blue),
      _ => (status, Colors.grey),
    };

    return Card(
      child: Padding(
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
            _SummaryRow('Salarios', 'C\$ ${_run!['totalSalaries']?.toStringAsFixed(2) ?? '0.00'}'),
            _SummaryRow('Deducciones', '- C\$ ${_run!['totalDeductions']?.toStringAsFixed(2) ?? '0.00'}'),
            const Divider(),
            _SummaryRow('Neto a Pagar', 'C\$ ${_run!['totalNetPay']?.toStringAsFixed(2) ?? '0.00'}', bold: true),
            const SizedBox(height: 8),
            _SummaryRow('Empleados', '${_run!['employeeCount'] ?? 0}'),
            if (_run!['status'] == 'draft')
              Padding(
                padding: const EdgeInsets.only(top: 16),
                child: SizedBox(
                  width: double.infinity,
                  child: FilledButton.icon(
                    onPressed: _approve,
                    icon: const Icon(Icons.check),
                    label: const Text('Aprobar Nómina'),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _SummaryRow(String label, String value, {bool bold = false}) {
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

  Widget _DetailCard(dynamic d, ThemeData theme) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
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
      ),
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
}
