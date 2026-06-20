import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../shared/ds/ds.dart';
import '../providers/fleet_provider.dart';

final class FleetVehicleListPage extends ConsumerStatefulWidget {
  const FleetVehicleListPage({super.key});

  @override
  ConsumerState<FleetVehicleListPage> createState() => _FleetVehicleListPageState();
}

final class _FleetVehicleListPageState extends ConsumerState<FleetVehicleListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(fleetProvider.notifier).load());
  }

  List<FleetVehicleItem> _filter(List<FleetVehicleItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items.where((v) =>
      v.plate.toLowerCase().contains(q) ||
      v.brandName.toLowerCase().contains(q) ||
      v.model.toLowerCase().contains(q) ||
      v.code.toLowerCase().contains(q)
    ).toList();
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'Active': return ZColors.success;
      case 'Available': return ZColors.info;
      case 'InRoute': return ZColors.moduleFleet;
      case 'Maintenance': return ZColors.danger;
      default: return ZColors.neutral400;
    }
  }

  String _statusLabel(String status) {
    switch (status) {
      case 'Active': return 'Activo';
      case 'Available': return 'Disponible';
      case 'InRoute': return 'En Ruta';
      case 'Maintenance': return 'Mantenimiento';
      case 'OutOfService': return 'Fuera de Servicio';
      default: return status;
    }
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);

    return Scaffold(
      appBar: AppBar(title: const Text('Vehículos')),
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
                          hintText: 'Buscar por placa, marca, modelo...',
                          prefixIcon: const Icon(Icons.search),
                          suffixIcon: _searchQuery.isNotEmpty
                              ? IconButton(icon: const Icon(Icons.clear), onPressed: () {
                                  _searchCtrl.clear();
                                  setState(() => _searchQuery = '');
                                })
                              : null,
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                          contentPadding: const EdgeInsets.symmetric(vertical: 0),
                        ),
                        onChanged: (v) => setState(() => _searchQuery = v),
                      ),
                    ),
                    Expanded(
                      child: filtered.isEmpty
                          ? _searchQuery.isNotEmpty
                              ? const ZEmptyState.search()
                              : ZEmptyState.list(
                                  itemType: 'vehículos',
                                  actionLabel: 'Nuevo Vehículo',
                                  onAction: () => context.push('/fleet/vehicles/new'),
                                )
                          : RefreshIndicator(
                              onRefresh: () => ref.read(fleetProvider.notifier).load(),
                              child: ListView.separated(
                                itemCount: filtered.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final v = filtered[i];
                                  return ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor: _statusColor(v.status).withValues(alpha: 0.2),
                                      child: Icon(Icons.time_to_leave_outlined, color: _statusColor(v.status), size: 20),
                                    ),
                                    title: Text('${v.plate} · ${v.brandName} ${v.model}',
                                        style: const TextStyle(fontWeight: FontWeight.w600)),
                                    subtitle: Text('${v.code} · ${v.year} · ${v.driverName ?? "Sin conductor"}'),
                                    trailing: Row(
                                      mainAxisSize: MainAxisSize.min,
                                      children: [
                                        Container(
                                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                          decoration: BoxDecoration(
                                            color: _statusColor(v.status).withValues(alpha: 0.15),
                                            borderRadius: BorderRadius.circular(12),
                                          ),
                                          child: Text(
                                            _statusLabel(v.status),
                                            style: TextStyle(fontSize: 11, color: _statusColor(v.status), fontWeight: FontWeight.w600),
                                          ),
                                        ),
                                        PopupMenuButton<String>(
                                          onSelected: (action) {
                                            if (action == 'edit') context.push('/fleet/vehicles/${v.id}/edit');
                                            if (action == 'delete') _confirmDelete(v.id, v.plate);
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
                    ),
                  ],
                ),
    );
  }

  Future<void> _confirmDelete(String id, String plate) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar vehículo',
      message: '¿Eliminar el vehículo $plate?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
    );
    if (ok != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('fleet/vehicles/$id');
      ref.read(fleetProvider.notifier).load();
    } catch (_) {
      if (mounted) ZToast.error(context, 'Error al eliminar');
    }
  }
}
