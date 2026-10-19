import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/purchase_request_provider.dart';

final class PurchaseRequestListPage extends ConsumerStatefulWidget {
  const PurchaseRequestListPage({super.key});
  @override
  ConsumerState<PurchaseRequestListPage> createState() => _PurchaseRequestListPageState();
}

final class _PurchaseRequestListPageState extends ConsumerState<PurchaseRequestListPage> {
  final _search = TextEditingController();

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(purchaseRequestProvider.notifier).load());
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  String _statusLabel(String s) => switch (s) {
    'draft' => 'Borrador',
    'submitted' => 'Enviada',
    'approved' => 'Aprobada',
    'rejected' => 'Rechazada',
    'cancelled' => 'Anulada',
    _ => s,
  };

  Color _statusColor(String s) => switch (s) {
    'submitted' => Colors.blue,
    'approved' => Colors.green,
    'rejected' => Colors.red,
    'cancelled' => Colors.red,
    'draft' => Colors.grey,
    _ => Colors.grey,
  };

  String _priorityLabel(String p) => switch (p) {
    'low' => 'Baja',
    'high' => 'Alta',
    'urgent' => 'Urgente',
    _ => 'Normal',
  };

  @override
  Widget build(BuildContext context) {
    final requests = ref.watch(purchaseRequestProvider);
    return Scaffold(
      floatingActionButton: ref.canWriteLocation('/purchase-requests/new')
          ? FloatingActionButton.extended(
              heroTag: 'add-purchase-request',
              onPressed: () => context.push('/purchase-requests/new'),
              icon: const Icon(Icons.add),
              label: const Text('Nueva Solicitud'),
            )
          : null,
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: TextField(
              controller: _search,
              decoration: InputDecoration(
                hintText: 'Buscar solicitud...',
                prefixIcon: const Icon(Icons.search),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                suffixIcon: _search.text.isNotEmpty
                    ? IconButton(icon: const Icon(Icons.clear), onPressed: () { _search.clear(); ref.read(purchaseRequestProvider.notifier).load(); })
                    : null,
              ),
              onChanged: (v) => ref.read(purchaseRequestProvider.notifier).load(search: v),
            ),
          ),
          Expanded(
            child: ZAsyncRenderer<List<PurchaseRequestItem>>(
              value: requests,
              onRetry: () => ref.read(purchaseRequestProvider.notifier).load(),
              builder: (items) => RefreshIndicator(
                onRefresh: () => ref.read(purchaseRequestProvider.notifier).load(),
                child: ListView.separated(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  itemCount: items.length,
                  separatorBuilder: (_, _) => const Divider(height: 1),
                  itemBuilder: (_, i) {
                    final r = items[i];
                    return ListTile(
                      title: Row(
                        children: [
                          Flexible(child: Text(r.requestNumber, style: const TextStyle(fontWeight: FontWeight.w600))),
                          const SizedBox(width: 8),
                          Chip(
                            label: Text(_priorityLabel(r.priority), style: const TextStyle(fontSize: 11)),
                            visualDensity: VisualDensity.compact,
                            materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                          ),
                        ],
                      ),
                      subtitle: Text('${r.title}${r.branchName != null && r.branchName!.isNotEmpty ? ' · ${r.branchName}' : ''}'),
                      trailing: Chip(label: Text(_statusLabel(r.status), style: TextStyle(color: _statusColor(r.status), fontSize: 12))),
                      onTap: () => context.push('/purchase-requests/${r.id}'),
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
