import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../providers/brand_provider.dart';

final class BrandListPage extends ConsumerStatefulWidget {
  const BrandListPage({super.key});
  @override
  ConsumerState<BrandListPage> createState() => _BrandListPageState();
}

final class _BrandListPageState extends ConsumerState<BrandListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(brandProvider.notifier).load());
  }

  List<BrandItem> _filter(List<BrandItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items.where((b) => b.name.toLowerCase().contains(q)).toList();
  }

  Future<void> _confirmDelete(String id, String name) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar marca',
      message: '¿Eliminar $name?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
      confirmColor: Colors.red,
    );
    if (ok != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('brands/$id');
      ref.read(brandProvider.notifier).load();
    } catch (_) {
      if (mounted) ZToast.error(context, 'Error al eliminar marca');
    }
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(brandProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);
    return Scaffold(
      appBar: AppBar(title: const Text('Marcas')),
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
                          ? Center(child: Text(_searchQuery.isNotEmpty ? 'Sin resultados' : 'No hay marcas'))
                          : RefreshIndicator(
                              onRefresh: () => ref.read(brandProvider.notifier).load(),
                              child: ListView.separated(
                                itemCount: filtered.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final b = filtered[i];
                                  return ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor: theme.colorScheme.primaryContainer,
                                      child: Icon(Icons.branding_watermark, color: theme.colorScheme.onPrimaryContainer),
                                    ),
                                    title: Text(b.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                                    subtitle: Text(b.description ?? 'Sin descripción'),
                                    trailing: PopupMenuButton<String>(
                                      onSelected: (v) async {
                                        if (v == 'edit') {
                                          final result = await context.push<bool>('/brands/${b.id}/edit');
                                          if (result == true) ref.read(brandProvider.notifier).load();
                                        }
                                        if (v == 'delete') _confirmDelete(b.id, b.name);
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
          final result = await context.push<bool>('/brands/new');
          if (result == true) ref.read(brandProvider.notifier).load();
        },
        child: const Icon(Icons.add),
      ),
    );
  }
}
