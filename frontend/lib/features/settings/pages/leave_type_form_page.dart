import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

class LeaveTypeFormPage extends ConsumerStatefulWidget {
  const LeaveTypeFormPage({super.key});

  @override
  ConsumerState<LeaveTypeFormPage> createState() => _LeaveTypeFormPageState();
}

class _LeaveTypeFormPageState extends ConsumerState<LeaveTypeFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _codeCtrl = TextEditingController();
  final _nameCtrl = TextEditingController();
  final _descCtrl = TextEditingController();
  bool _isPaid = false;
  bool _requiresAttachment = false;
  bool _requiresApproval = true;
  final _maxDaysPerYearCtrl = TextEditingController();
  bool _saving = false;

  @override
  void dispose() {
    _codeCtrl.dispose();
    _nameCtrl.dispose();
    _descCtrl.dispose();
    _maxDaysPerYearCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('leave-types', data: {
        'code': _codeCtrl.text.trim(),
        'name': _nameCtrl.text.trim(),
        'description': _descCtrl.text.trim().isEmpty ? null : _descCtrl.text.trim(),
        'isPaid': _isPaid,
        'requiresAttachment': _requiresAttachment,
        'requiresApproval': _requiresApproval,
        'maxDaysPerYear': _maxDaysPerYearCtrl.text.isEmpty ? null : int.tryParse(_maxDaysPerYearCtrl.text),
      });
      if (mounted) context.pop(true);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error al guardar: $e'), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Nuevo tipo de permiso')),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            TextFormField(
              controller: _codeCtrl,
              decoration: const InputDecoration(labelText: 'Código', hintText: 'Ej: VAC'),
              validator: (v) => v == null || v.trim().isEmpty ? 'Requerido' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _nameCtrl,
              decoration: const InputDecoration(labelText: 'Nombre', hintText: 'Ej: Vacaciones'),
              validator: (v) => v == null || v.trim().isEmpty ? 'Requerido' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _descCtrl,
              decoration: const InputDecoration(labelText: 'Descripción (opcional)'),
              maxLines: 2,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _maxDaysPerYearCtrl,
              decoration: const InputDecoration(labelText: 'Máx. días por año (opcional)', hintText: 'Ej: 30'),
              keyboardType: TextInputType.number,
            ),
            const SizedBox(height: 16),
            SwitchListTile(
              title: const Text('Permiso pagado'),
              value: _isPaid,
              onChanged: (v) => setState(() => _isPaid = v),
            ),
            SwitchListTile(
              title: const Text('Requiere adjunto'),
              value: _requiresAttachment,
              onChanged: (v) => setState(() => _requiresAttachment = v),
            ),
            SwitchListTile(
              title: const Text('Requiere aprobación'),
              value: _requiresApproval,
              onChanged: (v) => setState(() => _requiresApproval = v),
            ),
            const SizedBox(height: 24),
            FilledButton.icon(
              onPressed: _saving ? null : _save,
              icon: _saving ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.save),
              label: Text(_saving ? 'Guardando...' : 'Guardar'),
            ),
          ],
        ),
      ),
    );
  }
}
