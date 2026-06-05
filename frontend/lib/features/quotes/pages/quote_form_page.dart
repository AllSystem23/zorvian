import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/printing/qr_code_dialog.dart';
import '../../clients/providers/client_provider.dart';
import '../../products/providers/product_provider.dart';

class _CartItem {
  final String productId;
  final String productName;
  int quantity;
  final double unitPrice;
  double discount = 0;

  _CartItem({required this.productId, required this.productName, required this.quantity, required this.unitPrice});

  double get subtotal => quantity * unitPrice - discount;
}

class QuoteFormPage extends ConsumerStatefulWidget {
  final String? quoteId;
  const QuoteFormPage({super.key, this.quoteId});

  @override
  ConsumerState<QuoteFormPage> createState() => _QuoteFormPageState();
}

class _QuoteFormPageState extends ConsumerState<QuoteFormPage> {
  final _searchCtrl = TextEditingController();
  final _discountCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  ClientItem? _selectedClient;
  final List<_CartItem> _cart = [];
  bool _saving = false;
  bool _isEdit = false;

  double get _subtotal => _cart.fold(0, (sum, item) => sum + item.subtotal);
  double get _discount => double.tryParse(_discountCtrl.text) ?? 0;
  double get _taxable => (_subtotal - _discount).clamp(0, double.infinity);
  double get _tax => _taxable * 0.15;
  double get _total => _taxable + _tax;

  @override
  void initState() {
    super.initState();
    _isEdit = widget.quoteId != null;
    Future.microtask(() {
      ref.read(clientProvider.notifier).load();
      ref.read(productProvider.notifier).load();
      if (_isEdit) _loadQuote();
    });
  }

  Future<void> _loadQuote() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('quotes/${widget.quoteId}');
      final d = r.data as Map<String, dynamic>;
      final clientId = d['clientId'] as String;
      final clients = ref.read(clientProvider).items;
      final client = clients.where((c) => c.id == clientId).firstOrNull;
      if (client != null) _selectedClient = client;
      _discountCtrl.text = (d['discount'] as num?)?.toStringAsFixed(2) ?? '';
      _notesCtrl.text = d['notes'] as String? ?? '';
      final details = d['details'] as List? ?? [];
      for (final item in details) {
        _cart.add(_CartItem(
          productId: item['productId'] as String,
          productName: item['productName'] as String? ?? '',
          quantity: (item['quantity'] as num?)?.toInt() ?? 1,
          unitPrice: (item['unitPrice'] as num?)?.toDouble() ?? 0,
        ));
        _cart.last.discount = (item['discount'] as num?)?.toDouble() ?? 0;
      }
      setState(() {});
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Error al cargar cotización'), backgroundColor: Colors.red),
      );
    }
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _discountCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  void _addProduct(ProductItem p) {
    final existing = _cart.where((c) => c.productId == p.id).firstOrNull;
    if (existing != null) {
      setState(() => existing.quantity++);
    } else {
      setState(() => _cart.add(_CartItem(productId: p.id, productName: p.name, quantity: 1, unitPrice: p.price)));
    }
  }

  Future<void> _save() async {
    if (_selectedClient == null) { _err('Seleccione un cliente'); return; }
    if (_cart.isEmpty) { _err('Agregue al menos un producto'); return; }

    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'clientId': _selectedClient!.id,
        'branchId': '00000000-0000-0000-0000-000000000000',
        'discount': _discount,
        'notes': _notesCtrl.text.trim().isEmpty ? null : _notesCtrl.text.trim(),
        'details': _cart.map((c) => {
          'productId': c.productId,
          'quantity': c.quantity,
          'unitPrice': c.unitPrice,
          'discount': c.discount,
        }).toList(),
      };

      if (_isEdit) {
        await dio.put('quotes/${widget.quoteId}', data: body);
      } else {
        await dio.post('quotes', data: body);
      }

      if (mounted) context.pop(true);
    } catch (e) {
      _err('Error al guardar: $e');
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  void _err(String msg) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg), backgroundColor: Colors.red));
  }

  void _scanProduct(String code) {
    final product = ref.read(productProvider).items.where((p) => p.code.toLowerCase() == code.toLowerCase()).firstOrNull;
    if (product == null) {
      _err('Producto con código "$code" no encontrado');
      return;
    }
    _addProduct(product);
    _searchCtrl.clear();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final clientsAsync = ref.watch(clientProvider);
    final productsAsync = ref.watch(productProvider);

    return Scaffold(
      appBar: AppBar(title: Text(_isEdit ? 'Editar cotización' : 'Nueva cotización')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          DropdownButtonFormField<ClientItem>(
            decoration: const InputDecoration(labelText: 'Cliente'),
            value: _selectedClient,
            items: clientsAsync.items.map((c) => DropdownMenuItem(value: c, child: Text(c.fullName))).toList(),
            onChanged: (v) => setState(() => _selectedClient = v),
          ),
          const SizedBox(height: 16),
          Text('Productos', style: theme.textTheme.titleMedium),
          const SizedBox(height: 8),
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _searchCtrl,
                  decoration: InputDecoration(
                    hintText: 'Buscar producto...',
                    prefixIcon: const Icon(Icons.search, size: 20),
                    suffixIcon: IconButton(
                      icon: const Icon(Icons.qr_code_scanner, size: 20),
                      tooltip: 'Escanear código',
                      onPressed: () => showScannerDialog(context, onScan: _scanProduct),
                    ),
                    border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                    contentPadding: const EdgeInsets.symmetric(vertical: 0, horizontal: 12),
                  ),
                  onChanged: (v) => setState(() {}),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          if (productsAsync.items.where((p) => _searchCtrl.text.isEmpty || p.name.toLowerCase().contains(_searchCtrl.text.toLowerCase())).take(5).toList() case final found?)
            ...found.map((p) => Card(
              child: ListTile(
                dense: true,
                title: Text(p.name, style: const TextStyle(fontSize: 13)),
                subtitle: Text('\$${p.price.toStringAsFixed(2)}', style: const TextStyle(fontSize: 11)),
                trailing: IconButton(
                  icon: const Icon(Icons.add_circle_outline, size: 20),
                  onPressed: () => _addProduct(p),
                ),
              ),
            )),
          const SizedBox(height: 16),
          if (_cart.isEmpty)
            const Center(child: Text('No hay productos agregados'))
          else
            ..._cart.asMap().entries.map((entry) {
              final i = entry.key;
              final c = entry.value;
              return Card(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                  child: Row(children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(c.productName, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
                          Text('\$${c.unitPrice.toStringAsFixed(2)} c/u', style: const TextStyle(fontSize: 11)),
                        ],
                      ),
                    ),
                    IconButton(
                      icon: const Icon(Icons.remove_circle_outline, size: 18),
                      onPressed: c.quantity > 1 ? () => setState(() => c.quantity--) : null,
                    ),
                    Text('${c.quantity}', style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold)),
                    IconButton(
                      icon: const Icon(Icons.add_circle_outline, size: 18),
                      onPressed: () => setState(() => c.quantity++),
                    ),
                    SizedBox(
                      width: 80,
                      child: TextField(
                        keyboardType: TextInputType.number,
                        decoration: const InputDecoration(labelText: 'Desc', isDense: true, contentPadding: EdgeInsets.symmetric(vertical: 8, horizontal: 4)),
                        controller: TextEditingController(text: c.discount.toStringAsFixed(0)),
                        style: const TextStyle(fontSize: 12),
                        onChanged: (v) => setState(() => c.discount = double.tryParse(v) ?? 0),
                      ),
                    ),
                    SizedBox(width: 80, child: Text('\$${c.subtotal.toStringAsFixed(0)}', textAlign: TextAlign.right, style: const TextStyle(fontWeight: FontWeight.w600))),
                    IconButton(
                      icon: const Icon(Icons.close, size: 18, color: Colors.red),
                      onPressed: () => setState(() => _cart.removeAt(i)),
                    ),
                  ]),
                ),
              );
            }),
          const SizedBox(height: 16),
          TextFormField(
            controller: _discountCtrl,
            decoration: const InputDecoration(labelText: 'Descuento global', prefixText: '\$ '),
            keyboardType: TextInputType.number,
            onChanged: (_) => setState(() {}),
          ),
          const SizedBox(height: 8),
          TextFormField(
            controller: _notesCtrl,
            decoration: const InputDecoration(labelText: 'Notas (opcional)'),
            maxLines: 2,
          ),
          const Divider(height: 32),
          _row('Subtotal', '\$${_subtotal.toStringAsFixed(2)}'),
          _row('Descuento', '\$${_discount.toStringAsFixed(2)}'),
          _row('IVA (15%)', '\$${_tax.toStringAsFixed(2)}'),
          _row('Total', '\$${_total.toStringAsFixed(2)}', bold: true),
          const SizedBox(height: 24),
          FilledButton.icon(
            onPressed: _saving ? null : _save,
            icon: _saving
                ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                : const Icon(Icons.save),
            label: Text(_saving ? 'Guardando...' : 'Guardar cotización'),
          ),
        ],
      ),
    );
  }

  Widget _row(String label, String value, {bool bold = false}) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(children: [
        Expanded(child: Text(label, style: TextStyle(color: theme.colorScheme.onSurfaceVariant, fontSize: 13))),
        Text(value, style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.w600, fontSize: 13)),
      ]),
    );
  }
}
