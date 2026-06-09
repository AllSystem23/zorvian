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
  
  final _paymentIdController = TextEditingController();
  final _amountController = TextEditingController();
  final _interestController = TextEditingController();
  final _lateFeeController = TextEditingController();
  final _invoiceIdController = TextEditingController();
  final _costCenterIdController = TextEditingController();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Registrar Cobranza')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: ZCard(
          child: Form(
            key: _formKey,
            child: Column(
              children: [
                ZTextField(controller: _paymentIdController, label: 'ID de Pago'),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: _amountController, label: 'Monto', keyboardType: TextInputType.number),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: _interestController, label: 'Interés', keyboardType: TextInputType.number),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: _lateFeeController, label: 'Recargo', keyboardType: TextInputType.number),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: _invoiceIdController, label: 'ID de Factura'),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: _costCenterIdController, label: 'ID de Centro de Costos'),
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
          'invoiceId': _invoiceIdController.text,
          'costCenterId': _costCenterIdController.text,
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
