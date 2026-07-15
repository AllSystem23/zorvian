import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_dashboard_provider.dart';

final class FleetDashboardPage extends ConsumerStatefulWidget {
  const FleetDashboardPage({super.key});

  @override
  ConsumerState<FleetDashboardPage> createState() => _FleetDashboardPageState();
}

final class _FleetDashboardPageState extends ConsumerState<FleetDashboardPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(fleetDashboardProvider.notifier).load());
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetDashboardProvider);
    final d = state.data;

    return Scaffold(
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
              : Column(
                  children: [
                    Padding(
                      padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
                      child: Row(
                        children: [
                          if (d.alerts.isNotEmpty)
                            Badge(
                              isLabelVisible: true,
                              label: Text('${d.alerts.length}'),
                              child: IconButton(
                                icon: const Icon(Icons.notifications_outlined),
                                onPressed: () => _showAlerts(context, d.alerts),
                              ),
                            ),
                          IconButton(
                            icon: const Icon(Icons.refresh_outlined),
                            onPressed: () => ref.read(fleetDashboardProvider.notifier).load(),
                          ),
                        ],
                      ),
                    ),
                    Expanded(
                      child: RefreshIndicator(
                        onRefresh: () => ref.read(fleetDashboardProvider.notifier).load(),
                        child: SingleChildScrollView(
                          physics: const AlwaysScrollableScrollPhysics(),
                          padding: const EdgeInsets.all(16),
                          child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        _buildKpiRow([
                          ('Vehículos', '${d.totalVehicles}', Icons.time_to_leave_outlined, ZStatVariant.primary),
                          ('Activos', '${d.activeVehicles}', Icons.check_circle_outlined, ZStatVariant.primary),
                          ('Mantenimiento', '${d.inMaintenance}', Icons.build_outlined, ZStatVariant.primary),
                          ('Viajes Hoy', '${d.tripsToday}', Icons.flight_takeoff_outlined, ZStatVariant.primary),
                        ]),
                        const SizedBox(height: 12),
                        _buildKpiRow([
                          ('Conductores Disp.', '${d.availableDrivers}', Icons.person_outline, ZStatVariant.module),
                          ('Rutas Activas', '${d.activeRoutes}', Icons.route_outlined, ZStatVariant.module),
                          ('Entregas Pend.', '${d.pendingDeliveries}', Icons.inventory_2_outlined, ZStatVariant.module),
                          ('Docs. por Vencer', '${d.expiringDocuments}', Icons.description_outlined, ZStatVariant.module),
                        ]),
                        if (d.alerts.isNotEmpty) ...[
                          const SizedBox(height: 24),
                          _buildAlertsSection(d.alerts),
                        ],
                        const SizedBox(height: 24),
                        _buildStatusCards(context, d),
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ),
    );
  }

  Widget _buildKpiRow(List<(String, String, IconData, ZStatVariant)> items) {
    return Row(
      children: items.map((item) {
        final (title, value, icon, variant) = item;
        return Expanded(
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 4),
            child: ZStatCard(title: title, value: value, icon: icon, variant: variant),
          ),
        );
      }).toList(),
    );
  }

  Widget _buildAlertsSection(List<AlertItem> alerts) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.notifications_active_outlined, size: 20, color: ZColors.warning),
              const SizedBox(width: 8),
              Text('Alertas', style: Theme.of(context).textTheme.titleMedium),
            ],
          ),
          const SizedBox(height: 12),
          for (final alert in alerts) ...[
            Row(
              children: [
                Icon(
                  alert.severity == 'warning' ? Icons.warning_amber_rounded : Icons.info_outline,
                  size: 18,
                  color: alert.severity == 'warning' ? ZColors.warning : ZColors.info,
                ),
                const SizedBox(width: 8),
                Expanded(child: Text(alert.message, style: const TextStyle(fontSize: 13))),
              ],
            ),
            if (alert != alerts.last) const SizedBox(height: 8),
          ],
        ],
      ),
    );
  }

  Widget _buildStatusCards(BuildContext context, FleetDashboardData d) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Resumen de Flota', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 16),
          _buildStatusRow('Vehículos Activos', d.activeVehicles, d.totalVehicles, ZColors.success),
          const SizedBox(height: 8),
          _buildStatusRow('En Mantenimiento', d.inMaintenance, d.totalVehicles, ZColors.warning),
          const SizedBox(height: 8),
          _buildStatusRow('Disponibles', d.totalVehicles - d.activeVehicles - d.inMaintenance, d.totalVehicles, ZColors.moduleFleet),
        ],
      ),
    );
  }

  Widget _buildStatusRow(String label, int count, int total, Color color) {
    final pct = total > 0 ? count / total : 0.0;
    return Row(
      children: [
        SizedBox(width: 140, child: Text(label, style: const TextStyle(fontSize: 13))),
        Expanded(
          child: ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: LinearProgressIndicator(
              value: pct,
              backgroundColor: color.withValues(alpha: 0.15),
              color: color,
              minHeight: 8,
            ),
          ),
        ),
        const SizedBox(width: 8),
        SizedBox(width: 40, child: Text('$count', style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600), textAlign: TextAlign.right)),
      ],
    );
  }

  void _showAlerts(BuildContext context, List<AlertItem> alerts) {
    ZModal.show(context,
      title: 'Alertas de Flota',
      confirmText: 'Cerrar',
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: alerts.map((a) => Padding(
          padding: const EdgeInsets.symmetric(vertical: 4),
          child: Row(
            children: [
              Icon(
                a.severity == 'warning' ? Icons.warning_amber_rounded : Icons.info_outline,
                size: 18,
                color: a.severity == 'warning' ? ZColors.warning : ZColors.info,
              ),
              const SizedBox(width: 8),
              Expanded(child: Text(a.message, style: const TextStyle(fontSize: 13))),
            ],
          ),
        )).toList(),
      ),
    );
  }
}
