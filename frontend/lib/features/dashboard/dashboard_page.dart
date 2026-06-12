import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../../core/services/signalr_service.dart';
import '../../core/theme/theme_provider.dart';
import '../../core/widgets/command_palette.dart';
import '../../core/widgets/responsive_layout.dart';
import '../../shared/ds/ds.dart';
import 'providers/dashboard_provider.dart';

class DashboardPage extends ConsumerStatefulWidget {
  const DashboardPage({super.key});

  @override
  ConsumerState<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends ConsumerState<DashboardPage> {
  final _mainContentFocus = FocusNode();

  @override
  void dispose() {
    _mainContentFocus.dispose();
    super.dispose();
  }

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(dashboardProvider.notifier).loadAll();
      _connectSignalR();
    });
  }

  Future<void> _connectSignalR() async {
    final auth = ref.read(authProvider);
    if (auth.status != AuthStatus.authenticated) return;
    const apiUrl = String.fromEnvironment('API_URL', defaultValue: 'https://nexora-9yal.onrender.com/zorvian/v1');
    final uri = Uri.parse(apiUrl);
    final rootUrl = '${uri.scheme}://${uri.host}${uri.hasPort ? ':${uri.port}' : ''}';
    final storage = ref.read(secureStorageProvider);
    final token = await storage.getAccessToken();
    if (token != null) {
      ref.read(signalRProvider.notifier).connect(rootUrl, token);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final auth = ref.watch(authProvider);
    final dash = ref.watch(dashboardProvider);
    final user = auth.displayName ?? 'Usuario';

    return CallbackShortcuts(
      bindings: {
        SingleActivator(LogicalKeyboardKey.keyK, control: true, meta: true): () => CommandPalette.show(context),
        SingleActivator(LogicalKeyboardKey.keyK, control: true): () => CommandPalette.show(context),
        SingleActivator(LogicalKeyboardKey.slash, control: true): () => CommandPalette.show(context),
      },
      child: Focus(
        autofocus: true,
        child: Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            Image.asset('assets/Zorvian.png', height: 28, excludeFromSemantics: true),
            const SizedBox(width: 8),
            Text('Hola, $user'),
          ],
        ),
        actions: [
          FocusTraversalGroup(
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                IconButton(
                  icon: const Icon(Icons.search),
                  tooltip: 'Buscar (Ctrl+K)',
                  onPressed: () => CommandPalette.show(context),
                ),
                Consumer(
                  builder: (_, ref, _) {
                    final notifState = ref.watch(signalRProvider);
                    final unread = notifState.notifications.length;
                    return Stack(
                      children: [
                        IconButton(
                          icon: const Icon(Icons.notifications_outlined),
                          tooltip: 'Notificaciones',
                          onPressed: unread > 0
                              ? () => _showNotifications(context, ref)
                              : null,
                        ),
                        if (unread > 0)
                          Positioned(
                            right: 8,
                            top: 8,
                            child: ZLiveRegion(
                              label: '$unread notificaciones sin leer',
                              child: Container(
                                padding: const EdgeInsets.all(4),
                                decoration: const BoxDecoration(
                                  color: Colors.red,
                                  shape: BoxShape.circle,
                                ),
                                child: Text(
                                  '$unread',
                                  style: const TextStyle(color: Colors.white, fontSize: 10),
                                ),
                              ),
                            ),
                          ),
                      ],
                    );
                  },
                ),
                Consumer(
                  builder: (context, ref, child) {
                    final mode = ref.watch(themeModeProvider);
                    return IconButton(
                      icon: Icon(mode == ThemeMode.dark ? Icons.light_mode : Icons.dark_mode),
                      tooltip: mode == ThemeMode.dark ? 'Modo claro' : 'Modo oscuro',
                      onPressed: () => ref.read(themeModeProvider.notifier).toggle(),
                    );
                  },
                ),
                IconButton(
                  icon: const Icon(Icons.person_outline),
                  tooltip: 'Perfil',
                  onPressed: () => context.push('/profile'),
                ),
                IconButton(
                  icon: const Icon(Icons.logout),
                  tooltip: 'Cerrar sesión',
                  onPressed: () => ref.read(authProvider.notifier).logout(),
                ),
              ],
            ),
          ),
        ],
      ),
      body: Column(
        children: [
          ZSkipLink(targetFocus: _mainContentFocus),
          Expanded(
            child: dash.loading
                ? const Center(child: CircularProgressIndicator())
                : dash.error != null
                    ? Center(child: Text(dash.error!))
                    : ZLiveRegion(
                  label: dash.error != null ? 'Error al cargar dashboard' : 'Dashboard cargado correctamente',
                  child: RefreshIndicator(
                        onRefresh: () => ref.read(dashboardProvider.notifier).refresh(),
                        child: ZMainContent(
                          focusNode: _mainContentFocus,
                          child: ListView(
                            padding: const EdgeInsets.all(16),
                            children: [
                              if (dash.kpis != null) _buildKpiRow(dash.kpis!),
                              const SizedBox(height: 24),
                              _buildNavigationCards(context),
                              const SizedBox(height: 24),
                              Text('Solicitudes Recientes',
                                style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                              const SizedBox(height: 8),
                              if (dash.recentRequests.isNotEmpty)
                                ...dash.recentRequests.take(5).map(_buildRecentRequestTile)
                              else
                                ZCard(
                                  padding: const EdgeInsets.all(24),
                                  child: Center(child: Text('No hay solicitudes recientes',
                                    style: const TextStyle(color: ZColors.neutral500))),
                                ),
                            ],
                          ),
                        ),
                      ),
          ),
          ),
        ],
      ),
      ),
    ),
  );
  }

  void _showNotifications(BuildContext context, WidgetRef ref) {
    final list = ref.read(signalRProvider).notifications;
    ZModal.show(context,
      title: 'Notificaciones',
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox(
            width: double.maxFinite,
            child: list.isEmpty
                ? const Padding(
                    padding: EdgeInsets.symmetric(vertical: 24),
                    child: Center(child: Text('Sin notificaciones')),
                  )
                : ListView.separated(
                    shrinkWrap: true,
                    itemCount: list.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (_, i) {
                      final n = list[i];
                      return ListTile(
                        dense: true,
                        leading: Icon(
                          n.type == 'approval' ? Icons.approval : Icons.notifications,
                          color: n.type == 'approval' ? Colors.orange : Colors.blue,
                        ),
                        title: Text(n.title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
                        subtitle: Text(n.message, style: const TextStyle(fontSize: 12)),
                      );
                    },
                  ),
          ),
          const SizedBox(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              TextButton(
                onPressed: () {
                  ref.read(signalRProvider.notifier).clearNotifications();
                  Navigator.pop(context);
                },
                child: const Text('Limpiar'),
              ),
              const SizedBox(width: 8),
              ZButton(
                onPressed: () => Navigator.pop(context),
                text: 'Cerrar',
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildKpiRow(DashboardKpis kpis) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Resumen', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 12),
        FocusTraversalGroup(
          child: Row(
              children: [
                Expanded(child: _KpiCard(
                  icon: Icons.people,
                  label: 'Activos',
                  value: '${kpis.activeEmployees}',
                  color: ZColors.brandPrimary,
                )),
                const SizedBox(width: 8),
                Expanded(child: _KpiCard(
                  icon: Icons.pending_actions,
                  label: 'Pendientes',
                  value: '${kpis.pendingVacationRequests + kpis.pendingPermissionRequests}',
                  color: ZColors.warning,
                )),
              ],
            ),
          ),
          const SizedBox(height: 8),
          FocusTraversalGroup(
            child: Row(
              children: [
                Expanded(child: _KpiCard(
                  icon: Icons.cake,
                  label: 'Cumpleaños',
                  value: '${kpis.birthdaysThisMonth}',
                  color: ZColors.info,
                )),
                const SizedBox(width: 8),
                Expanded(child: _KpiCard(
                  icon: Icons.work_history,
                  label: 'Aniversarios',
                  value: '${kpis.workAnniversariesThisMonth}',
                  color: ZColors.success,
                )),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildNavigationCards(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Módulos', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 12),
        FocusTraversalGroup(
          child: ResponsiveGrid(
            spacing: 12,
            children: [
              _NavCard(icon: Icons.people, label: 'Empleados', color: ZColors.brandPrimary, route: '/employees'),
              _NavCard(icon: Icons.calendar_month, label: 'Calendario', color: ZColors.info, route: '/absence-calendar'),
              _NavCard(icon: Icons.business, label: 'Dptos.', color: const Color(0xFF7C3AED), route: '/departments'),
              _NavCard(icon: Icons.beach_access, label: 'Vacaciones', color: ZColors.warning, route: '/vacations'),
              _NavCard(icon: Icons.description, label: 'Permisos', color: ZColors.danger, route: '/permissions'),
              _NavCard(icon: Icons.schedule, label: 'Asistencia', color: ZColors.success, route: '/attendance'),
              _NavCard(icon: Icons.person, label: 'Perfil', color: const Color(0xFF9333EA), route: '/profile'),
              _NavCard(icon: Icons.assessment, label: 'Reportes', color: ZColors.brandTeal, route: '/reports'),
              _NavCard(icon: Icons.admin_panel_settings, label: 'Admin', color: ZColors.neutral800, route: '/admin/users'),
              _NavCard(icon: Icons.settings, label: 'Config.', color: ZColors.neutral600, route: '/settings'),
              _NavCard(icon: Icons.receipt_long, label: 'Nómina', color: ZColors.success, route: '/payroll'),
              _NavCard(icon: Icons.tablet, label: 'Kiosko', color: ZColors.info, route: '/attendance/kiosk'),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildRecentRequestTile(RecentRequestItem item) {
    final icon = item.requestType == 'vacacion' ? Icons.beach_access : Icons.description;
    final color = item.requestType == 'vacacion' ? ZColors.warning : ZColors.danger;
    return ZCard(
      child: ListTile(
        leading: Icon(icon, color: color),
        title: Text(item.employeeName),
        subtitle: Text(item.description ?? item.requestType),
        trailing: _statusChip(item.status),
        onTap: () {
          if (item.requestType == 'vacacion') {
            context.push('/vacations/${item.id}');
          } else {
            context.push('/permissions/${item.id}');
          }
        },
      ),
    );
  }

  Widget _statusChip(String status) {
    final (label, color) = switch (status) {
      'approved' => ('Aprobado', ZColors.success),
      'rejected' => ('Rechazado', ZColors.danger),
      'pending' => ('Pendiente', ZColors.warning),
      _ => (status, ZColors.neutral500),
    };
    return Chip(label: Text(label, style: TextStyle(fontSize: 11, color: color)), materialTapTargetSize: MaterialTapTargetSize.shrinkWrap);
  }
}

class _KpiCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color color;

  const _KpiCard({required this.icon, required this.label, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return ZCard(
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: color, size: 24),
          const SizedBox(height: 8),
          Text(value, style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: color)),
            Text(label, style: const TextStyle(fontSize: 12, color: ZColors.neutral500)),
        ],
      ),
    );
  }
}

class _NavCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final Color color;
  final String? route;

  const _NavCard({required this.icon, required this.label, required this.color, this.route});

  @override
  Widget build(BuildContext context) {
    return ZCard(
      child: Semantics(
        label: 'Ir a $label',
        button: true,
        child: InkWell(
          onTap: route != null ? () => context.push(route!) : null,
          borderRadius: BorderRadius.circular(12),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 28, color: color),
            const SizedBox(height: 8),
            Text(label, style: const TextStyle(fontWeight: FontWeight.w600, color: ZColors.neutral700, fontSize: 12)),
          ],
        ),
      ),
    ),
    );
  }
}
