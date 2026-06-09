import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/treasury_provider.dart';

class CheckIssuancePage extends ConsumerStatefulWidget {
  const CheckIssuancePage({super.key});

  @override
  ConsumerState<CheckIssuancePage> createState() => _CheckIssuancePageState();
}

class _CheckIssuancePageState extends ConsumerState<CheckIssuancePage> {
  final _formKey = GlobalKey<FormState>();
  
  final _beneficiaryController = TextEditingController();
  final _amountController = TextEditingController();
  final _descriptionController = TextEditingController();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Emitir Cheque')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: ZCard(
          child: Form(
            key: _formKey,
            child: Column(
              children: [
                ZTextField(
                  controller: _beneficiaryController,
                  label: 'Beneficiario',
                ),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(
                  controller: _amountController,
                  label: 'Monto',
                  keyboardType: TextInputType.number,
                ),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(
                  controller: _descriptionController,
                  label: 'Concepto',
                ),
                const SizedBox(height: ZSpacing.lg),
                ZButton(
                  text: 'Guardar Cheque',
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
        final response = await treasuryService.issueCheck({
          'beneficiary': _beneficiaryController.text,
          'amount': double.parse(_amountController.text),
          'description': _descriptionController.text,
          'issueDate': DateTime.now().toIso8601String(),
        });
        
        final checkId = response['id'];
        
        await treasuryService.generateCheckEntry({
          'checkId': checkId,
          'amount': double.parse(_amountController.text),
          'checkType': 'Issuance',
          'payeeId': _beneficiaryController.text,
        });
        
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Cheque emitido y contabilizado exitosamente')),
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
