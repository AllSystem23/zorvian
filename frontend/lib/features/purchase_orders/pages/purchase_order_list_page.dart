import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/purchase_order_provider.dart';

final class PurchaseOrderListPage extends ConsumerStatefulWidget {
  const PurchaseOrderListPage({super.key});
  @override
  ConsumerState<PurchaseOrderListPage> createState() => _PurchaseOrderListPageState();
}

final class _PurchaseOrderListPageState extends ConsumerState<PurchaseOrderListPage> {
  final _search = TextEditingController();

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(purchaseOrderProvider.notifier).load());
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Color _statusColor(String s) => switch (s) {
    'completed' => Colors.green,
    'cancelled' => Colors.red,
    'approved' => Colors.blue,
    'draft' => Colors.grey,
    'partially_received' => Colors.orange,
    _ => Colors.grey,
  };

  String _statusLabel(String s) => switch (s) {
    'draft' => 'Borrador',
    'pending_approval' => 'Pendiente',
    'approved' => 'Aprobada',
    'partially_received' => 'Recibida Parcial',
    'completed' => 'Completada',
    'cancelled' => 'Anulada',
    _ => s,
  };

  @override
  Widget build(BuildContext context) {
    final orders = ref.watch(purchaseOrderProvider);
    return Scaffold(
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: TextField(
              controller: _search,
              decoration: InputDecoration(
                hintText: 'Buscar orden...',
                prefixIcon: const Icon(Icons.search),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                suffixIcon: _search.text.isNotEmpty
                    ? IconButton(icon: const Icon(Icons.clear), onPressed: () { _search.clear(); ref.read(purchaseOrderProvider.notifier).load(); })
                    : null,
              ),
              onChanged: (v) => ref.read(purchaseOrderProvider.notifier).load(search: v),
            ),
          ),
          Expanded(
            child: ZAsyncRenderer<List<PurchaseOrderItem>>(
              value: orders,
              onRetry: () => ref.read(purchaseOrderProvider.notifier).load(),
              builder: (items) => RefreshIndicator(
                onRefresh: () => ref.read(purchaseOrderProvider.notifier).load(),
                child: ListView.separated(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  itemCount: items.length,
                  separatorBuilder: (_, _) => const Divider(height: 1),
                  itemBuilder: (_, i) {
                    final o = items[i];
                    return ListTile(
                      title: Text(o.orderNumber, style: const TextStyle(fontWeight: FontWeight.w600)),
                      subtitle: Text('${o.supplierName} · ${o.createdAt}'),
                      trailing: Chip(label: Text(_statusLabel(o.status), style: TextStyle(color: _statusColor(o.status), fontSize: 12))),
                      onTap: () => context.push('/purchase-orders/${o.id}'),
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
