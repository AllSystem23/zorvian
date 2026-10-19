import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/service_payment_provider.dart';

final class ServicePaymentListPage extends ConsumerStatefulWidget {
  const ServicePaymentListPage({super.key});
  @override
  ConsumerState<ServicePaymentListPage> createState() => _ServicePaymentListPageState();
}

final class _ServicePaymentListPageState extends ConsumerState<ServicePaymentListPage> {
  final _search = TextEditingController();
  String? _status;
  int? _year;

  static const _years = [2025, 2026, 2027];

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(servicePaymentProvider.notifier).load());
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  void _reload() => ref.read(servicePaymentProvider.notifier).load(
        search: _search.text, status: _status, year: _year);

  String _statusLabel(String s) => switch (s) {
    'draft' => 'Borrador',
    'approved' => 'Aprobado',
    'paid' => 'Pagado',
    'cancelled' => 'Cancelado',
    _ => s,
  };

  Color _statusColor(String s) => switch (s) {
    'approved' => Colors.blue,
    'paid' => Colors.green,
    'cancelled' => Colors.red,
    'draft' => Colors.grey,
    _ => Colors.grey,
  };

  @override
  Widget build(BuildContext context) {
    final payments = ref.watch(servicePaymentProvider);
    return Scaffold(
      floatingActionButton: ref.canWriteLocation('/service-payments/new')
          ? FloatingActionButton.extended(
              heroTag: 'add-service-payment',
              onPressed: () => context.push('/service-payments/new'),
              icon: const Icon(Icons.add),
              label: const Text('Nuevo Pago'),
            )
          : null,
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              children: [
                TextField(
                  controller: _search,
                  decoration: InputDecoration(
                    hintText: 'Buscar por número o prestador...',
                    prefixIcon: const Icon(Icons.search),
                    border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                    suffixIcon: _search.text.isNotEmpty
                        ? IconButton(
                            icon: const Icon(Icons.clear),
                            onPressed: () { _search.clear(); _reload(); },
                          )
                        : null,
                  ),
                  onChanged: (_) => _reload(),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: DropdownButtonFormField<String?>(
                        initialValue: _status,
                        isExpanded: true,
                        decoration: const InputDecoration(
                          labelText: 'Estado',
                          border: OutlineInputBorder(),
                          isDense: true,
                        ),
                        items: const [
                          DropdownMenuItem(value: null, child: Text('Todos')),
                          DropdownMenuItem(value: 'draft', child: Text('Borrador')),
                          DropdownMenuItem(value: 'approved', child: Text('Aprobado')),
                          DropdownMenuItem(value: 'paid', child: Text('Pagado')),
                          DropdownMenuItem(value: 'cancelled', child: Text('Cancelado')),
                        ],
                        onChanged: (v) { setState(() => _status = v); _reload(); },
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: DropdownButtonFormField<int?>(
                        initialValue: _year,
                        isExpanded: true,
                        decoration: const InputDecoration(
                          labelText: 'Año',
                          border: OutlineInputBorder(),
                          isDense: true,
                        ),
                        items: [
                          const DropdownMenuItem<int?>(value: null, child: Text('Todos')),
                          ..._years.map((y) => DropdownMenuItem<int?>(value: y, child: Text('$y'))),
                        ],
                        onChanged: (v) { setState(() => _year = v); _reload(); },
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          Expanded(
            child: ZAsyncRenderer<List<ServiceProviderPaymentItem>>(
              value: payments,
              onRetry: () => ref.read(servicePaymentProvider.notifier).load(),
              builder: (items) => RefreshIndicator(
                onRefresh: () => ref.read(servicePaymentProvider.notifier).load(),
                child: items.isEmpty
                    ? ListView(
                        children: const [
                          SizedBox(height: 80),
                          Center(child: Text('Sin pagos a prestadores registrados')),
                        ],
                      )
                    : ListView.separated(
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                        itemCount: items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final p = items[i];
                          return ListTile(
                            title: Row(
                              children: [
                                Flexible(
                                  child: Text(p.paymentNumber,
                                      style: const TextStyle(fontWeight: FontWeight.w600)),
                                ),
                                const SizedBox(width: 8),
                                Chip(
                                  label: Text(p.periodLabel,
                                      style: const TextStyle(fontSize: 11)),
                                  visualDensity: VisualDensity.compact,
                                  materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                ),
                                if (p.goalMet) ...[
                                  const SizedBox(width: 4),
                                  const Chip(
                                    label: Text('Meta ✓',
                                        style: TextStyle(fontSize: 11, color: Colors.green)),
                                    visualDensity: VisualDensity.compact,
                                    materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                  ),
                                ],
                              ],
                            ),
                            subtitle: Text(
                              '${p.employeeName ?? '—'}'
                              '${p.branchName != null ? ' · ${p.branchName}' : ''}'
                              ' · Neto: ${p.netAmount.toStringAsFixed(2)}',
                            ),
                            trailing: Chip(
                              label: Text(_statusLabel(p.status),
                                  style: TextStyle(color: _statusColor(p.status), fontSize: 12)),
                            ),
                            onTap: () => context.push('/service-payments/${p.id}'),
                          );
                        },
                      ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
