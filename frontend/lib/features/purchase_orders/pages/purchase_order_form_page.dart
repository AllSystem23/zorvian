import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../suppliers/providers/supplier_provider.dart';
import '../../products/providers/product_provider.dart';
import '../../settings/providers/company_settings_provider.dart';
import '../../../core/utils/country_config.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../providers/purchase_order_provider.dart';

final class _CartLine {
  final String productId;
  final String productName;
  final double stock;
  String quantity;
  double unitCost;
  String discount;

  _CartLine({
    required this.productId, required this.productName, required this.stock,
    required this.quantity, required this.unitCost, required this.discount,
  });

  double get subtotal => (double.tryParse(quantity) ?? 0) * unitCost - (double.tryParse(discount) ?? 0);
}

final class PurchaseOrderFormPage extends ConsumerStatefulWidget {
  const PurchaseOrderFormPage({super.key});
  @override
  ConsumerState<PurchaseOrderFormPage> createState() => _PurchaseOrderFormPageState();
}

final class _PurchaseOrderFormPageState extends ConsumerState<PurchaseOrderFormPage> {
  String? _selectedSupplier;
  final _notesCtrl = TextEditingController();
  final List<_CartLine> _cart = [];
  bool _saving = false;
  String _currencyCode = 'NIO';
  String _countryCode = 'NI';

  static final _currencies = CountryConfig.countries.values.map((c) => c.currency).toSet().toList();

  @override
  void initState() {
    super.initState();
    Future.microtask(() {
      ref.read(supplierProvider.notifier).load();
      ref.read(productProvider.notifier).load();
      ref.read(companyInfoProvider.future).then((c) {
        if (c['country'] != null) {
          final country = c['country'] as String;
          setState(() => _countryCode = switch (country) {
            'Nicaragua' => 'NI',
            'Costa Rica' => 'CR',
            'El Salvador' => 'SV',
            'Honduras' => 'HN',
            'Guatemala' => 'GT',
            'Panamá' => 'PA',
            _ => 'NI',
          });
        }
      });
    });
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
            _cart.add(_CartLine(
              productId: p.id, productName: p.name, stock: p.stock,
              quantity: '1', unitCost: 0, discount: '0',
            ));
          });
          Navigator.pop(ctx);
        },
      ),
    );
  }

  void _err(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg), backgroundColor: Colors.red));
  }

  double get _subtotal => _cart.fold(0, (s, l) => s + l.subtotal);
  double get _total => _subtotal;

  Future<void> _save() async {
    if (_selectedSupplier == null) { _err('Seleccione un proveedor'); return; }
    if (_cart.isEmpty) { _err('Agregue al menos un producto'); return; }
    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'supplierId': _selectedSupplier,
        'orderDate': DateTime.now().toUtc().toIso8601String(),
        'expectedDate': null,
        'branchId': '00000000-0000-0000-0000-000000000000',
        'discount': 0,
        'currencyCode': _currencyCode,
        'countryCode': _countryCode,
        'notes': _notesCtrl.text.isEmpty ? null : _notesCtrl.text,
        'details': _cart.map((l) => {
          'productId': l.productId,
          'quantityOrdered': int.tryParse(l.quantity) ?? 0,
          'unitCost': l.unitCost,
          'discount': double.tryParse(l.discount) ?? 0,
        }).toList(),
      };
      await dio.post('purchase-orders', data: body);
      ref.invalidate(purchaseOrderProvider);
      if (mounted) context.pop();
    } catch (_) {
      _err('Error al crear orden de compra');
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final suppliers = ref.watch(supplierProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Nueva Orden de Compra'), actions: [
        TextButton(onPressed: _saving ? null : _save, child: _saving ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)) : const Text('Guardar')),
      ]),
      body: SingleChildScrollView(
        padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ZCard(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Proveedor', style: theme.textTheme.titleSmall),
                  const SizedBox(height: 8),
                  ZDropdownFormField<String>(
                    value: _selectedSupplier,
                    label: 'Proveedor',
                    items: suppliers.items.map((s) => DropdownMenuItem(value: s.id, child: Text(s.name))).toList(),
                    onChanged: (v) => setState(() => _selectedSupplier = v),
                  ),
                  const SizedBox(height: 12),
                  ZDropdownFormField<String>(
                    value: _currencyCode,
                    label: 'Moneda',
                    items: _currencies.map((c) => DropdownMenuItem(value: c, child: Text(c))).toList(),
                    onChanged: (v) => setState(() => _currencyCode = v!),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _notesCtrl,
                    decoration: InputDecoration(border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)), labelText: 'Notas'),
                    maxLines: 2,
                  ),
                ],
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
              const ZEmptyState(icon: Icons.inventory_2, title: 'No hay productos', subtitle: 'Agregue productos a la orden')
            else
              ZCard(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: _cart.asMap().entries.map((e) => _buildLine(e.key, e.value)).toList(),
                ),
              ),
            const SizedBox(height: 16),
            ZCard(
              padding: const EdgeInsets.all(16),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Total', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                  Text('$_currencyCode ${_total.toStringAsFixed(2)}', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                ],
              ),
            ),
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
          Row(
            children: [
              Expanded(
                child: TextField(
                  decoration: const InputDecoration(labelText: 'Cantidad', isDense: true),
                  controller: TextEditingController(text: line.quantity),
                  keyboardType: TextInputType.number,
                  onChanged: (v) => line.quantity = v,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: TextField(
                  decoration: const InputDecoration(labelText: 'Costo U.', isDense: true),
                  controller: TextEditingController(text: line.unitCost.toString()),
                  keyboardType: TextInputType.number,
                  onChanged: (v) => line.unitCost = double.tryParse(v) ?? 0,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: TextField(
                  decoration: const InputDecoration(labelText: 'Dscto', isDense: true),
                  controller: TextEditingController(text: line.discount),
                  keyboardType: TextInputType.number,
                  onChanged: (v) => line.discount = v,
                ),
              ),
            ],
          ),
          Text('Subtotal: ${line.subtotal.toStringAsFixed(2)}', style: TextStyle(color: Theme.of(context).colorScheme.primary, fontSize: 13)),
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
