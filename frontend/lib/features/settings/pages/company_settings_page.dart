import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/company_settings_provider.dart';

class CompanySettingsPage extends ConsumerStatefulWidget {
  const CompanySettingsPage({super.key});

  @override
  ConsumerState<CompanySettingsPage> createState() => _CompanySettingsPageState();
}

class _CompanySettingsPageState extends ConsumerState<CompanySettingsPage> {
  final _nameCtrl = TextEditingController();
  final _legalCtrl = TextEditingController();
  final _taxIdCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _countryCtrl = TextEditingController();
  final _vacationDaysCtrl = TextEditingController();
  final _toleranceCtrl = TextEditingController();
  final _workingHoursCtrl = TextEditingController();
  final _currencyCtrl = TextEditingController();
  final _timezoneCtrl = TextEditingController();
  final _lateFeeRateCtrl = TextEditingController();
  final _lateFeePctCtrl = TextEditingController();
  final _graceDaysCtrl = TextEditingController();
  final _taxRateCtrl = TextEditingController();
  bool _taxEnabled = true;

  @override
  void dispose() {
    _nameCtrl.dispose();
    _legalCtrl.dispose();
    _taxIdCtrl.dispose();
    _phoneCtrl.dispose();
    _addressCtrl.dispose();
    _emailCtrl.dispose();
    _countryCtrl.dispose();
    _vacationDaysCtrl.dispose();
    _toleranceCtrl.dispose();
    _workingHoursCtrl.dispose();
    _currencyCtrl.dispose();
    _timezoneCtrl.dispose();
    _lateFeeRateCtrl.dispose();
    _lateFeePctCtrl.dispose();
    _graceDaysCtrl.dispose();
    _taxRateCtrl.dispose();
    super.dispose();
  }

  Future<void> _saveCompany() async {
    try {
      final dio = ref.read(dioClientProvider);
      final auth = ref.read(authProvider);
      final companyBranch = ref.read(companyBranchProvider);
      final data = {
        'name': _nameCtrl.text,
        'legalName': _legalCtrl.text,
        'taxId': _taxIdCtrl.text,
        'phone': _phoneCtrl.text.isNotEmpty ? _phoneCtrl.text : null,
        'address': _addressCtrl.text.isNotEmpty ? _addressCtrl.text : null,
        'email': _emailCtrl.text.isNotEmpty ? _emailCtrl.text : null,
        'country': _countryCtrl.text,
        'currency': _currencyCtrl.text,
        'timezone': _timezoneCtrl.text,
      };
      if (auth.role == 'SuperAdmin' && companyBranch.companyId != null) {
        await dio.put('companies/${companyBranch.companyId}', data: data);
      } else {
        await dio.put('companies/current', data: data);
      }
      ref.invalidate(companyInfoProvider);
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Empresa actualizada')));
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
    }
  }

  Future<void> _saveSettings() async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('companies/settings', data: {
        'vacationDaysPerYear': int.tryParse(_vacationDaysCtrl.text),
        'lateToleranceMinutes': int.tryParse(_toleranceCtrl.text),
        'workingHoursPerDay': double.tryParse(_workingHoursCtrl.text),
        'currency': _currencyCtrl.text,
        'timezone': _timezoneCtrl.text,
        'lateFeeDailyRate': double.tryParse(_lateFeeRateCtrl.text),
        'lateFeePercentage': double.tryParse(_lateFeePctCtrl.text),
        'lateFeeGracePeriod': int.tryParse(_graceDaysCtrl.text),
        'taxEnabled': _taxEnabled,
        'taxRate': double.tryParse(_taxRateCtrl.text),
      });
      ref.invalidate(companySettingsProvider);
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Configuración actualizada')));
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
    }
  }

  @override
  Widget build(BuildContext context) {
    final companyAsync = ref.watch(companyInfoProvider);
    final settingsAsync = ref.watch(companySettingsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Configuración')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          ZCard(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Información de la Empresa', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 12),
                companyAsync.when(
                  loading: () => const Center(child: CircularProgressIndicator()),
                  error: (e, stack) => Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Error: $e', style: const TextStyle(color: Colors.red)),
                      const SizedBox(height: 12),
                      ZTextField(controller: _nameCtrl, label: 'Nombre'),
                      const SizedBox(height: 12),
                      ZTextField(controller: _legalCtrl, label: 'Razón Social'),
                      const SizedBox(height: 12),
                      ZTextField(controller: _taxIdCtrl, label: 'RUC/NIT'),
                      const SizedBox(height: 12),
                      ZButton(text: 'Guardar Empresa', onPressed: _saveCompany),
                    ],
                  ),
                  data: (c) {
                    _nameCtrl.text = c['name'] ?? '';
                    _legalCtrl.text = c['legalName'] ?? '';
                    _taxIdCtrl.text = c['taxId'] ?? '';
                    _phoneCtrl.text = c['phone'] ?? '';
                    _addressCtrl.text = c['address'] ?? '';
                    _emailCtrl.text = c['email'] ?? '';
                    _countryCtrl.text = c['country'] ?? '';
                    return Column(
                      children: [
                        ZTextField(controller: _nameCtrl, label: 'Nombre'),
                        const SizedBox(height: 12),
                        ZTextField(controller: _legalCtrl, label: 'Razón Social'),
                        const SizedBox(height: 12),
                        ZTextField(controller: _taxIdCtrl, label: 'RUC/NIT'),
                        const SizedBox(height: 12),
                        ZTextField(controller: _emailCtrl, label: 'Correo Electrónico'),
                        const SizedBox(height: 12),
                        ZTextField(controller: _phoneCtrl, label: 'Teléfono'),
                        const SizedBox(height: 12),
                        ZTextField(controller: _addressCtrl, label: 'Dirección'),
                        const SizedBox(height: 12),
                        ZTextField(controller: _countryCtrl, label: 'País'),
                        const SizedBox(height: 12),
                        ZButton(text: 'Guardar Empresa', onPressed: _saveCompany),
                      ],
                    );
                  },
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          ZCard(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Configuración General', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 12),
                settingsAsync.when(
                  loading: () => const Center(child: CircularProgressIndicator()),
                  error: (e, stack) {
                    if (e.toString().contains('404')) {
                      return _buildConfigForm({});
                    }
                    return Text('Error: $e', style: const TextStyle(color: Colors.red));
                  },
                  data: (s) => _buildConfigForm(s),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          ZCard(
            child: ListTile(
              leading: const Icon(Icons.event_note),
              title: const Text('Tipos de Permiso'),
              subtitle: const Text('Gestionar tipos de permiso disponibles'),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => context.push('/leave-types'),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildConfigForm(Map<String, dynamic> s) {
    _vacationDaysCtrl.text = (s['vacationDaysPerYear'] ?? 15).toString();
    _toleranceCtrl.text = (s['lateToleranceMinutes'] ?? 15).toString();
    _workingHoursCtrl.text = (s['workingHoursPerDay'] ?? 8).toString();
    _currencyCtrl.text = s['currency'] ?? 'NIO';
    _timezoneCtrl.text = s['timezone'] ?? 'America/Managua';
    _lateFeeRateCtrl.text = (s['lateFeeDailyRate'] ?? 0.001).toString();
    _lateFeePctCtrl.text = (s['lateFeePercentage'] ?? 0.05).toString();
    _graceDaysCtrl.text = (s['lateFeeGracePeriod'] ?? 0).toString();
    _taxEnabled = s['taxEnabled'] as bool? ?? true;
    _taxRateCtrl.text = (s['taxRate'] ?? 0.15).toString();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('General', style: const TextStyle(fontWeight: FontWeight.w600)),
        const SizedBox(height: 8),
        ZTextField(controller: _vacationDaysCtrl, label: 'Días de Vacaciones/Año', keyboardType: TextInputType.number),
        const SizedBox(height: 12),
        ZTextField(controller: _toleranceCtrl, label: 'Tolerancia (minutos)', keyboardType: TextInputType.number),
        const SizedBox(height: 12),
        ZTextField(controller: _workingHoursCtrl, label: 'Horas Laborales/Día', keyboardType: TextInputType.number),
        const SizedBox(height: 12),
        ZTextField(controller: _currencyCtrl, label: 'Moneda'),
        const SizedBox(height: 12),
        ZTextField(controller: _timezoneCtrl, label: 'Zona Horaria'),
        const SizedBox(height: 20),
        Text('Facturación / IVA', style: const TextStyle(fontWeight: FontWeight.w600)),
        const SizedBox(height: 8),
        SwitchListTile(
          title: const Text('Aplicar IVA'),
          value: _taxEnabled,
          onChanged: (v) => setState(() => _taxEnabled = v),
          contentPadding: EdgeInsets.zero,
        ),
        if (_taxEnabled)
          ZTextField(controller: _taxRateCtrl, label: 'Tasa de IVA (ej: 0.15)', keyboardType: const TextInputType.numberWithOptions(decimal: true), hint: '0.15 = 15%'),
        const SizedBox(height: 20),
        Text('Cobranza y Mora', style: const TextStyle(fontWeight: FontWeight.w600)),
        const SizedBox(height: 8),
        ZTextField(controller: _lateFeeRateCtrl, label: 'Tasa de Interés Moratorio Diario (ej: 0.001)', keyboardType: const TextInputType.numberWithOptions(decimal: true), hint: '0.001 = 0.1% diario'),
        const SizedBox(height: 12),
        ZTextField(controller: _lateFeePctCtrl, label: 'Porcentaje de Multa por Cuota (ej: 0.05)', keyboardType: const TextInputType.numberWithOptions(decimal: true), hint: '0.05 = 5%'),
        const SizedBox(height: 12),
        ZTextField(controller: _graceDaysCtrl, label: 'Días de Gracia', keyboardType: TextInputType.number, hint: '0 = sin gracia'),
        const SizedBox(height: 12),
        ZButton(text: 'Guardar Configuración', onPressed: _saveSettings),
      ],
    );
  }
}
