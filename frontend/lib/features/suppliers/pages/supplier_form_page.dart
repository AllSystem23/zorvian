import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';

final class SupplierFormPage extends ConsumerStatefulWidget {
  final String? supplierId;
  const SupplierFormPage({super.key, this.supplierId});
  @override
  ConsumerState<SupplierFormPage> createState() => _SupplierFormPageState();
}

final class _SupplierFormPageState extends ConsumerState<SupplierFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _codeCtrl = TextEditingController();
  final _nameCtrl = TextEditingController();
  final _contactCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();
  bool _isActive = true;
  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.supplierId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('suppliers/${widget.supplierId}');
      final d = r.data;
      _codeCtrl.text = d['code'] ?? '';
      _nameCtrl.text = d['name'] ?? '';
      _contactCtrl.text = d['contactName'] ?? '';
      _phoneCtrl.text = d['phone'] ?? '';
      _emailCtrl.text = d['email'] ?? '';
      _addressCtrl.text = d['address'] ?? '';
      _isActive = d['isActive'] ?? true;
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar proveedor');
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
        'contactName': _contactCtrl.text.trim().isEmpty ? null : _contactCtrl.text.trim(),
        'phone': _phoneCtrl.text.trim().isEmpty ? null : _phoneCtrl.text.trim(),
        'email': _emailCtrl.text.trim().isEmpty ? null : _emailCtrl.text.trim(),
        'address': _addressCtrl.text.trim().isEmpty ? null : _addressCtrl.text.trim(),
        'isActive': _isActive,
      };
      if (_isEditing) {
        await dio.put('suppliers/${widget.supplierId}', data: body);
      } else {
        await dio.post('suppliers', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar proveedor');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _codeCtrl.dispose(); _nameCtrl.dispose(); _contactCtrl.dispose();
    _phoneCtrl.dispose(); _emailCtrl.dispose(); _addressCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar proveedor' : 'Nuevo proveedor')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null) Container(
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(color: theme.colorScheme.errorContainer, borderRadius: BorderRadius.circular(8)),
                child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
              ),
              TextFormField(controller: _codeCtrl, decoration: const InputDecoration(labelText: 'Código', prefixIcon: Icon(Icons.tag)), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _nameCtrl, decoration: const InputDecoration(labelText: 'Nombre', prefixIcon: Icon(Icons.business)), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _contactCtrl, decoration: const InputDecoration(labelText: 'Contacto', prefixIcon: Icon(Icons.person))),
              const SizedBox(height: 12),
              TextFormField(controller: _phoneCtrl, decoration: const InputDecoration(labelText: 'Teléfono', prefixIcon: Icon(Icons.phone))),
              const SizedBox(height: 12),
              TextFormField(controller: _emailCtrl, decoration: const InputDecoration(labelText: 'Email', prefixIcon: Icon(Icons.email))),
              const SizedBox(height: 12),
              TextFormField(controller: _addressCtrl, decoration: const InputDecoration(labelText: 'Dirección', prefixIcon: Icon(Icons.location_on)), maxLines: 2),
              const SizedBox(height: 12),
              SwitchListTile(title: const Text('Activo'), value: _isActive, onChanged: (v) => setState(() => _isActive = v)),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _loading ? null : _save,
                child: _loading
                    ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : Text(_isEditing ? 'Actualizar' : 'Crear proveedor'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
