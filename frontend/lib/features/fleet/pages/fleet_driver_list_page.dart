import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../shared/ds/ds.dart';
import '../providers/driver_provider.dart';

final class FleetDriverListPage extends ConsumerStatefulWidget {
  const FleetDriverListPage({super.key});

  @override
  ConsumerState<FleetDriverListPage> createState() => _FleetDriverListPageState();
}

final class _FleetDriverListPageState extends ConsumerState<FleetDriverListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(driverProvider.notifier).load());
  }

  List<FleetDriverItem> _filter(List<FleetDriverItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items.where((d) =>
      d.fullName.toLowerCase().contains(q) ||
      d.idDocument.toLowerCase().contains(q) ||
      d.licenseNumber.toLowerCase().contains(q)
    ).toList();
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'Active': return ZColors.success;
      case 'Suspended': return ZColors.warning;
      case 'Inactive': return ZColors.neutral400;
      default: return ZColors.neutral400;
    }
  }

  String _statusLabel(String status) {
    switch (status) {
      case 'Active': return 'Activo';
      case 'Suspended': return 'Suspensión';
      case 'Vacation': return 'Vacaciones';
      case 'Inactive': return 'Inactivo';
      case 'Terminated': return 'Despedido';
      default: return status;
    }
  }

  Color _licenseExpiryColor(String? expiryDate) {
    if (expiryDate == null) return ZColors.neutral400;
    final expiry = DateTime.tryParse(expiryDate);
    if (expiry == null) return ZColors.neutral400;
    final daysLeft = expiry.difference(DateTime.now()).inDays;
    if (daysLeft < 0) return ZColors.danger;
    if (daysLeft <= 30) return ZColors.warning;
    return ZColors.success;
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(driverProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);

    return Scaffold(
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
                          hintText: 'Buscar por nombre, documento, licencia...',
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
                                  itemType: 'conductores',
                                  actionLabel: 'Nuevo Conductor',
                                  onAction: () => context.push('/fleet/drivers/new'),
                                )
                          : RefreshIndicator(
                              onRefresh: () => ref.read(driverProvider.notifier).load(),
                              child: ListView.separated(
                                itemCount: filtered.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final d = filtered[i];
                                  return ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor: theme.colorScheme.primaryContainer,
                                      child: Text('${d.firstName[0]}${d.lastName[0]}',
                                          style: TextStyle(fontWeight: FontWeight.w600, color: theme.colorScheme.onPrimaryContainer)),
                                    ),
                                    title: Text(d.fullName, style: const TextStyle(fontWeight: FontWeight.w600)),
                                    subtitle: Text('${d.licenseNumber}  ·  ${d.branchName ?? ""}'),
                                    trailing: Row(
                                      mainAxisSize: MainAxisSize.min,
                                      children: [
                                        Container(
                                          width: 10, height: 10,
                                          decoration: BoxDecoration(
                                            color: _licenseExpiryColor(d.licenseExpiryDate),
                                            shape: BoxShape.circle,
                                          ),
                                        ),
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
                                          onSelected: (action) {
                                            if (action == 'edit') context.push('/fleet/drivers/${d.id}/edit');
                                            if (action == 'delete') _confirmDelete(d.id, d.fullName);
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

  Future<void> _confirmDelete(String id, String name) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar conductor',
      message: '¿Eliminar a $name?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
    );
    if (ok != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('fleet/drivers/$id');
      ref.read(driverProvider.notifier).load();
    } catch (_) {
      if (mounted) ZToast.error(context, 'Error al eliminar');
    }
  }
}
