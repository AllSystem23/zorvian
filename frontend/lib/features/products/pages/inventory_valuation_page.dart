import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/providers/company_currency_provider.dart';
import '../../../core/widgets/responsive_layout.dart';
import '../../../shared/ds/ds.dart';
import '../providers/product_provider.dart';

class _CategoryValuation {
  final String category;
  final int products;
  final double stock;
  final double value;

  const _CategoryValuation({
    required this.category,
    required this.products,
    required this.stock,
    required this.value,
  });
}

class InventoryValuationPage extends ConsumerStatefulWidget {
  const InventoryValuationPage({super.key});

  @override
  ConsumerState<InventoryValuationPage> createState() =>
      _InventoryValuationPageState();
}

class _InventoryValuationPageState
    extends ConsumerState<InventoryValuationPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() {
      if (mounted) ref.read(productProvider.notifier).load();
    });
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(productProvider);
    final theme = Theme.of(context);
    final fmt = ref.watch(currencyFormatServiceProvider);

    final items = state.items;
    final costedItems = items.where((p) => (p.cost ?? 0) > 0).toList();
    final totalValue = items.fold<double>(
      0,
      (sum, p) => sum + (p.cost ?? 0) * p.stock,
    );
    final totalStock = items.fold<double>(0, (sum, p) => sum + p.stock);
    final lowStock = items
        .where((p) => p.stock > 0 && p.stock <= p.minStock)
        .length;
    final outOfStock = items.where((p) => p.stock <= 0 && p.isActive).length;
    final averageCost = costedItems.isEmpty
        ? 0.0
        : costedItems.fold<double>(0, (sum, p) => sum + (p.cost ?? 0)) /
              costedItems.length;

    final categories = <String, _CategoryAccumulator>{};
    for (final p in items) {
      final category = p.categoryName?.isNotEmpty == true
          ? p.categoryName!
          : 'Sin categoría';
      final current =
          categories[category] ??
          const _CategoryAccumulator(products: 0, stock: 0, value: 0);
      categories[category] = current.copyWith(
        products: current.products + 1,
        stock: current.stock + p.stock,
        value: current.value + (p.cost ?? 0) * p.stock,
      );
    }

    final categoryRows =
        categories.entries
            .map(
              (e) => _CategoryValuation(
                category: e.key,
                products: e.value.products,
                stock: e.value.stock,
                value: e.value.value,
              ),
            )
            .toList()
          ..sort((a, b) => b.value.compareTo(a.value));

    return Scaffold(
      backgroundColor: theme.brightness == Brightness.dark
          ? ZColors.darkBackground
          : ZColors.neutral50,
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
          ? Center(
              child: Text(
                state.error!,
                style: TextStyle(color: theme.colorScheme.error),
              ),
            )
          : SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  ResponsiveGrid(
                    mobileColumns: 1,
                    tabletColumns: 2,
                    desktopColumns: 4,
                    children: [
                      ZStatCard(
                        title: 'Valor Total (Costo)',
                        value: fmt.currency(totalValue),
                        icon: Icons.inventory_2_outlined,
                        variant: ZStatVariant.primary,
                      ),
                      ZStatCard(
                        title: 'Unidades en Stock',
                        value: totalStock.toStringAsFixed(0),
                        icon: Icons.category_outlined,
                        variant: ZStatVariant.info,
                      ),
                      ZStatCard(
                        title: 'Stock Bajo',
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
                  const SizedBox(height: 24),
                  ZCard(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Resumen de Costeo',
                          style: ZTypography.titleMedium,
                        ),
                        const SizedBox(height: 16),
                        ResponsiveGrid(
                          mobileColumns: 1,
                          tabletColumns: 2,
                          desktopColumns: 3,
                          children: [
                            _MetricChip(
                              label: 'Productos activos',
                              value: items
                                  .where((p) => p.isActive)
                                  .length
                                  .toString(),
                            ),
                            _MetricChip(
                              label: 'Costo promedio',
                              value: fmt.currency(averageCost),
                            ),
                            _MetricChip(
                              label: 'Categorías',
                              value: categories.length.toString(),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),
                  LayoutBuilder(
                    builder: (context, constraints) {
                      final tableH = constraints.maxWidth < 576 ? 300.0 : 420.0;
                      return SizedBox(
                        height: tableH,
                        child: ZCard(
                      padding: EdgeInsets.zero,
                      child: ZDataTable<_CategoryValuation>(
                        columns: const [
                          ZColumn(id: 'category', label: 'Categoría'),
                          ZColumn(
                            id: 'products',
                            label: 'Productos',
                            numeric: true,
                          ),
                          ZColumn(id: 'stock', label: 'Stock', numeric: true),
                          ZColumn(
                            id: 'value',
                            label: 'Valor en Costo',
                            numeric: true,
                          ),
                        ],
                        rows: categoryRows,
                        rowMapper: (row) => DataRow(
                          cells: [
                            DataCell(
                              Text(
                                row.category,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                            DataCell(
                              Text(
                                row.products.toString(),
                                textAlign: TextAlign.right,
                              ),
                            ),
                            DataCell(
                              Text(
                                row.stock.toStringAsFixed(0),
                                textAlign: TextAlign.right,
                              ),
                            ),
                            DataCell(
                              Text(
                                fmt.currency(row.value),
                                textAlign: TextAlign.right,
                              ),
                            ),
                          ],
                        ),
                        emptyMessage: 'No hay productos para valorar',
                      ),
                    ),
                          );
                        },
                      ),
                ],
              ),
            ),
    );
  }
}

class _MetricChip extends StatelessWidget {
  final String label;
  final String value;

  const _MetricChip({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: ZTypography.labelMedium.copyWith(color: ZColors.neutral500),
          ),
          const SizedBox(height: 8),
          Text(
            value,
            style: ZTypography.titleLarge.copyWith(fontWeight: FontWeight.bold),
          ),
        ],
      ),
    );
  }
}

class _CategoryAccumulator {
  final int products;
  final double stock;
  final double value;

  const _CategoryAccumulator({
    required this.products,
    required this.stock,
    required this.value,
  });

  _CategoryAccumulator copyWith({int? products, double? stock, double? value}) {
    return _CategoryAccumulator(
      products: products ?? this.products,
      stock: stock ?? this.stock,
      value: value ?? this.value,
    );
  }
}
