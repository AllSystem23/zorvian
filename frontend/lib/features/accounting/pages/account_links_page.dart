import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/accounting_provider.dart';

final class AccountLinksPage extends ConsumerStatefulWidget {
  const AccountLinksPage({super.key});
  @override
  ConsumerState<AccountLinksPage> createState() => _AccountLinksPageState();
}

final class _AccountLinksPageState extends ConsumerState<AccountLinksPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(accountingProvider.notifier).loadLinks());
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(accountingProvider);
    final theme = Theme.of(context);

    final grouped = <String, List<AccountLinkItem>>{};
    for (final link in state.links) {
      grouped.putIfAbsent(link.transactionType, () => []);
      grouped[link.transactionType]!.add(link);
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('Vinculación Contable'),
        actions: [
          IconButton(
            icon: const Icon(Icons.auto_fix_high),
            tooltip: 'Vínculos por defecto',
            onPressed: () => ref.read(accountingProvider.notifier).seedLinks(),
          ),
        ],
      ),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.links.isEmpty
              ? const Center(child: Text('Sin vínculos. Cree el catálogo de cuentas y siembre los vínculos.'))
              : RefreshIndicator(
                  onRefresh: () => ref.read(accountingProvider.notifier).loadLinks(),
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: grouped.entries.map((entry) => Card(
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(_txTypeName(entry.key), style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                            const Divider(),
                            ...entry.value.map((l) => Padding(
                              padding: const EdgeInsets.symmetric(vertical: 4),
                              child: Row(
                                children: [
                                  SizedBox(width: 100, child: Text(_roleName(l.role), style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 12))),
                                  Expanded(child: Text('${l.accountCode} - ${l.accountName}', style: const TextStyle(fontSize: 12))),
                                ],
                              ),
                            )),
                          ],
                        ),
                      ),
                    )).toList(),
                  ),
                ),
    );
  }

  String _txTypeName(String t) => switch (t) {
    'Sale' => 'Ventas',
    'Purchase' => 'Compras',
    'InventoryMovement' => 'Inventario',
    'CreditPayment' => 'Cobros',
    'CashMovement' => 'Caja',
    _ => t,
  };

  String _roleName(String r) => switch (r) {
    'Inventory' => 'Inventario',
    'AccountsReceivable' => 'CxC',
    'AccountsPayable' => 'CxP',
    'SalesRevenue' => 'Ventas',
    'CostOfSales' => 'Costo Venta',
    'VatPayable' => 'IVA Débito',
    'VatReceivable' => 'IVA Crédito',
    'Cash' => 'Caja',
    'InventoryAdjustment' => 'Ajuste Inv.',
    _ => r,
  };
}
