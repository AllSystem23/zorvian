import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../../../shared/printing/qr_code_dialog.dart';

final class ProductFormPage extends ConsumerStatefulWidget {
  final String? productId;
  const ProductFormPage({super.key, this.productId});
  @override
  ConsumerState<ProductFormPage> createState() => _ProductFormPageState();
}

final class _ProductFormPageState extends ConsumerState<ProductFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _codeCtrl = TextEditingController();
  final _nameCtrl = TextEditingController();
  final _descCtrl = TextEditingController();
  final _barcodeCtrl = TextEditingController();
  final _priceCtrl = TextEditingController();
  final _costCtrl = TextEditingController();
  final _stockCtrl = TextEditingController();
  final _minStockCtrl = TextEditingController();
  final _maxStockCtrl = TextEditingController();
  final _unitCtrl = TextEditingController(text: 'pz');
  String _categoryId = '';
  String _brandId = '';
  bool _isActive = true;
  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.productId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('products/${widget.productId}');
      final d = r.data;
      _codeCtrl.text = d['code'] ?? '';
      _nameCtrl.text = d['name'] ?? '';
      _descCtrl.text = d['description'] ?? '';
      _barcodeCtrl.text = d['barcode'] ?? '';
      _priceCtrl.text = (d['sellingPrice'] ?? d['price'] ?? 0).toString();
      _costCtrl.text = (d['costPrice'] ?? d['cost'] ?? 0).toString();
      _stockCtrl.text = (d['stock'] ?? 0).toString();
      _minStockCtrl.text = (d['minStock'] ?? 0).toString();
      _maxStockCtrl.text = (d['maxStock'] ?? 0).toString();
      _unitCtrl.text = d['unitOfMeasure'] ?? d['unit'] ?? 'pz';
      _categoryId = d['categoryId'] ?? '';
      _brandId = d['brandId'] ?? '';
      _isActive = d['isActive'] ?? true;
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar producto');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'code': _codeCtrl.text.trim(),
        'name': _nameCtrl.text.trim(),
        'description': _descCtrl.text.trim().isEmpty ? null : _descCtrl.text.trim(),
        'barcode': _barcodeCtrl.text.trim().isEmpty ? null : _barcodeCtrl.text.trim(),
        'price': double.tryParse(_priceCtrl.text) ?? 0,
        'cost': double.tryParse(_costCtrl.text),
        'stock': double.tryParse(_stockCtrl.text) ?? 0,
        'minStock': double.tryParse(_minStockCtrl.text) ?? 0,
        'maxStock': double.tryParse(_maxStockCtrl.text) ?? 0,
        'unitOfMeasure': _unitCtrl.text.trim(),
        'categoryId': _categoryId.isEmpty ? null : _categoryId,
        'brandId': _brandId.isEmpty ? null : _brandId,
        'isActive': _isActive,
      };
      if (_isEditing) {
        await dio.put('products/${widget.productId}', data: body);
      } else {
        await dio.post('products', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar producto');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _codeCtrl.dispose(); _nameCtrl.dispose(); _descCtrl.dispose();
    _barcodeCtrl.dispose();
    _priceCtrl.dispose(); _costCtrl.dispose(); _stockCtrl.dispose();
    _minStockCtrl.dispose(); _maxStockCtrl.dispose(); _unitCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar producto' : 'Nuevo producto')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null) Container(
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(color: theme.colorScheme.errorContainer, borderRadius: BorderRadius.circular(8)),
                child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
              ),
              TextFormField(controller: _codeCtrl, decoration: const InputDecoration(labelText: 'Código', prefixIcon: Icon(Icons.tag)), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _nameCtrl, decoration: const InputDecoration(labelText: 'Nombre', prefixIcon: Icon(Icons.inventory_2)), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _descCtrl, decoration: const InputDecoration(labelText: 'Descripción', prefixIcon: Icon(Icons.description)), maxLines: 2),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(
                  child: TextFormField(
                    controller: _barcodeCtrl,
                    decoration: const InputDecoration(
                      labelText: 'Código de barras',
                      prefixIcon: Icon(Icons.qr_code),
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                IconButton(
                  icon: const Icon(Icons.qr_code_scanner),
                  tooltip: 'Escanear código de barras',
                  onPressed: () => showScannerDialog(context, onScan: (code) {
                    setState(() => _barcodeCtrl.text = code);
                  }),
                ),
              ]),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(child: TextFormField(controller: _priceCtrl, decoration: const InputDecoration(labelText: 'Precio', prefixIcon: Icon(Icons.attach_money)), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null)),
                const SizedBox(width: 12),
                Expanded(child: TextFormField(controller: _costCtrl, decoration: const InputDecoration(labelText: 'Costo', prefixIcon: Icon(Icons.money_off)), keyboardType: TextInputType.number)),
              ]),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(child: TextFormField(controller: _stockCtrl, decoration: const InputDecoration(labelText: 'Stock inicial', prefixIcon: Icon(Icons.inventory)), keyboardType: TextInputType.number)),
                const SizedBox(width: 12),
                Expanded(child: TextFormField(controller: _minStockCtrl, decoration: const InputDecoration(labelText: 'Stock mínimo', prefixIcon: Icon(Icons.warning)), keyboardType: TextInputType.number)),
              ]),
              const SizedBox(height: 12),
              TextFormField(controller: _maxStockCtrl, decoration: const InputDecoration(labelText: 'Stock máximo', prefixIcon: Icon(Icons.trending_up)), keyboardType: TextInputType.number),
              const SizedBox(height: 12),
              TextFormField(controller: _unitCtrl, decoration: const InputDecoration(labelText: 'Unidad', prefixIcon: Icon(Icons.straighten))),
              const SizedBox(height: 12),
              SwitchListTile(title: const Text('Activo'), value: _isActive, onChanged: (v) => setState(() => _isActive = v)),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear producto',
                onPressed: _save,
                isLoading: _loading,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
