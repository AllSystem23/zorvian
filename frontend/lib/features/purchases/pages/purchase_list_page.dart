import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/purchase_provider.dart';

final class PurchaseListPage extends ConsumerStatefulWidget {
  const PurchaseListPage({super.key});
  @override
  ConsumerState<PurchaseListPage> createState() => _PurchaseListPageState();
}

final class _PurchaseListPageState extends ConsumerState<PurchaseListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(purchaseProvider.notifier).load());
  }

  List<PurchaseItem> _filter(List<PurchaseItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items.where((s) =>
      s.supplierName.toLowerCase().contains(q) ||
      s.purchaseNumber.toLowerCase().contains(q) ||
      s.status.toLowerCase().contains(q)
    ).toList();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(purchaseProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);

    return Scaffold(
      appBar: AppBar(title: const Text('Compras')),
      floatingActionButton: FloatingActionButton(
        child: const Icon(Icons.add),
        onPressed: () => context.push('/purchases/new'),
      ),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : Column(
                  children: [
                    Padding(
                      padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
                      child: TextField(
                        controller: _searchCtrl,
                        decoration: InputDecoration(
                          hintText: 'Buscar por proveedor, número o estado...',
                          prefixIcon: const Icon(Icons.search),
                          suffixIcon: _searchQuery.isNotEmpty
                              ? IconButton(icon: const Icon(Icons.clear), onPressed: () { _searchCtrl.clear(); setState(() => _searchQuery = ''); })
                              : null,
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                          contentPadding: const EdgeInsets.symmetric(vertical: 0),
                        ),
                        onChanged: (v) => setState(() => _searchQuery = v),
                      ),
                    ),
                    Expanded(
                      child: filtered.isEmpty
                          ? Center(child: Text(_searchQuery.isNotEmpty ? 'Sin resultados' : 'No hay compras'))
                          : RefreshIndicator(
                              onRefresh: () => ref.read(purchaseProvider.notifier).load(),
                              child: ListView.separated(
                                itemCount: filtered.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final s = filtered[i];
                                  final statusColor = switch (s.status) {
                                    'completed' => Colors.green,
                                    'cancelled' => Colors.red,
                                    _ => Colors.orange,
                                  };
                                  return ListTile(
                                    title: Text(s.supplierName, style: const TextStyle(fontWeight: FontWeight.w600)),
                                    subtitle: Text('${s.purchaseNumber} · \$${s.total.toStringAsFixed(2)} · ${s.createdAt.substring(0, 10)}'),
                                    trailing: Chip(
                                      label: Text(s.status, style: const TextStyle(fontSize: 11, color: Colors.white)),
                                      backgroundColor: statusColor,
                                      padding: EdgeInsets.zero,
                                      visualDensity: VisualDensity.compact,
                                    ),
                                    onTap: () => context.push('/purchases/${s.id}'),
                                  );
                                },
                              ),
                            ),
                    ),
                  ],
                ),
    );
  }
}
