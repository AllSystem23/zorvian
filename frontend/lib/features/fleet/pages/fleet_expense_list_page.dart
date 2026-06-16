import 'dart:typed_data';

import 'package:dio/dio.dart' show Options, ResponseType;
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:share_plus/share_plus.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../shared/ds/ds.dart';
import '../providers/fleet_expense_provider.dart';

final class FleetExpenseListPage extends ConsumerStatefulWidget {
  const FleetExpenseListPage({super.key});

  @override
  ConsumerState<FleetExpenseListPage> createState() => _FleetExpenseListPageState();
}

enum _ExpenseFilter { all, pending, approved }

final class _FleetExpenseListPageState extends ConsumerState<FleetExpenseListPage> {
  bool _approving = false;
  bool _multiSelectMode = false;
  _ExpenseFilter _filter = _ExpenseFilter.all;
  final Set<String> _selectedIds = {};

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(fleetExpenseProvider.notifier).load());
  }

  String _paymentLabel(String method) {
    switch (method) {
      case 'Cash': return 'Efectivo';
      case 'Card': return 'Tarjeta';
      case 'Transfer': return 'Transferencia';
      case 'Check': return 'Cheque';
      default: return method;
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetExpenseProvider);

    return Scaffold(
      appBar: AppBar(
        title: _multiSelectMode
            ? Text('${_selectedIds.length} seleccionados')
            : const Text('Gastos de Flota'),
        bottom: _multiSelectMode ? null : PreferredSize(
          preferredSize: const Size.fromHeight(48),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Row(
              children: [ ..._ExpenseFilter.values.map((f) {
                final label = switch (f) {
                  _ExpenseFilter.all => 'Todos',
                  _ExpenseFilter.pending => 'Pendientes',
                  _ExpenseFilter.approved => 'Aprobados',
                };
                final count = switch (f) {
                  _ExpenseFilter.all => state.items.length,
                  _ExpenseFilter.pending => state.items.where((e) => !e.approved).length,
                  _ExpenseFilter.approved => state.items.where((e) => e.approved).length,
                };
                final selected = _filter == f;
                return Padding(
                  padding: const EdgeInsets.only(right: 8),
                  child: ChoiceChip(
                    label: Text('$label ($count)'),
                    selected: selected,
                    onSelected: (_) => setState(() => _filter = f),
                    selectedColor: ZColors.brandAccent.withValues(alpha: 0.15),
                    labelStyle: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: selected ? ZColors.brandAccent : ZColors.neutral600,
                    ),
                  ),
                );
              }),],
            ),
          ),
        ),
        leading: _multiSelectMode
            ? IconButton(
                icon: const Icon(Icons.close),
                onPressed: () => setState(() { _multiSelectMode = false; _selectedIds.clear(); }),
              )
            : null,
        actions: [
          if (_multiSelectMode && _selectedIds.isNotEmpty)
            IconButton(
              icon: const Icon(Icons.check_circle),
              color: ZColors.success,
              tooltip: 'Aprobar seleccionados',
              onPressed: _approveBatch,
            ),
          if (!_multiSelectMode) ...[
            IconButton(
              icon: const Icon(Icons.download_outlined),
              tooltip: 'Exportar PDF',
              onPressed: () => _exportExpenses('pdf'),
            ),
            IconButton(
              icon: const Icon(Icons.table_chart_outlined),
              tooltip: 'Exportar Excel',
              onPressed: () => _exportExpenses('xlsx'),
            ),
            IconButton(
              icon: const Icon(Icons.checklist),
              tooltip: 'Seleccionar varios',
              onPressed: () => setState(() => _multiSelectMode = true),
            ),
          ],
        ],
      ),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
              : state.items.isEmpty
                  ? ZEmptyState.list(
                      itemType: 'gastos',
                      actionLabel: 'Nuevo Gasto',
                      onAction: () async {
                        final r = await context.push<bool>('/fleet/expenses/new');
                        if (r == true) ref.read(fleetExpenseProvider.notifier).load();
                      },
                    )
                  : Column(
                    children: [
                      _ExpenseKpiBar(items: state.items, activeFilter: _filter),
                      Expanded(
                        child: RefreshIndicator(
                          onRefresh: () => ref.read(fleetExpenseProvider.notifier).load(),
                          child: Builder(
                            builder: (_) {
                              final items = _filteredItems(state);
                              return ListView.separated(
                                itemCount: items.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final e = items[i];
                                  return ListTile(
                                    leading: _multiSelectMode
                                        ? Checkbox(
                                            value: _selectedIds.contains(e.id),
                                            onChanged: (v) => setState(() {
                                              if (v == true) {
                                                _selectedIds.add(e.id);
                                              } else {
                                                _selectedIds.remove(e.id);
                                              }
                                            }),
                                          )
                                        : CircleAvatar(
                                            backgroundColor: ZColors.moduleFinance.withValues(alpha: 0.2),
                                            child: Icon(
                                              e.approved ? Icons.check_circle : Icons.schedule,
                                              color: e.approved ? ZColors.success : ZColors.warning,
                                              size: 20,
                                            ),
                                          ),
                                    title: Row(
                                      children: [
                                        Expanded(child: Text(e.description, style: const TextStyle(fontWeight: FontWeight.w600))),
                                        Container(
                                          padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
                                          decoration: BoxDecoration(
                                            color: e.approved
                                                ? ZColors.success.withValues(alpha: 0.15)
                                                : ZColors.warning.withValues(alpha: 0.15),
                                            borderRadius: BorderRadius.circular(8),
                                          ),
                                          child: Text(
                                            e.approved ? 'Aprobado' : 'Pendiente',
                                            style: TextStyle(
                                              fontSize: 10,
                                              fontWeight: FontWeight.w600,
                                              color: e.approved ? ZColors.success : ZColors.warning,
                                            ),
                                          ),
                                        ),
                                      ],
                                    ),
                                    subtitle: Text(
                                      '${e.currency} \$${e.amount.toStringAsFixed(2)} · ${e.categoryName}${e.vehiclePlate != null ? ' · ${e.vehiclePlate}' : ''}',
                                    ),
                                    trailing: _multiSelectMode
                                        ? null
                                        : Row(
                                            mainAxisSize: MainAxisSize.min,
                                            children: [
                                              Column(
                                                mainAxisAlignment: MainAxisAlignment.center,
                                                crossAxisAlignment: CrossAxisAlignment.end,
                                                children: [
                                                  Container(
                                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                                    decoration: BoxDecoration(
                                                      color: _paymentLabel(e.paymentMethod) == e.paymentMethod
                                                          ? ZColors.moduleFleet.withValues(alpha: 0.15)
                                                          : ZColors.success.withValues(alpha: 0.15),
                                                      borderRadius: BorderRadius.circular(10),
                                                    ),
                                                    child: Text(
                                                      _paymentLabel(e.paymentMethod),
                                                      style: TextStyle(fontSize: 10, color: ZColors.moduleFleet, fontWeight: FontWeight.w600),
                                                    ),
                                                  ),
                                                  if (e.approved)
                                                    Padding(
                                                      padding: const EdgeInsets.only(top: 2),
                                                      child: Icon(Icons.check_circle, size: 14, color: ZColors.success),
                                                    ),
                                                ],
                                              ),
                                              if (!e.approved && !_approving)
                                                IconButton(
                                                  icon: const Icon(Icons.check_circle_outline, size: 20),
                                                  color: ZColors.success,
                                                  tooltip: 'Aprobar gasto',
                                                  onPressed: () => _confirmApprove(e.id, e.description),
                                                ),
                                              if (_approving)
                                                const SizedBox(
                                                  width: 20,
                                                  height: 20,
                                                  child: CircularProgressIndicator(strokeWidth: 2),
                                                ),
                                              PopupMenuButton<String>(
                                                onSelected: (action) async {
                                                  if (action == 'edit') {
                                                    final r2 = await context.push<bool>('/fleet/expenses/${e.id}/edit');
                                                    if (r2 == true) ref.read(fleetExpenseProvider.notifier).load();
                                                  }
                                                  if (action == 'delete') _confirmDelete(e.id, e.description);
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
                              );
                            },
                          ),
                        ),
                      ),                    ],
                  ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final r = await context.push<bool>('/fleet/expenses/new');
          if (r == true) ref.read(fleetExpenseProvider.notifier).load();
        },
        child: const Icon(Icons.add),
      ),
    );
  }

  Future<void> _approveBatch() async {
    final unapproved = _selectedIds
        .where((id) => ref.read(fleetExpenseProvider).items
            .any((e) => e.id == id && !e.approved))
        .toList();
    if (unapproved.isEmpty) {
      ZToast.warning(context, 'Todos los seleccionados ya están aprobados');
      return;
    }
    final ok = await ZModal.confirm(
      context,
      title: 'Aprobar en lote',
      message: '¿Aprobar ${unapproved.length} gastos? La IA clasificará automáticamente cada cuenta contable.',
      confirmText: 'Aprobar todo',
      cancelText: 'Cancelar',
    );
    if (ok != true) return;

    setState(() => _approving = true);
    final result = await ref.read(fleetExpenseProvider.notifier).approveBatch(unapproved);
    if (mounted) {
      setState(() {
        _approving = false;
        _multiSelectMode = false;
        _selectedIds.clear();
      });
      if (result != null) {
        final approved = result['approved'] ?? 0;
        final failed = result['failed'] ?? 0;
        if (failed > 0) {
          ZToast.warning(context, '$approved aprobados, $failed fallaron');
        } else {
          ZToast.success(context, '$approved gastos aprobados y asientos contables generados');
        }
      } else {
        ZToast.error(context, 'Error al aprobar gastos en lote');
      }
    }
  }

  Future<void> _confirmApprove(String id, String desc) async {
    final ok = await ZModal.confirm(
      context,
      title: 'Aprobar gasto',
      message: '¿Aprobar "$desc"? La IA clasificará automáticamente la cuenta contable y se generará el asiento.',
      confirmText: 'Aprobar',
      cancelText: 'Cancelar',
    );
    if (ok != true) return;

    setState(() => _approving = true);

    // Step 1: Classify with ML to get best account suggestion
    final expense = ref.read(fleetExpenseProvider).items
        .where((e) => e.id == id).toList();
    String? bestAccountId;
    if (expense.isNotEmpty) {
      final classification = await ref.read(fleetExpenseProvider.notifier)
          .classify(desc, expense.first.amount);
      if (!mounted) return;
      if (classification != null) {
        final suggestions = (classification['suggestions'] as List?)?.cast<Map<String, dynamic>>() ?? [];
        if (suggestions.isNotEmpty) {
          bestAccountId = suggestions.first['accountId'] as String?;
        }
      }
    }

    // Step 2: Approve with best ML account (or empty for auto-fallback)
    final success = await ref.read(fleetExpenseProvider.notifier)
        .approve(id, accountId: bestAccountId);

    if (mounted) {
      setState(() => _approving = false);
      if (success) {
        ZToast.success(context, 'Gasto aprobado y asiento contable generado');
      } else {
        ZToast.error(context, 'Error al aprobar gasto');
      }
    }
  }

  List<FleetExpenseItem> _filteredItems(FleetExpenseState state) {
    return switch (_filter) {
      _ExpenseFilter.all => state.items,
      _ExpenseFilter.pending => state.items.where((e) => !e.approved).toList(),
      _ExpenseFilter.approved => state.items.where((e) => e.approved).toList(),
    };
  }

  Future<void> _confirmDelete(String id, String desc) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar gasto',
      message: '¿Eliminar el gasto "$desc"?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
    );
    if (ok != true) return;
    final success = await ref.read(fleetExpenseProvider.notifier).delete(id);
    if (mounted && !success) ZToast.error(context, 'Error al eliminar');
  }

  Future<void> _exportExpenses(String format) async {
    try {
      final dio = ref.read(dioClientProvider);
      // Build query params based on current filter
      final params = <String, String>{'format': format};
      if (_filter == _ExpenseFilter.pending) params['status'] = 'pending';
      if (_filter == _ExpenseFilter.approved) params['status'] = 'approved';
      final qs = params.entries.map((e) => '${e.key}=${e.value}').join('&');

      final r = await dio.post('fleet/expenses/export?$qs',
        options: Options(responseType: ResponseType.bytes));
      final blob = r.data as List<int>;
      final ext = format == 'pdf' ? 'pdf' : 'xlsx';
      final mimeType = format == 'pdf' ? 'application/pdf' : 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
      final fileName = 'Gastos_Flota.$ext';

      await SharePlus.instance.share(
        ShareParams(files: [
          XFile.fromData(Uint8List.fromList(blob),
            mimeType: mimeType,
            name: fileName),
        ]),
      );
      if (mounted) ZToast.success(context, 'Exportado: $fileName');
    } catch (_) {
      if (mounted) ZToast.error(context, 'Error al exportar');
    }
  }
}

// ══════════════════════════════════════════════
//  EXPENSE KPI BAR
// ══════════════════════════════════════════════

class _ExpenseKpiBar extends StatelessWidget {
  final List<FleetExpenseItem> items;
  final _ExpenseFilter activeFilter;
  const _ExpenseKpiBar({required this.items, required this.activeFilter});

  @override
  Widget build(BuildContext context) {
    final total = items.length;
    final pending = items.where((e) => !e.approved).length;
    final approved = items.where((e) => e.approved).length;
    final totalAmount = items.fold<double>(0, (sum, e) => sum + e.amount);
    final pendingAmount = items.where((e) => !e.approved).fold<double>(0, (sum, e) => sum + e.amount);
    final approvedAmount = items.where((e) => e.approved).fold<double>(0, (sum, e) => sum + e.amount);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surfaceContainerLow,
        border: Border(bottom: BorderSide(color: Theme.of(context).dividerColor.withValues(alpha: 0.2))),
      ),
      child: Row(
        children: [
          _KpiItem(
            icon: Icons.receipt_outlined,
            label: 'Total',
            value: '$total',
            subValue: 'C\$ ${totalAmount.toStringAsFixed(2)}',
            color: ZColors.moduleFleet,
            highlighted: activeFilter == _ExpenseFilter.all,
            progress: 1.0,
          ),
          const SizedBox(width: 16),
          _KpiItem(
            icon: Icons.schedule_outlined,
            label: 'Pendientes',
            value: '$pending',
            subValue: 'C\$ ${pendingAmount.toStringAsFixed(2)}',
            color: ZColors.warning,
            highlighted: activeFilter == _ExpenseFilter.pending,
            progress: total > 0 ? pending / total : 0.0,
          ),
          const SizedBox(width: 16),
          _KpiItem(
            icon: Icons.check_circle_outlined,
            label: 'Aprobados',
            value: '$approved',
            subValue: 'C\$ ${approvedAmount.toStringAsFixed(2)}',
            color: ZColors.success,
            highlighted: activeFilter == _ExpenseFilter.approved,
            progress: total > 0 ? approved / total : 0.0,
          ),
        ],
      ),
    );
  }
}

class _KpiItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final String subValue;
  final Color color;
  final bool highlighted;
  final double progress;
  const _KpiItem({required this.icon, required this.label, required this.value, required this.subValue, required this.color, this.highlighted = false, this.progress = 0.0});

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.all(8),
        decoration: BoxDecoration(
          color: highlighted ? color.withValues(alpha: 0.08) : Colors.transparent,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(
            color: highlighted ? color.withValues(alpha: 0.3) : Colors.transparent,
            width: 1.5,
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(6),
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Icon(icon, size: 18, color: color),
                ),
                const SizedBox(width: 8),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(label, style: TextStyle(fontSize: 10, fontWeight: FontWeight.w500, color: highlighted ? color : ZColors.neutral500)),
                    Text(value, style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: color)),
                    Text(subValue, style: const TextStyle(fontSize: 11, color: ZColors.neutral600)),
                  ],
                ),
              ],
            ),
            const SizedBox(height: 6),
            ClipRRect(
              borderRadius: BorderRadius.circular(3),
              child: LinearProgressIndicator(
                value: progress.clamp(0.0, 1.0),
                minHeight: 4,
                backgroundColor: color.withValues(alpha: 0.1),
                valueColor: AlwaysStoppedAnimation<Color>(color.withValues(alpha: highlighted ? 0.9 : 0.5)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

