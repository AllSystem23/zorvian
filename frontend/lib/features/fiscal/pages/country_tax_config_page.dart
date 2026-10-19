import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

// ── Model ──
//
// Espeja 1:1 `CountryTaxConfigResponse`. La configuración fiscal es un catálogo
// GLOBAL (una fila por país) que alimenta la nómina, la liquidación final y el
// motor documental: si la pantalla no carga/guarda un campo, el PDF sale mal.
class CountryTaxConfigItem {
  final String id;
  final String countryCode;
  final String countryName;
  final String currency;

  // Tasas y topes (generales / respaldo)
  final double inssEmployeeRate;
  final double inssEmployeeMax;
  final double inssEmployerRate;
  final double inssEmployerMax;
  final double otherEmployerRate;
  final String otherEmployerName;

  // Regímenes INSS (Decreto 06-2019 en Nicaragua)
  final double inssIntegralEmployeeRate;
  final double inssIntegralEmployerRate;
  final double inssIntegralEmployerRateSmall;
  final double inssIvmEmployeeRate;
  final double inssIvmEmployerRate;
  final double inssIvmEmployerRateSmall;
  final int inssSmallEmployerThreshold;

  // IR
  final double irExemptAmount;
  final String irTableJson;

  // Prestaciones
  final int vacationDaysPerYear;
  final double christmasBonusPercentage;
  final int indemnityDaysPerYear;
  final int maxIndemnityYears;
  final String indemnityTiersJson;
  final int maxIndemnityDays;
  final bool hasThirteenthMonth;
  final bool hasFourteenthMonth;

  // Período de aguinaldo / año fiscal
  final int aguinaldoPeriodStartMonth;
  final int aguinaldoPeriodStartDay;
  final int defaultFiscalStartMonth;

  final bool isActive;

  CountryTaxConfigItem({
    required this.id,
    required this.countryCode,
    required this.countryName,
    required this.currency,
    required this.inssEmployeeRate,
    required this.inssEmployeeMax,
    required this.inssEmployerRate,
    required this.inssEmployerMax,
    required this.otherEmployerRate,
    required this.otherEmployerName,
    required this.inssIntegralEmployeeRate,
    required this.inssIntegralEmployerRate,
    required this.inssIntegralEmployerRateSmall,
    required this.inssIvmEmployeeRate,
    required this.inssIvmEmployerRate,
    required this.inssIvmEmployerRateSmall,
    required this.inssSmallEmployerThreshold,
    required this.irExemptAmount,
    required this.irTableJson,
    required this.vacationDaysPerYear,
    required this.christmasBonusPercentage,
    required this.indemnityDaysPerYear,
    required this.maxIndemnityYears,
    required this.indemnityTiersJson,
    required this.maxIndemnityDays,
    required this.hasThirteenthMonth,
    required this.hasFourteenthMonth,
    required this.aguinaldoPeriodStartMonth,
    required this.aguinaldoPeriodStartDay,
    required this.defaultFiscalStartMonth,
    required this.isActive,
  });

  factory CountryTaxConfigItem.fromJson(Map<String, dynamic> j) {
    double d(String k) => (j[k] as num?)?.toDouble() ?? 0;
    int i(String k, [int def = 0]) => (j[k] as num?)?.toInt() ?? def;
    bool b(String k) => j[k] as bool? ?? false;
    String s(String k) => j[k]?.toString() ?? '';

    return CountryTaxConfigItem(
      id: s('id'),
      countryCode: s('countryCode'),
      countryName: s('countryName'),
      currency: s('currency'),
      inssEmployeeRate: d('inssEmployeeRate'),
      inssEmployeeMax: d('inssEmployeeMax'),
      inssEmployerRate: d('inssEmployerRate'),
      inssEmployerMax: d('inssEmployerMax'),
      otherEmployerRate: d('otherEmployerRate'),
      otherEmployerName: s('otherEmployerName'),
      inssIntegralEmployeeRate: d('inssIntegralEmployeeRate'),
      inssIntegralEmployerRate: d('inssIntegralEmployerRate'),
      inssIntegralEmployerRateSmall: d('inssIntegralEmployerRateSmall'),
      inssIvmEmployeeRate: d('inssIvmEmployeeRate'),
      inssIvmEmployerRate: d('inssIvmEmployerRate'),
      inssIvmEmployerRateSmall: d('inssIvmEmployerRateSmall'),
      inssSmallEmployerThreshold: i('inssSmallEmployerThreshold', 50),
      irExemptAmount: d('irExemptAmount'),
      irTableJson: s('irTableJson'),
      vacationDaysPerYear: i('vacationDaysPerYear', 15),
      christmasBonusPercentage: d('christmasBonusPercentage'),
      indemnityDaysPerYear: i('indemnityDaysPerYear'),
      maxIndemnityYears: i('maxIndemnityYears'),
      indemnityTiersJson: s('indemnityTiersJson'),
      maxIndemnityDays: i('maxIndemnityDays'),
      hasThirteenthMonth: b('hasThirteenthMonth'),
      hasFourteenthMonth: b('hasFourteenthMonth'),
      aguinaldoPeriodStartMonth: i('aguinaldoPeriodStartMonth', 12),
      aguinaldoPeriodStartDay: i('aguinaldoPeriodStartDay', 1),
      defaultFiscalStartMonth: i('defaultFiscalStartMonth', 1),
      isActive: j['isActive'] as bool? ?? true,
    );
  }
}

/// Formatea un decimal sin `.0` sobrante para precargar los campos.
String _fmtRate(double value) =>
    value == value.roundToDouble() ? value.toInt().toString() : value.toString();

String _fmtPct(double rate) => '${(rate * 100).toStringAsFixed(1)}%';

// ── Notifier (Riverpod 3.x) ──
class CountryTaxConfigNotifier extends AsyncNotifier<List<CountryTaxConfigItem>> {
  @override
  Future<List<CountryTaxConfigItem>> build() => _load();

  Future<List<CountryTaxConfigItem>> _load() async {
    final dio = ref.read(dioClientProvider);
    final res = await dio.get('country-tax-configs');
    return (res.data as List)
        .map((e) => CountryTaxConfigItem.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<void> load() async {
    state = await AsyncValue.guard(() => _load());
  }
}

final countryTaxConfigProvider =
    AsyncNotifierProvider<CountryTaxConfigNotifier, List<CountryTaxConfigItem>>(
        CountryTaxConfigNotifier.new);

// ── Configuración fiscal de la COMPAÑÍA ──
//
// Espeja `MyCountryTaxConfigResponse`. `isCustomized = false` significa que hoy se
// usa el catálogo global tal cual; `config = null`, que la compañía no tiene país.
class MyCountryTaxConfig {
  final String countryCode;
  final String countryName;
  final bool isCustomized;
  final CountryTaxConfigItem? config;

  const MyCountryTaxConfig({
    required this.countryCode,
    required this.countryName,
    required this.isCustomized,
    required this.config,
  });

  factory MyCountryTaxConfig.fromJson(Map<String, dynamic> j) {
    final raw = j['config'];
    return MyCountryTaxConfig(
      countryCode: j['countryCode']?.toString() ?? '',
      countryName: j['countryName']?.toString() ?? '',
      isCustomized: j['isCustomized'] as bool? ?? false,
      config: raw is Map
          ? CountryTaxConfigItem.fromJson(Map<String, dynamic>.from(raw))
          : null,
    );
  }
}

class MyCountryTaxConfigNotifier extends AsyncNotifier<MyCountryTaxConfig> {
  @override
  Future<MyCountryTaxConfig> build() => _load();

  Future<MyCountryTaxConfig> _load() async {
    final dio = ref.read(dioClientProvider);
    final res = await dio.get('country-tax-configs/my-country');
    return MyCountryTaxConfig.fromJson(
        Map<String, dynamic>.from(res.data as Map));
  }

  Future<void> load() async {
    state = await AsyncValue.guard(_load);
  }
}

final myCountryTaxConfigProvider =
    AsyncNotifierProvider<MyCountryTaxConfigNotifier, MyCountryTaxConfig>(
        MyCountryTaxConfigNotifier.new);

// ── Page ──
//
// Pantalla ADAPTATIVA por rol:
//  - SuperAdmin → catálogo GLOBAL (una fila por país, afecta a todas las compañías).
//  - Cualquier otro rol → configuración fiscal de SU país: una fila propia de la
//    compañía que se apoya en el catálogo global y no lo modifica.
class CountryTaxConfigPage extends ConsumerWidget {
  const CountryTaxConfigPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isSuperAdmin = ref.watch(authProvider).role == 'SuperAdmin';
    return isSuperAdmin
        ? const _GlobalCatalogView()
        : const _CompanyTaxConfigView();
  }
}

class _GlobalCatalogView extends ConsumerStatefulWidget {
  const _GlobalCatalogView();

  @override
  ConsumerState<_GlobalCatalogView> createState() => _GlobalCatalogViewState();
}

class _GlobalCatalogViewState extends ConsumerState<_GlobalCatalogView> {
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

  void _openEditForm(CountryTaxConfigItem existing) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => CountryTaxConfigForm(
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
                            final other = c.otherEmployerName.isNotEmpty
                                ? '${c.otherEmployerName} ${_fmtPct(c.otherEmployerRate)}'
                                : 'sin otro aporte';
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
                                  '${c.currency} · INSS ${_fmtPct(c.inssEmployeeRate)}/${_fmtPct(c.inssEmployerRate)} · '
                                  '$other · Vacaciones: ${c.vacationDaysPerYear}d'),
                              trailing: PopupMenuButton<String>(
                                onSelected: (v) {
                                  if (v == 'edit') _openEditForm(c);
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
    );
  }
}

// ── Vista de la compañía (ajuste sobre el catálogo global) ──
class _CompanyTaxConfigView extends ConsumerWidget {
  const _CompanyTaxConfigView();

  void _openEditForm(
      BuildContext context, WidgetRef ref, CountryTaxConfigItem item) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => CountryTaxConfigForm(
        existing: item,
        companyMode: true,
        onSaved: () => ref.read(myCountryTaxConfigProvider.notifier).load(),
      ),
    );
  }

  Future<void> _confirmReset(
      BuildContext context, WidgetRef ref, MyCountryTaxConfig data) async {
    final ok = await ZModal.confirm(
      context,
      title: 'Restablecer configuración fiscal',
      message:
          'Se descartarán los valores propios de tu compañía para ${data.countryName} '
          'y se volverá a usar el catálogo global. ¿Continuar?',
      confirmText: 'Restablecer',
      cancelText: 'Cancelar',
      confirmColor: ZColors.danger,
    );
    if (ok != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('country-tax-configs/my-country',
          queryParameters: {'countryCode': data.countryCode});
      await ref.read(myCountryTaxConfigProvider.notifier).load();
      if (context.mounted) {
        ZToast.success(context, 'Configuración restablecida al catálogo global');
      }
    } catch (_) {
      if (context.mounted) {
        ZToast.error(context, 'No se pudo restablecer la configuración');
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(myCountryTaxConfigProvider);
    return Scaffold(
      body: state.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Text('No se pudo cargar la configuración fiscal.\n$e',
                textAlign: TextAlign.center),
          ),
        ),
        data: (data) => RefreshIndicator(
          onRefresh: () => ref.read(myCountryTaxConfigProvider.notifier).load(),
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              _banner(context, data),
              const SizedBox(height: 16),
              if (data.config == null)
                _missingCountry(context)
              else
                _configCard(context, ref, data),
            ],
          ),
        ),
      ),
    );
  }

  Widget _banner(BuildContext context, MyCountryTaxConfig data) {
    final own = data.isCustomized;
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: (own ? ZColors.brandAccent : ZColors.neutral500).withAlpha(18),
        borderRadius: BorderRadius.circular(ZRadii.sm),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(own ? Icons.tune : Icons.public,
              size: 20, color: own ? ZColors.brandAccent : ZColors.neutral500),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  own
                      ? 'Tu compañía usa valores propios'
                      : 'Tu compañía usa el catálogo global',
                  style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13),
                ),
                const SizedBox(height: 4),
                Text(
                  'Estos valores alimentan la nómina, la liquidación final y los '
                  'documentos de TU compañía. No modifican el catálogo global del '
                  'resto de las empresas.',
                  style: TextStyle(
                    fontSize: 12,
                    height: 1.4,
                    color: Theme.of(context).brightness == Brightness.dark
                        ? ZColors.neutral400
                        : ZColors.neutral600,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _missingCountry(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            children: [
              const Icon(Icons.public_off, size: 36),
              const SizedBox(height: 12),
              const Text('Tu compañía no tiene un país configurado',
                  textAlign: TextAlign.center,
                  style: TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 8),
              Text(
                'Configura el país de la empresa para poder ajustar sus tasas de INSS, '
                'IR y prestaciones.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 12,
                  color: Theme.of(context).brightness == Brightness.dark
                      ? ZColors.neutral400
                      : ZColors.neutral600,
                ),
              ),
            ],
          ),
        ),
      );

  Widget _configCard(
      BuildContext context, WidgetRef ref, MyCountryTaxConfig data) {
    final c = data.config!;
    final other = c.otherEmployerName.isNotEmpty
        ? '${c.otherEmployerName} ${_fmtPct(c.otherEmployerRate)}'
        : '—';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    CircleAvatar(
                      backgroundColor: Theme.of(context).colorScheme.primaryContainer,
                      child: Text(c.countryCode,
                          style: const TextStyle(fontWeight: FontWeight.bold)),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(c.countryName,
                              style: const TextStyle(
                                  fontWeight: FontWeight.w700, fontSize: 16)),
                          Text('Moneda ${c.currency}',
                              style: const TextStyle(fontSize: 12)),
                        ],
                      ),
                    ),
                    Chip(
                      label: Text(
                        data.isCustomized ? 'Valores propios' : 'Catálogo global',
                        style: const TextStyle(fontSize: 11),
                      ),
                      visualDensity: VisualDensity.compact,
                    ),
                  ],
                ),
                const Divider(height: 28),
                Wrap(
                  spacing: 28,
                  runSpacing: 14,
                  children: [
                    _metric('INSS trabajador', _fmtPct(c.inssEmployeeRate)),
                    _metric('INSS patronal', _fmtPct(c.inssEmployerRate)),
                    _metric('Otro aporte patronal', other),
                    _metric('Vacaciones', '${c.vacationDaysPerYear} días'),
                    _metric('Aguinaldo', _fmtPct(c.christmasBonusPercentage)),
                    _metric('Indemnización', '${c.maxIndemnityDays} días máx.'),
                  ],
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        ZButton(
          text: 'Editar configuración',
          icon: Icons.edit_outlined,
          onPressed: () => _openEditForm(context, ref, c),
        ),
        if (data.isCustomized) ...[
          const SizedBox(height: 8),
          ZButton(
            text: 'Restablecer al catálogo global',
            icon: Icons.restore,
            type: ZButtonType.secondary,
            onPressed: () => _confirmReset(context, ref, data),
          ),
        ],
      ],
    );
  }

  Widget _metric(String label, String value) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(label, style: const TextStyle(fontSize: 11)),
          const SizedBox(height: 2),
          Text(value,
              style:
                  const TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
        ],
      );
}

// ── Form Sheet (public for ZQuickActionsFAB callback) ──

/// Fila editable de la tabla progresiva de IR (`irTableJson`).
class _BracketRow {
  _BracketRow({String from = '', String to = '', String rate = ''})
      : from = TextEditingController(text: from),
        to = TextEditingController(text: to),
        rate = TextEditingController(text: rate);

  final TextEditingController from;
  final TextEditingController to;
  final TextEditingController rate;

  void dispose() {
    from.dispose();
    to.dispose();
    rate.dispose();
  }
}

/// Fila editable de los escalones de indemnización (`indemnityTiersJson`).
class _TierRow {
  _TierRow({String upToYears = '', String daysPerYear = ''})
      : upToYears = TextEditingController(text: upToYears),
        daysPerYear = TextEditingController(text: daysPerYear);

  final TextEditingController upToYears;
  final TextEditingController daysPerYear;

  void dispose() {
    upToYears.dispose();
    daysPerYear.dispose();
  }
}

class CountryTaxConfigForm extends ConsumerStatefulWidget {
  final CountryTaxConfigItem? existing;
  final VoidCallback onSaved;

  /// `true` = ajuste de la compañía (`PUT country-tax-configs/my-country`): no se
  /// editan los datos de identificación del país y no se toca el catálogo global.
  final bool companyMode;

  const CountryTaxConfigForm({
    super.key,
    this.existing,
    required this.onSaved,
    this.companyMode = false,
  });

  @override
  ConsumerState<CountryTaxConfigForm> createState() => _CountryTaxConfigFormState();
}

class _CountryTaxConfigFormState extends ConsumerState<CountryTaxConfigForm> {
  final _formKey = GlobalKey<FormState>();
  final _c = <String, TextEditingController>{};

  final List<_BracketRow> _brackets = [];
  final List<_TierRow> _tiers = [];

  bool _has13th = false;
  bool _has14th = false;
  bool _isActive = true;
  bool _saving = false;

  /// Avisos cuando el JSON guardado no se puede interpretar (se reemplazará al guardar).
  String? _irParseWarning;
  String? _tiersParseWarning;

  int _aguinaldoMonth = 12;
  int _aguinaldoDay = 1;
  int _fiscalStartMonth = 1;

  bool get _isEditing => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final e = widget.existing;

    _str('countryCode', e?.countryCode ?? '');
    _str('countryName', e?.countryName ?? '');
    _str('currency', e?.currency ?? '');
    _str('otherEmployerName', e?.otherEmployerName ?? '');

    _str('inssEmployeeRate', _fmtRate(e?.inssEmployeeRate ?? 0));
    _str('inssEmployeeMax', _fmtRate(e?.inssEmployeeMax ?? 0));
    _str('inssEmployerRate', _fmtRate(e?.inssEmployerRate ?? 0));
    _str('inssEmployerMax', _fmtRate(e?.inssEmployerMax ?? 0));
    _str('otherEmployerRate', _fmtRate(e?.otherEmployerRate ?? 0));

    _str('inssIntegralEmployeeRate', _fmtRate(e?.inssIntegralEmployeeRate ?? 0));
    _str('inssIntegralEmployerRate', _fmtRate(e?.inssIntegralEmployerRate ?? 0));
    _str('inssIntegralEmployerRateSmall', _fmtRate(e?.inssIntegralEmployerRateSmall ?? 0));
    _str('inssIvmEmployeeRate', _fmtRate(e?.inssIvmEmployeeRate ?? 0));
    _str('inssIvmEmployerRate', _fmtRate(e?.inssIvmEmployerRate ?? 0));
    _str('inssIvmEmployerRateSmall', _fmtRate(e?.inssIvmEmployerRateSmall ?? 0));
    _str('inssSmallEmployerThreshold', (e?.inssSmallEmployerThreshold ?? 50).toString());

    _str('irExemptAmount', _fmtRate(e?.irExemptAmount ?? 0));

    _str('vacationDaysPerYear', (e?.vacationDaysPerYear ?? 15).toString());
    _str('christmasBonusPercentage', _fmtRate(e?.christmasBonusPercentage ?? 0));
    _str('indemnityDaysPerYear', (e?.indemnityDaysPerYear ?? 0).toString());
    _str('maxIndemnityYears', (e?.maxIndemnityYears ?? 0).toString());
    _str('maxIndemnityDays', (e?.maxIndemnityDays ?? 0).toString());

    _has13th = e?.hasThirteenthMonth ?? false;
    _has14th = e?.hasFourteenthMonth ?? false;
    _isActive = e?.isActive ?? true;
    _aguinaldoMonth = e?.aguinaldoPeriodStartMonth ?? 12;
    _aguinaldoDay = e?.aguinaldoPeriodStartDay ?? 1;
    _fiscalStartMonth = e?.defaultFiscalStartMonth ?? 1;

    _loadBrackets(e?.irTableJson ?? '[]');
    _loadTiers(e?.indemnityTiersJson ?? '[]');
  }

  void _str(String key, String value) {
    _c[key] = TextEditingController(text: value);
  }

  String _t(String key) => _c[key]!.text.trim();

  double _num(String key) => double.tryParse(_t(key).replaceAll(',', '.')) ?? 0;

  int _int(String key, int fallback) => int.tryParse(_t(key)) ?? fallback;

  void _loadBrackets(String json) {
    try {
      final data = jsonDecode(json);
      if (data is List) {
        for (final item in data) {
          if (item is Map) {
            _brackets.add(_BracketRow(
              from: _fmtRate((item['min'] as num?)?.toDouble() ?? 0),
              to: _fmtRate((item['max'] as num?)?.toDouble() ?? 0),
              rate: _fmtRate((item['rate'] as num?)?.toDouble() ?? 0),
            ));
          }
        }
      }
    } catch (_) {
      if (json.trim().isNotEmpty && json.trim() != '[]') {
        _irParseWarning =
            'La tabla de IR guardada no se pudo interpretar; se reemplazará al guardar.';
      }
    }
  }

  void _loadTiers(String json) {
    try {
      final data = jsonDecode(json);
      if (data is List) {
        for (final item in data) {
          if (item is Map) {
            _tiers.add(_TierRow(
              upToYears: ((item['upToYears'] as num?)?.toInt() ?? 0).toString(),
              daysPerYear: ((item['daysPerYear'] as num?)?.toInt() ?? 0).toString(),
            ));
          }
        }
      }
    } catch (_) {
      if (json.trim().isNotEmpty && json.trim() != '[]') {
        _tiersParseWarning =
            'Los escalones de indemnización guardados no se pudieron interpretar; se reemplazarán al guardar.';
      }
    }
  }

  @override
  void dispose() {
    for (final c in _c.values) {
      c.dispose();
    }
    for (final b in _brackets) {
      b.dispose();
    }
    for (final t in _tiers) {
      t.dispose();
    }
    super.dispose();
  }

  /// JSON de la tabla de IR. `null` = alguna fila está incompleta (no se guarda).
  String? _serializeBrackets() {
    final list = <Map<String, dynamic>>[];
    for (final r in _brackets) {
      final min = double.tryParse(r.from.text.trim().replaceAll(',', '.'));
      final max = double.tryParse(r.to.text.trim().replaceAll(',', '.'));
      final rate = double.tryParse(r.rate.text.trim().replaceAll(',', '.'));
      if (min == null || max == null || rate == null || max < min) return null;
      list.add({'min': min, 'max': max, 'rate': rate});
    }
    return jsonEncode(list);
  }

  /// JSON de los escalones de indemnización. `null` = alguna fila está incompleta.
  String? _serializeTiers() {
    final list = <Map<String, dynamic>>[];
    for (final t in _tiers) {
      final upTo = int.tryParse(t.upToYears.text.trim());
      final days = int.tryParse(t.daysPerYear.text.trim());
      if (upTo == null || days == null || upTo <= 0) return null;
      list.add({'upToYears': upTo, 'daysPerYear': days});
    }
    return jsonEncode(list);
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    final irJson = _serializeBrackets();
    if (irJson == null) {
      ZToast.error(context,
          'Revise la tabla de IR: cada tramo necesita Desde, Hasta y Tasa (Hasta ≥ Desde).');
      return;
    }
    final tiersJson = _serializeTiers();
    if (tiersJson == null) {
      ZToast.error(context,
          'Revise los escalones de indemnización: cada uno necesita Años y Días por año.');
      return;
    }

    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      final data = <String, dynamic>{
        'countryCode': _t('countryCode').toUpperCase(),
        'countryName': _t('countryName'),
        'currency': _t('currency').toUpperCase(),
        // Tasas en fracción (0.07 = 7 %). El backend valida 0..1.
        'inssEmployeeRate': _num('inssEmployeeRate'),
        'inssEmployeeMax': _num('inssEmployeeMax'),
        'inssEmployerRate': _num('inssEmployerRate'),
        'inssEmployerMax': _num('inssEmployerMax'),
        'otherEmployerRate': _num('otherEmployerRate'),
        // Vacío = el país no tiene otro aporte (el backend lo normaliza a null).
        'otherEmployerName': _t('otherEmployerName'),
        'inssIntegralEmployeeRate': _num('inssIntegralEmployeeRate'),
        'inssIntegralEmployerRate': _num('inssIntegralEmployerRate'),
        'inssIntegralEmployerRateSmall': _num('inssIntegralEmployerRateSmall'),
        'inssIvmEmployeeRate': _num('inssIvmEmployeeRate'),
        'inssIvmEmployerRate': _num('inssIvmEmployerRate'),
        'inssIvmEmployerRateSmall': _num('inssIvmEmployerRateSmall'),
        'inssSmallEmployerThreshold': _int('inssSmallEmployerThreshold', 50),
        'irExemptAmount': _num('irExemptAmount'),
        'irTableJson': irJson,
        'vacationDaysPerYear': _int('vacationDaysPerYear', 15),
        'christmasBonusPercentage': _num('christmasBonusPercentage'),
        'indemnityDaysPerYear': _int('indemnityDaysPerYear', 0),
        'maxIndemnityYears': _int('maxIndemnityYears', 0),
        'indemnityTiersJson': tiersJson,
        'maxIndemnityDays': _int('maxIndemnityDays', 0),
        'hasThirteenthMonth': _has13th,
        'hasFourteenthMonth': _has14th,
        'aguinaldoPeriodStartMonth': _aguinaldoMonth,
        'aguinaldoPeriodStartDay': _aguinaldoDay,
        'defaultFiscalStartMonth': _fiscalStartMonth,
        // Un ajuste de compañía no puede quedar inactivo (quedaría sin efecto).
        'isActive': widget.companyMode ? true : _isActive,
      };
      if (widget.companyMode) {
        await dio.put('country-tax-configs/my-country', data: data);
      } else if (_isEditing) {
        await dio.put('country-tax-configs/${widget.existing!.id}', data: data);
      } else {
        await dio.post('country-tax-configs', data: data);
      }
      if (!mounted) return;
      final messenger = ScaffoldMessenger.of(context);
      Navigator.pop(context);
      widget.onSaved();
      final savedMessage = widget.companyMode
          ? 'Configuración de tu compañía actualizada'
          : _isEditing
              ? 'Configuración actualizada'
              : 'Configuración creada';
      messenger.showSnackBar(SnackBar(content: Text(savedMessage)));
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('Error: ${_errorMessage(e)}')));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  /// Mensaje del backend (`{error: …}` / `{message: …}`) cuando existe: las
  /// validaciones de tasas e IR son accionables y no deben quedar ocultas tras
  /// un `DioException [bad response]`.
  String _errorMessage(Object e) {
    if (e is DioException) {
      final data = e.response?.data;
      if (data is Map) {
        final detail = data['error'] ?? data['message'] ?? data['detail'];
        if (detail != null && detail.toString().isNotEmpty) return detail.toString();
      }
    }
    return e.toString();
  }

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.9,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      expand: false,
      builder: (_, scrollCtrl) => Scaffold(
        appBar: AppBar(
          title: Text(widget.companyMode
              ? 'Ajustar Configuración Fiscal'
              : _isEditing
                  ? 'Editar País'
                  : 'Nuevo País'),
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
              _conventionNote(),
              if (widget.companyMode)
                _countryIdentityReadOnly()
              else ...[
                _section('Identificación del País'),
                ZTextField(controller: _c['countryCode'], label: 'Código ISO (ej: NIC)', validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _c['countryName'], label: 'Nombre del País', validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _c['currency'], label: 'Moneda (ej: NIO)', validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                if (_isEditing)
                  SwitchListTile(
                    title: const Text('Configuración activa'),
                    subtitle: const Text('Inactiva = no se usa como predeterminada del país'),
                    value: _isActive,
                    onChanged: (v) => setState(() => _isActive = v),
                    contentPadding: EdgeInsets.zero,
                  ),
              ],
              const SizedBox(height: 20),

              _section('INSS — Tasas Generales'),
              _pair(
                _rate('inssEmployeeRate', 'Tasa INSS Trabajador'),
                _rate('inssEmployeeMax', 'Tope INSS Trabajador', isRate: false),
              ),
              const SizedBox(height: 12),
              _pair(
                _rate('inssEmployerRate', 'Tasa INSS Patronal'),
                _rate('inssEmployerMax', 'Tope INSS Patronal', isRate: false),
              ),
              const SizedBox(height: 12),
              _pair(
                _rate('otherEmployerRate', 'Otro Aporte Patronal'),
                ZTextField(
                  controller: _c['otherEmployerName'],
                  label: 'Nombre del otro aporte',
                  hint: 'INATEC, RAP, Seguro Educativo…',
                ),
              ),
              const SizedBox(height: 20),

              _section('INSS — Regímenes (opcional)'),
              const _Hint('Solo si el país distingue régimen y tamaño de empresa (NIC: Decreto 06-2019).'),
              const SizedBox(height: 12),
              ZDropdownFormField<String>(
                value: _regimePreset,
                label: 'Régimen del plan',
                items: const [
                  DropdownMenuItem(value: '', child: Text('Sin régimen (usa las tasas generales)')),
                  DropdownMenuItem(value: 'integral', child: Text('Integral')),
                  DropdownMenuItem(value: 'ivm', child: Text('IVM (Invalidez, Vejez y Muerte)')),
                ],
                onChanged: (v) => setState(() => _applyRegimePreset(v ?? '')),
              ),
              const SizedBox(height: 12),
              _pair(
                _rate('inssIntegralEmployeeRate', 'Integral · Trabajador'),
                _rate('inssIntegralEmployerRate', 'Integral · Patronal ≥ umbral'),
              ),
              const SizedBox(height: 12),
              _pair(
                _rate('inssIntegralEmployerRateSmall', 'Integral · Patronal < umbral'),
                _rate('inssSmallEmployerThreshold', 'Umbral de empleados', isRate: false, isInt: true),
              ),
              const SizedBox(height: 12),
              _pair(
                _rate('inssIvmEmployeeRate', 'IVM · Trabajador'),
                _rate('inssIvmEmployerRate', 'IVM · Patronal ≥ umbral'),
              ),
              const SizedBox(height: 12),
              _rate('inssIvmEmployerRateSmall', 'IVM · Patronal < umbral'),
              const SizedBox(height: 20),

              _section('Impuesto sobre la Renta (IR)'),
              _rate('irExemptAmount', 'Monto Exento IR', isRate: false),
              const SizedBox(height: 12),
              if (_irParseWarning != null) _warning(_irParseWarning!),
              const _Hint('Tabla progresiva anual (Art. 23 Ley 822 en NIC). «Hasta» del último tramo debe cubrir el ingreso máximo.'),
              const SizedBox(height: 8),
              for (var i = 0; i < _brackets.length; i++) _bracketEditor(i),
              Align(
                alignment: Alignment.centerLeft,
                child: TextButton.icon(
                  onPressed: () => setState(() => _brackets.add(_BracketRow())),
                  icon: const Icon(Icons.add, size: 18),
                  label: const Text('Agregar tramo'),
                ),
              ),
              const SizedBox(height: 20),

              _section('Prestaciones Laborales'),
              _pair(
                _intField('vacationDaysPerYear', 'Días de Vacaciones/Año'),
                _rate('christmasBonusPercentage', 'Aguinaldo'),
              ),
              const SizedBox(height: 12),
              _pair(
                _intField('indemnityDaysPerYear', 'Días de Indemnización/Año'),
                _intField('maxIndemnityYears', 'Años Máx. Indemnización'),
              ),
              const SizedBox(height: 12),
              _intField('maxIndemnityDays', 'Tope de Indemnización (días)'),
              const SizedBox(height: 12),
              if (_tiersParseWarning != null) _warning(_tiersParseWarning!),
              const _Hint('Escalones de indemnización por antigüedad: hasta N años, N días por año.'),
              const SizedBox(height: 8),
              for (var i = 0; i < _tiers.length; i++) _tierEditor(i),
              Align(
                alignment: Alignment.centerLeft,
                child: TextButton.icon(
                  onPressed: () => setState(() => _tiers.add(_TierRow())),
                  icon: const Icon(Icons.add, size: 18),
                  label: const Text('Agregar escalón'),
                ),
              ),
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
              const SizedBox(height: 20),

              _section('Período de Aguinaldo y Año Fiscal'),
              const _Hint('Inicio del período de aguinaldo: NIC/GTM/CRI/SLV 1/dic; HND 1/ene.'),
              const SizedBox(height: 12),
              _pair(
                ZDropdownFormField<int>(
                  value: _aguinaldoMonth,
                  label: 'Mes de inicio',
                  items: _monthItems(),
                  onChanged: (v) => setState(() => _aguinaldoMonth = v ?? 12),
                ),
                ZDropdownFormField<int>(
                  value: _aguinaldoDay,
                  label: 'Día de inicio',
                  items: [for (var d = 1; d <= 31; d++) DropdownMenuItem(value: d, child: Text('$d'))],
                  onChanged: (v) => setState(() => _aguinaldoDay = v ?? 1),
                ),
              ),
              const SizedBox(height: 12),
              ZDropdownFormField<int>(
                value: _fiscalStartMonth,
                label: 'Inicio del año fiscal',
                items: _monthItems(),
                onChanged: (v) => setState(() => _fiscalStartMonth = v ?? 1),
              ),
              const SizedBox(height: 32),
            ],
          ),
        ),
      ),
    );
  }

  String _regimePreset = '';

  void _applyRegimePreset(String preset) {
    _regimePreset = preset;
    // Precarga las tasas del régimen elegido en los campos generales (editable después).
    final map = <String, String>{
      'integral': 'inssIntegralEmployeeRate',
      'ivm': 'inssIvmEmployeeRate',
    };
    final source = map[preset];
    if (source != null && _num(source) > 0) {
      _c['inssEmployeeRate']!.text = _t(source);
    }
    final employerSource = preset == 'integral'
        ? 'inssIntegralEmployerRateSmall'
        : preset == 'ivm'
            ? 'inssIvmEmployerRateSmall'
            : null;
    if (employerSource != null && _num(employerSource) > 0) {
      _c['inssEmployerRate']!.text = _t(employerSource);
    }
    setState(() {});
  }

  List<DropdownMenuItem<int>> _monthItems() => const [
        DropdownMenuItem(value: 1, child: Text('Enero')),
        DropdownMenuItem(value: 2, child: Text('Febrero')),
        DropdownMenuItem(value: 3, child: Text('Marzo')),
        DropdownMenuItem(value: 4, child: Text('Abril')),
        DropdownMenuItem(value: 5, child: Text('Mayo')),
        DropdownMenuItem(value: 6, child: Text('Junio')),
        DropdownMenuItem(value: 7, child: Text('Julio')),
        DropdownMenuItem(value: 8, child: Text('Agosto')),
        DropdownMenuItem(value: 9, child: Text('Septiembre')),
        DropdownMenuItem(value: 10, child: Text('Octubre')),
        DropdownMenuItem(value: 11, child: Text('Noviembre')),
        DropdownMenuItem(value: 12, child: Text('Diciembre')),
      ];

  Widget _bracketEditor(int index) {
    final row = _brackets[index];
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Expanded(
            flex: 2,
            child: ZTextField(
              controller: row.from,
              label: 'Desde',
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            flex: 2,
            child: ZTextField(
              controller: row.to,
              label: 'Hasta',
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            flex: 2,
            child: ZTextField(
              controller: row.rate,
              label: 'Tasa',
              hint: '0.15 = 15%',
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
            ),
          ),
          IconButton(
            tooltip: 'Quitar tramo',
            onPressed: () {
              final removed = _brackets[index];
              setState(() => _brackets.removeAt(index));
              // Se libera después del frame: el TextField todavía la referencia al desmontarse.
              WidgetsBinding.instance.addPostFrameCallback((_) => removed.dispose());
            },
            icon: const Icon(Icons.delete_outline),
          ),
        ],
      ),
    );
  }

  Widget _tierEditor(int index) {
    final row = _tiers[index];
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Expanded(
            child: ZTextField(
              controller: row.upToYears,
              label: 'Hasta (años)',
              keyboardType: TextInputType.number,
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: ZTextField(
              controller: row.daysPerYear,
              label: 'Días por año',
              keyboardType: TextInputType.number,
            ),
          ),
          IconButton(
            tooltip: 'Quitar escalón',
            onPressed: () {
              final removed = _tiers[index];
              setState(() => _tiers.removeAt(index));
              WidgetsBinding.instance.addPostFrameCallback((_) => removed.dispose());
            },
            icon: const Icon(Icons.delete_outline),
          ),
        ],
      ),
    );
  }

  Widget _rate(String key, String label, {bool isRate = true, bool isInt = false}) {
    return ZTextField(
      controller: _c[key],
      label: label,
      hint: isRate
          ? '0.07 = 7%'
          : isInt
              ? 'empleados'
              : 'monto',
      keyboardType: isInt
          ? TextInputType.number
          : const TextInputType.numberWithOptions(decimal: true),
    );
  }

  Widget _intField(String key, String label) => ZTextField(
        controller: _c[key],
        label: label,
        keyboardType: TextInputType.number,
      );

  Widget _pair(Widget a, Widget b) => Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(child: a),
          const SizedBox(width: 12),
          Expanded(child: b),
        ],
      );

  Widget _countryIdentityReadOnly() => Container(
        margin: const EdgeInsets.only(bottom: 4),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          border: Border.all(color: ZColors.neutral300),
          borderRadius: BorderRadius.circular(ZRadii.sm),
        ),
        child: Row(
          children: [
            const Icon(Icons.public, size: 18),
            const SizedBox(width: 8),
            Expanded(
              child: Text(
                '${_t('countryName')} (${_t('countryCode')}) · ${_t('currency')}',
                style:
                    const TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
              ),
            ),
          ],
        ),
      );

  Widget _conventionNote() => Container(
        margin: const EdgeInsets.only(bottom: 16),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: (widget.companyMode
                  ? ZColors.brandAccent
                  : ZColors.moduleFinance)
              .withAlpha(18),
          borderRadius: BorderRadius.circular(ZRadii.sm),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(Icons.info_outline,
                size: 18,
                color: widget.companyMode
                    ? ZColors.brandAccent
                    : ZColors.moduleFinance),
            const SizedBox(width: 8),
            Expanded(
              child: Text(
                widget.companyMode
                    ? 'Estos valores aplican SÓLO a tu compañía (nómina, liquidación final '
                        'y documentos de tu empresa); no modifican el catálogo global. '
                        'Las tasas van en fracción: 0.07 = 7 %.'
                    : 'Las tasas se guardan en fracción: 0.07 = 7 %. Los montos (topes, exento) '
                        'van en la moneda del país. Estos valores alimentan la nómina, la '
                        'liquidación final y los documentos de TODAS las compañías.',
                style: const TextStyle(fontSize: 12, height: 1.4),
              ),
            ),
          ],
        ),
      );

  Widget _warning(String text) => Container(
        margin: const EdgeInsets.only(bottom: 8),
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: ZColors.warning.withAlpha(24),
          borderRadius: BorderRadius.circular(ZRadii.sm),
        ),
        child: Row(
          children: [
            const Icon(Icons.warning_amber_rounded, size: 18, color: ZColors.warning),
            const SizedBox(width: 8),
            Expanded(child: Text(text, style: const TextStyle(fontSize: 12))),
          ],
        ),
      );

  Widget _section(String title) => Padding(
        padding: const EdgeInsets.only(bottom: 12),
        child: Text(title,
            style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14)),
      );
}

/// Nota auxiliar en gris dentro del formulario.
class _Hint extends StatelessWidget {
  const _Hint(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Text(
        text,
        style: TextStyle(
          fontSize: 12,
          height: 1.4,
          color: Theme.of(context).brightness == Brightness.dark
              ? ZColors.neutral400
              : ZColors.neutral600,
        ),
      );
}
