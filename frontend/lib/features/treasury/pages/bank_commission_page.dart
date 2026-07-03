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
  
  String? _selectedBankAccountId;
  final _amountController = TextEditingController();
  final _descriptionController = TextEditingController();

  @override
  void dispose() {
    _amountController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final bankAccounts = ref.watch(treasuryBankAccountsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Registrar Comisión')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: ZCard(
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                bankAccounts.when(
                  data: (items) => ZSelect<String>(
                    value: _selectedBankAccountId,
                    label: 'Cuenta Bancaria',
                    hint: 'Seleccione una cuenta...',
                    items: items,
                    onChanged: (val) => setState(() => _selectedBankAccountId = val),
                    validator: (v) => v == null ? 'Requerido' : null,
                  ),
                  loading: () => const SizedBox(height: 80, child: Center(child: CircularProgressIndicator())),
                  error: (_, __) => ZTextField(controller: TextEditingController(), label: 'ID de Cuenta Bancaria'),
                ),
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
          'bankAccountId': _selectedBankAccountId,
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
