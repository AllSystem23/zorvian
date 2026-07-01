import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/shared/ds/ds.dart';
import '../providers/warranty_provider.dart';
import '../utils/warranty_utils.dart';

final class WarrantyListPage extends ConsumerStatefulWidget {
  const WarrantyListPage({super.key});
  @override
  ConsumerState<WarrantyListPage> createState() => _WarrantyListPageState();
}

class _WarrantyListPageState extends ConsumerState<WarrantyListPage> {
  final _searchCtrl = TextEditingController();
  int _currentPage = 1;
  String? _statusFilter;
  int _totalItems = 0;
  static const _pageSize = 20;

  @override
  void initState() {
    super.initState();
    Future.microtask(() => _load());
  }

  void _load() {
    ref.read(warrantyProvider.notifier).load(
      page: _currentPage,
      pageSize: _pageSize,
      status: _statusFilter,
    );
  }

  int get _totalPages => _totalItems == 0 ? 1 : (_totalItems / _pageSize).ceil();

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(warrantyProvider);

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
      body: ZErrorBoundary(
        child: Column(
          children: [
            // ── Filtros con ZFilterBar ──
            Padding(
              padding: const EdgeInsets.fromLTRB(ZSpacing.lg, ZSpacing.md, ZSpacing.lg, 0),
              child: ZFilterBar(
                searchController: _searchCtrl,
                onSearchChanged: (v) => setState(() {}),
                filters: [
                  const ZFilterChipData(label: 'Todas', value: 'all'),
                  const ZFilterChipData(label: 'Registradas', value: 'Registered'),
                  const ZFilterChipData(label: 'Revisión', value: 'PendingReview'),
                  const ZFilterChipData(label: 'Diagnóstico', value: 'InDiagnosis'),
                  const ZFilterChipData(label: 'En taller', value: 'SentToWorkshop'),
                  const ZFilterChipData(label: 'Reparando', value: 'InRepair'),
                  const ZFilterChipData(label: 'Reparadas', value: 'Repaired'),
                  const ZFilterChipData(label: 'Entregadas', value: 'Delivered'),
                  const ZFilterChipData(label: 'Cerradas', value: 'Closed'),
                ],
                activeFilter: _statusFilter ?? 'all',
                onFilterChanged: (v) {
                  setState(() {
                    _statusFilter = (v == null || v == 'all') ? null : v;
                    _currentPage = 1;
                  });
                  _load();
                },
                onClearAll: () {
                  setState(() {
                    _statusFilter = null;
                    _currentPage = 1;
                    _searchCtrl.clear();
                  });
                  _load();
                },
              ),
            ),

            // ── Contenido ──
            Expanded(
              child: state.loading
                  ? _buildSkeleton()
                  : state.error != null
                      ? ZErrorDisplay(
                          message: state.error!,
                          onRetry: () { _currentPage = 1; _load(); },
                        )
                      : _buildList(state),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSkeleton() {
    return ListView.builder(
      padding: const EdgeInsets.all(ZSpacing.lg),
      itemCount: 6,
      itemBuilder: (_, _) => Padding(
        padding: const EdgeInsets.only(bottom: ZSpacing.sm),
        child: ZSkeleton.listTile(),
      ),
    );
  }

  Widget _buildList(WarrantyState state) {
    final query = _searchCtrl.text.toLowerCase();
    var items = state.items;
    if (query.isNotEmpty) {
      items = items.where((w) =>
        w.warrantyNumber.toLowerCase().contains(query) ||
        w.clientName.toLowerCase().contains(query) ||
        w.productName.toLowerCase().contains(query)
      ).toList();
    }

    if (items.isEmpty) {
      return ZEmptyState.list(
        itemType: 'garantías',
        actionLabel: 'Nueva Garantía',
        onAction: () => context.push('/warranties/new'),
      );
    }

    // Estimar total para paginación
    if (_totalItems == 0 && items.length >= _pageSize) {
      _totalItems = items.length;
    }

    return Column(
      children: [
        Expanded(
          child: RefreshIndicator(
            onRefresh: () async => _load(),
            child: ListView.separated(
              itemCount: items.length,
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (_, i) {
                final w = items[i];
                final isExpiring = w.endDate != null &&
                    DateTime.parse(w.endDate!).difference(DateTime.now()).inDays < 30;
                return ListTile(
                  leading: CircleAvatar(
                    backgroundColor: warrantyStatusColor(w.status).withAlpha(25),
                    child: Icon(Icons.shield, size: 18, color: warrantyStatusColor(w.status)),
                  ),
                  title: Text(w.warrantyNumber, style: ZTypography.titleSmall),
                  subtitle: Text(
                    '${w.productName} · ${w.clientName}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500),
                  ),
                  trailing: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      if (isExpiring)
                        const ZBadge(text: 'Por vencer', type: ZBadgeType.warning),
                      if (isExpiring) const SizedBox(width: ZSpacing.xs),
                      ZBadge(text: warrantyStatusLabel(w.status), type: warrantyBadgeType(w.status)),
                    ],
                  ),
                  onTap: () => context.push('/warranties/${w.id}'),
                );
              },
            ),
          ),
        ),

        // ── Paginación con ZPagination ──
        if (state.items.length >= _pageSize || _currentPage > 1)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: ZSpacing.lg),
            child: ZPagination(
              currentPage: _currentPage,
              totalPages: _totalPages.clamp(1, 999),
              totalItems: _totalItems,
              pageSize: _pageSize,
              onPageChanged: (page) {
                setState(() => _currentPage = page);
                _load();
              },
            ),
          ),
      ],
    );
  }
}
