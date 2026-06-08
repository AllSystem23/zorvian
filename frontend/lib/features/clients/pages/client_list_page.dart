import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/client_provider.dart';

final class ClientListPage extends ConsumerStatefulWidget {
  const ClientListPage({super.key});
  @override
  ConsumerState<ClientListPage> createState() => _ClientListPageState();
}

final class _ClientListPageState extends ConsumerState<ClientListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(clientProvider.notifier).load());
  }

  List<ClientItem> _filter(List<ClientItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items.where((c) =>
      c.fullName.toLowerCase().contains(q) ||
      c.code.toLowerCase().contains(q) ||
      (c.documentNumber?.toLowerCase().contains(q) ?? false) ||
      (c.phone?.contains(q) ?? false)
    ).toList();
  }

  Future<void> _confirmDelete(String id, String name) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar cliente',
      message: '¿Eliminar $name?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
    );
    if (ok != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('clients/$id');
      ref.read(clientProvider.notifier).load();
    } catch (_) {
      if (mounted) ZToast.error(context, 'Error al eliminar cliente');
    }
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(clientProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);
    return Scaffold(
      appBar: AppBar(title: const Text('Clientes')),
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
                          hintText: 'Buscar por nombre, código, doc. o teléfono...',
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
                          ? Center(child: Text(_searchQuery.isNotEmpty ? 'Sin resultados' : 'No hay clientes'))
                          : RefreshIndicator(
                              onRefresh: () => ref.read(clientProvider.notifier).load(),
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
                            title: Text(c.fullName, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('${c.type} · ${c.documentNumber ?? "Sin doc."} · ${c.email ?? "Sin email"}'),
                            trailing: PopupMenuButton<String>(
                              onSelected: (v) {
                                if (v == 'edit') context.push('/clients/${c.id}/edit');
                                if (v == 'delete') _confirmDelete(c.id, c.fullName);
                                if (v == 'statement') context.push('/clients/${c.id}/statement');
                              },
                              itemBuilder: (_) => [
                                const PopupMenuItem(value: 'edit', child: Text('Editar')),
                                const PopupMenuItem(value: 'statement', child: Text('Estado de Cuenta')),
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
          final result = await context.push<bool>('/clients/new');
          if (result == true) ref.read(clientProvider.notifier).load();
        },
        child: const Icon(Icons.add),
      ),
    );
  }
}
