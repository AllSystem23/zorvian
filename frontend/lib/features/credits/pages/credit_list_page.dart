import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/components/z_async_renderer.dart';
import '../providers/credit_provider.dart';

final class CreditListPage extends ConsumerStatefulWidget {
  const CreditListPage({super.key});
  @override
  ConsumerState<CreditListPage> createState() => _CreditListPageState();
}

final class _CreditListPageState extends ConsumerState<CreditListPage> {
  final _searchCtrl = TextEditingController();

  void _onSearch(String v) {
    ref.read(creditProvider.notifier).load(search: v.isNotEmpty ? v : null);
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: ZAsyncRenderer<List<CreditItem>>(
        value: ref.watch(creditProvider),
        builder: (items) => Column(
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
                  ? Center(child: Text(_searchCtrl.text.isNotEmpty ? 'Sin resultados' : 'No hay créditos'))
                  : RefreshIndicator(
                      onRefresh: () => ref.read(creditProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final c = items[i];
                          final stColor = switch (c.status) {
                            'active' => Colors.green,
                            'completed' => Colors.blue,
                            'defaulted' => Colors.red,
                            _ => Colors.orange,
                          };
                          final progress = c.financedAmount > 0 ? (c.paidAmount / (c.financedAmount + c.balance)).clamp(0.0, 1.0) : 0.0;
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: stColor.withAlpha(30),
                              child: Text(c.creditNumber.length > 4 ? c.creditNumber.substring(c.creditNumber.length - 4) : c.creditNumber,
                                style: TextStyle(fontSize: 10, color: stColor, fontWeight: FontWeight.bold)),
                            ),
                            title: Row(children: [
                              Expanded(child: Text(c.clientName, style: const TextStyle(fontWeight: FontWeight.w600))),
                              if (c.status == 'defaulted')
                                const Chip(label: Text('MORA', style: TextStyle(fontSize: 9, color: Colors.white)), backgroundColor: Colors.red, materialTapTargetSize: MaterialTapTargetSize.shrinkWrap, padding: EdgeInsets.zero, visualDensity: VisualDensity.compact),
                            ]),
                            subtitle: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('Total: \$${(c.financedAmount + c.balance).toStringAsFixed(0)} · Saldo: \$${c.balance.toStringAsFixed(0)}'),
                                const SizedBox(height: 4),
                                LinearProgressIndicator(value: progress, backgroundColor: Colors.grey.withAlpha(30), color: stColor),
                              ],
                            ),
                            trailing: Chip(label: Text(c.status, style: TextStyle(fontSize: 11, color: stColor)), materialTapTargetSize: MaterialTapTargetSize.shrinkWrap),
                            onTap: () => context.push('/credits/${c.id}'),
                          );
                        },
                      ),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}
