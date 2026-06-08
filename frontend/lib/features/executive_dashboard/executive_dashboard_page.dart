import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../../shared/ds/ds.dart';
import 'providers/executive_dashboard_provider.dart';

final class ExecutiveDashboardPage extends ConsumerStatefulWidget {
  const ExecutiveDashboardPage({super.key});
  @override
  ConsumerState<ExecutiveDashboardPage> createState() => _ExecutiveDashboardPageState();
}

final class _ExecutiveDashboardPageState extends ConsumerState<ExecutiveDashboardPage> {
  final _mainContentFocus = FocusNode();

  @override
  void dispose() {
    _mainContentFocus.dispose();
    super.dispose();
  }

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(executiveDashboardProvider.notifier).load());
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(executiveDashboardProvider);
    final theme = Theme.of(context);
    final auth = ref.watch(authProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text('Panel Ejecutivo · ${auth.displayName ?? "Usuario"}'),
        actions: [
          FocusTraversalGroup(
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                IconButton(
                  icon: const Icon(Icons.refresh),
                  onPressed: () => ref.read(executiveDashboardProvider.notifier).load(),
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
            child: ZMainContent(
              focusNode: _mainContentFocus,
              child: state.loading
                  ? const ZLiveRegion(
                      label: 'Cargando panel ejecutivo...',
                      child: Center(child: CircularProgressIndicator()),
                    )
                  : state.error != null
                      ? ZLiveRegion(
                          label: 'Error al cargar panel ejecutivo',
                          child: Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error))),
                        )
                      : RefreshIndicator(
                  onRefresh: () => ref.read(executiveDashboardProvider.notifier).load(),
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      _sectionTitle(theme, 'Comercial'),
                      FocusTraversalGroup(child: _KpiRow(kpis: [
                        _KpiData(Icons.today, 'Ventas hoy', _fmt(state.kpis!.todaySales), const Color(0xFF059669)),
                        _KpiData(Icons.monetization_on, 'Ventas/mes', _fmt(state.kpis!.monthSales), const Color(0xFF0891B2)),
                        _KpiData(Icons.receipt, 'Ticket prom.', _fmt(state.kpis!.averageTicket), const Color(0xFF7C3AED)),
                        _KpiData(Icons.shopping_cart, 'Transacciones', '${state.kpis!.todaySalesCount}', const Color(0xFFD97706)),
                      ])),
                      const SizedBox(height: 24),
                      _sectionTitle(theme, 'Créditos y Cobranza'),
                      FocusTraversalGroup(child: _KpiRow(kpis: [
                        _KpiData(Icons.credit_card, 'Créditos activos', '${state.kpis!.activeCredits}', const Color(0xFFD97706)),
                        _KpiData(Icons.warning, 'Vencidos', '${state.kpis!.overdueCredits}', const Color(0xFFDC2626)),
                        _KpiData(Icons.account_balance, 'Cartera total', _fmt(state.kpis!.totalPortfolio), const Color(0xFF4F46E5)),
                        _KpiData(Icons.payments, 'Recuperación/mes', _fmt(state.kpis!.monthlyRecovery), const Color(0xFF059669)),
                      ])),
                      const SizedBox(height: 24),
                      _sectionTitle(theme, 'Inventario'),
                      FocusTraversalGroup(child: _KpiRow(kpis: [
                        _KpiData(Icons.inventory_2, 'Productos', '${state.kpis!.totalProducts}', const Color(0xFF7C3AED)),
                        _KpiData(Icons.warning, 'Stock bajo', '${state.kpis!.lowStockProducts}', const Color(0xFFD97706)),
                        _KpiData(Icons.dangerous, 'Agotados', '${state.kpis!.outOfStockProducts}', const Color(0xFFDC2626)),
                      ])),
                      if (state.kpis!.topSelling.isNotEmpty) ...[
                        const SizedBox(height: 16),
                        _topSellingSection(theme, state.kpis!.topSelling),
                      ],
                      const SizedBox(height: 24),
                      _sectionTitle(theme, 'Caja'),
                      FocusTraversalGroup(child: _KpiRow(kpis: [
                        _KpiData(Icons.arrow_downward, 'Ingresos hoy', _fmt(state.kpis!.todayIncome), const Color(0xFF059669)),
                        _KpiData(Icons.arrow_upward, 'Egresos hoy', _fmt(state.kpis!.todayExpense), const Color(0xFFDC2626)),
                        _KpiData(Icons.monetization_on, 'Cajas abiertas', '${state.kpis!.openRegisters}', const Color(0xFF0891B2)),
                      ])),
                      const SizedBox(height: 24),
                      _sectionTitle(theme, 'Recursos Humanos'),
                      FocusTraversalGroup(child: _KpiRow(kpis: [
                        _KpiData(Icons.people, 'Activos', '${state.kpis!.activeEmployees}', const Color(0xFF4F46E5)),
                        _KpiData(Icons.beach_access, 'Vacaciones pend.', '${state.kpis!.pendingVacations}', const Color(0xFFD97706)),
                        _KpiData(Icons.description, 'Permisos activos', '${state.kpis!.activePermissions}', const Color(0xFFDC2626)),
                        _KpiData(Icons.people_outline, 'Total empleados', '${state.kpis!.totalEmployees}', const Color(0xFF64748B)),
                      ])),
                      const SizedBox(height: 24),
                      _buildQuickActions(context),
                    ],
                  ),
                ),
          ),
        ),
      ],
    ),
    );
  }

  Widget _sectionTitle(ThemeData theme, String title) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Text(title, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
    );
  }

  Widget _topSellingSection(ThemeData theme, List<TopProductItem> items) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Productos más vendidos', style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 8),
        ZCard(
          child: Column(
            children: items.take(5).map((p) => ListTile(
              dense: true,
              leading: CircleAvatar(
                backgroundColor: theme.colorScheme.primaryContainer,
                child: Text('${p.totalSold}', style: TextStyle(fontSize: 12, color: theme.colorScheme.onPrimaryContainer)),
              ),
              title: Text(p.productName),
              trailing: Text('${p.totalSold} uds.', style: const TextStyle(fontWeight: FontWeight.w600)),
            )).toList(),
          ),
        ),
      ],
    );
  }

  Widget _buildQuickActions(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Acceso rápido', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 12),
        FocusTraversalGroup(
          child: Wrap(
            spacing: 8, runSpacing: 8,
            children: [
              _ActionChip(Icons.person_add, 'Nuevo cliente', () => context.push('/clients/new')),
              _ActionChip(Icons.add_shopping_cart, 'Nueva venta', () => context.push('/sales/new')),
              _ActionChip(Icons.description, 'Nueva cotización', () => context.push('/quotes/new')),
              _ActionChip(Icons.inventory_2, 'Nuevo producto', () => context.push('/products/new')),
              _ActionChip(Icons.verified, 'Nueva garantía', () => context.push('/warranties/new')),
              _ActionChip(Icons.payments, 'Registrar pago', () => context.push('/credits')),
            ],
          ),
        ),
      ],
    );
  }

  String _fmt(double v) => '\$${v.toStringAsFixed(0)}';
}

final class _KpiData {
  final IconData icon;
  final String label;
  final String value;
  final Color color;
  const _KpiData(this.icon, this.label, this.value, this.color);
}

final class _KpiRow extends StatelessWidget {
  final List<_KpiData> kpis;
  const _KpiRow({required this.kpis});
  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8, runSpacing: 8,
      children: kpis.map((k) => SizedBox(
        width: MediaQuery.of(context).size.width > 600 ? 180 : (MediaQuery.of(context).size.width - 56) / 2,
        child: ZCard(
          padding: const EdgeInsets.all(12),
          child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(k.icon, color: k.color, size: 22),
                const SizedBox(height: 8),
                Text(k.value, style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: k.color)),
                Text(k.label, style: const TextStyle(fontSize: 11, color: ZColors.neutral500)),
              ],
            ),
          ),
        ),
      ).toList(),
    );
  }
}

final class _ActionChip extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;
  const _ActionChip(this.icon, this.label, this.onTap);
  @override
  Widget build(BuildContext context) {
    return ActionChip(avatar: Icon(icon, size: 18), label: Text(label), onPressed: onTap);
  }
}
