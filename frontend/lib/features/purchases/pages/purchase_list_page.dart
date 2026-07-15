import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/purchase_provider.dart';
import '../../../shared/ds/components/z_async_renderer.dart';

final class PurchaseListPage extends ConsumerStatefulWidget {
  const PurchaseListPage({super.key});
  @override
  ConsumerState<PurchaseListPage> createState() => _PurchaseListPageState();
}

final class _PurchaseListPageState extends ConsumerState<PurchaseListPage> {
  final _searchCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    // Removed explicit load() as AsyncNotifier does it in build()
  }

  void _onSearch(String v) {
    ref.read(purchaseProvider.notifier).load(search: v.isNotEmpty ? v : null);
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(purchaseProvider);
    return Scaffold(
      body: ZAsyncRenderer<List<PurchaseItem>>(
        value: state,
        builder: (items) {
          return Column(
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
                child: TextField(
                  controller: _searchCtrl,
                  decoration: InputDecoration(
                    hintText: 'Buscar por proveedor, número o estado...',
                    prefixIcon: const Icon(Icons.search),
                    suffixIcon: _searchCtrl.text.isNotEmpty
                        ? IconButton(icon: const Icon(Icons.clear), onPressed: () { _searchCtrl.clear(); _onSearch(''); })
                        : null,
                    border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                    contentPadding: const EdgeInsets.symmetric(vertical: 0),
                  ),
                  onChanged: _onSearch,
                ),
              ),
              Expanded(
                child: items.isEmpty
                    ? Center(child: Text(_searchCtrl.text.isNotEmpty ? 'Sin resultados' : 'No hay compras'))
                    : RefreshIndicator(
                        onRefresh: () => ref.read(purchaseProvider.notifier).load(),
                        child: ListView.separated(
                          itemCount: items.length,
                          separatorBuilder: (_, _) => const Divider(height: 1),
                          itemBuilder: (_, i) {
                            final s = items[i];
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
          );
        },
      ),
    );
  }
}
