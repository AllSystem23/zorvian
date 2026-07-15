import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../providers/budget_provider.dart';

final class BudgetListPage extends ConsumerStatefulWidget {
  const BudgetListPage({super.key});
  @override
  ConsumerState<BudgetListPage> createState() => _BudgetListPageState();
}

final class _BudgetListPageState extends ConsumerState<BudgetListPage> {
  int? _year;
  int? _month;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    _year = now.year;
    _month = now.month;
    Future.microtask(() => ref.read(budgetProvider.notifier).load(year: _year, month: _month));
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(budgetProvider);
    final theme = Theme.of(context);
    return Scaffold(
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    decoration: const InputDecoration(labelText: 'Año', prefixIcon: Icon(Icons.calendar_today)),
                    keyboardType: TextInputType.number,
                    controller: TextEditingController(text: _year?.toString() ?? ''),
                    onChanged: (v) => _year = int.tryParse(v),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextField(
                    decoration: const InputDecoration(labelText: 'Mes', prefixIcon: Icon(Icons.date_range)),
                    keyboardType: TextInputType.number,
                    controller: TextEditingController(text: _month?.toString() ?? ''),
                    onChanged: (v) => _month = int.tryParse(v),
                  ),
                ),
                const SizedBox(width: 8),
                IconButton(
                  icon: const Icon(Icons.search),
                  onPressed: () => ref.read(budgetProvider.notifier).load(year: _year, month: _month),
                ),
              ],
            ),
          ),
          Expanded(
            child: state.loading
                ? const Center(child: CircularProgressIndicator())
                : state.error != null
                    ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
                    : state.items.isEmpty
                        ? const Center(child: Text('No hay presupuestos para este período'))
                        : RefreshIndicator(
                            onRefresh: () => ref.read(budgetProvider.notifier).load(year: _year, month: _month),
                            child: ListView.separated(
                              itemCount: state.items.length,
                              separatorBuilder: (_, _) => const Divider(height: 1),
                              itemBuilder: (_, i) {
                                final b = state.items[i];
                                return ListTile(
                                  title: Text('${b.accountCode} - ${b.accountName}', style: const TextStyle(fontWeight: FontWeight.w600)),
                                  subtitle: Text('${b.year}/${b.month.toString().padLeft(2, '0')}  \$${b.budgetedAmount.toStringAsFixed(2)}${b.costCenterName != null ? '  | ${b.costCenterName}' : ''}'),
                                  trailing: PopupMenuButton<String>(
                                    onSelected: (v) async {
                                      if (v == 'edit') {
                                        final result = await context.push<bool>('/budgets/${b.id}/edit');
                                        if (!context.mounted) return;
                                        if (result == true) ref.read(budgetProvider.notifier).load(year: _year, month: _month);
                                      }
                                      if (v == 'delete') {
                                        final ok = await ZModal.confirm(context,
                                          title: 'Eliminar presupuesto',
                                          message: '¿Eliminar presupuesto de ${b.accountName}?',
                                          confirmText: 'Eliminar',
                                          cancelText: 'Cancelar',
                                          confirmColor: Colors.red,
                                        );
                                        if (ok == true) {
                                          try {
                                            final dio = ref.read(dioClientProvider);
                                            await dio.delete('budgets/${b.id}');
                                            if (!context.mounted) return;
                                            ref.read(budgetProvider.notifier).load(year: _year, month: _month);
                                          } catch (_) {
                                            if (context.mounted) ZToast.error(context, 'Error al eliminar');
                                          }
                                        }
                                      }
                                    },
                                    itemBuilder: (_) => [
                                      const PopupMenuItem(value: 'edit', child: Text('Editar')),
                                      const PopupMenuItem(value: 'delete', child: Text('Eliminar', style: TextStyle(color: Colors.red))),
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
}
