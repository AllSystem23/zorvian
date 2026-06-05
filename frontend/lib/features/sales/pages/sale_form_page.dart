import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/printing/qr_code_dialog.dart';
import '../../clients/providers/client_provider.dart';
import '../../products/providers/product_provider.dart';
import '../providers/sale_provider.dart';

final class _CartItem {
  final String productId;
  final String productName;
  int quantity;
  final double unitPrice;
  double discount = 0;

  _CartItem({required this.productId, required this.productName, required this.quantity, required this.unitPrice});

  double get subtotal => quantity * unitPrice - discount;
}

final class NewSalePage extends ConsumerStatefulWidget {
  const NewSalePage({super.key});
  @override
  ConsumerState<NewSalePage> createState() => _NewSalePageState();
}

final class _NewSalePageState extends ConsumerState<NewSalePage> {
  final _searchCtrl = TextEditingController();
 ClientItem? _selectedClient;
  bool _isCredit = false;
  final _downPaymentCtrl = TextEditingController();
  final _installmentCtrl = TextEditingController(text: '6');
  final _interestCtrl = TextEditingController(text: '10');
  final _discountCtrl = TextEditingController();
  final List<_CartItem> _cart = [];
  bool _saving = false;

  @override
  void dispose() {
    _searchCtrl.dispose();
    _downPaymentCtrl.dispose();
    _installmentCtrl.dispose();
    _interestCtrl.dispose();
    _discountCtrl.dispose();
    super.dispose();
  }

  double get _subtotal => _cart.fold(0, (sum, item) => sum + item.subtotal);
  double get _discount => double.tryParse(_discountCtrl.text) ?? 0;
  double get _taxable => (_subtotal - _discount).clamp(0, double.infinity);

  bool _taxEnabled = true;
  double _taxRate = 0.15;

  @override
  void initState() {
    super.initState();
    Future.microtask(() {
      ref.read(clientProvider.notifier).load();
      ref.read(productProvider.notifier).load();
    });
    _loadTaxConfig();
  }

  Future<void> _loadTaxConfig() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('companies/settings');
      final s = r.data as Map<String, dynamic>;
      if (s.containsKey('taxEnabled')) setState(() => _taxEnabled = s['taxEnabled'] as bool);
      if (s.containsKey('taxRate')) setState(() => _taxRate = (s['taxRate'] as num).toDouble());
    } catch (_) {}
  }

  double get _tax => _taxEnabled ? _taxable * _taxRate : 0;
  double get _total => _taxable + _tax;

  Future<void> _save() async {
    if (_selectedClient == null) { _err('Seleccione un cliente'); return; }
    if (_cart.isEmpty) { _err('Agregue al menos un producto'); return; }

    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      final details = _cart.map((c) => {
        'productId': c.productId,
        'productName': c.productName,
        'quantity': c.quantity,
        'unitPrice': c.unitPrice,
        'discount': c.discount,
        'subtotal': c.subtotal,
      }).toList();

      if (_isCredit) {
        await dio.post('sales/credit', data: {
          'clientId': _selectedClient!.id,
          'employeeId': '00000000-0000-0000-0000-000000000000',
          'discount': _discount,
          'notes': null,
          'branchId': '00000000-0000-0000-0000-000000000000',
          'details': details,
          'downPayment': double.tryParse(_downPaymentCtrl.text) ?? 0,
          'installmentCount': int.tryParse(_installmentCtrl.text) ?? 6,
          'interestRate': double.tryParse(_interestCtrl.text) ?? 10,
        });
      } else {
        await dio.post('sales/cash', data: {
          'clientId': _selectedClient!.id,
          'employeeId': '00000000-0000-0000-0000-000000000000',
          'discount': _discount,
          'notes': null,
          'branchId': '00000000-0000-0000-0000-000000000000',
          'details': details,
          'payment': {'amount': _total, 'paymentMethod': 'cash', 'referenceNumber': null, 'cashRegisterId': null},
        });
      }

      ref.invalidate(saleProvider);
      if (mounted) context.go('/sales');
    } catch (e) {
      _err('Error al crear venta: $e');
    }
    setState(() => _saving = false);
  }

  void _err(String msg) {
    if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  void _scanProduct(String code) {
    final product = ref.read(productProvider).items.where((p) => p.code.toLowerCase() == code.toLowerCase()).firstOrNull;
    if (product == null) {
      _err('Producto con código "$code" no encontrado');
      return;
    }
    setState(() {
      final existing = _cart.where((c) => c.productId == product.id).firstOrNull;
      if (existing != null) { existing.quantity++; } else { _cart.add(_CartItem(productId: product.id, productName: product.name, quantity: 1, unitPrice: product.price)); }
      _searchCtrl.clear();
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final clients = ref.watch(clientProvider);
    final products = ref.watch(productProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Nueva Venta')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text('Cliente', style: theme.textTheme.titleSmall),
          const SizedBox(height: 4),
          clients.loading
              ? const LinearProgressIndicator()
              : DropdownButtonFormField<ClientItem>(
                  initialValue: _selectedClient,
                  isExpanded: true,
                  items: clients.items.map((c) => DropdownMenuItem(value: c, child: Text('${c.fullName} (${c.code})', overflow: TextOverflow.ellipsis))).toList(),
                  onChanged: (v) => setState(() => _selectedClient = v),
                  decoration: const InputDecoration(border: OutlineInputBorder(), hintText: 'Seleccionar cliente'),
                ),
          const SizedBox(height: 16),
          SwitchListTile(
            title: const Text('Venta a Crédito'),
            subtitle: Text(_isCredit ? 'Genera crédito con cuotas' : 'Pago de contado'),
            value: _isCredit,
            onChanged: (v) => setState(() => _isCredit = v),
          ),
          if (_isCredit) ...[
            TextField(controller: _downPaymentCtrl, decoration: const InputDecoration(labelText: 'Enganche', border: OutlineInputBorder(), prefixText: '\$ '), keyboardType: TextInputType.number),
            const SizedBox(height: 8),
            Row(children: [
              Expanded(child: TextField(controller: _installmentCtrl, decoration: const InputDecoration(labelText: 'Cuotas', border: OutlineInputBorder()), keyboardType: TextInputType.number)),
              const SizedBox(width: 12),
              Expanded(child: TextField(controller: _interestCtrl, decoration: const InputDecoration(labelText: 'Interés %', border: OutlineInputBorder()), keyboardType: TextInputType.number)),
            ]),
          ],
          const SizedBox(height: 16),
          Text('Productos', style: theme.textTheme.titleSmall),
          const SizedBox(height: 4),
          ..._cart.asMap().entries.map((entry) {
            final i = entry.key;
            final item = entry.value;
            return Card(
              child: Padding(
                padding: const EdgeInsets.all(8),
                child: Row(children: [
                  Expanded(child: Text(item.productName, style: const TextStyle(fontWeight: FontWeight.w600))),
                  SizedBox(
                    width: 60,
                    child: TextField(
                      controller: TextEditingController(text: item.quantity.toString()),
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(labelText: 'Cant', border: OutlineInputBorder(), contentPadding: EdgeInsets.symmetric(horizontal: 4, vertical: 8), isDense: true),
                      onChanged: (v) => setState(() => item.quantity = int.tryParse(v) ?? 1),
                    ),
                  ),
                  const SizedBox(width: 4),
                  Text('\$${item.subtotal.toStringAsFixed(0)}', style: const TextStyle(fontWeight: FontWeight.bold)),
                  IconButton(icon: const Icon(Icons.remove_circle, color: Colors.red, size: 20), onPressed: () => setState(() => _cart.removeAt(i))),
                ]),
              ),
            );
          }),
          const SizedBox(height: 8),
          if (products.error != null)
            Text(products.error!, style: TextStyle(color: theme.colorScheme.error))
          else
            Row(
              children: [
                Expanded(
                  child: TextField(
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
                    onChanged: (v) => setState(() {}),
                  ),
                ),
              ],
            ),
          const SizedBox(height: 4),
          if (_searchCtrl.text.isNotEmpty)
            SizedBox(
              height: 160,
              child: products.loading
                  ? const Center(child: CircularProgressIndicator())
                  : ListView(
                      children: products.items
                          .where((p) => p.name.toLowerCase().contains(_searchCtrl.text.toLowerCase()) || p.code.toLowerCase().contains(_searchCtrl.text.toLowerCase()))
                          .take(10)
                          .map((p) => ListTile(
                            dense: true,
                            title: Text(p.name),
                            subtitle: Text('\$${p.price.toStringAsFixed(2)} · Stock: ${p.stock.toStringAsFixed(0)}'),
                            trailing: IconButton(
                              icon: const Icon(Icons.add_shopping_cart),
                              onPressed: () {
                                setState(() {
                                  final existing = _cart.where((c) => c.productId == p.id).firstOrNull;
                                  if (existing != null) { existing.quantity++; } else { _cart.add(_CartItem(productId: p.id, productName: p.name, quantity: 1, unitPrice: p.price)); }
                                  _searchCtrl.clear();
                                });
                              },
                            ),
                          ))
                          .toList(),
                    ),
            ),
          const Divider(),
          Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
            const Text('Subtotal', style: TextStyle(color: Colors.grey)),
            Text('\$${_subtotal.toStringAsFixed(2)}', style: const TextStyle(fontWeight: FontWeight.bold)),
          ]),
          if (_discount > 0)
            Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
              const Text('Descuento', style: TextStyle(color: Colors.grey)),
              Text('-\$${_discount.toStringAsFixed(2)}', style: const TextStyle(color: Colors.red)),
            ]),
          const SizedBox(height: 4),
          TextField(controller: _discountCtrl, decoration: const InputDecoration(labelText: 'Descuento', border: OutlineInputBorder(), prefixText: '\$ ', isDense: true), keyboardType: TextInputType.number, onChanged: (_) => setState(() {})),
          if (_taxEnabled)
            Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
              Text('IVA (${(_taxRate * 100).toStringAsFixed(0)}%)', style: const TextStyle(color: Colors.grey)),
              Text('\$${_tax.toStringAsFixed(2)}'),
            ]),
          const Divider(),
          Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
            Text('Total', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            Text('\$${_total.toStringAsFixed(2)}', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold, color: Colors.green)),
          ]),
          const SizedBox(height: 24),
          SizedBox(
            width: double.infinity,
            height: 48,
            child: FilledButton.icon(
              icon: _saving ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white)) : const Icon(Icons.receipt),
              label: Text(_isCredit ? 'Crear Venta a Crédito' : 'Crear Venta de Contado'),
              onPressed: _saving ? null : _save,
            ),
          ),
        ],
      ),
    );
  }
}
