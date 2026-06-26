import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/credit_note_provider.dart';

final class CreditNoteListPage extends ConsumerStatefulWidget {
  const CreditNoteListPage({super.key});
  @override
  ConsumerState<CreditNoteListPage> createState() => _CreditNoteListPageState();
}

final class _CreditNoteListPageState extends ConsumerState<CreditNoteListPage> {
  @override
  void initState() {
    super.initState();
    _ensureCompanyAndLoad();
  }

  void _ensureCompanyAndLoad() {
    Future.microtask(() => ref.read(creditNoteProvider.notifier).load());
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(creditNoteProvider);
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Notas de Crédito')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : state.items.isEmpty
                  ? const Center(child: Text('No hay notas de crédito'))
                  : RefreshIndicator(
                      onRefresh: () => ref.read(creditNoteProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final cn = state.items[i];
                          return ListTile(
                            title: Text(cn.creditNoteNumber, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('Factura ${cn.invoiceNumber}  |  \$${cn.total.toStringAsFixed(2)}'),
                            trailing: Column(
                              crossAxisAlignment: CrossAxisAlignment.end,
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Chip(label: Text(cn.status, style: const TextStyle(fontSize: 11, color: Colors.white)), backgroundColor: Colors.orange, materialTapTargetSize: MaterialTapTargetSize.shrinkWrap),
                                Text(cn.issueDate.length >= 10 ? cn.issueDate.substring(0, 10) : cn.issueDate, style: const TextStyle(fontSize: 11, color: Colors.grey)),
                              ],
                            ),
                          );
                        },
                      ),
                    ),
    );
  }
}
