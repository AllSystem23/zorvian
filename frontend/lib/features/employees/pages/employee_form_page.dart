import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../departments/providers/department_provider.dart';
import '../../../shared/ds/ds.dart';
import '../../../core/widgets/responsive_layout.dart';

class EmployeeFormPage extends ConsumerStatefulWidget {
  final String? employeeId;
  const EmployeeFormPage({super.key, this.employeeId});

  @override
  ConsumerState<EmployeeFormPage> createState() => _EmployeeFormPageState();
}

class _EmployeeFormPageState extends ConsumerState<EmployeeFormPage> {
  final _formKey = GlobalKey<FormState>();
  
  // Controllers
  final _firstNameCtrl = TextEditingController();
  final _lastNameCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _positionCtrl = TextEditingController();
  final _salaryCtrl = TextEditingController();
  final _bankNameCtrl = TextEditingController();
  final _bankAccountCtrl = TextEditingController();
  
  // State
  int _currentStep = 0;
  String? _selectedDeptId;
  String _salaryType = 'monthly';
  String _bankAccountType = 'ahorro';
  bool _loading = false;
  String? _error;
  bool _isEditing = false;
  bool _hasChanges = false;

  final List<String> _stepLabels = [
    'Datos Personales',
    'Posición y Salario',
    'Información Bancaria'
  ];

  @override
  void initState() {
    super.initState();
    _isEditing = widget.employeeId != null;
    Future.microtask(() => ref.read(departmentProvider.notifier).load());
    if (_isEditing) _loadEmployee();
    
    // Track changes for "auto-save" simulation
    _firstNameCtrl.addListener(_onFieldChanged);
    _lastNameCtrl.addListener(_onFieldChanged);
    _emailCtrl.addListener(_onFieldChanged);
    _phoneCtrl.addListener(_onFieldChanged);
    _positionCtrl.addListener(_onFieldChanged);
    _salaryCtrl.addListener(_onFieldChanged);
    _bankNameCtrl.addListener(_onFieldChanged);
    _bankAccountCtrl.addListener(_onFieldChanged);
  }

  void _onFieldChanged() {
    if (!_hasChanges) setState(() => _hasChanges = true);
  }

  Future<void> _loadEmployee() async {
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('employees/${widget.employeeId}');
      final data = response.data;
      setState(() {
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
        _hasChanges = false;
      });
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
      if (mounted) {
        setState(() => _hasChanges = false);
        context.pop(true);
      }
    } catch (e) {
      setState(() => _error = 'Error al guardar empleado');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _nextStep() {
    if (_currentStep < _stepLabels.length - 1) {
      setState(() => _currentStep++);
    } else {
      _save();
    }
  }

  void _prevStep() {
    if (_currentStep > 0) {
      setState(() => _currentStep--);
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
    _bankNameCtrl.dispose();
    _bankAccountCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return Scaffold(
      backgroundColor: isDark ? ZColors.darkBackground : ZColors.neutral50,
      appBar: AppBar(
        title: Text(_isEditing ? 'Editar Colaborador' : 'Registro de Colaborador'),
        actions: [
          if (_hasChanges)
            Center(
              child: Padding(
                padding: const EdgeInsets.only(right: 16.0),
                child: Row(
                  children: [
                    const Icon(Icons.cloud_upload_outlined, size: 16, color: ZColors.neutral400),
                    const SizedBox(width: 8),
                    Text('Cambios sin guardar', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral400)),
                  ],
                ),
              ),
            ),
        ],
      ),
      body: Column(
        children: [
          // STEPPER HEADER
          Container(
            color: isDark ? ZColors.darkSurface : Colors.white,
            padding: const EdgeInsets.symmetric(vertical: 24, horizontal: 32),
            child: ZStepper(
              currentStep: _currentStep,
              steps: _stepLabels,
            ),
          ),
          
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 800),
                  child: Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        if (_error != null)
                          ZAlertCard(message: _error!, severity: 'high'),
                        
                        AnimatedSwitcher(
                          duration: const Duration(milliseconds: 300),
                          child: _buildCurrentStepContent(),
                        ),
                        
                        const SizedBox(height: 40),
                        
                        // NAVIGATION BUTTONS
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            if (_currentStep > 0)
                              ZButton(
                                text: 'Anterior',
                                type: ZButtonType.secondary,
                                fullWidth: false,
                                onPressed: _prevStep,
                              )
                            else
                              const SizedBox.shrink(),
                            
                            ZButton(
                              text: _currentStep == _stepLabels.length - 1 ? (_isEditing ? 'Actualizar' : 'Finalizar Registro') : 'Continuar',
                              fullWidth: false,
                              isLoading: _loading,
                              onPressed: _nextStep,
                              icon: _currentStep == _stepLabels.length - 1 ? Icons.check : Icons.arrow_forward,
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildCurrentStepContent() {
    switch (_currentStep) {
      case 0: return _buildStepOne();
      case 1: return _buildStepTwo();
      case 2: return _buildStepThree();
      default: return const SizedBox.shrink();
    }
  }

  Widget _buildStepOne() {
    return Column(
      key: const ValueKey(0),
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _buildSectionHeader('Información Personal', 'Complete los datos básicos del nuevo integrante.'),
        const SizedBox(height: 24),
        ResponsiveGrid(
          mobileColumns: 1,
          tabletColumns: 2,
          desktopColumns: 2,
          children: [
            TextFormField(
              controller: _firstNameCtrl,
              decoration: const InputDecoration(labelText: 'Nombres', prefixIcon: Icon(Icons.person_outline)),
              validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
            ),
            TextFormField(
              controller: _lastNameCtrl,
              decoration: const InputDecoration(labelText: 'Apellidos', prefixIcon: Icon(Icons.person_outline)),
              validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
            ),
            TextFormField(
              controller: _emailCtrl,
              decoration: const InputDecoration(labelText: 'Correo Electrónico', prefixIcon: Icon(Icons.email_outlined)),
              keyboardType: TextInputType.emailAddress,
              validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
            ),
            TextFormField(
              controller: _phoneCtrl,
              decoration: const InputDecoration(labelText: 'Teléfono de Contacto', prefixIcon: Icon(Icons.phone_outlined)),
              keyboardType: TextInputType.phone,
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildStepTwo() {
    final deptState = ref.watch(departmentProvider);
    return Column(
      key: const ValueKey(1),
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _buildSectionHeader('Cargo y Compensación', 'Defina el rol y la estructura salarial del colaborador.'),
        const SizedBox(height: 24),
        ResponsiveGrid(
          mobileColumns: 1,
          tabletColumns: 2,
          desktopColumns: 2,
          children: [
            TextFormField(
              controller: _positionCtrl,
              decoration: const InputDecoration(labelText: 'Cargo / Puesto', prefixIcon: Icon(Icons.work_outline)),
              validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
            ),
            ZDropdownFormField<String>(
              value: _selectedDeptId,
              label: 'Departamento',
              prefixIcon: Icons.business_outlined,
              items: deptState.items
                  .where((d) => d.isActive)
                  .map((d) => DropdownMenuItem(value: d.id, child: Text(d.name)))
                  .toList(),
              onChanged: (v) => setState(() { _selectedDeptId = v; _onFieldChanged(); }),
              validator: (v) => v == null ? 'Requerido' : null,
            ),
            ZDropdownFormField<String>(
              value: _salaryType,
              label: 'Tipo de Salario',
              prefixIcon: Icons.payments_outlined,
              items: const [
                DropdownMenuItem(value: 'monthly', child: Text('Mensual')),
                DropdownMenuItem(value: 'biweekly', child: Text('Quincenal')),
                DropdownMenuItem(value: 'hourly', child: Text('Por hora')),
              ],
              onChanged: (v) => setState(() { _salaryType = v!; _onFieldChanged(); }),
            ),
            TextFormField(
              controller: _salaryCtrl,
              decoration: const InputDecoration(labelText: 'Monto Salarial', prefixIcon: Icon(Icons.attach_money)),
              keyboardType: TextInputType.number,
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildStepThree() {
    return Column(
      key: const ValueKey(2),
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _buildSectionHeader('Información Bancaria', 'Datos necesarios para la dispersión de nómina.'),
        const SizedBox(height: 24),
        ZCard(
          padding: const EdgeInsets.all(24),
          child: ResponsiveGrid(
            mobileColumns: 1,
            tabletColumns: 2,
            desktopColumns: 2,
            children: [
              TextFormField(
                controller: _bankNameCtrl,
                decoration: const InputDecoration(labelText: 'Nombre del Banco', prefixIcon: Icon(Icons.account_balance_outlined)),
              ),
              ZDropdownFormField<String>(
                value: _bankAccountType,
                label: 'Tipo de Cuenta',
                prefixIcon: Icons.credit_card_outlined,
                items: const [
                  DropdownMenuItem(value: 'ahorro', child: Text('Ahorro')),
                  DropdownMenuItem(value: 'corriente', child: Text('Corriente')),
                ],
                onChanged: (v) => setState(() { _bankAccountType = v!; _onFieldChanged(); }),
              ),
              TextFormField(
                controller: _bankAccountCtrl,
                decoration: const InputDecoration(labelText: 'Número de Cuenta', prefixIcon: Icon(Icons.numbers_outlined)),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildSectionHeader(String title, String subtitle) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(title, style: ZTypography.headlineSmall.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 4),
        Text(subtitle, style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral500)),
      ],
    );
  }
}
