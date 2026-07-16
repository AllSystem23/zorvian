import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../providers/treasury_provider.dart';

/// Beneficiary model for check issuance
class _BeneficiaryOption {
  final String id;
  final String displayName;
  final String beneficiaryName;
  final String type;
  _BeneficiaryOption({
    required this.id,
    required this.displayName,
    required this.beneficiaryName,
    required this.type,
  });
}

/// Provider that loads all possible beneficiaries (clients, suppliers, employees)
final _beneficiariesProvider = FutureProvider<List<_BeneficiaryOption>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final beneficiaries = <_BeneficiaryOption>[];

  try {
    final clientsRes = await dio.get('clients', params: {'page': 1, 'pageSize': 200});
    final clients = clientsRes.data['items'] as List? ?? clientsRes.data as List? ?? [];
    for (final c in clients) {
      final name = c['fullName'] ?? c['name'] ?? '';
      beneficiaries.add(_BeneficiaryOption(
        id: c['id']?.toString() ?? '',
        displayName: '$name (Cliente)',
        beneficiaryName: name,
        type: 'client',
      ));
    }
  } catch (_) {}

  try {
    final suppliersRes = await dio.get('suppliers', params: {'page': 1, 'pageSize': 200});
    final suppliers = suppliersRes.data['items'] as List? ?? suppliersRes.data as List? ?? [];
    for (final s in suppliers) {
      final name = s['name'] ?? s['companyName'] ?? '';
      beneficiaries.add(_BeneficiaryOption(
        id: s['id']?.toString() ?? '',
        displayName: '$name (Proveedor)',
        beneficiaryName: name,
        type: 'supplier',
      ));
    }
  } catch (_) {}

  try {
    final employeesRes = await dio.get('employees', params: {'page': 1, 'pageSize': 200});
    final employees = employeesRes.data['items'] as List? ?? employeesRes.data as List? ?? [];
    for (final e in employees) {
      final name = '${e['firstName'] ?? ''} ${e['lastName'] ?? ''}';
      beneficiaries.add(_BeneficiaryOption(
        id: e['id']?.toString() ?? '',
        displayName: '${name.trim()} (Trabajador)',
        beneficiaryName: name.trim(),
        type: 'employee',
      ));
    }
  } catch (_) {}

  beneficiaries.sort((a, b) => a.displayName.compareTo(b.displayName));
  return beneficiaries;
});

class CheckIssuancePage extends ConsumerStatefulWidget {
  const CheckIssuancePage({super.key});

  @override
  ConsumerState<CheckIssuancePage> createState() => _CheckIssuancePageState();
}

class _CheckIssuancePageState extends ConsumerState<CheckIssuancePage> {
  final _formKey = GlobalKey<FormState>();
  
  _BeneficiaryOption? _selectedBeneficiary;
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
    final beneficiariesAsync = ref.watch(_beneficiariesProvider);

    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: ZCard(
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                beneficiariesAsync.when(
                  data: (beneficiaries) => ZSelect<_BeneficiaryOption>(
                    value: _selectedBeneficiary,
                    label: 'Beneficiario',
                    hint: 'Seleccione un beneficiario...',
                    items: beneficiaries.map((b) => DropdownMenuItem(
                      value: b,
                      child: Text(b.displayName, overflow: TextOverflow.ellipsis),
                    )).toList(),
                    onChanged: (val) => setState(() => _selectedBeneficiary = val),
                    validator: (v) => v == null ? 'Seleccione un beneficiario' : null,
                  ),
                  loading: () => const SizedBox(
                    height: 80,
                    child: Center(child: CircularProgressIndicator()),
                  ),
                  error: (e, _) => ZAlertCard(
                    message: 'Error cargando beneficiarios: $e',
                    severity: 'high',
                  ),
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
          'beneficiary': _selectedBeneficiary!.beneficiaryName,
          'amount': double.parse(_amountController.text),
          'description': _descriptionController.text,
          'issueDate': DateTime.now().toIso8601String(),
        });
        
        final checkId = response['id'];
        
        // Send the real GUID as payeeId, not the name
        await treasuryService.generateCheckEntry({
          'checkId': checkId,
          'amount': double.parse(_amountController.text),
          'checkType': 'Issuance',
          'payeeId': _selectedBeneficiary!.id,
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
