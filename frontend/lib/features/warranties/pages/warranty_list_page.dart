import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/shared/ds/ds.dart';
import '../providers/warranty_provider.dart';

final class WarrantyListPage extends ConsumerStatefulWidget {
  const WarrantyListPage({super.key});
  @override
  ConsumerState<WarrantyListPage> createState() => _WarrantyListPageState();
}

final class _WarrantyListPageState extends ConsumerState<WarrantyListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';
  String _statusFilter = 'all';
  int _currentPage = 1;

  @override
  void initState() {
    super.initState();
    Future.microtask(() => _load());
  }

  void _load() => ref.read(warrantyProvider.notifier).load(page: _currentPage);

  List<WarrantyItem> _filter(List<WarrantyItem> items) {
    var result = items;
    if (_statusFilter != 'all') {
      result = result.where((w) => w.status == _statusFilter).toList();
    }
    if (_searchQuery.isNotEmpty) {
      final q = _searchQuery.toLowerCase();
      result = result.where((w) =>
        w.warrantyNumber.toLowerCase().contains(q) ||
        w.clientName.toLowerCase().contains(q) ||
        w.productName.toLowerCase().contains(q)
      ).toList();
    }
    return result;
  }

  Color _statusColor(String status) => switch (status) {
    'Registered' => ZColors.brandPrimary,
    'PendingReview' => ZColors.warning,
    'InDiagnosis' => ZColors.brandSecondary,
    'SentToWorkshop' => ZColors.brandSecondary,
    'InRepair' => ZColors.brandAccent,
    'Repaired' => ZColors.success,
    'ReplacementApproved' => ZColors.success,
    'ReadyForDelivery' => ZColors.brandAccent,
    'Delivered' => ZColors.success,
    'Closed' => ZColors.neutral900,
    'Rejected' => ZColors.danger,
    'Cancelled' => ZColors.danger,
    _ => ZColors.brandSecondary,
  };

  String _statusLabel(String status) => switch (status) {
    'Registered' => 'Registrada',
    'PendingReview' => 'Revisión',
    'InDiagnosis' => 'Diagnóstico',
    'SentToWorkshop' => 'En taller',
    'InRepair' => 'Reparando',
    'Repaired' => 'Reparada',
    'ReplacementApproved' => 'Reemplazo',
    'ReadyForDelivery' => 'Lista para entregar',
    'Delivered' => 'Entregada',
    'Closed' => 'Cerrada',
    'Rejected' => 'Rechazada',
    'Cancelled' => 'Cancelada',
    _ => status,
  };

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(warrantyProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Garantías'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: 'Nueva garantía',
            onPressed: () => context.push('/warranties/new'),
          ),
        ],
      ),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
          ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
          : Column(
              children: [
                // ── Búsqueda + Filtros ──
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
                  child: Column(children: [
                    TextField(
                      controller: _searchCtrl,
                      decoration: InputDecoration(
                        hintText: 'Buscar por número, cliente o producto...',
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
                    const SizedBox(height: 8),
                    SizedBox(
                      height: 36,
                      child: ListView(
                        scrollDirection: Axis.horizontal,
                        children: [
                          _FilterChip(label: 'Todas', selected: _statusFilter == 'all', onTap: () => setState(() => _statusFilter = 'all')),
                          _FilterChip(label: 'Registradas', selected: _statusFilter == 'Registered', onTap: () => setState(() => _statusFilter = 'Registered')),
                          _FilterChip(label: 'En revisión', selected: _statusFilter == 'PendingReview', onTap: () => setState(() => _statusFilter = 'PendingReview')),
                          _FilterChip(label: 'En taller', selected: _statusFilter == 'SentToWorkshop', onTap: () => setState(() => _statusFilter = 'SentToWorkshop')),
                          _FilterChip(label: 'Reparadas', selected: _statusFilter == 'Repaired', onTap: () => setState(() => _statusFilter = 'Repaired')),
                          _FilterChip(label: 'Entregadas', selected: _statusFilter == 'Delivered', onTap: () => setState(() => _statusFilter = 'Delivered')),
                          _FilterChip(label: 'Cerradas', selected: _statusFilter == 'Closed', onTap: () => setState(() => _statusFilter = 'Closed')),
                        ],
                      ),
                    ),
                  ]),
                ),

                // ── Lista ──
                Expanded(
                  child: filtered.isEmpty
                      ? _searchQuery.isNotEmpty || _statusFilter != 'all'
                          ? const ZEmptyState.search()
                          : ZEmptyState.list(
                              itemType: 'garantías',
                              actionLabel: 'Nueva Garantía',
                              onAction: () => context.push('/warranties/new'),
                            )
                      : RefreshIndicator(
                          onRefresh: () async => _load(),
                          child: ListView.separated(
                            itemCount: filtered.length,
                            separatorBuilder: (_, _) => const Divider(height: 1),
                            itemBuilder: (_, i) {
                              final w = filtered[i];
                              final isExpiring = w.endDate != null &&
                                  DateTime.parse(w.endDate!).difference(DateTime.now()).inDays < 30;
                              return ListTile(
                                leading: CircleAvatar(
                                  backgroundColor: _statusColor(w.status).withAlpha(30),
                                  child: Icon(Icons.shield, size: 18, color: _statusColor(w.status)),
                                ),
                                title: Text(w.warrantyNumber, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
                                subtitle: Text('${w.productName} · ${w.clientName}', maxLines: 1, overflow: TextOverflow.ellipsis),
                                trailing: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    if (isExpiring)
                                      Container(
                                        margin: const EdgeInsets.only(right: 8),
                                        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                        decoration: BoxDecoration(color: ZColors.warning.withAlpha(25), borderRadius: BorderRadius.circular(8)),
                                        child: const Text('Por vencer', style: TextStyle(fontSize: 10, color: ZColors.warning)),
                                      ),
                                    Chip(
                                      label: Text(_statusLabel(w.status), style: const TextStyle(fontSize: 10)),
                                      backgroundColor: _statusColor(w.status).withAlpha(30),
                                      padding: EdgeInsets.zero,
                                      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                    ),
                                  ],
                                ),
                                onTap: () => context.push('/warranties/${w.id}'),
                              );
                            },
                          ),
                        ),
                ),

                // ── Paginación ──
                if (state.items.length >= 20)
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      IconButton(
                        icon: const Icon(Icons.chevron_left),
                        onPressed: _currentPage > 1 ? () { _currentPage--; _load(); } : null,
                      ),
                      Text('Página $_currentPage'),
                      IconButton(
                        icon: const Icon(Icons.chevron_right),
                        onPressed: state.items.length >= 20 ? () { _currentPage++; _load(); } : null,
                      ),
                    ],
                  ),
              ],
            ),
    );
  }
}

class _FilterChip extends StatelessWidget {
  final String label;
  final bool selected;
  final VoidCallback onTap;

  const _FilterChip({required this.label, required this.selected, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(right: 6),
      child: GestureDetector(
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
          decoration: BoxDecoration(
            color: selected ? ZColors.brandPrimary : ZColors.neutral100,
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: selected ? ZColors.brandPrimary : ZColors.neutral300),
          ),
          child: Text(
            label,
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: selected ? Colors.white : ZColors.neutral700,
            ),
          ),
        ),
      ),
    );
  }
}
