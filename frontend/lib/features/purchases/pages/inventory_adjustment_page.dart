import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../products/providers/product_provider.dart';
import '../../inventory_movements/providers/inventory_movement_provider.dart';
import '../../../shared/printing/qr_code_dialog.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

final class _SelectedProduct {
  final String id;
  final String name;
  final String code;
  final double stock;
  _SelectedProduct({required this.id, required this.name, required this.code, required this.stock});
}

final class InventoryAdjustmentPage extends ConsumerStatefulWidget {
  const InventoryAdjustmentPage({super.key});
  @override
  ConsumerState<InventoryAdjustmentPage> createState() => _InventoryAdjustmentPageState();
}

final class _InventoryAdjustmentPageState extends ConsumerState<InventoryAdjustmentPage> {
  _SelectedProduct? _product;
  String _type = 'entry';
  final _qtyCtrl = TextEditingController(text: '1');
  final _costCtrl = TextEditingController(text: '0');
  final _notesCtrl = TextEditingController();
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(productProvider.notifier).load());
  }

  void _scanProduct(String code) {
    final products = ref.read(productProvider).items;
    final found = products.where((p) => p.code == code || p.id == code).toList();
    if (found.isEmpty) {
      if (mounted) _err('Producto no encontrado: $code');
      return;
    }
    final p = found.first;
    setState(() {
      _product = _SelectedProduct(id: p.id, name: p.name, code: p.code, stock: p.stock);
      _searchCtrl.clear();
      _searchQuery = '';
    });
  }

  void _err(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg), backgroundColor: Colors.red));
  }

  Future<void> _save() async {
    if (_product == null) { _err('Seleccione un producto'); return; }

    final qty = int.tryParse(_qtyCtrl.text);
    if (qty == null || qty <= 0) { _err('Ingrese una cantidad válida'); return; }

    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      final finalQty = _type == 'exit' || _type == 'adjustment_negative' ? -qty : qty;
      final body = {
        'productId': _product!.id,
        'movementType': _type,
        'quantity': finalQty.abs(),
        'unitCost': double.tryParse(_costCtrl.text) ?? 0,
        'notes': _notesCtrl.text.isNotEmpty ? _notesCtrl.text : null,
      };
      await dio.post('inventory-movements', data: body);
      ref.invalidate(inventoryMovementProvider);
      ref.invalidate(productProvider);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Movimiento registrado'), backgroundColor: Colors.green));
        context.pop();
      }
    } catch (e) {
      _err('Error al registrar movimiento');
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  void dispose() {
    _qtyCtrl.dispose(); _costCtrl.dispose(); _notesCtrl.dispose(); _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final products = ref.watch(productProvider);
    final filtered = _searchQuery.isEmpty
        ? products.items
        : products.items.where((p) =>
            p.name.toLowerCase().contains(_searchQuery.toLowerCase()) ||
            p.code.toLowerCase().contains(_searchQuery.toLowerCase())
          ).take(10).toList();

    return Scaffold(
      appBar: AppBar(title: const Text('Ajuste de Inventario')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          ZCard(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Producto', style: TextStyle(fontWeight: FontWeight.w600)),
                const SizedBox(height: 8),
                TextField(
                  controller: _searchCtrl,
                  decoration: InputDecoration(
                    hintText: 'Buscar producto...',
                    prefixIcon: const Icon(Icons.search),
                    suffixIcon: IconButton(
                      icon: const Icon(Icons.qr_code_scanner, size: 20),
                      tooltip: 'Escanear código',
                      onPressed: () => showScannerDialog(context, onScan: _scanProduct),
                    ),
                    border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                    contentPadding: const EdgeInsets.symmetric(vertical: 0),
                  ),
                  onChanged: (v) => setState(() => _searchQuery = v),
                ),
                  if (_searchQuery.isNotEmpty && _product == null)
                    ...filtered.map((p) => ListTile(
                      dense: true,
                      title: Text('${p.name} (${p.code})'),
                      subtitle: Text('Stock: ${p.stock.toStringAsFixed(0)} · Costo: \$${p.cost?.toStringAsFixed(2) ?? "N/A"}'),
                      onTap: () {
                        setState(() {
                          _product = _SelectedProduct(id: p.id, name: p.name, code: p.code, stock: p.stock);
                          _searchCtrl.clear();
                          _searchQuery = '';
                        });
                      },
                    )),
                  if (_product != null)
                    Container(
                      margin: const EdgeInsets.only(top: 8),
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: theme.colorScheme.primaryContainer.withAlpha(30),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('${_product!.name} (${_product!.code})', style: const TextStyle(fontWeight: FontWeight.w600)),
                                Text('Stock actual: ${_product!.stock.toStringAsFixed(0)}'),
                              ],
                            ),
                          ),
                          IconButton(
                            icon: const Icon(Icons.close, size: 18),
                            onPressed: () => setState(() => _product = null),
                          ),
                        ],
                      ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            ZCard(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Tipo de Movimiento', style: TextStyle(fontWeight: FontWeight.w600)),
                const SizedBox(height: 8),
                DropdownButtonFormField<String>(
                  initialValue: _type,
                  decoration: const InputDecoration(border: OutlineInputBorder(), contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 8)),
                  items: const [
                    DropdownMenuItem(value: 'entry', child: Text('Entrada')),
                    DropdownMenuItem(value: 'exit', child: Text('Salida')),
                    DropdownMenuItem(value: 'adjustment_positive', child: Text('Ajuste (+)')),
                    DropdownMenuItem(value: 'adjustment_negative', child: Text('Ajuste (-)')),
                  ],
                  onChanged: (v) => setState(() => _type = v!),
                ),
                const SizedBox(height: 12),
                ZTextField(
                  controller: _qtyCtrl,
                  label: 'Cantidad',
                  keyboardType: TextInputType.number,
                ),
                const SizedBox(height: 12),
                ZTextField(
                  controller: _costCtrl,
                  label: 'Costo unitario',
                  keyboardType: TextInputType.number,
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          ZTextField(
            controller: _notesCtrl,
            label: 'Notas',
            maxLines: 2,
          ),
          const SizedBox(height: 24),
          ZButton(
            text: 'Registrar Movimiento',
            onPressed: _save,
            isLoading: _saving,
          ),
          const SizedBox(height: 40),
        ],
      ),
    );
  }
}
