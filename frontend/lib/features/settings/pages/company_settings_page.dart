import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../../admin/widgets/company_logo_picker.dart';
import '../providers/company_settings_provider.dart';

class CompanySettingsPage extends ConsumerStatefulWidget {
  const CompanySettingsPage({super.key});

  @override
  ConsumerState<CompanySettingsPage> createState() =>
      _CompanySettingsPageState();
}

class _CompanySettingsPageState extends ConsumerState<CompanySettingsPage> {
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
  String _retentionBase = 'taxable_gross';

  /// Logo elegido y pendiente de subir (se sube al instante en esta pantalla).
  Uint8List? _logoBytes;
  String? _logoFileName;
  bool _logoBusy = false;

  void _toast(String message, {bool isError = false}) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: isError ? ZColors.danger : null,
      ),
    );
  }

  /// Abre el selector y sube el logo de inmediato (no hay formulario que guardar).
  Future<void> _pickLogo() async {
    final PickedLogo? picked;
    try {
      picked = await ref.read(logoPickerProvider)();
    } catch (e) {
      _toast(companyLogoErrorMessage(e), isError: true);
      return;
    }
    if (picked == null || !mounted) return;
    final logo = picked;

    setState(() {
      _logoBusy = true;
      _logoBytes = logo.bytes;
      _logoFileName = logo.fileName;
    });

    try {
      final info = await ref.read(companyLogoProvider.future);
      await uploadCompanyLogo(
        ref.read(dioClientProvider),
        info.id,
        bytes: logo.bytes,
        fileName: logo.fileName,
      );
      ref.invalidate(companyLogoProvider);
      _toast('Logo actualizado');
    } catch (e) {
      _toast('No se pudo guardar el logo: ${companyLogoErrorMessage(e)}',
          isError: true);
    } finally {
      if (mounted) {
        setState(() {
          _logoBusy = false;
          _logoBytes = null;
          _logoFileName = null;
        });
      }
    }
  }

  /// Quita el logo (con confirmación): el API limpia la referencia y borra el
  /// archivo, así no quedan imágenes huérfanas en el bucket.
  Future<void> _removeLogo(CompanyLogoInfo info) async {
    if (info.logoUrl == null) return;

    final confirmed = await ZConfirmDialog.show(
      context,
      title: 'Quitar logo',
      message: 'El logo se borrará de la empresa y del almacenamiento. '
          'Podrá subir otro cuando quiera.',
      confirmLabel: 'Quitar',
      icon: Icons.delete_outline,
      isDestructive: true,
    );
    if (!confirmed || !mounted) return;

    setState(() => _logoBusy = true);
    try {
      await removeCompanyLogo(ref.read(dioClientProvider), info.id);
      ref.invalidate(companyLogoProvider);
      _toast('Logo eliminado');
    } catch (e) {
      _toast('No se pudo quitar el logo: ${companyLogoErrorMessage(e)}',
          isError: true);
    } finally {
      if (mounted) setState(() => _logoBusy = false);
    }
  }

  @override
  void dispose() {
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
        'retentionBase': _retentionBase,
      });
      ref.invalidate(companySettingsProvider);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Configuración actualizada')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final settingsAsync = ref.watch(companySettingsProvider);

    return Scaffold(
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          _buildLogoCard(context),
          const SizedBox(height: 16),
          ZCard(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Configuración General',
                    style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 12),
                settingsAsync.when(
                  loading: () =>
                      const Center(child: CircularProgressIndicator()),
                  error: (e, stack) {
                    if (e.toString().contains('404')) {
                      return _buildConfigForm({});
                    }
                    return Text('Error: $e',
                        style: const TextStyle(color: Colors.red));
                  },
                  data: (s) => _buildConfigForm(s),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          ZCard(
            // `Material` transparente: `ListTile` dentro de `ZCard`
            // (DecoratedBox) dispara el assert de ink splashes en debug.
            child: Material(
              type: MaterialType.transparency,
              child: ListTile(
                leading: const Icon(Icons.event_note),
                title: const Text('Tipos de Permiso'),
                subtitle: const Text('Gestionar tipos de permiso disponibles'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () => context.push('/leave-types'),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildLogoCard(BuildContext context) {
    final logoAsync = ref.watch(companyLogoProvider);

    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Logo de la Empresa',
              style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 4),
          const Text(
            'Se muestra en el encabezado, en los documentos y en los PDF generados. '
            'Formatos: PNG, JPG o WebP (máx. 5 MB).',
            style: TextStyle(fontSize: 12, color: Colors.grey),
          ),
          const SizedBox(height: 12),
          logoAsync.when(
            loading: () => const Center(
              child: Padding(
                padding: EdgeInsets.all(12),
                child: CircularProgressIndicator(),
              ),
            ),
            error: (e, stack) => Text(
              'No se pudo cargar el logo: $e',
              style: const TextStyle(color: ZColors.danger),
            ),
            data: (info) => Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                CompanyLogoField(
                  pendingBytes: _logoBytes,
                  fileName: _logoFileName,
                  currentLogoUrl: info.logoUrl,
                  uploading: _logoBusy,
                  onPick: _pickLogo,
                  onRemove: (info.logoUrl == null)
                      ? null
                      : () => _removeLogo(info),
                ),
                if (info.id.isEmpty)
                  const Padding(
                    padding: EdgeInsets.only(top: 8),
                    child: Text(
                      'No se encontró la empresa para asociar el logo.',
                      style: TextStyle(fontSize: 12, color: ZColors.danger),
                    ),
                  ),
              ],
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
    _retentionBase = (s['retentionBase'] as String?) ?? 'taxable_gross';
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('General', style: const TextStyle(fontWeight: FontWeight.w600)),
        const SizedBox(height: 8),
        ZTextField(
            controller: _vacationDaysCtrl,
            label: 'Días de Vacaciones/Año',
            keyboardType: TextInputType.number),
        const SizedBox(height: 12),
        ZTextField(
            controller: _toleranceCtrl,
            label: 'Tolerancia (minutos)',
            keyboardType: TextInputType.number),
        const SizedBox(height: 12),
        ZTextField(
            controller: _workingHoursCtrl,
            label: 'Horas Laborales/Día',
            keyboardType: TextInputType.number),
        const SizedBox(height: 12),
        ZTextField(controller: _currencyCtrl, label: 'Moneda'),
        const SizedBox(height: 12),
        ZTextField(controller: _timezoneCtrl, label: 'Zona Horaria'),
        const SizedBox(height: 20),
        Text('Facturación / IVA',
            style: const TextStyle(fontWeight: FontWeight.w600)),
        const SizedBox(height: 8),
        // `Material` transparente: `SwitchListTile` dentro de `ZCard`
        // (DecoratedBox) dispara el assert de ink splashes en debug.
        Material(
          type: MaterialType.transparency,
          child: SwitchListTile(
            title: const Text('Aplicar IVA'),
            value: _taxEnabled,
            onChanged: (v) => setState(() => _taxEnabled = v),
            contentPadding: EdgeInsets.zero,
          ),
        ),
        if (_taxEnabled)
          ZTextField(
              controller: _taxRateCtrl,
              label: 'Tasa de IVA (ej: 0.15)',
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
              hint: '0.15 = 15%'),
        const SizedBox(height: 20),
        Text('Cobranza y Mora',
            style: const TextStyle(fontWeight: FontWeight.w600)),
        const SizedBox(height: 8),
        ZTextField(
            controller: _lateFeeRateCtrl,
            label: 'Tasa de Interés Moratorio Diario (ej: 0.001)',
            keyboardType:
                const TextInputType.numberWithOptions(decimal: true),
            hint: '0.001 = 0.1% diario'),
        const SizedBox(height: 12),
        ZTextField(
            controller: _lateFeePctCtrl,
            label: 'Porcentaje de Multa por Cuota (ej: 0.05)',
            keyboardType:
                const TextInputType.numberWithOptions(decimal: true),
            hint: '0.05 = 5%'),
        const SizedBox(height: 12),
        ZTextField(
            controller: _graceDaysCtrl,
            label: 'Días de Gracia',
            keyboardType: TextInputType.number,
            hint: '0 = sin gracia'),
        const SizedBox(height: 20),
        Text('Nómina',
            style: const TextStyle(fontWeight: FontWeight.w600)),
        const SizedBox(height: 8),
        DropdownButtonFormField<String>(
          isExpanded: true,
          initialValue: _retentionBase,
          decoration: const InputDecoration(
              labelText: 'Base de retenciones de ley (INSS/IR)'),
          items: const [
            DropdownMenuItem(
                value: 'taxable_gross',
                child: Text('Remuneración gravable (recomendado)')),
            DropdownMenuItem(
                value: 'base_salary', child: Text('Solo salario base')),
          ],
          onChanged: (v) =>
              setState(() => _retentionBase = v ?? 'taxable_gross'),
        ),
        const SizedBox(height: 6),
        const Text(
          'La remuneración gravable incluye salario + comisiones/bonos/horas extras y '
          'excluye vacaciones. "Solo salario base" aplica solo si la ley local lo permite '
          'y es bajo responsabilidad de la empresa.',
          style: TextStyle(fontSize: 12, color: Colors.grey),
        ),
        const SizedBox(height: 12),
        ZButton(text: 'Guardar Configuración', onPressed: _saveSettings),
      ],
    );
  }
}
