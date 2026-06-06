import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/warranty_provider.dart';

final class WarrantyListPage extends ConsumerStatefulWidget {
  const WarrantyListPage({super.key});
  @override
  ConsumerState<WarrantyListPage> createState() => _WarrantyListPageState();
}

final class _WarrantyListPageState extends ConsumerState<WarrantyListPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(warrantyProvider.notifier).load());
  }

  List<WarrantyItem> _filter(List<WarrantyItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items.where((w) =>
      w.clientName.toLowerCase().contains(q) ||
      w.productName.toLowerCase().contains(q) ||
      w.warrantyNumber.toLowerCase().contains(q) ||
      w.status.toLowerCase().contains(q)
    ).toList();
  }

  Color _statusColor(String status) => switch (status) {
    'Registered' => Colors.blue,
    'PendingReview' => Colors.orange,
    'InDiagnosis' => Colors.purple,
    'SentToWorkshop' => Colors.indigo,
    'InRepair' => Colors.deepOrange,
    'PendingParts' => Colors.amber,
    'Repaired' => Colors.teal,
    'ReplacementApproved' => Colors.cyan,
    'ReadyForDelivery' => Colors.green,
    'Delivered' => Colors.green,
    'Closed' => Colors.grey,
    'Cancelled' => Colors.red,
    _ => Colors.grey,
  };

  String _statusLabel(String status) => switch (status) {
    'Registered' => 'Registrada',
    'PendingReview' => 'Pendiente Revisión',
    'InDiagnosis' => 'En Diagnóstico',
    'SentToWorkshop' => 'En Taller',
    'InRepair' => 'En Reparación',
    'PendingParts' => 'Pendiente Repuestos',
    'Repaired' => 'Reparada',
    'ReplacementApproved' => 'Reemplazo Aprobado',
    'ReadyForDelivery' => 'Lista para Entrega',
    'Delivered' => 'Entregada',
    'Closed' => 'Cerrada',
    'Cancelled' => 'Cancelada',
    _ => status,
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
    final filtered = _filter(state.items);
    return Scaffold(
      appBar: AppBar(title: const Text('Garantías')),
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
                          hintText: 'Buscar por cliente, producto, folio o estado...',
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
                          ? Center(child: Text(_searchQuery.isNotEmpty ? 'Sin resultados' : 'No hay garantías'))
                          : RefreshIndicator(
                              onRefresh: () => ref.read(warrantyProvider.notifier).load(),
                              child: ListView.separated(
                                itemCount: filtered.length,
                                separatorBuilder: (_, _) => const Divider(height: 1),
                                itemBuilder: (_, i) {
                                  final w = filtered[i];
                                  final stColor = _statusColor(w.status);
                                  return ListTile(
                                    leading: CircleAvatar(
                                      backgroundColor: stColor.withAlpha(30),
                                      child: Icon(Icons.verified, color: stColor),
                                    ),
                                    title: Text('${w.productName} - ${w.clientName}', style: const TextStyle(fontWeight: FontWeight.w600)),
                                    subtitle: Text('${w.warrantyNumber}' + (w.endDate != null ? ' · Exp: ${w.endDate!.substring(0, 10)}' : '')),
                                    trailing: Chip(label: Text(_statusLabel(w.status), style: TextStyle(fontSize: 11, color: stColor)), materialTapTargetSize: MaterialTapTargetSize.shrinkWrap),
                                  );
                                },
                              ),
                            ),
                    ),
                  ],
                ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final result = await context.push<bool>('/warranties/new');
          if (result == true) ref.read(warrantyProvider.notifier).load();
        },
        child: const Icon(Icons.add),
      ),
    );
  }
}
