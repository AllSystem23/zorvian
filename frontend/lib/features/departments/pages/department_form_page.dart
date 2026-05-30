import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';

class DepartmentFormPage extends ConsumerStatefulWidget {
  final String? departmentId;
  const DepartmentFormPage({super.key, this.departmentId});

  @override
  ConsumerState<DepartmentFormPage> createState() => _DepartmentFormPageState();
}

class _DepartmentFormPageState extends ConsumerState<DepartmentFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _codeCtrl = TextEditingController();
  final _nameCtrl = TextEditingController();
  final _descCtrl = TextEditingController();
  bool _loading = false;
  bool _isActive = true;
  String? _error;
  bool _isEditing = false;

  @override
  void initState() {
    super.initState();
    _isEditing = widget.departmentId != null;
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('/departments/${widget.departmentId}');
      final data = r.data;
      _codeCtrl.text = data['code'] ?? '';
      _nameCtrl.text = data['name'] ?? '';
      _descCtrl.text = data['description'] ?? '';
      _isActive = data['isActive'] ?? true;
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar departamento');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });

    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'code': _codeCtrl.text.trim(),
        'name': _nameCtrl.text.trim(),
        'description': _descCtrl.text.trim(),
        'isActive': _isActive,
      };

      if (_isEditing) {
        await dio.put('/departments/${widget.departmentId}', data: body);
      } else {
        await dio.post('/departments', data: body);
      }
      if (mounted) context.pop(true);
    } catch (e) {
      setState(() => _error = 'Error al guardar departamento');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _codeCtrl.dispose();
    _nameCtrl.dispose();
    _descCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar departamento' : 'Nuevo departamento')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null)
                Container(
                  padding: const EdgeInsets.all(12),
                  margin: const EdgeInsets.only(bottom: 16),
                  decoration: BoxDecoration(
                    color: theme.colorScheme.errorContainer,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
                ),
              TextFormField(
                controller: _codeCtrl,
                decoration: const InputDecoration(labelText: 'Código', prefixIcon: Icon(Icons.tag)),
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _nameCtrl,
                decoration: const InputDecoration(labelText: 'Nombre', prefixIcon: Icon(Icons.business)),
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _descCtrl,
                decoration: const InputDecoration(labelText: 'Descripción', prefixIcon: Icon(Icons.description)),
                maxLines: 3,
              ),
              const SizedBox(height: 16),
              SwitchListTile(
                title: const Text('Activo'),
                value: _isActive,
                onChanged: (v) => setState(() => _isActive = v),
              ),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _loading ? null : _save,
                child: _loading
                    ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : Text(_isEditing ? 'Actualizar' : 'Crear departamento'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
