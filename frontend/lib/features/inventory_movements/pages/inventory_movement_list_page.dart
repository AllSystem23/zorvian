import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/inventory_movement_provider.dart';

final class InventoryMovementListPage extends ConsumerStatefulWidget {
  const InventoryMovementListPage({super.key});
  @override
  ConsumerState<InventoryMovementListPage> createState() => _InventoryMovementListPageState();
}

final class _InventoryMovementListPageState extends ConsumerState<InventoryMovementListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(inventoryMovementProvider.notifier).load());
  }

  List<InventoryMovementItem> _filter(List<InventoryMovementItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items.where((m) =>
      m.productName.toLowerCase().contains(q) ||
      m.productCode.toLowerCase().contains(q) ||
      m.type.toLowerCase().contains(q) ||
      (m.reference?.toLowerCase().contains(q) ?? false)
    ).toList();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(inventoryMovementProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);
    return Scaffold(
      appBar: AppBar(title: const Text('Kardex - Movimientos')),
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
                          hintText: 'Buscar por producto, código, tipo o referencia...',
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
                          ? Center(child: Text(_searchQuery.isNotEmpty ? 'Sin resultados' : 'No hay movimientos'))
                          : RefreshIndicator(
                              onRefresh: () => ref.read(inventoryMovementProvider.notifier).load(),
                              child: ListView.separated(
                                itemCount: filtered.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final m = filtered[i];
                                  final isIn = m.type == 'in';
                                  final color = isIn ? Colors.green : Colors.red;
                                  return ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor: color.withAlpha(30),
                                      child: Icon(isIn ? Icons.add_circle_outline : Icons.remove_circle_outline, color: color),
                                    ),
                                    title: Text('${m.productName} (${m.productCode})', style: const TextStyle(fontWeight: FontWeight.w600)),
                                    subtitle: Text('${isIn ? "Entrada" : "Salida"}: ${m.quantity.toStringAsFixed(0)} · Stock: ${m.stockAfter.toStringAsFixed(0)} · ${m.createdAt.substring(0, 10)}'),
                                    trailing: Text(m.type, style: TextStyle(fontSize: 12, color: color, fontWeight: FontWeight.bold)),
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
