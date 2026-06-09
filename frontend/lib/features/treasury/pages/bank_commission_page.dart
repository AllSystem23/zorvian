import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/treasury_provider.dart';

class BankCommissionPage extends ConsumerStatefulWidget {
  const BankCommissionPage({super.key});

  @override
  ConsumerState<BankCommissionPage> createState() => _BankCommissionPageState();
}

class _BankCommissionPageState extends ConsumerState<BankCommissionPage> {
  final _formKey = GlobalKey<FormState>();
  
  final _amountController = TextEditingController();
  final _bankAccountIdController = TextEditingController();
  final _descriptionController = TextEditingController();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Registrar Comisión')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: ZCard(
          child: Form(
            key: _formKey,
            child: Column(
              children: [
                ZTextField(controller: _bankAccountIdController, label: 'ID de Cuenta Bancaria'),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: _amountController, label: 'Monto', keyboardType: TextInputType.number),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: _descriptionController, label: 'Descripción'),
                const SizedBox(height: ZSpacing.lg),
                ZButton(
                  text: 'Guardar Comisión',
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
        await treasuryService.generateBankCommissionEntry({
          'amount': double.parse(_amountController.text),
          'bankAccountId': _bankAccountIdController.text,
          'description': _descriptionController.text,
        });
        
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Comisión contabilizada exitosamente')),
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
