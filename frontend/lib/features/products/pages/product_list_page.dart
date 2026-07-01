import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/components/z_empty_state.dart';
import '../../../shared/printing/qr_code_dialog.dart';
import '../providers/product_provider.dart';

final class ProductListPage extends ConsumerStatefulWidget {
  const ProductListPage({super.key});
  @override
  ConsumerState<ProductListPage> createState() => _ProductListPageState();
}

final class _ProductListPageState extends ConsumerState<ProductListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(productProvider.notifier).load());
  }

  List<ProductItem> _filter(List<ProductItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items.where((p) =>
      p.name.toLowerCase().contains(q) ||
      p.code.toLowerCase().contains(q) ||
      (p.categoryName?.toLowerCase().contains(q) ?? false) ||
      (p.brandName?.toLowerCase().contains(q) ?? false)
    ).toList();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(productProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);
    return Scaffold(
      appBar: AppBar(title: const Text('Productos')),
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
                          hintText: 'Buscar por nombre, código, categoría o marca...',
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
                          ? _searchQuery.isNotEmpty
                              ? const ZEmptyState.search()
                              : ZEmptyState.list(
                                  itemType: 'productos',
                                  actionLabel: 'Nuevo Producto',
                                  onAction: () => context.push('/products/new'),
                                )
                          : RefreshIndicator(
                              onRefresh: () => ref.read(productProvider.notifier).load(),
                              child: ListView.separated(
                                itemCount: filtered.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final p = filtered[i];
                          final lowStock = p.stock <= p.minStock;
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: theme.colorScheme.primaryContainer,
                              child: Text(p.code, style: TextStyle(fontSize: 10, color: theme.colorScheme.onPrimaryContainer)),
                            ),
                            title: Text(p.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('${p.categoryName ?? "Sin cat."} · ${p.brandName ?? "Sin marca"} · Stock: ${p.stock.toStringAsFixed(0)} ${p.unit}'),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                if (lowStock)
                                  Container(
                                    margin: const EdgeInsets.only(right: 8),
                                    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                    decoration: BoxDecoration(color: Colors.red.withAlpha(25), borderRadius: BorderRadius.circular(8)),
                                    child: const Text('Stock bajo', style: TextStyle(fontSize: 10, color: Colors.red)),
                                  ),
                                PopupMenuButton<String>(
                                  onSelected: (v) {
                                    if (v == 'edit') context.push('/products/${p.id}/edit');
                                    if (v == 'label') showProductLabelDialog(context, code: p.code, name: p.name, price: p.price, barcode: p.barcode, category: p.categoryName);
                                  },
                                  itemBuilder: (_) => [
                                    const PopupMenuItem(value: 'edit', child: Text('Editar')),
                                    const PopupMenuItem(value: 'label', child: Text('Etiqueta QR')),
                                  ],
                                ),
                              ],
                            ),
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
