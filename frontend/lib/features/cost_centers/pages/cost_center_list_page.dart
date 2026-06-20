import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../providers/cost_center_provider.dart';

final class CostCenterListPage extends ConsumerStatefulWidget {
  const CostCenterListPage({super.key});
  @override
  ConsumerState<CostCenterListPage> createState() => _CostCenterListPageState();
}

final class _CostCenterListPageState extends ConsumerState<CostCenterListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(costCenterProvider.notifier).load());
  }

  List<CostCenterItem> _filter(List<CostCenterItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items.where((c) =>
      c.name.toLowerCase().contains(q) ||
      c.code.toLowerCase().contains(q)
    ).toList();
  }

  Future<void> _confirmDelete(String id, String name) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar centro de costo',
      message: '¿Eliminar $name?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
      confirmColor: Colors.red,
    );
    if (ok != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('cost-centers/$id');
      ref.read(costCenterProvider.notifier).load();
    } catch (_) {
      if (mounted) ZToast.error(context, 'Error al eliminar centro de costo');
    }
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(costCenterProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);
    return Scaffold(
      appBar: AppBar(title: const Text('Centros de Costo')),
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
                          hintText: 'Buscar por nombre o código...',
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
                          ? Center(child: Text(_searchQuery.isNotEmpty ? 'Sin resultados' : 'No hay centros de costo'))
                          : RefreshIndicator(
                              onRefresh: () => ref.read(costCenterProvider.notifier).load(),
                              child: ListView.separated(
                                itemCount: filtered.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final c = filtered[i];
                                  return ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor: theme.colorScheme.primaryContainer,
                                      child: Text(c.code, style: TextStyle(fontSize: 11, color: theme.colorScheme.onPrimaryContainer)),
                                    ),
                                    title: Text(c.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                                    subtitle: Text(c.description ?? 'Sin descripción'),
                                    trailing: PopupMenuButton<String>(
                                      onSelected: (v) {
                                        if (v == 'edit') context.push('/cost-centers/${c.id}/edit');
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
