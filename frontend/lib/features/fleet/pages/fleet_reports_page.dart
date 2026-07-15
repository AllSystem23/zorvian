import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_reports_provider.dart';

/// Fleet Reports page — fully using project Design System components.
final class FleetReportsPage extends ConsumerStatefulWidget {
  const FleetReportsPage({super.key});

  @override
  ConsumerState<FleetReportsPage> createState() => _FleetReportsPageState();
}

class _FleetReportsPageState extends ConsumerState<FleetReportsPage>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    _tabController.addListener(_onTabChanged);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(fleetReportProvider.notifier).loadReport();
    });
  }

  void _onTabChanged() {
    if (_tabController.indexIsChanging) return;
    final tabs = FleetReportTab.values;
    ref.read(fleetReportProvider.notifier).setTab(tabs[_tabController.index]);
  }

  @override
  void dispose() {
    _tabController.removeListener(_onTabChanged);
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetReportProvider);
    final theme = Theme.of(context);

    return Scaffold(
      body: Column(
        children: [
          TabBar(
            controller: _tabController,
            indicatorColor: ZColors.moduleFleet,
            labelColor: ZColors.moduleFleet,
            unselectedLabelColor: theme.colorScheme.onSurface.withValues(alpha: 0.6),
            tabs: const [
              Tab(text: 'Operativos', icon: Icon(Icons.speed_outlined, size: 20)),
              Tab(text: 'Financieros', icon: Icon(Icons.account_balance_outlined, size: 20)),
              Tab(text: 'Gerenciales', icon: Icon(Icons.analytics_outlined, size: 20)),
            ],
          ),
          Row(
            children: [
              _DateRangeButton(state: state),
              const SizedBox(width: 8),
            ],
          ),
          // ── Report selector chips (ZFilterBar) ──
          _ReportChips(state: state),

          // ── Export bar (ZButton) ──
          _ExportBar(state: state),

          // ── Content ──
          Expanded(
            child: state.loading
                ? _buildLoadingSkeleton()
                : state.error != null
                    ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
                    : state.data == null
                        ? const ZEmptyState(
                            icon: Icons.analytics_outlined,
                            title: 'Selecciona un reporte',
                            subtitle: 'Elige un tipo de reporte arriba para comenzar',
                          )
                        : _ReportContent(data: state.data!, reportType: state.activeReport),
          ),
        ],
      ),
    );
  }

  Widget _buildLoadingSkeleton() {
    return Padding(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        children: [
          ZSkeleton.header(),
          const SizedBox(height: ZSpacing.lg),
          ...List.generate(4, (_) => Padding(
            padding: const EdgeInsets.only(bottom: ZSpacing.md),
            child: ZSkeleton.statCard(),
          )),
        ],
      ),
    );
  }
}

// ══════════════════════════════════════════════
//  REPORT TYPE CHIPS (ZFilterBar)
// ══════════════════════════════════════════════

class _ReportChips extends StatelessWidget {
  final FleetReportState state;
  const _ReportChips({required this.state});

  @override
  Widget build(BuildContext context) {
    final reports = reportsForTab(state.activeTab);
    return Consumer(
      builder: (context, ref, _) {
        return ZFilterBar(
          filters: reports.map((r) => ZFilterChipData(
            label: r.label,
            value: r.apiPath,
            icon: _iconFor(r),
          )).toList(),
          activeFilter: state.activeReport.apiPath,
          onFilterChanged: (value) {
            if (value == null) return;
            final match = reports.firstWhere((r) => r.apiPath == value);
            ref.read(fleetReportProvider.notifier).setReport(match);
          },
          showSearch: false,
        );
      },
    );
  }

  IconData _iconFor(FleetReportType r) => switch (r) {
        FleetReportType.vehicleUsage => Icons.time_to_leave_outlined,
        FleetReportType.delivery => Icons.inventory_2_outlined,
        FleetReportType.route => Icons.alt_route_outlined,
        FleetReportType.costSummary => Icons.pie_chart_outline,
        FleetReportType.costByVehicle => Icons.bar_chart_outlined,
        FleetReportType.costTrend => Icons.show_chart_outlined,
        FleetReportType.profitability => Icons.trending_up_outlined,
        FleetReportType.fleetKpi => Icons.dashboard_outlined,
        FleetReportType.driverScorecard => Icons.person_outline,
        FleetReportType.vehicleScorecard => Icons.time_to_leave_outlined,
        FleetReportType.fuelTrend => Icons.local_gas_station_outlined,
        FleetReportType.expenseByAccount => Icons.account_balance_outlined,
      };
}

// ══════════════════════════════════════════════
//  EXPORT BAR (ZButton)
// ══════════════════════════════════════════════

class _ExportBar extends StatelessWidget {
  final FleetReportState state;
  const _ExportBar({required this.state});

  @override
  Widget build(BuildContext context) {
    return Consumer(
      builder: (context, ref, _) {
        return Container(
          padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.sm),
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surfaceContainerLow,
            border: Border(
              bottom: BorderSide(color: Theme.of(context).dividerColor.withValues(alpha: 0.2)),
            ),
          ),
          child: Row(
            children: [
              Icon(Icons.download_outlined, size: 18, color: ZColors.moduleFleet),
              const SizedBox(width: ZSpacing.sm),
              Text('Exportar:', style: TextStyle(
                fontSize: 13, fontWeight: FontWeight.w500,
                color: Theme.of(context).colorScheme.onSurface,
              )),
              const SizedBox(width: ZSpacing.md),
              ZButton(
                text: 'PDF',
                icon: Icons.picture_as_pdf_outlined,
                onPressed: () => ref.read(fleetReportProvider.notifier).exportReport('pdf'),
                type: ZButtonType.secondary,
                fullWidth: false,
                isLoading: state.exporting,
              ),
              const SizedBox(width: ZSpacing.sm),
              ZButton(
                text: 'Excel',
                icon: Icons.table_chart_outlined,
                onPressed: () => ref.read(fleetReportProvider.notifier).exportReport('xlsx'),
                type: ZButtonType.secondary,
                fullWidth: false,
                isLoading: state.exporting,
              ),
            ],
          ),
        );
      },
    );
  }
}

// ══════════════════════════════════════════════
//  DATE RANGE BUTTON
// ══════════════════════════════════════════════

class _DateRangeButton extends StatelessWidget {
  final FleetReportState state;
  const _DateRangeButton({required this.state});

  @override
  Widget build(BuildContext context) {
    final label = state.startDate != null && state.endDate != null
        ? '${_fmt(state.startDate!)} — ${_fmt(state.endDate!)}'
        : 'Todo el período';
    return Consumer(
      builder: (context, ref, _) {
        return TextButton.icon(
          onPressed: () async {
            final now = DateTime.now();
            final picked = await showDateRangePicker(
              context: context,
              firstDate: DateTime(2020),
              lastDate: now,
              initialDateRange: state.startDate != null && state.endDate != null
                  ? DateTimeRange(start: state.startDate!, end: state.endDate!)
                  : DateTimeRange(start: now.subtract(const Duration(days: 365)), end: now),
            );
            if (picked != null) {
              ref.read(fleetReportProvider.notifier).setDateRange(picked.start, picked.end);
            }
          },
          icon: const Icon(Icons.date_range_outlined, size: 18),
          label: Text(label, style: const TextStyle(fontSize: 11)),
          style: TextButton.styleFrom(
            foregroundColor: Theme.of(context).colorScheme.onSurface,
            padding: const EdgeInsets.symmetric(horizontal: 12),
          ),
        );
      },
    );
  }

  String _fmt(DateTime d) => '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';
}

// ══════════════════════════════════════════════
//  REPORT CONTENT DISPATCHER
// ══════════════════════════════════════════════

class _ReportContent extends StatelessWidget {
  final Map<String, dynamic> data;
  final FleetReportType reportType;
  const _ReportContent({required this.data, required this.reportType});

  @override
  Widget build(BuildContext context) {
    return switch (reportType) {
      FleetReportType.fleetKpi => _KpiDashboard(data: data),
      FleetReportType.costSummary => _CostSummaryView(data: data),
      FleetReportType.costTrend => _CostTrendView(data: data),
      FleetReportType.fuelTrend => _FuelTrendView(data: data),
      FleetReportType.expenseByAccount => _ExpenseByAccountView(data: data),
      _ => _DataTableReport(data: data, reportType: reportType),
    };
  }
}

// ══════════════════════════════════════════════
//  KPI DASHBOARD (ZStatCard)
// ══════════════════════════════════════════════

class _KpiDashboard extends StatelessWidget {
  final Map<String, dynamic> data;
  const _KpiDashboard({required this.data});

  @override
  Widget build(BuildContext context) {
    final kpis = [
      _KpiData('Total Vehículos', '${data['totalVehicles'] ?? 0}', Icons.time_to_leave_outlined, ZStatVariant.module, ZColors.moduleFleet),
      _KpiData('Vehículos Activos', '${data['activeVehicles'] ?? 0}', Icons.check_circle_outlined, ZStatVariant.success, null),
      _KpiData('Disponibles', '${data['availableVehicles'] ?? 0}', Icons.inventory_outlined, ZStatVariant.info, null),
      _KpiData('En Mantenimiento', '${data['inMaintenanceVehicles'] ?? 0}', Icons.build_outlined, ZStatVariant.warning, null),
      _KpiData('Fuera de Servicio', '${data['outOfServiceVehicles'] ?? 0}', Icons.block_outlined, ZStatVariant.danger, null),
      _KpiData('Disponibilidad', '${data['fleetAvailabilityRate'] ?? 0}%', Icons.speed_outlined, ZStatVariant.module, ZColors.moduleFleet),
      _KpiData('Costo/Km', 'C\$${data['averageCostPerKm'] ?? 0}', Icons.attach_money_outlined, ZStatVariant.module, Colors.teal),
      _KpiData('Eficiencia', '${data['averageFuelEfficiency'] ?? 0} km/L', Icons.local_gas_station_outlined, ZStatVariant.success, null),
      _KpiData('Entregas Totales', '${data['totalDeliveries'] ?? 0}', Icons.inventory_2_outlined, ZStatVariant.info, null),
      _KpiData('Entregas Completadas', '${data['completedDeliveries'] ?? 0}', Icons.check_outlined, ZStatVariant.success, null),
      _KpiData('A Tiempo', '${data['onTimeDeliveryRate'] ?? 0}%', Icons.timer_outlined, ZStatVariant.module, ZColors.moduleFleet),
      _KpiData('Docs por Vencer', '${data['expiringDocuments'] ?? 0}', Icons.warning_amber_outlined, ZStatVariant.warning, null),
      _KpiData('Mant. Vencido', '${data['overdueMaintenance'] ?? 0}', Icons.error_outline, ZStatVariant.danger, null),
      _KpiData('OTs Abiertas', '${data['openWorkOrders'] ?? 0}', Icons.assignment_outlined, ZStatVariant.warning, null),
    ];

    return SingleChildScrollView(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('KPIs de Flota', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.lg),
          LayoutBuilder(
            builder: (context, constraints) {
              final crossCount = constraints.maxWidth > 900 ? 5
                  : constraints.maxWidth > 600 ? 4
                  : constraints.maxWidth > 400 ? 3 : 2;
              return GridView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: crossCount,
                  mainAxisSpacing: ZSpacing.md,
                  crossAxisSpacing: ZSpacing.md,
                  childAspectRatio: 1.4,
                ),
                itemCount: kpis.length,
                itemBuilder: (_, i) => ZStatCard(
                  title: kpis[i].label,
                  value: kpis[i].value,
                  icon: kpis[i].icon,
                  variant: kpis[i].variant,
                  moduleColor: kpis[i].moduleColor,
                ),
              );
            },
          ),
        ],
      ),
    );
  }
}

class _KpiData {
  final String label;
  final String value;
  final IconData icon;
  final ZStatVariant variant;
  final Color? moduleColor;
  const _KpiData(this.label, this.value, this.icon, this.variant, this.moduleColor);
}

// ══════════════════════════════════════════════
//  COST SUMMARY (ZStatCard + ZProgress)
// ══════════════════════════════════════════════

class _CostSummaryView extends StatelessWidget {
  final Map<String, dynamic> data;
  const _CostSummaryView({required this.data});

  @override
  Widget build(BuildContext context) {
    final categories = (data['categories'] as List?)?.map((e) => Map<String, dynamic>.from(e as Map)).toList() ?? [];
    final grandTotal = (data['grandTotal'] as num?)?.toDouble() ?? 0;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          ZStatCard(
            title: 'Costo Total del Período',
            value: 'C\$ ${grandTotal.toStringAsFixed(2)}',
            icon: Icons.account_balance_outlined,
            variant: ZStatVariant.module,
            moduleColor: ZColors.moduleFleet,
          ),
          const SizedBox(height: ZSpacing.lg),
          Text('Distribución por Categoría', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.md),
          ...categories.map((c) => Padding(
            padding: const EdgeInsets.only(bottom: ZSpacing.md),
            child: ZProgress(
              label: c['categoryName'] ?? '',
              value: grandTotal > 0 ? ((c['totalAmount'] as num?)?.toDouble() ?? 0) / grandTotal : 0,
              color: ZColors.moduleFleet,
              valueLabel: 'C\$ ${((c['totalAmount'] as num?)?.toDouble() ?? 0).toStringAsFixed(2)}',
            ),
          )),
        ],
      ),
    );
  }
}

// ══════════════════════════════════════════════
//  COST TREND (ZDataTable)
// ══════════════════════════════════════════════

class _CostTrendView extends StatelessWidget {
  final Map<String, dynamic> data;
  const _CostTrendView({required this.data});

  @override
  Widget build(BuildContext context) {
    final trends = (data['trends'] as List?)?.map((e) => Map<String, dynamic>.from(e as Map)).toList() ?? [];
    if (trends.isEmpty) {
      return const ZEmptyState(icon: Icons.show_chart_outlined, title: 'Sin datos de tendencia', subtitle: 'Registra gastos para ver la tendencia');
    }
    final maxTotal = trends.fold<double>(0, (m, t) {
      final v = (t['totalCost'] as num?)?.toDouble() ?? 0;
      return v > m ? v : m;
    });

    return SingleChildScrollView(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Tendencia de Costos', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.lg),
          // Bar chart
          SizedBox(
            height: 280,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: trends.map((t) {
                final total = (t['totalCost'] as num?)?.toDouble() ?? 0;
                final fraction = maxTotal > 0 ? total / maxTotal : 0.0;
                final label = (t['monthLabel'] as String?) ?? '';
                return Expanded(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 3),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        Text('C\$${total.toStringAsFixed(0)}', style: const TextStyle(fontSize: 9)),
                        const SizedBox(height: 4),
                        Container(
                          height: 200 * fraction,
                          decoration: BoxDecoration(
                            borderRadius: BorderRadius.circular(ZRadii.sm),
                            gradient: LinearGradient(
                              begin: Alignment.bottomCenter,
                              end: Alignment.topCenter,
                              colors: [ZColors.moduleFleet, ZColors.moduleFleet.withValues(alpha: 0.6)],
                            ),
                          ),
                        ),
                        const SizedBox(height: ZSpacing.sm),
                        Text(label, style: const TextStyle(fontSize: 9), textAlign: TextAlign.center),
                      ],
                    ),
                  ),
                );
              }).toList(),
            ),
          ),
          const SizedBox(height: ZSpacing.lg),
          // ZDataTable
          SizedBox(
            height: 300,
            child: ZDataTable(
              columns: const [
                ZColumn(id: 'period', label: 'Período'),
                ZColumn(id: 'fuel', label: 'Combustible'),
                ZColumn(id: 'maintenance', label: 'Mantenimiento'),
                ZColumn(id: 'expense', label: 'Gastos'),
                ZColumn(id: 'total', label: 'Total'),
              ],
              rows: trends,
              rowMapper: (t) => DataRow(cells: [
                DataCell(Text('${t['monthLabel'] ?? ''}')),
                DataCell(Text('C\$${(t['fuelCost'] as num?)?.toStringAsFixed(2) ?? '0'}')),
                DataCell(Text('C\$${(t['maintenanceCost'] as num?)?.toStringAsFixed(2) ?? '0'}')),
                DataCell(Text('C\$${(t['expenseCost'] as num?)?.toStringAsFixed(2) ?? '0'}')),
                DataCell(Text('C\$${(t['totalCost'] as num?)?.toStringAsFixed(2) ?? '0'}')),
              ]),
              emptyMessage: 'Sin datos',
            ),
          ),
        ],
      ),
    );
  }
}

// ══════════════════════════════════════════════
//  FUEL TREND (ZDataTable + ZStatInline)
// ══════════════════════════════════════════════

class _FuelTrendView extends StatelessWidget {
  final Map<String, dynamic> data;
  const _FuelTrendView({required this.data});

  @override
  Widget build(BuildContext context) {
    final trends = (data['trends'] as List?)?.map((e) => Map<String, dynamic>.from(e as Map)).toList() ?? [];
    if (trends.isEmpty) {
      return const ZEmptyState(icon: Icons.local_gas_station_outlined, title: 'Sin datos de combustible', subtitle: 'Registra abastecimientos para ver la tendencia');
    }

    return SingleChildScrollView(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text('Tendencia de Combustible', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
              const Spacer(),
              ZStatInline(label: 'Total Litros', value: '${(data['grandTotalLiters'] as num?)?.toStringAsFixed(0) ?? '0'} L', icon: Icons.local_gas_station_outlined, color: ZColors.moduleFleet),
              const SizedBox(width: ZSpacing.md),
              ZStatInline(label: 'Total Costo', value: 'C\$${(data['grandTotalCost'] as num?)?.toStringAsFixed(0) ?? '0'}', icon: Icons.attach_money_outlined, color: ZColors.danger),
            ],
          ),
          const SizedBox(height: ZSpacing.lg),
          SizedBox(
            height: 350,
            child: ZDataTable(
              columns: const [
                ZColumn(id: 'period', label: 'Período'),
                ZColumn(id: 'liters', label: 'Litros'),
                ZColumn(id: 'cost', label: 'Costo Total'),
                ZColumn(id: 'price', label: 'Precio/L'),
                ZColumn(id: 'efficiency', label: 'Eficiencia (km/L)'),
              ],
              rows: trends,
              rowMapper: (t) => DataRow(cells: [
                DataCell(Text('${t['monthLabel'] ?? ''}')),
                DataCell(Text((t['totalLiters'] as num?)?.toStringAsFixed(1) ?? '0')),
                DataCell(Text('C\$${(t['totalCost'] as num?)?.toStringAsFixed(2) ?? '0'}')),
                DataCell(Text('C\$${(t['averagePricePerLiter'] as num?)?.toStringAsFixed(2) ?? '0'}')),
                DataCell(Text((t['averageEfficiency'] as num?)?.toStringAsFixed(1) ?? '0')),
              ]),
              emptyMessage: 'Sin datos',
            ),
          ),
        ],
      ),
    );
  }
}

// ══════════════════════════════════════════════
//  GENERIC TABLE REPORT (ZDataTable + ZEmptyState)
// ══════════════════════════════════════════════

class _ExpenseByAccountView extends StatelessWidget {
  final Map<String, dynamic> data;
  const _ExpenseByAccountView({required this.data});

  @override
  Widget build(BuildContext context) {
    final accounts = (data['accounts'] as List?)?.map((e) => Map<String, dynamic>.from(e as Map)).toList() ?? [];
    final grandTotal = (data['grandTotal'] as num?)?.toDouble() ?? 0;
    final totalApproved = data['totalApproved'] ?? 0;
    final totalPending = data['totalPending'] ?? 0;

    if (accounts.isEmpty) {
      return const ZEmptyState(
        icon: Icons.account_balance_outlined,
        title: 'Sin gastos clasificados',
        subtitle: 'Aprobá gastos para ver la distribución por cuenta contable',
      );
    }

    return SingleChildScrollView(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // KPIs
          Row(
            children: [
              Expanded(child: ZStatCard(title: 'Total Gastos', value: 'C\$ ${grandTotal.toStringAsFixed(2)}', icon: Icons.attach_money_outlined, variant: ZStatVariant.module, moduleColor: ZColors.moduleFleet)),
              const SizedBox(width: ZSpacing.md),
              Expanded(child: ZStatCard(title: 'Aprobados', value: '$totalApproved', icon: Icons.check_circle_outlined, variant: ZStatVariant.success)),
              const SizedBox(width: ZSpacing.md),
              Expanded(child: ZStatCard(title: 'Pendientes', value: '$totalPending', icon: Icons.schedule_outlined, variant: ZStatVariant.warning)),
            ],
          ),
          const SizedBox(height: ZSpacing.xl),
          Text('Distribución por Cuenta Contable', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.md),
          ...accounts.map((a) {
            final total = (a['totalAmount'] as num?)?.toDouble() ?? 0;
            final pct = grandTotal > 0 ? total / grandTotal : 0.0;
            final accountCode = a['accountCode'] as String? ?? '';
            final accountName = a['accountName'] as String? ?? '';
            final approved = a['approvedCount'] ?? 0;
            final pending = a['pendingCount'] ?? 0;
            return Padding(
              padding: const EdgeInsets.only(bottom: ZSpacing.md),
              child: ZCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Icon(Icons.account_balance_outlined, size: 18, color: ZColors.moduleFleet),
                        const SizedBox(width: 8),
                        Expanded(child: Text(accountName.isNotEmpty ? '$accountCode — $accountName' : accountCode, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13))),
                        Text('C\$ ${total.toStringAsFixed(2)}', style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
                      ],
                    ),
                    const SizedBox(height: ZSpacing.sm),
                    ZProgress(label: '', value: pct, color: ZColors.moduleFleet, valueLabel: '${(pct * 100).toStringAsFixed(1)}%'),
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        Text('$approved aprobados', style: TextStyle(fontSize: 11, color: ZColors.success)),
                        const SizedBox(width: 12),
                        Text('$pending pendientes', style: TextStyle(fontSize: 11, color: ZColors.warning)),
                      ],
                    ),
                  ],
                ),
              ),
            );
          }),
        ],
      ),
    );
  }
}

class _DataTableReport extends StatelessWidget {
  final Map<String, dynamic> data;
  final FleetReportType reportType;
  const _DataTableReport({required this.data, required this.reportType});

  @override
  Widget build(BuildContext context) {
    final items = _extractItems();
    if (items.isEmpty) {
      return ZEmptyState(
        icon: _iconForReport(),
        title: 'Sin datos',
        subtitle: 'No hay datos para ${reportType.label.toLowerCase()}',
      );
    }
    final columnDefs = _columnDefsForReport();

    return Padding(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(reportType.label, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.md),
          Expanded(
            child: ZDataTable(
              columns: columnDefs.map((c) => ZColumn(id: c.$1, label: c.$2)).toList(),
              rows: items,
              rowMapper: (item) => DataRow(
                cells: columnDefs.map((c) => DataCell(Text(_formatValue(item, c.$1)))).toList(),
              ),
              emptyMessage: 'Sin datos para este reporte',
            ),
          ),
        ],
      ),
    );
  }

  List<Map<String, dynamic>> _extractItems() {
    for (final key in ['vehicles', 'byStatus', 'routes', 'categories', 'trends', 'drivers', 'rows']) {
      final val = data[key];
      if (val is List && val.isNotEmpty) {
        return val.map((e) => Map<String, dynamic>.from(e as Map)).toList();
      }
    }
    return [];
  }

  List<(String, String)> _columnDefsForReport() {
    if (reportType == FleetReportType.vehicleUsage) return [('plate', 'Placa'), ('brandModel', 'Marca/Modelo'), ('status', 'Estado'), ('totalKm', 'Km Totales'), ('tripCount', 'Viajes'), ('averageKmPerTrip', 'Km/Viaje')];
    if (reportType == FleetReportType.delivery) return [('status', 'Estado'), ('count', 'Cantidad'), ('percentage', 'Porcentaje')];
    if (reportType == FleetReportType.route) return [('name', 'Nombre'), ('type', 'Tipo'), ('status', 'Estado'), ('deliveryCount', 'Entregas'), ('estimatedKm', 'Km Est.')];
    if (reportType == FleetReportType.costByVehicle) return [('plate', 'Placa'), ('brandModel', 'Marca/Modelo'), ('totalFuel', 'Combustible'), ('totalExpenses', 'Gastos'), ('grandTotal', 'Total'), ('costPerKm', 'Costo/Km')];
    if (reportType == FleetReportType.profitability) return [('entityName', 'Vehículo'), ('totalCost', 'Costo Total')];
    if (reportType == FleetReportType.driverScorecard) return [('fullName', 'Nombre'), ('licenseNumber', 'Licencia'), ('status', 'Estado'), ('tripCount', 'Viajes'), ('totalKm', 'Km'), ('completedDeliveries', 'Entregas'), ('onTimeRate', 'A Tiempo'), ('totalExpenses', 'Gastos')];
    if (reportType == FleetReportType.vehicleScorecard) return [('plate', 'Placa'), ('brandModel', 'Marca/Modelo'), ('status', 'Estado'), ('currentKm', 'Km Actual'), ('totalFuelLiters', 'Litros'), ('averageFuelEfficiency', 'Eficiencia'), ('totalExpenses', 'Gastos')];
    return [('status', 'Estado'), ('count', 'Cantidad'), ('percentage', 'Porcentaje')];
  }

  IconData _iconForReport() => switch (reportType) {
        FleetReportType.vehicleUsage => Icons.time_to_leave_outlined,
        FleetReportType.delivery => Icons.inventory_2_outlined,
        FleetReportType.route => Icons.alt_route_outlined,
        FleetReportType.costSummary => Icons.pie_chart_outline,
        FleetReportType.costByVehicle => Icons.bar_chart_outlined,
        FleetReportType.costTrend => Icons.show_chart_outlined,
        FleetReportType.profitability => Icons.trending_up_outlined,
        FleetReportType.fleetKpi => Icons.dashboard_outlined,
        FleetReportType.driverScorecard => Icons.person_outline,
        FleetReportType.vehicleScorecard => Icons.time_to_leave_outlined,
        FleetReportType.fuelTrend => Icons.local_gas_station_outlined,
        FleetReportType.expenseByAccount => Icons.account_balance_outlined,
      };

  String _formatValue(Map<String, dynamic> item, String key) {
    final val = item[key];
    if (val == null) return '-';
    if (val is num) {
      final d = val.toDouble();
      if (key == 'totalCost' || key == 'totalAmount' || key == 'totalFuel' || key == 'totalExpenses' || key == 'grandTotal' || key == 'totalFuelCost' || key == 'totalMaintenanceCost' || key == 'costPerKm' || key == 'averagePricePerLiter' || key == 'totalRevenue') {
        return 'C\$ ${d.toStringAsFixed(2)}';
      }
      if (key == 'percentage' || key == 'onTimeRate') return '${d.toStringAsFixed(1)}%';
      return d == d.toInt().toDouble() ? d.toInt().toString() : d.toStringAsFixed(1);
    }
    return val.toString();
  }
}
