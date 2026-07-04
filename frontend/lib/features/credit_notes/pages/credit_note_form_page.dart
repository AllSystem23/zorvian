import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

final class _ReturnItem {
  final String productId;
  final String productName;
  final double unitPrice;
  int quantity;
  int maxQuantity;
  _ReturnItem({required this.productId, required this.productName, required this.unitPrice, required this.quantity, required this.maxQuantity});
}

final class CreditNoteFormPage extends ConsumerStatefulWidget {
  final String saleId;
  const CreditNoteFormPage({super.key, required this.saleId});
  @override
  ConsumerState<CreditNoteFormPage> createState() => _CreditNoteFormPageState();
}

final class _CreditNoteFormPageState extends ConsumerState<CreditNoteFormPage> {
  final _reasonCtrl = TextEditingController();
  List<_ReturnItem> _items = [];
  bool _loading = true;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadSale();
  }

  Future<void> _loadSale() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('sales/${widget.saleId}');
      final data = r.data as Map<String, dynamic>;
      final details = (data['details'] as List?) ?? [];
      setState(() {
        _items = details.map((d) {
          final j = d as Map<String, dynamic>;
          return _ReturnItem(
            productId: j['productId'] as String? ?? '',
            productName: j['productName'] as String? ?? '',
            unitPrice: (j['unitPrice'] as num?)?.toDouble() ?? 0,
            quantity: j['quantity'] as int? ?? 0,
            maxQuantity: j['quantity'] as int? ?? 0,
          );
        }).toList();
        _loading = false;
      });
    } catch (_) {
      setState(() { _error = 'Error al cargar venta'; _loading = false; });
    }
  }

  Future<void> _save() async {
    if (_reasonCtrl.text.trim().isEmpty) {
      setState(() => _error = 'Ingrese un motivo');
      return;
    }
    final selectedItems = _items.where((i) => i.quantity > 0).toList();
    if (selectedItems.isEmpty) {
      setState(() => _error = 'Seleccione al menos un producto');
      return;
    }
    setState(() { _saving = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('credit-notes', data: {
        'saleId': widget.saleId,
        'reason': _reasonCtrl.text.trim(),
        'details': selectedItems.map((i) => {
          'productId': i.productId,
          'quantity': i.quantity,
          'unitPrice': i.unitPrice,
        }).toList(),
      }, options: Options(extra: {'suppressGlobalError': true}));
      if (mounted) context.pop(true);
    } catch (e) {
      var msg = 'Error al crear nota de crédito';
      if (e is DioException && e.response?.data is Map && e.response?.data['redirectTo'] != null && mounted) {
        context.go('/onboarding');
        return;
      }
      setState(() => _error = msg);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  void dispose() {
    _reasonCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Nueva Nota de Crédito')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null && _items.isEmpty
              ? Center(child: Text(_error!))
              : SingleChildScrollView(
                  padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      if (_error != null) Container(
                        padding: const EdgeInsets.all(12),
                        margin: const EdgeInsets.only(bottom: 16),
                        decoration: BoxDecoration(color: theme.colorScheme.errorContainer, borderRadius: BorderRadius.circular(8)),
                        child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
                      ),
                      Text('Productos a devolver', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                      const SizedBox(height: 8),
                      ..._items.map((item) => ZCard(
                        padding: const EdgeInsets.all(12),
                        child: Row(
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(item.productName, style: const TextStyle(fontWeight: FontWeight.w600)),
                                    Text('\$${item.unitPrice.toStringAsFixed(2)} c/u', style: const TextStyle(color: Colors.grey, fontSize: 12)),
                                  ],
                                ),
                              ),
                              SizedBox(
                                width: 80,
                                child: TextFormField(
                                  initialValue: item.maxQuantity.toString(),
                                  keyboardType: TextInputType.number,
                                  decoration: const InputDecoration(labelText: 'Cant.', isDense: true, contentPadding: EdgeInsets.symmetric(vertical: 8, horizontal: 8)),
                                  onChanged: (v) => item.quantity = (int.tryParse(v) ?? 0).clamp(0, item.maxQuantity),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _reasonCtrl,
                        decoration: const InputDecoration(labelText: 'Motivo de la devolución', prefixIcon: Icon(Icons.description)),
                        maxLines: 3,
                      ),
                      const SizedBox(height: 24),
                      ZButton(
                        text: 'Crear Nota de Crédito',
                        onPressed: _save,
                        isLoading: _saving,
                      ),
                    ],
                  ),
                ),
    );
  }
}