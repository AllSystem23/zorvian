import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_trip_provider.dart';

final class FleetTripListPage extends ConsumerStatefulWidget {
  const FleetTripListPage({super.key});

  @override
  ConsumerState<FleetTripListPage> createState() => _FleetTripListPageState();
}

final class _FleetTripListPageState extends ConsumerState<FleetTripListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(fleetTripProvider.notifier).load());
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
      case 'Planned': return 'Planificado';
      case 'InProgress': return 'En curso';
      case 'Completed': return 'Completado';
      case 'Cancelled': return 'Cancelado';
      default: return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetTripProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Viajes')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
              : state.items.isEmpty
                  ? ZEmptyState.list(
                      itemType: 'viajes',
                      actionLabel: 'Nuevo Viaje',
                      onAction: () async {
                        final r = await context.push<bool>('/fleet/trips/new');
                        if (r == true) ref.read(fleetTripProvider.notifier).load();
                      },
                    )
                  : RefreshIndicator(
                      onRefresh: () => ref.read(fleetTripProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final t = state.items[i];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: _statusColor(t.status).withValues(alpha: 0.2),
                              child: Icon(Icons.flight_takeoff_outlined, color: _statusColor(t.status), size: 20),
                            ),
                            title: Text('${t.code} · ${t.vehiclePlate}', style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('${t.driverName} · ${t.origin} → ${t.destination}'),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Text(t.status == 'Completed' && t.totalKm != null ? '${t.totalKm!.toStringAsFixed(0)} km' : '', style: const TextStyle(fontSize: 12)),
                                const SizedBox(width: 8),
                                Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                  decoration: BoxDecoration(
                                    color: _statusColor(t.status).withValues(alpha: 0.15),
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  child: Text(
                                    _statusLabel(t.status),
                                    style: TextStyle(fontSize: 11, color: _statusColor(t.status), fontWeight: FontWeight.w600),
                                  ),
                                ),
                                PopupMenuButton<String>(
                                  onSelected: (action) async {
                                    if (action == 'edit') {
                                      final r2 = await context.push<bool>('/fleet/trips/${t.id}/edit');
                                      if (r2 == true) ref.read(fleetTripProvider.notifier).load();
                                    }
                                    if (action == 'delete') _confirmDelete(t.id, t.code);
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
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final r = await context.push<bool>('/fleet/trips/new');
          if (r == true) ref.read(fleetTripProvider.notifier).load();
        },
        child: const Icon(Icons.add),
      ),
    );
  }

  Future<void> _confirmDelete(String id, String code) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar viaje',
      message: '¿Eliminar el viaje $code?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
    );
    if (ok != true) return;
    final success = await ref.read(fleetTripProvider.notifier).delete(id);
    if (mounted && !success) ZToast.error(context, 'Error al eliminar');
  }
}
