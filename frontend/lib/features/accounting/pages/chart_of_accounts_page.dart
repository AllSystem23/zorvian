import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/accounting_provider.dart';

final class ChartOfAccountsPage extends ConsumerStatefulWidget {
  const ChartOfAccountsPage({super.key});
  @override
  ConsumerState<ChartOfAccountsPage> createState() => _ChartOfAccountsPageState();
}

final class _ChartOfAccountsPageState extends ConsumerState<ChartOfAccountsPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(accountingProvider.notifier).loadAccounts());
  }

  Widget _buildAccountNode(AccountItem a, int depth) {
    final hasChildren = a.children.isNotEmpty;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: EdgeInsets.only(left: depth * 24.0),
          child: ListTile(
            dense: true,
            leading: Icon(
              _typeIcon(a.type),
              size: 18,
              color: _typeColor(a.type),
            ),
            title: Text('${a.code} - ${a.name}', style: TextStyle(fontWeight: depth <= 1 ? FontWeight.bold : FontWeight.normal, fontSize: depth <= 1 ? 14 : 13)),
            subtitle: Text(a.type, style: TextStyle(fontSize: 11, color: _typeColor(a.type))),
            trailing: Text('\$${a.currentBalance.toStringAsFixed(2)}', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
          ),
        ),
        if (hasChildren)
          ...a.children.map((c) => _buildAccountNode(c, depth + 1)),
      ],
    );
  }

  Color _typeColor(String type) => switch (type) {
    'Asset' => Colors.blue,
    'Liability' => Colors.orange,
    'Equity' => Colors.green,
    'Income' => Colors.teal,
    'Cost' => Colors.red,
    'Expense' => Colors.purple,
    _ => Colors.grey,
  };

  IconData _typeIcon(String type) => switch (type) {
    'Asset' => Icons.account_balance,
    'Liability' => Icons.credit_card,
    'Equity' => Icons.savings,
    'Income' => Icons.trending_up,
    'Cost' => Icons.shopping_cart,
    'Expense' => Icons.money_off,
    _ => Icons.book,
  };

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(accountingProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Catálogo de Cuentas'),
        actions: [
          IconButton(
            icon: const Icon(Icons.auto_fix_high),
            tooltip: 'Sembrar catálogo por defecto',
            onPressed: () => ref.read(accountingProvider.notifier).seedAccounts(),
          ),
        ],
      ),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : state.accounts.isEmpty
                  ? const Center(child: Text('No hay cuentas. Presione el botón de sembrar para crear el catálogo por defecto.'))
                  : RefreshIndicator(
                      onRefresh: () => ref.read(accountingProvider.notifier).loadAccounts(),
                      child: ListView(children: state.accounts.map((a) => _buildAccountNode(a, 0)).toList()),
                    ),
    );
  }
}
