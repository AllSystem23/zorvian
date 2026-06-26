import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../../shared/ds/ds.dart';
import 'providers/dashboard_provider.dart';

/// MobileDashboardPage — Optimized dashboard for mobile devices.
/// Uses a card-based layout with swipe gestures and compact metrics.
class MobileDashboardPage extends ConsumerStatefulWidget {
  const MobileDashboardPage({super.key});

  @override
  ConsumerState<MobileDashboardPage> createState() => _MobileDashboardPageState();
}

class _MobileDashboardPageState extends ConsumerState<MobileDashboardPage> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(dashboardProvider.notifier).loadAll();
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final state = ref.watch(dashboardProvider);
    final auth = ref.watch(authProvider);

    if (state.loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline, size: 48, color: ZColors.danger),
            const SizedBox(height: 12),
            Text(state.error!, style: ZTypography.bodyMedium),
            const SizedBox(height: 12),
            FilledButton(
              onPressed: () => ref.read(dashboardProvider.notifier).loadAll(),
              child: const Text('Reintentar'),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () async => ref.read(dashboardProvider.notifier).loadAll(),
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Greeting ──
            _buildGreeting(auth, isDark),
            const SizedBox(height: 20),

            // ── Quick Stats Grid ──
            _buildQuickStats(state, isDark),
            const SizedBox(height: 20),

            // ── Quick Actions ──
            _buildQuickActions(context, isDark),
            const SizedBox(height: 20),

            // ── Recent Requests ──
            _buildRecentRequests(state, isDark),
            const SizedBox(height: 20),

            // ── Calendar Events ──
            _buildCalendarEvents(state, isDark),
          ],
        ),
      ),
    );
  }

  Widget _buildGreeting(dynamic auth, bool isDark) {
    final hour = DateTime.now().hour;
    final greeting = hour < 12 ? 'Buenos días' : hour < 18 ? 'Buenas tardes' : 'Buenas noches';
    final name = auth.displayName ?? auth.email ?? 'Usuario';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          '$greeting,',
          style: ZTypography.bodyMedium.copyWith(
            color: isDark ? ZColors.neutral400 : ZColors.neutral500,
          ),
        ),
        Text(
          name.split(' ').first,
          style: ZTypography.headlineSmall.copyWith(
            fontWeight: FontWeight.w700,
            color: isDark ? ZColors.neutral100 : ZColors.neutral800,
          ),
        ),
      ],
    );
  }

  Widget _buildQuickStats(DashboardState state, bool isDark) {
    final kpis = state.kpis;
    if (kpis == null) return const SizedBox.shrink();

    final metrics = [
      _MetricData('Trabajadores', '${kpis.totalEmployees}', Icons.people_outline, ZColors.brandPrimary),
      _MetricData('Activos', '${kpis.activeEmployees}', Icons.check_circle_outline, ZColors.success),
      _MetricData('Vacaciones', '${kpis.pendingVacationRequests}', Icons.beach_access_outlined, ZColors.warning),
      _MetricData('Permisos', '${kpis.pendingPermissionRequests}', Icons.event_available_outlined, ZColors.info),
    ];

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        mainAxisSpacing: 12,
        crossAxisSpacing: 12,
        childAspectRatio: 1.6,
      ),
      itemCount: metrics.length,
      itemBuilder: (_, i) {
        final m = metrics[i];
        return _MetricCard(
          label: m.label,
          value: m.value,
          icon: m.icon,
          color: m.color,
          isDark: isDark,
        );
      },
    );
  }

  Widget _buildQuickActions(BuildContext context, bool isDark) {
    final actions = [
      _ActionData(Icons.add_shopping_cart_outlined, 'Nueva Venta', '/sales/new', ZColors.success),
      _ActionData(Icons.request_quote_outlined, 'Cotizar', '/quotes/new', ZColors.info),
      _ActionData(Icons.person_add_outlined, 'Trabajador', '/employees/new', ZColors.brandPrimary),
      _ActionData(Icons.inventory_2_outlined, 'Producto', '/products/new', ZColors.warning),
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Acciones Rápidas',
          style: ZTypography.titleSmall.copyWith(
            fontWeight: FontWeight.w600,
            color: isDark ? ZColors.neutral200 : ZColors.neutral700,
          ),
        ),
        const SizedBox(height: 12),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceAround,
          children: actions.map((a) {
            return GestureDetector(
              onTap: () => context.push(a.route),
              child: Column(
                children: [
                  Container(
                    width: 52,
                    height: 52,
                    decoration: BoxDecoration(
                      color: a.color.withValues(alpha: isDark ? 0.2 : 0.1),
                      borderRadius: BorderRadius.circular(ZRadii.lg),
                    ),
                    child: Icon(a.icon, color: a.color, size: 24),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    a.label,
                    style: ZTypography.labelSmall.copyWith(
                      color: isDark ? ZColors.neutral300 : ZColors.neutral600,
                      fontSize: 11,
                    ),
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
            );
          }).toList(),
        ),
      ],
    );
  }

  Widget _buildRecentRequests(DashboardState state, bool isDark) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Solicitudes Recientes',
          style: ZTypography.titleSmall.copyWith(
            fontWeight: FontWeight.w600,
            color: isDark ? ZColors.neutral200 : ZColors.neutral700,
          ),
        ),
        const SizedBox(height: 12),
        if (state.recentRequests.isEmpty)
          _EmptyActivityCard(
            icon: Icons.pending_actions,
            message: 'Sin solicitudes recientes',
            isDark: isDark,
          )
        else
          ...state.recentRequests.take(5).map((req) {
            return _ActivityTile(
              title: req.requestType,
              subtitle: req.employeeName,
              status: req.status,
              isDark: isDark,
            );
          }),
      ],
    );
  }

  Widget _buildCalendarEvents(DashboardState state, bool isDark) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Calendario',
          style: ZTypography.titleSmall.copyWith(
            fontWeight: FontWeight.w600,
            color: isDark ? ZColors.neutral200 : ZColors.neutral700,
          ),
        ),
        const SizedBox(height: 12),
        if (state.calendarEvents.isEmpty)
          _EmptyActivityCard(
            icon: Icons.calendar_today_outlined,
            message: 'Sin eventos próximos',
            isDark: isDark,
          )
        else
          ...state.calendarEvents.take(5).map((evt) {
            return _ActivityTile(
              title: '${evt.type} — ${evt.employeeName}',
              subtitle: '${evt.startDate} al ${evt.endDate}',
              status: evt.status,
              isDark: isDark,
            );
          }),
      ],
    );
  }
}

class _MetricData {
  final String label;
  final String value;
  final IconData icon;
  final Color color;

  const _MetricData(this.label, this.value, this.icon, this.color);
}

class _ActionData {
  final IconData icon;
  final String label;
  final String route;
  final Color color;

  const _ActionData(this.icon, this.label, this.route, this.color);
}

class _MetricCard extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color color;
  final bool isDark;

  const _MetricCard({
    required this.label,
    required this.value,
    required this.icon,
    required this.color,
    required this.isDark,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: isDark ? ZColors.darkSurface : ZColors.surface,
        borderRadius: BorderRadius.circular(ZRadii.lg),
        border: Border.all(
          color: isDark ? ZColors.darkBorder : ZColors.border,
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Icon(icon, size: 20, color: color),
          const SizedBox(height: 4),
          Text(
            value,
            style: ZTypography.titleLarge.copyWith(
              fontWeight: FontWeight.w700,
              color: isDark ? ZColors.neutral100 : ZColors.neutral800,
            ),
          ),
          Text(
            label,
            style: ZTypography.labelSmall.copyWith(
              color: isDark ? ZColors.neutral400 : ZColors.neutral500,
            ),
          ),
        ],
      ),
    );
  }
}

class _ActivityTile extends StatelessWidget {
  final String title;
  final String subtitle;
  final String status;
  final bool isDark;

  const _ActivityTile({
    required this.title,
    required this.subtitle,
    required this.status,
    required this.isDark,
  });

  @override
  Widget build(BuildContext context) {
    final statusColor = switch (status.toLowerCase()) {
      'approved' || 'completed' => ZColors.success,
      'rejected' || 'cancelled' => ZColors.danger,
      _ => ZColors.warning,
    };

    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: isDark ? ZColors.darkSurface : ZColors.surface,
        borderRadius: BorderRadius.circular(ZRadii.md),
        border: Border.all(
          color: isDark ? ZColors.darkBorder : ZColors.border,
        ),
      ),
      child: Row(
        children: [
          Container(
            width: 8,
            height: 8,
            decoration: BoxDecoration(
              color: statusColor,
              shape: BoxShape.circle,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: ZTypography.bodySmall.copyWith(
                    fontWeight: FontWeight.w600,
                    color: isDark ? ZColors.neutral200 : ZColors.neutral700,
                  ),
                ),
                Text(
                  subtitle,
                  style: ZTypography.labelSmall.copyWith(
                    color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _EmptyActivityCard extends StatelessWidget {
  final IconData icon;
  final String message;
  final bool isDark;

  const _EmptyActivityCard({
    required this.icon,
    required this.message,
    required this.isDark,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        color: isDark ? ZColors.darkSurface : ZColors.surface,
        borderRadius: BorderRadius.circular(ZRadii.lg),
        border: Border.all(
          color: isDark ? ZColors.darkBorder : ZColors.border,
        ),
      ),
      child: Column(
        children: [
          Icon(
            icon,
            size: 32,
            color: isDark ? ZColors.neutral500 : ZColors.neutral400,
          ),
          const SizedBox(height: 8),
          Text(
            message,
            style: ZTypography.bodySmall.copyWith(
              color: isDark ? ZColors.neutral400 : ZColors.neutral500,
            ),
          ),
        ],
      ),
    );
  }
}