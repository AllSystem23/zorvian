import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_delivery_provider.dart';

final class FleetDeliveryListPage extends ConsumerStatefulWidget {
  const FleetDeliveryListPage({super.key});

  @override
  ConsumerState<FleetDeliveryListPage> createState() => _FleetDeliveryListPageState();
}

final class _FleetDeliveryListPageState extends ConsumerState<FleetDeliveryListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(fleetDeliveryProvider.notifier).load());
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'Pending': return ZColors.warning;
      case 'InRoute': return ZColors.info;
      case 'Delivered': return ZColors.success;
      case 'Returned': return ZColors.neutral500;
      case 'Cancelled': return ZColors.danger;
      default: return ZColors.neutral400;
    }
  }

  String _statusLabel(String status) {
    switch (status) {
      case 'Pending': return 'Pendiente';
      case 'InRoute': return 'En ruta';
      case 'Delivered': return 'Entregado';
      case 'Returned': return 'Devuelto';
      case 'Cancelled': return 'Cancelado';
      default: return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetDeliveryProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Entregas')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
              : state.items.isEmpty
                  ? ZEmptyState.list(
                      itemType: 'entregas',
                      actionLabel: 'Nueva Entrega',
                      onAction: () async {
                        final r = await context.push<bool>('/fleet/deliveries/new');
                        if (r == true) ref.read(fleetDeliveryProvider.notifier).load();
                      },
                    )
                  : RefreshIndicator(
                      onRefresh: () => ref.read(fleetDeliveryProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final d = state.items[i];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: _statusColor(d.status).withValues(alpha: 0.2),
                              child: Icon(Icons.inventory_2_outlined, color: _statusColor(d.status), size: 20),
                            ),
                            title: Text('${d.code} · ${d.clientName ?? "—"}', style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text(d.deliveryAddress),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Text(d.scheduledDate, style: const TextStyle(fontSize: 12)),
                                const SizedBox(width: 8),
                                Container(
                                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                  decoration: BoxDecoration(
                                    color: _statusColor(d.status).withValues(alpha: 0.15),
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  child: Text(
                                    _statusLabel(d.status),
                                    style: TextStyle(fontSize: 11, color: _statusColor(d.status), fontWeight: FontWeight.w600),
                                  ),
                                ),
                                PopupMenuButton<String>(
                                  onSelected: (action) async {
                                    if (action == 'edit') {
                                      final r2 = await context.push<bool>('/fleet/deliveries/${d.id}/edit');
                                      if (r2 == true) ref.read(fleetDeliveryProvider.notifier).load();
                                    }
                                    if (action == 'delete') _confirmDelete(d.id, d.code);
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
          final r = await context.push<bool>('/fleet/deliveries/new');
          if (r == true) ref.read(fleetDeliveryProvider.notifier).load();
        },
        child: const Icon(Icons.add),
      ),
    );
  }

  Future<void> _confirmDelete(String id, String code) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar entrega',
      message: '¿Eliminar la entrega $code?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
    );
    if (ok != true) return;
    final success = await ref.read(fleetDeliveryProvider.notifier).delete(id);
    if (mounted && !success) ZToast.error(context, 'Error al eliminar');
  }
}
