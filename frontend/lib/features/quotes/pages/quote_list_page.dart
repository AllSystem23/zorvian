import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/quote_provider.dart';

final class QuoteListPage extends ConsumerStatefulWidget {
  const QuoteListPage({super.key});
  @override
  ConsumerState<QuoteListPage> createState() => _QuoteListPageState();
}

final class _QuoteListPageState extends ConsumerState<QuoteListPage> {
  final _searchCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(quoteProvider.notifier).load());
  }

  void _onSearch(String v) {
    ref.read(quoteProvider.notifier).load(search: v.isNotEmpty ? v : null);
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(quoteProvider);
    final theme = Theme.of(context);
    final items = state.items;
    return Scaffold(
      appBar: AppBar(title: const Text('Cotizaciones')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : Column(
                  children: [
                    Padding(
                      padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
                      child: TextField(
                        controller: _searchCtrl,
                        decoration: InputDecoration(
                          hintText: 'Buscar por cliente o folio...',
                          prefixIcon: const Icon(Icons.search),
                          suffixIcon: _searchCtrl.text.isNotEmpty
                              ? IconButton(icon: const Icon(Icons.clear), onPressed: () { _searchCtrl.clear(); _onSearch(''); })
                              : null,
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                          contentPadding: const EdgeInsets.symmetric(vertical: 0),
                        ),
                        onChanged: _onSearch,
                      ),
                    ),
                    Expanded(
                      child: items.isEmpty
                          ? Center(child: Text(_searchCtrl.text.isNotEmpty ? 'Sin resultados' : 'No hay cotizaciones'))
                          : RefreshIndicator(
                              onRefresh: () => ref.read(quoteProvider.notifier).load(),
                              child: ListView.separated(
                                itemCount: items.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final q = items[i];
                                  final stColor = switch (q.status) {
                                    'approved' => Colors.green,
                                    'rejected' => Colors.red,
                                    'converted' => Colors.blue,
                                    _ => Colors.orange,
                                  };
                                  return ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor: stColor.withAlpha(30),
                                      child: Text(q.quoteNumber, style: TextStyle(fontSize: 10, color: stColor, fontWeight: FontWeight.bold)),
                                    ),
                                    title: Text(q.clientName, style: const TextStyle(fontWeight: FontWeight.w600)),
                                    subtitle: Text('\$${q.total.toStringAsFixed(0)} · ${q.quoteDate.substring(0, 10)}'),
                                    trailing: Chip(
                                      label: Text(q.status, style: TextStyle(fontSize: 11, color: stColor)),
                                      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                    ),
                                    onTap: () async {
                                      final result = await context.push<bool>('/quotes/${q.id}');
                                      if (result == true) ref.read(quoteProvider.notifier).load();
                                    },
                                  );
                                },
                              ),
                            ),
                    ),
                  ],
                ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final result = await context.push<bool>('/quotes/new');
          if (result == true) ref.read(quoteProvider.notifier).load();
        },
        child: const Icon(Icons.add),
      ),
    );
  }
}
