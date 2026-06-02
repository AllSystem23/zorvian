import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../providers/supplier_provider.dart';

final class SupplierListPage extends ConsumerStatefulWidget {
  const SupplierListPage({super.key});
  @override
  ConsumerState<SupplierListPage> createState() => _SupplierListPageState();
}

final class _SupplierListPageState extends ConsumerState<SupplierListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(supplierProvider.notifier).load());
  }

  List<SupplierItem> _filter(List<SupplierItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items.where((s) =>
      s.name.toLowerCase().contains(q) ||
      s.code.toLowerCase().contains(q) ||
      (s.contactName?.toLowerCase().contains(q) ?? false) ||
      (s.phone?.contains(q) ?? false)
    ).toList();
  }

  Future<void> _confirmDelete(String id, String name) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Eliminar proveedor'),
        content: Text('¿Eliminar $name?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancelar')),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text('Eliminar', style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (ok != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('suppliers/$id');
      ref.read(supplierProvider.notifier).load();
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error al eliminar proveedor')));
    }
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(supplierProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);
    return Scaffold(
      appBar: AppBar(title: const Text('Proveedores')),
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
                          hintText: 'Buscar por nombre, código, contacto o teléfono...',
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
                          ? Center(child: Text(_searchQuery.isNotEmpty ? 'Sin resultados' : 'No hay proveedores'))
                          : RefreshIndicator(
                              onRefresh: () => ref.read(supplierProvider.notifier).load(),
                              child: ListView.separated(
                                itemCount: filtered.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final s = filtered[i];
                                  return ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor: theme.colorScheme.primaryContainer,
                                      child: Text(s.code, style: TextStyle(fontSize: 11, color: theme.colorScheme.onPrimaryContainer)),
                                    ),
                                    title: Text(s.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                                    subtitle: Text('${s.contactName ?? "Sin contacto"} · ${s.phone ?? "Sin teléfono"}'),
                                    trailing: PopupMenuButton<String>(
                                      onSelected: (v) {
                                        if (v == 'edit') context.push('/suppliers/${s.id}/edit');
                                        if (v == 'delete') _confirmDelete(s.id, s.name);
                                      },
                                      itemBuilder: (_) => [
                                        const PopupMenuItem(value: 'edit', child: Text('Editar')),
                                        const PopupMenuItem(value: 'delete', child: Text('Eliminar', style: TextStyle(color: Colors.red))),
                                      ],
                                    ),
                                  );
                                },
                              ),
                            ),
                    ),
                  ],
                ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final result = await context.push<bool>('/suppliers/new');
          if (result == true) ref.read(supplierProvider.notifier).load();
        },
        child: const Icon(Icons.add),
      ),
    );
  }
}
