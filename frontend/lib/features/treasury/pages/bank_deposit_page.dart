import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/treasury_provider.dart';

class BankDepositPage extends ConsumerStatefulWidget {
  const BankDepositPage({super.key});

  @override
  ConsumerState<BankDepositPage> createState() => _BankDepositPageState();
}

class _BankDepositPageState extends ConsumerState<BankDepositPage> {
  final _formKey = GlobalKey<FormState>();
  
  final _amountController = TextEditingController();
  final _bankAccountIdController = TextEditingController(); // Debería ser un ZSelect en el futuro
  final _costCenterIdController = TextEditingController();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Registrar Depósito')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: ZCard(
          child: Form(
            key: _formKey,
            child: Column(
              children: [
                ZTextField(
                  controller: _bankAccountIdController,
                  label: 'ID de Cuenta Bancaria',
                ),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(
                  controller: _amountController,
                  label: 'Monto',
                  keyboardType: TextInputType.number,
                ),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(
                  controller: _costCenterIdController,
                  label: 'ID de Centro de Costos',
                ),
                const SizedBox(height: ZSpacing.lg),
                ZButton(
                  text: 'Guardar Depósito',
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
        // NOTA: bankMovementId debe ser proporcionado por el flujo de selección 
        // de movimientos pendientes de depósito en una implementación completa.
        await treasuryService.generateBankDepositEntry({
          'amount': double.parse(_amountController.text),
          'bankAccountId': _bankAccountIdController.text,
          'costCenterId': _costCenterIdController.text,
        });
        
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Depósito contabilizado exitosamente')),
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
