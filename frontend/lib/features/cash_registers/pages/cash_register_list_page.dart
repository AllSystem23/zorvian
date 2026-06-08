import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../providers/cash_register_provider.dart';

final class CashRegisterListPage extends ConsumerStatefulWidget {
  const CashRegisterListPage({super.key});
  @override
  ConsumerState<CashRegisterListPage> createState() => _CashRegisterListPageState();
}

final class _CashRegisterListPageState extends ConsumerState<CashRegisterListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(cashRegisterProvider.notifier).load());
  }

  List<CashRegisterItem> _filter(List<CashRegisterItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items.where((c) =>
      c.code.toLowerCase().contains(q) ||
      c.status.toLowerCase().contains(q)
    ).toList();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _openRegister() async {
    final codeCtrl = TextEditingController();
    final balanceCtrl = TextEditingController();
    final result = await ZModal.show<bool>(context,
      title: 'Abrir Caja',
      confirmText: 'Abrir',
      cancelText: 'Cancelar',
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ZTextField(controller: codeCtrl, label: 'Código'),
          const SizedBox(height: 12),
          ZTextField(controller: balanceCtrl, label: 'Saldo Inicial', keyboardType: TextInputType.number, prefix: const Text('\$ ')),
        ],
      ),
    );
    if (result != true || codeCtrl.text.isEmpty) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('cash-registers/open', data: {
        'code': codeCtrl.text,
        'openingBalance': double.tryParse(balanceCtrl.text) ?? 0,
        'branchId': '00000000-0000-0000-0000-000000000000',
      });
      await ref.read(cashRegisterProvider.notifier).load();
      if (mounted) ZToast.success(context, 'Caja abierta');
    } catch (e) {
      if (mounted) ZToast.error(context, 'Error: $e');
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(cashRegisterProvider);
    final theme = Theme.of(context);
    final filtered = _filter(state.items);
    return Scaffold(
      appBar: AppBar(title: const Text('Caja')),
      floatingActionButton: FloatingActionButton(
        onPressed: _openRegister,
        child: const Icon(Icons.add),
      ),
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
                          hintText: 'Buscar por código o estado...',
                          prefixIcon: const Icon(Icons.search),
                          suffixIcon: _searchQuery.isNotEmpty
                              ? IconButton(icon: const Icon(Icons.clear), onPressed: () { _searchCtrl.clear(); setState(() => _searchQuery = ''); })
                              : null,
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                          contentPadding: const EdgeInsets.symmetric(vertical: 0),
                        ),
                        onChanged: (v) => setState(() => _searchQuery = v),
                      ),
                    ),
                    Expanded(
                      child: filtered.isEmpty
                          ? Center(child: Text(_searchQuery.isNotEmpty ? 'Sin resultados' : 'No hay registros de caja'))
                          : RefreshIndicator(
                              onRefresh: () => ref.read(cashRegisterProvider.notifier).load(),
                              child: ListView.separated(
                                itemCount: filtered.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final c = filtered[i];
                                  final diffColor = c.difference.abs() > 0.01 ? Colors.red : Colors.green;
                                  return ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor: (c.isOpen ? Colors.green : Colors.grey).withAlpha(30),
                                      child: Icon(c.isOpen ? Icons.lock_open : Icons.lock, color: c.isOpen ? Colors.green : Colors.grey),
                                    ),
                                    title: Text(c.code, style: const TextStyle(fontWeight: FontWeight.w600)),
                                    subtitle: Text('Esperado: \$${c.expectedBalance.toStringAsFixed(0)} · ${c.openedAt.length >= 10 ? c.openedAt.substring(0, 10) : c.openedAt}'),
                                    trailing: Column(
                                      mainAxisAlignment: MainAxisAlignment.center,
                                      crossAxisAlignment: CrossAxisAlignment.end,
                                      children: [
                                        Text('Dif: \$${c.difference.toStringAsFixed(0)}', style: TextStyle(fontSize: 12, color: diffColor, fontWeight: FontWeight.bold)),
                                        Text(c.status, style: TextStyle(fontSize: 11, color: c.isOpen ? Colors.green : Colors.grey)),
                                      ],
                                    ),
                                    onTap: () => context.push('/cash-registers/${c.id}'),
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
