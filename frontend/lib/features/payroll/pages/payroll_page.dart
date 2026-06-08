import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/widgets/empty_state.dart';
import '../providers/payroll_provider.dart';
import '../../../shared/ds/ds.dart';

class PayrollPage extends ConsumerStatefulWidget {
  const PayrollPage({super.key});

  @override
  ConsumerState<PayrollPage> createState() => _PayrollPageState();
}

class _PayrollPageState extends ConsumerState<PayrollPage> {
  List<dynamic> _runs = [];
  List<dynamic> _periods = [];
  int _employeeCount = 0;
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
      final dio = ref.read(dioClientProvider);
      _runs = await svc.getRuns(null);
      _periods = await svc.getPeriods(null);
      final empRes = await dio.get('employees', params: {'page': 1, 'pageSize': 1});
      _employeeCount = (empRes.data['total'] as int?) ?? 0;
    } catch (_) {}
    setState(() => _loading = false);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Nómina'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: 'Nuevo periodo',
            onPressed: () => context.push('/payroll/periods/new'),
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  _buildSummaryCards(theme),
                  const SizedBox(height: 16),
                  if (ref.watch(authProvider).role == 'SuperAdmin' || ref.watch(authProvider).role == 'CompanyAdmin')
                    _buildAdminLinks(theme),
                  const SizedBox(height: 24),
                  Text('Corridas de Nómina', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                  const SizedBox(height: 12),
                  if (_runs.isEmpty)
                    const EmptyState(icon: Icons.receipt_long, title: 'Sin corridas', subtitle: 'Genere una corrida desde un periodo abierto'),
                  if (_runs.isNotEmpty)
                    ..._runs.map((r) => _buildRunCard(r, theme)),
                ],
              ),
            ),
    );
  }

  Widget _buildSummaryCards(ThemeData theme) {
    return Row(
      children: [
        Expanded(child: _SummaryCard(
          icon: Icons.people,
          label: 'Empleados',
          value: '$_employeeCount',
          color: const Color(0xFF4F46E5),
        )),
        const SizedBox(width: 12),
        Expanded(child: _SummaryCard(
          icon: Icons.calendar_month,
          label: 'Períodos',
          value: '${_periods.length}',
          color: const Color(0xFF059669),
        )),
      ],
    );
  }

  Widget _buildAdminLinks(ThemeData theme) {
    return Row(
      children: [
        Expanded(
          child: ZCard(
            padding: EdgeInsets.zero,
            child: ListTile(
              leading: Icon(Icons.attach_money, color: Colors.green.shade600),
              title: const Text('Salarios', style: TextStyle(fontSize: 13)),
              trailing: const Icon(Icons.chevron_right, size: 18),
              onTap: () => context.push('/payroll/salaries'),
            ),
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: ZCard(
            padding: EdgeInsets.zero,
            child: ListTile(
              leading: Icon(Icons.category, color: Colors.indigo.shade400),
              title: const Text('Deducciones', style: TextStyle(fontSize: 13)),
              trailing: const Icon(Icons.chevron_right, size: 18),
              onTap: () => context.push('/payroll/deduction-types'),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildRunCard(dynamic run, ThemeData theme) {
    final status = run['status'] as String? ?? 'draft';
    final (label, color) = switch (status) {
      'draft' => ('Borrador', Colors.grey),
      'approved' => ('Aprobado', Colors.green),
      'paid' => ('Pagado', Colors.blue),
      'cancelled' => ('Cancelado', Colors.red),
      _ => (status, Colors.grey),
    };

    return ZCard(
      margin: const EdgeInsets.only(bottom: 8),
      padding: EdgeInsets.zero,
      child: ListTile(
        leading: Icon(Icons.receipt, color: color),
        title: Text(run['periodName'] ?? 'Sin nombre'),
        subtitle: Text('C\$${run['totalNetPay']?.toStringAsFixed(2) ?? '0.00'} · ${run['employeeCount']} emp.'),
        trailing: Chip(label: Text(label, style: TextStyle(fontSize: 11, color: color)), materialTapTargetSize: MaterialTapTargetSize.shrinkWrap),
        onTap: run['id'] != null ? () async {
          final deleted = await context.push<bool>('/payroll/runs/${run['id']}');
          if (deleted == true) _load();
        } : null,
      ),
    );
  }
}

class _SummaryCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color color;

  const _SummaryCard({required this.icon, required this.label, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: color, size: 24),
          const SizedBox(height: 8),
          Text(value, style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: color)),
          Text(label, style: const TextStyle(fontSize: 12, color: Colors.grey)),
        ],
      ),
    );
  }
}
