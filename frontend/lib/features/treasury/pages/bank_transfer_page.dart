import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import 'package:zorvian/core/widgets/responsive_layout.dart';
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
      backgroundColor: Theme.of(context).brightness == Brightness.dark ? ZColors.darkBackground : ZColors.neutral50,
      appBar: AppBar(title: const Text('Nueva Transferencia Bancaria')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 800),
            child: ZCard(
              padding: const EdgeInsets.all(24),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Detalles de la Transferencia', style: ZTypography.titleLarge),
                    const SizedBox(height: 24),
                    ResponsiveGrid(
                      mobileColumns: 1,
                      tabletColumns: 2,
                      desktopColumns: 2,
                      children: [
                        ZTextField(
                          controller: _sourceAccountIdController,
                          label: 'Cuenta Origen',
                          prefix: const Icon(Icons.account_balance_wallet_outlined),
                          validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
                        ),
                        ZTextField(
                          controller: _destinationAccountIdController,
                          label: 'Cuenta Destino',
                          prefix: const Icon(Icons.account_balance_outlined),
                          validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    ZTextField(
                      controller: _amountController,
                      label: 'Monto a Transferir',
                      prefix: const Icon(Icons.attach_money),
                      keyboardType: TextInputType.number,
                      validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
                    ),
                    const SizedBox(height: 32),
                    ZButton(
                      text: 'Registrar Transferencia',
                      onPressed: _submit,
                      icon: Icons.sync_alt,
                      fullWidth: true,
                    ),
                  ],
                ),
              ),
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
