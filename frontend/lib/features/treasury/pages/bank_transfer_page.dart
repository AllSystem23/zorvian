import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/treasury_provider.dart';

class BankTransferPage extends ConsumerStatefulWidget {
  const BankTransferPage({super.key});

  @override
  ConsumerState<BankTransferPage> createState() => _BankTransferPageState();
}

class _BankTransferPageState extends ConsumerState<BankTransferPage> {
  final _formKey = GlobalKey<FormState>();
  
  final _amountController = TextEditingController();
  final _sourceAccountIdController = TextEditingController();
  final _destinationAccountIdController = TextEditingController();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Registrar Transferencia')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: ZCard(
          child: Form(
            key: _formKey,
            child: Column(
              children: [
                ZTextField(controller: _sourceAccountIdController, label: 'Cuenta Origen'),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: _destinationAccountIdController, label: 'Cuenta Destino'),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: _amountController, label: 'Monto', keyboardType: TextInputType.number),
                const SizedBox(height: ZSpacing.lg),
                ZButton(
                  text: 'Guardar Transferencia',
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
        await treasuryService.generateBankTransferEntry({
          'amount': double.parse(_amountController.text),
          'sourceAccountId': _sourceAccountIdController.text,
          'destinationAccountId': _destinationAccountIdController.text,
        });
        
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Transferencia contabilizada exitosamente')),
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
