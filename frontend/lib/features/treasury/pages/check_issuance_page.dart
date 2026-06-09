import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/treasury_provider.dart';

class CheckIssuancePage extends ConsumerStatefulWidget {
  const CheckIssuancePage({super.key});

  @override
  ConsumerState<CheckIssuancePage> createState() => _CheckIssuancePageState();
}

class _CheckIssuancePageState extends ConsumerState<CheckIssuancePage> {
  final _formKey = GlobalKey<FormState>();
  
  // Controladores para los campos del cheque
  final _beneficiaryController = TextEditingController();
  final _amountController = TextEditingController();
  final _descriptionController = TextEditingController();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Emitir Cheque')),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Form(
          key: _formKey,
          child: ListView(
            children: [
              TextFormField(
                controller: _beneficiaryController,
                decoration: const InputDecoration(labelText: 'Beneficiario'),
                validator: (value) => value!.isEmpty ? 'Requerido' : null,
              ),
              TextFormField(
                controller: _amountController,
                decoration: const InputDecoration(labelText: 'Monto'),
                keyboardType: TextInputType.number,
                validator: (value) => value!.isEmpty ? 'Requerido' : null,
              ),
              TextFormField(
                controller: _descriptionController,
                decoration: const InputDecoration(labelText: 'Concepto'),
              ),
              const SizedBox(height: 20),
              ElevatedButton(
                onPressed: () async {
                  if (_formKey.currentState!.validate()) {
                    final treasuryService = ref.read(treasuryServiceProvider);
                    try {
                      await treasuryService.issueCheck({
                        'beneficiary': _beneficiaryController.text,
                        'amount': double.parse(_amountController.text),
                        'description': _descriptionController.text,
                        'issueDate': DateTime.now().toIso8601String(),
                      });
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(content: Text('Cheque emitido exitosamente')),
                        );
                      }
                    } catch (e) {
                      if (context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(content: Text('Error: $e')),
                        );
                      }
                    }
                  }
                },
                child: const Text('Guardar Cheque'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
