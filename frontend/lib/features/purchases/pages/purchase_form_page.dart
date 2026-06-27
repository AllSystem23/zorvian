import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:file_picker/file_picker.dart';
import 'package:dio/dio.dart';
import '../../suppliers/providers/supplier_provider.dart';
import '../../products/providers/product_provider.dart';
import '../../../shared/printing/qr_code_dialog.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/utils/country_config.dart';
import '../../settings/providers/company_settings_provider.dart';
import '../providers/purchase_provider.dart';

final class _CartItem {
  final String productId;
  final String productName;
  final double stock;
  String quantity;
  double unitCost;
  String discount;

  _CartItem({
    required this.productId, required this.productName, required this.stock,
    required this.quantity, required this.unitCost, required this.discount,
  });

  double get subtotal => (double.tryParse(quantity) ?? 0) * unitCost - (double.tryParse(discount) ?? 0);
}

final class PurchaseFormPage extends ConsumerStatefulWidget {
  const PurchaseFormPage({super.key});
  @override
  ConsumerState<PurchaseFormPage> createState() => _PurchaseFormPageState();
}

final class _PurchaseFormPageState extends ConsumerState<PurchaseFormPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';
  String? _selectedSupplier;
  final _invoiceRefCtrl = TextEditingController();
  final _discountCtrl = TextEditingController(text: '0');
  final _notesCtrl = TextEditingController();
  final _dateCtrl = TextEditingController(text: DateTime.now().toIso8601String().substring(0, 10));
  final List<_CartItem> _cart = [];
  bool _saving = false;
  bool _analyzing = false;
  String _currencyCode = 'NIO';

  static final _currencies = CountryConfig.countries.values.map((c) => c.currency).toSet().toList();

  @override
  void initState() {
    super.initState();
    Future.microtask(() {
      ref.read(supplierProvider.notifier).load();
      ref.read(productProvider.notifier).load();
    });
  }

  Future<void> _analyzeWithAi() async {
    final result = await FilePicker.pickFiles(type: FileType.image);
    if (result == null || result.files.single.path == null) return;

    setState(() => _analyzing = true);
    try {
      final dio = ref.read(dioClientProvider);
      final file = await MultipartFile.fromFile(result.files.single.path!);
      final formData = FormData.fromMap({'file': file});

      final response = await dio.post('purchases/analyze', data: formData);
      final data = response.data;

      setState(() {
        _invoiceRefCtrl.text = data['invoiceReference'] ?? '';
        _dateCtrl.text = (data['purchaseDate'] as String).substring(0, 10);
        _currencyCode = data['currencyCode'] ?? 'NIO';
        _notesCtrl.text = data['notes'] ?? '';
        _cart.clear();
        for (final d in (data['details'] as List)) {
          _scanProduct(d['productName']); // Simplification for demo
          if (_cart.isNotEmpty) {
            _cart.last.quantity = d['quantity'].toString();
            _cart.last.unitCost = (d['unitCost'] as num).toDouble();
          }
        }
      });
    } catch (e) {
      _err('Error al analizar factura con IA');
    } finally {
      if (mounted) setState(() => _analyzing = false);
    }
  }

  void _scanProduct(String code) {
    final products = ref.read(productProvider).items;
    final found = products.where((p) => p.code == code || p.id == code).toList();
    if (found.isEmpty) {
      if (mounted) _err('Producto no encontrado: $code');
      return;
    }
    for (final p in found) {
      final existing = _cart.indexWhere((c) => c.productId == p.id);
      if (existing >= 0) {
        setState(() {
          final qty = (double.tryParse(_cart[existing].quantity) ?? 0) + 1;
          _cart[existing].quantity = qty.toStringAsFixed(0);
        });
      } else {
        setState(() {
          _cart.add(_CartItem(
            productId: p.id, productName: p.name, stock: p.stock,
            quantity: '1', unitCost: p.cost ?? 0, discount: '0',
          ));
        });
      }
    }
  }

  void _err(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg), backgroundColor: Colors.red));
  }

  Future<void> _save() async {
    if (_selectedSupplier == null) { _err('Seleccione un proveedor'); return; }
    if (_cart.isEmpty) { _err('Agregue al menos un producto'); return; }

    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'supplierId': _selectedSupplier,
        'purchaseDate': _dateCtrl.text.isNotEmpty ? '${_dateCtrl.text}T00:00:00Z' : null,
        'invoiceReference': _invoiceRefCtrl.text.isNotEmpty ? _invoiceRefCtrl.text : null,
        'discount': double.tryParse(_discountCtrl.text) ?? 0,
        'notes': _notesCtrl.text.isNotEmpty ? _notesCtrl.text : null,
        'branchId': '00000000-0000-0000-0000-000000000000',
        'currencyCode': _currencyCode,
        'exchangeRateToReporting': _currencyCode == 'NIO' ? null : CountryConfig.exchangeRateToNIO(_currencyCode),
        'details': _cart.map((c) => {
          'productId': c.productId,
          'quantity': int.tryParse(c.quantity) ?? 0,
          'unitPrice': c.unitCost,
          'discount': double.tryParse(c.discount) ?? 0,
          'subtotal': c.subtotal,
        }).toList(),
      };
      await dio.post('purchases', data: body);
      ref.invalidate(purchaseProvider);
      if (mounted) context.go('/purchases');
    } catch (e) {
      _err('Error al guardar compra');
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  void dispose() {
    _searchCtrl.dispose(); _invoiceRefCtrl.dispose();
    _discountCtrl.dispose(); _notesCtrl.dispose(); _dateCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final suppliers = ref.watch(supplierProvider);
    final products = ref.watch(productProvider);
    final companySettingsAsync = ref.watch(companySettingsProvider);
    final taxRate = (companySettingsAsync.asData?.value['taxRate'] as num?)?.toDouble() ?? 0.15;
    final filteredProducts = _searchQuery.isEmpty
        ? products.items
        : products.items.where((p) =>
            p.name.toLowerCase().contains(_searchQuery.toLowerCase()) ||
            p.code.toLowerCase().contains(_searchQuery.toLowerCase())
          ).take(10).toList();

    final subtotal = _cart.fold<double>(0, (sum, c) => sum + c.subtotal);
    final discount = double.tryParse(_discountCtrl.text) ?? 0;
    final taxable = subtotal - discount;
    final tax = taxable * taxRate;
    final total = taxable + tax;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Nueva Compra'),
        actions: [
          IconButton(
            icon: _analyzing ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white)) : const Icon(Icons.auto_awesome),
            tooltip: 'Analizar factura con IA',
            onPressed: _analyzing ? null : _analyzeWithAi,
          ),
          const SizedBox(width: 16),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          ZCard(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Proveedor', style: TextStyle(fontWeight: FontWeight.w600)),
                const SizedBox(height: 8),
                ZDropdownFormField<String>(
                  value: _selectedSupplier,
                  label: 'Proveedor',
                  prefixIcon: Icons.person_outline,
                  items: suppliers.items
                      .where((c) => c.id.isNotEmpty)
                      .map((c) => DropdownMenuItem(value: c.id, child: Text(c.name)))
                      .toList(),
                  onChanged: (v) => setState(() => _selectedSupplier = v),
                ),
                const SizedBox(height: 12),
                ZDropdownFormField<String>(
                  value: _currencyCode,
                  label: 'Moneda',
                  prefixIcon: Icons.attach_money,
                  items: _currencies.map((c) => DropdownMenuItem(value: c, child: Text(c))).toList(),
                  onChanged: (v) => setState(() => _currencyCode = v ?? 'NIO'),
                ),
                const SizedBox(height: 12),
                ZTextField(
                  controller: _dateCtrl,
                  label: 'Fecha',
                ),
                const SizedBox(height: 12),
                ZTextField(
                  controller: _invoiceRefCtrl,
                  label: 'Referencia de factura',
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
                Text('Productos (${_cart.length})', style: TextStyle(fontWeight: FontWeight.w600, color: theme.colorScheme.primary)),
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
                  if (_searchQuery.isNotEmpty)
                    ...filteredProducts.map((p) => ListTile(
                      dense: true,
                      title: Text('${p.name} (${p.code})'),
                      subtitle: Text('Costo: \$${p.cost?.toStringAsFixed(2) ?? "N/A"} · Stock: ${p.stock.toStringAsFixed(0)}'),
                      trailing: IconButton(
                        icon: const Icon(Icons.add_shopping_cart, size: 20),
                        onPressed: () {
                          setState(() {
                            _cart.add(_CartItem(
                              productId: p.id, productName: p.name, stock: p.stock,
                              quantity: '1', unitCost: p.cost ?? 0, discount: '0',
                            ));
                            _searchCtrl.clear();
                            _searchQuery = '';
                          });
                        },
                      ),
                    )),
                  const SizedBox(height: 8),
                  ..._cart.asMap().entries.map((e) {
                    final i = e.value;
                    return ZCard(
                      margin: const EdgeInsets.only(bottom: 8),
                      padding: const EdgeInsets.all(12),
                      child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Expanded(child: Text(i.productName, style: const TextStyle(fontWeight: FontWeight.w600))),
                                IconButton(
                                  icon: const Icon(Icons.delete_outline, size: 18, color: Colors.red),
                                  onPressed: () => setState(() => _cart.removeAt(e.key)),
                                ),
                              ],
                            ),
                            const SizedBox(height: 8),
                            Row(
                              children: [
                                Expanded(
                                  child: TextField(
                                    decoration: const InputDecoration(labelText: 'Cantidad', border: OutlineInputBorder(), contentPadding: EdgeInsets.symmetric(horizontal: 8, vertical: 4)),
                                    keyboardType: TextInputType.number,
                                    controller: TextEditingController(text: i.quantity),
                                    onChanged: (v) => setState(() => i.quantity = v),
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: TextField(
                                    decoration: const InputDecoration(labelText: 'Costo', border: OutlineInputBorder(), contentPadding: EdgeInsets.symmetric(horizontal: 8, vertical: 4)),
                                    keyboardType: TextInputType.number,
                                    controller: TextEditingController(text: i.unitCost.toStringAsFixed(2)),
                                    onChanged: (v) => setState(() => i.unitCost = double.tryParse(v) ?? 0),
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: TextField(
                                    decoration: const InputDecoration(labelText: 'Dto', border: OutlineInputBorder(), contentPadding: EdgeInsets.symmetric(horizontal: 8, vertical: 4)),
                                    keyboardType: TextInputType.number,
                                    controller: TextEditingController(text: i.discount),
                                    onChanged: (v) => setState(() => i.discount = v),
                                  ),
                                ),
                              ],
                            ),
                            Text('Subtotal: \$${i.subtotal.toStringAsFixed(2)}', style: TextStyle(color: theme.colorScheme.primary, fontWeight: FontWeight.w500)),
                        ],
                      ),
                    );
                  }),
                ],
              ),
            ),
            const SizedBox(height: 12),
          ZCard(
            padding: const EdgeInsets.all(16),
            child: Column(
                children: [
                  _row('Subtotal', subtotal, theme),
                  TextField(
                    controller: _discountCtrl,
                    decoration: const InputDecoration(labelText: 'Descuento global', border: OutlineInputBorder()),
                    keyboardType: TextInputType.number,
                    onChanged: (_) => setState(() {}),
                  ),
                  const SizedBox(height: 4),
                  _row('IVA (15%)', tax, theme),
                  _row('Total', total, theme, bold: true),
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
            text: 'Guardar Compra',
            onPressed: _save,
            isLoading: _saving,
          ),
          const SizedBox(height: 40),
        ],
      ),
    );
  }

  Widget _row(String label, double amount, ThemeData theme, {bool bold = false}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
          Text('\$${amount.toStringAsFixed(2)}', style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.normal, color: bold ? theme.colorScheme.primary : null)),
        ],
      ),
    );
  }
}
