import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/accounting_provider.dart';

final class AccountingEntriesPage extends ConsumerStatefulWidget {
  const AccountingEntriesPage({super.key});
  @override
  ConsumerState<AccountingEntriesPage> createState() => _AccountingEntriesPageState();
}

final class _AccountingEntriesPageState extends ConsumerState<AccountingEntriesPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(accountingProvider.notifier).loadEntries());
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(accountingProvider);
    final filtered = _searchQuery.isEmpty
        ? state.entries
        : state.entries.where((e) =>
            e.entryNumber.toLowerCase().contains(_searchQuery.toLowerCase()) ||
            e.description.toLowerCase().contains(_searchQuery.toLowerCase()) ||
            e.referenceType.toLowerCase().contains(_searchQuery.toLowerCase())
          ).toList();

    return Scaffold(
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
            child: TextField(
              controller: _searchCtrl,
              decoration: InputDecoration(
                hintText: 'Buscar por número, descripción o tipo...',
                prefixIcon: const Icon(Icons.search),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                contentPadding: const EdgeInsets.symmetric(vertical: 0),
              ),
              onChanged: (v) => setState(() => _searchQuery = v),
            ),
          ),
          Expanded(
            child: filtered.isEmpty
                ? Center(child: Text(_searchQuery.isNotEmpty ? 'Sin resultados' : 'No hay asientos'))
                : RefreshIndicator(
                    onRefresh: () => ref.read(accountingProvider.notifier).loadEntries(),
                    child: ListView.separated(
                      itemCount: filtered.length,
                      separatorBuilder: (_, _) => const Divider(height: 1),
                      itemBuilder: (_, i) {
                        final e = filtered[i];
                        final isPosted = e.status == 'posted';
                        return ListTile(
                          leading: CircleAvatar(
                            backgroundColor: isPosted ? Colors.green.withAlpha(30) : Colors.orange.withAlpha(30),
                            child: Icon(isPosted ? Icons.check_circle : Icons.drafts, size: 20, color: isPosted ? Colors.green : Colors.orange),
                          ),
                          title: Text('${e.entryNumber} - ${e.description}', style: const TextStyle(fontWeight: FontWeight.w600)),
                          subtitle: Text('${e.entryDate.substring(0, 10)} · ${e.referenceType} · Débito: \$${e.totalDebit.toStringAsFixed(2)} / Crédito: \$${e.totalCredit.toStringAsFixed(2)}'),
                          trailing: Text(e.periodName ?? '', style: TextStyle(fontSize: 11, color: Colors.grey[600])),
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
