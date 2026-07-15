import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';

final class ClientStatementPage extends ConsumerStatefulWidget {
  final String clientId;
  const ClientStatementPage({super.key, required this.clientId});
  @override
  ConsumerState<ClientStatementPage> createState() => _ClientStatementPageState();
}

final class _ClientStatementPageState extends ConsumerState<ClientStatementPage> {
  Map<String, dynamic>? _data;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('clients/${widget.clientId}/statement');
      setState(() { _data = r.data as Map<String, dynamic>; _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar estado de cuenta'; _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (_loading) return Scaffold(body: const Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(body: Center(child: Text(_error!)));

    final d = _data!;
    final sales = d['recentSales'] as List? ?? [];
    final credits = d['activeCreditsList'] as List? ?? [];

    return Scaffold(
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          ZCard(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(d['clientName'] ?? '', style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)),
                const SizedBox(height: 4),
                Text('Código: ${d['clientCode'] ?? ''}', style: const TextStyle(color: Colors.grey)),
                if (d['clientPhone'] != null) Text('Tel: ${d['clientPhone']}', style: const TextStyle(color: Colors.grey)),
                if (d['creditLimit'] != null) Text('Límite de crédito: \$${(d['creditLimit'] as num).toStringAsFixed(0)}', style: const TextStyle(color: Colors.grey)),
              ],
            ),
          ),
          const SizedBox(height: 12),
          Row(children: [
            _kpiCard('Ventas', '${d['totalSales'] ?? 0}', Colors.blue, theme),
            _kpiCard('Créditos', '${d['activeCredits'] ?? 0}', Colors.orange, theme),
          ]),
          const SizedBox(height: 8),
          Row(children: [
            _kpiCard('Saldo Total', '\$${(d['totalBalance'] as num?)?.toStringAsFixed(0) ?? '0'}', Colors.red, theme),
            _kpiCard('Vencido', '\$${(d['overdueBalance'] as num?)?.toStringAsFixed(0) ?? '0'}', Colors.deepOrange, theme),
          ]),
          const SizedBox(height: 16),
          Text('Ventas Recientes', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          if (sales.isEmpty)
            const Text('Sin ventas registradas', style: TextStyle(color: Colors.grey))
          else
            ...sales.take(5).map((s) => ZCard(
              child: ListTile(
                dense: true,
                title: Text('Factura ${s['invoiceNumber'] ?? ''}', style: const TextStyle(fontWeight: FontWeight.w600)),
                subtitle: Text('${(s['saleDate'] as String?)?.substring(0, 10) ?? ''} · ${s['saleType'] ?? ''}'),
                trailing: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text('\$${(s['total'] as num?)?.toStringAsFixed(0) ?? '0'}', style: const TextStyle(fontWeight: FontWeight.bold)),
                    Text(s['status'] as String? ?? '', style: const TextStyle(fontSize: 11)),
                  ],
                ),
                onTap: () => context.push('/sales/${s['id']}'),
              ),
            )),
          const SizedBox(height: 16),
          Text('Créditos Activos', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          if (credits.isEmpty)
            const Text('Sin créditos activos', style: TextStyle(color: Colors.grey))
          else
            ...credits.map((c) => ZCard(
              child: ListTile(
                dense: true,
                title: Text(c['creditNumber'] ?? '', style: const TextStyle(fontWeight: FontWeight.w600)),
                subtitle: Text('Saldo: \$${(c['balance'] as num?)?.toStringAsFixed(0) ?? '0'} · Vence: ${(c['nextDueDate'] as String?)?.substring(0, 10) ?? ''}'),
                trailing: Chip(
                  label: Text(c['status'] as String? ?? '', style: const TextStyle(fontSize: 10)),
                  materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  padding: EdgeInsets.zero,
                ),
                onTap: () => context.push('/credits/${c['id']}'),
              ),
            )),
        ],
      ),
    );
  }

  Widget _kpiCard(String label, String value, Color color, ThemeData theme) {
    return Expanded(
      child: ZCard(
        margin: const EdgeInsets.symmetric(horizontal: 4),
        padding: const EdgeInsets.all(12),
        child: Column(
          children: [
            Text(value, style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold, color: color)),
            Text(label, style: const TextStyle(color: Colors.grey, fontSize: 12)),
          ],
        ),
      ),
    );
  }
}
