import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../../clients/providers/client_provider.dart';
import '../../products/providers/product_provider.dart';
import '../../../shared/printing/qr_code_dialog.dart';

final class WarrantyFormPage extends ConsumerStatefulWidget {
  final String? warrantyId;
  const WarrantyFormPage({super.key, this.warrantyId});
  @override
  ConsumerState<WarrantyFormPage> createState() => _WarrantyFormPageState();
}

final class _WarrantyFormPageState extends ConsumerState<WarrantyFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _serialCtrl = TextEditingController();
  final _imeiCtrl = TextEditingController();
  final _lotCtrl = TextEditingController();
  final _termsCtrl = TextEditingController();
  final _clientSearchCtrl = TextEditingController();
  final _productSearchCtrl = TextEditingController();

  ClientItem? _selectedClient;
  ProductItem? _selectedProduct;
  int _durationMonths = 12;
  DateTime _startDate = DateTime.now();
  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.warrantyId != null;

  @override
  void initState() {
    super.initState();
    Future.microtask(() {
      ref.read(clientProvider.notifier).load();
      ref.read(productProvider.notifier).load();
    });
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('warranties/${widget.warrantyId}');
      final d = r.data;
      _serialCtrl.text = d['serialNumber'] ?? '';
      _imeiCtrl.text = d['imei'] ?? '';
      _lotCtrl.text = d['lotNumber'] ?? '';
      _termsCtrl.text = d['terms'] ?? '';
      _durationMonths = d['durationMonths'] ?? 12;
      if (d['startDate'] != null) {
        _startDate = DateTime.parse(d['startDate']);
      }
      final clientId = d['clientId'] as String?;
      final productId = d['productId'] as String?;
      if (clientId != null) {
        final clients = ref.read(clientProvider).items;
        _selectedClient = clients.where((c) => c.id == clientId).firstOrNull;
        if (_selectedClient != null) _clientSearchCtrl.text = _selectedClient!.fullName;
      }
      if (productId != null) {
        final products = ref.read(productProvider).items;
        _selectedProduct = products.where((p) => p.id == productId).firstOrNull;
        if (_selectedProduct != null) _productSearchCtrl.text = _selectedProduct!.name;
      }
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar garantía');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedClient == null || _selectedProduct == null) {
      setState(() => _error = 'Debe seleccionar cliente y producto');
      return;
    }
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'clientId': _selectedClient!.id,
        'productId': _selectedProduct!.id,
        'startDate': _startDate.toIso8601String().substring(0, 10),
        'durationMonths': _durationMonths,
        'terms': _termsCtrl.text.trim().isEmpty ? null : _termsCtrl.text.trim(),
        'serialNumber': _serialCtrl.text.trim().isEmpty ? null : _serialCtrl.text.trim(),
        'imei': _imeiCtrl.text.trim().isEmpty ? null : _imeiCtrl.text.trim(),
        'lotNumber': _lotCtrl.text.trim().isEmpty ? null : _lotCtrl.text.trim(),
      };
      if (_isEditing) {
        await dio.put('warranties/${widget.warrantyId}', data: body);
      } else {
        await dio.post('warranties', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar garantía');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _serialCtrl.dispose(); _imeiCtrl.dispose(); _lotCtrl.dispose();
    _termsCtrl.dispose(); _clientSearchCtrl.dispose(); _productSearchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final clientsState = ref.watch(clientProvider);
    final productsState = ref.watch(productProvider);

    final filteredClients = _clientSearchCtrl.text.isEmpty
        ? clientsState.items
        : clientsState.items.where((c) =>
            c.fullName.toLowerCase().contains(_clientSearchCtrl.text.toLowerCase()) ||
            c.code.toLowerCase().contains(_clientSearchCtrl.text.toLowerCase())
          ).toList();

    final filteredProducts = _productSearchCtrl.text.isEmpty
        ? productsState.items
        : productsState.items.where((p) =>
            p.name.toLowerCase().contains(_productSearchCtrl.text.toLowerCase()) ||
            p.code.toLowerCase().contains(_productSearchCtrl.text.toLowerCase()) ||
            (p.barcode != null && p.barcode!.toLowerCase().contains(_productSearchCtrl.text.toLowerCase()))
          ).toList();

    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar garantía' : 'Nueva garantía')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null) Container(
                padding: const EdgeInsets.all(ZSpacing.md),
                margin: const EdgeInsets.only(bottom: ZSpacing.md),
                decoration: BoxDecoration(color: ZColors.danger.withAlpha(30), borderRadius: BorderRadius.circular(8)),
                child: Text(_error!, style: const TextStyle(color: ZColors.danger)),
              ),

              // ── Cliente ──
              Text('Cliente', style: ZTypography.titleSmall.copyWith(color: ZColors.neutral700)),
              const SizedBox(height: ZSpacing.xs),
              TextFormField(
                controller: _clientSearchCtrl,
                decoration: InputDecoration(
                  hintText: 'Buscar por nombre o código...',
                  prefixIcon: const Icon(Icons.person_search),
                  suffixIcon: _selectedClient != null
                      ? IconButton(icon: const Icon(Icons.clear), onPressed: () {
                          setState(() { _selectedClient = null; _clientSearchCtrl.clear(); });
                        })
                      : null,
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                  isDense: true,
                ),
                onChanged: (_) => setState(() {}),
                validator: (_) => _selectedClient == null ? 'Seleccione un cliente' : null,
              ),
              if (_selectedClient == null && _clientSearchCtrl.text.isNotEmpty && filteredClients.isNotEmpty)
                Container(
                  constraints: const BoxConstraints(maxHeight: 200),
                  margin: const EdgeInsets.only(top: 4),
                  decoration: BoxDecoration(
                    border: Border.all(color: ZColors.neutral300),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: ListView.separated(
                    shrinkWrap: true,
                    itemCount: filteredClients.take(8).length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (_, i) {
                      final c = filteredClients[i];
                      return ListTile(
                        dense: true,
                        title: Text(c.fullName, style: const TextStyle(fontWeight: FontWeight.w600)),
                        subtitle: Text('${c.code} · ${c.phone ?? "Sin teléfono"}'),
                        onTap: () => setState(() {
                          _selectedClient = c;
                          _clientSearchCtrl.text = c.fullName;
                        }),
                      );
                    },
                  ),
                ),
              if (_selectedClient != null)
                Container(
                  margin: const EdgeInsets.only(top: 8),
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: ZColors.brandPrimary.withAlpha(15),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Row(children: [
                    const Icon(Icons.person, size: 18, color: ZColors.brandPrimary),
                    const SizedBox(width: 8),
                    Expanded(child: Text('${_selectedClient!.fullName} (${_selectedClient!.code})', style: const TextStyle(fontWeight: FontWeight.w600))),
                  ]),
                ),

              const SizedBox(height: ZSpacing.lg),

              // ── Producto ──
              Text('Producto', style: ZTypography.titleSmall.copyWith(color: ZColors.neutral700)),
              const SizedBox(height: ZSpacing.xs),
              TextFormField(
                controller: _productSearchCtrl,
                decoration: InputDecoration(
                  hintText: 'Buscar por nombre, código o escanear...',
                  prefixIcon: const Icon(Icons.inventory_2),
                  suffixIcon: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      if (_selectedProduct != null)
                        IconButton(icon: const Icon(Icons.clear), onPressed: () {
                          setState(() { _selectedProduct = null; _productSearchCtrl.clear(); });
                        }),
                      IconButton(
                        icon: const Icon(Icons.qr_code_scanner),
                        tooltip: 'Escanear código de barras',
                        onPressed: () => showScannerDialog(context, onScan: (code) {
                          final product = findProductByScan(ref.read(productProvider).items, code);
                          if (product != null) {
                            setState(() {
                              _selectedProduct = product;
                              _productSearchCtrl.text = product.name;
                            });
                          } else {
                            ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(content: Text('Producto no encontrado: $code'), backgroundColor: Colors.red),
                            );
                          }
                        }),
                      ),
                    ],
                  ),
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                  isDense: true,
                ),
                onChanged: (_) => setState(() {}),
                validator: (_) => _selectedProduct == null ? 'Seleccione un producto' : null,
              ),
              if (_selectedProduct == null && _productSearchCtrl.text.isNotEmpty && filteredProducts.isNotEmpty)
                Container(
                  constraints: const BoxConstraints(maxHeight: 200),
                  margin: const EdgeInsets.only(top: 4),
                  decoration: BoxDecoration(
                    border: Border.all(color: ZColors.neutral300),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: ListView.separated(
                    shrinkWrap: true,
                    itemCount: filteredProducts.take(8).length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (_, i) {
                      final p = filteredProducts[i];
                      return ListTile(
                        dense: true,
                        title: Text(p.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                        subtitle: Text('${p.code} · Stock: ${p.stock.toStringAsFixed(0)} ${p.unit}'),
                        trailing: Text('C\$ ${p.price.toStringAsFixed(2)}', style: const TextStyle(fontWeight: FontWeight.bold)),
                        onTap: () => setState(() {
                          _selectedProduct = p;
                          _productSearchCtrl.text = p.name;
                        }),
                      );
                    },
                  ),
                ),
              if (_selectedProduct != null)
                Container(
                  margin: const EdgeInsets.only(top: 8),
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: ZColors.brandSecondary.withAlpha(15),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Row(children: [
                    const Icon(Icons.inventory_2, size: 18, color: ZColors.brandSecondary),
                    const SizedBox(width: 8),
                    Expanded(child: Text('${_selectedProduct!.name} (${_selectedProduct!.code})', style: const TextStyle(fontWeight: FontWeight.w600))),
                    Text('C\$ ${_selectedProduct!.price.toStringAsFixed(2)}', style: TextStyle(fontWeight: FontWeight.bold, color: ZColors.brandPrimary)),
                  ]),
                ),

              const SizedBox(height: ZSpacing.lg),

              // ── Fecha de inicio ──
              Text('Fecha de inicio', style: ZTypography.titleSmall.copyWith(color: ZColors.neutral700)),
              const SizedBox(height: ZSpacing.xs),
              InkWell(
                onTap: () async {
                  final picked = await showDatePicker(
                    context: context,
                    initialDate: _startDate,
                    firstDate: DateTime(2020),
                    lastDate: DateTime.now(),
                  );
                  if (picked != null) setState(() => _startDate = picked);
                },
                child: InputDecorator(
                  decoration: InputDecoration(
                    border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                    prefixIcon: const Icon(Icons.calendar_today),
                  ),
                  child: Text('${_startDate.day}/${_startDate.month}/${_startDate.year}'),
                ),
              ),

              const SizedBox(height: ZSpacing.lg),

              // ── Identificación del producto ──
              Text('Identificación del producto', style: ZTypography.titleSmall.copyWith(color: ZColors.neutral700)),
              const SizedBox(height: ZSpacing.xs),
              ZTextField(controller: _serialCtrl, label: 'Número de Serie'),
              const SizedBox(height: ZSpacing.md),
              ZTextField(controller: _imeiCtrl, label: 'IMEI'),
              const SizedBox(height: ZSpacing.md),
              ZTextField(controller: _lotCtrl, label: 'Lote'),

              const SizedBox(height: ZSpacing.lg),

              // ── Duración ──
              Text('Duración', style: ZTypography.titleSmall.copyWith(color: ZColors.neutral700)),
              const SizedBox(height: ZSpacing.xs),
              DropdownButtonFormField<int>(
                initialValue: _durationMonths,
                decoration: InputDecoration(
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                  prefixIcon: const Icon(Icons.timer),
                ),
                items: [3, 6, 12, 24, 36].map((m) => DropdownMenuItem(value: m, child: Text('$m meses'))).toList(),
                onChanged: (v) => setState(() => _durationMonths = v ?? 12),
              ),

              const SizedBox(height: ZSpacing.lg),

              // ── Términos ──
              Text('Términos y condiciones', style: ZTypography.titleSmall.copyWith(color: ZColors.neutral700)),
              const SizedBox(height: ZSpacing.xs),
              ZTextField(controller: _termsCtrl, label: 'Términos', maxLines: 3),

              const SizedBox(height: ZSpacing.xl),

              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear garantía',
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
