import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/stock_transfer_provider.dart';

final class StockTransferListPage extends ConsumerStatefulWidget {
  const StockTransferListPage({super.key});
  @override
  ConsumerState<StockTransferListPage> createState() => _StockTransferListPageState();
}

final class _StockTransferListPageState extends ConsumerState<StockTransferListPage> {
  final _search = TextEditingController();

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(stockTransferProvider.notifier).load());
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  String _statusLabel(String s) => switch (s) {
    'draft' => 'Borrador',
    'in_transit' => 'En Tránsito',
    'partially_received' => 'Recepción Parcial',
    'completed' => 'Completada',
    'cancelled' => 'Anulada',
    _ => s,
  };

  Color _statusColor(String s) => switch (s) {
    'in_transit' => Colors.orange,
    'partially_received' => Colors.amber.shade800,
    'completed' => Colors.green,
    'cancelled' => Colors.red,
    'draft' => Colors.grey,
    _ => Colors.grey,
  };

  /// Ruta origen → destino priorizando bodegas; las sucursales quedan como
  /// desambiguación cuando hay transferencias entre sucursales.
  String _route(StockTransferItem t) {
    final origin = t.fromWarehouseName.isNotEmpty ? t.fromWarehouseName : t.fromBranchName;
    final dest = t.toWarehouseName.isNotEmpty ? t.toWarehouseName : t.toBranchName;
    final crossBranch = t.fromBranchName.isNotEmpty &&
        t.toBranchName.isNotEmpty &&
        t.fromBranchName != t.toBranchName;
    return crossBranch
        ? '$origin (${t.fromBranchName}) → $dest (${t.toBranchName})'
        : '$origin → $dest';
  }

  @override
  Widget build(BuildContext context) {
    final transfers = ref.watch(stockTransferProvider);
    return Scaffold(
      floatingActionButton: ref.canWriteLocation('/stock-transfers/new')
          ? FloatingActionButton.extended(
              heroTag: 'add-stock-transfer',
              onPressed: () => context.push('/stock-transfers/new'),
              icon: const Icon(Icons.add),
              label: const Text('Nueva Transferencia'),
            )
          : null,
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: TextField(
              controller: _search,
              decoration: InputDecoration(
                hintText: 'Buscar por número o sucursal...',
                prefixIcon: const Icon(Icons.search),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                suffixIcon: _search.text.isNotEmpty
                    ? IconButton(icon: const Icon(Icons.clear), onPressed: () { _search.clear(); ref.read(stockTransferProvider.notifier).load(); })
                    : null,
              ),
              onChanged: (v) => ref.read(stockTransferProvider.notifier).load(search: v),
            ),
          ),
          Expanded(
            child: ZAsyncRenderer<List<StockTransferItem>>(
              value: transfers,
              onRetry: () => ref.read(stockTransferProvider.notifier).load(),
              builder: (items) => RefreshIndicator(
                onRefresh: () => ref.read(stockTransferProvider.notifier).load(),
                child: ListView.separated(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  itemCount: items.length,
                  separatorBuilder: (_, _) => const Divider(height: 1),
                  itemBuilder: (_, i) {
                    final t = items[i];
                    return ListTile(
                      title: Text(t.transferNumber, style: const TextStyle(fontWeight: FontWeight.w600)),
                      subtitle: Text('${_route(t)} · ${t.createdAt}'),
                      leading: const Icon(Icons.swap_horiz),
                      trailing: Chip(label: Text(_statusLabel(t.status), style: TextStyle(color: _statusColor(t.status), fontSize: 12))),
                      onTap: () => context.push('/stock-transfers/${t.id}'),
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
