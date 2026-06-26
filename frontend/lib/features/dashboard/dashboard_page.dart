import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../../core/network/api_config.dart';
import '../../core/services/signalr_service.dart';
import '../../core/widgets/bi/bi_bar_chart.dart';
import '../../core/widgets/bi/bi_pie_chart.dart';
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
  String _selectedPeriod = 'Este Mes';

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
    final storage = ref.read(secureStorageProvider);
    final token = await storage.getAccessToken();
    if (token != null) {
      ref.read(signalRProvider.notifier).connect(ApiConfig.originUrl, token);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = ref.watch(authProvider);
    final dash = ref.watch(dashboardProvider);
    final kpis = dash.kpis ?? DashboardKpis.empty();
    final user = auth.displayName ?? auth.email?.split('@').first ?? 'Usuario';

    return CallbackShortcuts(
      bindings: {
        SingleActivator(LogicalKeyboardKey.keyK, control: true, meta: true): () => ZCommandPalette.show(context),
        SingleActivator(LogicalKeyboardKey.keyK, control: true): () => ZCommandPalette.show(context),
        SingleActivator(LogicalKeyboardKey.slash, control: true): () => ZCommandPalette.show(context),
      },
      child: Focus(
        autofocus: true,
        child: Column(
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
                              padding: const EdgeInsets.all(24),
                              children: [
                                _buildHeader(user),
                                const SizedBox(height: 32),
                                ..._buildKpiSection(kpis),
                                const SizedBox(height: 40),
                                _buildModuleGrid(),
                                const SizedBox(height: 40),
                                _buildAnalysisSection(kpis),
                                const SizedBox(height: 40),
                                _buildRecentActivitySection(dash),
                              ],
                            ),
                          ),
                        ),
            ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader(String user) {
    final auth = ref.watch(authProvider);
    
    return Row(
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Bienvenido de nuevo,',
                style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral500),
              ),
              const SizedBox(height: 4),
              Row(
                children: [
                  Text(
                    user,
                    style: ZTypography.displaySmall.copyWith(
                      fontWeight: FontWeight.bold,
                      letterSpacing: -0.5,
                    ),
                  ),
                  if (auth.role == 'SuperAdmin') ...[
                    const SizedBox(width: 12),
                    const ZCompanySwitcher(),
                  ],
                ],
              ),
            ],
          ),
        ),
        ZPeriodDropdown(
          value: _selectedPeriod,
          onChanged: (v) => setState(() => _selectedPeriod = v!),
        ),
      ],
    );
  }



  List<Widget> _buildKpiSection(DashboardKpis kpis) {
    return [
      const _SectionHeader(title: 'Resumen Ejecutivo', icon: Icons.analytics_outlined),
      const SizedBox(height: 16),
      ResponsiveGrid(
        spacing: 16,
        runSpacing: 16,
        mobileColumns: 1,
        tabletColumns: 2,
        desktopColumns: 4,
        children: [
          ZStatCard(
            title: 'Colaboradores Activos',
            value: '${kpis.activeEmployees}',
            label: 'de ${kpis.totalEmployees} totales',
            icon: Icons.people_outline,
            variant: ZStatVariant.primary,
            trend: kpis.activeEmployeesTrend,
            trendUp: (kpis.activeEmployeesTrend ?? 0) >= 0,
            onTap: () => context.push('/employees'),
          ),
          ZStatCard(
            title: 'Solicitudes Pendientes',
            value: '${kpis.pendingVacationRequests + kpis.pendingPermissionRequests}',
            label: 'Vacaciones + Permisos',
            icon: Icons.pending_actions_outlined,
            variant: ZStatVariant.warning,
            trend: kpis.pendingRequestsTrend?.abs(),
            trendUp: (kpis.pendingRequestsTrend ?? 0) < 0,
            onTap: () => context.push('/vacations'),
          ),
          ZStatCard(
            title: 'Cumpleaños del Mes',
            value: '${kpis.birthdaysThisMonth}',
            label: 'celebraciones este mes',
            icon: Icons.cake_outlined,
            variant: ZStatVariant.info,
            trend: kpis.birthdaysTrend,
            trendUp: (kpis.birthdaysTrend ?? 0) >= 0,
            onTap: () => context.push('/employees'),
          ),
          ZStatCard(
            title: 'Aniversarios',
            value: '${kpis.workAnniversariesThisMonth}',
            label: 'años de servicio este mes',
            icon: Icons.work_history_outlined,
            variant: ZStatVariant.success,
            trend: kpis.anniversariesTrend,
            trendUp: (kpis.anniversariesTrend ?? 0) >= 0,
            onTap: () => context.push('/employees'),
          ),
        ],
      ),
    ];
  }

  Widget _buildModuleGrid() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const _SectionHeader(title: 'Módulos del Sistema', icon: Icons.grid_view_outlined),
        const SizedBox(height: 16),
        ResponsiveGrid(
          spacing: 16,
          runSpacing: 16,
          mobileColumns: 2,
          tabletColumns: 4,
          desktopColumns: 6,
          children: const [
            _ModuleCard(icon: Icons.people_alt_outlined, label: 'Capital Humano', color: ZColors.moduleHr, route: '/employees'),
            _ModuleCard(icon: Icons.receipt_long_outlined, label: 'Nómina', color: ZColors.moduleHr, route: '/payroll'),
            _ModuleCard(icon: Icons.shopping_cart_outlined, label: 'Ventas', color: ZColors.moduleSales, route: '/sales'),
            _ModuleCard(icon: Icons.inventory_2_outlined, label: 'Inventario', color: ZColors.moduleInventory, route: '/products'),
            _ModuleCard(icon: Icons.account_balance_outlined, label: 'Contabilidad', color: ZColors.moduleFinance, route: '/accounting/trial-balance'),
            _ModuleCard(icon: Icons.payments_outlined, label: 'Tesorería', color: ZColors.moduleTreasury, route: '/cash-registers'),
            _ModuleCard(icon: Icons.assignment_outlined, label: 'CRM', color: ZColors.moduleCrm, route: '/crm'),
            _ModuleCard(icon: Icons.smart_toy_outlined, label: 'Z-IA Assistant', color: ZColors.moduleIa, route: '/accounting/ai-assistant'),
            _ModuleCard(icon: Icons.bar_chart_outlined, label: 'Inteligencia BI', color: ZColors.moduleIa, route: '/bi/executive'),
            _ModuleCard(icon: Icons.description_outlined, label: 'Documentos', color: ZColors.moduleAdmin, route: '/documents'),
            _ModuleCard(icon: Icons.settings_outlined, label: 'Configuración', color: ZColors.moduleAdmin, route: '/settings'),
            _ModuleCard(icon: Icons.admin_panel_settings_outlined, label: 'Administración', color: ZColors.moduleSecurity, route: '/admin/users'),
          ],
        ),
      ],
    );
  }

  Widget _buildAnalysisSection(DashboardKpis kpis) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const _SectionHeader(title: 'Análisis de Talento', icon: Icons.insights_outlined),
        const SizedBox(height: 16),
        ResponsiveGrid(
          spacing: 16,
          runSpacing: 16,
          mobileColumns: 1,
          tabletColumns: 1,
          desktopColumns: 2,
          children: [
            ZCard(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Distribución por Departamento', style: ZTypography.titleSmall),
                  const SizedBox(height: 24),
                  BiBarChart(
                    items: kpis.employeesByDepartment
                        .map((d) => BarChartItem(d.departmentName, d.count.toDouble(), color: ZColors.brandAccent))
                        .toList(),
                    height: 220,
                  ),
                ],
              ),
            ),
            ZCard(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Estado de Fuerza Laboral', style: ZTypography.titleSmall),
                  const SizedBox(height: 24),
                  Center(
                    child: BiPieChart(
                      size: 180,
                      items: [
                        PieChartItem('Activos', _percentage(kpis.activeEmployees, kpis.totalEmployees), color: ZColors.success),
                        PieChartItem('Inactivos', _percentage(kpis.inactiveEmployees, kpis.totalEmployees), color: ZColors.neutral400),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildRecentActivitySection(DashboardState dash) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const _SectionHeader(title: 'Actividad Reciente', icon: Icons.history_outlined),
        const SizedBox(height: 16),
        if (dash.recentRequests.isNotEmpty)
          ListView.separated(
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            itemCount: dash.recentRequests.take(5).length,
            separatorBuilder: (_, _) => const SizedBox(height: 12),
            itemBuilder: (_, i) => _buildRecentRequestTile(dash.recentRequests[i]),
          )
        else
          ZCard(
            padding: const EdgeInsets.all(40),
            child: Center(
              child: Column(
                children: [
                  Icon(Icons.inbox_outlined, size: 48, color: ZColors.neutral300),
                  const SizedBox(height: 16),
                  Text('No hay actividad reciente registrada', style: TextStyle(color: ZColors.neutral500)),
                ],
              ),
            ),
          ),
      ],
    );
  }

  double _percentage(int part, int total) {
    if (total == 0) return 0;
    return part / total * 100;
  }

  Widget _buildRecentRequestTile(RecentRequestItem item) {
    final icon = item.requestType == 'vacacion' ? Icons.beach_access_outlined : Icons.description_outlined;
    final color = item.requestType == 'vacacion' ? ZColors.warning : ZColors.danger;
    
    return ZCard(
      child: ListTile(
        leading: Container(
          padding: const EdgeInsets.all(8),
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.1),
            shape: BoxShape.circle,
          ),
          child: Icon(icon, color: color, size: 20),
        ),
        title: Text(item.employeeName, style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Text(item.description ?? item.requestType.toUpperCase(), style: const TextStyle(fontSize: 12)),
        trailing: _statusBadge(item.status),
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

  Widget _statusBadge(String status) {
    final (label, type) = switch (status) {
      'approved' => ('Aprobado', ZBadgeType.success),
      'rejected' => ('Rechazado', ZBadgeType.danger),
      'pending' => ('Pendiente', ZBadgeType.warning),
      _ => (status, ZBadgeType.neutral),
    };
    return ZBadge(text: label, type: type);
  }
}

class _SectionHeader extends StatelessWidget {
  final String title;
  final IconData icon;

  const _SectionHeader({required this.title, required this.icon});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, size: 20, color: ZColors.brandPrimary),
        const SizedBox(width: 12),
        Text(
          title,
          style: ZTypography.titleMedium.copyWith(fontWeight: FontWeight.bold),
        ),
      ],
    );
  }
}

class _ModuleCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final Color color;
  final String route;

  const _ModuleCard({required this.icon, required this.label, required this.color, required this.route});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return ZCard(
      child: InkWell(
        onTap: () => context.push(route),
        borderRadius: BorderRadius.circular(ZRadii.lg),
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 20, horizontal: 12),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(ZRadii.md),
                ),
                child: Icon(icon, size: 24, color: color),
              ),
              const SizedBox(height: 12),
              Text(
                label,
                textAlign: TextAlign.center,
                style: ZTypography.labelSmall.copyWith(
                  fontWeight: FontWeight.w600,
                  color: isDark ? ZColors.neutral200 : ZColors.neutral700,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
