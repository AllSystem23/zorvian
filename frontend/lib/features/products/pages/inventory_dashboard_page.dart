import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../core/widgets/responsive_layout.dart';
import '../providers/product_provider.dart';

class InventoryDashboardPage extends ConsumerWidget {
  const InventoryDashboardPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(productProvider);
    final items = state.items;
    final totalValue = items.fold<double>(
      0,
      (sum, p) => sum + (p.cost ?? 0) * p.stock,
    );
    final lowStock = items
        .where((p) => p.stock > 0 && p.stock <= p.minStock)
        .length;
    final outOfStock = items.where((p) => p.stock <= 0 && p.isActive).length;

    return Scaffold(
      backgroundColor: Theme.of(context).brightness == Brightness.dark
          ? ZColors.darkBackground
          : ZColors.neutral50,
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
          ? Center(
              child: Text(
                state.error!,
                style: const TextStyle(color: ZColors.danger),
              ),
            )
          : SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Valoración de Inventario',
                    style: ZTypography.titleLarge,
                  ),
                  const SizedBox(height: 16),
                  ResponsiveGrid(
                    mobileColumns: 1,
                    tabletColumns: 2,
                    desktopColumns: 4,
                    children: [
                      ZStatCard(
                        title: 'Valor Total (Costo)',
                        value: 'C\$ ${totalValue.toStringAsFixed(2)}',
                        icon: Icons.inventory_2_outlined,
                        variant: ZStatVariant.primary,
                      ),
                      ZStatCard(
                        title: 'Productos en Stock',
                        value: items.length.toString(),
                        icon: Icons.grid_view_outlined,
                        variant: ZStatVariant.info,
                      ),
                      ZStatCard(
                        title: 'Productos en Stock Bajo',
                        value: lowStock.toString(),
                        icon: Icons.warning_amber_outlined,
                        variant: ZStatVariant.warning,
                      ),
                      ZStatCard(
                        title: 'Sin Stock',
                        value: outOfStock.toString(),
                        icon: Icons.remove_shopping_cart_outlined,
                        variant: ZStatVariant.danger,
                      ),
                    ],
                  ),

                  const SizedBox(height: 40),

                  _buildQuickOperations(context),

                  const SizedBox(height: 40),

                  const Text(
                    'Alertas de Stock Bajo',
                    style: ZTypography.titleLarge,
                  ),
                  const SizedBox(height: 16),
                  ZCard(
                    padding: EdgeInsets.zero,
                    child: items.isEmpty
                        ? const SizedBox(
                            height: 300,
                            child: Center(
                              child: Text('No hay productos registrados'),
                            ),
                          )
                        : ListView.separated(
                            shrinkWrap: true,
                            physics: const NeverScrollableScrollPhysics(),
                            itemCount: items
                                .where((p) => p.stock <= p.minStock)
                                .take(8)
                                .length,
                            separatorBuilder: (_, _) =>
                                const Divider(height: 1),
                            itemBuilder: (_, i) {
                              final p = items
                                  .where((p) => p.stock <= p.minStock)
                                  .toList()[i];
                              return ListTile(
                                title: Text('${p.name} (${p.code})'),
                                subtitle: Text(
                                  'Stock: ${p.stock.toStringAsFixed(0)} ${p.unit} · Mínimo: ${p.minStock.toStringAsFixed(0)}',
                                ),
                                trailing: p.stock <= 0
                                    ? const Text(
                                        'Sin stock',
                                        style: TextStyle(
                                          color: ZColors.danger,
                                          fontWeight: FontWeight.bold,
                                        ),
                                      )
                                    : const Text(
                                        'Stock bajo',
                                        style: TextStyle(
                                          color: ZColors.warning,
                                          fontWeight: FontWeight.bold,
                                        ),
                                      ),
                              );
                            },
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
            _OperationButton(
              label: 'Productos',
              icon: Icons.category,
              color: ZColors.brandPrimary,
              route: '/products',
            ),
            _OperationButton(
              label: 'Movimientos',
              icon: Icons.sync_alt,
              color: ZColors.success,
              route: '/products/movements',
            ),
            _OperationButton(
              label: 'Ajustes',
              icon: Icons.tune,
              color: ZColors.warning,
              route: '/products/adjustments',
            ),
            _OperationButton(
              label: 'Valuación',
              icon: Icons.analytics,
              color: ZColors.brandAccent,
              route: '/products/valuation',
            ),
            _OperationButton(
              label: 'Kardex',
              icon: Icons.history,
              color: ZColors.moduleIa,
              route: '/products/kardex',
            ),
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

  const _OperationButton({
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
          padding: const EdgeInsets.all(20),
          child: Column(
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(ZRadii.md),
                ),
                child: Icon(icon, color: color, size: 24),
              ),
              const SizedBox(height: 12),
              Text(
                label,
                style: ZTypography.labelMedium.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
