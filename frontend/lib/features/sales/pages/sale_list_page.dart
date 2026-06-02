import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/sale_provider.dart';

final class SaleListPage extends ConsumerStatefulWidget {
  const SaleListPage({super.key});
  @override
  ConsumerState<SaleListPage> createState() => _SaleListPageState();
}

final class _SaleListPageState extends ConsumerState<SaleListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(saleProvider.notifier).load());
  }

  List<SaleItem> _filter(List<SaleItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items.where((s) =>
      s.clientName.toLowerCase().contains(q) ||
      s.invoiceNumber.toLowerCase().contains(q) ||
      s.saleType.toLowerCase().contains(q) ||
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
    final state = ref.watch(saleProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);
    return Scaffold(
      appBar: AppBar(title: const Text('Ventas')),
      floatingActionButton: FloatingActionButton(
        onPressed: () => context.push('/sales/new'),
        child: const Icon(Icons.add),
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
                          hintText: 'Buscar por cliente, factura, tipo o estado...',
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
                          ? Center(child: Text(_searchQuery.isNotEmpty ? 'Sin resultados' : 'No hay ventas'))
                          : RefreshIndicator(
                              onRefresh: () => ref.read(saleProvider.notifier).load(),
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
                                    leading: CircleAvatar(
                                      backgroundColor: statusColor.withAlpha(30),
                                      child: Text(s.invoiceNumber.length > 4 ? s.invoiceNumber.substring(s.invoiceNumber.length - 4) : s.invoiceNumber,
                                        style: TextStyle(fontSize: 10, color: statusColor, fontWeight: FontWeight.bold)),
                                    ),
                                    title: Text(s.clientName, style: const TextStyle(fontWeight: FontWeight.w600)),
                                    subtitle: Text('\$${s.total.toStringAsFixed(0)} · ${s.saleType} · ${s.saleDate.length >= 10 ? s.saleDate.substring(0, 10) : s.saleDate}'),
                                    trailing: Chip(label: Text(s.status, style: TextStyle(fontSize: 11, color: statusColor)), materialTapTargetSize: MaterialTapTargetSize.shrinkWrap),
                                    onTap: () => context.push('/sales/${s.id}'),
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
