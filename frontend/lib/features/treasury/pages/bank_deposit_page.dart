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
  
  String? _selectedBankAccountId;
  String? _selectedCostCenterId;
  final _amountController = TextEditingController();

  @override
  void dispose() {
    _amountController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final bankAccounts = ref.watch(treasuryBankAccountsProvider);
    final costCenters = ref.watch(treasuryCostCentersProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Registrar Depósito')),
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
                  error: (_, _) => ZTextField(controller: TextEditingController(), label: 'ID de Cuenta Bancaria'),
                ),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(
                  controller: _amountController,
                  label: 'Monto',
                  keyboardType: TextInputType.number,
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
                  error: (_, _) => const SizedBox.shrink(),
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
        await treasuryService.generateBankDepositEntry({
          'amount': double.parse(_amountController.text),
          'bankAccountId': _selectedBankAccountId,
          'costCenterId': _selectedCostCenterId,
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
