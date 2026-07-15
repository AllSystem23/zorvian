import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../core/providers/company_branch_provider.dart' show companyBranchProvider;
import '../../../shared/ds/ds.dart';

class SupplierCreditNoteFormPage extends ConsumerStatefulWidget {
  final String? purchaseId;
  final String? supplierId;
  const SupplierCreditNoteFormPage({super.key, this.purchaseId, this.supplierId});

  @override
  ConsumerState<SupplierCreditNoteFormPage> createState() => _SupplierCreditNoteFormPageState();
}

class _SupplierCreditNoteFormPageState extends ConsumerState<SupplierCreditNoteFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _reasonCtrl = TextEditingController();
  final _subtotalCtrl = TextEditingController();
  final _taxCtrl = TextEditingController();
  String? _selectedSupplierId;
  String? _selectedPurchaseId;
  bool _loading = false;

  @override
  void initState() {
    super.initState();
    _selectedSupplierId = widget.supplierId;
    _selectedPurchaseId = widget.purchaseId;
  }

  @override
  void dispose() {
    _reasonCtrl.dispose();
    _subtotalCtrl.dispose();
    _taxCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    try {
      final dio = ref.read(dioClientProvider);
      final branch = ref.read(companyBranchProvider);
      final subtotal = double.tryParse(_subtotalCtrl.text) ?? 0;
      final tax = double.tryParse(_taxCtrl.text) ?? 0;
      await dio.post('supplier-credit-notes', data: {
        'supplierId': _selectedSupplierId,
        'purchaseId': _selectedPurchaseId,
        'creditNoteDate': DateTime.now().toIso8601String().substring(0, 10),
        'reason': _reasonCtrl.text.trim(),
        'subtotal': subtotal,
        'tax': tax,
        'total': subtotal + tax,
        'branchId': branch.branchId ?? '',
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Nota de crédito registrada')));
        context.pop(true);
      }
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: ZCard(
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Datos de la Nota de Crédito', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                const SizedBox(height: 24),
                ZTextField(controller: _subtotalCtrl, label: 'Subtotal', keyboardType: TextInputType.number),
                const SizedBox(height: 16),
                ZTextField(controller: _taxCtrl, label: 'Impuesto', keyboardType: TextInputType.number),
                const SizedBox(height: 16),
                ZTextField(controller: _reasonCtrl, label: 'Motivo', maxLines: 3),
                const SizedBox(height: 24),
                ZButton(text: 'Guardar Nota de Crédito', icon: Icons.save, isLoading: _loading, onPressed: _submit),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
