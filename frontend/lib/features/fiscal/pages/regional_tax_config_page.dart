import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

// ── Model ──
class RegionalTaxConfigItem {
  final String id;
  final String countryCode;
  final String taxType;
  final double rate;
  final String effectiveDate;
  final bool isActive;

  RegionalTaxConfigItem({
    required this.id,
    required this.countryCode,
    required this.taxType,
    required this.rate,
    required this.effectiveDate,
    required this.isActive,
  });

  factory RegionalTaxConfigItem.fromJson(Map<String, dynamic> j) =>
      RegionalTaxConfigItem(
        id: j['id'] as String,
        countryCode: j['countryCode'] as String? ?? '',
        taxType: j['taxType'] as String? ?? '',
        rate: (j['rate'] as num?)?.toDouble() ?? 0,
        effectiveDate: j['effectiveDate'] as String? ?? '',
        isActive: j['isActive'] as bool? ?? true,
      );
}

// ── Notifier (Riverpod 3.x) ──
class RegionalTaxConfigNotifier extends AsyncNotifier<List<RegionalTaxConfigItem>> {
  @override
  Future<List<RegionalTaxConfigItem>> build() => _load();

  Future<List<RegionalTaxConfigItem>> _load() async {
    final dio = ref.read(dioClientProvider);
    final res = await dio.get('regional-tax-configs');
    return (res.data as List)
        .map((e) => RegionalTaxConfigItem.fromJson(e))
        .toList();
  }

  Future<void> load() async {
    state = await AsyncValue.guard(() => _load());
  }
}

final regionalTaxConfigProvider =
    AsyncNotifierProvider<RegionalTaxConfigNotifier, List<RegionalTaxConfigItem>>(
        RegionalTaxConfigNotifier.new);

// ── Page ──
class RegionalTaxConfigPage extends ConsumerStatefulWidget {
  const RegionalTaxConfigPage({super.key});

  @override
  ConsumerState<RegionalTaxConfigPage> createState() => _RegionalTaxConfigPageState();
}

class _RegionalTaxConfigPageState extends ConsumerState<RegionalTaxConfigPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  List<RegionalTaxConfigItem> _filter(List<RegionalTaxConfigItem> items) {
    if (_searchQuery.isEmpty) return items;
    final q = _searchQuery.toLowerCase();
    return items
        .where((c) =>
            c.countryCode.toLowerCase().contains(q) ||
            c.taxType.toLowerCase().contains(q))
        .toList();
  }

  void _openEditForm(RegionalTaxConfigItem existing) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => RegionalTaxConfigForm(
        existing: existing,
        onSaved: () => ref.read(regionalTaxConfigProvider.notifier).load(),
      ),
    );
  }

  Future<void> _confirmDelete(RegionalTaxConfigItem item) async {
    final ok = await ZModal.confirm(
      context,
      title: 'Eliminar configuración',
      message: '¿Eliminar ${item.taxType} de ${item.countryCode}?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
      confirmColor: Colors.red,
    );
    if (ok != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('regional-tax-configs/${item.id}');
      ref.read(regionalTaxConfigProvider.notifier).load();
    } catch (_) {
      if (mounted) ZToast.error(context, 'Error al eliminar');
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(regionalTaxConfigProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Tasas Fiscales Regionales'),
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
                    hintText: 'Buscar por país o tipo...',
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
                        _searchQuery.isNotEmpty ? 'Sin resultados' : 'No hay tasas fiscales',
                      ))
                    : RefreshIndicator(
                        onRefresh: () =>
                            ref.read(regionalTaxConfigProvider.notifier).load(),
                        child: ListView.separated(
                          itemCount: filtered.length,
                          separatorBuilder: (_, _) => const Divider(height: 1),
                          itemBuilder: (_, i) {
                            final c = filtered[i];
                            return ListTile(
                              leading: CircleAvatar(
                                backgroundColor: c.isActive
                                    ? theme.colorScheme.secondaryContainer
                                    : theme.colorScheme.surfaceContainerHighest,
                                child: Icon(Icons.receipt_long,
                                    color: c.isActive
                                        ? theme.colorScheme.onSecondaryContainer
                                        : theme.colorScheme.onSurfaceVariant),
                              ),
                              title: Text('${c.taxType} — ${c.countryCode}',
                                  style: const TextStyle(fontWeight: FontWeight.w600)),
                              subtitle: Text(
                                  'Tasa: ${(c.rate * 100).toStringAsFixed(1)}% · Desde: ${c.effectiveDate.split('T').first}'),
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

// ── Form Sheet (public for ZQuickActionsFAB callback) ──
class RegionalTaxConfigForm extends ConsumerStatefulWidget {
  final RegionalTaxConfigItem? existing;
  final VoidCallback onSaved;

  const RegionalTaxConfigForm({super.key, this.existing, required this.onSaved});

  @override
  ConsumerState<RegionalTaxConfigForm> createState() => _RegionalTaxConfigFormState();
}

class _RegionalTaxConfigFormState extends ConsumerState<RegionalTaxConfigForm> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _countryCodeCtrl;
  late final TextEditingController _taxTypeCtrl;
  late final TextEditingController _rateCtrl;
  DateTime _effectiveDate = DateTime.now();
  bool _saving = false;

  bool get _isEditing => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final e = widget.existing;
    _countryCodeCtrl = TextEditingController(text: e?.countryCode ?? '');
    _taxTypeCtrl = TextEditingController(text: e?.taxType ?? '');
    _rateCtrl = TextEditingController(text: e != null ? (e.rate * 100).toString() : '');
    if (e != null && e.effectiveDate.isNotEmpty) {
      _effectiveDate = DateTime.tryParse(e.effectiveDate) ?? DateTime.now();
    }
  }

  @override
  void dispose() {
    _countryCodeCtrl.dispose();
    _taxTypeCtrl.dispose();
    _rateCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      final rateValue = (double.tryParse(_rateCtrl.text) ?? 0) / 100;
      final data = {
        'countryCode': _countryCodeCtrl.text.trim().toUpperCase(),
        'taxType': _taxTypeCtrl.text.trim(),
        'rate': rateValue,
        'effectiveDate': _effectiveDate.toUtc().toIso8601String(),
      };
      if (_isEditing) {
        data['isActive'] = true;
        await dio.put('regional-tax-configs/${widget.existing!.id}', data: data);
      } else {
        await dio.post('regional-tax-configs', data: data);
      }
      if (mounted) {
        Navigator.pop(context);
        widget.onSaved();
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(_isEditing ? 'Tasa actualizada' : 'Tasa creada')),
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
      initialChildSize: 0.6,
      minChildSize: 0.4,
      maxChildSize: 0.85,
      expand: false,
      builder: (_, scrollCtrl) => Scaffold(
        appBar: AppBar(
          title: Text(_isEditing ? 'Editar Tasa Fiscal' : 'Nueva Tasa Fiscal'),
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
              ZTextField(
                controller: _countryCodeCtrl,
                label: 'Código País (ej: NI, CR, HN)',
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: 12),
              ZTextField(
                controller: _taxTypeCtrl,
                label: 'Tipo de Impuesto (ej: IVA, IR, ISV)',
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: 12),
              ZTextField(
                controller: _rateCtrl,
                label: 'Tasa (%)',
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                hint: '15 = 15%',
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: 12),
              ListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text('Fecha de Vigencia'),
                subtitle: Text(_effectiveDate.toLocal().toString().split(' ').first),
                trailing: const Icon(Icons.calendar_today),
                onTap: () async {
                  final picked = await showDatePicker(
                    context: context,
                    initialDate: _effectiveDate,
                    firstDate: DateTime(2020),
                    lastDate: DateTime(2030),
                  );
                  if (picked != null) setState(() => _effectiveDate = picked);
                },
              ),
              const SizedBox(height: 32),
            ],
          ),
        ),
      ),
    );
  }
}
