import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../../../core/widgets/responsive_layout.dart';
import '../config/subscription_plans_config.dart';

/// Premium subscription plans management page (SuperAdmin only).
/// Displays plan tiers, feature comparison, and company distribution.
class SubscriptionPlansPage extends ConsumerStatefulWidget {
  const SubscriptionPlansPage({super.key});

  @override
  ConsumerState<SubscriptionPlansPage> createState() => _SubscriptionPlansPageState();
}

class _SubscriptionPlansPageState extends ConsumerState<SubscriptionPlansPage> {
  List<Map<String, dynamic>> _companies = [];
  bool _loadingCompanies = true;
  String? _companiesError;

  @override
  void initState() {
    super.initState();
    _loadCompanies();
  }

  Future<void> _loadCompanies() async {
    setState(() { _loadingCompanies = true; _companiesError = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('companies/all');
      final data = response.data as List<dynamic>;
      setState(() {
        _companies = data.map((e) => Map<String, dynamic>.from(e as Map)).toList();
        _loadingCompanies = false;
      });
    } catch (e) {
      if (mounted) setState(() { _companiesError = e.toString(); _loadingCompanies = false; });
    }
  }

  int _countByPlan(String planId) => _companies.where((c) =>
      (c['subscriptionPlan'] as String? ?? 'starter') == planId).length;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final plansAsync = ref.watch(subscriptionPlansProvider);

    return Scaffold(
      backgroundColor: isDark ? ZColors.darkBackground : ZColors.background,
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                IconButton(
                  icon: const Icon(Icons.refresh),
                  tooltip: 'Actualizar',
                  onPressed: () {
                    _loadCompanies();
                    ref.invalidate(subscriptionPlansProvider);
                  },
                ),
              ],
            ),
          ),
          Expanded(
            child: plansAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => _buildError('Error al cargar planes: $e'),
        data: (plans) => _loadingCompanies
            ? const Center(child: CircularProgressIndicator())
            : _companiesError != null
                ? _buildError(_companiesError!)
                : _buildContent(plans, isDark),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildContent(List<SubscriptionPlanConfig> plans, bool isDark) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildHeader(isDark),
          const SizedBox(height: 32),
          _buildStatsRow(plans, isDark),
          const SizedBox(height: 40),
          _buildPlansSection(plans, isDark),
          const SizedBox(height: 40),
          _buildFeatureComparison(plans, isDark),
          const SizedBox(height: 40),
          _buildCompaniesByPlan(plans, isDark),
          const SizedBox(height: 48),
        ],
      ),
    );
  }

  Widget _buildHeader(bool isDark) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Container(
              width: 48, height: 48,
              decoration: BoxDecoration(
                gradient: const LinearGradient(colors: ZColors.accentGradient),
                borderRadius: BorderRadius.circular(12),
              ),
              child: const Icon(Icons.workspace_premium, color: Colors.white, size: 24),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Gestión de Planes', style: ZTypography.headlineLarge),
                  Text('Administra los planes de suscripción y sus límites por empresa',
                    style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral500)),
                ],
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildStatsRow(List<SubscriptionPlanConfig> plans, bool isDark) {
    return ResponsiveGrid(
      mobileColumns: 1,
      tabletColumns: 2,
      desktopColumns: 3,
      children: plans.map((plan) => _buildStatCard(
        title: 'Empresas ${plan.name}',
        value: '${_countByPlan(plan.id)}',
        icon: plan.icon,
        color: plan.color,
        isDark: isDark,
      )).toList(),
    );
  }

  Widget _buildStatCard({
    required String title,
    required String value,
    required IconData icon,
    required Color color,
    required bool isDark,
  }) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: isDark ? ZColors.darkSurface : ZColors.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: isDark ? ZColors.darkBorder : ZColors.border),
        boxShadow: isDark ? [] : [
          BoxShadow(color: Colors.black.withValues(alpha: 0.03), blurRadius: 20, offset: const Offset(0, 4)),
        ],
      ),
      child: Row(
        children: [
          Container(
            width: 48, height: 48,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(icon, size: 24, color: color),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(value, style: ZTypography.displaySmall.copyWith(
                  fontWeight: FontWeight.w800, color: color)),
                Text(title, style: ZTypography.labelMedium.copyWith(
                  color: ZColors.neutral500)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildPlansSection(List<SubscriptionPlanConfig> plans, bool isDark) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Plan Disponible', style: ZTypography.headlineMedium),
        const SizedBox(height: 8),
        Text('Define el plan de cada empresa según sus necesidades', style: ZTypography.bodyMedium.copyWith(
          color: ZColors.neutral500)),
        const SizedBox(height: 24),
        ResponsiveGrid(
          mobileColumns: 1,
          tabletColumns: 2,
          desktopColumns: 3,
          children: plans.map((plan) => _buildPlanCard(
            config: plan,
            isDark: isDark,
            count: _countByPlan(plan.id),
          )).toList(),
        ),
      ],
    );
  }

  Widget _buildPlanCard({
    required SubscriptionPlanConfig config,
    required bool isDark,
    required int count,
  }) {
    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      decoration: BoxDecoration(
        color: isDark ? ZColors.darkSurface : ZColors.surface,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(
          color: config.isPopular ? config.color : (isDark ? ZColors.darkBorder : ZColors.border),
          width: config.isPopular ? 2 : 1,
        ),
        boxShadow: isDark ? [] : [
          BoxShadow(color: Colors.black.withValues(alpha: 0.04), blurRadius: 24, offset: const Offset(0, 8)),
        ],
      ),
      child: Column(
        children: [
          // Header
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(24),
            decoration: BoxDecoration(
              gradient: config.isPopular
                  ? LinearGradient(colors: [config.color.withValues(alpha: 0.15), config.color.withValues(alpha: 0.05)])
                  : null,
              borderRadius: const BorderRadius.vertical(top: Radius.circular(19)),
            ),
            child: Column(
              children: [
                if (config.isPopular)
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                    decoration: BoxDecoration(
                      color: config.color.withValues(alpha: 0.2),
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Text('MÁS POPULAR', style: ZTypography.labelSmall.copyWith(
                      color: config.color, fontWeight: FontWeight.w800)),
                  ),
                if (config.isPopular) const SizedBox(height: 12),
                Container(
                  width: 56, height: 56,
                  decoration: BoxDecoration(
                    color: config.color.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Icon(config.icon, size: 28, color: config.color),
                ),
                const SizedBox(height: 16),
                Text(config.name, style: ZTypography.headlineSmall),
                const SizedBox(height: 4),
                Wrap(
                  alignment: WrapAlignment.center,
                  crossAxisAlignment: WrapCrossAlignment.end,
                  spacing: 4,
                  children: [
                    Text(config.price, style: ZTypography.displaySmall.copyWith(
                      fontWeight: FontWeight.w800, color: config.color)),
                    Padding(
                      padding: const EdgeInsets.only(bottom: 4),
                      child: Text(config.period, style: ZTypography.bodySmall.copyWith(
                        color: ZColors.neutral500)),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                Text('$count empresas activas', style: ZTypography.labelSmall.copyWith(
                  color: ZColors.neutral400)),
              ],
            ),
          ),
          // Features
          Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('INCLUYE', style: ZTypography.labelSmall.copyWith(
                  color: ZColors.neutral500, letterSpacing: 1.2)),
                const SizedBox(height: 12),
                ...config.features.map((f) => Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Icon(Icons.check_circle_outline, size: 18, color: config.color),
                      const SizedBox(width: 10),
                      Expanded(child: Text(f, style: ZTypography.bodySmall.copyWith(
                        color: isDark ? ZColors.neutral300 : ZColors.neutral700))),
                    ],
                  ),
                )),
                if (config.limitations.isNotEmpty) ...[
                  const SizedBox(height: 16),
                  Text('LIMITACIONES', style: ZTypography.labelSmall.copyWith(
                    color: ZColors.neutral500, letterSpacing: 1.2)),
                  const SizedBox(height: 12),
                  ...config.limitations.map((l) => Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Icon(Icons.cancel_outlined, size: 18, color: ZColors.neutral400),
                        const SizedBox(width: 10),
                        Expanded(child: Text(l, style: ZTypography.bodySmall.copyWith(
                          color: ZColors.neutral400))),
                      ],
                    ),
                  )),
                ],
              ],
            ),
          ),
          _buildModulesSection(config, isDark),
        ],
      ),
    );
  }

  /// Lista los módulos licenciables del plan + acceso al editor (SuperAdmin).
  Widget _buildModulesSection(SubscriptionPlanConfig config, bool isDark) {
    final labels = config.moduleLabels;
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(24, 0, 24, 24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Divider(color: isDark ? ZColors.darkBorder : ZColors.border, height: 1),
          const SizedBox(height: 16),
          // Wrap (y no Row) para que en tarjetas angostas el botón baje de
          // línea en lugar de desbordar la tarjeta.
          Wrap(
            alignment: WrapAlignment.spaceBetween,
            crossAxisAlignment: WrapCrossAlignment.center,
            spacing: 12,
            runSpacing: 8,
            children: [
              Text('MÓDULOS DEL PLAN', style: ZTypography.labelSmall.copyWith(
                color: ZColors.neutral500, letterSpacing: 1.2)),
              if (config.isEditable)
                Tooltip(
                  message: 'Define qué módulos del ERP incluye este plan',
                  child: ZButton(
                    text: 'Editar módulos',
                    icon: Icons.tune_outlined,
                    type: ZButtonType.secondary,
                    fullWidth: false,
                    onPressed: () => _editModules(config),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 12),
          if (labels.isEmpty)
            Text('Sin módulos configurados', style: ZTypography.bodySmall.copyWith(
              color: ZColors.neutral400))
          else
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: labels
                  .map((label) => ZBadge(text: label, type: ZBadgeType.neutral))
                  .toList(),
            ),
        ],
      ),
    );
  }

  Future<void> _editModules(SubscriptionPlanConfig config) async {
    final selected = await showZAdaptiveDialog<List<String>>(
      context,
      builder: (_) => _PlanModulesDialog(config: config),
    );
    if (selected == null || !mounted) return;
    await _saveModules(config, selected);
  }

  Future<void> _saveModules(
    SubscriptionPlanConfig config,
    List<String> modules,
  ) async {
    final planGuid = config.planGuid;
    if (planGuid == null) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put(
        SubscriptionPlanEndpoints.modulesPath(planGuid),
        data: SubscriptionPlanEndpoints.modulesBody(modules),
      );
      if (!mounted) return;
      ref.invalidate(subscriptionPlansProvider);
      ZToast.show(
        context,
        'Módulos de "${config.name}" actualizados. Las compañías del plan verán '
        'el cambio al refrescar su sesión.',
        type: ZToastType.success,
      );
    } on DioException catch (e) {
      if (!mounted) return;
      final data = e.response?.data;
      final message = data is Map && data['error'] != null
          ? '${data['error']}'
          : 'No se pudieron guardar los módulos del plan';
      ZToast.show(context, message, type: ZToastType.error);
    } catch (_) {
      if (!mounted) return;
      ZToast.show(context, 'No se pudieron guardar los módulos del plan',
          type: ZToastType.error);
    }
  }

  Widget _buildFeatureComparison(List<SubscriptionPlanConfig> plans, bool isDark) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Comparativa de Planes', style: ZTypography.headlineMedium),
        const SizedBox(height: 8),
        Text('Detalles de las funcionalidades incluidas en cada plan', style: ZTypography.bodyMedium.copyWith(
          color: ZColors.neutral500)),
        const SizedBox(height: 24),
        Container(
          decoration: BoxDecoration(
            color: isDark ? ZColors.darkSurface : ZColors.surface,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: isDark ? ZColors.darkBorder : ZColors.border),
          ),
          child: Column(
            children: [
              // Header
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
                decoration: BoxDecoration(
                  color: isDark ? ZColors.darkCard : ZColors.neutral50,
                  borderRadius: const BorderRadius.vertical(top: Radius.circular(15)),
                ),
                child: Row(
                  children: [
                    Expanded(flex: 3, child: Text('Funcionalidad', style: ZTypography.labelMedium.copyWith(
                      color: ZColors.neutral500, letterSpacing: 1.0))),
                    for (final plan in plans)
                      Expanded(flex: 2, child: Text(plan.name, textAlign: TextAlign.center,
                        style: ZTypography.labelMedium.copyWith(color: plan.color, fontWeight: FontWeight.w700))),
                  ],
                ),
              ),
              // Rows
              for (int i = 0; i < SubscriptionPlanConfig.comparisonRows.length; i++)
                _buildComparisonRow(SubscriptionPlanConfig.comparisonRows[i], i, isDark),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildComparisonRow((String, String, String, String) feature, int index, bool isDark) {
    final isEven = index % 2 == 0;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
      color: isEven ? Colors.transparent : (isDark ? ZColors.darkCard.withValues(alpha: 0.3) : ZColors.neutral50),
      child: Row(
        children: [
          Expanded(flex: 3, child: Text(feature.$1, style: ZTypography.bodyMedium.copyWith(
            fontWeight: FontWeight.w500))),
          Expanded(flex: 2, child: Text(feature.$2, textAlign: TextAlign.center,
            style: ZTypography.bodySmall.copyWith(
              color: feature.$2 == '—' ? ZColors.neutral400 : null))),
          Expanded(flex: 2, child: Text(feature.$3, textAlign: TextAlign.center,
            style: ZTypography.bodySmall.copyWith(
              color: feature.$3 == '—' ? ZColors.neutral400 : (feature.$3 == '✓' ? ZColors.brandAccent : null),
              fontWeight: feature.$3 == '✓' ? FontWeight.w700 : null))),
          Expanded(flex: 2, child: Text(feature.$4, textAlign: TextAlign.center,
            style: ZTypography.bodySmall.copyWith(
              color: feature.$4 == '—' ? ZColors.neutral400 : (feature.$4 == '✓' ? ZColors.brandGold : null),
              fontWeight: feature.$4 == '✓' ? FontWeight.w700 : null))),
        ],
      ),
    );
  }

  Widget _buildCompaniesByPlan(List<SubscriptionPlanConfig> plans, bool isDark) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Empresas por Plan', style: ZTypography.headlineMedium),
        const SizedBox(height: 8),
        Text('Distribución actual de empresas en cada plan de suscripción',
          style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral500)),
        const SizedBox(height: 24),
        for (int i = 0; i < plans.length; i++) ...[
          if (i > 0) const SizedBox(height: 16),
          _buildPlanGroup(plans[i], isDark),
        ],
      ],
    );
  }

  Widget _buildPlanGroup(SubscriptionPlanConfig config, bool isDark) {
    final color = config.color;
    final companies = _companies.where((c) =>
        (c['subscriptionPlan'] as String? ?? 'starter') == config.id).toList();

    return Container(
      decoration: BoxDecoration(
        color: isDark ? ZColors.darkSurface : ZColors.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: isDark ? ZColors.darkBorder : ZColors.border),
      ),
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                Container(
                  width: 36, height: 36,
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Icon(config.icon, size: 18, color: color),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    config.name,
                    style: ZTypography.titleMedium,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                const SizedBox(width: 8),
                ZBadge(text: '${companies.length}', type: ZBadgeType.neutral),
                const SizedBox(width: 8),
                Text('${companies.length} empresas', style: ZTypography.labelSmall.copyWith(
                  color: ZColors.neutral400)),
              ],
            ),
          ),
          if (companies.isEmpty)
            Padding(
              padding: const EdgeInsets.all(24),
              child: Text('No hay empresas en este plan',
                style: ZTypography.bodySmall.copyWith(color: ZColors.neutral400)),
            )
          else
            ...companies.take(5).map((c) => ListTile(
              dense: true,
              leading: CircleAvatar(
                radius: 16,
                backgroundColor: color.withValues(alpha: 0.1),
                child: Icon(Icons.business, size: 14, color: color),
              ),
              title: Text(c['name'] as String? ?? '', style: ZTypography.bodyMedium.copyWith(
                fontWeight: FontWeight.w500)),
              subtitle: Text('${c['country'] ?? ''} · ${c['maxEmployees'] ?? 0} trabajadores',
                style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
              trailing: ZBadge(
                text: (c['isActive'] == true) ? 'Activa' : 'Inactiva',
                type: (c['isActive'] == true) ? ZBadgeType.success : ZBadgeType.danger,
              ),
            )),
          if (companies.length > 5)
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: Text('... y ${companies.length - 5} más',
                style: ZTypography.labelSmall.copyWith(color: ZColors.neutral400)),
            ),
        ],
      ),
    );
  }

  Widget _buildError(String message) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.cloud_off, size: 48, color: ZColors.danger),
            const SizedBox(height: 16),
            Text('Error al cargar datos', style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            Text(message, style: ZTypography.labelSmall, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: () {
                _loadCompanies();
                ref.invalidate(subscriptionPlansProvider);
              },
              icon: const Icon(Icons.refresh, size: 18),
              label: const Text('Reintentar'),
            ),
          ],
        ),
      ),
    );
  }
}

/// Editor de módulos licenciables de un plan (SuperAdmin).
///
/// Devuelve por `Navigator.pop` la lista de `ModuleType` names seleccionada, o
/// `null` si se cancela. No llama al API: el guardado lo hace la página.
class _PlanModulesDialog extends StatefulWidget {
  const _PlanModulesDialog({required this.config});

  final SubscriptionPlanConfig config;

  @override
  State<_PlanModulesDialog> createState() => _PlanModulesDialogState();
}

class _PlanModulesDialogState extends State<_PlanModulesDialog> {
  late Set<String> _selected;

  @override
  void initState() {
    super.initState();
    _selected = widget.config.modules.toSet();
  }

  void _toggle(String key, bool? value) {
    if (value == null) return;
    setState(() {
      value ? _selected.add(key) : _selected.remove(key);
    });
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final empty = _selected.isEmpty;
    // El orden del catálogo es el orden de guardado que verá el API.
    final ordered = SubscriptionPlanConfig.availableModules
        .where((o) => _selected.contains(o.key))
        .map((o) => o.key)
        .toList();

    return ZResponsiveDialog(
      title: 'Módulos de ${widget.config.name}',
      icon: Icons.extension_outlined,
      maxWidth: 520,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
              Text(
                'Define qué módulos del ERP incluye este plan. Las compañías '
                'suscriptas solo verán la navegación de los módulos habilitados.',
                style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  TextButton(
                    onPressed: () => setState(() => _selected = {
                      ...SubscriptionPlanConfig.availableModules.map((o) => o.key)
                    }),
                    child: const Text('Seleccionar todos'),
                  ),
                  TextButton(
                    onPressed: () => setState(() => _selected = {}),
                    child: const Text('Ninguno'),
                  ),
                  const Spacer(),
                  ZBadge(
                    text: '${_selected.length}/${SubscriptionPlanConfig.availableModules.length}',
                    type: ZBadgeType.accent,
                  ),
                ],
              ),
              const SizedBox(height: 4),
              for (final option in SubscriptionPlanConfig.availableModules)
                SwitchListTile(
                  dense: true,
                  contentPadding: const EdgeInsets.symmetric(horizontal: 4),
                  secondary: Icon(option.icon, size: 20, color: ZColors.brandAccent),
                  title: Text(option.label, style: ZTypography.bodyMedium),
                  subtitle: Text(option.key, style: ZTypography.labelSmall.copyWith(
                    color: ZColors.neutral400)),
                  value: _selected.contains(option.key),
                  onChanged: (value) => _toggle(option.key, value),
                ),
              if (empty) ...[
                const SizedBox(height: 8),
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: ZColors.warning.withValues(alpha: 0.08),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: ZColors.warning.withValues(alpha: 0.4)),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.warning_amber_outlined, size: 18, color: ZColors.warning),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Text(
                          'Sin módulos seleccionados, las compañías de este plan '
                          'no verán NINGÚN módulo del ERP.',
                          style: ZTypography.bodySmall.copyWith(
                            color: isDark ? ZColors.neutral200 : ZColors.neutral700),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ],
          ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancelar'),
        ),
        ZButton(
          text: empty ? 'Guardar sin módulos' : 'Guardar',
          icon: Icons.save_outlined,
          type: empty ? ZButtonType.danger : ZButtonType.primary,
          fullWidth: false,
          onPressed: () => Navigator.of(context).pop(List<String>.from(ordered)),
        ),
      ],
    );
  }
}
