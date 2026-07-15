import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_route_provider.dart';

final class FleetRouteListPage extends ConsumerStatefulWidget {
  const FleetRouteListPage({super.key});

  @override
  ConsumerState<FleetRouteListPage> createState() => _FleetRouteListPageState();
}

final class _FleetRouteListPageState extends ConsumerState<FleetRouteListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(fleetRouteProvider.notifier).load());
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'Planned': return ZColors.info;
      case 'InProgress': return ZColors.warning;
      case 'Completed': return ZColors.success;
      case 'Cancelled': return ZColors.danger;
      default: return ZColors.neutral400;
    }
  }

  String _statusLabel(String status) {
    switch (status) {
      case 'Planned': return 'Planificada';
      case 'InProgress': return 'En curso';
      case 'Completed': return 'Completada';
      case 'Cancelled': return 'Cancelada';
      default: return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetRouteProvider);

    return Scaffold(
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
              : state.items.isEmpty
                  ? ZEmptyState.list(
                      itemType: 'rutas',
                      actionLabel: 'Nueva Ruta',
                      onAction: () async {
                        final r = await context.push<bool>('/fleet/routes/new');
                        if (r == true) ref.read(fleetRouteProvider.notifier).load();
                      },
                    )
                  : RefreshIndicator(
                      onRefresh: () => ref.read(fleetRouteProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final r = state.items[i];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: _statusColor(r.status).withValues(alpha: 0.2),
                              child: Icon(Icons.route_outlined, color: _statusColor(r.status), size: 20),
                            ),
                            title: Text('${r.code} · ${r.name}', style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('${r.originAddress} → ${r.destinationAddress ?? "—"}'),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Text(r.scheduledDate, style: const TextStyle(fontSize: 12)),
                                const SizedBox(width: 8),
                                Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                  decoration: BoxDecoration(
                                    color: _statusColor(r.status).withValues(alpha: 0.15),
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  child: Text(
                                    _statusLabel(r.status),
                                    style: TextStyle(fontSize: 11, color: _statusColor(r.status), fontWeight: FontWeight.w600),
                                  ),
                                ),
                                PopupMenuButton<String>(
                                  onSelected: (action) async {
                                    if (action == 'edit') {
                                      final r2 = await context.push<bool>('/fleet/routes/${r.id}/edit');
                                      if (r2 == true) ref.read(fleetRouteProvider.notifier).load();
                                    }
                                    if (action == 'delete') _confirmDelete(r.id, r.code);
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

  Future<void> _confirmDelete(String id, String code) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar ruta',
      message: '¿Eliminar la ruta $code?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
    );
    if (ok != true) return;
    final success = await ref.read(fleetRouteProvider.notifier).delete(id);
    if (mounted && !success) ZToast.error(context, 'Error al eliminar');
  }
}
