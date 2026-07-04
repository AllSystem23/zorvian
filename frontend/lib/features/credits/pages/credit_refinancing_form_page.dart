import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

final class CreditRefinancingFormPage extends ConsumerStatefulWidget {
  final String creditId;
  const CreditRefinancingFormPage({super.key, required this.creditId});
  @override
  ConsumerState<CreditRefinancingFormPage> createState() => _CreditRefinancingFormPageState();
}

final class _CreditRefinancingFormPageState extends ConsumerState<CreditRefinancingFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _rateCtrl = TextEditingController();
  final _countCtrl = TextEditingController();
  final _installmentCtrl = TextEditingController();
  final _financedCtrl = TextEditingController();
  final _reasonCtrl = TextEditingController();
  bool _loading = false;
  String? _error;

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('credits/${widget.creditId}/refinancing', data: {
        'newInterestRate': double.parse(_rateCtrl.text),
        'newInstallmentCount': int.parse(_countCtrl.text),
        'newInstallmentAmount': double.parse(_installmentCtrl.text),
        'newFinancedAmount': double.parse(_financedCtrl.text),
        'reason': _reasonCtrl.text,
      });
      if (mounted) context.pop(true);
    } catch (e) {
      setState(() => _error = 'Error al refinanciar');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _rateCtrl.dispose(); _countCtrl.dispose(); _installmentCtrl.dispose(); _financedCtrl.dispose(); _reasonCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Refinanciar Crédito')),
      body: SingleChildScrollView(
        padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null) Container(
                padding: const EdgeInsets.all(12), margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(color: theme.colorScheme.errorContainer, borderRadius: BorderRadius.circular(8)),
                child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
              ),
              TextFormField(controller: _financedCtrl, decoration: const InputDecoration(labelText: 'Nuevo monto financiado', prefixText: '\$ '), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _rateCtrl, decoration: const InputDecoration(labelText: 'Nueva tasa de interés (%)', suffixText: '%'), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _countCtrl, decoration: const InputDecoration(labelText: 'Nuevo número de cuotas'), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _installmentCtrl, decoration: const InputDecoration(labelText: 'Monto de cuota', prefixText: '\$ '), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _reasonCtrl, decoration: const InputDecoration(labelText: 'Motivo del refinanciamiento'), maxLines: 3),
              const SizedBox(height: 24),
              ZButton(
                text: 'Guardar Refinanciamiento',
                onPressed: _save,
                isLoading: _loading,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
