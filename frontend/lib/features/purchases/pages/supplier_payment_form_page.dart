import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';

class SupplierPaymentFormPage extends ConsumerStatefulWidget {
  final String purchaseId;
  const SupplierPaymentFormPage({super.key, required this.purchaseId});

  @override
  ConsumerState<SupplierPaymentFormPage> createState() => _SupplierPaymentFormPageState();
}

class _SupplierPaymentFormPageState extends ConsumerState<SupplierPaymentFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _amountCtrl = TextEditingController();
  final _referenceCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  String _paymentMethod = 'cash';
  bool _loading = false;

  @override
  void dispose() {
    _amountCtrl.dispose();
    _referenceCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('supplier-payments', data: {
        'purchaseId': widget.purchaseId,
        'amount': double.tryParse(_amountCtrl.text) ?? 0,
        'paymentMethod': _paymentMethod,
        'referenceNumber': _referenceCtrl.text.trim(),
        'notes': _notesCtrl.text.trim(),
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Pago registrado exitosamente')));
        context.pop(true);
      }
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: ZCard(
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Datos del Pago', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                const SizedBox(height: 24),
                ZTextField(controller: _amountCtrl, label: 'Monto', keyboardType: TextInputType.number),
                const SizedBox(height: 16),
                ZDropdownFormField<String>(
                  value: _paymentMethod,
                  label: 'Método de Pago',
                  prefixIcon: Icons.payments_outlined,
                  items: const [
                    DropdownMenuItem(value: 'cash', child: Text('Efectivo')),
                    DropdownMenuItem(value: 'bank_transfer', child: Text('Transferencia Bancaria')),
                    DropdownMenuItem(value: 'check', child: Text('Cheque')),
                    DropdownMenuItem(value: 'credit_card', child: Text('Tarjeta de Crédito')),
                    DropdownMenuItem(value: 'retention', child: Text('Retención')),
                  ],
                  onChanged: (v) => setState(() => _paymentMethod = v ?? 'cash'),
                ),
                const SizedBox(height: 16),
                ZTextField(controller: _referenceCtrl, label: 'N° de Referencia'),
                const SizedBox(height: 16),
                ZTextField(controller: _notesCtrl, label: 'Notas', maxLines: 3),
                const SizedBox(height: 24),
                ZButton(text: 'Guardar Pago', icon: Icons.save, isLoading: _loading, onPressed: _submit),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
