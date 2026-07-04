import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';

final class _StepEntry {
  final TextEditingController roleCtrl = TextEditingController();
  final TextEditingController minCtrl = TextEditingController();
  final TextEditingController maxCtrl = TextEditingController();
  void dispose() { roleCtrl.dispose(); minCtrl.dispose(); maxCtrl.dispose(); }
}

final class ApprovalFlowFormPage extends ConsumerStatefulWidget {
  final String? flowId;
  const ApprovalFlowFormPage({super.key, this.flowId});
  @override
  ConsumerState<ApprovalFlowFormPage> createState() => _ApprovalFlowFormPageState();
}

final class _ApprovalFlowFormPageState extends ConsumerState<ApprovalFlowFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _moduleCtrl = TextEditingController();
  final _eventCtrl = TextEditingController();
  final _descCtrl = TextEditingController();
  final _steps = <_StepEntry>[];
  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.flowId != null;

  @override
  void initState() {
    super.initState();
    _addStep();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('approval-flows/${widget.flowId}');
      final d = r.data;
      _moduleCtrl.text = d['module'] ?? '';
      _eventCtrl.text = d['eventType'] ?? '';
      _descCtrl.text = d['description'] ?? '';
      final steps = (d['steps'] as List?) ?? [];
      for (final s in steps.skip(1)) {
        final e = _StepEntry();
        e.roleCtrl.text = s['approverRole'] ?? '';
        e.minCtrl.text = s['minAmount']?.toString() ?? '';
        e.maxCtrl.text = s['maxAmount']?.toString() ?? '';
        _steps.add(e);
      }
      if (_steps.isNotEmpty) {
        _steps[0].roleCtrl.text = steps.isNotEmpty ? steps[0]['approverRole'] ?? '' : '';
        _steps[0].minCtrl.text = steps.isNotEmpty ? steps[0]['minAmount']?.toString() ?? '' : '';
        _steps[0].maxCtrl.text = steps.isNotEmpty ? steps[0]['maxAmount']?.toString() ?? '' : '';
      }
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar flujo');
    }
  }

  void _addStep() => setState(() => _steps.add(_StepEntry()));
  void _removeStep(int i) {
    _steps[i].dispose();
    setState(() => _steps.removeAt(i));
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'module': _moduleCtrl.text.trim(),
        'eventType': _eventCtrl.text.trim(),
        'description': _descCtrl.text.trim(),
        'steps': _steps.asMap().entries.map((e) => {
          'stepOrder': e.key + 1,
          'approverRole': e.value.roleCtrl.text.trim(),
          'minAmount': e.value.minCtrl.text.trim().isEmpty ? null : double.tryParse(e.value.minCtrl.text.trim()),
          'maxAmount': e.value.maxCtrl.text.trim().isEmpty ? null : double.tryParse(e.value.maxCtrl.text.trim()),
        }).toList(),
      };
      if (_isEditing) {
        await dio.put('approval-flows/${widget.flowId}', data: body);
      } else {
        await dio.post('approval-flows', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar flujo');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _moduleCtrl.dispose(); _eventCtrl.dispose(); _descCtrl.dispose();
    for (final s in _steps) { s.dispose(); }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar flujo' : 'Nuevo flujo')),
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
              TextFormField(controller: _moduleCtrl, decoration: const InputDecoration(labelText: 'Módulo', prefixIcon: Icon(Icons.widgets)), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _eventCtrl, decoration: const InputDecoration(labelText: 'Evento', prefixIcon: Icon(Icons.event)), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _descCtrl, decoration: const InputDecoration(labelText: 'Descripción', prefixIcon: Icon(Icons.description)), maxLines: 2),
              const SizedBox(height: 16),
              Row(children: [
                Text('Pasos de Aprobación', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                const Spacer(),
                TextButton.icon(icon: const Icon(Icons.add, size: 18), label: const Text('Agregar paso'), onPressed: _addStep),
              ]),
              const SizedBox(height: 8),
              ..._steps.asMap().entries.map((entry) {
                final i = entry.key;
                final s = entry.value;
                return ZCard(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                      children: [
                        Row(children: [Text('Paso ${i + 1}', style: const TextStyle(fontWeight: FontWeight.bold)), const Spacer(), if (_steps.length > 1) IconButton(icon: const Icon(Icons.remove_circle, color: Colors.red, size: 18), onPressed: () => _removeStep(i))]),
                        const SizedBox(height: 8),
                        TextFormField(controller: s.roleCtrl, decoration: const InputDecoration(labelText: 'Rol aprobador', isDense: true), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                        const SizedBox(height: 8),
                        Row(children: [
                          Expanded(child: TextFormField(controller: s.minCtrl, decoration: const InputDecoration(labelText: 'Monto mínimo', isDense: true), keyboardType: TextInputType.number)),
                          const SizedBox(width: 8),
                          Expanded(child: TextFormField(controller: s.maxCtrl, decoration: const InputDecoration(labelText: 'Monto máximo', isDense: true), keyboardType: TextInputType.number)),
                        ]),
                      ],
                    ),
                );
              }),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Guardar flujo',
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
