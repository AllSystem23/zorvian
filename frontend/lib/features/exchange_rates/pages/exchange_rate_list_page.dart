import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/exchange_rate_provider.dart';

class ExchangeRateListPage extends ConsumerStatefulWidget {
  const ExchangeRateListPage({super.key});

  @override
  ConsumerState<ExchangeRateListPage> createState() =>
      _ExchangeRateListPageState();
}

class _ExchangeRateListPageState extends ConsumerState<ExchangeRateListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(exchangeRateProvider.notifier).load());
  }

  Future<void> _confirmDelete(String id, String label) async {
    final ok = await ZModal.confirm(
      context,
      title: 'Eliminar tipo de cambio',
      message: '¿Eliminar tipo de cambio $label?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
      confirmColor: Colors.red,
    );
    if (!ok) return;

    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('exchange-rates/$id');
      ref.read(exchangeRateProvider.notifier).load();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error al eliminar tipo de cambio')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(exchangeRateProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: const Text('Tipos de cambio')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(
                  child: Text(state.error!,
                      style: TextStyle(color: theme.colorScheme.error)))
              : state.items.isEmpty
                  ? const Center(child: Text('No hay tipos de cambio'))
                  : RefreshIndicator(
                      onRefresh: () =>
                          ref.read(exchangeRateProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final item = state.items[i];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor:
                                  theme.colorScheme.primaryContainer,
                              child: Text(
                                '${item.fromCurrency}/${item.toCurrency}',
                                style: TextStyle(
                                  fontSize: 10,
                                  fontWeight: FontWeight.bold,
                                  color:
                                      theme.colorScheme.onPrimaryContainer,
                                ),
                              ),
                            ),
                            title: Text(
                              '${item.fromCurrency} → ${item.toCurrency}',
                              style:
                                  const TextStyle(fontWeight: FontWeight.w600),
                            ),
                            subtitle: Text(
                                'Tasa: ${item.displayRate} · Vigente: ${item.formattedDate}'),
                            trailing: PopupMenuButton<String>(
                              onSelected: (v) {
                                if (v == 'edit') {
                                  context.push(
                                      '/exchange-rates/${item.id}/edit');
                                }
                                if (v == 'delete') {
                                  _confirmDelete(
                                      item.id,
                                      '${item.fromCurrency}→${item.toCurrency}');
                                }
                              },
                              itemBuilder: (_) => [
                                const PopupMenuItem(
                                    value: 'edit', child: Text('Editar')),
                                const PopupMenuItem(
                                  value: 'delete',
                                  child: Text('Eliminar',
                                      style: TextStyle(color: Colors.red)),
                                ),
                              ],
                            ),
                          );
                        },
                      ),
                    ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final result =
              await context.push<bool>('/exchange-rates/new');
          if (result == true) {
            ref.read(exchangeRateProvider.notifier).load();
          }
        },
        child: const Icon(Icons.add),
      ),
    );
  }
}
