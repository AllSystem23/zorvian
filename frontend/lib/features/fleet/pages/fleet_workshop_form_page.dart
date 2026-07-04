import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../shared/ds/ds.dart';

final class FleetWorkshopFormPage extends ConsumerStatefulWidget {
  final String? workshopId;
  const FleetWorkshopFormPage({super.key, this.workshopId});

  @override
  ConsumerState<FleetWorkshopFormPage> createState() => _FleetWorkshopFormPageState();
}

final class _FleetWorkshopFormPageState extends ConsumerState<FleetWorkshopFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _nameCtrl = TextEditingController();
  final _contactCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();

  bool _loading = false;
  bool _isInternal = false;
  String? _error;
  bool get _isEditing => widget.workshopId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final d = await dio.get('fleet/workshops/${widget.workshopId}');
      final dd = d.data;
      _nameCtrl.text = dd['name'] ?? '';
      _contactCtrl.text = dd['contactPerson'] ?? '';
      _phoneCtrl.text = dd['phone'] ?? '';
      _emailCtrl.text = dd['email'] ?? '';
      _addressCtrl.text = dd['address'] ?? '';
      _isInternal = dd['isInternal'] as bool? ?? false;
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{
        'name': _nameCtrl.text.trim(),
        'contactPerson': _contactCtrl.text.trim().isEmpty ? null : _contactCtrl.text.trim(),
        'phone': _phoneCtrl.text.trim(),
        'email': _emailCtrl.text.trim().isEmpty ? null : _emailCtrl.text.trim(),
        'address': _addressCtrl.text.trim().isEmpty ? null : _addressCtrl.text.trim(),
        'isInternal': _isInternal,
      };
      if (_isEditing) {
        await dio.put('fleet/workshops/${widget.workshopId}', data: body);
      } else {
        await dio.post('fleet/workshops', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    _contactCtrl.dispose();
    _phoneCtrl.dispose();
    _emailCtrl.dispose();
    _addressCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar taller' : 'Nuevo taller')),
      body: SingleChildScrollView(
        padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: ZAlertCard(message: _error!, severity: 'high'),
                ),
              _buildSection('Información del Taller', Icons.precision_manufacturing_outlined, [
                ZTextField(controller: _nameCtrl, label: 'Nombre', prefix: const Icon(Icons.business_outlined), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _contactCtrl, label: 'Persona de contacto', prefix: const Icon(Icons.person_outline)),
                const SizedBox(height: 12),
                ZTextField(controller: _phoneCtrl, label: 'Teléfono', prefix: const Icon(Icons.phone_outlined), keyboardType: TextInputType.phone, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _emailCtrl, label: 'Correo electrónico', prefix: const Icon(Icons.email_outlined), keyboardType: TextInputType.emailAddress),
                const SizedBox(height: 12),
                ZTextField(controller: _addressCtrl, label: 'Dirección', prefix: const Icon(Icons.location_on_outlined)),
                const SizedBox(height: 12),
                SwitchListTile(
                  title: const Text('Taller interno'),
                  subtitle: const Text('Pertenece a la empresa'),
                  value: _isInternal,
                  onChanged: (v) => setState(() => _isInternal = v),
                  activeThumbColor: ZColors.moduleFleet,
                ),
              ]),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear taller',
                onPressed: _save,
                isLoading: _loading,
                icon: _isEditing ? Icons.save_outlined : Icons.add_circle_outline,
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSection(String title, IconData icon, List<Widget> children) {
    return ZCard(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(icon, size: 20, color: ZColors.moduleFleet),
              const SizedBox(width: 8),
              Text(title, style: Theme.of(context).textTheme.titleMedium),
            ],
          ),
          const SizedBox(height: 16),
          ...children,
        ],
      ),
    );
  }
}
