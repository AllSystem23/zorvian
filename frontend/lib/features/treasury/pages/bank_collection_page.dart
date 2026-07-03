import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/treasury_provider.dart';

class BankCollectionPage extends ConsumerStatefulWidget {
  const BankCollectionPage({super.key});

  @override
  ConsumerState<BankCollectionPage> createState() => _BankCollectionPageState();
}

class _BankCollectionPageState extends ConsumerState<BankCollectionPage> {
  final _formKey = GlobalKey<FormState>();
  
  String? _selectedInvoiceId;
  String? _selectedCostCenterId;
  final _paymentIdController = TextEditingController();
  final _amountController = TextEditingController();
  final _interestController = TextEditingController();
  final _lateFeeController = TextEditingController();

  @override
  void dispose() {
    _paymentIdController.dispose();
    _amountController.dispose();
    _interestController.dispose();
    _lateFeeController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final invoices = ref.watch(treasuryInvoicesProvider);
    final costCenters = ref.watch(treasuryCostCentersProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Registrar Cobranza')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: ZCard(
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                ZTextField(controller: _paymentIdController, label: 'ID de Pago'),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: _amountController, label: 'Monto', keyboardType: TextInputType.number),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: _interestController, label: 'Interés', keyboardType: TextInputType.number),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: _lateFeeController, label: 'Recargo', keyboardType: TextInputType.number),
                const SizedBox(height: ZSpacing.sm),
                invoices.when(
                  data: (items) => ZSelect<String>(
                    value: _selectedInvoiceId,
                    label: 'Factura',
                    hint: 'Seleccione una factura...',
                    items: items,
                    onChanged: (val) => setState(() => _selectedInvoiceId = val),
                    validator: (v) => v == null ? 'Requerido' : null,
                  ),
                  loading: () => const SizedBox(height: 80, child: Center(child: CircularProgressIndicator())),
                  error: (_, __) => ZTextField(controller: TextEditingController(), label: 'ID de Factura'),
                ),
                const SizedBox(height: ZSpacing.sm),
                costCenters.when(
                  data: (items) => ZSelect<String>(
                    value: _selectedCostCenterId,
                    label: 'Centro de Costos',
                    hint: 'Seleccione un centro...',
                    items: items,
                    onChanged: (val) => setState(() => _selectedCostCenterId = val),
                  ),
                  loading: () => const SizedBox(height: 80, child: Center(child: CircularProgressIndicator())),
                  error: (_, __) => const SizedBox.shrink(),
                ),
                const SizedBox(height: ZSpacing.lg),
                ZButton(
                  text: 'Guardar Cobranza',
                  onPressed: _submit,
                  icon: Icons.save,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _submit() async {
    if (_formKey.currentState!.validate()) {
      final treasuryService = ref.read(treasuryServiceProvider);
      try {
        await treasuryService.generateCollectionEntry({
          'paymentId': _paymentIdController.text,
          'amount': double.parse(_amountController.text),
          'interest': double.parse(_interestController.text),
          'lateFee': double.parse(_lateFeeController.text),
          'invoiceId': _selectedInvoiceId,
          'costCenterId': _selectedCostCenterId,
        });
        
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Cobranza contabilizada exitosamente')),
          );
        }
      } catch (e) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Error: $e')),
          );
        }
      }
    }
  }
}
