import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/utils/currency_colors.dart';
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
                          return _ExchangeRateTile(
                            item: item,
                            onEdit: () => context.push(
                                '/exchange-rates/${item.id}/edit'),
                            onDelete: () => _confirmDelete(
                                item.id,
                                '${item.fromCurrency}→${item.toCurrency}'),
                          );
                        },
                      ),
                    ),
    );
  }
}

/// A polished list tile for exchange rates with a premium currency badge.
class _ExchangeRateTile extends StatelessWidget {
  final ExchangeRateItem item;
  final VoidCallback onEdit;
  final VoidCallback onDelete;

  const _ExchangeRateTile({
    required this.item,
    required this.onEdit,
    required this.onDelete,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
      leading: _CurrencyBadge(
        fromCode: item.fromCurrency,
        toCode: item.toCurrency,
        isDark: isDark,
      ),
      title: Text(
        '${item.fromCurrency} → ${item.toCurrency}',
        style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
      ),
      subtitle: Row(
        children: [
          Text(
            item.displayRate,
            style: TextStyle(
              fontWeight: FontWeight.w700,
              fontSize: 13,
              color: isDark ? ZColors.brandAccent : ZColors.brandPrimary,
            ),
          ),
          const SizedBox(width: 8),
          Icon(Icons.calendar_today, size: 11,
              color: isDark ? ZColors.neutral500 : ZColors.neutral400),
          const SizedBox(width: 3),
          Text(
            item.formattedDate,
            style: TextStyle(
              fontSize: 11,
              color: isDark ? ZColors.neutral500 : ZColors.neutral400,
            ),
          ),
        ],
      ),
      trailing: PopupMenuButton<String>(
        onSelected: (v) {
          if (v == 'edit') onEdit();
          if (v == 'delete') onDelete();
        },
        itemBuilder: (_) => [
          const PopupMenuItem(
              value: 'edit',
              child: Row(
                children: [
                  Icon(Icons.edit_outlined, size: 18),
                  SizedBox(width: 8),
                  Text('Editar'),
                ],
              )),
          const PopupMenuItem(
            value: 'delete',
            child: Row(
              children: [
                Icon(Icons.delete_outline, size: 18, color: Colors.red),
                SizedBox(width: 8),
                Text('Eliminar', style: TextStyle(color: Colors.red)),
              ],
            ),
          ),
        ],
      ),
      onTap: onEdit,
    );
  }
}

/// Premium currency badge with gradient, icon, and dual-currency label.
class _CurrencyBadge extends StatelessWidget {
  final String fromCode;
  final String toCode;
  final bool isDark;

  const _CurrencyBadge({
    required this.fromCode,
    required this.toCode,
    required this.isDark,
  });

  @override
  Widget build(BuildContext context) {
    final bgColor = CurrencyColors.background(fromCode, isDark: isDark);
    final fgColor = CurrencyColors.foreground(fromCode, isDark: isDark);
    final gradient = CurrencyColors.gradient(fromCode, isDark: isDark);

    return Container(
      width: 48,
      height: 48,
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: gradient,
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(
          color: fgColor.withValues(alpha: 0.3),
          width: 1.2,
        ),
        boxShadow: [
          BoxShadow(
            color: bgColor.withValues(alpha: 0.2),
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Text(
            fromCode,
            style: TextStyle(
              fontSize: 10,
              fontWeight: FontWeight.w800,
              color: fgColor,
              height: 1.1,
            ),
          ),
          Container(
            margin: const EdgeInsets.symmetric(vertical: 1),
            width: 14,
            height: 1,
            color: fgColor.withValues(alpha: 0.4),
          ),
          Text(
            toCode,
            style: TextStyle(
              fontSize: 10,
              fontWeight: FontWeight.w600,
              color: fgColor.withValues(alpha: 0.7),
              height: 1.1,
            ),
          ),
        ],
      ),
    );
  }
}
