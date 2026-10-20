import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../products/providers/product_provider.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';
import '../../../core/widgets/responsive_layout.dart';
import '../providers/purchase_request_provider.dart';

final class _CartLine {
  final String productId;
  final String productName;
  String quantity;
  double unitPrice;

  _CartLine({
    required this.productId, required this.productName,
    required this.quantity, required this.unitPrice,
  });

  double get subtotal => (double.tryParse(quantity) ?? 0) * unitPrice;
}

final class PurchaseRequestFormPage extends ConsumerStatefulWidget {
  const PurchaseRequestFormPage({super.key});
  @override
  ConsumerState<PurchaseRequestFormPage> createState() => _PurchaseRequestFormPageState();
}

final class _PurchaseRequestFormPageState extends ConsumerState<PurchaseRequestFormPage> {
  final _titleCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  final List<_CartLine> _cart = [];
  String _priority = 'normal';
  DateTime? _neededByDate;
  bool _saving = false;

  static const _priorities = <String, String>{
    'low': 'Baja',
    'normal': 'Normal',
    'high': 'Alta',
    'urgent': 'Urgente',
  };

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(productProvider.notifier).load());
  }

  @override
  void dispose() {
    _titleCtrl.dispose();
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
            _cart.add(_CartLine(productId: p.id, productName: p.name, quantity: '1', unitPrice: 0));
          });
          Navigator.pop(ctx);
        },
      ),
    );
  }

  void _err(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg), backgroundColor: Colors.red));
  }

  double get _total => _cart.fold(0, (s, l) => s + l.subtotal);

  Future<void> _save() async {
    if (_titleCtrl.text.trim().isEmpty) { _err('Ingrese un título para la solicitud'); return; }
    if (_cart.isEmpty) { _err('Agregue al menos un producto'); return; }
    final branchId = ref.read(companyBranchProvider).branchId;
    if (branchId == null) { _err('Seleccione una sucursal en el cabecero superior'); return; }

    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      final body = CreatePurchaseRequestRequest(
        title: _titleCtrl.text.trim(),
        priority: _priority,
        neededByDate: _neededByDate == null
            ? null
            : '${_neededByDate!.year.toString().padLeft(4, '0')}-'
                '${_neededByDate!.month.toString().padLeft(2, '0')}-'
                '${_neededByDate!.day.toString().padLeft(2, '0')}',
        branchId: branchId,
        notes: _notesCtrl.text.isEmpty ? null : _notesCtrl.text,
        details: _cart.map((l) => CreatePurchaseRequestLine(
          productId: l.productId,
          quantity: int.tryParse(l.quantity) ?? 0,
          estimatedUnitPrice: l.unitPrice,
        )).toList(),
      );
      await dio.post('purchase-requests', data: body.toJson());
      ref.invalidate(purchaseRequestProvider);
      if (mounted) context.pop();
    } catch (_) {
      _err('Error al crear la solicitud');
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final branchId = ref.watch(companyBranchProvider).branchId;
    final branches = ref.watch(headerBranchListProvider);

    return Scaffold(
      body: SingleChildScrollView(
        padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ZPageHeader(
              title: 'Nueva Solicitud de Cotización',
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
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  TextField(
                    controller: _titleCtrl,
                    decoration: InputDecoration(
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                      labelText: 'Título *',
                      hintText: 'Ej: Reposición de laptops',
                    ),
                  ),
                  const SizedBox(height: 12),
                  ZDropdownFormField<String>(
                    value: _priority,
                    label: 'Prioridad',
                    items: _priorities.entries
                        .map((e) => DropdownMenuItem(value: e.key, child: Text(e.value)))
                        .toList(),
                    onChanged: (v) => setState(() => _priority = v ?? 'normal'),
                  ),
                  const SizedBox(height: 12),
                  InkWell(
                    onTap: () async {
                      final date = await showDatePicker(
                        context: context,
                        initialDate: _neededByDate ?? DateTime.now(),
                        firstDate: DateTime.now(),
                        lastDate: DateTime.now().add(const Duration(days: 365)),
                      );
                      if (date != null) setState(() => _neededByDate = date);
                    },
                    child: InputDecorator(
                      decoration: InputDecoration(
                        labelText: 'Fecha requerida (opcional)',
                        prefixIcon: const Icon(Icons.date_range),
                        suffixIcon: _neededByDate != null
                            ? IconButton(
                                icon: const Icon(Icons.clear, size: 18),
                                onPressed: () => setState(() => _neededByDate = null),
                              )
                            : const Icon(Icons.event),
                        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                      ),
                      child: Text(_neededByDate == null
                          ? 'Sin fecha'
                          : '${_neededByDate!.day.toString().padLeft(2, '0')}/${_neededByDate!.month.toString().padLeft(2, '0')}/${_neededByDate!.year}'),
                    ),
                  ),
                  const SizedBox(height: 12),
                  branches.when(
                    data: (items) {
                      final name = branchId == null
                          ? null
                          : items.where((e) => e['id'] == branchId).map((e) => e['name'] as String? ?? '').firstOrNull;
                      return InputDecorator(
                        decoration: InputDecoration(
                          labelText: 'Sucursal (origen)',
                          prefixIcon: const Icon(Icons.business_outlined),
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                        ),
                        child: Text(name == null || name.isEmpty ? 'Sucursal actual' : name),
                      );
                    },
                    loading: () => const LinearProgressIndicator(minHeight: 2),
                    error: (_, _) => const SizedBox.shrink(),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _notesCtrl,
                    decoration: InputDecoration(
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                      labelText: 'Notas / Justificación',
                    ),
                    maxLines: 2,
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            // `Wrap`: a 360 px el botón no cabe al lado del título y baja a
            // la línea siguiente en vez de desbordar.
            Wrap(
              alignment: WrapAlignment.spaceBetween,
              crossAxisAlignment: WrapCrossAlignment.center,
              spacing: 12,
              runSpacing: 8,
              children: [
                Text('Productos (${_cart.length})', style: theme.textTheme.titleSmall),
                ZButton(text: 'Agregar producto', icon: Icons.add, onPressed: _addProduct, fullWidth: false),
              ],
            ),
            const SizedBox(height: 8),
            if (_cart.isEmpty)
              const ZEmptyState(icon: Icons.shopping_basket_outlined, title: 'No hay productos', subtitle: 'Agregue productos a cotizar')
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
                  const Text('Total estimado', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                  Expanded(
                    child: Text(
                      _total.toStringAsFixed(2),
                      textAlign: TextAlign.end,
                      style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                    ),
                  ),
                ],
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
          ResponsiveFormRow(
            spacing: 8,
            children: [
              TextField(
                decoration: const InputDecoration(labelText: 'Cantidad', isDense: true),
                controller: TextEditingController(text: line.quantity),
                keyboardType: TextInputType.number,
                onChanged: (v) => line.quantity = v,
              ),
              TextField(
                decoration: const InputDecoration(labelText: 'Precio U. estimado', isDense: true),
                controller: TextEditingController(text: line.unitPrice.toString()),
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                onChanged: (v) => line.unitPrice = double.tryParse(v) ?? 0,
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
