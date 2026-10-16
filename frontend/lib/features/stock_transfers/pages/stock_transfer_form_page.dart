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
  String? _toBranchId;
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
    final fromBranchId = ref.read(companyBranchProvider).branchId;
    if (fromBranchId == null) { _err('Seleccione la sucursal origen en el cabecero superior'); return; }
    if (_toBranchId == null) { _err('Seleccione la sucursal destino'); return; }
    if (_toBranchId == fromBranchId) { _err('El destino debe ser distinto al origen'); return; }
    if (_cart.isEmpty) { _err('Agregue al menos un producto'); return; }
    if (_cart.any((l) => l.qty <= 0)) { _err('Todas las cantidades deben ser mayores a 0'); return; }

    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      final body = CreateStockTransferRequest(
        fromBranchId: fromBranchId,
        toBranchId: _toBranchId,
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
    final fromBranchId = ref.watch(companyBranchProvider).branchId;
    final branches = ref.watch(headerBranchListProvider);

    return Scaffold(
      body: SingleChildScrollView(
        padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text('Nueva Transferencia entre Bodegas', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
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
              child: branches.when(
                data: (items) {
                  final fromName = fromBranchId == null
                      ? null
                      : items.where((e) => e['id'] == fromBranchId).map((e) => e['name'] as String? ?? '').firstOrNull;
                  final destinations = items.where((e) => e['id'] != fromBranchId).toList();
                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      InputDecorator(
                        decoration: InputDecoration(
                          labelText: 'Sucursal origen (actual)',
                          prefixIcon: const Icon(Icons.warehouse_outlined),
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                        ),
                        child: Text(fromName == null || fromName.isEmpty ? 'Sucursal actual' : fromName),
                      ),
                      const SizedBox(height: 12),
                      ZDropdownFormField<String>(
                        value: _toBranchId,
                        label: 'Sucursal destino *',
                        prefixIcon: Icons.store_mall_directory_outlined,
                        items: destinations
                            .map((e) => DropdownMenuItem(value: e['id'] as String, child: Text(e['name'] as String? ?? '')))
                            .toList(),
                        onChanged: (v) => setState(() => _toBranchId = v),
                        validator: (v) => v == null ? 'Requerido' : null,
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
                  children: [LinearProgressIndicator(), SizedBox(height: 16), Text('Cargando sucursales...')],
                ),
                error: (_, _) => const Text('No se pudieron cargar las sucursales'),
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
                subtitle: 'Agregue los productos a transferir (catálogo de la sucursal origen)',
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
