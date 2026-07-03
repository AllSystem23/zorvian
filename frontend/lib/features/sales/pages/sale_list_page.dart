import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/components/z_badge.dart';
import '../../../shared/ds/components/z_empty_state.dart';
import '../../../shared/ds/components/z_error_boundary.dart';
import '../../../shared/ds/components/z_pagination.dart';
import '../../../shared/ds/components/z_search_field.dart';
import '../../../shared/ds/tokens/colors.dart';
import '../providers/sale_provider.dart';

final class SaleListPage extends ConsumerStatefulWidget {
  const SaleListPage({super.key});
  @override
  ConsumerState<SaleListPage> createState() => _SaleListPageState();
}

final class _SaleListPageState extends ConsumerState<SaleListPage> {
  final _searchCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(saleProvider.notifier).load());
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(saleProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Ventas')),
      floatingActionButton: FloatingActionButton.extended(
        heroTag: 'newSale',
        icon: const Icon(Icons.add),
        label: const Text('Nueva Venta'),
        onPressed: () async {
          await context.push('/sales/new');
          ref.read(saleProvider.notifier).load();
        },
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
            child: ZSearchField(
              controller: _searchCtrl,
              hintText: 'Buscar por cliente o factura...',
              onChanged: (v) => ref.read(saleProvider.notifier).load(search: v.isNotEmpty ? v : null),
            ),
          ),
          Expanded(
            child: state.loading && state.items.isEmpty
                ? const Center(child: CircularProgressIndicator())
                : state.error != null
                    ? Center(child: ZErrorDisplay(message: state.error!, onRetry: () => ref.read(saleProvider.notifier).load()))
                    : state.items.isEmpty
                        ? _searchCtrl.text.isNotEmpty
                            ? const ZEmptyState.search()
                            : ZEmptyState.list(
                                itemType: 'ventas',
                                actionLabel: 'Nueva Venta',
                                onAction: () async {
                                  await context.push('/sales/new');
                                  ref.read(saleProvider.notifier).load();
                                },
                              )
                        : RefreshIndicator(
                            onRefresh: () => ref.read(saleProvider.notifier).load(search: _searchCtrl.text.isNotEmpty ? _searchCtrl.text : null),
                            child: ListView.separated(
                              itemCount: state.items.length + 1,
                              separatorBuilder: (_, _) => const Divider(height: 1),
                              itemBuilder: (_, i) {
                                if (i == state.items.length) {
                                  return ZPagination(
                                    currentPage: state.page,
                                    totalPages: (state.total / state.pageSize).ceil(),
                                    totalItems: state.total,
                                    pageSize: state.pageSize,
                                    onPageChanged: (p) => ref.read(saleProvider.notifier).load(page: p),
                                  );
                                }
                                final s = state.items[i];
                                final badgeType = switch (s.status) {
                                  'completed' => ZBadgeType.success,
                                  'cancelled' || 'refunded' => ZBadgeType.danger,
                                  'pending' => ZBadgeType.warning,
                                  _ => ZBadgeType.neutral,
                                };
                                return ListTile(
                                  leading: CircleAvatar(
                                    backgroundColor: ZColors.neutral100,
                                    child: Text(
                                      s.invoiceNumber.length > 4
                                          ? s.invoiceNumber.substring(s.invoiceNumber.length - 4)
                                          : s.invoiceNumber,
                                      style: TextStyle(fontSize: 10, color: ZColors.neutral600, fontWeight: FontWeight.bold),
                                    ),
                                  ),
                                  title: Text(s.clientName, style: const TextStyle(fontWeight: FontWeight.w600)),
                                  subtitle: Text('\$${s.total.toStringAsFixed(0)} \u00b7 ${s.saleType} \u00b7 ${s.saleDate.length >= 10 ? s.saleDate.substring(0, 10) : s.saleDate}'),
                                  trailing: ZBadge(text: s.status, type: badgeType),
                                  onTap: () => context.push('/sales/${s.id}'),
                                );
                              },
                            ),
                          ),
          ),
        ],
      ),
    );
  }
}