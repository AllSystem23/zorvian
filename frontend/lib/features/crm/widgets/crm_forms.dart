import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/leads_provider.dart';
import '../providers/opportunities_provider.dart';

class AddLeadSheet extends ConsumerStatefulWidget {
  const AddLeadSheet({super.key});

  @override
  ConsumerState<AddLeadSheet> createState() => _AddLeadSheetState();
}

class _AddLeadSheetState extends ConsumerState<AddLeadSheet> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _companyController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();
  String _source = 'Web';

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _companyController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.only(
        bottom: MediaQuery.of(context).viewInsets.bottom,
        left: 20,
        right: 20,
        top: 20,
      ),
      child: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Nuevo Prospecto', style: ZTypography.titleMedium),
            const SizedBox(height: 20),
            Row(
              children: [
                Expanded(
                  child: ZTextField(
                    label: 'Nombre',
                    controller: _firstNameController,
                    validator: (v) => v?.isEmpty ?? true ? 'Requerido' : null,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: ZTextField(
                    label: 'Apellido',
                    controller: _lastNameController,
                    validator: (v) => v?.isEmpty ?? true ? 'Requerido' : null,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            ZTextField(
              label: 'Empresa',
              controller: _companyController,
            ),
            const SizedBox(height: 12),
            ZTextField(
              label: 'Email',
              controller: _emailController,
              keyboardType: TextInputType.emailAddress,
            ),
            const SizedBox(height: 12),
            ZTextField(
              label: 'Teléfono / WhatsApp',
              controller: _phoneController,
              keyboardType: TextInputType.phone,
            ),
            const SizedBox(height: 20),
            ZButton(
              label: 'Crear Prospecto',
              onPressed: _submit,
            ),
            const SizedBox(height: 20),
          ],
        ),
      ),
    );
  }

  void _submit() async {
    if (_formKey.currentState?.validate() ?? false) {
      final success = await ref.read(leadsProvider.notifier).createLead({
        'firstName': _firstNameController.text,
        'lastName': _lastNameController.text,
        'companyName': _companyController.text,
        'email': _emailController.text,
        'phone': _phoneController.text,
        'source': _source,
        'status': 'New',
      });

      if (success && mounted) {
        Navigator.pop(context);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Lead creado exitosamente')),
        );
      }
    }
  }
}

class AddOpportunitySheet extends ConsumerStatefulWidget {
  const AddOpportunitySheet({super.key});

  @override
  ConsumerState<AddOpportunitySheet> createState() => _AddOpportunitySheetState();
}

class _AddOpportunitySheetState extends ConsumerState<AddOpportunitySheet> {
  final _formKey = GlobalKey<FormState>();
  final _titleController = TextEditingController();
  final _valueController = TextEditingController();
  String? _selectedLeadId;

  @override
  void dispose() {
    _titleController.dispose();
    _valueController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final leads = ref.watch(leadsProvider).leads;

    return Container(
      padding: EdgeInsets.only(
        bottom: MediaQuery.of(context).viewInsets.bottom,
        left: 20,
        right: 20,
        top: 20,
      ),
      child: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Nueva Oportunidad', style: ZTypography.titleMedium),
            const SizedBox(height: 20),
            ZTextField(
              label: 'Título de la Negociación',
              controller: _titleController,
              validator: (v) => v?.isEmpty ?? true ? 'Requerido' : null,
            ),
            const SizedBox(height: 12),
            ZTextField(
              label: 'Valor Esperado (USD)',
              controller: _valueController,
              keyboardType: TextInputType.number,
              validator: (v) => v?.isEmpty ?? true ? 'Requerido' : null,
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              decoration: const InputDecoration(labelText: 'Vincular a Lead'),
              items: leads.map((l) => DropdownMenuItem(
                value: l.id,
                child: Text(l.fullName),
              )).toList(),
              onChanged: (v) => setState(() => _selectedLeadId = v),
            ),
            const SizedBox(height: 20),
            ZButton(
              label: 'Crear Oportunidad',
              onPressed: _submit,
            ),
            const SizedBox(height: 20),
          ],
        ),
      ),
    );
  }

  void _submit() async {
    if (_formKey.currentState?.validate() ?? false) {
      final stages = ref.read(opportunitiesProvider).stages;
      if (stages.isEmpty) return;

      final success = await ref.read(opportunitiesProvider.notifier).createOpportunity({
        'title': _titleController.text,
        'expectedValue': double.parse(_valueController.text),
        'currencyCode': 'USD',
        'probability': 10,
        'stageId': stages.first.id,
        'status': 'open',
        'priority': 'medium',
        'leadId': _selectedLeadId,
        'expectedCloseDate': DateTime.now().add(const Duration(days: 30)).toIso8601String(),
      });

      if (success && mounted) {
        Navigator.pop(context);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Oportunidad creada exitosamente')),
        );
      }
    }
  }
}
