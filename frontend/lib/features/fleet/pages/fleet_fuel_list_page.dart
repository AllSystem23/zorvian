import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_fuel_provider.dart';

final class FleetFuelListPage extends ConsumerStatefulWidget {
  const FleetFuelListPage({super.key});

  @override
  ConsumerState<FleetFuelListPage> createState() => _FleetFuelListPageState();
}

final class _FleetFuelListPageState extends ConsumerState<FleetFuelListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(fleetFuelProvider.notifier).load());
  }

  Color _anomalyColor(bool flag) => flag ? ZColors.danger : ZColors.success;

  String _refillTypeLabel(String type) {
    switch (type) {
      case 'Full': return 'Completo';
      case 'Partial': return 'Parcial';
      case 'Emergency': return 'Emergencia';
      default: return type;
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetFuelProvider);

    return Scaffold(
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
              : state.items.isEmpty
                  ? ZEmptyState.list(
                      itemType: 'cargas de combustible',
                      actionLabel: 'Nueva Carga',
                      onAction: () async {
                        final r = await context.push<bool>('/fleet/fuel/new');
                        if (r == true) ref.read(fleetFuelProvider.notifier).load();
                      },
                    )
                  : RefreshIndicator(
                      onRefresh: () => ref.read(fleetFuelProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final f = state.items[i];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: _anomalyColor(f.anomalyFlag).withValues(alpha: 0.2),
                              child: Icon(Icons.local_gas_station_outlined, color: _anomalyColor(f.anomalyFlag), size: 20),
                            ),
                            title: Text('${f.vehiclePlate} · ${f.liters.toStringAsFixed(1)} L', style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('\$${f.totalCost.toStringAsFixed(2)} · ${f.driverName} · ${f.fuelTypeName}'),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  crossAxisAlignment: CrossAxisAlignment.end,
                                  children: [
                                    Text('${f.currentKm.toStringAsFixed(0)} km', style: const TextStyle(fontSize: 12)),
                                    const SizedBox(height: 2),
                                    Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                      decoration: BoxDecoration(
                                        color: ZColors.moduleFleet.withValues(alpha: 0.15),
                                        borderRadius: BorderRadius.circular(10),
                                      ),
                                      child: Text(
                                        _refillTypeLabel(f.refillType),
                                        style: TextStyle(fontSize: 10, color: ZColors.moduleFleet, fontWeight: FontWeight.w600),
                                      ),
                                    ),
                                  ],
                                ),
                                PopupMenuButton<String>(
                                  onSelected: (action) async {
                                    if (action == 'edit') {
                                      final r2 = await context.push<bool>('/fleet/fuel/${f.id}/edit');
                                      if (r2 == true) ref.read(fleetFuelProvider.notifier).load();
                                    }
                                    if (action == 'delete') _confirmDelete(f.id, f.vehiclePlate);
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

  Future<void> _confirmDelete(String id, String plate) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar carga',
      message: '¿Eliminar la carga de $plate?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
    );
    if (ok != true) return;
    final success = await ref.read(fleetFuelProvider.notifier).delete(id);
    if (mounted && !success) ZToast.error(context, 'Error al eliminar');
  }
}
