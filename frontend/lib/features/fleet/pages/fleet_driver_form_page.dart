import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../core/providers/company_branch_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_catalog_provider.dart';

final class FleetDriverFormPage extends ConsumerStatefulWidget {
  final String? driverId;
  const FleetDriverFormPage({super.key, this.driverId});

  @override
  ConsumerState<FleetDriverFormPage> createState() => _FleetDriverFormPageState();
}

final class _FleetDriverFormPageState extends ConsumerState<FleetDriverFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameCtrl = TextEditingController();
  final _lastNameCtrl = TextEditingController();
  final _idDocCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();
  final _licenseNumberCtrl = TextEditingController();
  final _additionalCatsCtrl = TextEditingController();

  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.driverId != null;

  String? _licenseCategoryId;
  String? _branchId;
  DateTime? _birthDate;
  DateTime? _licenseIssueDate;
  DateTime? _licenseExpiryDate;
  DateTime? _hireDate;
  String? _status;

  static const _statusOptions = ['Active', 'Inactive', 'Suspended', 'OnLeave'];

  @override
  void initState() {
    super.initState();
    _hireDate = DateTime.now();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/drivers/${widget.driverId}');
      final d = r.data;
      _firstNameCtrl.text = d['firstName'] ?? '';
      _lastNameCtrl.text = d['lastName'] ?? '';
      _idDocCtrl.text = d['idDocument'] ?? '';
      _phoneCtrl.text = d['phone'] ?? '';
      _emailCtrl.text = d['email'] ?? '';
      _addressCtrl.text = d['address'] ?? '';
      _licenseNumberCtrl.text = d['licenseNumber'] ?? '';
      _additionalCatsCtrl.text = d['additionalCategories'] ?? '';
      _licenseCategoryId = d['licenseCategoryId'] as String?;
      _branchId = d['branchId'] as String?;
      _status = d['status'] as String?;
      _birthDate = d['birthDate'] != null ? DateTime.tryParse(d['birthDate'] as String) : null;
      _licenseIssueDate = d['licenseIssueDate'] != null ? DateTime.tryParse(d['licenseIssueDate'] as String) : null;
      _licenseExpiryDate = d['licenseExpiryDate'] != null ? DateTime.tryParse(d['licenseExpiryDate'] as String) : null;
      _hireDate = d['hireDate'] != null ? DateTime.tryParse(d['hireDate'] as String) : DateTime.now();
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar conductor');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{
        'firstName': _firstNameCtrl.text.trim(),
        'lastName': _lastNameCtrl.text.trim(),
        'idDocument': _idDocCtrl.text.trim(),
        'phone': _phoneCtrl.text.trim(),
        'email': _emailCtrl.text.trim(),
        'address': _addressCtrl.text.trim().isEmpty ? null : _addressCtrl.text.trim(),
        'licenseNumber': _licenseNumberCtrl.text.trim(),
        'additionalCategories': _additionalCatsCtrl.text.trim().isEmpty ? null : _additionalCatsCtrl.text.trim(),
        'birthDate': _birthDate?.toIso8601String().substring(0, 10),
        'licenseIssueDate': _licenseIssueDate?.toIso8601String().substring(0, 10),
        'licenseExpiryDate': _licenseExpiryDate?.toIso8601String().substring(0, 10),
        'hireDate': _hireDate?.toIso8601String().substring(0, 10),
        'licenseCategoryId': _licenseCategoryId,
        'branchId': _branchId,
        'status': _status,
      };
      if (_isEditing) {
        await dio.put('fleet/drivers/${widget.driverId}', data: body);
      } else {
        await dio.post('fleet/drivers', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _pickDate({
    required DateTime? current,
    required ValueChanged<DateTime> onPicked,
    DateTime? firstDate,
    DateTime? lastDate,
  }) async {
    final date = await showDatePicker(
      context: context,
      initialDate: current ?? DateTime.now(),
      firstDate: firstDate ?? DateTime(1950),
      lastDate: lastDate ?? DateTime.now().add(const Duration(days: 365 * 10)),
    );
    if (date != null) onPicked(date);
  }

  Widget _dateField({
    required String label,
    required IconData icon,
    required DateTime? value,
    required ValueChanged<DateTime> onChanged,
    DateTime? firstDate,
    DateTime? lastDate,
  }) {
    return InkWell(
      onTap: () => _pickDate(current: value, onPicked: onChanged, firstDate: firstDate, lastDate: lastDate),
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          prefixIcon: Icon(icon),
          suffixIcon: Icon(value != null ? Icons.check_circle : Icons.date_range, color: value != null ? Colors.green : null),
        ),
        child: Text(
          value != null ? '${value.day}/${value.month}/${value.year}' : 'Seleccionar fecha',
        ),
      ),
    );
  }

  @override
  void dispose() {
    _firstNameCtrl.dispose();
    _lastNameCtrl.dispose();
    _idDocCtrl.dispose();
    _phoneCtrl.dispose();
    _emailCtrl.dispose();
    _addressCtrl.dispose();
    _licenseNumberCtrl.dispose();
    _additionalCatsCtrl.dispose();
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
              if (_error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: ZAlertCard(message: _error!, severity: 'high'),
                ),
              _buildSection('Información Personal', Icons.person_outline, [
                ZTextField(controller: _firstNameCtrl, label: 'Nombres', prefix: const Icon(Icons.person_outline), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _lastNameCtrl, label: 'Apellidos', prefix: const Icon(Icons.person_outline), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _idDocCtrl, label: 'Documento de identidad', prefix: const Icon(Icons.badge_outlined), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                _dateField(
                  label: 'Fecha de nacimiento',
                  icon: Icons.cake_outlined,
                  value: _birthDate,
                  onChanged: (d) => setState(() => _birthDate = d),
                  lastDate: DateTime.now(),
                ),
                const SizedBox(height: 12),
                ZTextField(controller: _phoneCtrl, label: 'Teléfono', prefix: const Icon(Icons.phone_outlined), keyboardType: TextInputType.phone, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _emailCtrl, label: 'Correo electrónico', prefix: const Icon(Icons.email_outlined), keyboardType: TextInputType.emailAddress, validator: (v) {
                  if (v == null || v.isEmpty) return 'Requerido';
                  if (!v.contains('@')) return 'Correo inválido';
                  return null;
                }, suffix: const Icon(Icons.email_outlined)),
                const SizedBox(height: 12),
                ZTextField(controller: _addressCtrl, label: 'Dirección', prefix: const Icon(Icons.location_on_outlined)),
              ]),
              const SizedBox(height: 20),
              _buildSection('Licencia', Icons.credit_card_outlined, [
                ZTextField(controller: _licenseNumberCtrl, label: 'Número de licencia', prefix: const Icon(Icons.credit_card_outlined), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZAsyncRenderer(
                  value: ref.watch(driverLicenseCategoryListProvider),
                  builder: (items) => ZDropdownFormField<String>(
                    value: _licenseCategoryId,
                    label: 'Categoría de licencia',
                    prefixIcon: Icons.category_outlined,
                    items: items.map((e) => DropdownMenuItem(value: e.id, child: Text(e.name))).toList(),
                    onChanged: (v) => setState(() => _licenseCategoryId = v),
                    validator: (v) => v == null ? 'Requerido' : null,
                  ),
                ),
                const SizedBox(height: 12),
                _dateField(
                  label: 'Fecha de expedición',
                  icon: Icons.edit_calendar_outlined,
                  value: _licenseIssueDate,
                  onChanged: (d) => setState(() => _licenseIssueDate = d),
                ),
                const SizedBox(height: 12),
                _dateField(
                  label: 'Fecha de vencimiento',
                  icon: Icons.event_outlined,
                  value: _licenseExpiryDate,
                  onChanged: (d) => setState(() => _licenseExpiryDate = d),
                  firstDate: DateTime.now().subtract(const Duration(days: 365)),
                ),
                const SizedBox(height: 12),
                ZTextField(controller: _additionalCatsCtrl, label: 'Categorías adicionales', prefix: const Icon(Icons.category_outlined)),
              ]),
              const SizedBox(height: 20),
              _buildSection('Asignación', Icons.assignment_outlined, [
                ZAsyncRenderer(
                  value: ref.watch(headerBranchListProvider),
                  builder: (items) => ZDropdownFormField<String>(
                    value: _branchId,
                    label: 'Sucursal',
                    prefixIcon: Icons.business_outlined,
                    items: items.map((e) => DropdownMenuItem(value: e['id'] as String, child: Text(e['name'] as String? ?? ''))).toList(),
                    onChanged: (v) => setState(() => _branchId = v),
                    validator: (v) => v == null ? 'Requerido' : null,
                  ),
                ),
                const SizedBox(height: 12),
                _dateField(
                  label: 'Fecha de contratación',
                  icon: Icons.work_outline,
                  value: _hireDate,
                  onChanged: (d) => setState(() => _hireDate = d),
                  lastDate: DateTime.now(),
                ),
                const SizedBox(height: 12),
                ZDropdownFormField<String>(
                  value: _status,
                  label: 'Estado',
                  prefixIcon: Icons.circle_outlined,
                  items: _statusOptions.map((e) => DropdownMenuItem(value: e, child: Text(e))).toList(),
                  onChanged: (v) => setState(() => _status = v),
                ),
              ]),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear conductor',
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
