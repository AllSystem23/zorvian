import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_tracking_provider.dart';

/// Fleet Delivery Tracking page — timeline, ETA, status updates,
/// and client-facing tracking using full DS.
final class FleetTrackingPage extends ConsumerStatefulWidget {
  const FleetTrackingPage({super.key});

  @override
  ConsumerState<FleetTrackingPage> createState() => _FleetTrackingPageState();
}

class _FleetTrackingPageState extends ConsumerState<FleetTrackingPage> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(fleetTrackingProvider.notifier).loadRecentDeliveries();
    });
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(fleetTrackingProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Tracking de Entregas'),
      ),
      body: state.loading && state.recentDeliveries.isEmpty
          ? _buildSkeleton()
          : state.error != null && state.deliveryDetail == null
              ? Center(child: ZAlertCard(message: state.error!, severity: 'high'))
              : _buildContent(state, theme),
    );
  }

  Widget _buildSkeleton() {
    return Padding(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        children: List.generate(5, (_) => Padding(
          padding: const EdgeInsets.only(bottom: ZSpacing.md),
          child: ZSkeleton.listTile(),
        )),
      ),
    );
  }

  Widget _buildContent(FleetTrackingState state, ThemeData theme) {
    // If a delivery detail is loaded, show timeline view
    if (state.deliveryDetail != null) {
      return _buildTimelineView(state, theme);
    }

    // Otherwise show list of deliveries
    final deliveries = state.recentDeliveries;

    if (deliveries.isEmpty) {
      return const ZEmptyState(
        icon: Icons.local_shipping_outlined,
        title: 'Sin entregas',
        subtitle: 'No hay entregas recientes para mostrar',
      );
    }

    return _buildDeliveryList(deliveries, theme);
  }

  Widget _buildDeliveryList(List<Map<String, dynamic>> deliveries, ThemeData theme) {
    return Padding(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Entregas Recientes', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.md),
          Expanded(
            child: ListView.separated(
              itemCount: deliveries.length,
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (context, index) {
                final d = deliveries[index];
                final code = d['code'] ?? 'N/A';
                final status = d['status'] ?? 'Pending';
                final address = d['deliveryAddress'] ?? '';
                final clientName = d['clientName'] ?? 'N/A';
                final vehiclePlate = d['vehiclePlate'] ?? '';


                return ZCard(
                  margin: const EdgeInsets.only(bottom: ZSpacing.sm),
                  child: ListTile(
                    contentPadding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.xs),
                    leading: Container(
                      width: 6,
                      height: 48,
                      decoration: BoxDecoration(
                        color: _statusColor(status),
                        borderRadius: BorderRadius.circular(3),
                      ),
                    ),
                    title: Row(
                      children: [
                        Text(code, style: const TextStyle(fontWeight: FontWeight.w600)),
                        const SizedBox(width: ZSpacing.sm),
                        ZBadge(text: status, type: _statusBadgeType(status)),
                      ],
                    ),
                    subtitle: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const SizedBox(height: ZSpacing.xs),
                        Text(clientName, style: theme.textTheme.bodySmall),
                        Text(address, style: theme.textTheme.bodySmall?.copyWith(
                          color: theme.colorScheme.onSurface.withValues(alpha: 0.5),
                        )),
                      ],
                    ),
                    trailing: vehiclePlate.isNotEmpty
                        ? ZBadge(text: vehiclePlate, type: ZBadgeType.neutral)
                        : null,
                    onTap: () {
                      final did = d['id']?.toString();
                      if (did != null) ref.read(fleetTrackingProvider.notifier).loadTrackingTimeline(did);
                    },
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTimelineView(FleetTrackingState state, ThemeData theme) {
    final detail = state.deliveryDetail!;
    final timeline = state.timeline;
    final code = detail['code'] ?? 'N/A';
    final status = detail['status'] ?? 'Pending';
    final address = detail['deliveryAddress'] ?? '';
    final clientName = detail['clientName'] ?? 'N/A';
    final deliveredAt = detail['deliveredAt'] != null ? DateTime.tryParse(detail['deliveredAt'].toString()) : null;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Header ──
          Row(
            children: [
              IconButton(
                icon: const Icon(Icons.arrow_back),
                onPressed: () {
                  ref.read(fleetTrackingProvider.notifier).clearDetail();
                  ref.read(fleetTrackingProvider.notifier).loadRecentDeliveries();
                },
              ),
              const SizedBox(width: ZSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Text(code, style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700)),
                        const SizedBox(width: ZSpacing.sm),
                        ZBadge(text: status, type: _statusBadgeType(status)),
                      ],
                    ),
                    Text(clientName, style: theme.textTheme.bodyMedium?.copyWith(
                      color: theme.colorScheme.onSurface.withValues(alpha: 0.6),
                    )),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: ZSpacing.lg),

          // ── Info Cards ──
          LayoutBuilder(
            builder: (context, constraints) {
              final crossCount = constraints.maxWidth > 600 ? 3 : 2;
              return GridView.count(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                crossAxisCount: crossCount,
                mainAxisSpacing: ZSpacing.md,
                crossAxisSpacing: ZSpacing.md,
                childAspectRatio: 2.0,
                children: [
                  ZStatCard(
                    title: 'Dirección',
                    value: address,
                    icon: Icons.location_on_outlined,
                    variant: ZStatVariant.module,
                    moduleColor: ZColors.moduleFleet,
                  ),
                  ZStatCard(
                    title: 'Fecha Programada',
                    value: detail['scheduledDate']?.toString() ?? 'N/A',
                    icon: Icons.calendar_today_outlined,
                    variant: ZStatVariant.info,
                  ),
                  if (deliveredAt != null)
                    ZStatCard(
                      title: 'Entregado',
                      value: '${deliveredAt.day}/${deliveredAt.month}/${deliveredAt.year} ${deliveredAt.hour}:${deliveredAt.minute.toString().padLeft(2, '0')}',
                      icon: Icons.check_circle_outline,
                      variant: ZStatVariant.success,
                    ),
                ],
              );
            },
          ),
          const SizedBox(height: ZSpacing.xl),

          // ── Timeline ──
          Text('Historial de Eventos', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.md),

          if (timeline.isEmpty)
            const ZEmptyState(
              icon: Icons.history_outlined,
              title: 'Sin eventos',
              subtitle: 'No hay eventos registrados para esta entrega',
            )
          else
            ...timeline.asMap().entries.map((entry) {
              final event = entry.value;
              final isLast = entry.key == timeline.length - 1;
              final eventStatus = event['status'] ?? '';
              final timestamp = event['timestamp'] != null
                  ? DateTime.tryParse(event['timestamp'].toString())
                  : null;
              final notes = event['notes'] as String?;

              return _buildTimelineEvent(eventStatus, timestamp, notes, isLast, theme);
            }),

          const SizedBox(height: ZSpacing.xl),

          // ── Actions ──
          if (status != 'Delivered' && status != 'Cancelled')
            Row(
              children: [
                ZButton(
                  text: 'Enviar ETA',
                  icon: Icons.notifications_outlined,
                  onPressed: () => ref.read(fleetTrackingProvider.notifier).sendEtaNotification(detail['id']?.toString() ?? ''),
                  type: ZButtonType.secondary,
                  fullWidth: false,
                ),
                const SizedBox(width: ZSpacing.md),
                ZButton(
                  text: 'Confirmar Entrega',
                  icon: Icons.check_circle_outline,
                  onPressed: () => _showConfirmDialog(context, detail['id']?.toString() ?? ''),
                  fullWidth: false,
                ),
              ],
            ),
        ],
      ),
    );
  }

  Widget _buildTimelineEvent(String status, DateTime? timestamp, String? notes, bool isLast, ThemeData theme) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Column(
          children: [
            Container(
              width: 12,
              height: 12,
              decoration: BoxDecoration(
                color: _statusColor(status),
                shape: BoxShape.circle,
              ),
            ),
            if (!isLast)
              Container(width: 2, height: 40, color: ZColors.neutral200),
          ],
        ),
        const SizedBox(width: ZSpacing.md),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.only(bottom: ZSpacing.md),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    ZBadge(text: status, type: _statusBadgeType(status)),
                    if (timestamp != null) ...[
                      const SizedBox(width: ZSpacing.sm),
                      Text(
                        '${timestamp.day}/${timestamp.month}/${timestamp.year} ${timestamp.hour}:${timestamp.minute.toString().padLeft(2, '0')}',
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: theme.colorScheme.onSurface.withValues(alpha: 0.5),
                        ),
                      ),
                    ],
                  ],
                ),
                if (notes != null && notes.isNotEmpty) ...[
                  const SizedBox(height: ZSpacing.xs),
                  Text(notes, style: theme.textTheme.bodySmall),
                ],
              ],
            ),
          ),
        ),
      ],
    );
  }

  void _showConfirmDialog(BuildContext context, String deliveryId) {
    final nameController = TextEditingController();
    final idController = TextEditingController();

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Confirmar Entrega'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ZTextField(
              controller: nameController,
              label: 'Nombre del receptor',
              prefix: const Icon(Icons.person_outline),
            ),
            const SizedBox(height: ZSpacing.md),
            ZTextField(
              controller: idController,
              label: 'Identificación',
              prefix: const Icon(Icons.badge_outlined),
            )
          ],
        ),
        actions: [
          ZButton(
            text: 'Cancelar',
            onPressed: () => Navigator.pop(ctx),
            type: ZButtonType.secondary,
            fullWidth: false,
          ),
          ZButton(
            text: 'Confirmar',
            onPressed: () {
              Navigator.pop(ctx);
              ref.read(fleetTrackingProvider.notifier).confirmDelivery(
                deliveryId,
                nameController.text,
                idController.text,
              );
            },
            fullWidth: false,
          ),
        ],
      ),
    );
  }

  Color _statusColor(String status) => switch (status) {
        'Pending' => ZColors.neutral400,
        'InRoute' => ZColors.info,
        'Delivered' => ZColors.success,
        'Partial' => ZColors.warning,
        'Returned' => ZColors.danger,
        'Cancelled' => ZColors.neutral300,
        _ => ZColors.neutral400,
      };

  ZBadgeType _statusBadgeType(String status) => switch (status) {
        'Pending' => ZBadgeType.neutral,
        'InRoute' => ZBadgeType.info,
        'Delivered' => ZBadgeType.success,
        'Partial' => ZBadgeType.warning,
        'Returned' => ZBadgeType.danger,
        'Cancelled' => ZBadgeType.neutral,
        _ => ZBadgeType.neutral,
      };
}
