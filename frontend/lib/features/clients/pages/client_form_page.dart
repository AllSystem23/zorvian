import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';

final class ClientFormPage extends ConsumerStatefulWidget {
  final String? clientId;
  const ClientFormPage({super.key, this.clientId});
  @override
  ConsumerState<ClientFormPage> createState() => _ClientFormPageState();
}

final class _ClientFormPageState extends ConsumerState<ClientFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _codeCtrl = TextEditingController();
  final _nameCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();
  final _cityCtrl = TextEditingController();
  final _stateCtrl = TextEditingController();
  final _referencesCtrl = TextEditingController();
  final _docCtrl = TextEditingController();
  final _creditLimitCtrl = TextEditingController();
  String _type = 'Person';
  bool _isActive = true;
  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.clientId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('clients/${widget.clientId}');
      final d = r.data;
      _codeCtrl.text = d['code'] ?? '';
      _nameCtrl.text = d['fullName'] ?? '';
      _emailCtrl.text = d['email'] ?? '';
      _phoneCtrl.text = d['phone'] ?? '';
      _addressCtrl.text = d['address'] ?? '';
      _cityCtrl.text = d['city'] ?? '';
      _stateCtrl.text = d['state'] ?? '';
      _referencesCtrl.text = d['references'] ?? '';
      _docCtrl.text = d['identificationNumber'] ?? d['documentNumber'] ?? '';
      _creditLimitCtrl.text = (d['creditLimit'] ?? 0).toString();
      _type = d['type'] ?? 'Person';
      _isActive = d['isActive'] ?? true;
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar cliente');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'code': _codeCtrl.text.trim(),
        'fullName': _nameCtrl.text.trim(),
        'email': _emailCtrl.text.trim().isEmpty ? null : _emailCtrl.text.trim(),
        'phone': _phoneCtrl.text.trim().isEmpty ? null : _phoneCtrl.text.trim(),
        'address': _addressCtrl.text.trim().isEmpty ? null : _addressCtrl.text.trim(),
        'city': _cityCtrl.text.trim().isEmpty ? null : _cityCtrl.text.trim(),
        'state': _stateCtrl.text.trim().isEmpty ? null : _stateCtrl.text.trim(),
        'references': _referencesCtrl.text.trim().isEmpty ? null : _referencesCtrl.text.trim(),
        'identificationNumber': _docCtrl.text.trim().isEmpty ? null : _docCtrl.text.trim(),
        'type': _type,
        'creditLimit': double.tryParse(_creditLimitCtrl.text) ?? 0,
        'isActive': _isActive,
      };
      if (_isEditing) {
        await dio.put('clients/${widget.clientId}', data: body);
      } else {
        await dio.post('clients', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar cliente');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _codeCtrl.dispose(); _nameCtrl.dispose(); _emailCtrl.dispose();
    _phoneCtrl.dispose(); _addressCtrl.dispose(); _cityCtrl.dispose();
    _stateCtrl.dispose(); _referencesCtrl.dispose(); _docCtrl.dispose(); _creditLimitCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar cliente' : 'Nuevo cliente')),
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
              TextFormField(controller: _nameCtrl, decoration: const InputDecoration(labelText: 'Nombre completo', prefixIcon: Icon(Icons.person)), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _emailCtrl, decoration: const InputDecoration(labelText: 'Email', prefixIcon: Icon(Icons.email))),
              const SizedBox(height: 12),
              TextFormField(controller: _phoneCtrl, decoration: const InputDecoration(labelText: 'Teléfono', prefixIcon: Icon(Icons.phone))),
              const SizedBox(height: 12),
              TextFormField(controller: _addressCtrl, decoration: const InputDecoration(labelText: 'Dirección', prefixIcon: Icon(Icons.location_on)), maxLines: 2),
              const SizedBox(height: 12),
              Row(children: [
                Expanded(child: TextFormField(controller: _cityCtrl, decoration: const InputDecoration(labelText: 'Municipio', prefixIcon: Icon(Icons.location_city)))),
                const SizedBox(width: 12),
                Expanded(child: TextFormField(controller: _stateCtrl, decoration: const InputDecoration(labelText: 'Departamento', prefixIcon: Icon(Icons.map)))),
              ]),
              const SizedBox(height: 12),
              TextFormField(controller: _referencesCtrl, decoration: const InputDecoration(labelText: 'Referencias', prefixIcon: Icon(Icons.notes)), maxLines: 2),
              const SizedBox(height: 12),
              TextFormField(controller: _docCtrl, decoration: const InputDecoration(labelText: 'Documento', prefixIcon: Icon(Icons.badge))),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                initialValue: _type,
                decoration: const InputDecoration(labelText: 'Tipo', prefixIcon: Icon(Icons.category)),
                items: const [
                  DropdownMenuItem(value: 'Person', child: Text('Persona')),
                  DropdownMenuItem(value: 'Company', child: Text('Empresa')),
                ],
                onChanged: (v) => setState(() => _type = v ?? 'Person'),
              ),
              const SizedBox(height: 12),
              TextFormField(controller: _creditLimitCtrl, decoration: const InputDecoration(labelText: 'Límite de crédito', prefixIcon: Icon(Icons.credit_card)), keyboardType: TextInputType.number),
              const SizedBox(height: 12),
              SwitchListTile(title: const Text('Activo'), value: _isActive, onChanged: (v) => setState(() => _isActive = v)),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _loading ? null : _save,
                child: _loading
                    ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : Text(_isEditing ? 'Actualizar' : 'Crear cliente'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
