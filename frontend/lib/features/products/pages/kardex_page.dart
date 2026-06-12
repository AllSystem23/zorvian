import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../../inventory_movements/providers/inventory_movement_provider.dart';

class KardexPage extends ConsumerWidget {
  final String? productId;

  const KardexPage({super.key, this.productId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(inventoryMovementProvider);
    final theme = Theme.of(context);
    
    return Scaffold(
      backgroundColor: theme.brightness == Brightness.dark ? ZColors.darkBackground : ZColors.neutral50,
      appBar: AppBar(title: const Text('Kardex - Trazabilidad de Inventario')),
      body: state.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Error: $e', style: TextStyle(color: theme.colorScheme.error))),
        data: (movements) {
          final filtered = productId != null 
              ? movements.where((m) => m.productCode == productId).toList()
              : movements;

          return Padding(
            padding: const EdgeInsets.all(24),
            child: ZDataTable<InventoryMovementItem>(
              columns: const [
                ZColumn(id: 'date', label: 'Fecha'),
                ZColumn(id: 'product', label: 'Producto'),
                ZColumn(id: 'type', label: 'Tipo'),
                ZColumn(id: 'qty', label: 'Cantidad', numeric: true),
                ZColumn(id: 'stock', label: 'Stock Final', numeric: true),
              ],
              rows: filtered,
              rowMapper: (m) => DataRow(cells: [
                DataCell(Text(m.createdAt.substring(0, 16))),
                DataCell(Text('${m.productName} (${m.productCode})', style: const TextStyle(fontWeight: FontWeight.w600))),
                DataCell(ZBadge(
                  text: m.type.toUpperCase(),
                  type: m.type == 'in' ? ZBadgeType.success : ZBadgeType.danger,
                )),
                DataCell(Text(m.quantity.toString())),
                DataCell(Text(m.stockAfter.toString(), style: const TextStyle(fontWeight: FontWeight.bold))),
              ]),
            ),
          );
        },
      ),
    );
  }
}
