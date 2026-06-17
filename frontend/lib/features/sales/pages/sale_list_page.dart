import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/components/z_async_renderer.dart';
import '../../../shared/ds/components/z_empty_state.dart';
import '../providers/sale_provider.dart';

final class SaleListPage extends ConsumerStatefulWidget {
  const SaleListPage({super.key});
  @override
  ConsumerState<SaleListPage> createState() => _SaleListPageState();
}

final class _SaleListPageState extends ConsumerState<SaleListPage> {
  final _searchCtrl = TextEditingController();

  void _onSearch(String v) {
    ref.read(saleProvider.notifier).load(search: v.isNotEmpty ? v : null);
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Ventas')),
      body: ZAsyncRenderer<List<SaleItem>>(
        value: ref.watch(saleProvider),
        builder: (items) => Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
              child: TextField(
                controller: _searchCtrl,
                decoration: InputDecoration(
                  hintText: 'Buscar por cliente o factura...',
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
                  ? _searchCtrl.text.isNotEmpty
                      ? const ZEmptyState.search()
                      : ZEmptyState.list(
                          itemType: 'ventas',
                          actionLabel: 'Nueva Venta',
                          onAction: () => context.push('/sales/new'),
                        )
                  : RefreshIndicator(
                      onRefresh: () => ref.read(saleProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final s = items[i];
                          final statusColor = switch (s.status) {
                            'completed' => Colors.green,
                            'cancelled' => Colors.red,
                            _ => Colors.orange,
                          };
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: statusColor.withAlpha(30),
                              child: Text(s.invoiceNumber.length > 4 ? s.invoiceNumber.substring(s.invoiceNumber.length - 4) : s.invoiceNumber,
                                style: TextStyle(fontSize: 10, color: statusColor, fontWeight: FontWeight.bold)),
                            ),
                            title: Text(s.clientName, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('\$${s.total.toStringAsFixed(0)} · ${s.saleType} · ${s.saleDate.length >= 10 ? s.saleDate.substring(0, 10) : s.saleDate}'),
                            trailing: Chip(label: Text(s.status, style: TextStyle(fontSize: 11, color: statusColor)), materialTapTargetSize: MaterialTapTargetSize.shrinkWrap),
                            onTap: () => context.push('/sales/${s.id}'),
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
