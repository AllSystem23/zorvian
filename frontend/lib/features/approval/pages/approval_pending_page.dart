import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../providers/approval_provider.dart';

final class ApprovalPendingPage extends ConsumerStatefulWidget {
  const ApprovalPendingPage({super.key});
  @override
  ConsumerState<ApprovalPendingPage> createState() => _ApprovalPendingPageState();
}

final class _ApprovalPendingPageState extends ConsumerState<ApprovalPendingPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(approvalProvider.notifier).loadPending());
  }

  Future<void> _act(String id, String action) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('approval-requests/$id/$action', data: {'comment': ''});
      ref.read(approvalProvider.notifier).loadPending();
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error al $action')));
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(approvalProvider);
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Aprobaciones Pendientes')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : state.pendingRequests.isEmpty
                  ? const Center(child: Text('No hay solicitudes pendientes'))
                  : RefreshIndicator(
                      onRefresh: () => ref.read(approvalProvider.notifier).loadPending(),
                      child: ListView.separated(
                        itemCount: state.pendingRequests.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final r = state.pendingRequests[i];
                          return ListTile(
                            title: Text('${r.module} — ${r.eventType}', style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('Paso ${r.currentStep}/${r.totalSteps} — Solicitado por ${r.requestedBy}'),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                IconButton(icon: const Icon(Icons.check_circle, color: Colors.green), onPressed: () => _act(r.id, 'approve')),
                                IconButton(icon: const Icon(Icons.cancel, color: Colors.red), onPressed: () => _act(r.id, 'reject')),
                              ],
                            ),
                          );
                        },
                      ),
                    ),
    );
  }
}
