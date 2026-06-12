import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/shared/ds/ds.dart';
import '../providers/warranty_provider.dart';

final class WarrantyListPage extends ConsumerStatefulWidget {
  const WarrantyListPage({super.key});
  @override
  ConsumerState<WarrantyListPage> createState() => _WarrantyListPageState();
}

final class _WarrantyListPageState extends ConsumerState<WarrantyListPage> {
  final _searchCtrl = TextEditingController();
  int _currentPage = 1;

  @override
  void initState() {
    super.initState();
    Future.microtask(() => _load());
  }

  void _load() => ref.read(warrantyProvider.notifier).load(page: _currentPage);

  Color _statusColor(String status) => switch (status) {
    'Registered' => ZColors.brandPrimary,
    'PendingReview' => ZColors.warning,
    'InDiagnosis' => ZColors.brandSecondary,
    'SentToWorkshop' => ZColors.brandSecondary,
    'Repaired' => ZColors.success,
    'ReplacementApproved' => ZColors.success,
    'Delivered' => ZColors.success,
    'Closed' => ZColors.neutral900,
    'Cancelled' => ZColors.danger,
    _ => ZColors.brandSecondary,
  };

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(warrantyProvider);
    final theme = Theme.of(context);
    
    return Scaffold(
      appBar: AppBar(title: const Text('Garantías')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : Column(
                  children: [
                    Expanded(
                      child: ZDataTable<WarrantyItem>(
                        columns: const [
                          ZColumn(id: 'product', label: 'Producto'),
                          ZColumn(id: 'client', label: 'Cliente'),
                          ZColumn(id: 'status', label: 'Estado'),
                        ],
                        rows: state.items,
                        rowMapper: (item) => DataRow(cells: [
                          DataCell(Text(item.productName)),
                          DataCell(Text(item.clientName)),
                          DataCell(Chip(
                              label: Text(item.status), 
                              backgroundColor: _statusColor(item.status).withAlpha(30)
                          )),
                        ]),
                      ),
                    ),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        IconButton(icon: const Icon(Icons.chevron_left), onPressed: _currentPage > 1 ? () { _currentPage--; _load(); } : null),
                        Text('Página $_currentPage'),
                        IconButton(icon: const Icon(Icons.chevron_right), onPressed: state.items.length >= 20 ? () { _currentPage++; _load(); } : null),
                      ],
                    )
                  ],
                ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final result = await context.push<bool>('/warranties/new');
          if (result == true) _load();
        },
        child: const Icon(Icons.add),
      ),
    );
  }
}
