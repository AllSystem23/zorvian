import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';

final class ProviderFormPage extends ConsumerStatefulWidget {
  final String? providerId;
  const ProviderFormPage({super.key, this.providerId});
  @override
  ConsumerState<ProviderFormPage> createState() => _ProviderFormPageState();
}

final class _ProviderFormPageState extends ConsumerState<ProviderFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _businessNameCtrl = TextEditingController();
  final _fiscalAddressCtrl = TextEditingController();
  final _taxRegimeCtrl = TextEditingController();
  final _licenseCtrl = TextEditingController();
  final _specializationCtrl = TextEditingController();
  final _insurancePolicyCtrl = TextEditingController();
  String _serviceCategory = 'Professional';
  String _countryCode = 'NI';
  String _status = 'active';
  DateTime? _insuranceExpiration;
  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.providerId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('providers/${widget.providerId}');
      final d = r.data;
      _businessNameCtrl.text = d['businessName'] ?? '';
      _fiscalAddressCtrl.text = d['fiscalAddress'] ?? '';
      _taxRegimeCtrl.text = d['taxRegime'] ?? '';
      _licenseCtrl.text = d['professionalLicense'] ?? '';
      _specializationCtrl.text = d['specialization'] ?? '';
      _insurancePolicyCtrl.text = d['insurancePolicy'] ?? '';
      _serviceCategory = d['serviceCategory'] ?? 'Professional';
      _countryCode = d['countryCode'] ?? 'NI';
      _status = d['status'] ?? 'active';
      if (d['insuranceExpiration'] != null) {
        _insuranceExpiration = DateTime.tryParse(d['insuranceExpiration']);
      }
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar prestador');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'businessName': _businessNameCtrl.text.trim(),
        'fiscalAddress': _fiscalAddressCtrl.text.trim().isEmpty ? null : _fiscalAddressCtrl.text.trim(),
        'taxRegime': _taxRegimeCtrl.text.trim().isEmpty ? null : _taxRegimeCtrl.text.trim(),
        'professionalLicense': _licenseCtrl.text.trim().isEmpty ? null : _licenseCtrl.text.trim(),
        'specialization': _specializationCtrl.text.trim().isEmpty ? null : _specializationCtrl.text.trim(),
        'serviceCategory': _serviceCategory,
        'countryCode': _countryCode,
        'insurancePolicy': _insurancePolicyCtrl.text.trim().isEmpty ? null : _insurancePolicyCtrl.text.trim(),
        'insuranceExpiration': _insuranceExpiration?.toIso8601String().split('T')[0],
        'status': _status,
      };
      if (_isEditing) {
        await dio.put('providers/${widget.providerId}', data: body);
      } else {
        await dio.post('providers', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar prestador');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _businessNameCtrl.dispose();
    _fiscalAddressCtrl.dispose();
    _taxRegimeCtrl.dispose();
    _licenseCtrl.dispose();
    _specializationCtrl.dispose();
    _insurancePolicyCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar Prestador' : 'Nuevo Prestador')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
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
                    color: Theme.of(context).colorScheme.errorContainer,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                ),
              TextFormField(
                controller: _businessNameCtrl,
                decoration: const InputDecoration(
                  labelText: 'Nombre / Razón Social',
                  prefixIcon: Icon(Icons.business),
                ),
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: ZSpacing.md),
              TextFormField(
                controller: _specializationCtrl,
                decoration: const InputDecoration(
                  labelText: 'Especialidad',
                  prefixIcon: Icon(Icons.work),
                ),
              ),
              const SizedBox(height: ZSpacing.md),
              DropdownButtonFormField<String>(
                initialValue: _countryCode,
                decoration: const InputDecoration(
                  labelText: 'País',
                  prefixIcon: Icon(Icons.flag),
                ),
                items: const [
                  DropdownMenuItem(value: 'NI', child: Text('Nicaragua')),
                  DropdownMenuItem(value: 'HN', child: Text('Honduras')),
                  DropdownMenuItem(value: 'SV', child: Text('El Salvador')),
                  DropdownMenuItem(value: 'CR', child: Text('Costa Rica')),
                  DropdownMenuItem(value: 'GT', child: Text('Guatemala')),
                  DropdownMenuItem(value: 'PA', child: Text('Panamá')),
                ],
                onChanged: (v) => setState(() => _countryCode = v ?? 'NI'),
              ),
              const SizedBox(height: ZSpacing.md),
              DropdownButtonFormField<String>(
                initialValue: _serviceCategory,
                decoration: const InputDecoration(
                  labelText: 'Categoría de Servicio',
                  prefixIcon: Icon(Icons.category),
                ),
                items: const [
                  DropdownMenuItem(value: 'Professional', child: Text('Profesional')),
                  DropdownMenuItem(value: 'Technical', child: Text('Técnico')),
                  DropdownMenuItem(value: 'Maintenance', child: Text('Mantenimiento')),
                  DropdownMenuItem(value: 'Consulting', child: Text('Consultoría')),
                  DropdownMenuItem(value: 'Other', child: Text('Otro')),
                ],
                onChanged: (v) => setState(() => _serviceCategory = v ?? 'Professional'),
              ),
              const SizedBox(height: ZSpacing.md),
              TextFormField(
                controller: _fiscalAddressCtrl,
                decoration: const InputDecoration(
                  labelText: 'Dirección Fiscal',
                  prefixIcon: Icon(Icons.location_on),
                ),
                maxLines: 2,
              ),
              const SizedBox(height: ZSpacing.md),
              TextFormField(
                controller: _taxRegimeCtrl,
                decoration: const InputDecoration(
                  labelText: 'Régimen Fiscal',
                  prefixIcon: Icon(Icons.receipt),
                ),
              ),
              const SizedBox(height: ZSpacing.md),
              TextFormField(
                controller: _licenseCtrl,
                decoration: const InputDecoration(
                  labelText: 'Licencia Profesional',
                  prefixIcon: Icon(Icons.verified),
                ),
              ),
              const SizedBox(height: ZSpacing.md),
              TextFormField(
                controller: _insurancePolicyCtrl,
                decoration: const InputDecoration(
                  labelText: 'Póliza de Seguro',
                  prefixIcon: Icon(Icons.security),
                ),
              ),
              const SizedBox(height: ZSpacing.md),
              InkWell(
                onTap: () async {
                  final date = await showDatePicker(
                    context: context,
                    initialDate: _insuranceExpiration ?? DateTime.now(),
                    firstDate: DateTime.now(),
                    lastDate: DateTime.now().add(const Duration(days: 365 * 5)),
                  );
                  if (date != null) setState(() => _insuranceExpiration = date);
                },
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Vencimiento del Seguro',
                    prefixIcon: Icon(Icons.calendar_today),
                  ),
                  child: Text(_insuranceExpiration != null
                      ? '${_insuranceExpiration!.day}/${_insuranceExpiration!.month}/${_insuranceExpiration!.year}'
                      : 'Seleccionar fecha'),
                ),
              ),
              const SizedBox(height: ZSpacing.md),
              SwitchListTile(
                title: const Text('Activo'),
                value: _status == 'active',
                onChanged: (v) => setState(() => _status = v ? 'active' : 'inactive'),
              ),
              const SizedBox(height: ZSpacing.xl),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear Prestador',
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
