import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_workshop_provider.dart';

final class FleetWorkshopListPage extends ConsumerStatefulWidget {
  const FleetWorkshopListPage({super.key});

  @override
  ConsumerState<FleetWorkshopListPage> createState() => _FleetWorkshopListPageState();
}

final class _FleetWorkshopListPageState extends ConsumerState<FleetWorkshopListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(fleetWorkshopProvider.notifier).load());
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetWorkshopProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Talleres')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
              : state.items.isEmpty
                  ? ZEmptyState.list(
                      itemType: 'talleres',
                      actionLabel: 'Nuevo Taller',
                      onAction: () async {
                        final r = await context.push<bool>('/fleet/workshop/new');
                        if (r == true) ref.read(fleetWorkshopProvider.notifier).load();
                      },
                    )
                  : RefreshIndicator(
                      onRefresh: () => ref.read(fleetWorkshopProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final w = state.items[i];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: ZColors.moduleFleet.withValues(alpha: 0.2),
                              child: Icon(Icons.precision_manufacturing_outlined, color: ZColors.moduleFleet, size: 20),
                            ),
                            title: Text(w.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('${w.phone}${w.email != null ? ' · ${w.email}' : ''}'),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                if (w.isInternal)
                                  Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                    decoration: BoxDecoration(
                                      color: ZColors.info.withValues(alpha: 0.15),
                                      borderRadius: BorderRadius.circular(10),
                                    ),
                                    child: const Text('Interno', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w600)),
                                  ),
                                PopupMenuButton<String>(
                                  onSelected: (action) async {
                                    if (action == 'edit') {
                                      final r2 = await context.push<bool>('/fleet/workshop/${w.id}/edit');
                                      if (r2 == true) ref.read(fleetWorkshopProvider.notifier).load();
                                    }
                                    if (action == 'delete') _confirmDelete(w.id, w.name);
                                  },
                                  itemBuilder: (_) => [
                                    const PopupMenuItem(value: 'edit', child: Text('Editar')),
                                    const PopupMenuItem(value: 'delete', child: Text('Eliminar', style: TextStyle(color: Colors.red))),
                                  ],
                                ),
                              ],
                            ),
                          );
                        },
                      ),
                    ),
    );
  }

  Future<void> _confirmDelete(String id, String name) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar taller',
      message: '¿Eliminar el taller $name?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
    );
    if (ok != true) return;
    final success = await ref.read(fleetWorkshopProvider.notifier).delete(id);
    if (mounted && !success) ZToast.error(context, 'Error al eliminar');
  }
}
