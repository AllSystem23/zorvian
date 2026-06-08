import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../providers/credit_provider.dart';

final class OverdueDashboardPage extends ConsumerStatefulWidget {
  const OverdueDashboardPage({super.key});
  @override
  ConsumerState<OverdueDashboardPage> createState() => _OverdueDashboardPageState();
}

final class _OverdueDashboardPageState extends ConsumerState<OverdueDashboardPage> {
  OverdueDashboard? _data;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('credits/overdue-dashboard');
      setState(() { _data = OverdueDashboard.fromJson(r.data as Map<String, dynamic>); _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar dashboard'; _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Dashboard de Mora'),
        actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: _load)],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)))
              : _buildContent(theme),
    );
  }

  Widget _buildContent(ThemeData theme) {
    final d = _data!;
    final overduePct = d.totalActiveCredits > 0 ? (d.totalOverdueCredits / d.totalActiveCredits * 100).toStringAsFixed(1) : '0';
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        padding: const EdgeInsets.all(12),
        children: [
          Row(children: [
            Expanded(child: _kpiCard(theme, 'Cartera Total', '\$${d.totalPortfolio.toStringAsFixed(0)}', Colors.blue)),
            Expanded(child: _kpiCard(theme, 'Saldo Vencido', '\$${d.totalOverdueBalance.toStringAsFixed(0)}', Colors.red)),
          ]),
          const SizedBox(height: 8),
          Row(children: [
            Expanded(child: _kpiCard(theme, 'Créditos Activos', '${d.totalActiveCredits}', Colors.green)),
            Expanded(child: _kpiCard(theme, 'Créditos en Mora', '${d.totalOverdueCredits} ($overduePct%)', Colors.orange)),
          ]),
          const SizedBox(height: 8),
          _kpiCard(theme, 'Tasa de Recuperación', '${d.recoveryRate.toStringAsFixed(1)}%', Colors.teal),
          const SizedBox(height: 16),
          Text('Distribución por Antigüedad', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          ...d.agingBuckets.map((b) => ZCard(
            margin: const EdgeInsets.only(bottom: 8),
            padding: const EdgeInsets.all(12),
            child: Row(children: [
              Expanded(flex: 2, child: Text(b.label, style: const TextStyle(fontWeight: FontWeight.w600))),
              Expanded(child: Column(children: [Text('${b.creditCount}', style: const TextStyle(fontWeight: FontWeight.bold)), Text('créditos', style: const TextStyle(fontSize: 10))])),
              Expanded(child: Column(children: [Text('${b.installmentCount}', style: const TextStyle(fontWeight: FontWeight.bold)), Text('cuotas', style: const TextStyle(fontSize: 10))])),
              Expanded(flex: 2, child: Text('\$${b.totalBalance.toStringAsFixed(0)}', textAlign: TextAlign.right, style: TextStyle(fontWeight: FontWeight.bold, color: b.totalBalance > 0 ? Colors.red : null))),
            ]),
          )),
          if (d.criticalOverdue.isNotEmpty) ...[
            const SizedBox(height: 16),
            Text('Cuotas Críticas (>90 días)', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold, color: Colors.red)),
            const SizedBox(height: 8),
            ...d.criticalOverdue.map((c) => ListTile(
              dense: true,
              title: Text('Cuota #${c.installmentNumber} · ${c.daysOverdue} días vencida', style: TextStyle(color: Colors.red.shade700)),
              subtitle: Text('Vence: ${c.dueDate.length >= 10 ? c.dueDate.substring(0, 10) : c.dueDate} · Saldo: \$${c.balance.toStringAsFixed(0)}'),
            )),
          ],
        ],
      ),
    );
  }

  Widget _kpiCard(ThemeData theme, String label, String value, Color color) {
    return ZCard(
      margin: const EdgeInsets.all(4),
      padding: const EdgeInsets.all(12),
      child: Column(
          children: [
            Text(value, style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: color)),
            Text(label, style: const TextStyle(fontSize: 11, color: Colors.grey)),
          ],
          ),
        );
   }
}
