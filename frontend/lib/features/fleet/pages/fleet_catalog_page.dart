import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../shared/ds/ds.dart';
import '../providers/fleet_catalog_provider.dart';

/// Supported countries for driver license categories.
const _supportedCountries = <String, String>{
  'NIC': '🇳🇮 Nicaragua',
  'CRI': '🇨🇷 Costa Rica',
  'GTM': '🇬🇹 Guatemala',
  'HND': '🇭🇳 Honduras',
  'SLV': '🇸🇻 El Salvador',
  'PAN': '🇵🇦 Panamá',
};

/// Catalog management page for Fleet module.
final class FleetCatalogPage extends ConsumerStatefulWidget {
  const FleetCatalogPage({super.key});

  @override
  ConsumerState<FleetCatalogPage> createState() => _FleetCatalogPageState();
}

class _FleetCatalogPageState extends ConsumerState<FleetCatalogPage>
    with SingleTickerProviderStateMixin {
  late final TabController _tabCtrl;
  String _selectedCountry = 'NIC';

  static const _tabEndpoints = [
    'fleet/brands',
    'fleet/vehicle-types',
    'fleet/fuel-types',
    'fleet/driver-license-categories',
  ];

  @override
  void initState() {
    super.initState();
    _tabCtrl = TabController(length: 4, vsync: this);
  }

  @override
  void dispose() {
    _tabCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Catálogos de Flota'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add_circle_outline),
            tooltip: 'Agregar al catálogo activo',
            onPressed: () {
              final endpoint = _tabEndpoints[_tabCtrl.index];
              final extra = _tabCtrl.index == 3 ? {'countryCode': _selectedCountry} : null;
              _CatalogDialog.show(context, ref: ref, endpoint: endpoint, extra: extra);
            },
          ),
        ],
        bottom: TabBar(
          controller: _tabCtrl,
          isScrollable: true,
          tabs: const [
            Tab(icon: Icon(Icons.directions_car_outlined), text: 'Marcas'),
            Tab(icon: Icon(Icons.category_outlined), text: 'Tipos'),
            Tab(icon: Icon(Icons.local_gas_station_outlined), text: 'Combustible'),
            Tab(icon: Icon(Icons.badge_outlined), text: 'Licencias'),
          ],
        ),
      ),
      body: Column(
        children: [
          // Country filter bar — only visible on Licencias tab
          ListenableBuilder(
            listenable: _tabCtrl,
            builder: (ctx, _) {
              final index = _tabCtrl.index;
              if (index != 3) return const SizedBox.shrink();
              return Container(
                margin: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.xs),
                padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.sm),
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.surface,
                  borderRadius: BorderRadius.circular(ZRadii.md),
                  border: Border.all(color: ZColors.border, width: 0.8),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.flag_outlined, size: 18, color: ZColors.moduleFleet),
                    const SizedBox(width: ZSpacing.sm),
                    const Text('País: ', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
                    const SizedBox(width: ZSpacing.xs),
                    Expanded(
                      child: DropdownButtonHideUnderline(
                        child: DropdownButton<String>(
                          value: _selectedCountry,
                          isDense: true,
                          isExpanded: true,
                          style: TextStyle(fontSize: 13, color: Theme.of(context).colorScheme.onSurface),
                          iconEnabledColor: ZColors.moduleFleet,
                          dropdownColor: Theme.of(context).colorScheme.surface,
                          items: _supportedCountries.entries
                              .map((e) => DropdownMenuItem(value: e.key, child: Text(e.value)))
                              .toList(),
                          onChanged: (v) => setState(() => _selectedCountry = v ?? 'NIC'),
                        ),
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
          // Tab content
          Expanded(
            child: TabBarView(
              controller: _tabCtrl,
              children: [
                _CatalogTab(
                  provider: vehicleBrandListProvider,
                  endpoint: 'fleet/brands',
                  emptyIcon: Icons.directions_car_outlined,
                  emptyTitle: 'No hay marcas registradas',
                ),
                _CatalogTab(
                  provider: vehicleTypeListProvider,
                  endpoint: 'fleet/vehicle-types',
                  emptyIcon: Icons.category_outlined,
                  emptyTitle: 'No hay tipos registrados',
                ),
                _CatalogTab(
                  provider: fuelTypeListProvider,
                  endpoint: 'fleet/fuel-types',
                  emptyIcon: Icons.local_gas_station_outlined,
                  emptyTitle: 'No hay combustibles registrados',
                ),
      _LicenseTab(
        countryCode: _selectedCountry,
      ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Generic catalog tab ──────────────────────────────────────────────

class _CatalogTab extends ConsumerWidget {
  final FutureProvider<List<FleetCatalogItem>> provider;
  final String endpoint;
  final IconData emptyIcon;
  final String emptyTitle;

  const _CatalogTab({
    required this.provider,
    required this.endpoint,
    required this.emptyIcon,
    required this.emptyTitle,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final asyncItems = ref.watch(provider);

    return ZAsyncRenderer<List<FleetCatalogItem>>(
      value: asyncItems,
      onRetry: () => ref.invalidate(provider),
      emptyWidget: ZEmptyState(icon: emptyIcon, title: emptyTitle),
      builder: (items) => RefreshIndicator(
        onRefresh: () => ref.refresh(provider.future),
        child: ListView.builder(
          padding: const EdgeInsets.all(ZSpacing.md),
          itemCount: items.length,
          itemBuilder: (_, i) => _CatalogItemCard(
            item: items[i],
            endpoint: endpoint,
            emptyIcon: emptyIcon,
          ),
        ),
      ),
    );
  }
}

// ── License categories tab (country-aware) ───────────────────────────

class _LicenseTab extends ConsumerWidget {
  final String countryCode;

  const _LicenseTab({required this.countryCode});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final asyncItems = ref.watch(driverLicenseCategoryByCountryProvider(countryCode));

    return ZAsyncRenderer<List<FleetCatalogItem>>(
      value: asyncItems,
      onRetry: () => ref.invalidate(driverLicenseCategoryByCountryProvider(countryCode)),
      emptyWidget: ZEmptyState(
        icon: Icons.badge_outlined,
        title: 'No hay categorías para ${_supportedCountries[countryCode] ?? countryCode}',
      ),
      builder: (items) => RefreshIndicator(
        onRefresh: () => ref.refresh(driverLicenseCategoryByCountryProvider(countryCode).future),
        child: ListView.builder(
          padding: const EdgeInsets.all(ZSpacing.md),
          itemCount: items.length,
          itemBuilder: (_, i) => _CatalogItemCard(
            item: items[i],
            endpoint: 'fleet/driver-license-categories',
            emptyIcon: Icons.badge_outlined,
            onInvalidated: () => ref.invalidate(driverLicenseCategoryByCountryProvider(countryCode)),
            subtitle: items[i].countryCode.isNotEmpty ? items[i].countryCode : null,
          ),
        ),
      ),
    );
  }
}

// ── Shared item card ─────────────────────────────────────────────────

class _CatalogItemCard extends ConsumerWidget {
  final FleetCatalogItem item;
  final String endpoint;
  final IconData emptyIcon;
  final VoidCallback? onInvalidated;
  final String? subtitle;

  const _CatalogItemCard({
    required this.item,
    required this.endpoint,
    required this.emptyIcon,
    this.onInvalidated,
    this.subtitle,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return ZCard(
      margin: const EdgeInsets.only(bottom: ZSpacing.sm),
      padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.sm),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          CircleAvatar(
            backgroundColor: ZColors.moduleFleet.withValues(alpha: 0.12),
            radius: 18,
            child: Icon(emptyIcon, color: ZColors.moduleFleet, size: 18),
          ),
          const SizedBox(width: ZSpacing.sm),
          Flexible(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  item.name,
                  style: const TextStyle(fontWeight: FontWeight.w600),
                  overflow: TextOverflow.ellipsis,
                ),
                if (subtitle != null) ...[
                  const SizedBox(height: 2),
                  Text(subtitle!, style: TextStyle(fontSize: 11, color: ZColors.neutral500)),
                ] else ...[
                  const SizedBox(height: 2),
                  Text('Activo', style: TextStyle(fontSize: 12, color: ZColors.success)),
                ],
              ],
            ),
          ),
          const SizedBox(width: ZSpacing.xs),
          IconButton(
            icon: const Icon(Icons.edit_outlined, size: 18),
            onPressed: () => _showEditDialog(context, ref),
            tooltip: 'Editar',
            visualDensity: VisualDensity.compact,
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints(minWidth: 32, minHeight: 32),
          ),
          IconButton(
            icon: const Icon(Icons.delete_outline, size: 18, color: ZColors.danger),
            onPressed: () => _confirmDelete(context, ref),
            tooltip: 'Eliminar',
            visualDensity: VisualDensity.compact,
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints(minWidth: 32, minHeight: 32),
          ),
        ],
      ),
    );
  }

  void _showEditDialog(BuildContext context, WidgetRef ref) {
    _CatalogDialog.show(context, ref: ref, item: item, endpoint: endpoint);
  }

  Future<void> _confirmDelete(BuildContext context, WidgetRef ref) async {
    final confirmed = await ZConfirmDialog.show(
      context,
      title: 'Eliminar',
      message: '¿Eliminar "${item.name}"? Esta acción no se puede deshacer.',
      confirmLabel: 'Eliminar',
      icon: Icons.delete_outline,
      isDestructive: true,
    );
    if (!confirmed) return;

    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('fleet/$endpoint/${item.id}');
      if (onInvalidated != null) {
        onInvalidated!();
      } else {
        ref.invalidate(vehicleBrandListProvider);
        ref.invalidate(vehicleTypeListProvider);
        ref.invalidate(fuelTypeListProvider);
        ref.invalidate(driverLicenseCategoryListProvider);
      }
      if (context.mounted) ZToast.success(context, 'Eliminado correctamente');
    } catch (e) {
      if (context.mounted) ZToast.error(context, 'Error al eliminar');
    }
  }
}

// ── Add/Edit dialog ──────────────────────────────────────────────────

class _CatalogDialog extends ConsumerStatefulWidget {
  final FleetCatalogItem? item;
  final String endpoint;
  final Map<String, dynamic>? extra;

  const _CatalogDialog({this.item, required this.endpoint, this.extra});

  static Future<void> show(
    BuildContext context, {
    required WidgetRef ref,
    FleetCatalogItem? item,
    String endpoint = 'fleet/brands',
    Map<String, dynamic>? extra,
  }) {
    return showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => _CatalogDialog(item: item, endpoint: endpoint, extra: extra),
    );
  }

  @override
  ConsumerState<_CatalogDialog> createState() => _CatalogDialogState();
}

class _CatalogDialogState extends ConsumerState<_CatalogDialog> {
  late final TextEditingController _nameCtrl;
  bool _saving = false;

  bool get _isEditing => widget.item != null;
  bool get _isLicense => widget.endpoint.contains('driver-license');
  String? _selectedCountry;

  @override
  void initState() {
    super.initState();
    _nameCtrl = TextEditingController(text: widget.item?.name ?? '');
    if (_isLicense && widget.extra != null) {
      _selectedCountry = widget.extra!['countryCode'] as String?;
    }
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(
        left: 24,
        right: 24,
        top: 24,
        bottom: MediaQuery.of(context).viewInsets.bottom + 24,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            _isEditing ? 'Editar' : 'Nuevo',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 20),
          ZTextField(
            controller: _nameCtrl,
            label: 'Nombre',
            prefix: const Icon(Icons.label_outline),
            validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
          ),
          if (_isLicense && !_isEditing) ...[
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              value: _selectedCountry,
              decoration: const InputDecoration(
                labelText: 'País',
                prefixIcon: Icon(Icons.flag_outlined),
                border: OutlineInputBorder(),
              ),
              items: _supportedCountries.entries
                  .map((e) => DropdownMenuItem(value: e.key, child: Text(e.value)))
                  .toList(),
              onChanged: (v) => setState(() => _selectedCountry = v),
              validator: (v) => v == null ? 'Requerido' : null,
            ),
          ],
          const SizedBox(height: 20),
          ZButton(
            text: _isEditing ? 'Actualizar' : 'Crear',
            icon: _isEditing ? Icons.save_outlined : Icons.add_circle_outline,
            isLoading: _saving,
            onPressed: _save,
          ),
        ],
      ),
    );
  }

  Future<void> _save() async {
    if (_nameCtrl.text.trim().isEmpty) return;
    if (_isLicense && !_isEditing && _selectedCountry == null) return;
    setState(() => _saving = true);

    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{
        'name': _nameCtrl.text.trim(),
      };

      if (_isLicense && !_isEditing) {
        body['countryCode'] = _selectedCountry;
      }

      if (_isEditing) {
        await dio.put('fleet/${widget.endpoint}/${widget.item!.id}', data: body);
      } else {
        await dio.post('fleet/${widget.endpoint}', data: body);
      }

      if (mounted) {
        ref.invalidate(vehicleBrandListProvider);
        ref.invalidate(vehicleTypeListProvider);
        ref.invalidate(fuelTypeListProvider);
        ref.invalidate(driverLicenseCategoryListProvider);
        if (_isLicense && _selectedCountry != null) {
          ref.invalidate(driverLicenseCategoryByCountryProvider(_selectedCountry!));
        }
        Navigator.of(context).pop();
        ZToast.success(
          context,
          _isEditing ? 'Actualizado correctamente' : 'Creado correctamente',
        );
      }
    } on DioException catch (e) {
      if (mounted) {
        final msg = e.response?.data is Map
            ? (e.response?.data['detail'] ?? e.response?.data['message'] ?? 'Error')
            : 'Error al guardar';
        ZToast.error(context, '$msg');
      }
    } catch (e) {
      if (mounted) ZToast.error(context, 'Error inesperado');
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }
}
