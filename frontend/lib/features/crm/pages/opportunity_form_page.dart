import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../models/crm_models.dart';
import '../providers/leads_provider.dart';
import '../providers/opportunities_provider.dart';

class OpportunityFormPage extends ConsumerStatefulWidget {
  final String? opportunityId;

  const OpportunityFormPage({super.key, this.opportunityId});

  @override
  ConsumerState<OpportunityFormPage> createState() => _OpportunityFormPageState();
}

class _OpportunityFormPageState extends ConsumerState<OpportunityFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _titleController = TextEditingController();
  final _valueController = TextEditingController();
  final _descriptionController = TextEditingController();
  String? _selectedStageId;
  String? _selectedLeadId;
  String _priority = 'medium';
  String _status = 'open';
  int _probability = 10;
  bool _loading = false;

  bool get _isEditing => widget.opportunityId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) {
      final state = ref.read(opportunitiesProvider);
      final opp = state.opportunities.where((o) => o.id == widget.opportunityId).firstOrNull;
      if (opp != null) _populateForm(opp);
    }
  }

  void _populateForm(Opportunity opp) {
    _titleController.text = opp.title;
    _valueController.text = opp.expectedValue.toStringAsFixed(2);
    _descriptionController.text = opp.description ?? '';
    _selectedStageId = opp.stageId;
    _selectedLeadId = opp.leadId;
    _priority = opp.priority;
    _status = opp.status;
    _probability = opp.probability;
  }

  @override
  void dispose() {
    _titleController.dispose();
    _valueController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final pipeline = ref.watch(opportunitiesProvider);
    final leads = ref.watch(leadsProvider);

    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar Oportunidad' : 'Nueva Oportunidad')),
      body: SingleChildScrollView(
        padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text('Información general', style: ZTypography.titleSmall),
              const SizedBox(height: 12),
              ZTextField(label: 'Título de la Negociación', controller: _titleController, validator: _required),
              const SizedBox(height: 12),
              ZTextField(label: 'Valor Esperado (USD)', controller: _valueController, keyboardType: TextInputType.number, validator: _required),
              const SizedBox(height: 12),
              ZTextField(label: 'Descripción', controller: _descriptionController, maxLines: 3),
              const SizedBox(height: 24),
              Text('Clasificación', style: ZTypography.titleSmall),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                decoration: const InputDecoration(labelText: 'Etapa del pipeline'),
                initialValue: _selectedStageId,
                items: pipeline.stages.map((s) => DropdownMenuItem(
                  value: s.id,
                  child: Text(s.name),
                )).toList(),
                onChanged: (v) => _selectedStageId = v,
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                decoration: const InputDecoration(labelText: 'Prioridad'),
                initialValue: _priority,
                items: ['high', 'medium', 'low']
                    .map((s) => DropdownMenuItem(value: s, child: Text(s.toUpperCase())))
                    .toList(),
                onChanged: (v) => _priority = v!,
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                decoration: const InputDecoration(labelText: 'Estado'),
                initialValue: _status,
                items: ['open', 'won', 'lost']
                    .map((s) => DropdownMenuItem(value: s, child: Text(s.toUpperCase())))
                    .toList(),
                onChanged: (v) => _status = v!,
              ),
              const SizedBox(height: 12),
              Text('Probabilidad: $_probability%', style: ZTypography.bodyMedium),
              Slider(
                value: _probability.toDouble(),
                min: 0,
                max: 100,
                divisions: 10,
                label: '$_probability%',
                onChanged: (v) => setState(() => _probability = v.round()),
              ),
              const SizedBox(height: 24),
              Text('Vinculación', style: ZTypography.titleSmall),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                decoration: const InputDecoration(labelText: 'Vincular a Lead'),
                initialValue: _selectedLeadId,
                items: leads.leads.map((l) => DropdownMenuItem(
                  value: l.id,
                  child: Text(l.fullName),
                )).toList(),
                onChanged: (v) => _selectedLeadId = v,
              ),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Guardar Cambios' : 'Crear Oportunidad',
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
      'title': _titleController.text,
      'expectedValue': double.parse(_valueController.text),
      'description': _descriptionController.text.isNotEmpty ? _descriptionController.text : null,
      'currencyCode': 'USD',
      'probability': _probability,
      'stageId': _selectedStageId,
      'status': _status,
      'priority': _priority,
      'leadId': _selectedLeadId,
      'expectedCloseDate': DateTime.now().add(const Duration(days: 30)).toIso8601String(),
    };

    final success = _isEditing
        ? await ref.read(opportunitiesProvider.notifier).updateOpportunity(widget.opportunityId!, data)
        : await ref.read(opportunitiesProvider.notifier).createOpportunity(data);

    setState(() => _loading = false);

    if (success && mounted) {
      ZToast.success(context, _isEditing ? 'Oportunidad actualizada' : 'Oportunidad creada');
      context.pop(true);
    }
  }
}
