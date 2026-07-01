import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/widgets/responsive_layout.dart';

class WarrantyDashboardPage extends ConsumerStatefulWidget {
  const WarrantyDashboardPage({super.key});
  @override
  ConsumerState<WarrantyDashboardPage> createState() => _WarrantyDashboardPageState();
}

class _WarrantyDashboardPageState extends ConsumerState<WarrantyDashboardPage> {
  Map<String, dynamic>? _metrics;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('warranty-dashboard/metrics');
      _metrics = r.data as Map<String, dynamic>;
    } catch (_) {}
    setState(() => _loading = false);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return Scaffold(
      backgroundColor: isDark ? ZColors.darkBackground : ZColors.neutral50,
      appBar: AppBar(
        title: const Text('Garantías'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: 'Nueva garantía',
            onPressed: () => context.push('/warranties/new'),
          ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(ZSpacing.lg),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('Resumen de Garantías', style: ZTypography.titleLarge),
                  const SizedBox(height: ZSpacing.md),
                  ResponsiveGrid(
                    mobileColumns: 1,
                    tabletColumns: 2,
                    desktopColumns: 4,
                    children: [
                      ZStatCard(
                        title: 'Total',
                        value: '${_metrics?['total'] ?? 0}',
                        icon: Icons.shield_outlined,
                        variant: ZStatVariant.primary,
                      ),
                      ZStatCard(
                        title: 'Activas',
                        value: '${_metrics?['active'] ?? 0}',
                        icon: Icons.check_circle_outline,
                        variant: ZStatVariant.info,
                      ),
                      ZStatCard(
                        title: 'En reparación',
                        value: '${_metrics?['inRepair'] ?? 0}',
                        icon: Icons.build_outlined,
                        variant: ZStatVariant.warning,
                      ),
                      ZStatCard(
                        title: 'Vencidas',
                        value: '${_metrics?['expired'] ?? 0}',
                        icon: Icons.warning_amber_outlined,
                        variant: ZStatVariant.danger,
                      ),
                    ],
                  ),

                  const SizedBox(height: ZSpacing.xxl),

                  const Text('Acciones rápidas', style: ZTypography.titleLarge),
                  const SizedBox(height: ZSpacing.md),
                  ResponsiveGrid(
                    mobileColumns: 2,
                    tabletColumns: 3,
                    desktopColumns: 4,
                    children: [
                      _ActionCard(
                        label: 'Lista de garantías',
                        icon: Icons.list,
                        color: ZColors.brandPrimary,
                        route: '/warranties',
                      ),
                      _ActionCard(
                        label: 'Nueva garantía',
                        icon: Icons.add_circle_outline,
                        color: ZColors.success,
                        route: '/warranties/new',
                      ),
                    ],
                  ),
                ],
              ),
            ),
    );
  }
}

class _ActionCard extends StatelessWidget {
  final String label;
  final IconData icon;
  final Color color;
  final String route;

  const _ActionCard({
    required this.label,
    required this.icon,
    required this.color,
    required this.route,
  });

  @override
  Widget build(BuildContext context) {
    return ZCard(
      child: InkWell(
        onTap: () => context.push(route),
        borderRadius: BorderRadius.circular(ZRadii.lg),
        child: Padding(
          padding: const EdgeInsets.all(ZSpacing.lg),
          child: Column(
            children: [
              Container(
                padding: const EdgeInsets.all(ZSpacing.md),
                decoration: BoxDecoration(
                  color: color.withAlpha(20),
                  borderRadius: BorderRadius.circular(ZRadii.md),
                ),
                child: Icon(icon, color: color, size: 24),
              ),
              const SizedBox(height: ZSpacing.sm),
              Text(label, style: ZTypography.labelMedium.copyWith(fontWeight: FontWeight.bold), textAlign: TextAlign.center),
            ],
          ),
        ),
      ),
    );
  }
}
