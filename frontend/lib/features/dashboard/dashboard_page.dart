import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../../core/services/signalr_service.dart';
import '../../core/theme/theme_provider.dart';
import '../../core/widgets/command_palette.dart';
import '../../core/widgets/responsive_layout.dart';
import 'providers/dashboard_provider.dart';

class DashboardPage extends ConsumerStatefulWidget {
  const DashboardPage({super.key});

  @override
  ConsumerState<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends ConsumerState<DashboardPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() async {
      ref.read(dashboardProvider.notifier).loadAll();
      // Connect SignalR for real-time notifications
      final auth = ref.read(authProvider);
      if (auth.status == AuthStatus.authenticated) {
        // Aseguramos que la URL base se concatene correctamente sin doble slash
        const apiUrl = String.fromEnvironment('API_URL', defaultValue: 'https://nexora-9yal.onrender.com/api/v1');
        final cleanApiUrl = apiUrl.replaceAll(RegExp(r'(?<!:)/+'), '/').replaceAll(RegExp(r'/$'), '');
        
        final storage = ref.read(secureStorageProvider);
        final token = await storage.getAccessToken();
        if (token != null) {
          ref.read(signalRProvider.notifier).connect(cleanApiUrl, token);
        }
      }
    });
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
            Image.asset('assets/logo3.png', height: 28, errorBuilder: (_, __, ___) => const SizedBox.shrink()),
            const SizedBox(width: 8),
            Text('Hola, $user'),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.search),
            tooltip: 'Buscar (Ctrl+K)',
            onPressed: () => CommandPalette.show(context),
          ),
          Consumer(
            builder: (_, ref, __) {
              final notifState = ref.watch(signalRProvider);
              final unread = notifState.notifications.length;
              return Stack(
                children: [
                  IconButton(
                    icon: const Icon(Icons.notifications_outlined),
                    onPressed: unread > 0
                        ? () => _showNotifications(context, ref)
                        : null,
                  ),
                  if (unread > 0)
                    Positioned(
                      right: 8,
                      top: 8,
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
                ],
              );
            },
          ),
          Consumer(
            builder: (_, ref, __) {
              final mode = ref.watch(themeModeProvider);
              return IconButton(
                icon: Icon(mode == ThemeMode.dark ? Icons.light_mode : Icons.dark_mode),
                onPressed: () => ref.read(themeModeProvider.notifier).toggle(),
              );
            },
          ),
          IconButton(
            icon: const Icon(Icons.person_outline),
            onPressed: () => context.push('/profile'),
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () => ref.read(authProvider.notifier).logout(),
          ),
        ],
      ),
      body: dash.loading
          ? const Center(child: CircularProgressIndicator())
          : dash.error != null
              ? Center(child: Text(dash.error!))
              : RefreshIndicator(
                  onRefresh: () => ref.read(dashboardProvider.notifier).refresh(),
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
                        Card(child: Padding(
                          padding: const EdgeInsets.all(24),
                          child: Center(child: Text('No hay solicitudes recientes', style: const TextStyle(color: Colors.grey))),
                        )),
                    ],
                  ),
                ),
      ),
    ),
  );
  }

  void _showNotifications(BuildContext context, WidgetRef ref) {
    final list = ref.read(signalRProvider).notifications;
    showDialog(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Notificaciones'),
        content: SizedBox(
          width: double.maxFinite,
          child: list.isEmpty
              ? const Text('Sin notificaciones')
              : ListView.separated(
                  shrinkWrap: true,
                  itemCount: list.length,
                  separatorBuilder: (_, __) => const Divider(height: 1),
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
        actions: [
          TextButton(onPressed: () => ref.read(signalRProvider.notifier).clearNotifications(), child: const Text('Limpiar')),
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cerrar')),
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
        Row(
          children: [
            Expanded(child: _KpiCard(
              icon: Icons.people,
              label: 'Activos',
              value: '${kpis.activeEmployees}',
              color: const Color(0xFF4F46E5),
            )),
            const SizedBox(width: 8),
            Expanded(child: _KpiCard(
              icon: Icons.pending_actions,
              label: 'Pendientes',
              value: '${kpis.pendingVacationRequests + kpis.pendingPermissionRequests}',
              color: const Color(0xFFD97706),
            )),
          ],
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            Expanded(child: _KpiCard(
              icon: Icons.cake,
              label: 'Cumpleaños',
              value: '${kpis.birthdaysThisMonth}',
              color: const Color(0xFF0891B2),
            )),
            const SizedBox(width: 8),
            Expanded(child: _KpiCard(
              icon: Icons.work_history,
              label: 'Aniversarios',
              value: '${kpis.workAnniversariesThisMonth}',
              color: const Color(0xFF059669),
            )),
          ],
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
        ResponsiveGrid(
          spacing: 12,
          children: [
            _NavCard(icon: Icons.people, label: 'Empleados', color: const Color(0xFF4F46E5), route: '/employees'),
            _NavCard(icon: Icons.calendar_month, label: 'Calendario', color: const Color(0xFF0891B2), route: '/absence-calendar'),
            _NavCard(icon: Icons.business, label: 'Dptos.', color: const Color(0xFF7C3AED), route: '/departments'),
            _NavCard(icon: Icons.beach_access, label: 'Vacaciones', color: const Color(0xFFD97706), route: '/vacations'),
            _NavCard(icon: Icons.description, label: 'Permisos', color: const Color(0xFFDC2626), route: '/permissions'),
            _NavCard(icon: Icons.schedule, label: 'Asistencia', color: const Color(0xFF059669), route: '/attendance'),
            _NavCard(icon: Icons.person, label: 'Perfil', color: const Color(0xFF9333EA), route: '/profile'),
            _NavCard(icon: Icons.assessment, label: 'Reportes', color: const Color(0xFF0D9488), route: '/reports'),
            _NavCard(icon: Icons.admin_panel_settings, label: 'Admin', color: const Color(0xFF1E293B), route: '/admin/users'),
            _NavCard(icon: Icons.settings, label: 'Config.', color: const Color(0xFF64748B), route: '/settings'),
            _NavCard(icon: Icons.receipt_long, label: 'Nómina', color: const Color(0xFF059669), route: '/payroll'),
            _NavCard(icon: Icons.tablet, label: 'Kiosko', color: const Color(0xFF0891B2), route: '/attendance/kiosk'),
          ],
        ),
      ],
    );
  }

  Widget _buildRecentRequestTile(RecentRequestItem item) {
    final icon = item.requestType == 'vacacion' ? Icons.beach_access : Icons.description;
    final color = item.requestType == 'vacacion' ? const Color(0xFFD97706) : const Color(0xFFDC2626);
    return Card(
      child: ListTile(
        leading: Icon(icon, color: color),
        title: Text(item.employeeName),
        subtitle: Text(item.description ?? item.requestType),
        trailing: _statusChip(item.status),
        onTap: () {
          if (item.requestType == 'vacacion') context.push('/vacations/${item.id}');
          else context.push('/permissions/${item.id}');
        },
      ),
    );
  }

  Widget _statusChip(String status) {
    final (label, color) = switch (status) {
      'approved' => ('Aprobado', Colors.green),
      'rejected' => ('Rechazado', Colors.red),
      'pending' => ('Pendiente', Colors.orange),
      _ => (status, Colors.grey),
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
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: color, size: 24),
            const SizedBox(height: 8),
            Text(value, style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: color)),
            Text(label, style: const TextStyle(fontSize: 12, color: Colors.grey)),
          ],
        ),
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
    return Card(
      child: InkWell(
        onTap: route != null ? () => context.push(route!) : null,
        borderRadius: BorderRadius.circular(12),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 28, color: color),
            const SizedBox(height: 8),
            Text(label, style: TextStyle(fontWeight: FontWeight.w600, color: color, fontSize: 12)),
          ],
        ),
      ),
    );
  }
}
