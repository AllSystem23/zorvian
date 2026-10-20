import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../products/providers/product_provider.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';
import '../providers/stock_transfer_provider.dart';

final class _CartLine {
  final String productId;
  final String productName;
  String quantity;

  _CartLine({required this.productId, required this.productName, required this.quantity});

  int get qty => int.tryParse(quantity) ?? 0;
}

final class StockTransferFormPage extends ConsumerStatefulWidget {
  const StockTransferFormPage({super.key});
  @override
  ConsumerState<StockTransferFormPage> createState() => _StockTransferFormPageState();
}

final class _StockTransferFormPageState extends ConsumerState<StockTransferFormPage> {
  final _notesCtrl = TextEditingController();
  final List<_CartLine> _cart = [];
  String? _fromWarehouseId;
  String? _toBranchId;
  String? _toWarehouseId;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(productProvider.notifier).load());
  }

  @override
  void dispose() {
    _notesCtrl.dispose();
    super.dispose();
  }

  void _addProduct() {
    final products = ref.read(productProvider).items;
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => _ProductPicker(
        products: products,
        onSelect: (p) {
          setState(() {
            _cart.add(_CartLine(productId: p.id, productName: p.name, quantity: '1'));
          });
          Navigator.pop(ctx);
        },
      ),
    );
  }

  void _err(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg), backgroundColor: Colors.red));
  }

  Future<void> _save() async {
    final branchId = ref.read(companyBranchProvider).branchId;
    // Bodega origen: la elegida en el form, o la del cabecero; null = default de la sucursal.
    final headerWarehouseId = ref.read(companyBranchProvider).warehouseId;
    final fromWarehouseId = _fromWarehouseId ?? headerWarehouseId;
    if (branchId == null) { _err('Seleccione la sucursal origen en el cabecero superior'); return; }
    if (_toWarehouseId == null) { _err('Seleccione la bodega destino'); return; }
    if (fromWarehouseId != null && _toWarehouseId == fromWarehouseId) {
      _err('La bodega destino debe ser distinta a la de origen');
      return;
    }
    if (_cart.isEmpty) { _err('Agregue al menos un producto'); return; }
    if (_cart.any((l) => l.qty <= 0)) { _err('Todas las cantidades deben ser mayores a 0'); return; }

    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      final body = CreateStockTransferRequest(
        fromWarehouseId: fromWarehouseId,
        toWarehouseId: _toWarehouseId,
        notes: _notesCtrl.text.isEmpty ? null : _notesCtrl.text,
        details: _cart.map((l) => CreateStockTransferLine(
          productId: l.productId,
          quantity: l.qty,
        )).toList(),
      );
      await dio.post('stock-transfers', data: body.toJson());
      ref.invalidate(stockTransferProvider);
      if (mounted) context.pop();
    } catch (_) {
      _err('Error al crear la transferencia');
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cb = ref.watch(companyBranchProvider);
    final branches = ref.watch(headerBranchListProvider);
    final warehouses = ref.watch(headerWarehouseListProvider);

    return Scaffold(
      body: SingleChildScrollView(
        padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ZPageHeader(
              title: 'Nueva Transferencia entre Bodegas',
              actions: [
                TextButton(
                  onPressed: _saving ? null : _save,
                  child: _saving
                      ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                      : const Text('Guardar'),
                ),
              ],
            ),
            const SizedBox(height: 16),
            ZCard(
              padding: const EdgeInsets.all(16),
              child: warehouses.when(
                data: (whItems) {
                  // Bodega origen preseleccionada: la del cabecero, o la default.
                  _fromWarehouseId ??= cb.warehouseId ??
                      whItems.where((w) => (w['isDefault'] as bool? ?? false)).map((w) => w['id'] as String).firstOrNull ??
                      (whItems.isNotEmpty ? whItems.first['id'] as String? : null);
                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      InputDecorator(
                        decoration: InputDecoration(
                          labelText: 'Sucursal origen (actual)',
                          prefixIcon: const Icon(Icons.store_mall_directory_outlined),
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                        ),
                        child: Text(cb.branchName == null || cb.branchName!.isEmpty ? 'Sucursal actual' : cb.branchName!),
                      ),
                      const SizedBox(height: 12),
                      ZDropdownFormField<String>(
                        value: _fromWarehouseId,
                        label: 'Bodega origen *',
                        prefixIcon: Icons.warehouse_outlined,
                        items: whItems
                            .map((w) => DropdownMenuItem(
                                  value: w['id'] as String,
                                  child: Text((w['name'] as String? ?? 'Bodega') +
                                      ((w['isDefault'] as bool? ?? false) ? ' (principal)' : '')),
                                ))
                            .toList(),
                        onChanged: (v) => setState(() => _fromWarehouseId = v),
                        validator: (v) => v == null ? 'Requerido' : null,
                      ),
                      const SizedBox(height: 12),
                      // Destino: cascada sucursal → bodega (permite otras sucursales
                      // u otras bodegas de la misma sucursal).
                      branches.when(
                        data: (items) {
                          return Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              ZDropdownFormField<String>(
                                value: _toBranchId,
                                label: 'Sucursal destino *',
                                prefixIcon: Icons.store_mall_directory_outlined,
                                items: items
                                    .map((e) => DropdownMenuItem(
                                          value: e['id'] as String,
                                          child: Text(e['name'] as String? ?? ''),
                                        ))
                                    .toList(),
                                onChanged: (v) => setState(() {
                                  _toBranchId = v;
                                  _toWarehouseId = null; // recarga de la nueva sucursal
                                }),
                                validator: (v) => v == null ? 'Requerido' : null,
                              ),
                              if (_toBranchId != null) ...[
                                const SizedBox(height: 12),
                                _DestinationWarehousesField(
                                  branchId: _toBranchId!,
                                  value: _toWarehouseId,
                                  excludeWarehouseId: _fromWarehouseId,
                                  onChanged: (v) => setState(() => _toWarehouseId = v),
                                ),
                              ],
                            ],
                          );
                        },
                        loading: () => const LinearProgressIndicator(),
                        error: (_, _) => const Text('No se pudieron cargar las sucursales'),
                      ),
                      const SizedBox(height: 12),
                      TextField(
                        controller: _notesCtrl,
                        decoration: InputDecoration(
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                          labelText: 'Notas',
                        ),
                        maxLines: 2,
                      ),
                    ],
                  );
                },
                loading: () => const Column(
                  children: [LinearProgressIndicator(), SizedBox(height: 16), Text('Cargando bodegas...')],
                ),
                error: (_, _) => const Text('No se pudieron cargar las bodegas'),
              ),
            ),
            const SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Productos (${_cart.length})', style: theme.textTheme.titleSmall),
                ZButton(text: 'Agregar producto', icon: Icons.add, onPressed: _addProduct, fullWidth: false),
              ],
            ),
            const SizedBox(height: 8),
            if (_cart.isEmpty)
              const ZEmptyState(
                icon: Icons.swap_horizontal_circle_outlined,
                title: 'No hay productos',
                subtitle: 'Agregue los productos a transferir (catálogo de la bodega origen)',
              )
            else
              ZCard(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: _cart.asMap().entries.map((e) => _buildLine(e.key, e.value)).toList(),
                ),
              ),
            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }

  Widget _buildLine(int index, _CartLine line) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(child: Text(line.productName, style: const TextStyle(fontWeight: FontWeight.w500))),
              IconButton(icon: const Icon(Icons.close, size: 18), onPressed: () => setState(() => _cart.removeAt(index))),
            ],
          ),
          const SizedBox(height: 4),
          TextField(
            decoration: const InputDecoration(labelText: 'Cantidad a transferir', isDense: true),
            controller: TextEditingController(text: line.quantity),
            keyboardType: TextInputType.number,
            onChanged: (v) => line.quantity = v,
          ),
        ],
      ),
    );
  }
}

/// Dropdown de bodegas de la sucursal destino (fetch con branchId explícito,
/// que manda sobre la sucursal del contexto). Excluye la bodega origen.
final class _DestinationWarehousesField extends ConsumerStatefulWidget {
  final String branchId;
  final String? value;
  final String? excludeWarehouseId;
  final ValueChanged<String?> onChanged;
  const _DestinationWarehousesField({
    required this.branchId,
    required this.value,
    required this.excludeWarehouseId,
    required this.onChanged,
  });
  @override
  ConsumerState<_DestinationWarehousesField> createState() => _DestinationWarehousesFieldState();
}

final class _DestinationWarehousesFieldState extends ConsumerState<_DestinationWarehousesField> {
  late Future<List<Map<String, dynamic>>> _future;

  @override
  void initState() {
    super.initState();
    _future = _fetch();
  }

  @override
  void didUpdateWidget(covariant _DestinationWarehousesField old) {
    super.didUpdateWidget(old);
    if (old.branchId != widget.branchId) {
      _future = _fetch();
    }
  }

  Future<List<Map<String, dynamic>>> _fetch() async {
    final dio = ref.read(dioClientProvider);
    try {
      final r = await dio.get('warehouses', params: {'branchId': widget.branchId});
      if (r.data is List) return (r.data as List).cast<Map<String, dynamic>>();
      return [];
    } catch (_) {
      return [];
    }
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<Map<String, dynamic>>>(
      future: _future,
      builder: (context, snap) {
        if (snap.connectionState != ConnectionState.done) {
          return const Padding(
            padding: EdgeInsets.symmetric(vertical: 12),
            child: LinearProgressIndicator(),
          );
        }
        final items = (snap.data ?? [])
            .where((w) => w['id'] != widget.excludeWarehouseId)
            .toList();
        if (items.isEmpty) {
          return const Text('Esta sucursal no tiene otras bodegas.',
              style: TextStyle(fontSize: 13, color: Colors.grey));
        }
        return ZDropdownFormField<String>(
          value: widget.value,
          label: 'Bodega destino *',
          prefixIcon: Icons.warehouse_outlined,
          items: items
              .map((w) => DropdownMenuItem(
                    value: w['id'] as String,
                    child: Text((w['name'] as String? ?? 'Bodega') +
                        ((w['isDefault'] as bool? ?? false) ? ' (principal)' : '')),
                  ))
              .toList(),
          onChanged: widget.onChanged,
          validator: (v) => v == null ? 'Requerido' : null,
        );
      },
    );
  }
}

final class _ProductPicker extends ConsumerStatefulWidget {
  final List<ProductItem> products;
  final void Function(ProductItem) onSelect;
  const _ProductPicker({required this.products, required this.onSelect});
  @override
  ConsumerState<_ProductPicker> createState() => _ProductPickerState();
}

final class _ProductPickerState extends ConsumerState<_ProductPicker> {
  String _q = '';

  @override
  Widget build(BuildContext context) {
    final filtered = widget.products.where((p) => _q.isEmpty || p.name.toLowerCase().contains(_q.toLowerCase())).toList();
    return DraggableScrollableSheet(
      initialChildSize: 0.7,
      builder: (_, scroll) => Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: TextField(
              decoration: InputDecoration(hintText: 'Buscar producto...', prefixIcon: const Icon(Icons.search), border: OutlineInputBorder(borderRadius: BorderRadius.circular(12))),
              onChanged: (v) => setState(() => _q = v),
            ),
          ),
          Expanded(
            child: ListView.separated(
              controller: scroll,
              itemCount: filtered.length,
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (_, i) {
                final p = filtered[i];
                return ListTile(
                  title: Text(p.name),
                  subtitle: Text('Cód: ${p.code} | Stock: ${p.stock}'),
                  onTap: () => widget.onSelect(p),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
