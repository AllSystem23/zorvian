import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../models/crm_models.dart';
import '../providers/leads_provider.dart';

class LeadFormPage extends ConsumerStatefulWidget {
  final String? leadId;

  const LeadFormPage({super.key, this.leadId});

  @override
  ConsumerState<LeadFormPage> createState() => _LeadFormPageState();
}

class _LeadFormPageState extends ConsumerState<LeadFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _companyController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();
  final _whatsappController = TextEditingController();
  final _cityController = TextEditingController();
  final _jobTitleController = TextEditingController();
  final _notesController = TextEditingController();
  String _source = 'Web';
  String _status = 'new';
  String _interestLevel = 'medium';
  bool _loading = false;

  bool get _isEditing => widget.leadId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) {
      final state = ref.read(leadsProvider);
      final lead = state.leads.where((l) => l.id == widget.leadId).firstOrNull;
      if (lead != null) _populateForm(lead);
    }
  }

  void _populateForm(Lead lead) {
    _firstNameController.text = lead.firstName;
    _lastNameController.text = lead.lastName;
    _companyController.text = lead.companyName ?? '';
    _emailController.text = lead.email ?? '';
    _phoneController.text = lead.phone ?? '';
    _whatsappController.text = lead.whatsapp ?? '';
    _cityController.text = lead.city ?? '';
    _jobTitleController.text = lead.jobTitle ?? '';
    _notesController.text = lead.notes ?? '';
    _source = lead.source ?? 'Web';
    _status = lead.status;
    _interestLevel = lead.interestLevel ?? 'medium';
  }

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    _companyController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    _whatsappController.dispose();
    _cityController.dispose();
    _jobTitleController.dispose();
    _notesController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SingleChildScrollView(
        padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text('Información personal', style: ZTypography.titleSmall),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(child: ZTextField(label: 'Nombre', controller: _firstNameController, validator: _required)),
                  const SizedBox(width: 12),
                  Expanded(child: ZTextField(label: 'Apellido', controller: _lastNameController, validator: _required)),
                ],
              ),
              const SizedBox(height: 12),
              ZTextField(label: 'Empresa', controller: _companyController),
              const SizedBox(height: 12),
              ZTextField(label: 'Cargo', controller: _jobTitleController),
              const SizedBox(height: 24),
              Text('Contacto', style: ZTypography.titleSmall),
              const SizedBox(height: 12),
              ZTextField(label: 'Email', controller: _emailController, keyboardType: TextInputType.emailAddress),
              const SizedBox(height: 12),
              ZTextField(label: 'Teléfono', controller: _phoneController, keyboardType: TextInputType.phone),
              const SizedBox(height: 12),
              ZTextField(label: 'WhatsApp', controller: _whatsappController, keyboardType: TextInputType.phone),
              const SizedBox(height: 24),
              Text('Ubicación', style: ZTypography.titleSmall),
              const SizedBox(height: 12),
              ZTextField(label: 'Ciudad', controller: _cityController),
              const SizedBox(height: 24),
              Text('Clasificación', style: ZTypography.titleSmall),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                decoration: const InputDecoration(labelText: 'Origen'),
                initialValue: _source,
                items: ['Web', 'Referido', 'Red social', 'Llamada entrante', 'Evento', 'Otro']
                    .map((s) => DropdownMenuItem(value: s, child: Text(s)))
                    .toList(),
                onChanged: (v) => _source = v!,
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                decoration: const InputDecoration(labelText: 'Nivel de interés'),
                initialValue: _interestLevel,
                items: ['high', 'medium', 'low']
                    .map((s) => DropdownMenuItem(value: s, child: Text(s.toUpperCase())))
                    .toList(),
                onChanged: (v) => _interestLevel = v!,
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                decoration: const InputDecoration(labelText: 'Estado'),
                initialValue: _status,
                items: ['new', 'contacted', 'qualified', 'lost']
                    .map((s) => DropdownMenuItem(value: s, child: Text(s.toUpperCase())))
                    .toList(),
                onChanged: (v) => _status = v!,
              ),
              const SizedBox(height: 24),
              Text('Notas', style: ZTypography.titleSmall),
              const SizedBox(height: 12),
              ZTextField(label: 'Notas', controller: _notesController, maxLines: 3),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Guardar Cambios' : 'Crear Prospecto',
                onPressed: _submit,
                isLoading: _loading,
              ),
              const SizedBox(height: 32),
            ],
          ),
        ),
      ),
    );
  }

  String? _required(String? v) => v?.isEmpty ?? true ? 'Requerido' : null;

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    setState(() => _loading = true);

    final data = {
      'firstName': _firstNameController.text,
      'lastName': _lastNameController.text,
      'companyName': _companyController.text.isNotEmpty ? _companyController.text : null,
      'jobTitle': _jobTitleController.text.isNotEmpty ? _jobTitleController.text : null,
      'email': _emailController.text.isNotEmpty ? _emailController.text : null,
      'phone': _phoneController.text.isNotEmpty ? _phoneController.text : null,
      'whatsapp': _whatsappController.text.isNotEmpty ? _whatsappController.text : null,
      'city': _cityController.text.isNotEmpty ? _cityController.text : null,
      'source': _source,
      'interestLevel': _interestLevel,
      'status': _status,
      'notes': _notesController.text.isNotEmpty ? _notesController.text : null,
    };

    final success = _isEditing
        ? await ref.read(leadsProvider.notifier).updateLead(widget.leadId!, data)
        : await ref.read(leadsProvider.notifier).createLead(data);

    setState(() => _loading = false);

    if (success && mounted) {
      ZToast.success(context, _isEditing ? 'Prospecto actualizado' : 'Prospecto creado');
      context.pop(true);
    }
  }
}
