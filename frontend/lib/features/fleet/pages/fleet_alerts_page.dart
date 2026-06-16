import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_alerts_provider.dart';

/// Fleet Alerts page — alert dashboard with severity badges,
/// driver blocking, notification dispatch, using full DS.
final class FleetAlertsPage extends ConsumerStatefulWidget {
  const FleetAlertsPage({super.key});

  @override
  ConsumerState<FleetAlertsPage> createState() => _FleetAlertsPageState();
}

class _FleetAlertsPageState extends ConsumerState<FleetAlertsPage> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(fleetAlertProvider.notifier).loadSummary();
      ref.read(fleetAlertProvider.notifier).loadActiveAlerts();
    });
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetAlertProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Alertas de Flota'),
        actions: [
          ZButton(
            text: 'Enviar Notificaciones',
            icon: Icons.notifications_active_outlined,
            onPressed: state.dispatching ? () {} : () { ref.read(fleetAlertProvider.notifier).dispatchNotifications(); },
            type: ZButtonType.secondary,
            fullWidth: false,
            isLoading: state.dispatching,
          ),
          const SizedBox(width: ZSpacing.sm),
        ],
      ),
      body: state.loading && state.summary == null
          ? _buildSkeleton()
          : state.error != null
              ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
              : _buildContent(state, theme),
    );
  }

  Widget _buildSkeleton() {
    return Padding(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        children: [
          ...List.generate(4, (_) => Padding(
            padding: const EdgeInsets.only(bottom: ZSpacing.md),
            child: ZSkeleton.statCard(),
          )),
        ],
      ),
    );
  }

  Widget _buildContent(FleetAlertState state, ThemeData theme) {
    final summary = state.summary ?? {};
    final activeAlerts = state.activeAlerts;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── KPI Cards ──
          Text('Resumen de Alertas', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.md),
          LayoutBuilder(
            builder: (context, constraints) {
              final crossCount = constraints.maxWidth > 900 ? 5
                  : constraints.maxWidth > 600 ? 4
                  : constraints.maxWidth > 400 ? 3 : 2;
              return GridView.count(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                crossAxisCount: crossCount,
                mainAxisSpacing: ZSpacing.md,
                crossAxisSpacing: ZSpacing.md,
                childAspectRatio: 1.4,
                children: [
                  ZStatCard(
                    title: 'Total Alertas',
                    value: '${summary['activeAlerts'] ?? 0}',
                    icon: Icons.warning_amber_outlined,
                    variant: ZStatVariant.module,
                    moduleColor: ZColors.moduleFleet,
                  ),
                  ZStatCard(
                    title: 'Críticas',
                    value: '${summary['criticalAlerts'] ?? 0}',
                    icon: Icons.error_outline,
                    variant: ZStatVariant.danger,
                  ),
                  ZStatCard(
                    title: 'Advertencias',
                    value: '${summary['warningAlerts'] ?? 0}',
                    icon: Icons.info_outline,
                    variant: ZStatVariant.warning,
                  ),
                  ZStatCard(
                    title: 'Informativas',
                    value: '${summary['infoAlerts'] ?? 0}',
                    icon: Icons.help_outline,
                    variant: ZStatVariant.info,
                  ),
                  ZStatCard(
                    title: 'Sin Aceptar',
                    value: '${summary['unacknowledgedAlerts'] ?? 0}',
                    icon: Icons.mail_outline,
                    variant: ZStatVariant.module,
                    moduleColor: ZColors.danger,
                  ),
                ],
              );
            },
          ),
          const SizedBox(height: ZSpacing.xl),

          // ── Active Alerts List ──
          Text('Alertas Activas', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.md),

          if (activeAlerts.isEmpty)
            const ZEmptyState(
              icon: Icons.check_circle_outline,
              title: 'Sin alertas activas',
              subtitle: 'Toda la flota está operando normalmente',
            )
          else
            ...activeAlerts.map((alert) => _buildAlertCard(alert, theme)),
        ],
      ),
    );
  }

  ZBadgeType _badgeTypeForSeverity(String severity) => switch (severity) {
        'critical' => ZBadgeType.danger,
        'warning' => ZBadgeType.warning,
        _ => ZBadgeType.info,
      };

  Widget _buildAlertCard(Map<String, dynamic> alert, ThemeData theme) {
    final severity = alert['severity'] ?? 'info';
    final category = alert['category'] ?? '';
    final title = alert['title'] ?? '';
    final message = alert['message'] ?? '';
    final createdAt = alert['createdAt'] != null
        ? DateTime.tryParse(alert['createdAt'].toString())
        : null;

    final Color severityColor;
    final IconData severityIcon;
    switch (severity) {
      case 'critical':
        severityColor = ZColors.danger;
        severityIcon = Icons.error_outline;
      case 'warning':
        severityColor = ZColors.warning;
        severityIcon = Icons.warning_amber_outlined;
      default:
        severityColor = ZColors.info;
        severityIcon = Icons.info_outline;
    }

    return ZCard(
      margin: const EdgeInsets.only(bottom: ZSpacing.md),
      child: Row(
        children: [
          Container(
            width: 6,
            height: 60,
            decoration: BoxDecoration(
              color: severityColor,
              borderRadius: BorderRadius.circular(3),
            ),
          ),
          const SizedBox(width: ZSpacing.md),
          Icon(severityIcon, color: severityColor, size: 24),
          const SizedBox(width: ZSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Text(title, style: TextStyle(fontWeight: FontWeight.w600, color: theme.colorScheme.onSurface)),
                    const SizedBox(width: ZSpacing.sm),
                    ZBadge(text: category, type: _badgeTypeForSeverity(severity)),
                    ZBadge(text: severity.toUpperCase(), type: _badgeTypeForSeverity(severity)),
                  ],
                ),
                const SizedBox(height: ZSpacing.xs),
                Text(message, style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurface.withValues(alpha: 0.7),
                )),
                if (createdAt != null) ...[
                  const SizedBox(height: ZSpacing.xs),
                  Text(
                    '${createdAt.day}/${createdAt.month}/${createdAt.year} ${createdAt.hour}:${createdAt.minute.toString().padLeft(2, '0')}',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurface.withValues(alpha: 0.4),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}
