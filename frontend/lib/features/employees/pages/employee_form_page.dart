import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../departments/providers/department_provider.dart';

class EmployeeFormPage extends ConsumerStatefulWidget {
  final String? employeeId;
  const EmployeeFormPage({super.key, this.employeeId});

  @override
  ConsumerState<EmployeeFormPage> createState() => _EmployeeFormPageState();
}

class _EmployeeFormPageState extends ConsumerState<EmployeeFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameCtrl = TextEditingController();
  final _lastNameCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _positionCtrl = TextEditingController();
  final _salaryCtrl = TextEditingController();
  final _bankNameCtrl = TextEditingController();
  final _bankAccountCtrl = TextEditingController();
  String? _selectedDeptId;
  String _salaryType = 'monthly';
  String _bankAccountType = 'ahorro';
  bool _loading = false;
  String? _error;
  bool _isEditing = false;

  @override
  void initState() {
    super.initState();
    _isEditing = widget.employeeId != null;
    Future.microtask(() => ref.read(departmentProvider.notifier).load());
    if (_isEditing) _loadEmployee();
  }

  Future<void> _loadEmployee() async {
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('employees/${widget.employeeId}');
      final data = response.data;
      _firstNameCtrl.text = data['firstName'] ?? '';
      _lastNameCtrl.text = data['lastName'] ?? '';
      _emailCtrl.text = data['email'] ?? '';
      _phoneCtrl.text = data['phone'] ?? '';
      _positionCtrl.text = data['position'] ?? '';
      _selectedDeptId = data['departmentId'] as String?;
      _salaryType = data['salaryType'] as String? ?? 'monthly';
      _salaryCtrl.text = data['salary']?.toString() ?? '';
      _bankNameCtrl.text = data['bankName'] ?? '';
      _bankAccountCtrl.text = data['bankAccountNumber'] ?? '';
      _bankAccountType = data['bankAccountType'] as String? ?? 'ahorro';
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar empleado');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });

    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'firstName': _firstNameCtrl.text.trim(),
        'lastName': _lastNameCtrl.text.trim(),
        'email': _emailCtrl.text.trim(),
        'phone': _phoneCtrl.text.trim(),
        'position': _positionCtrl.text.trim(),
        'departmentId': _selectedDeptId,
        'salaryType': _salaryType,
        'bankName': _bankNameCtrl.text.trim(),
        'bankAccountNumber': _bankAccountCtrl.text.trim(),
        'bankAccountType': _bankAccountType,
        if (_salaryCtrl.text.isNotEmpty) 'salary': double.tryParse(_salaryCtrl.text),
      };

      if (_isEditing) {
        await dio.put('employees/${widget.employeeId}', data: body);
      } else {
        await dio.post('employees', data: body);
      }
      if (mounted) context.pop(true);
    } catch (e) {
      setState(() => _error = 'Error al guardar empleado');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _firstNameCtrl.dispose();
    _lastNameCtrl.dispose();
    _emailCtrl.dispose();
    _phoneCtrl.dispose();
    _positionCtrl.dispose();
    _salaryCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final deptState = ref.watch(departmentProvider);

    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar empleado' : 'Nuevo empleado')),
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
                controller: _firstNameCtrl,
                decoration: const InputDecoration(labelText: 'Nombres', prefixIcon: Icon(Icons.person)),
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _lastNameCtrl,
                decoration: const InputDecoration(labelText: 'Apellidos', prefixIcon: Icon(Icons.person)),
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _emailCtrl,
                decoration: const InputDecoration(labelText: 'Correo', prefixIcon: Icon(Icons.email)),
                keyboardType: TextInputType.emailAddress,
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _phoneCtrl,
                decoration: const InputDecoration(labelText: 'Teléfono', prefixIcon: Icon(Icons.phone)),
                keyboardType: TextInputType.phone,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _positionCtrl,
                decoration: const InputDecoration(labelText: 'Cargo', prefixIcon: Icon(Icons.work)),
              ),
              const SizedBox(height: 16),
              DropdownButtonFormField<String>(
                initialValue: _selectedDeptId,
                decoration: const InputDecoration(labelText: 'Departamento', prefixIcon: Icon(Icons.business)),
                items: deptState.items
                    .where((d) => d.isActive)
                    .map((d) => DropdownMenuItem(value: d.id, child: Text(d.name)))
                    .toList(),
                onChanged: (v) => setState(() => _selectedDeptId = v),
              ),
              const SizedBox(height: 16),
              DropdownButtonFormField<String>(
                initialValue: _salaryType,
                decoration: const InputDecoration(labelText: 'Tipo salario', prefixIcon: Icon(Icons.attach_money)),
                items: const [
                  DropdownMenuItem(value: 'monthly', child: Text('Mensual')),
                  DropdownMenuItem(value: 'biweekly', child: Text('Quincenal')),
                  DropdownMenuItem(value: 'hourly', child: Text('Por hora')),
                ],
                onChanged: (v) => setState(() => _salaryType = v!),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _salaryCtrl,
                decoration: const InputDecoration(labelText: 'Salario', prefixIcon: Icon(Icons.money)),
                keyboardType: TextInputType.number,
              ),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _loading ? null : _save,
                child: _loading
                    ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : Text(_isEditing ? 'Actualizar' : 'Crear empleado'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
