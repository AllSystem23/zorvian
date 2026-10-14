import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/utils/formatters.dart';
import '../providers/vacation_receipt_provider.dart';

class VacationReceiptsListPage extends ConsumerStatefulWidget {
  const VacationReceiptsListPage({super.key});

  @override
  ConsumerState<VacationReceiptsListPage> createState() => _VacationReceiptsListPageState();
}

class _VacationReceiptsListPageState extends ConsumerState<VacationReceiptsListPage> {
  int? _year;

  @override
  void initState() {
    super.initState();
    _year = DateTime.now().year;
    Future.microtask(() => _load());
  }

  void _load({int? page}) {
    ref.read(vacationReceiptProvider.notifier).load(
      year: _year,
      page: page ?? 1,
    );
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(vacationReceiptProvider);
    final theme = Theme.of(context);
    final currentYear = DateTime.now().year;

    return Scaffold(
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
            child: Row(
              children: [
                Text('Año', style: theme.textTheme.bodySmall),
                const SizedBox(width: 8),
                DropdownButton<int?>(
                  value: _year,
                  items: [
                    for (var y = currentYear; y >= currentYear - 4; y--)
                      DropdownMenuItem(value: y, child: Text('$y')),
                  ],
                  onChanged: (v) {
                    setState(() => _year = v);
                    _load();
                  },
                ),
              ],
            ),
          ),
          Expanded(
            child: state.loading
                ? const Center(child: CircularProgressIndicator())
                : state.error != null
                    ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
                    : state.items.isEmpty
                        ? const Center(child: Text('No hay recibos de vacaciones'))
                        : RefreshIndicator(
                            onRefresh: () async => _load(page: state.page),
                            child: ListView.separated(
                              itemCount: state.items.length + 1,
                              separatorBuilder: (_, _) => const Divider(height: 1),
                              itemBuilder: (_, i) {
                                if (i == state.items.length) return _paginationBar(state);
                                final r = state.items[i];
                                return ListTile(
                                  title: Text(r.employeeName, style: const TextStyle(fontWeight: FontWeight.w600)),
                                  subtitle: Text('${r.receiptNumber} · ${r.startDate} → ${r.endDate} · ${r.businessDays.toStringAsFixed(0)} días'),
                                  trailing: Text(
                                    ZFormatters.currency(r.netPay, currencyCode: r.currency),
                                    style: TextStyle(
                                      fontWeight: FontWeight.bold,
                                      color: Colors.green.shade700,
                                    ),
                                  ),
                                  onTap: () => context.push('/vacations/receipts/${r.id}'),
                                );
                              },
                            ),
                          ),
          ),
        ],
      ),
    );
  }

  Widget _paginationBar(VacationReceiptListState state) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 16),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          IconButton(
            icon: const Icon(Icons.chevron_left),
            onPressed: state.hasPrevious ? () => _load(page: state.page - 1) : null,
          ),
          Text('${state.page} / ${state.totalPages}'),
          IconButton(
            icon: const Icon(Icons.chevron_right),
            onPressed: state.hasNext ? () => _load(page: state.page + 1) : null,
          ),
        ],
      ),
    );
  }
}
