import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../providers/warehouse_provider.dart';

final class WarehouseListPage extends ConsumerStatefulWidget {
  const WarehouseListPage({super.key});
  @override
  ConsumerState<WarehouseListPage> createState() => _WarehouseListPageState();
}

final class _WarehouseListPageState extends ConsumerState<WarehouseListPage> {
  final _search = TextEditingController();

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(warehouseProvider.notifier).load());
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _openForm({String? warehouseId}) async {
    final saved = await context.push<bool>('/warehouses/form', extra: warehouseId);
    if (saved == true) ref.read(warehouseProvider.notifier).load();
  }

  Future<void> _setDefault(WarehouseItem w) async {
    final confirm = await ZModal.confirm(
      context,
      title: 'Bodega por defecto',
      message: '"${w.name}" será la bodega por defecto de la sucursal. ¿Continuar?',
    );
    if (!confirm) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('warehouses/${w.id}/set-default');
      ref.read(warehouseProvider.notifier).load();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error al cambiar la bodega por defecto'), backgroundColor: Colors.red),
        );
      }
    }
  }

  Future<void> _delete(WarehouseItem w) async {
    final confirm = await ZModal.confirm(
      context,
      title: 'Eliminar bodega',
      message: '¿Eliminar "${w.name}"? Solo funciona si no tiene productos asignados.',
    );
    if (!confirm) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('warehouses/${w.id}');
      ref.read(warehouseProvider.notifier).load();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('No se pudo eliminar: tiene productos o es la bodega por defecto'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final wh = ref.watch(warehouseProvider);
    final q = _search.text.trim().toLowerCase();
    final filtered = wh.items
        .where((w) => q.isEmpty ||
            w.name.toLowerCase().contains(q) ||
            (w.code ?? '').toLowerCase().contains(q) ||
            (w.address ?? '').toLowerCase().contains(q))
        .toList();

    return Scaffold(
      floatingActionButton: ref.canWriteLocation('/warehouses/form')
          ? FloatingActionButton.extended(
              heroTag: 'add-warehouse',
              onPressed: () => _openForm(),
              icon: const Icon(Icons.add),
              label: const Text('Nueva Bodega'),
            )
          : null,
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: TextField(
              controller: _search,
              decoration: InputDecoration(
                hintText: 'Buscar por nombre, código o dirección...',
                prefixIcon: const Icon(Icons.search),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                suffixIcon: _search.text.isNotEmpty
                    ? IconButton(
                        icon: const Icon(Icons.clear),
                        onPressed: () { _search.clear(); setState(() {}); },
                      )
                    : null,
              ),
              onChanged: (_) => setState(() {}),
            ),
          ),
          Expanded(
            child: wh.loading && wh.items.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : wh.error != null && wh.items.isEmpty
                    ? Center(child: Text(wh.error!))
                    : filtered.isEmpty
                        ? const ZEmptyState(
                            icon: Icons.warehouse_outlined,
                            title: 'No hay bodegas',
                            subtitle: 'Las bodegas pertenecen a la sucursal activa del cabecero',
                          )
                        : RefreshIndicator(
                            onRefresh: () => ref.read(warehouseProvider.notifier).load(),
                            child: ListView.separated(
                              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                              itemCount: filtered.length,
                              separatorBuilder: (_, _) => const Divider(height: 1),
                              itemBuilder: (_, i) {
                                final w = filtered[i];
                                final subtitleParts = <String>[
                                  if (w.code != null && w.code!.isNotEmpty) 'Cód: ${w.code}',
                                  if (w.address != null && w.address!.isNotEmpty) w.address!,
                                ];
                                return ListTile(
                                  leading: Icon(
                                    Icons.warehouse_outlined,
                                    color: w.isActive
                                        ? (w.isDefault ? Theme.of(context).colorScheme.primary : null)
                                        : Colors.grey,
                                  ),
                                  // `Wrap`: con dos chips (`Principal` + `Inactiva`)
                                  // el `Flexible` del nombre se encogía a 0 px y el
                                  // nombre de la bodega desaparecía en teléfono.
                                  title: Wrap(
                                    crossAxisAlignment: WrapCrossAlignment.center,
                                    spacing: 8,
                                    runSpacing: 2,
                                    children: [
                                      Text(w.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                                      if (w.isDefault)
                                        const Chip(
                                          label: Text('Principal', style: TextStyle(fontSize: 10)),
                                          visualDensity: VisualDensity.compact,
                                          materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                        ),
                                      if (!w.isActive)
                                        const Chip(
                                          label: Text('Inactiva', style: TextStyle(fontSize: 10)),
                                          visualDensity: VisualDensity.compact,
                                          materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                        ),
                                    ],
                                  ),
                                  subtitle: subtitleParts.isEmpty ? null : Text(subtitleParts.join(' · ')),
                                  trailing: PopupMenuButton<String>(
                                    onSelected: (v) => switch (v) {
                                      'edit' => _openForm(warehouseId: w.id),
                                      'default' => _setDefault(w),
                                      'delete' => _delete(w),
                                      _ => null,
                                    },
                                    itemBuilder: (_) => [
                                      const PopupMenuItem(value: 'edit', child: Text('Editar')),
                                      if (!w.isDefault && w.isActive)
                                        const PopupMenuItem(value: 'default', child: Text('Hacer por defecto')),
                                      const PopupMenuItem(value: 'delete', child: Text('Eliminar')),
                                    ],
                                  ),
                                  onTap: () => _openForm(warehouseId: w.id),
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
