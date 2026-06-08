import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/approval_provider.dart';

final class ApprovalFlowListPage extends ConsumerStatefulWidget {
  const ApprovalFlowListPage({super.key});
  @override
  ConsumerState<ApprovalFlowListPage> createState() => _ApprovalFlowListPageState();
}

final class _ApprovalFlowListPageState extends ConsumerState<ApprovalFlowListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(approvalProvider.notifier).loadFlows());
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(approvalProvider);
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Flujos de Aprobación')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : state.flows.isEmpty
                  ? const Center(child: Text('No hay flujos configurados'))
                  : RefreshIndicator(
                      onRefresh: () => ref.read(approvalProvider.notifier).loadFlows(),
                      child: ListView.separated(
                        itemCount: state.flows.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final f = state.flows[i];
                          return ListTile(
                            title: Text('${f.module} → ${f.eventType}', style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('${f.steps.length} paso(s) — ${f.description.isNotEmpty ? f.description : "Sin descripción"}'),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Chip(label: Text(f.isActive ? 'Activo' : 'Inactivo', style: const TextStyle(fontSize: 11, color: Colors.white)), backgroundColor: f.isActive ? Colors.green : Colors.grey, materialTapTargetSize: MaterialTapTargetSize.shrinkWrap),
                                PopupMenuButton<String>(
                                  onSelected: (v) async {
                                    if (v == 'edit') {
                                      final result = await context.push<bool>('/approval-flows/${f.id}/edit');
                                      if (result == true) ref.read(approvalProvider.notifier).loadFlows();
                                    }
                                    if (v == 'delete') ref.read(approvalProvider.notifier).deleteFlow(f.id);
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
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final result = await context.push<bool>('/approval-flows/new');
          if (result == true) ref.read(approvalProvider.notifier).loadFlows();
        },
        child: const Icon(Icons.add),
      ),
    );
  }
}
