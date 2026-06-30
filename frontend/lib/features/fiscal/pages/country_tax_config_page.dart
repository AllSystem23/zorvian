import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

// ── Model ──
class CountryTaxConfigItem {
  final String id;
  final String countryCode;
  final String countryName;
  final String currency;
  final double inssEmployeeRate;
  final double inssEmployerRate;
  final int vacationDaysPerYear;
  final double christmasBonusPercentage;
  final int indemnityDaysPerYear;
  final bool hasThirteenthMonth;
  final bool hasFourteenthMonth;
  final bool isActive;

  CountryTaxConfigItem({
    required this.id,
    required this.countryCode,
    required this.countryName,
    required this.currency,
    required this.inssEmployeeRate,
    required this.inssEmployerRate,
    required this.vacationDaysPerYear,
    required this.christmasBonusPercentage,
    required this.indemnityDaysPerYear,
    required this.hasThirteenthMonth,
    required this.hasFourteenthMonth,
    required this.isActive,
  });

  factory CountryTaxConfigItem.fromJson(Map<String, dynamic> j) =>
      CountryTaxConfigItem(
        id: j['id'] as String,
        countryCode: j['countryCode'] as String? ?? '',
        countryName: j['countryName'] as String? ?? '',
        currency: j['currency'] as String? ?? '',
        inssEmployeeRate: (j['inssEmployeeRate'] as num?)?.toDouble() ?? 0,
        inssEmployerRate: (j['inssEmployerRate'] as num?)?.toDouble() ?? 0,
        vacationDaysPerYear: j['vacationDaysPerYear'] as int? ?? 15,
        christmasBonusPercentage:
            (j['christmasBonusPercentage'] as num?)?.toDouble() ?? 0,
        indemnityDaysPerYear: j['indemnityDaysPerYear'] as int? ?? 0,
        hasThirteenthMonth: j['hasThirteenthMonth'] as bool? ?? false,
        hasFourteenthMonth: j['hasFourteenthMonth'] as bool? ?? false,
        isActive: j['isActive'] as bool? ?? true,
      );
}

// ── Notifier (Riverpod 3.x) ──
class CountryTaxConfigNotifier extends AsyncNotifier<List<CountryTaxConfigItem>> {
  @override
  Future<List<CountryTaxConfigItem>> build() => _load();

  Future<List<CountryTaxConfigItem>> _load() async {
    final dio = ref.read(dioClientProvider);
    final res = await dio.get('country-tax-configs');
    return (res.data as List)
        .map((e) => CountryTaxConfigItem.fromJson(e))
        .toList();
  }

  Future<void> load() async {
    state = await AsyncValue.guard(() => _load());
  }
}

final countryTaxConfigProvider =
    AsyncNotifierProvider<CountryTaxConfigNotifier, List<CountryTaxConfigItem>>(
        CountryTaxConfigNotifier.new);

// ── Page ──
class CountryTaxConfigPage extends ConsumerStatefulWidget {
  const CountryTaxConfigPage({super.key});

  @override
  ConsumerState<CountryTaxConfigPage> createState() => _CountryTaxConfigPageState();
}

class _CountryTaxConfigPageState extends ConsumerState<CountryTaxConfigPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  List<CountryTaxConfigItem> _filter(List<CountryTaxConfigItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items
        .where((c) =>
            c.countryName.toLowerCase().contains(q) ||
            c.countryCode.toLowerCase().contains(q))
        .toList();
  }

  void _showForm({CountryTaxConfigItem? existing}) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => _CountryTaxConfigForm(
        existing: existing,
        onSaved: () {
          ref.read(countryTaxConfigProvider.notifier).load();
        },
      ),
    );
  }

  Future<void> _confirmDelete(CountryTaxConfigItem item) async {
    final ok = await ZModal.confirm(
      context,
      title: 'Eliminar configuración',
      message: '¿Eliminar la configuración fiscal de ${item.countryName}?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
      confirmColor: Colors.red,
    );
    if (ok != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('country-tax-configs/${item.id}');
      ref.read(countryTaxConfigProvider.notifier).load();
    } catch (_) {
      if (mounted) ZToast.error(context, 'Error al eliminar');
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(countryTaxConfigProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Configuración Fiscal por País'),
      ),
      body: state.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Error: $e')),
        data: (items) {
          final filtered = _filter(items);
          return Column(
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
                child: TextField(
                  controller: _searchCtrl,
                  decoration: InputDecoration(
                    hintText: 'Buscar país o código...',
                    prefixIcon: const Icon(Icons.search),
                    suffixIcon: _searchQuery.isNotEmpty
                        ? IconButton(
                            icon: const Icon(Icons.clear),
                            onPressed: () {
                              _searchCtrl.clear();
                              setState(() => _searchQuery = '');
                            },
                          )
                        : null,
                    border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12)),
                    contentPadding: const EdgeInsets.symmetric(vertical: 0),
                  ),
                  onChanged: (v) => setState(() => _searchQuery = v),
                ),
              ),
              Expanded(
                child: filtered.isEmpty
                    ? Center(
                        child: Text(
                        _searchQuery.isNotEmpty ? 'Sin resultados' : 'No hay configuraciones fiscales',
                      ))
                    : RefreshIndicator(
                        onRefresh: () =>
                            ref.read(countryTaxConfigProvider.notifier).load(),
                        child: ListView.separated(
                          itemCount: filtered.length,
                          separatorBuilder: (_, _) => const Divider(height: 1),
                          itemBuilder: (_, i) {
                            final c = filtered[i];
                            return ListTile(
                              leading: CircleAvatar(
                                backgroundColor: c.isActive
                                    ? theme.colorScheme.primaryContainer
                                    : theme.colorScheme.surfaceContainerHighest,
                                child: Text(c.countryCode,
                                    style: TextStyle(
                                        fontWeight: FontWeight.bold,
                                        color: c.isActive
                                            ? theme.colorScheme.onPrimaryContainer
                                            : theme.colorScheme.onSurfaceVariant)),
                              ),
                              title: Text(c.countryName,
                                  style: const TextStyle(fontWeight: FontWeight.w600)),
                              subtitle: Text(
                                  '${c.currency} · INSS Emp: ${(c.inssEmployeeRate * 100).toStringAsFixed(1)}% · Vacaciones: ${c.vacationDaysPerYear}d'),
                              trailing: PopupMenuButton<String>(
                                onSelected: (v) {
                                  if (v == 'edit') _showForm(existing: c);
                                  if (v == 'delete') _confirmDelete(c);
                                },
                                itemBuilder: (_) => [
                                  const PopupMenuItem(
                                      value: 'edit', child: Text('Editar')),
                                  const PopupMenuItem(
                                      value: 'delete',
                                      child: Text('Eliminar',
                                          style: TextStyle(color: Colors.red))),
                                ],
                              ),
                            );
                          },
                        ),
                      ),
              ),
            ],
          );
        },
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => _showForm(),
        child: const Icon(Icons.add),
      ),
    );
  }
}

// ── Form Sheet ──
class _CountryTaxConfigForm extends ConsumerStatefulWidget {
  final CountryTaxConfigItem? existing;
  final VoidCallback onSaved;

  const _CountryTaxConfigForm({this.existing, required this.onSaved});

  @override
  ConsumerState<_CountryTaxConfigForm> createState() => _CountryTaxConfigFormState();
}

class _CountryTaxConfigFormState extends ConsumerState<_CountryTaxConfigForm> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _codeCtrl;
  late final TextEditingController _nameCtrl;
  late final TextEditingController _currencyCtrl;
  late final TextEditingController _inssEmpRateCtrl;
  late final TextEditingController _inssEmpMaxCtrl;
  late final TextEditingController _inssEmplrRateCtrl;
  late final TextEditingController _inssEmplrMaxCtrl;
  late final TextEditingController _irExemptCtrl;
  late final TextEditingController _vacationDaysCtrl;
  late final TextEditingController _christmasBonusCtrl;
  late final TextEditingController _indemnityDaysCtrl;
  late final TextEditingController _maxIndemnityYearsCtrl;
  late final TextEditingController _otherEmployerRateCtrl;
  late final TextEditingController _otherEmployerNameCtrl;
  bool _has13th = false;
  bool _has14th = false;
  bool _saving = false;

  bool get _isEditing => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final e = widget.existing;
    _codeCtrl = TextEditingController(text: e?.countryCode ?? '');
    _nameCtrl = TextEditingController(text: e?.countryName ?? '');
    _currencyCtrl = TextEditingController(text: e?.currency ?? '');
    _inssEmpRateCtrl = TextEditingController(text: (e?.inssEmployeeRate ?? 0).toString());
    _inssEmpMaxCtrl = TextEditingController(text: '0');
    _inssEmplrRateCtrl = TextEditingController(text: (e?.inssEmployerRate ?? 0).toString());
    _inssEmplrMaxCtrl = TextEditingController(text: '0');
    _irExemptCtrl = TextEditingController(text: '0');
    _vacationDaysCtrl = TextEditingController(text: (e?.vacationDaysPerYear ?? 15).toString());
    _christmasBonusCtrl = TextEditingController(text: (e?.christmasBonusPercentage ?? 0).toString());
    _indemnityDaysCtrl = TextEditingController(text: (e?.indemnityDaysPerYear ?? 0).toString());
    _maxIndemnityYearsCtrl = TextEditingController(text: '0');
    _otherEmployerRateCtrl = TextEditingController(text: '0');
    _otherEmployerNameCtrl = TextEditingController();
    _has13th = e?.hasThirteenthMonth ?? false;
    _has14th = e?.hasFourteenthMonth ?? false;
  }

  @override
  void dispose() {
    _codeCtrl.dispose();
    _nameCtrl.dispose();
    _currencyCtrl.dispose();
    _inssEmpRateCtrl.dispose();
    _inssEmpMaxCtrl.dispose();
    _inssEmplrRateCtrl.dispose();
    _inssEmplrMaxCtrl.dispose();
    _irExemptCtrl.dispose();
    _vacationDaysCtrl.dispose();
    _christmasBonusCtrl.dispose();
    _indemnityDaysCtrl.dispose();
    _maxIndemnityYearsCtrl.dispose();
    _otherEmployerRateCtrl.dispose();
    _otherEmployerNameCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      final data = {
        'countryCode': _codeCtrl.text.trim().toUpperCase(),
        'countryName': _nameCtrl.text.trim(),
        'currency': _currencyCtrl.text.trim(),
        'inssEmployeeRate': double.tryParse(_inssEmpRateCtrl.text) ?? 0,
        'inssEmployeeMax': double.tryParse(_inssEmpMaxCtrl.text) ?? 0,
        'inssEmployerRate': double.tryParse(_inssEmplrRateCtrl.text) ?? 0,
        'inssEmployerMax': double.tryParse(_inssEmplrMaxCtrl.text) ?? 0,
        'otherEmployerRate': double.tryParse(_otherEmployerRateCtrl.text) ?? 0,
        'otherEmployerName': _otherEmployerNameCtrl.text.isNotEmpty ? _otherEmployerNameCtrl.text : null,
        'irExemptAmount': double.tryParse(_irExemptCtrl.text) ?? 0,
        'irTableJson': '[]',
        'vacationDaysPerYear': int.tryParse(_vacationDaysCtrl.text) ?? 15,
        'christmasBonusPercentage': double.tryParse(_christmasBonusCtrl.text) ?? 0,
        'indemnityDaysPerYear': int.tryParse(_indemnityDaysCtrl.text) ?? 0,
        'maxIndemnityYears': int.tryParse(_maxIndemnityYearsCtrl.text) ?? 0,
        'hasThirteenthMonth': _has13th,
        'hasFourteenthMonth': _has14th,
      };
      if (_isEditing) {
        await dio.put('country-tax-configs/${widget.existing!.id}', data: data);
      } else {
        await dio.post('country-tax-configs', data: data);
      }
      if (mounted) {
        Navigator.pop(context);
        widget.onSaved();
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(_isEditing ? 'Configuración actualizada' : 'Configuración creada')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.85,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      expand: false,
      builder: (_, scrollCtrl) => Scaffold(
        appBar: AppBar(
          title: Text(_isEditing ? 'Editar País' : 'Nuevo País'),
          actions: [
            TextButton(
              onPressed: _saving ? null : _save,
              child: _saving
                  ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Guardar'),
            ),
          ],
        ),
        body: Form(
          key: _formKey,
          child: ListView(
            controller: scrollCtrl,
            padding: const EdgeInsets.all(16),
            children: [
              _section('Identificación del País'),
              ZTextField(controller: _codeCtrl, label: 'Código ISO (ej: NIC)', validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              ZTextField(controller: _nameCtrl, label: 'Nombre del País', validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              ZTextField(controller: _currencyCtrl, label: 'Moneda (ej: NIO)', validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 20),
              _section('INSS (Seguridad Social)'),
              ZTextField(controller: _inssEmpRateCtrl, label: 'Tasa INSS Empleado', keyboardType: const TextInputType.numberWithOptions(decimal: true), hint: '0.07 = 7%'),
              const SizedBox(height: 12),
              ZTextField(controller: _inssEmpMaxCtrl, label: 'Tope INSS Empleado', keyboardType: const TextInputType.numberWithOptions(decimal: true)),
              const SizedBox(height: 12),
              ZTextField(controller: _inssEmplrRateCtrl, label: 'Tasa INSS Empleador', keyboardType: const TextInputType.numberWithOptions(decimal: true), hint: '0.125 = 12.5%'),
              const SizedBox(height: 12),
              ZTextField(controller: _inssEmplrMaxCtrl, label: 'Tope INSS Empleador', keyboardType: const TextInputType.numberWithOptions(decimal: true)),
              const SizedBox(height: 12),
              ZTextField(controller: _otherEmployerRateCtrl, label: 'Otra Tasa Empleador', keyboardType: const TextInputType.numberWithOptions(decimal: true)),
              const SizedBox(height: 12),
              ZTextField(controller: _otherEmployerNameCtrl, label: 'Nombre Otra Tasa (ej: INATEC)'),
              const SizedBox(height: 20),
              _section('Impuesto sobre la Renta'),
              ZTextField(controller: _irExemptCtrl, label: 'Monto Exento IR', keyboardType: const TextInputType.numberWithOptions(decimal: true)),
              const SizedBox(height: 20),
              _section('Prestaciones Laborales'),
              ZTextField(controller: _vacationDaysCtrl, label: 'Días de Vacaciones/Año', keyboardType: TextInputType.number),
              const SizedBox(height: 12),
              ZTextField(controller: _christmasBonusCtrl, label: 'Aguinaldo (%)', keyboardType: const TextInputType.numberWithOptions(decimal: true), hint: '0.0833 = 8.33%'),
              const SizedBox(height: 12),
              ZTextField(controller: _indemnityDaysCtrl, label: 'Días de Indemnización/Año', keyboardType: TextInputType.number),
              const SizedBox(height: 12),
              ZTextField(controller: _maxIndemnityYearsCtrl, label: 'Años Máx. Indemnización', keyboardType: TextInputType.number),
              const SizedBox(height: 12),
              SwitchListTile(
                title: const Text('Aguinaldo 13°'),
                value: _has13th,
                onChanged: (v) => setState(() => _has13th = v),
                contentPadding: EdgeInsets.zero,
              ),
              SwitchListTile(
                title: const Text('Aguinaldo 14°'),
                value: _has14th,
                onChanged: (v) => setState(() => _has14th = v),
                contentPadding: EdgeInsets.zero,
              ),
              const SizedBox(height: 32),
            ],
          ),
        ),
      ),
    );
  }

  Widget _section(String title) => Padding(
        padding: const EdgeInsets.only(bottom: 12),
        child: Text(title,
            style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14)),
      );
}
