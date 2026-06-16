import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_predictive_provider.dart';

/// Fleet Predictive Intelligence page — maintenance forecasts, risk scores,
/// fuel anomaly detection, and consumption trends using full DS.
final class FleetPredictivePage extends ConsumerStatefulWidget {
  const FleetPredictivePage({super.key});

  @override
  ConsumerState<FleetPredictivePage> createState() => _FleetPredictivePageState();
}

class _FleetPredictivePageState extends ConsumerState<FleetPredictivePage>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(fleetPredictiveProvider.notifier).loadSummary();
      ref.read(fleetPredictiveProvider.notifier).loadForecasts();
      ref.read(fleetPredictiveProvider.notifier).loadFuelAnomalies();
      ref.read(fleetPredictiveProvider.notifier).loadFuelTrends();
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetPredictiveProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Inteligencia Predictiva'),
        bottom: TabBar(
          controller: _tabController,
          tabs: const [
            Tab(text: 'Mantenimiento', icon: Icon(Icons.build_outlined)),
            Tab(text: 'Combustible', icon: Icon(Icons.local_gas_station_outlined)),
            Tab(text: 'Tendencias', icon: Icon(Icons.trending_up_outlined)),
          ],
        ),
      ),
      body: state.loading && state.forecasts.isEmpty && state.fuelAnomalies.isEmpty
          ? _buildSkeleton()
          : state.error != null && state.forecasts.isEmpty && state.fuelAnomalies.isEmpty
              ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
              : TabBarView(
                  controller: _tabController,
                  children: [
                    _buildMaintenanceTab(state, theme),
                    _buildFuelAnomalyTab(state, theme),
                    _buildTrendsTab(state, theme),
                  ],
                ),
    );
  }

  Widget _buildSkeleton() {
    return Padding(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        children: [
          Row(
            children: [
              Expanded(child: ZSkeleton.statCard()),
              const SizedBox(width: ZSpacing.md),
              Expanded(child: ZSkeleton.statCard()),
              const SizedBox(width: ZSpacing.md),
              Expanded(child: ZSkeleton.statCard()),
              const SizedBox(width: ZSpacing.md),
              Expanded(child: ZSkeleton.statCard()),
            ],
          ),
          const SizedBox(height: ZSpacing.lg),
          ...List.generate(4, (_) => Padding(
            padding: const EdgeInsets.only(bottom: ZSpacing.md),
            child: ZSkeleton.listTile(),
          )),
        ],
      ),
    );
  }

  // ── Maintenance Tab ──

  Widget _buildMaintenanceTab(FleetPredictiveState state, ThemeData theme) {
    final summary = state.maintenanceSummary;
    final forecasts = state.forecasts;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Summary KPI Cards ──
          if (summary != null) ...[
            LayoutBuilder(
              builder: (context, constraints) {
                final crossCount = constraints.maxWidth > 800 ? 4 : 2;
                return GridView.count(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  crossAxisCount: crossCount,
                  mainAxisSpacing: ZSpacing.md,
                  crossAxisSpacing: ZSpacing.md,
                  childAspectRatio: 2.2,
                  children: [
                    ZStatCard(
                      title: 'Total Vehículos',
                      value: '${summary['totalVehicles'] ?? 0}',
                      icon: Icons.local_shipping_outlined,
                      variant: ZStatVariant.module,
                      moduleColor: ZColors.moduleFleet,
                    ),
                    ZStatCard(
                      title: 'Vencidos',
                      value: '${summary['vehiclesOverdue'] ?? 0}',
                      icon: Icons.warning_amber_outlined,
                      variant: ZStatVariant.danger,
                    ),
                    ZStatCard(
                      title: 'Por Vencer',
                      value: '${summary['vehiclesDueSoon'] ?? 0}',
                      icon: Icons.schedule_outlined,
                      variant: ZStatVariant.warning,
                    ),
                    ZStatCard(
                      title: 'Riesgo Promedio',
                      value: '${summary['avgRiskScore'] ?? 0}',
                      icon: Icons.speed_outlined,
                      variant: ZStatVariant.info,
                    ),
                  ],
                );
              },
            ),
            const SizedBox(height: ZSpacing.xl),
          ],

          // ── Forecasts List ──
          Text('Pronósticos por Vehículo',
              style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.md),

          if (forecasts.isEmpty)
            const ZEmptyState(
              icon: Icons.build_outlined,
              title: 'Sin pronósticos',
              subtitle: 'No hay datos de mantenimiento predictivo disponibles',
            )
          else
            ...forecasts.map((f) => _buildForecastCard(f, theme)),
        ],
      ),
    );
  }

  Widget _buildForecastCard(Map<String, dynamic> forecast, ThemeData theme) {
    final plate = forecast['plate'] ?? 'N/A';
    final brandModel = forecast['brandModel'] ?? '';
    final riskScore = (forecast['riskScore'] as num?)?.toInt() ?? 0;
    final riskLevel = forecast['riskLevel'] ?? 'Low';
    final components = (forecast['components'] as List?) ?? [];

    return ZCard(
      margin: const EdgeInsets.only(bottom: ZSpacing.md),
      child: Padding(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header: plate, brand/model, risk badge
            Row(
              children: [
                Icon(Icons.local_shipping_outlined, color: ZColors.moduleFleet, size: 24),
                const SizedBox(width: ZSpacing.sm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(plate, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
                      Text(brandModel, style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurface.withValues(alpha: 0.6),
                      )),
                    ],
                  ),
                ),
                _riskBadge(riskLevel, riskScore),
              ],
            ),
            const SizedBox(height: ZSpacing.md),

            // Risk score progress bar
            Row(
              children: [
                Text('Riesgo: $riskScore%', style: theme.textTheme.bodySmall),
                const SizedBox(width: ZSpacing.sm),
                Expanded(
                  child: ZProgress(
                    label: 'Riesgo',
                    value: riskScore / 100.0,
                    color: _riskColor(riskLevel),
                  ),
                ),
              ],
            ),
            const SizedBox(height: ZSpacing.md),

            // Components
            if (components.isNotEmpty)
              Wrap(
                spacing: ZSpacing.sm,
                runSpacing: ZSpacing.xs,
                children: components.take(6).map((c) {
                  final name = c['componentName'] ?? '';
                  final status = c['status'] ?? 'Healthy';
                  final daysUntilDue = c['daysUntilDue'] ?? 0;
                  final kmUntilDue = c['kmUntilDue'] ?? 0;

                  return Chip(
                    avatar: Icon(
                      status == 'Overdue' ? Icons.error_outline :
                      status == 'DueSoon' ? Icons.warning_amber_outlined :
                      Icons.check_circle_outline,
                      size: 16,
                      color: _statusColor(status),
                    ),
                    label: Text('$name (${daysUntilDue}d / ${kmUntilDue}km)',
                      style: const TextStyle(fontSize: 11)),
                    visualDensity: VisualDensity.compact,
                    padding: EdgeInsets.zero,
                  );
                }).toList(),
              ),
          ],
        ),
      ),
    );
  }

  // ── Fuel Anomaly Tab ──

  Widget _buildFuelAnomalyTab(FleetPredictiveState state, ThemeData theme) {
    final anomalies = state.fuelAnomalies;
    final fuelSummary = state.fuelSummary;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Summary KPI Cards ──
          if (fuelSummary != null) ...[
            LayoutBuilder(
              builder: (context, constraints) {
                final crossCount = constraints.maxWidth > 800 ? 4 : 2;
                return GridView.count(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  crossAxisCount: crossCount,
                  mainAxisSpacing: ZSpacing.md,
                  crossAxisSpacing: ZSpacing.md,
                  childAspectRatio: 2.2,
                  children: [
                    ZStatCard(
                      title: 'Abastecimientos Analizados',
                      value: '${fuelSummary['totalRefillsAnalyzed'] ?? 0}',
                      icon: Icons.analytics_outlined,
                      variant: ZStatVariant.module,
                      moduleColor: ZColors.moduleFleet,
                    ),
                    ZStatCard(
                      title: 'Anomalías Detectadas',
                      value: '${fuelSummary['anomaliesDetected'] ?? 0}',
                      icon: Icons.bug_report_outlined,
                      variant: ZStatVariant.danger,
                    ),
                    ZStatCard(
                      title: 'Alta Severidad',
                      value: '${fuelSummary['highSeverity'] ?? 0}',
                      icon: Icons.error_outline,
                      variant: ZStatVariant.danger,
                    ),
                    ZStatCard(
                      title: 'Pérdida Estimada',
                      value: '\$${fuelSummary['estimatedMonthlyWaste'] ?? 0}',
                      icon: Icons.attach_money_outlined,
                      variant: ZStatVariant.warning,
                    ),
                  ],
                );
              },
            ),
            const SizedBox(height: ZSpacing.xl),
          ],

          // ── Anomalies List ──
          Text('Anomalías de Combustible',
              style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.md),

          if (anomalies.isEmpty)
            const ZEmptyState(
              icon: Icons.local_gas_station_outlined,
              title: 'Sin anomalías',
              subtitle: 'No se detectaron anomalías en el consumo de combustible',
            )
          else
            ...anomalies.map((a) => _buildAnomalyCard(a, theme)),
        ],
      ),
    );
  }

  Widget _buildAnomalyCard(Map<String, dynamic> anomaly, ThemeData theme) {
    final plate = anomaly['plate'] ?? 'N/A';
    final anomalyType = anomaly['anomalyType'] ?? '';
    final severity = anomaly['severity'] ?? 'Low';
    final description = anomaly['description'] ?? '';
    final liters = (anomaly['liters'] as num?)?.toDouble() ?? 0;
    final cost = (anomaly['totalCost'] as num?)?.toDouble() ?? 0;
    final deviation = (anomaly['deviationPercent'] as num?)?.toDouble() ?? 0;

    return ZCard(
      margin: const EdgeInsets.only(bottom: ZSpacing.md),
      child: Padding(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Severity indicator
            Container(
              width: 4,
              height: 60,
              decoration: BoxDecoration(
                color: _anomalySeverityColor(severity),
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            const SizedBox(width: ZSpacing.md),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      ZBadge(text: plate, type: ZBadgeType.neutral),
                      const SizedBox(width: ZSpacing.sm),
                      ZBadge(text: _anomalyTypeLabel(anomalyType), type: _anomalySeverityBadgeType(severity)),
                      const SizedBox(width: ZSpacing.sm),
                      ZBadge(text: severity, type: _anomalySeverityBadgeType(severity)),
                    ],
                  ),
                  const SizedBox(height: ZSpacing.xs),
                  Text(description, style: theme.textTheme.bodySmall),
                  const SizedBox(height: ZSpacing.xs),
                  Text('${liters.toStringAsFixed(1)}L · \$${cost.toStringAsFixed(2)} · +${deviation.toStringAsFixed(0)}%',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurface.withValues(alpha: 0.5),
                    )),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ── Trends Tab ──

  Widget _buildTrendsTab(FleetPredictiveState state, ThemeData theme) {
    final trends = state.fuelTrends;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Tendencias de Consumo por Vehículo',
              style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.md),

          if (trends.isEmpty)
            const ZEmptyState(
              icon: Icons.trending_up_outlined,
              title: 'Sin tendencias',
              subtitle: 'No hay datos suficientes para calcular tendencias de consumo',
            )
          else
            ...trends.map((t) => _buildTrendCard(t, theme)),
        ],
      ),
    );
  }

  Widget _buildTrendCard(Map<String, dynamic> trend, ThemeData theme) {
    final plate = trend['plate'] ?? 'N/A';
    final avg = (trend['avgConsumptionPer100Km'] as num?)?.toDouble() ?? 0;
    final min = (trend['minConsumptionPer100Km'] as num?)?.toDouble() ?? 0;
    final max = (trend['maxConsumptionPer100Km'] as num?)?.toDouble() ?? 0;
    final stdDev = (trend['stdDeviation'] as num?)?.toDouble() ?? 0;
    final currentTrend = (trend['currentTrend'] as num?)?.toDouble() ?? 0;
    final sampleCount = trend['sampleCount'] ?? 0;

    final trendDirection = currentTrend > 0.5 ? 'up' : currentTrend < -0.5 ? 'down' : 'stable';
    final trendColor = trendDirection == 'up' ? ZColors.danger :
                       trendDirection == 'down' ? ZColors.success : ZColors.neutral400;
    final trendIcon = trendDirection == 'up' ? Icons.trending_up :
                      trendDirection == 'down' ? Icons.trending_down : Icons.trending_flat;

    return ZCard(
      margin: const EdgeInsets.only(bottom: ZSpacing.md),
      child: Padding(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.local_gas_station_outlined, color: ZColors.moduleFleet, size: 20),
                const SizedBox(width: ZSpacing.sm),
                Text(plate, style: const TextStyle(fontWeight: FontWeight.w700)),
                const Spacer(),
                Icon(trendIcon, color: trendColor, size: 20),
                const SizedBox(width: ZSpacing.xs),
                Text('${currentTrend > 0 ? '+' : ''}${currentTrend.toStringAsFixed(1)} L/100km',
                  style: TextStyle(color: trendColor, fontWeight: FontWeight.w600, fontSize: 13)),
              ],
            ),
            const SizedBox(height: ZSpacing.sm),
            Row(
              children: [
                _trendStat('Promedio', '${avg.toStringAsFixed(1)} L/100km', theme),
                const SizedBox(width: ZSpacing.lg),
                _trendStat('Mín', min.toStringAsFixed(1), theme),
                const SizedBox(width: ZSpacing.lg),
                _trendStat('Máx', max.toStringAsFixed(1), theme),
                const SizedBox(width: ZSpacing.lg),
                _trendStat('Std Dev', stdDev.toStringAsFixed(1), theme),
                const SizedBox(width: ZSpacing.lg),
                _trendStat('Muestras', '$sampleCount', theme),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _trendStat(String label, String value, ThemeData theme) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: theme.textTheme.bodySmall?.copyWith(
          color: theme.colorScheme.onSurface.withValues(alpha: 0.5), fontSize: 10)),
        Text(value, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
      ],
    );
  }

  // ── Helpers ──

  Widget _riskBadge(String level, int score) {
    final type = level == 'Critical' ? ZBadgeType.danger :
                 level == 'High' ? ZBadgeType.warning :
                 level == 'Medium' ? ZBadgeType.info : ZBadgeType.success;
    return ZBadge(text: '$level ($score%)', type: type);
  }

  Color _riskColor(String level) => switch (level) {
    'Critical' => ZColors.danger,
    'High' => ZColors.warning,
    'Medium' => ZColors.info,
    _ => ZColors.success,
  };

  Color _statusColor(String status) => switch (status) {
    'Overdue' => ZColors.danger,
    'DueSoon' => ZColors.warning,
    'Scheduled' => ZColors.info,
    _ => ZColors.success,
  };

  Color _anomalySeverityColor(String severity) => switch (severity) {
    'High' => ZColors.danger,
    'Medium' => ZColors.warning,
    _ => ZColors.info,
  };

  ZBadgeType _anomalySeverityBadgeType(String severity) => switch (severity) {
    'High' => ZBadgeType.danger,
    'Medium' => ZBadgeType.warning,
    _ => ZBadgeType.info,
  };

  String _anomalyTypeLabel(String type) => switch (type) {
    'HighConsumption' => 'Alto Consumo',
    'SuddenIncrease' => 'Aumento Súbito',
    'PriceOutlier' => 'Precio Anómalo',
    'FrequencyAnomaly' => 'Frecuencia',
    'VolumeOutlier' => 'Volumen Anómalo',
    _ => type,
  };
}
