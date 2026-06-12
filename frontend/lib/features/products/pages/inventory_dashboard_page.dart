import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../core/widgets/responsive_layout.dart';

class InventoryDashboardPage extends ConsumerWidget {
  const InventoryDashboardPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      backgroundColor: Theme.of(context).brightness == Brightness.dark ? ZColors.darkBackground : ZColors.neutral50,
      appBar: AppBar(
        title: const Text('Inventario y Costeo'),
        actions: [
          IconButton(icon: const Icon(Icons.search), onPressed: () => ZCommandPalette.show(context)),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Executive Summary ──
            const Text('Valoración de Inventario', style: ZTypography.titleLarge),
            const SizedBox(height: 16),
            ResponsiveGrid(
              mobileColumns: 1,
              tabletColumns: 2,
              desktopColumns: 4,
              children: [
                const ZStatCard(title: 'Valor Total (Costo)', value: 'C\$ 2,850,000.00', icon: Icons.inventory_2_outlined, variant: ZStatVariant.primary),
                const ZStatCard(title: 'Productos en Stock', value: '452', icon: Icons.grid_view_outlined, variant: ZStatVariant.info),
                const ZStatCard(title: 'Productos en Stock Bajo', value: '18', icon: Icons.warning_amber_outlined, variant: ZStatVariant.warning),
                const ZStatCard(title: 'Valor en Tránsito', value: 'C\$ 125,000.00', icon: Icons.local_shipping_outlined, variant: ZStatVariant.neutral),
              ],
            ),
            
            const SizedBox(height: 40),
            
            // ── Quick Operations ──
            _buildQuickOperations(context),
            
            const SizedBox(height: 40),

            // ── Low Stock Alert ──
            const Text('Alertas de Stock Bajo', style: ZTypography.titleLarge),
            const SizedBox(height: 16),
            ZCard(
              padding: EdgeInsets.zero,
              child: SizedBox(
                height: 300,
                child: Center(child: Text('Cargando productos...', style: TextStyle(color: ZColors.neutral500))),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildQuickOperations(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('Operaciones Rápidas', style: ZTypography.titleLarge),
        const SizedBox(height: 16),
        ResponsiveGrid(
          mobileColumns: 2,
          tabletColumns: 3,
          desktopColumns: 5,
          children: [
            _OperationButton(label: 'Productos', icon: Icons.category, color: ZColors.brandPrimary, route: '/products'),
            _OperationButton(label: 'Movimientos', icon: Icons.sync_alt, color: ZColors.success, route: '/products/movements'),
            _OperationButton(label: 'Ajustes', icon: Icons.tune, color: ZColors.warning, route: '/products/adjustments'),
            _OperationButton(label: 'Valuación', icon: Icons.analytics, color: ZColors.brandAccent, route: '/products/valuation'),
            _OperationButton(label: 'Kardex', icon: Icons.history, color: ZColors.moduleIa, route: '/products/kardex'),
          ],
        ),
      ],
    );
  }
}

class _OperationButton extends StatelessWidget {
  final String label;
  final IconData icon;
  final Color color;
  final String route;

  const _OperationButton({required this.label, required this.icon, required this.color, required this.route});

  @override
  Widget build(BuildContext context) {
    return ZCard(
      child: InkWell(
        onTap: () => context.push(route),
        borderRadius: BorderRadius.circular(ZRadii.lg),
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(color: color.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(ZRadii.md)),
                child: Icon(icon, color: color, size: 24),
              ),
              const SizedBox(height: 12),
              Text(label, style: ZTypography.labelMedium.copyWith(fontWeight: FontWeight.bold)),
            ],
          ),
        ),
      ),
    );
  }
}
