import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import 'package:zorvian/core/widgets/responsive_layout.dart';
import '../providers/cash_register_provider.dart';

final class CashRegisterListPage extends ConsumerStatefulWidget {
  const CashRegisterListPage({super.key});
  @override
  ConsumerState<CashRegisterListPage> createState() =>
      _CashRegisterListPageState();
}

final class _CashRegisterListPageState
    extends ConsumerState<CashRegisterListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(cashRegisterProvider.notifier).load());
  }

  List<CashRegisterItem> _filter(List<CashRegisterItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items
        .where(
          (c) =>
              c.code.toLowerCase().contains(q) ||
              c.status.toLowerCase().contains(q),
        )
        .toList();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(cashRegisterProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);

    final totalExpected = state.items.fold(
      0.0,
      (sum, item) => sum + item.expectedBalance,
    );
    final totalDifference = state.items.fold(
      0.0,
      (sum, item) => sum + item.difference,
    );
    final activeRegisters = state.items.where((i) => i.isOpen).length;

    return Scaffold(
      appBar: AppBar(title: const Text('Control de Cajas')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
          ? Center(
              child: Text(
                state.error!,
                style: TextStyle(color: theme.colorScheme.error),
              ),
            )
          : SingleChildScrollView(
              padding: const EdgeInsets.all(ZSpacing.lg),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ── Summary Stats ──
                  ResponsiveGrid(
                    mobileColumns: 1,
                    tabletColumns: 3,
                    desktopColumns: 3,
                    children: [
                      ZStatCard(
                        title: 'Cajas Abiertas',
                        value: '$activeRegisters',
                        icon: Icons.lock_open_outlined,
                        variant: ZStatVariant.success,
                      ),
                      ZStatCard(
                        title: 'Saldo Esperado Total',
                        value: 'C\$ ${totalExpected.toStringAsFixed(2)}',
                        icon: Icons.account_balance_wallet_outlined,
                        variant: ZStatVariant.primary,
                      ),
                      ZStatCard(
                        title: 'Diferencia Acumulada',
                        value: 'C\$ ${totalDifference.toStringAsFixed(2)}',
                        icon: Icons.warning_amber_outlined,
                        variant: totalDifference.abs() > 0
                            ? ZStatVariant.danger
                            : ZStatVariant.neutral,
                      ),
                    ],
                  ),

                  const SizedBox(height: ZSpacing.xl),

                  Text(
                    'Historial de Arqueos y Movimientos',
                    style: ZTypography.titleLarge,
                  ),
                  const SizedBox(height: ZSpacing.md),

                  // ── Data Table ──
                  SizedBox(
                    height: 600,
                    child: ZDataTable<CashRegisterItem>(
                      columns: const [
                        ZColumn(id: 'code', label: 'Código / Terminal'),
                        ZColumn(id: 'date', label: 'Fecha Apertura'),
                        ZColumn(
                          id: 'expected',
                          label: 'Esperado',
                          numeric: true,
                        ),
                        ZColumn(id: 'diff', label: 'Diferencia', numeric: true),
                        ZColumn(id: 'status', label: 'Estado'),
                        ZColumn(id: 'actions', label: ''),
                      ],
                      rows: filtered,
                      onSearch: (v) => setState(() => _searchQuery = v),
                      rowMapper: (c) {
                        final diffColor = c.difference.abs() > 0.01
                            ? ZColors.danger
                            : ZColors.success;
                        return DataRow(
                          cells: [
                            DataCell(
                              Text(
                                c.code,
                                style: const TextStyle(
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ),
                            DataCell(
                              Text(
                                c.openedAt.length >= 10
                                    ? c.openedAt.substring(0, 10)
                                    : c.openedAt,
                              ),
                            ),
                            DataCell(
                              Text(
                                'C\$ ${c.expectedBalance.toStringAsFixed(2)}',
                              ),
                            ),
                            DataCell(
                              Text(
                                'C\$ ${c.difference.toStringAsFixed(2)}',
                                style: TextStyle(
                                  color: diffColor,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ),
                            DataCell(
                              ZBadge(
                                text: c.status.toUpperCase(),
                                type: c.isOpen
                                    ? ZBadgeType.success
                                    : ZBadgeType.neutral,
                              ),
                            ),
                            DataCell(
                              IconButton(
                                icon: const Icon(Icons.chevron_right),
                                onPressed: () =>
                                    context.push('/cash-registers/${c.id}'),
                              ),
                            ),
                          ],
                        );
                      },
                    ),
                  ),
                ],
              ),
            ),
    );
  }
}
