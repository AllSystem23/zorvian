import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_maintenance_provider.dart';

final class FleetMaintenanceListPage extends ConsumerStatefulWidget {
  const FleetMaintenanceListPage({super.key});

  @override
  ConsumerState<FleetMaintenanceListPage> createState() => _FleetMaintenanceListPageState();
}

final class _FleetMaintenanceListPageState extends ConsumerState<FleetMaintenanceListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(fleetMaintenanceProvider.notifier).load());
  }

  Color _priorityColor(String priority) {
    switch (priority) {
      case 'Urgent': return ZColors.danger;
      case 'High': return Colors.deepOrange;
      case 'Medium': return ZColors.warning;
      case 'Low': return ZColors.info;
      default: return ZColors.neutral400;
    }
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'Reported': return ZColors.info;
      case 'Diagnosed': return ZColors.warning;
      case 'Approved': return Colors.deepOrange;
      case 'InRepair': return Colors.purple;
      case 'Completed': return ZColors.success;
      case 'Closed': return ZColors.neutral400;
      case 'Cancelled': return ZColors.danger;
      default: return ZColors.neutral400;
    }
  }

  String _statusLabel(String status) {
    switch (status) {
      case 'Reported': return 'Reportado';
      case 'Diagnosed': return 'Diagnosticado';
      case 'Approved': return 'Aprobado';
      case 'InRepair': return 'En reparación';
      case 'Completed': return 'Completado';
      case 'Closed': return 'Cerrado';
      case 'Cancelled': return 'Cancelado';
      default: return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetMaintenanceProvider);

    return Scaffold(
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
              : state.items.isEmpty
                  ? ZEmptyState.list(
                      itemType: 'órdenes de trabajo',
                      actionLabel: 'Nueva OT',
                      onAction: () async {
                        final r = await context.push<bool>('/fleet/maintenance/new');
                        if (r == true) ref.read(fleetMaintenanceProvider.notifier).load();
                      },
                    )
                  : RefreshIndicator(
                      onRefresh: () => ref.read(fleetMaintenanceProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final w = state.items[i];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: _priorityColor(w.priority).withValues(alpha: 0.2),
                              child: Icon(Icons.build_outlined, color: _priorityColor(w.priority), size: 20),
                            ),
                            title: Text('${w.number} · ${w.vehiclePlate}', style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text(w.failureTypeName ?? w.problemDescription ?? w.vehicleBrandModel),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  crossAxisAlignment: CrossAxisAlignment.end,
                                  children: [
                                    Text('\$${w.costTotal.toStringAsFixed(0)}', style: const TextStyle(fontSize: 12)),
                                    const SizedBox(height: 2),
                                    Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                      decoration: BoxDecoration(
                                        color: _statusColor(w.status).withValues(alpha: 0.15),
                                        borderRadius: BorderRadius.circular(10),
                                      ),
                                      child: Text(
                                        _statusLabel(w.status),
                                        style: TextStyle(fontSize: 10, color: _statusColor(w.status), fontWeight: FontWeight.w600),
                                      ),
                                    ),
                                  ],
                                ),
                                PopupMenuButton<String>(
                                  onSelected: (action) async {
                                    if (action == 'edit') {
                                      final r2 = await context.push<bool>('/fleet/maintenance/${w.id}/edit');
                                      if (r2 == true) ref.read(fleetMaintenanceProvider.notifier).load();
                                    }
                                    if (action == 'delete') _confirmDelete(w.id, w.number);
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

  Future<void> _confirmDelete(String id, String number) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar OT',
      message: '¿Eliminar la orden de trabajo $number?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
    );
    if (ok != true) return;
    final success = await ref.read(fleetMaintenanceProvider.notifier).delete(id);
    if (mounted && !success) ZToast.error(context, 'Error al eliminar');
  }
}
