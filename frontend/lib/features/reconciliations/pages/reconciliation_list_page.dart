import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/reconciliation_provider.dart';

final class ReconciliationListPage extends ConsumerStatefulWidget {
  const ReconciliationListPage({super.key});
  @override
  ConsumerState<ReconciliationListPage> createState() => _ReconciliationListPageState();
}

final class _ReconciliationListPageState extends ConsumerState<ReconciliationListPage> {
  String? _statusFilter;

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(reconciliationProvider.notifier).load());
  }

  Color _statusColor(String status) => switch (status) {
    'completed' => Colors.green,
    'in_progress' => Colors.orange,
    'draft' => Colors.grey,
    'cancelled' => Colors.red,
    _ => Colors.grey,
  };

  String _statusLabel(String status) => switch (status) {
    'completed' => 'Completada',
    'in_progress' => 'En Progreso',
    'draft' => 'Borrador',
    'cancelled' => 'Cancelada',
    _ => status,
  };

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(reconciliationProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Conciliaciones Bancarias'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: 'Nueva Conciliación',
            onPressed: () async {
              final result = await context.push<bool>('/reconciliations/new');
              if (result == true && mounted) ref.read(reconciliationProvider.notifier).load();
            },
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
            child: Row(
              children: [
                Expanded(
                  child: DropdownButtonFormField<String?>(
                    decoration: const InputDecoration(
                      labelText: 'Estado',
                      prefixIcon: Icon(Icons.filter_list),
                      isDense: true,
                    ),
                    initialValue: _statusFilter,
                    items: [
                      const DropdownMenuItem(value: null, child: Text('Todos')),
                      DropdownMenuItem(value: 'draft', child: Text('Borrador')),
                      DropdownMenuItem(value: 'in_progress', child: Text('En Progreso')),
                      DropdownMenuItem(value: 'completed', child: Text('Completada')),
                      DropdownMenuItem(value: 'cancelled', child: Text('Cancelada')),
                    ],
                    onChanged: (v) {
                      setState(() => _statusFilter = v);
                      ref.read(reconciliationProvider.notifier).load(status: v);
                    },
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: state.loading
                ? const Center(child: CircularProgressIndicator())
                : state.error != null
                    ? Center(
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.error_outline, size: 48, color: theme.colorScheme.error),
                            const SizedBox(height: 16),
                            Text(state.error!, style: TextStyle(color: theme.colorScheme.error)),
                          ],
                        ),
                      )
                    : state.items.isEmpty
                        ? const Center(child: Text('No hay conciliaciones registradas'))
                        : RefreshIndicator(
                            onRefresh: () => ref.read(reconciliationProvider.notifier).load(status: _statusFilter),
                            child: ListView.separated(
                              itemCount: state.items.length,
                              separatorBuilder: (_, _) => const Divider(height: 1),
                              itemBuilder: (_, i) {
                                final r = state.items[i];
                                return ListTile(
                                  leading: Icon(
                                    r.status == 'completed' ? Icons.check_circle : Icons.compare_arrows,
                                    color: _statusColor(r.status),
                                  ),
                                  title: Text(r.bankAccountName, style: const TextStyle(fontWeight: FontWeight.w600)),
                                  subtitle: Text(
                                    '${r.dateFrom} - ${r.dateTo}  |  ${r.matchedCount}/${r.totalTransactions} conciliados',
                                  ),
                                  trailing: Row(
                                    mainAxisSize: MainAxisSize.min,
                                    children: [
                                      Container(
                                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                        decoration: BoxDecoration(
                                          color: _statusColor(r.status).withValues(alpha: 0.15),
                                          borderRadius: BorderRadius.circular(12),
                                        ),
                                        child: Text(
                                          _statusLabel(r.status),
                                          style: TextStyle(
                                            color: _statusColor(r.status),
                                            fontSize: 12,
                                            fontWeight: FontWeight.w600,
                                          ),
                                        ),
                                      ),
                                      if (r.difference != 0)
                                        Padding(
                                          padding: const EdgeInsets.only(left: 4),
                                          child: Icon(Icons.warning_amber_rounded, color: Colors.orange, size: 18),
                                        ),
                                    ],
                                  ),
                                  onTap: () {
                                    // Navigate to detail page
                                  },
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
