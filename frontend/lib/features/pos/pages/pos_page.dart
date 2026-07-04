import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/pos_provider.dart';

/// PosPage — Point of Sale interface.
/// Two-panel layout: product search/grid on left, cart on right.
/// On mobile, cart is shown as a bottom sheet.
class PosPage extends ConsumerStatefulWidget {
  const PosPage({super.key});

  @override
  ConsumerState<PosPage> createState() => _PosPageState();
}

class _PosPageState extends ConsumerState<PosPage> {
  final _searchCtrl = TextEditingController();
  String _searchQuery = '';

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final posState = ref.watch(posProvider);
    final productsAsync = ref.watch(posProductsProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Punto de Venta'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () {
            if (posState.items.isNotEmpty) {
              showDialog(
                context: context,
                builder: (_) => AlertDialog(
                  title: const Text('¿Salir del POS?'),
                  content: const Text('Se perderán los items del carrito actual.'),
                  actions: [
                    TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancelar')),
                    FilledButton(
                      onPressed: () {
                        ref.read(posProvider.notifier).clearCart();
                        Navigator.pop(context);
                        context.pop();
                      },
                      style: FilledButton.styleFrom(backgroundColor: ZColors.danger),
                      child: const Text('Salir'),
                    ),
                  ],
                ),
              );
            } else {
              context.pop();
            }
          },
        ),
        actions: [
          if (posState.items.isNotEmpty)
            IconButton(
              icon: const Icon(Icons.delete_sweep_outlined),
              tooltip: 'Vaciar carrito',
              onPressed: () => ref.read(posProvider.notifier).clearCart(),
            ),
        ],
      ),
      body: LayoutBuilder(
        builder: (context, constraints) {
          final isWide = constraints.maxWidth > 720;

          if (isWide) {
            return _buildWideLayout(posState, productsAsync, isDark);
          }
          return _buildMobileLayout(posState, productsAsync, isDark);
        },
      ),
    );
  }

  Widget _buildWideLayout(PosState posState, AsyncValue<List<Map<String, dynamic>>> productsAsync, bool isDark) {
    return Row(
      children: [
        // ── Left: Product Grid ──
        Expanded(
          flex: 3,
          child: _buildProductPanel(productsAsync, isDark),
        ),
        // ── Right: Cart ──
        Container(
          width: 340,
          decoration: BoxDecoration(
            color: isDark ? ZColors.darkSurface : ZColors.surface,
            border: Border(
              left: BorderSide(color: isDark ? ZColors.darkBorder : ZColors.border),
            ),
          ),
          child: _buildCartPanel(posState, isDark),
        ),
      ],
    );
  }

  Widget _buildMobileLayout(PosState posState, AsyncValue<List<Map<String, dynamic>>> productsAsync, bool isDark) {
    return Column(
      children: [
        // Product search + grid
        Expanded(
          child: _buildProductPanel(productsAsync, isDark),
        ),
        // Cart bar at bottom
        _buildMobileCartBar(posState, isDark),
      ],
    );
  }

  Widget _buildProductPanel(AsyncValue<List<Map<String, dynamic>>> productsAsync, bool isDark) {
    return Column(
      children: [
        // Search bar
        Padding(
          padding: const EdgeInsets.all(12),
          child: TextField(
            controller: _searchCtrl,
            decoration: InputDecoration(
              hintText: 'Buscar producto por nombre o código...',
              prefixIcon: const Icon(Icons.search),
              suffixIcon: _searchQuery.isNotEmpty
                  ? IconButton(
                      icon: const Icon(Icons.clear),
                      onPressed: () {
                        _searchCtrl.clear();
                        setState(() => _searchQuery = '');
                      },
                    )
                  : null,
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
            ),
            onChanged: (v) => setState(() => _searchQuery = v),
          ),
        ),
        // Product grid
        Expanded(
          child: productsAsync.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (e, _) => Center(child: Text('Error: $e')),
            data: (products) {
              final filtered = _searchQuery.isEmpty
                  ? products
                  : products.where((p) {
                      final q = _searchQuery.toLowerCase();
                      final name = (p['name'] ?? '').toString().toLowerCase();
                      final code = (p['code'] ?? '').toString().toLowerCase();
                      return name.contains(q) || code.contains(q);
                    }).toList();

              if (filtered.isEmpty) {
                return const ZEmptyState(
                  icon: Icons.inventory_2_outlined,
                  title: 'Sin productos',
                  subtitle: 'No se encontraron productos',
                );
              }

              return LayoutBuilder(
                builder: (_, gridConstraints) {
                  final crossCount = gridConstraints.maxWidth < 576
                      ? 2
                      : gridConstraints.maxWidth < 992
                          ? 3
                          : 4;
                  return GridView.builder(
                    padding: const EdgeInsets.symmetric(horizontal: 12),
                    gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: crossCount,
                      mainAxisSpacing: 8,
                      crossAxisSpacing: 8,
                      childAspectRatio: crossCount <= 2 ? 2.0 : 1.8,
                    ),
                    itemCount: filtered.length,
                    itemBuilder: (_, i) {
                      final p = filtered[i];
                      final name = p['name'] ?? '';
                      final code = p['code'] ?? '';
                      final price = (p['price'] ?? 0).toDouble();
                      final stock = (p['stock'] ?? 0).toDouble();
                      final id = p['id'] ?? '';

                      return _ProductTile(
                        name: name,
                        code: code,
                        price: price,
                        stock: stock,
                        onTap: () {
                          ref.read(posProvider.notifier).addItem(
                                PosCartItem(
                                  productId: id,
                                  name: name,
                                  code: code,
                                  price: price,
                                ),
                              );
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text('$name agregado al carrito'),
                              duration: const Duration(seconds: 1),
                            ),
                          );
                        },
                      );
                    },
                  );
                },
              );
            },
          ),
        ),
      ],
    );
  }

  Widget _buildCartPanel(PosState posState, bool isDark) {
    return Column(
      children: [
        // Cart header
        Container(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              const Icon(Icons.shopping_cart_outlined, size: 20),
              const SizedBox(width: 8),
              Text(
                'Carrito (${posState.itemCount})',
                style: ZTypography.titleMedium.copyWith(fontWeight: FontWeight.w600),
              ),
            ],
          ),
        ),
        const Divider(height: 1),

        // Cart items
        Expanded(
          child: posState.items.isEmpty
              ? const ZEmptyState(
                  icon: Icons.shopping_cart_outlined,
                  title: 'Carrito vacío',
                  subtitle: 'Selecciona productos para agregar',
                )
              : ListView.separated(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                  itemCount: posState.items.length,
                  separatorBuilder: (_, _) => const Divider(height: 1),
                  itemBuilder: (_, i) {
                    final item = posState.items[i];
                    return _CartItemTile(
                      name: item.name,
                      code: item.code,
                      price: item.price,
                      quantity: item.quantity,
                      onQuantityChanged: (qty) {
                        ref.read(posProvider.notifier).updateQuantity(item.productId, qty);
                      },
                      onRemove: () {
                        ref.read(posProvider.notifier).removeItem(item.productId);
                      },
                    );
                  },
                ),
        ),

        // Cart summary + submit
        const Divider(height: 1),
        _CartSummary(posState: posState),
      ],
    );
  }

  Widget _buildMobileCartBar(PosState posState, bool isDark) {
    return GestureDetector(
      onTap: () => _showMobileCartSheet(posState),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: BoxDecoration(
          color: isDark ? ZColors.darkSurface : ZColors.surface,
          border: Border(top: BorderSide(color: isDark ? ZColors.darkBorder : ZColors.border)),
        ),
        child: Row(
          children: [
            Stack(
              clipBehavior: Clip.none,
              children: [
                Icon(Icons.shopping_cart_outlined, color: isDark ? ZColors.neutral200 : ZColors.neutral700),
                if (posState.items.isNotEmpty)
                  Positioned(
                    right: -6,
                    top: -6,
                    child: Container(
                      padding: const EdgeInsets.all(4),
                      decoration: const BoxDecoration(color: ZColors.brandPrimary, shape: BoxShape.circle),
                      child: Text('${posState.itemCount}', style: const TextStyle(color: Colors.white, fontSize: 10)),
                    ),
                  ),
              ],
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                posState.items.isEmpty ? 'Toca para agregar productos' : '${posState.itemCount} productos',
                style: ZTypography.bodyMedium.copyWith(
                  color: isDark ? ZColors.neutral300 : ZColors.neutral600,
                ),
              ),
            ),
            Text(
              '\$${posState.total.toStringAsFixed(2)}',
              style: ZTypography.titleMedium.copyWith(
                fontWeight: FontWeight.w700,
                color: ZColors.success,
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _showMobileCartSheet(PosState posState) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (ctx) => DraggableScrollableSheet(
        initialChildSize: 0.7,
        minChildSize: 0.4,
        maxChildSize: 0.95,
        expand: false,
        builder: (ctx, controller) => Column(
          children: [
            // Handle
            Container(
              margin: const EdgeInsets.only(top: 8),
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: ZColors.neutral300,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            Expanded(
              child: _buildCartPanel(posState, Theme.of(context).brightness == Brightness.dark),
            ),
          ],
        ),
      ),
    );
  }
}

class _ProductTile extends StatelessWidget {
  final String name;
  final String code;
  final double price;
  final double stock;
  final VoidCallback onTap;

  const _ProductTile({
    required this.name,
    required this.code,
    required this.price,
    required this.stock,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final lowStock = stock <= 0;

    return GestureDetector(
      onTap: lowStock ? null : onTap,
      child: Container(
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: isDark ? ZColors.darkSurface : ZColors.surface,
          borderRadius: BorderRadius.circular(ZRadii.md),
          border: Border.all(
            color: isDark ? ZColors.darkBorder : ZColors.border,
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(
              name,
              style: ZTypography.bodySmall.copyWith(fontWeight: FontWeight.w600),
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
            ),
            const SizedBox(height: 2),
            Text(
              code,
              style: ZTypography.labelSmall.copyWith(
                color: isDark ? ZColors.neutral400 : ZColors.neutral500,
              ),
            ),
            const Spacer(),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  '\$${price.toStringAsFixed(2)}',
                  style: ZTypography.labelMedium.copyWith(
                    fontWeight: FontWeight.w700,
                    color: ZColors.success,
                  ),
                ),
                if (lowStock)
                  const Text('Agotado', style: TextStyle(fontSize: 10, color: ZColors.danger))
                else
                  Text(
                    'Stock: ${stock.toStringAsFixed(0)}',
                    style: ZTypography.labelSmall.copyWith(
                      color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                      fontSize: 10,
                    ),
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _CartItemTile extends StatelessWidget {
  final String name;
  final String code;
  final double price;
  final double quantity;
  final ValueChanged<double> onQuantityChanged;
  final VoidCallback onRemove;

  const _CartItemTile({
    required this.name,
    required this.code,
    required this.price,
    required this.quantity,
    required this.onQuantityChanged,
    required this.onRemove,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        children: [
          // Info
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  name,
                  style: ZTypography.bodySmall.copyWith(fontWeight: FontWeight.w600),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                Text(
                  '\$${price.toStringAsFixed(2)} c/u',
                  style: ZTypography.labelSmall.copyWith(
                    color: ZColors.neutral500,
                  ),
                ),
              ],
            ),
          ),
          // Quantity controls
          Row(
            children: [
              IconButton(
                icon: const Icon(Icons.remove_circle_outline, size: 20),
                onPressed: () => onQuantityChanged(quantity - 1),
                constraints: const BoxConstraints(minWidth: 32, minHeight: 32),
              ),
              Text(
                quantity.toStringAsFixed(quantity == quantity.roundToDouble() ? 0 : 1),
                style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600),
              ),
              IconButton(
                icon: const Icon(Icons.add_circle_outline, size: 20),
                onPressed: () => onQuantityChanged(quantity + 1),
                constraints: const BoxConstraints(minWidth: 32, minHeight: 32),
              ),
            ],
          ),
          // Remove
          IconButton(
            icon: const Icon(Icons.close, size: 18, color: ZColors.danger),
            onPressed: onRemove,
            constraints: const BoxConstraints(minWidth: 32, minHeight: 32),
          ),
        ],
      ),
    );
  }
}

class _CartSummary extends ConsumerWidget {
  final PosState posState;

  const _CartSummary({required this.posState});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Container(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          // Subtotal
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Subtotal', style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500)),
              Text('\$${posState.subtotal.toStringAsFixed(2)}', style: ZTypography.bodySmall),
            ],
          ),
          if (posState.discount > 0) ...[
            const SizedBox(height: 4),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Descuento', style: ZTypography.bodySmall.copyWith(color: ZColors.danger)),
                Text('-\$${posState.discount.toStringAsFixed(2)}', style: ZTypography.bodySmall.copyWith(color: ZColors.danger)),
              ],
            ),
          ],
          const Divider(),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Total', style: ZTypography.titleMedium.copyWith(fontWeight: FontWeight.w700)),
              Text(
                '\$${posState.total.toStringAsFixed(2)}',
                style: ZTypography.titleMedium.copyWith(fontWeight: FontWeight.w700, color: ZColors.success),
              ),
            ],
          ),
          const SizedBox(height: 12),
          // Payment method selector
          _PaymentMethodSelector(
            selected: posState.paymentMethod,
            onChanged: (m) => ref.read(posProvider.notifier).setPaymentMethod(m),
          ),
          const SizedBox(height: 12),
          // Submit button
          SizedBox(
            width: double.infinity,
            child: FilledButton.icon(
              onPressed: posState.items.isEmpty || posState.loading
                  ? null
                  : () async {
                      final success = await ref.read(posProvider.notifier).submitSale();
                      if (success && context.mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(content: Text('Venta registrada exitosamente'), backgroundColor: ZColors.success),
                        );
                        context.pop();
                      }
                    },
              icon: posState.loading
                  ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                  : const Icon(Icons.payment, size: 20),
              label: Text(posState.loading ? 'Procesando...' : 'Cobrar \$${posState.total.toStringAsFixed(2)}'),
              style: FilledButton.styleFrom(
                backgroundColor: ZColors.success,
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(vertical: 14),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _PaymentMethodSelector extends ConsumerWidget {
  final String selected;
  final ValueChanged<String> onChanged;

  const _PaymentMethodSelector({required this.selected, required this.onChanged});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final methods = [
      ('cash', 'Efectivo', Icons.payments_outlined),
      ('card', 'Tarjeta', Icons.credit_card_outlined),
      ('transfer', 'Transferencia', Icons.account_balance_outlined),
      ('credit', 'Crédito', Icons.timer_outlined),
    ];

    return Row(
      children: methods.map((m) {
        final isSelected = selected == m.$1;
        return Expanded(
          child: GestureDetector(
            onTap: () => onChanged(m.$1),
            child: Container(
              margin: const EdgeInsets.symmetric(horizontal: 3),
              padding: const EdgeInsets.symmetric(vertical: 8),
              decoration: BoxDecoration(
                color: isSelected ? ZColors.brandPrimary.withValues(alpha: 0.1) : Colors.transparent,
                borderRadius: BorderRadius.circular(ZRadii.sm),
                border: Border.all(
                  color: isSelected ? ZColors.brandPrimary : ZColors.border,
                ),
              ),
              child: Column(
                children: [
                  Icon(m.$3, size: 18, color: isSelected ? ZColors.brandPrimary : ZColors.neutral500),
                  const SizedBox(height: 2),
                  Text(
                    m.$2,
                    style: ZTypography.labelSmall.copyWith(
                      fontSize: 10,
                      color: isSelected ? ZColors.brandPrimary : ZColors.neutral500,
                    ),
                  ),
                ],
              ),
            ),
          ),
        );
      }).toList(),
    );
  }
}
