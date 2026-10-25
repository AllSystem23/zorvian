import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';
import '../../departments/providers/department_provider.dart';
import '../../../shared/ds/ds.dart';
import '../../../core/widgets/responsive_layout.dart';
import '../identification_documents.dart';

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
  final _identificationNumberCtrl = TextEditingController();
  final _employeeCodeCtrl = TextEditingController();
  final _nationalityCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();
  final _cityCtrl = TextEditingController();
  final _emergencyContactCtrl = TextEditingController();
  final _emergencyPhoneCtrl = TextEditingController();
  final _photoUrlCtrl = TextEditingController();
  final _terminationReasonCtrl = TextEditingController();
  
  // State
  int _currentStep = 0;
  String? _selectedDeptId;
  String? _branchId;
  String _salaryType = 'monthly';
  String _bankAccountType = 'ahorro';
  String _collaboratorType = 'employee';
  String _identificationType = 'cedula_ni';
  String _maritalStatus = 'soltero';
  String _countryCode = 'NIC';
  String? _selectedContractId;
  String _status = 'active';
  DateTime? _hireDate;
  DateTime? _dateOfBirth;
  DateTime? _registrationDate;
  DateTime? _terminationDate;
  String _gender = 'M';
  bool _loading = false;
  String? _error;
  bool _isEditing = false;
  /// true mientras se carga el empleado en modo edición (estado de carga del form)
  bool _loadingData = false;
  bool _hasChanges = false;
  
  // Payroll deduction fields
  bool _deductInss = true;
  bool _deductIr = true;
  bool _deductAguinaldo = true;
  bool _isTrustPosition = false;
  bool _isDomesticWorkerWithBoard = false;

  final List<String> _stepLabels = [
    'Datos Personales',
    'Contacto y Dirección',
    'Posición y Salario',
    'Información Bancaria',
    'Configuración de Nómina'
  ];

  static const List<String> _maritalStatusOptions = [
    'soltero', 'casado', 'union_libre', 'divorciado', 'viudo'
  ];

  static const List<String> _countryOptions = [
    'NIC', 'HND', 'GTM', 'SLV', 'CRI', 'PAN', 'BLZ', 'DOM', 'MEX', 'USA', 'ESP'
  ];

  @override
  void initState() {
    super.initState();
    _isEditing = widget.employeeId != null;
    _loadingData = _isEditing;
    Future.microtask(() => ref.read(departmentProvider.notifier).load());
    if (_isEditing) {
      _loadEmployee();
    } else {
      _branchId = ref.read(companyBranchProvider).branchId;
    }
    
    // Track changes for "auto-save" simulation
    _firstNameCtrl.addListener(_onFieldChanged);
    _lastNameCtrl.addListener(_onFieldChanged);
    _emailCtrl.addListener(_onFieldChanged);
    _phoneCtrl.addListener(_onFieldChanged);
    _positionCtrl.addListener(_onFieldChanged);
    _salaryCtrl.addListener(_onFieldChanged);
    _bankNameCtrl.addListener(_onFieldChanged);
    _bankAccountCtrl.addListener(_onFieldChanged);
    _identificationNumberCtrl.addListener(_onFieldChanged);
    _employeeCodeCtrl.addListener(_onFieldChanged);
    _nationalityCtrl.addListener(_onFieldChanged);
    _addressCtrl.addListener(_onFieldChanged);
    _cityCtrl.addListener(_onFieldChanged);
    _emergencyContactCtrl.addListener(_onFieldChanged);
    _emergencyPhoneCtrl.addListener(_onFieldChanged);
    _photoUrlCtrl.addListener(_onFieldChanged);
    _terminationReasonCtrl.addListener(_onFieldChanged);
  }

  void _onFieldChanged() {
    if (!_hasChanges) setState(() => _hasChanges = true);
  }

  Future<void> _loadEmployee() async {
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('employees/${widget.employeeId}');
      if (!mounted) return;
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
        _identificationType = data['identificationType'] as String? ?? 'cedula_ni';
        _identificationNumberCtrl.text = data['identificationNumber'] ?? '';
        _collaboratorType = data['collaboratorType'] as String? ?? 'employee';
        _selectedContractId = data['contractId'] as String?;
        _status = data['status'] as String? ?? 'active';
        _gender = data['gender'] as String? ?? 'M';
        if (data['dateOfBirth'] != null) {
          try { _dateOfBirth = DateTime.parse(data['dateOfBirth'] as String); } catch (_) {}
        }
        if (data['hireDate'] != null) {
          try { _hireDate = DateTime.parse(data['hireDate'] as String); } catch (_) {}
        }
        _deductInss = data['deductInss'] ?? true;
        _deductIr = data['deductIr'] ?? true;
        _deductAguinaldo = data['deductAguinaldo'] ?? true;
        _isTrustPosition = data['isTrustPosition'] ?? false;
        _isDomesticWorkerWithBoard = data['isDomesticWorkerWithBoard'] ?? false;
        _employeeCodeCtrl.text = data['employeeCode'] ?? '';
        _nationalityCtrl.text = data['nationality'] ?? '';
        _addressCtrl.text = data['address'] ?? '';
        _cityCtrl.text = data['city'] ?? '';
        _emergencyContactCtrl.text = data['emergencyContact'] ?? '';
        _emergencyPhoneCtrl.text = data['emergencyPhone'] ?? '';
        _photoUrlCtrl.text = data['photoUrl'] ?? '';
        _terminationReasonCtrl.text = data['terminationReason'] ?? '';
        _maritalStatus = data['maritalStatus'] as String? ?? 'soltero';
        _countryCode = data['countryCode'] as String? ?? 'NIC';
        _branchId = data['branchId'] as String?;
        if (data['registrationDate'] != null) {
          try { _registrationDate = DateTime.parse(data['registrationDate'] as String); } catch (_) {}
        }
        if (data['terminationDate'] != null) {
          try { _terminationDate = DateTime.parse(data['terminationDate'] as String); } catch (_) {}
        }
        _hasChanges = false;
        _loadingData = false;
      });
    } catch (_) {
      setState(() {
        _error = 'Error al cargar trabajador';
        _loadingData = false;
      });
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
        if (_employeeCodeCtrl.text.trim().isNotEmpty) 'employeeCode': _employeeCodeCtrl.text.trim(),
        'position': _positionCtrl.text.trim(),
        'departmentId': _selectedDeptId,
        if (_branchId != null) 'branchId': _branchId,
        'collaboratorType': _collaboratorType,
        'salaryType': _salaryType,
        if (_selectedContractId != null) 'contractId': _selectedContractId,
        'bankName': _bankNameCtrl.text.trim(),
        'bankAccountNumber': _bankAccountCtrl.text.trim(),
        'bankAccountType': _bankAccountType,
        'identificationType': _identificationType,
        'identificationNumber': _identificationNumberCtrl.text.trim(),
        'gender': _gender,
        'nationality': _nationalityCtrl.text.trim(),
        'maritalStatus': _maritalStatus,
        'address': _addressCtrl.text.trim(),
        'city': _cityCtrl.text.trim(),
        'emergencyContact': _emergencyContactCtrl.text.trim(),
        'emergencyPhone': _emergencyPhoneCtrl.text.trim(),
        'photoUrl': _photoUrlCtrl.text.trim(),
        'countryCode': _countryCode,
        if (_dateOfBirth != null) 'dateOfBirth': _dateOfBirth!.toIso8601String().substring(0, 10),
        if (_registrationDate != null) 'registrationDate': _registrationDate!.toIso8601String().substring(0, 10),
        if (_salaryCtrl.text.isNotEmpty) 'salary': double.tryParse(_salaryCtrl.text),
        if (_isEditing) 'status': _status,
        if (_isEditing && _terminationDate != null)
          'terminationDate': _terminationDate!.toIso8601String().substring(0, 10),
        if (_isEditing && _terminationReasonCtrl.text.trim().isNotEmpty)
          'terminationReason': _terminationReasonCtrl.text.trim(),
        if (_hireDate != null) 'hireDate': _hireDate!.toIso8601String().substring(0, 10),
        'deductInss': _deductInss,
        'deductIr': _deductIr,
        'deductAguinaldo': _deductAguinaldo,
        'isTrustPosition': _isTrustPosition,
        'isDomesticWorkerWithBoard': _isDomesticWorkerWithBoard,
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
      setState(() => _error = 'Error al guardar trabajador');
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
    _identificationNumberCtrl.dispose();
    _employeeCodeCtrl.dispose();
    _nationalityCtrl.dispose();
    _addressCtrl.dispose();
    _cityCtrl.dispose();
    _emergencyContactCtrl.dispose();
    _emergencyPhoneCtrl.dispose();
    _photoUrlCtrl.dispose();
    _terminationReasonCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return Scaffold(
      backgroundColor: isDark ? ZColors.darkBackground : ZColors.neutral50,
      body: Column(
        children: [
          // UNSAVED CHANGES INDICATOR
          if (_hasChanges)
            Container(
              width: double.infinity,
              color: ZColors.warning.withAlpha(30),
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.cloud_upload_outlined, size: 16, color: ZColors.warning),
                  const SizedBox(width: 8),
                  Text('Cambios sin guardar', style: ZTypography.labelSmall.copyWith(color: ZColors.warning)),
                ],
              ),
            ),
          
          // STEPPER HEADER
          Container(
            color: isDark ? ZColors.darkSurface : Colors.white,
            padding: EdgeInsets.symmetric(
              vertical: 20,
              horizontal: MediaQuery.of(context).size.width < 576 ? 12 : 32,
            ),
            child: ZStepper(
              currentStep: _currentStep,
              steps: _stepLabels,
            ),
          ),
          
          Expanded(
            child: SingleChildScrollView(
              padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 800),
                  child: _loadingData ? const _EmployeeFormSkeleton() : Form(
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
      case 1: return _buildContactStep();
      case 2: return _buildStepTwo();
      case 3: return _buildStepThree();
      case 4: return _buildStepFour();
      default: return const SizedBox.shrink();
    }
  }

  Widget _buildContactStep() {
    return Column(
      key: const ValueKey(1),
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _buildSectionHeader('Contacto y Dirección', 'Datos de contacto, ubicación y emergencia.'),
        const SizedBox(height: 24),
        ZCard(
          padding: const EdgeInsets.all(24),
          child: ResponsiveGrid(
            mobileColumns: 1,
            tabletColumns: 2,
            desktopColumns: 2,
            children: [
              TextFormField(
                controller: _nationalityCtrl,
                decoration: const InputDecoration(labelText: 'Nacionalidad', prefixIcon: Icon(Icons.flag_outlined)),
              ),
              ZDropdownFormField<String>(
                value: _maritalStatus,
                label: 'Estado Civil',
                prefixIcon: Icons.favorite_border,
                items: _maritalStatusOptions
                    .map((s) => DropdownMenuItem(value: s, child: Text(_maritalStatusLabel(s))))
                    .toList(),
                onChanged: (v) => setState(() { _maritalStatus = v!; _onFieldChanged(); }),
              ),
              TextFormField(
                controller: _addressCtrl,
                decoration: const InputDecoration(labelText: 'Dirección', prefixIcon: Icon(Icons.home_outlined)),
              ),
              TextFormField(
                controller: _cityCtrl,
                decoration: const InputDecoration(labelText: 'Ciudad', prefixIcon: Icon(Icons.location_city_outlined)),
              ),
              ZDropdownFormField<String>(
                value: _countryCode,
                label: 'País',
                prefixIcon: Icons.public_outlined,
                items: _countryOptions
                    .map((c) => DropdownMenuItem(value: c, child: Text(c)))
                    .toList(),
                onChanged: (v) => setState(() { _countryCode = v!; _onFieldChanged(); }),
              ),
              _DatePickerField(
                label: 'Fecha de Registro',
                icon: Icons.event_available_outlined,
                value: _registrationDate,
                firstDate: DateTime(1940),
                lastDate: DateTime.now(),
                onChanged: (d) => setState(() { _registrationDate = d; _onFieldChanged(); }),
              ),
              TextFormField(
                controller: _emergencyContactCtrl,
                decoration: const InputDecoration(labelText: 'Contacto de Emergencia', prefixIcon: Icon(Icons.contact_emergency_outlined)),
              ),
              TextFormField(
                controller: _emergencyPhoneCtrl,
                decoration: const InputDecoration(labelText: 'Teléfono de Emergencia', prefixIcon: Icon(Icons.phone_callback_outlined)),
                keyboardType: TextInputType.phone,
              ),
              TextFormField(
                controller: _photoUrlCtrl,
                decoration: const InputDecoration(labelText: 'URL de Foto (opcional)', prefixIcon: Icon(Icons.image_outlined)),
              ),
            ],
          ),
        ),
      ],
    );
  }

  String _maritalStatusLabel(String code) {
    switch (code) {
      case 'soltero': return 'Soltero(a)';
      case 'casado': return 'Casado(a)';
      case 'union_libre': return 'Unión Libre';
      case 'divorciado': return 'Divorciado(a)';
      case 'viudo': return 'Viudo(a)';
      default: return code;
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
            ZDropdownFormField<String>(
              value: _identificationType,
              label: 'Documento de Identidad',
              prefixIcon: Icons.badge_outlined,
              items: kIdentificationDocumentTypes
                  .map((t) => DropdownMenuItem(value: t.code, child: Text(t.label)))
                  .toList(),
              onChanged: (v) => setState(() { _identificationType = v!; _onFieldChanged(); }),
            ),
            TextFormField(
              controller: _identificationNumberCtrl,
              decoration: InputDecoration(
                labelText: 'Número de Documento',
                hintText: identificationDocumentByCode(_identificationType)?.hint,
                prefixIcon: const Icon(Icons.pin_outlined),
              ),
              textCapitalization: TextCapitalization.characters,
              validator: (v) => validateIdentificationNumber(_identificationType, v),
            ),
            ZDropdownFormField<String>(
              value: _gender,
              label: 'Género',
              prefixIcon: Icons.wc_outlined,
              items: const [
                DropdownMenuItem(value: 'M', child: Text('Masculino')),
                DropdownMenuItem(value: 'F', child: Text('Femenino')),
                DropdownMenuItem(value: 'other', child: Text('Otro')),
              ],
              onChanged: (v) => setState(() { _gender = v!; _onFieldChanged(); }),
            ),
            _DateOfBirthField(
              dateOfBirth: _dateOfBirth,
              onChanged: (d) => setState(() { _dateOfBirth = d; _onFieldChanged(); }),
            ),
            ZDropdownFormField<String>(
              value: _collaboratorType,
              label: 'Tipo de Trabajador',
              prefixIcon: Icons.group_outlined,
              items: const [
                DropdownMenuItem(value: 'employee', child: Text('Trabajador Interno')),
                DropdownMenuItem(value: 'service_provider', child: Text('Prestador de Servicio')),
                DropdownMenuItem(value: 'contractor', child: Text('Contratista')),
                DropdownMenuItem(value: 'consultant', child: Text('Consultor')),
                DropdownMenuItem(value: 'freelancer', child: Text('Freelancer')),
              ],
              onChanged: (v) => setState(() { _collaboratorType = v!; _onFieldChanged(); }),
            ),
          ],
        ),
        if (_collaboratorType == 'service_provider') ...[
          const SizedBox(height: 16),
          _ContractSelector(
            selectedContractId: _selectedContractId,
            onChanged: (v) => setState(() { _selectedContractId = v; _onFieldChanged(); }),
          ),
        ],
      ],
    );
  }

  Widget _buildStepTwo() {
    final deptState = ref.watch(departmentProvider);
    return Column(
      key: const ValueKey(2),
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
            ZAsyncRenderer(
              value: ref.watch(headerBranchListProvider),
              builder: (items) => ZDropdownFormField<String>(
                value: _branchId,
                label: 'Sucursal',
                prefixIcon: Icons.store_outlined,
                items: items
                    .map((b) => DropdownMenuItem(
                          value: b['id'] as String,
                          child: Text(b['name'] as String? ?? ''),
                        ))
                    .toList(),
                onChanged: (v) => setState(() { _branchId = v; _onFieldChanged(); }),
              ),
            ),
            ZDropdownFormField<String>(
              value: _salaryType,
              label: 'Tipo de Salario',
              prefixIcon: Icons.payments_outlined,
              items: const [
                DropdownMenuItem(value: 'monthly', child: Text('Fijo (Mensual)')),
                DropdownMenuItem(value: 'variable', child: Text('Variable')),
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
            if (_isEditing)
              ZDropdownFormField<String>(
                value: _status,
                label: 'Estado',
                prefixIcon: Icons.flag_outlined,
                items: const [
                  DropdownMenuItem(value: 'active', child: Text('Activo')),
                  DropdownMenuItem(value: 'inactive', child: Text('Inactivo')),
                  DropdownMenuItem(value: 'terminated', child: Text('Terminado')),
                ],
                onChanged: (v) => setState(() { _status = v!; _onFieldChanged(); }),
              ),
            _HireDateField(
              hireDate: _hireDate,
              onChanged: (d) => setState(() { _hireDate = d; _onFieldChanged(); }),
            ),
            if (_isEditing && _status == 'terminated')
              _DatePickerField(
                label: 'Fecha de Terminación',
                icon: Icons.event_busy_outlined,
                value: _terminationDate,
                firstDate: DateTime(2000),
                lastDate: DateTime.now(),
                onChanged: (d) => setState(() { _terminationDate = d; _onFieldChanged(); }),
              ),
            if (_isEditing && _status == 'terminated')
              TextFormField(
                controller: _terminationReasonCtrl,
                decoration: const InputDecoration(labelText: 'Motivo de Terminación', prefixIcon: Icon(Icons.notes_outlined)),
              ),
          ],
        ),
      ],
    );
  }

  Widget _buildStepThree() {
    return Column(
      key: const ValueKey(3),
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

  Widget _buildStepFour() {
    return Column(
      key: const ValueKey(3),
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _buildSectionHeader('Configuración de Nómina', 'Defina las deducciones y condiciones especiales para el cálculo de nómina.'),
        const SizedBox(height: 24),
        ZCard(
          child: Column(
            children: [
              _buildPayrollCheckbox(
                value: _deductInss,
                label: 'Deducir INSS',
                subtitle: 'Aplica deducción de seguro social al trabajador',
                onChanged: (v) => setState(() { _deductInss = v ?? false; _onFieldChanged(); }),
              ),
              const Divider(height: 1),
              _buildPayrollCheckbox(
                value: _deductIr,
                label: 'Deducir IR',
                subtitle: 'Aplica retención de Impuesto sobre la Renta',
                onChanged: (v) => setState(() { _deductIr = v ?? false; _onFieldChanged(); }),
              ),
              const Divider(height: 1),
              _buildPayrollCheckbox(
                value: _deductAguinaldo,
                label: 'Deducir Aguinaldo',
                subtitle: 'Aplica provisión proporcional de aguinaldo',
                onChanged: (v) => setState(() { _deductAguinaldo = v ?? false; _onFieldChanged(); }),
              ),
              const Divider(height: 1),
              _buildPayrollCheckbox(
                value: _isTrustPosition,
                label: 'Cargo de Confianza',
                subtitle: 'Aplica topes de indemnización según Art. 46 CT (Nicaragua)',
                onChanged: (v) => setState(() { _isTrustPosition = v ?? false; _onFieldChanged(); }),
              ),
              const Divider(height: 1),
              _buildPayrollCheckbox(
                value: _isDomesticWorkerWithBoard,
                label: 'Trabajador Doméstico con Board',
                subtitle: 'Aplica 1.5× en aguinaldo e indemnización (Art. 145 CT)',
                onChanged: (v) => setState(() { _isDomesticWorkerWithBoard = v ?? false; _onFieldChanged(); }),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildPayrollCheckbox({
    required bool value,
    required String label,
    required String subtitle,
    required ValueChanged<bool?> onChanged,
  }) {
    return CheckboxListTile(
      value: value,
      onChanged: (v) => onChanged(v ?? false),
      title: Text(label, style: ZTypography.titleSmall),
      subtitle: Text(subtitle, style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500)),
      controlAffinity: ListTileControlAffinity.trailing,
      dense: true,
    );
  }
}

/// Esqueleto del formulario mientras se carga el empleado en modo edición.
///
/// Sin él, el stepper se pintaba con todos los campos vacíos y sin ningún
/// indicador: parecía un formulario roto en lugar de una carga. El esqueleto
/// vive dentro del `Form`, así que mientras carga tampoco se ofrecen los
/// botones "Continuar"/"Actualizar" (pulsarlos validaría un form vacío).
class _EmployeeFormSkeleton extends StatelessWidget {
  const _EmployeeFormSkeleton();

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        ZSkeleton.header(),
        const SizedBox(height: 24),
        ZSkeleton.card(height: 260),
        const SizedBox(height: 16),
        ZSkeleton.card(height: 160),
      ],
    );
  }
}

class _ContractSelector extends ConsumerStatefulWidget {
  final String? selectedContractId;
  final ValueChanged<String?> onChanged;
  const _ContractSelector({required this.selectedContractId, required this.onChanged});

  @override
  ConsumerState<_ContractSelector> createState() => _ContractSelectorState();
}

class _ContractSelectorState extends ConsumerState<_ContractSelector> {
  late final Future<dynamic> _contractsFuture;

  @override
  void initState() {
    super.initState();
    final dio = ref.read(dioClientProvider);
    _contractsFuture = dio.get('providers/contracts');
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder(
      future: _contractsFuture,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const SizedBox(height: 56, child: Center(child: LinearProgressIndicator()));
        }
        if (snapshot.hasError) {
          return const Text('Error al cargar contratos');
        }
        final contracts = (snapshot.data?.data as List? ?? []);
        if (contracts.isEmpty) {
          return ZAlertCard(
            message: 'No hay contratos disponibles. Cree uno primero en Prestadores > Contratos.',
            severity: 'info',
          );
        }
        return ZDropdownFormField<String>(
          value: widget.selectedContractId,
          label: 'Contrato de Servicio',
          prefixIcon: Icons.description_outlined,
          items: contracts.map<DropdownMenuItem<String>>((c) => DropdownMenuItem(
            value: c['id'] as String,
            child: Text('${c['contractNumber']} - ${c['contractName']}'),
          )).toList(),              onChanged: widget.onChanged,
        );
      },
    );
  }
}

class _HireDateField extends StatelessWidget {
  final DateTime? hireDate;
  final ValueChanged<DateTime?> onChanged;
  const _HireDateField({required this.hireDate, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: () async {
        final picked = await showDatePicker(
          context: context,
          initialDate: hireDate ?? DateTime.now(),
          firstDate: DateTime(2000),
          lastDate: DateTime.now(),
          locale: const Locale('es'),
        );
        onChanged(picked);
      },
      child: InputDecorator(
        decoration: const InputDecoration(
          labelText: 'Fecha de Contratación',
          prefixIcon: Icon(Icons.calendar_today_outlined),
        ),
        child: Text(
          hireDate != null ? '${hireDate!.day}/${hireDate!.month}/${hireDate!.year}' : 'Seleccionar fecha',
          style: TextStyle(
            color: hireDate != null ? null : Theme.of(context).hintColor,
          ),
        ),
      ),
    );
  }
}

class _DatePickerField extends StatelessWidget {
  final String label;
  final IconData icon;
  final DateTime? value;
  final DateTime firstDate;
  final DateTime lastDate;
  final ValueChanged<DateTime?> onChanged;

  const _DatePickerField({
    required this.label,
    required this.icon,
    required this.value,
    required this.firstDate,
    required this.lastDate,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: () async {
        final picked = await showDatePicker(
          context: context,
          initialDate: value ?? DateTime.now(),
          firstDate: firstDate,
          lastDate: lastDate,
          locale: const Locale('es'),
        );
        onChanged(picked);
      },
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          prefixIcon: Icon(icon),
        ),
        child: Text(
          value != null ? '${value!.day}/${value!.month}/${value!.year}' : 'Seleccionar fecha',
          style: TextStyle(
            color: value != null ? null : Theme.of(context).hintColor,
          ),
        ),
      ),
    );
  }
}

class _DateOfBirthField extends StatelessWidget {
  final DateTime? dateOfBirth;
  final ValueChanged<DateTime?> onChanged;
  const _DateOfBirthField({required this.dateOfBirth, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    return InkWell(
      onTap: () async {
        final picked = await showDatePicker(
          context: context,
          initialDate: dateOfBirth ?? DateTime(now.year - 25, now.month, now.day),
          firstDate: DateTime(1940),
          lastDate: now,
          locale: const Locale('es'),
        );
        onChanged(picked);
      },
      child: InputDecorator(
        decoration: const InputDecoration(
          labelText: 'Fecha de Nacimiento',
          prefixIcon: Icon(Icons.cake_outlined),
        ),
        child: Text(
          dateOfBirth != null
              ? '${dateOfBirth!.day}/${dateOfBirth!.month}/${dateOfBirth!.year}'
              : 'Seleccionar fecha',
          style: TextStyle(
            color: dateOfBirth != null ? null : Theme.of(context).hintColor,
          ),
        ),
      ),
    );
  }
}

