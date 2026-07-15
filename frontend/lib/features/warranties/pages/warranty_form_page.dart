import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/utils/formatters.dart';
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
  bool _initialLoading = false;
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
    setState(() => _initialLoading = true);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('warranties/${widget.warrantyId}');
      final d = r.data as Map<String, dynamic>;
      _serialCtrl.text = d['serialNumber'] ?? '';
      _imeiCtrl.text = d['imei'] ?? '';
      _lotCtrl.text = d['lotNumber'] ?? '';
      _termsCtrl.text = d['terms'] ?? '';
      _durationMonths = d['durationMonths'] ?? 12;
      if (d['startDate'] != null) _startDate = DateTime.parse(d['startDate']);
      final clientId = d['clientId'] as String?;
      final productId = d['productId'] as String?;
      if (clientId != null) {
        _selectedClient = ref.read(clientProvider).items.where((c) => c.id == clientId).firstOrNull;
        if (_selectedClient != null) _clientSearchCtrl.text = _selectedClient!.fullName;
      }
      if (productId != null) {
        _selectedProduct = ref.read(productProvider).items.where((p) => p.id == productId).firstOrNull;
        if (_selectedProduct != null) _productSearchCtrl.text = _selectedProduct!.name;
      }
    } catch (e) {
      setState(() => _error = 'Error al cargar garantía');
    } finally {
      if (mounted) setState(() => _initialLoading = false);
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
    } catch (e) {
      String msg = 'Error al guardar garantía';
      if (e.toString().contains('422')) msg = 'Datos inválidos. Verifique la información.';
      if (e.toString().contains('401')) msg = 'Sesión expirada. Ingrese nuevamente.';
      setState(() => _error = msg);
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
    final screenWidth = MediaQuery.of(context).size.width;
    final isDesktop = screenWidth > 992;

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

    if (_initialLoading) {
      return Scaffold(
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null) ...[
                ZAlertCard(message: _error!, severity: 'high'),
                const SizedBox(height: ZSpacing.md),
              ],

              if (isDesktop)
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(child: _buildClientSection(filteredClients, clientsState.loading)),
                    const SizedBox(width: ZSpacing.lg),
                    Expanded(child: _buildProductSection(filteredProducts, productsState.loading)),
                  ],
                )
              else ...[
                _buildClientSection(filteredClients, clientsState.loading),
                const SizedBox(height: ZSpacing.lg),
                _buildProductSection(filteredProducts, productsState.loading),
              ],

              const SizedBox(height: ZSpacing.lg),
              _buildDateSection(),
              const SizedBox(height: ZSpacing.lg),
              _buildIdentificationSection(),
              const SizedBox(height: ZSpacing.lg),
              _buildDurationSection(),
              const SizedBox(height: ZSpacing.lg),
              _buildTermsSection(),
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

  Widget _buildClientSection(List<ClientItem> filtered, bool loading) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
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
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
            isDense: true,
          ),
          onChanged: (_) => setState(() {}),
          validator: (_) => _selectedClient == null ? 'Seleccione un cliente' : null,
        ),
        if (loading) const Padding(padding: EdgeInsets.only(top: 4), child: LinearProgressIndicator()),
        if (_selectedClient == null && _clientSearchCtrl.text.isNotEmpty && filtered.isNotEmpty)
          _buildDropdown(filtered.map((c) => _DropdownItem(
            title: c.fullName,
            subtitle: '${c.code} · ${c.phone ?? "Sin teléfono"}',
            onTap: () => setState(() { _selectedClient = c; _clientSearchCtrl.text = c.fullName; }),
          )).toList()),
        if (_selectedClient != null)
          _buildSelectedChip(
            icon: Icons.person,
            label: '${_selectedClient!.fullName} (${_selectedClient!.code})',
            color: ZColors.brandPrimary,
          ),
      ],
    );
  }

  Widget _buildProductSection(List<ProductItem> filtered, bool loading) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Producto', style: ZTypography.titleSmall.copyWith(color: ZColors.neutral700)),
        const SizedBox(height: ZSpacing.xs),
        TextFormField(
          controller: _productSearchCtrl,
          decoration: InputDecoration(
            hintText: 'Nombre, código o escanear...',
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
                  tooltip: 'Escanear código',
                  onPressed: () => showScannerDialog(context, onScan: (code) {
                    final product = findProductByScan(ref.read(productProvider).items, code);
                    if (product != null) {
                      setState(() { _selectedProduct = product; _productSearchCtrl.text = product.name; });
                    } else {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(content: Text('Producto no encontrado: $code'), backgroundColor: ZColors.danger),
                      );
                    }
                  }),
                ),
              ],
            ),
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
            isDense: true,
          ),
          onChanged: (_) => setState(() {}),
          validator: (_) => _selectedProduct == null ? 'Seleccione un producto' : null,
        ),
        if (loading) const Padding(padding: EdgeInsets.only(top: 4), child: LinearProgressIndicator()),
        if (_selectedProduct == null && _productSearchCtrl.text.isNotEmpty && filtered.isNotEmpty)
          _buildDropdown(filtered.map((p) => _DropdownItem(
            title: p.name,
            subtitle: '${p.code} · Stock: ${p.stock.toStringAsFixed(0)} ${p.unit}',
            trailing: 'C\$ ${p.price.toStringAsFixed(2)}',
            onTap: () => setState(() { _selectedProduct = p; _productSearchCtrl.text = p.name; }),
          )).toList()),
        if (_selectedProduct != null)
          _buildSelectedChip(
            icon: Icons.inventory_2,
            label: '${_selectedProduct!.name} (${_selectedProduct!.code})',
            color: ZColors.brandSecondary,
            trailing: 'C\$ ${_selectedProduct!.price.toStringAsFixed(2)}',
          ),
      ],
    );
  }

  Widget _buildDateSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
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
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
              prefixIcon: const Icon(Icons.calendar_today),
            ),
            child: Text(ZFormatters.date(_startDate)),
          ),
        ),
      ],
    );
  }

  Widget _buildIdentificationSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Identificación del producto', style: ZTypography.titleSmall.copyWith(color: ZColors.neutral700)),
        const SizedBox(height: ZSpacing.xs),
        ZTextField(controller: _serialCtrl, label: 'Número de Serie'),
        const SizedBox(height: ZSpacing.md),
        ZTextField(controller: _imeiCtrl, label: 'IMEI'),
        const SizedBox(height: ZSpacing.md),
        ZTextField(controller: _lotCtrl, label: 'Lote'),
      ],
    );
  }

  Widget _buildDurationSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Duración', style: ZTypography.titleSmall.copyWith(color: ZColors.neutral700)),
        const SizedBox(height: ZSpacing.xs),
        DropdownButtonFormField<int>(
          initialValue: _durationMonths,
          decoration: InputDecoration(
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
            prefixIcon: const Icon(Icons.timer),
          ),
          items: [3, 6, 12, 24, 36].map((m) => DropdownMenuItem(value: m, child: Text('$m meses'))).toList(),
          onChanged: (v) => setState(() => _durationMonths = v ?? 12),
        ),
      ],
    );
  }

  Widget _buildTermsSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Términos y condiciones', style: ZTypography.titleSmall.copyWith(color: ZColors.neutral700)),
        const SizedBox(height: ZSpacing.xs),
        ZTextField(controller: _termsCtrl, label: 'Términos', maxLines: 3),
      ],
    );
  }

  Widget _buildDropdown(List<_DropdownItem> items) {
    return Container(
      constraints: const BoxConstraints(maxHeight: 200),
      margin: const EdgeInsets.only(top: 4),
      decoration: BoxDecoration(
        border: Border.all(color: ZColors.neutral300),
        borderRadius: BorderRadius.circular(ZRadii.md),
      ),
      child: ListView.separated(
        shrinkWrap: true,
        itemCount: items.take(8).length,
        separatorBuilder: (_, _) => const Divider(height: 1),
        itemBuilder: (_, i) => items[i],
      ),
    );
  }

  Widget _buildSelectedChip({
    required IconData icon,
    required String label,
    required Color color,
    String? trailing,
  }) {
    return Container(
      margin: const EdgeInsets.only(top: 8),
      padding: const EdgeInsets.all(ZSpacing.md),
      decoration: BoxDecoration(
        color: color.withAlpha(15),
        borderRadius: BorderRadius.circular(ZRadii.md),
      ),
      child: Row(children: [
        Icon(icon, size: 18, color: color),
        const SizedBox(width: ZSpacing.sm),
        Expanded(child: Text(label, style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600))),
        if (trailing != null) Text(trailing, style: TextStyle(fontWeight: FontWeight.bold, color: color)),
      ]),
    );
  }
}

class _DropdownItem extends StatelessWidget {
  final String title;
  final String subtitle;
  final String? trailing;
  final VoidCallback onTap;

  const _DropdownItem({required this.title, required this.subtitle, this.trailing, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return ListTile(
      dense: true,
      title: Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
      subtitle: Text(subtitle, style: ZTypography.bodySmall),
      trailing: trailing != null ? Text(trailing!, style: const TextStyle(fontWeight: FontWeight.bold)) : null,
      onTap: onTap,
    );
  }
}
