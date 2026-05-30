import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../providers/company_settings_provider.dart';

class CompanySettingsPage extends ConsumerStatefulWidget {
  const CompanySettingsPage({super.key});

  @override
  ConsumerState<CompanySettingsPage> createState() => _CompanySettingsPageState();
}

class _CompanySettingsPageState extends ConsumerState<CompanySettingsPage> {
  final _nameCtrl = TextEditingController();
  final _legalCtrl = TextEditingController();
  final _taxCtrl = TextEditingController();
  final _vacationDaysCtrl = TextEditingController();
  final _toleranceCtrl = TextEditingController();
  final _workingHoursCtrl = TextEditingController();
  final _currencyCtrl = TextEditingController();
  final _timezoneCtrl = TextEditingController();

  @override
  void dispose() {
    _nameCtrl.dispose();
    _legalCtrl.dispose();
    _taxCtrl.dispose();
    _vacationDaysCtrl.dispose();
    _toleranceCtrl.dispose();
    _workingHoursCtrl.dispose();
    _currencyCtrl.dispose();
    _timezoneCtrl.dispose();
    super.dispose();
  }

  Future<void> _saveCompany() async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('/companies/current', data: {
        'name': _nameCtrl.text,
        'legalName': _legalCtrl.text,
        'taxId': _taxCtrl.text,
        'currency': _currencyCtrl.text,
        'timezone': _timezoneCtrl.text,
      });
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Empresa actualizada')));
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
    }
  }

  Future<void> _saveSettings() async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('/companies/settings', data: {
        'vacationDaysPerYear': int.tryParse(_vacationDaysCtrl.text),
        'lateToleranceMinutes': int.tryParse(_toleranceCtrl.text),
        'workingHoursPerDay': double.tryParse(_workingHoursCtrl.text),
        'currency': _currencyCtrl.text,
        'timezone': _timezoneCtrl.text,
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
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Información de la Empresa', style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 12),
                  companyAsync.whenOrNull(data: (c) {
                    _nameCtrl.text = c['name'] ?? '';
                    _legalCtrl.text = c['legalName'] ?? '';
                    _taxCtrl.text = c['taxId'] ?? '';
                    return Column(
                      children: [
                        TextField(controller: _nameCtrl, decoration: const InputDecoration(labelText: 'Nombre', border: OutlineInputBorder())),
                        const SizedBox(height: 12),
                        TextField(controller: _legalCtrl, decoration: const InputDecoration(labelText: 'Razón Social', border: OutlineInputBorder())),
                        const SizedBox(height: 12),
                        TextField(controller: _taxCtrl, decoration: const InputDecoration(labelText: 'RUC/NIT', border: OutlineInputBorder())),
                        const SizedBox(height: 12),
                        FilledButton(onPressed: _saveCompany, child: const Text('Guardar Empresa')),
                      ],
                    );
                  }) ?? const Center(child: CircularProgressIndicator()),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Configuración General', style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 12),
                  settingsAsync.whenOrNull(data: (s) {
                    _vacationDaysCtrl.text = (s['vacationDaysPerYear'] ?? 15).toString();
                    _toleranceCtrl.text = (s['lateToleranceMinutes'] ?? 15).toString();
                    _workingHoursCtrl.text = (s['workingHoursPerDay'] ?? 8).toString();
                    _currencyCtrl.text = s['currency'] ?? 'NIO';
                    _timezoneCtrl.text = s['timezone'] ?? 'America/Managua';
                    return Column(
                      children: [
                        TextField(controller: _vacationDaysCtrl, decoration: const InputDecoration(labelText: 'Días de Vacaciones/Año', border: OutlineInputBorder()), keyboardType: TextInputType.number),
                        const SizedBox(height: 12),
                        TextField(controller: _toleranceCtrl, decoration: const InputDecoration(labelText: 'Tolerancia (minutos)', border: OutlineInputBorder()), keyboardType: TextInputType.number),
                        const SizedBox(height: 12),
                        TextField(controller: _workingHoursCtrl, decoration: const InputDecoration(labelText: 'Horas Laborales/Día', border: OutlineInputBorder()), keyboardType: TextInputType.number),
                        const SizedBox(height: 12),
                        TextField(controller: _currencyCtrl, decoration: const InputDecoration(labelText: 'Moneda', border: OutlineInputBorder())),
                        const SizedBox(height: 12),
                        TextField(controller: _timezoneCtrl, decoration: const InputDecoration(labelText: 'Zona Horaria', border: OutlineInputBorder())),
                        const SizedBox(height: 12),
                        FilledButton(onPressed: _saveSettings, child: const Text('Guardar Configuración')),
                      ],
                    );
                  }) ?? const Center(child: CircularProgressIndicator()),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Card(
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
}
