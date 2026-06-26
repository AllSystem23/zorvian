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
    final svc = ref.read(payrollServiceProvider);
    final dio = ref.read(dioClientProvider);

    try {
      _runs = await svc.getRuns(null);
    } catch (e) {
      debugPrint('PayrollPage: error loading runs: $e');
    }
    try {
      _periods = await svc.getPeriods(null);
    } catch (e) {
      debugPrint('PayrollPage: error loading periods: $e');
    }
    try {
      final empRes = await dio.get('employees', params: {'page': 1, 'pageSize': 1});
      _employeeCount = (empRes.data['total'] as int?) ?? 0;
    } catch (e) {
      debugPrint('PayrollPage: error loading employees: $e');
    }

    if (mounted) setState(() => _loading = false);
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
          label: 'Trabajadores',
          value: _employeeCount.toString(),
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
              title: const Text('Tipos de Deducción'),
              subtitle: const Text('Configurar deducciones de nómina'),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => context.push('/payroll/deduction-types'),
            ),
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: ZCard(
            padding: EdgeInsets.zero,
            child: ListTile(
              leading: Icon(Icons.calendar_month, color: Colors.blue.shade600),
              title: const Text('Períodos'),
              subtitle: const Text('Gestionar períodos de nómina'),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => context.push('/payroll/periods'),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildRunCard(dynamic run, ThemeData theme) {
    final status = run['status']?.toString() ?? '';
    final statusColor = switch (status) {
      'draft' => Colors.orange,
      'approved' => Colors.green,
      'paid' => Colors.blue,
      'cancelled' => Colors.red,
      _ => Colors.grey,
    };
    final statusLabel = switch (status) {
      'draft' => 'Borrador',
      'approved' => 'Aprobada',
      'paid' => 'Pagada',
      'cancelled' => 'Anulada',
      _ => status,
    };

    return ZCard(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: statusColor.withValues(alpha: 0.1),
          child: Icon(Icons.receipt_long, color: statusColor, size: 20),
        ),
        title: Text(run['periodName']?.toString() ?? 'Corrida'),
        subtitle: Text(
          'Neta: \$${run['totalNetPay']?.toString() ?? '0.00'} · $statusLabel',
          style: TextStyle(color: statusColor, fontWeight: FontWeight.w500),
        ),
        trailing: const Icon(Icons.chevron_right),
        onTap: () => context.push('/payroll/runs/${run['id']}'),
      ),
    );
  }
}

class _SummaryCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color color;

  const _SummaryCard({
    required this.icon,
    required this.label,
    required this.value,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return ZCard(
      child: Row(
        children: [
          CircleAvatar(
            backgroundColor: color.withValues(alpha: 0.1),
            child: Icon(icon, color: color, size: 20),
          ),
          const SizedBox(width: 12),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(value, style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)),
              Text(label, style: Theme.of(context).textTheme.bodySmall),
            ],
          ),
        ],
      ),
    );
  }
}
