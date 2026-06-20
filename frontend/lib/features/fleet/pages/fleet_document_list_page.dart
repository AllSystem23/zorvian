import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_document_provider.dart';

final class FleetDocumentListPage extends ConsumerStatefulWidget {
  final String? entityType;
  final String? entityId;
  const FleetDocumentListPage({super.key, this.entityType, this.entityId});

  @override
  ConsumerState<FleetDocumentListPage> createState() => _FleetDocumentListPageState();
}

final class _FleetDocumentListPageState extends ConsumerState<FleetDocumentListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';
  String? _statusFilter;

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(fleetDocumentProvider.notifier).load());
  }

  List<FleetDocumentItem> _filter(List<FleetDocumentItem> items) {
    var filtered = items;
    if (widget.entityType != null && widget.entityId != null) {
      filtered = filtered.where((d) => d.entityType == widget.entityType && d.entityId == widget.entityId).toList();
    }
    if (_statusFilter != null) {
      filtered = filtered.where((d) => d.status == _statusFilter).toList();
    }
    if (_searchQuery.isNotEmpty) {
      final q = _searchQuery.toLowerCase();
      filtered = filtered.where((d) =>
        d.documentNumber.toLowerCase().contains(q) ||
        d.documentTypeName.toLowerCase().contains(q) ||
        d.notes?.toLowerCase().contains(q) == true
      ).toList();
    }
    return filtered;
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'Valid': return ZColors.success;
      case 'Expired': return ZColors.danger;
      case 'Pending': return ZColors.warning;
      default: return ZColors.neutral400;
    }
  }

  Color _expiryColor(int days) {
    if (days < 0) return ZColors.danger;
    if (days <= 30) return ZColors.warning;
    return ZColors.success;
  }

  String _entityLabel(String entityType) {
    switch (entityType) {
      case 'Vehicle': return 'Vehículo';
      case 'Driver': return 'Conductor';
      default: return entityType;
    }
  }

  IconData _entityIcon(String entityType) {
    switch (entityType) {
      case 'Vehicle': return Icons.time_to_leave_outlined;
      case 'Driver': return Icons.person_outline;
      default: return Icons.description_outlined;
    }
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetDocumentProvider);
    final filtered = _filter(state.items);

    return Scaffold(
      appBar: AppBar(
        title: Text(widget.entityType != null
            ? 'Documentos de ${_entityLabel(widget.entityType!)}'
            : 'Documentos'),
      ),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
              : Column(
                  children: [
                    ZFilterBar(
                      filters: [
                        ZFilterChipData(label: 'Vigente', value: 'Valid'),
                        ZFilterChipData(label: 'Vencido', value: 'Expired'),
                        ZFilterChipData(label: 'Pendiente', value: 'Pending'),
                      ],
                      activeFilter: _statusFilter,
                      onFilterChanged: (v) => setState(() => _statusFilter = v),
                      showSearch: true,
                      searchController: _searchCtrl,
                      onSearchChanged: (v) => setState(() => _searchQuery = v),
                    ),
                    Expanded(
                      child: filtered.isEmpty
                          ? _searchQuery.isNotEmpty || _statusFilter != null
                              ? const ZEmptyState.search()
                              : ZEmptyState.list(
                                  itemType: 'documentos',
                                  actionLabel: 'Nuevo Documento',
                                  onAction: () async {
                                    final r = await context.push<bool>('/fleet/documents/new');
                                    if (r == true) ref.read(fleetDocumentProvider.notifier).load();
                                  },
                                )
                          : RefreshIndicator(
                              onRefresh: () => ref.read(fleetDocumentProvider.notifier).load(),
                              child: ListView.separated(
                                itemCount: filtered.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final d = filtered[i];
                                  final days = d.daysToExpiry;

                                  return ListTile(
                                    leading: _docTypeIcon(d.documentTypeName),
                                    title: Text('${d.documentTypeName} · ${d.documentNumber}',
                                        style: const TextStyle(fontWeight: FontWeight.w600)),
                                    subtitle: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        Text('Vence: ${d.expiryDate ?? "Sin vencimiento"}'),
                                        if (widget.entityType == null)
                                          Row(
                                            children: [
                                              Icon(_entityIcon(d.entityType), size: 14, color: ZColors.moduleFleet),
                                              const SizedBox(width: 4),
                                              Text(_entityLabel(d.entityType), style: const TextStyle(fontSize: 12)),
                                            ],
                                          ),
                                      ],
                                    ),
                                    trailing: Row(
                                      mainAxisSize: MainAxisSize.min,
                                      children: [
                                        if (d.documentTypeHasExpiry)
                                          Container(
                                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                            decoration: BoxDecoration(
                                              color: _expiryColor(days).withValues(alpha: 0.15),
                                              borderRadius: BorderRadius.circular(12),
                                            ),
                                            child: Text(
                                              days < 0 ? 'Vencido' : '${days}d',
                                              style: TextStyle(fontSize: 11, color: _expiryColor(days), fontWeight: FontWeight.w600),
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
                                              d.status,
                                              style: TextStyle(fontSize: 11, color: _statusColor(d.status), fontWeight: FontWeight.w600),
                                            ),
                                        ),
                                        PopupMenuButton<String>(
                                          onSelected: (action) async {
                                            if (action == 'edit') {
                                              final r = await context.push<bool>('/fleet/documents/${d.id}/edit', extra: d);
                                              if (r == true) ref.read(fleetDocumentProvider.notifier).load();
                                            }
                                            if (action == 'delete') _confirmDelete(d.id, d.documentNumber);
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

  Widget _docTypeIcon(String typeName) {
    final hash = typeName.hashCode;
    final colors = [ZColors.moduleFleet, ZColors.info, ZColors.success, ZColors.warning, ZColors.danger];
    final color = colors[hash.abs() % colors.length];
    return CircleAvatar(
      backgroundColor: color.withValues(alpha: 0.2),
      child: Icon(Icons.description_outlined, color: color, size: 20),
    );
  }

  Future<void> _confirmDelete(String id, String docNumber) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar documento',
      message: '¿Eliminar el documento $docNumber?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
    );
    if (ok != true) return;
    final success = await ref.read(fleetDocumentProvider.notifier).delete(id);
    if (mounted && !success) ZToast.error(context, 'Error al eliminar');
  }
}
