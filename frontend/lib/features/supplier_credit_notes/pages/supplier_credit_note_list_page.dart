import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/supplier_credit_note_provider.dart';

final class SupplierCreditNoteListPage extends ConsumerStatefulWidget {
  final String? purchaseId;
  const SupplierCreditNoteListPage({super.key, this.purchaseId});

  @override
  ConsumerState<SupplierCreditNoteListPage> createState() => _SupplierCreditNoteListPageState();
}

final class _SupplierCreditNoteListPageState extends ConsumerState<SupplierCreditNoteListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(supplierCreditNoteProvider.notifier).load(purchaseId: widget.purchaseId));
  }

  Color _statusColor(String status) {
    return switch (status) {
      'completed' => Colors.green,
      'cancelled' => Colors.red,
      _ => Colors.orange,
    };
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(supplierCreditNoteProvider);
    final theme = Theme.of(context);

    return Scaffold(
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : state.items.isEmpty
                  ? const Center(child: Text('No hay notas de crédito registradas'))
                  : RefreshIndicator(
                      onRefresh: () => ref.read(supplierCreditNoteProvider.notifier).load(purchaseId: widget.purchaseId),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final n = state.items[i];
                          final statusColor = _statusColor(n.status);
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: theme.colorScheme.errorContainer,
                              child: Icon(Icons.request_quote_outlined, color: theme.colorScheme.error),
                            ),
                            title: Text(n.creditNoteNumber, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text(
                              '${n.supplierName ?? "Proveedor"} · ${n.creditNoteDate.substring(0, 10)}',
                            ),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Text(
                                  '\$${n.total.toStringAsFixed(2)}',
                                  style: TextStyle(
                                    fontWeight: FontWeight.bold,
                                    color: theme.colorScheme.error,
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Chip(
                                  label: Text(n.status, style: const TextStyle(fontSize: 11, color: Colors.white)),
                                  backgroundColor: statusColor,
                                  padding: EdgeInsets.zero,
                                  visualDensity: VisualDensity.compact,
                                ),
                              ],
                            ),
                            onTap: () {
                              if (n.purchaseId != null) {
                                context.push('/purchases/${n.purchaseId}');
                              }
                            },
                          );
                        },
                      ),
                    ),
      floatingActionButton: FloatingActionButton.extended(
        heroTag: 'add-supplier-credit-note',
        onPressed: () => context.push('/purchases/credit-notes/new', extra: {'purchaseId': widget.purchaseId}),
        icon: const Icon(Icons.add),
        label: const Text('Nueva Nota de Crédito'),
      ),
    );
  }
}
