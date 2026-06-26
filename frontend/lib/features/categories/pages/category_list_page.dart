import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../providers/category_provider.dart';

final class CategoryListPage extends ConsumerStatefulWidget {
  const CategoryListPage({super.key});
  @override
  ConsumerState<CategoryListPage> createState() => _CategoryListPageState();
}

final class _CategoryListPageState extends ConsumerState<CategoryListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(categoryProvider.notifier).load());
  }

  List<CategoryItem> _filter(List<CategoryItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items.where((c) => c.name.toLowerCase().contains(q)).toList();
  }

  Future<void> _confirmDelete(String id, String name) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar categoría',
      message: '¿Eliminar $name?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
      confirmColor: Colors.red,
    );
    if (ok != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('categories/$id');
      ref.read(categoryProvider.notifier).load();
    } catch (_) {
      if (mounted) ZToast.error(context, 'Error al eliminar categoría');
    }
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(categoryProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);
    return Scaffold(
      appBar: AppBar(title: const Text('Categorías')),
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
                          hintText: 'Buscar por nombre...',
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
                          ? Center(child: Text(_searchQuery.isNotEmpty ? 'Sin resultados' : 'No hay categorías'))
                          : RefreshIndicator(
                              onRefresh: () => ref.read(categoryProvider.notifier).load(),
                              child: ListView.separated(
                                itemCount: filtered.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final c = filtered[i];
                                  return ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor: theme.colorScheme.primaryContainer,
                                      child: Icon(Icons.category, color: theme.colorScheme.onPrimaryContainer),
                                    ),
                                    title: Text(c.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                                    subtitle: Text(c.description ?? 'Sin descripción'),
                                    trailing: PopupMenuButton<String>(
                                      onSelected: (v) async {
                                        if (v == 'edit') {
                                          final result = await context.push<bool>('/categories/${c.id}/edit');
                                          if (result == true) ref.read(categoryProvider.notifier).load();
                                        }
                                        if (v == 'delete') _confirmDelete(c.id, c.name);
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
    );
  }
}
