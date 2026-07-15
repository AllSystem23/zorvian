import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/supplier_payment_provider.dart';

final class SupplierPaymentFormPage extends ConsumerStatefulWidget {
  final String? purchaseId;
  const SupplierPaymentFormPage({super.key, this.purchaseId});

  @override
  ConsumerState<SupplierPaymentFormPage> createState() => _SupplierPaymentFormPageState();
}

final class _SupplierPaymentFormPageState extends ConsumerState<SupplierPaymentFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _amountCtrl = TextEditingController();
  final _referenceCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  String? _selectedPurchaseId;
  String _paymentMethod = 'transfer';
  bool _loading = false;
  String? _error;

  final List<Map<String, String>> _paymentMethods = const [
    {'value': 'cash', 'label': 'Efectivo'},
    {'value': 'transfer', 'label': 'Transferencia'},
    {'value': 'check', 'label': 'Cheque'},
    {'value': 'card', 'label': 'Tarjeta'},
  ];

  @override
  void initState() {
    super.initState();
    _selectedPurchaseId = widget.purchaseId;
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedPurchaseId == null) {
      setState(() => _error = 'Debe seleccionar una compra');
      return;
    }

    setState(() { _loading = true; _error = null; });
    try {
      await ref.read(supplierPaymentProvider.notifier).create(
        purchaseId: _selectedPurchaseId!,
        amount: double.parse(_amountCtrl.text),
        paymentMethod: _paymentMethod,
        referenceNumber: _referenceCtrl.text.trim().isEmpty ? null : _referenceCtrl.text.trim(),
        notes: _notesCtrl.text.trim().isEmpty ? null : _notesCtrl.text.trim(),
      );
      if (mounted) {
        ZToast.success(context, 'Pago registrado exitosamente');
        context.pop(true);
      }
    } catch (e) {
      setState(() => _error = 'Error al registrar pago: $e');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _amountCtrl.dispose();
    _referenceCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null)
                Container(
                  padding: const EdgeInsets.all(12),
                  margin: const EdgeInsets.only(bottom: 16),
                  decoration: BoxDecoration(
                    color: theme.colorScheme.errorContainer,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
                ),
              if (_selectedPurchaseId == null)
                _PurchaseSelector(
                  onSelected: (id) => setState(() => _selectedPurchaseId = id),
                ),
              if (_selectedPurchaseId != null) ...[
                ZCard(
                  child: Row(
                    children: [
                      const Icon(Icons.receipt_outlined, color: Colors.green),
                      const SizedBox(width: 12),
                      Expanded(child: Text('Compra: $_selectedPurchaseId', style: const TextStyle(fontWeight: FontWeight.w600))),
                      IconButton(
                        icon: const Icon(Icons.close, size: 18),
                        onPressed: () => setState(() => _selectedPurchaseId = null),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),
              ],
              TextFormField(
                controller: _amountCtrl,
                decoration: const InputDecoration(
                  labelText: 'Monto',
                  prefixIcon: Icon(Icons.attach_money),
                ),
                keyboardType: TextInputType.number,
                validator: (v) {
                  if (v == null || v.isEmpty) return 'Requerido';
                  if (double.tryParse(v) == null || double.parse(v) <= 0) return 'Ingrese un monto válido';
                  return null;
                },
              ),
              const SizedBox(height: 16),
              DropdownButtonFormField<String>(
                initialValue: _paymentMethod,
                decoration: const InputDecoration(
                  labelText: 'Método de Pago',
                  prefixIcon: Icon(Icons.payment_outlined),
                ),
                items: _paymentMethods.map((m) => DropdownMenuItem(
                  value: m['value'],
                  child: Text(m['label']!),
                )).toList(),
                onChanged: (v) => setState(() => _paymentMethod = v!),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _referenceCtrl,
                decoration: const InputDecoration(
                  labelText: 'Número de Referencia (opcional)',
                  prefixIcon: Icon(Icons.tag),
                ),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _notesCtrl,
                decoration: const InputDecoration(
                  labelText: 'Notas (opcional)',
                  prefixIcon: Icon(Icons.notes),
                ),
                maxLines: 2,
              ),
              const SizedBox(height: 24),
              ZButton(
                text: 'Registrar Pago',
                onPressed: _save,
                isLoading: _loading,
                icon: Icons.check,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

final class _PurchaseSelector extends ConsumerStatefulWidget {
  final ValueChanged<String> onSelected;
  const _PurchaseSelector({required this.onSelected});

  @override
  ConsumerState<_PurchaseSelector> createState() => _PurchaseSelectorState();
}

final class _PurchaseSelectorState extends ConsumerState<_PurchaseSelector> {
  List<dynamic> _purchases = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('purchases');
      _purchases = (r.data as List?) ?? [];
    } catch (_) {}
    setState(() => _loading = false);
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return const SizedBox(height: 80, child: Center(child: CircularProgressIndicator()));
    if (_purchases.isEmpty) return const SizedBox.shrink();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('Seleccionar Compra', style: TextStyle(fontWeight: FontWeight.w600)),
        const SizedBox(height: 8),
        ..._purchases.take(10).map((p) => ListTile(
          dense: true,
          leading: const Icon(Icons.receipt_outlined),
          title: Text(p['purchaseNumber'] ?? p['id'] ?? ''),
          subtitle: Text('\$${(p['total'] as num?)?.toStringAsFixed(2) ?? '0.00'} · ${p['supplierName'] ?? ''}'),
          trailing: Text(p['status'] ?? '', style: const TextStyle(fontSize: 12)),
          onTap: () => widget.onSelected(p['id'] as String),
        )),
      ],
    );
  }
}
