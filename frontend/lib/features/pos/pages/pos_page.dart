import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../features/sales/providers/sale_provider.dart';
import '../../../shared/ds/ds.dart';
import '../../../shared/printing/pdf_generator.dart';
import '../../../shared/printing/print_share_sheet.dart';
import '../../../shared/printing/print_utils.dart';
import '../../../shared/printing/qr_code_dialog.dart';
import '../../../shared/printing/thermal_template.dart';
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
  String? _selectedCategory;

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
      body: Column(
        children: [
          // Period status banner
          _PeriodBanner(periodAsync: ref.watch(posCurrentPeriodProvider)),
          Expanded(
            child: LayoutBuilder(
              builder: (context, constraints) {
                final isWide = constraints.maxWidth > 720;

                if (isWide) {
                  return _buildWideLayout(posState, productsAsync, isDark);
                }
                return _buildMobileLayout(posState, productsAsync, isDark);
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildWideLayout(PosState posState, AsyncValue<List<Map<String, dynamic>>> productsAsync, bool isDark) {
    return Row(
      children: [
          Expanded(
            flex: 3,
            child: _buildProductPanel(productsAsync, posState, isDark),
          ),
          Container(
            width: 360,
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
        Expanded(
          child: _buildProductPanel(productsAsync, posState, isDark),
        ),
        // Cart bar at bottom
        _buildMobileCartBar(posState, isDark),
      ],
    );
  }

  Widget _buildProductPanel(AsyncValue<List<Map<String, dynamic>>> productsAsync, PosState posState, bool isDark) {
    return Column(
      children: [
          // Search bar
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 12, 12, 4),
          child: TextField(
            controller: _searchCtrl,
            decoration: InputDecoration(
              hintText: 'Buscar por nombre o código...',
              prefixIcon: const Icon(Icons.search),
              suffixIcon: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  IconButton(
                    icon: const Icon(Icons.qr_code_scanner, size: 20),
                    tooltip: 'Escanear código de barras',
                    onPressed: () {
                      showScannerDialog(context, onScan: (code) {
                        setState(() {
                          _searchCtrl.text = code;
                          _searchQuery = code;
                        });
                        final messenger = ScaffoldMessenger.of(context);
                        ref.read(posProductsProvider.future).then((products) {
                          final q = code.toLowerCase();
                          final match = products.cast<Map<String, dynamic>?>().firstWhere(
                            (p) => (p?['code'] ?? '').toString().toLowerCase() == q,
                            orElse: () => null,
                          );
                          if (match == null || !context.mounted) return;
                          final ok = ref.read(posProvider.notifier).addItem(
                            PosCartItem(
                              productId: match['id'] ?? '',
                              name: match['name'] ?? '',
                              code: match['code'] ?? '',
                              price: (match['price'] ?? 0).toDouble(),
                              stock: (match['stock'] ?? 0).toDouble(),
                              taxRate: (match['taxRate'] ?? 0).toDouble(),
                            ),
                          );
                          if (ok) {
                            HapticFeedback.lightImpact();
                          } else {
                            final err = ref.read(posProvider).error;
                            messenger.showSnackBar(SnackBar(
                              content: Text(err ?? 'No se pudo agregar el producto'),
                              backgroundColor: ZColors.danger,
                            ));
                          }
                        });
                      });
                    },
                  ),
                  if (_searchQuery.isNotEmpty)
                    IconButton(
                      icon: const Icon(Icons.clear),
                      onPressed: () {
                        _searchCtrl.clear();
                        setState(() => _searchQuery = '');
                      },
                    ),
                ],
              ),
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
              filled: true,
              fillColor: isDark ? ZColors.neutral800.withValues(alpha: 0.5) : ZColors.neutral50,
            ),
            onChanged: (v) => setState(() => _searchQuery = v),
          ),
        ),
        // Product grid
        Expanded(
          child: productsAsync.when(
            loading: () => _buildProductSkeletons(isDark),
            error: (e, _) => ZEmptyState(
              icon: Icons.cloud_off_outlined,
              title: 'Error al cargar productos',
              subtitle: e.toString(),
            ),
            data: (products) {
              // Extract unique categories
              final categories = <String>{};
              for (final p in products) {
                final cat = p['category'] as String?;
                if (cat != null && cat.isNotEmpty) categories.add(cat);
              }
              final categoryList = categories.toList()..sort();

              // Filter by search + category
              var filtered = products;
              if (_searchQuery.isNotEmpty) {
                final q = _searchQuery.toLowerCase();
                filtered = filtered.where((p) {
                  final name = (p['name'] ?? '').toString().toLowerCase();
                  final code = (p['code'] ?? '').toString().toLowerCase();
                  return name.contains(q) || code.contains(q);
                }).toList();
              }
              if (_selectedCategory != null) {
                filtered = filtered.where((p) => p['category'] == _selectedCategory).toList();
              }

              return Column(
                children: [
                  // Category filter chips
                  if (categoryList.length >= 2)
                    SizedBox(
                      height: 38,
                      child: ListView(
                        scrollDirection: Axis.horizontal,
                        padding: const EdgeInsets.symmetric(horizontal: 12),
                        children: [
                          _CategoryChip(
                            label: 'Todas',
                            selected: _selectedCategory == null,
                            onTap: () => setState(() => _selectedCategory = null),
                          ),
                          ...categoryList.map((cat) => _CategoryChip(
                            label: cat,
                            selected: _selectedCategory == cat,
                            onTap: () => setState(() => _selectedCategory = cat),
                          )),
                        ],
                      ),
                    ),
                  // Grid
                  Expanded(
                    child: filtered.isEmpty
                        ? const ZEmptyState(
                            icon: Icons.inventory_2_outlined,
                            title: 'Sin productos',
                            subtitle: 'No se encontraron productos',
                          )
                        : LayoutBuilder(
                            builder: (_, gridConstraints) {
                              final crossCount = gridConstraints.maxWidth < 576 ? 2
                                  : gridConstraints.maxWidth < 992 ? 3 : 4;
                              return GridView.builder(
                                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                                gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                                  crossAxisCount: crossCount,
                                  mainAxisSpacing: 8,
                                  crossAxisSpacing: 8,
                                  childAspectRatio: crossCount <= 2 ? 1.7 : 1.5,
                                ),
                                itemCount: filtered.length,
                                itemBuilder: (_, i) {
                                  final p = filtered[i];
                                  final id = p['id'] ?? '';
                                  final name = p['name'] ?? '';
                                  final code = p['code'] ?? '';
                                  final price = (p['price'] ?? 0).toDouble();
                                  final stock = (p['stock'] ?? 0).toDouble();

                                  return _ProductTile(
                                    name: name,
                                    code: code,
                                    price: price,
                                    stock: stock,
                                    inCart: posState.items.any((item) => item.productId == id),
                                    onTap: () {
                                      final ok = ref.read(posProvider.notifier).addItem(
                                        PosCartItem(
                                          productId: id,
                                          name: name,
                                          code: code,
                                          price: price,
                                          stock: stock,
                                          taxRate: (p['taxRate'] ?? 0).toDouble(),
                                        ),
                                      );
                                      if (ok) HapticFeedback.lightImpact();
                                    },
                                  );
                                },
                              );
                            },
                          ),
                  ),
                ],
              );
            },
          ),
        ),
      ],
    );
  }

  Widget _buildProductSkeletons(bool isDark) {
    return GridView.builder(
      padding: const EdgeInsets.all(12),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 4,
        mainAxisSpacing: 8,
        crossAxisSpacing: 8,
        childAspectRatio: 1.5,
      ),
      itemCount: 12,
      itemBuilder: (_, _) => ZSkeleton.card(height: 120),
    );
  }

  Widget _buildCartPanel(PosState posState, bool isDark) {
    return Column(
      children: [
        // Cart header
        Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            border: Border(bottom: BorderSide(color: isDark ? ZColors.darkBorder : ZColors.border)),
          ),
          child: Row(
            children: [
              const Icon(Icons.shopping_cart_outlined, size: 20),
              const SizedBox(width: 8),
              Text(
                'Carrito (${posState.itemCount})',
                style: ZTypography.titleMedium.copyWith(fontWeight: FontWeight.w600),
              ),
              const Spacer(),
              if (posState.items.isNotEmpty)
                GestureDetector(
                  onTap: () => ref.read(posProvider.notifier).clearCart(),
                  child: Text('Vaciar', style: ZTypography.labelSmall.copyWith(color: ZColors.danger)),
                ),
            ],
          ),
        ),

        // Client selector
        _ClientSelector(
          clientName: posState.clientName,
          onSelected: (id, name) => ref.read(posProvider.notifier).setClient(id, name),
        ),

        // Notes
        _NotesField(
          notes: posState.notes,
          onChanged: (v) => ref.read(posProvider.notifier).setNotes(v),
        ),

        // Cart items
        Expanded(
          child: posState.items.isEmpty
              ? const ZEmptyState(
                  icon: Icons.shopping_cart_outlined,
                  title: 'Carrito vacío',
                  subtitle: 'Selecciona productos para agregar',
                )
              : ListView.builder(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                  itemCount: posState.items.length,
                  itemBuilder: (_, i) {
                    final item = posState.items[i];
                    return _CartItemTile(
                      name: item.name,
                      code: item.code,
                      price: item.price,
                      quantity: item.quantity,
                      discount: item.discount,
                      lineTotal: item.subtotal,
                      onQuantityChanged: (qty) {
                        final ok = ref.read(posProvider.notifier).updateQuantity(item.productId, qty);
                        if (!ok && context.mounted) {
                          final err = ref.read(posProvider).error;
                          ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                            content: Text(err ?? 'Stock insuficiente'),
                            backgroundColor: ZColors.danger,
                          ));
                        }
                      },
                      onDiscountChanged: (d) {
                        ref.read(posProvider.notifier).setItemDiscount(item.productId, d);
                      },
                      onRemove: () {
                        ref.read(posProvider.notifier).removeItem(item.productId);
                      },
                    );
                  },
                ),
        ),

        // Cart summary + submit
        Container(
          decoration: BoxDecoration(
            border: Border(top: BorderSide(color: isDark ? ZColors.darkBorder : ZColors.border)),
          ),
          child: _CartSummary(posState: posState),
        ),
      ],
    );
  }

  Widget _buildMobileCartBar(PosState posState, bool isDark) {
    return GestureDetector(
      onTap: () {
        if (posState.items.isNotEmpty) _showMobileCartSheet();
      },
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: BoxDecoration(
          color: isDark ? ZColors.darkSurface : ZColors.surface,
          border: Border(top: BorderSide(color: isDark ? ZColors.darkBorder : ZColors.border)),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.05),
              blurRadius: 8,
              offset: const Offset(0, -2),
            ),
          ],
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
                      child: Text('${posState.itemCount}',
                          style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.w700)),
                    ),
                  ),
              ],
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                posState.items.isEmpty ? 'Busca y agrega productos' : '${posState.itemCount} producto${posState.itemCount == 1 ? '' : 's'}',
                style: ZTypography.bodyMedium.copyWith(
                  color: isDark ? ZColors.neutral300 : ZColors.neutral600,
                ),
              ),
            ),
            if (posState.items.isNotEmpty)
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: ZColors.success.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(ZRadii.md),
                ),
                child: Text(
                  '\$${posState.total.toStringAsFixed(2)}',
                  style: ZTypography.titleSmall.copyWith(
                    fontWeight: FontWeight.w700,
                    color: ZColors.success,
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  void _showMobileCartSheet() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => DraggableScrollableSheet(
        initialChildSize: 0.75,
        minChildSize: 0.4,
        maxChildSize: 0.95,
        expand: false,
        builder: (ctx, controller) {
          final isDark = Theme.of(ctx).brightness == Brightness.dark;
          final posState = ref.read(posProvider);
          return Column(
            children: [
              Container(
                margin: const EdgeInsets.only(top: 10),
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: isDark ? ZColors.neutral600 : ZColors.neutral300,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              Expanded(
                child: _buildCartPanel(posState, isDark),
              ),
            ],
          );
        },
      ),
    );
  }

}

/// ── Category filter chip ──
class _CategoryChip extends StatelessWidget {
  final String label;
  final bool selected;
  final VoidCallback onTap;

  const _CategoryChip({required this.label, required this.selected, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Padding(
      padding: const EdgeInsets.only(right: 6),
      child: GestureDetector(
        onTap: onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
          decoration: BoxDecoration(
            color: selected
                ? (isDark ? ZColors.brandAccent : ZColors.brandPrimary)
                : (isDark ? ZColors.neutral800 : ZColors.neutral100),
            borderRadius: BorderRadius.circular(20),
            border: Border.all(
              color: selected
                  ? (isDark ? ZColors.brandAccent : ZColors.brandPrimary)
                  : (isDark ? ZColors.darkBorder : ZColors.border),
            ),
          ),
          child: Text(
            label,
            style: ZTypography.labelSmall.copyWith(
              color: selected ? Colors.white : (isDark ? ZColors.neutral300 : ZColors.neutral600),
              fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
              fontSize: 11,
            ),
          ),
        ),
      ),
    );
  }
}

/// ── Client selector ──
class _ClientSelector extends ConsumerStatefulWidget {
  final String? clientName;
  final void Function(String? id, String? name) onSelected;

  const _ClientSelector({this.clientName, required this.onSelected});

  @override
  ConsumerState<_ClientSelector> createState() => _ClientSelectorState();
}

class _ClientSelectorState extends ConsumerState<_ClientSelector> {
  final _clientCtrl = TextEditingController();
  bool _showResults = false;

  @override
  void dispose() {
    _clientCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final clientsAsync = ref.watch(posClientsProvider);

    return Container(
      padding: const EdgeInsets.fromLTRB(12, 8, 12, 4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          if (widget.clientName != null)
            Padding(
              padding: const EdgeInsets.only(bottom: 6),
              child: Row(
                children: [
                  Icon(Icons.person_outline, size: 14, color: ZColors.success),
                  const SizedBox(width: 4),
                  Expanded(
                    child: Text(
                      widget.clientName!,
                      style: ZTypography.labelSmall.copyWith(
                        color: ZColors.success,
                        fontWeight: FontWeight.w600,
                      ),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  GestureDetector(
                    onTap: () {
                      _clientCtrl.clear();
                      widget.onSelected(null, null);
                    },
                    child: Icon(Icons.close, size: 14, color: isDark ? ZColors.neutral400 : ZColors.neutral500),
                  ),
                ],
              ),
            ),
          TextField(
            controller: _clientCtrl,
            decoration: InputDecoration(
              hintText: widget.clientName != null ? 'Cambiar cliente...' : 'Buscar cliente (opcional)...',
              prefixIcon: Icon(Icons.person_search_outlined, size: 18),
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
              contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              isDense: true,
              filled: true,
              fillColor: isDark ? ZColors.neutral800.withValues(alpha: 0.3) : ZColors.neutral50,
            ),
            style: ZTypography.bodySmall,
            onChanged: (v) => setState(() => _showResults = v.isNotEmpty),
          ),
          if (_showResults && _clientCtrl.text.isNotEmpty)
            clientsAsync.when(
              loading: () => const Padding(
                padding: EdgeInsets.only(top: 4),
                child: SizedBox(height: 20, child: Center(child: CircularProgressIndicator(strokeWidth: 2))),
              ),
              error: (_, _) => const SizedBox.shrink(),
              data: (clients) {
                final q = _clientCtrl.text.toLowerCase();
                final results = clients.where((c) {
                  final name = (c['name'] ?? '').toString().toLowerCase();
                  final doc = (c['documentId'] ?? '').toString().toLowerCase();
                  return name.contains(q) || doc.contains(q);
                }).take(5).toList();

                if (results.isEmpty) return const SizedBox.shrink();

                return Container(
                  margin: const EdgeInsets.only(top: 4),
                  decoration: BoxDecoration(
                    color: isDark ? ZColors.darkSurface : ZColors.surface,
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: isDark ? ZColors.darkBorder : ZColors.border),
                  ),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: results.map((c) {
                      final name = c['name'] ?? '';
                      final doc = c['documentId'] ?? '';
                      return ListTile(
                        dense: true,
                        title: Text(name, style: ZTypography.bodySmall.copyWith(fontWeight: FontWeight.w600)),
                        subtitle: doc.isNotEmpty
                            ? Text(doc, style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500))
                            : null,
                        leading: const Icon(Icons.person_outline, size: 18),
                        onTap: () {
                          _clientCtrl.text = name;
                          widget.onSelected(c['id'], name);
                          setState(() => _showResults = false);
                        },
                      );
                    }).toList(),
                  ),
                );
              },
            ),
        ],
      ),
    );
  }
}

/// ── Notes field ──
class _NotesField extends ConsumerStatefulWidget {
  final String? notes;
  final ValueChanged<String?> onChanged;

  const _NotesField({this.notes, required this.onChanged});

  @override
  ConsumerState<_NotesField> createState() => _NotesFieldState();
}

class _NotesFieldState extends ConsumerState<_NotesField> {
  late final TextEditingController _ctrl;

  @override
  void initState() {
    super.initState();
    _ctrl = TextEditingController(text: widget.notes);
  }

  @override
  void didUpdateWidget(covariant _NotesField oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.notes != oldWidget.notes && widget.notes != _ctrl.text) {
      _ctrl.text = widget.notes ?? '';
    }
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 4, 12, 4),
      child: TextField(
        controller: _ctrl,
        decoration: InputDecoration(
          hintText: 'Notas (opcional)...',
          prefixIcon: Icon(Icons.notes_outlined, size: 18),
          border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
          contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
          isDense: true,
          filled: true,
          fillColor: isDark ? ZColors.neutral800.withValues(alpha: 0.3) : ZColors.neutral50,
        ),
        style: ZTypography.bodySmall,
        maxLines: 1,
        onChanged: (v) => widget.onChanged(v.isEmpty ? null : v),
      ),
    );
  }
}

/// ── Period status banner ──
class _PeriodBanner extends ConsumerStatefulWidget {
  final AsyncValue<Map<String, dynamic>?> periodAsync;

  const _PeriodBanner({required this.periodAsync});

  @override
  ConsumerState<_PeriodBanner> createState() => _PeriodBannerState();
}

class _PeriodBannerState extends ConsumerState<_PeriodBanner> {
  bool _opening = false;

  Future<void> _openCurrentPeriod() async {
    setState(() => _opening = true);
    try {
      final dio = ref.read(dioClientProvider);
      final now = DateTime.now();
      await dio.post('accounting-periods', data: {
        'year': now.year,
        'month': now.month,
      });
      ref.invalidate(posCurrentPeriodProvider);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Período contable abierto exitosamente'),
          backgroundColor: ZColors.success,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Error al abrir período: $e'),
          backgroundColor: ZColors.danger,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } finally {
      if (mounted) setState(() => _opening = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return widget.periodAsync.when(
      loading: () => const SizedBox(height: 2),
      error: (_, _) => const SizedBox(height: 2),
      data: (period) {
        if (period == null || period['status'] != 'open') {
          return Container(
            width: double.infinity,
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            color: ZColors.warning.withValues(alpha: 0.15),
            child: Row(
              children: [
                Icon(Icons.lock_outline, size: 14, color: ZColors.warning),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(
                    period == null
                        ? 'No hay período contable abierto'
                        : 'Período contable cerrado',
                    style: ZTypography.labelSmall.copyWith(color: ZColors.warning, fontWeight: FontWeight.w600),
                  ),
                ),
                const SizedBox(width: 8),
                SizedBox(
                  height: 26,
                  child: TextButton.icon(
                    onPressed: _opening ? null : _openCurrentPeriod,
                    icon: _opening
                        ? const SizedBox(width: 12, height: 12, child: CircularProgressIndicator(strokeWidth: 1.5, color: Colors.white))
                        : const Icon(Icons.add_circle_outline, size: 14),
                    label: Text(_opening ? 'Abriendo...' : 'Abrir período', style: const TextStyle(fontSize: 11)),
                    style: TextButton.styleFrom(
                      foregroundColor: Colors.white,
                      backgroundColor: ZColors.warning,
                      padding: const EdgeInsets.symmetric(horizontal: 8),
                      minimumSize: Size.zero,
                      tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                    ),
                  ),
                ),
              ],
            ),
          );
        }
        if (period['status'] == 'open') {
          return Container(
            width: double.infinity,
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            color: ZColors.success.withValues(alpha: 0.08),
            child: Row(
              children: [
                Icon(Icons.check_circle_outline, size: 12, color: ZColors.success),
                const SizedBox(width: 6),
                Text(
                  'Período ${period['name'] ?? '${period['month']}/${period['year']}'} abierto',
                  style: ZTypography.labelSmall.copyWith(color: ZColors.success, fontWeight: FontWeight.w500, fontSize: 11),
                ),
              ],
            ),
          );
        }
        return const SizedBox(height: 2);
      },
    );
  }
}

/// ── Product tile ──
class _ProductTile extends StatelessWidget {
  final String name;
  final String code;
  final double price;
  final double stock;
  final bool inCart;
  final VoidCallback onTap;

  const _ProductTile({
    required this.name,
    required this.code,
    required this.price,
    required this.stock,
    this.inCart = false,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final outOfStock = stock <= 0;
    final lowStock = stock > 0 && stock <= 5;

    return GestureDetector(
      onTap: outOfStock ? null : onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: isDark ? ZColors.darkSurface : ZColors.surface,
          borderRadius: BorderRadius.circular(ZRadii.md),
          border: Border.all(
            color: inCart ? ZColors.success : (isDark ? ZColors.darkBorder : ZColors.border),
            width: inCart ? 1.5 : 0.5,
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Name
            Text(
              name,
              style: ZTypography.bodySmall.copyWith(
                fontWeight: FontWeight.w600,
                color: isDark ? ZColors.neutral100 : ZColors.neutral900,
              ),
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
            ),
            const SizedBox(height: 2),
            // Code
            Text(
              code,
              style: ZTypography.labelSmall.copyWith(
                color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                fontSize: 9,
              ),
            ),
            const Spacer(),
            // Price
            Text(
              '\$${price.toStringAsFixed(2)}',
              style: ZTypography.titleSmall.copyWith(
                fontWeight: FontWeight.w800,
                color: inCart ? ZColors.success : (isDark ? ZColors.brandAccent : ZColors.brandPrimary),
              ),
            ),
            const SizedBox(height: 2),
            // Stock indicator
            if (outOfStock)
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
                decoration: BoxDecoration(
                  color: ZColors.danger.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text('Agotado', style: TextStyle(fontSize: 9, color: ZColors.danger, fontWeight: FontWeight.w600)),
              )
            else
              Row(
                children: [
                  Expanded(
                    child: LinearProgressIndicator(
                      value: (stock / 20).clamp(0.0, 1.0),
                      backgroundColor: (isDark ? ZColors.neutral700 : ZColors.neutral200).withValues(alpha: 0.5),
                      color: lowStock ? ZColors.warning : ZColors.success,
                      minHeight: 3,
                      borderRadius: BorderRadius.circular(2),
                    ),
                  ),
                  const SizedBox(width: 4),
                  Text(
                    stock.toStringAsFixed(0),
                    style: ZTypography.labelSmall.copyWith(
                      color: lowStock ? ZColors.warning : (isDark ? ZColors.neutral400 : ZColors.neutral500),
                      fontSize: 9,
                    ),
                  ),
                ],
              ),
            // In-cart badge
            if (inCart)
              Positioned(
                top: 4,
                right: 4,
                child: Container(
                  padding: const EdgeInsets.all(3),
                  decoration: const BoxDecoration(color: ZColors.success, shape: BoxShape.circle),
                  child: const Icon(Icons.check, size: 10, color: Colors.white),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

/// ── Cart item tile ──
class _CartItemTile extends StatelessWidget {
  final String name;
  final String code;
  final double price;
  final double quantity;
  final double discount;
  final double lineTotal;
  final ValueChanged<double> onQuantityChanged;
  final ValueChanged<double> onDiscountChanged;
  final VoidCallback onRemove;

  const _CartItemTile({
    required this.name,
    required this.code,
    required this.price,
    required this.quantity,
    this.discount = 0,
    required this.lineTotal,
    required this.onQuantityChanged,
    required this.onDiscountChanged,
    required this.onRemove,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final hasDiscount = discount > 0;

    return Container(
      margin: const EdgeInsets.symmetric(vertical: 4),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: isDark ? ZColors.neutral800.withValues(alpha: 0.3) : ZColors.neutral50,
        borderRadius: BorderRadius.circular(ZRadii.md),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Name + remove
          Row(
            children: [
              Expanded(
                child: Text(
                  name,
                  style: ZTypography.bodySmall.copyWith(fontWeight: FontWeight.w600),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              // Discount badge
              if (hasDiscount)
                Container(
                  margin: const EdgeInsets.only(right: 6),
                  padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
                  decoration: BoxDecoration(
                    color: ZColors.danger.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(4),
                  ),
                  child: Text(
                    '-${discount.toStringAsFixed(2)}',
                    style: TextStyle(fontSize: 9, color: ZColors.danger, fontWeight: FontWeight.w700),
                  ),
                ),
              GestureDetector(
                onTap: onRemove,
                child: Icon(Icons.close, size: 16, color: isDark ? ZColors.neutral500 : ZColors.neutral400),
              ),
            ],
          ),
          const SizedBox(height: 4),
          // Price, quantity, line total
          Row(
            children: [
              Text(
                '\$${price.toStringAsFixed(2)}',
                style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500),
              ),
              const Spacer(),
              // Quantity stepper
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  InkWell(
                    borderRadius: BorderRadius.circular(12),
                    onTap: () => onQuantityChanged(quantity - 1),
                    child: Container(
                      padding: const EdgeInsets.all(2),
                      decoration: BoxDecoration(
                        border: Border.all(color: isDark ? ZColors.darkBorder : ZColors.border),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: const Icon(Icons.remove, size: 14),
                    ),
                  ),
                  SizedBox(
                    width: 36,
                    child: Text(
                      quantity.toStringAsFixed(quantity == quantity.roundToDouble() ? 0 : 1),
                      textAlign: TextAlign.center,
                      style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w700),
                    ),
                  ),
                  InkWell(
                    borderRadius: BorderRadius.circular(12),
                    onTap: () => onQuantityChanged(quantity + 1),
                    child: Container(
                      padding: const EdgeInsets.all(2),
                      decoration: BoxDecoration(
                        color: ZColors.success.withValues(alpha: 0.1),
                        border: Border.all(color: ZColors.success.withValues(alpha: 0.3)),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: const Icon(Icons.add, size: 14, color: ZColors.success),
                    ),
                  ),
                ],
              ),
              const SizedBox(width: 8),
              Text(
                hasDiscount
                    ? '\$${lineTotal.toStringAsFixed(2)}'
                    : '\$${lineTotal.toStringAsFixed(2)}',
                style: ZTypography.bodySmall.copyWith(
                    fontWeight: FontWeight.w700,
                    color: hasDiscount ? ZColors.danger : null),
              ),
            ],
          ),
          // Discount input
          const SizedBox(height: 4),
          Row(
            children: [
              Icon(Icons.discount_outlined, size: 12, color: isDark ? ZColors.neutral500 : ZColors.neutral400),
              const SizedBox(width: 4),
              SizedBox(
                width: 70,
                height: 22,
                child: TextField(
                  decoration: InputDecoration(
                    hintText: 'Dto',
                    prefixText: '\$ ',
                    border: OutlineInputBorder(borderRadius: BorderRadius.circular(6)),
                    contentPadding: const EdgeInsets.symmetric(horizontal: 4, vertical: 0),
                    isDense: true,
                  ),
                  style: TextStyle(fontSize: 10, color: hasDiscount ? ZColors.danger : null),
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  onChanged: (v) => onDiscountChanged(double.tryParse(v) ?? 0),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

/// ── Cash register selector ──
class _CashRegisterSelector extends ConsumerWidget {
  final String? cashRegisterId;
  final String? cashRegisterName;
  final ValueChanged<String?> onIdChanged;
  final ValueChanged<String?> onNameChanged;

  const _CashRegisterSelector({
    this.cashRegisterId,
    this.cashRegisterName,
    required this.onIdChanged,
    required this.onNameChanged,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final registersAsync = ref.watch(posCashRegistersProvider);

    return registersAsync.when(
      loading: () => const SizedBox.shrink(),
      error: (_, _) => const SizedBox.shrink(),
      data: (registers) {
        if (registers.isEmpty) return const SizedBox.shrink();
        return Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: Row(
            children: [
              Icon(Icons.point_of_sale_outlined, size: 16, color: isDark ? ZColors.neutral400 : ZColors.neutral500),
              const SizedBox(width: 6),
              Text('Caja:', style: ZTypography.labelSmall.copyWith(color: isDark ? ZColors.neutral400 : ZColors.neutral500)),
              const SizedBox(width: 6),
              Expanded(
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  decoration: BoxDecoration(
                    color: isDark ? ZColors.neutral800.withValues(alpha: 0.3) : ZColors.neutral50,
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: isDark ? ZColors.darkBorder : ZColors.border),
                  ),
                  child: DropdownButtonHideUnderline(
                    child: DropdownButton<String>(
                      value: cashRegisterId,
                      isExpanded: true,
                      hint: Text('Seleccionar', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
                      items: registers.map((r) {
                        final id = r['id']?.toString() ?? '';
                        final name = r['code']?.toString() ?? r['name']?.toString() ?? 'Caja';
                        return DropdownMenuItem(value: id, child: Text(name, style: ZTypography.labelSmall));
                      }).toList(),
                      onChanged: (id) {
                        final r = registers.firstWhere((r) => r['id']?.toString() == id, orElse: () => {});
                        onIdChanged(id);
                        onNameChanged(r['code']?.toString() ?? r['name']?.toString() ?? 'Caja');
                      },
                    ),
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

/// ── Cart summary + payment + submit ──
class _CartSummary extends ConsumerWidget {
  final PosState posState;

  const _CartSummary({required this.posState});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final isCredit = posState.paymentMethod == 'credit';
    final needsReference = posState.paymentMethod == 'card' || posState.paymentMethod == 'transfer';
    final periodAsync = ref.watch(posCurrentPeriodProvider);
    final period = periodAsync.asData?.value;
    final periodClosed = period == null || period['status'] != 'open';

    return Container(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          // Cash register selector
          _CashRegisterSelector(
            cashRegisterId: posState.cashRegisterId,
            cashRegisterName: posState.cashRegisterName,
            onIdChanged: (id) => ref.read(posProvider.notifier).setCashRegister(id, posState.cashRegisterName),
            onNameChanged: (name) => ref.read(posProvider.notifier).setCashRegister(posState.cashRegisterId, name),
          ),

          // Payment reference (for card/transfer)
          if (needsReference)
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: TextField(
                decoration: InputDecoration(
                  hintText: 'N° de referencia',
                  prefixIcon: Icon(Icons.receipt_outlined, size: 18),
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                  contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                  isDense: true,
                  filled: true,
                  fillColor: isDark ? ZColors.neutral800.withValues(alpha: 0.3) : ZColors.neutral50,
                ),
                style: ZTypography.bodySmall,
                onChanged: (v) => ref.read(posProvider.notifier).setPaymentReference(v.isEmpty ? null : v),
              ),
            ),

          // Credit fields
          if (isCredit) ...[
            // Down payment
            Row(
              children: [
                Expanded(
                  child: TextField(
                    decoration: InputDecoration(
                      labelText: 'Pago inicial',
                      prefixText: '\$ ',
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                      contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      isDense: true,
                    ),
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    style: ZTypography.bodySmall,
                    onChanged: (v) => ref.read(posProvider.notifier).setDownPayment(double.tryParse(v) ?? 0),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: TextField(
                    decoration: InputDecoration(
                      labelText: 'Cuotas',
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                      contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      isDense: true,
                    ),
                    keyboardType: TextInputType.number,
                    style: ZTypography.bodySmall,
                    onChanged: (v) => ref.read(posProvider.notifier).setInstallmentCount(int.tryParse(v) ?? 1),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: TextField(
                    decoration: InputDecoration(
                      labelText: 'Interés %',
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                      contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      isDense: true,
                    ),
                    keyboardType: const TextInputType.numberWithOptions(decimal: true),
                    style: ZTypography.bodySmall,
                    onChanged: (v) => ref.read(posProvider.notifier).setInterestRate(double.tryParse(v) ?? 0),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
          ],

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
          const SizedBox(height: 4),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Text('IVA estimado', style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500)),
                  const SizedBox(width: 4),
                  Icon(Icons.info_outline, size: 12, color: ZColors.neutral400),
                ],
              ),
              Text('\$${posState.estimatedTax.toStringAsFixed(2)}', style: ZTypography.bodySmall),
            ],
          ),
          const Divider(height: 12),
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
          const SizedBox(height: 10),
          // Payment method
          _PaymentMethodSelector(
            selected: posState.paymentMethod,
            onChanged: (m) => ref.read(posProvider.notifier).setPaymentMethod(m),
          ),
          const SizedBox(height: 10),
          // Submit
          SizedBox(
            width: double.infinity,
            child: ZButton(
              text: posState.loading
                  ? 'Procesando...'
                  : periodClosed
                      ? 'Período contable cerrado'
                      : 'Cobrar \$${posState.total.toStringAsFixed(2)}',
              icon: posState.loading ? null : (periodClosed ? Icons.lock_outline : Icons.payment),
              isLoading: posState.loading,
              onPressed: posState.items.isEmpty || posState.loading || periodClosed
                  ? () {}
                  : () {
                      ref.read(posProvider.notifier).submitSale().then((success) {
                        if (!success) {
                          if (!context.mounted) return;
                          final err = ref.read(posProvider).error;
                          ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                            content: Text(err ?? 'Error al procesar venta'),
                            backgroundColor: ZColors.danger,
                          ));
                          return;
                        }
                        if (!context.mounted) return;
                        final state = ref.read(posProvider);
                        final saleData = state.lastSaleResponse;
                        final saleId = saleData?['id']?.toString() ?? '';
                        final invoiceNo = saleData?['invoiceNumber']?.toString() ?? '';
                        final saleDate = saleData?['saleDate']?.toString() ?? '';
                        final clientName = saleData?['clientName']?.toString() ?? '';
                        final subtotal = (saleData?['subtotal'] as num?)?.toDouble() ?? 0;
                        final tax = (saleData?['tax'] as num?)?.toDouble() ?? 0;
                        final total = (saleData?['total'] as num?)?.toDouble() ?? 0;
                        final discount = (saleData?['discount'] as num?)?.toDouble() ?? 0;
                        final paid = (saleData?['paidAmount'] as num?)?.toDouble() ?? 0;
                        final balance = (saleData?['balance'] as num?)?.toDouble() ?? 0;
                        final status = (saleData?['status'] ?? '').toString();
                        final notes = saleData?['notes'] as String?;
                        final items = (saleData?['details'] as List?)?.cast<Map<String, dynamic>>() ?? <Map<String, dynamic>>[];

                        showModalBottomSheet(
                          context: context,
                          shape: const RoundedRectangleBorder(
                            borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
                          ),
                          builder: (ctx) => Padding(
                            padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
                            child: Column(
                              mainAxisSize: MainAxisSize.min,
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Center(child: Container(
                                  width: 40, height: 4,
                                  decoration: BoxDecoration(color: Colors.grey.shade300, borderRadius: BorderRadius.circular(2)),
                                )),
                                const SizedBox(height: 16),
                                Row(
                                  children: [
                                    const Icon(Icons.check_circle, color: ZColors.success, size: 24),
                                    const SizedBox(width: 8),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment: CrossAxisAlignment.start,
                                        children: [
                                          Text('Venta registrada', style: ZTypography.titleMedium.copyWith(fontWeight: FontWeight.w700)),
                                          if (invoiceNo.isNotEmpty)
                                            Text('Factura #$invoiceNo', style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500)),
                                        ],
                                      ),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 20),
                                SizedBox(
                                  width: double.infinity,
                                  child: ZButton(
                                    text: 'Imprimir / Compartir ticket',
                                    icon: Icons.receipt_outlined,
                                    onPressed: saleData == null
                                        ? () {}
                                        : () {
                                            Navigator.pop(ctx);
                                            showModalBottomSheet(
                                              context: context,
                                              isScrollControlled: true,
                                              builder: (_) => PrintShareSheet(
                                                title: 'Ticket de Venta',
                                                filename: 'venta_$invoiceNo',
                                                buildPdf: (company, settings) => generateSalePdf(
                                                  sale: SaleDetail.fromJson(saleData),
                                                  company: company,
                                                  settings: settings,
                                                ),
                                                  buildThermal: (company) => saleThermalHtml(
                                                  company: company,
                                                  invoiceNumber: invoiceNo,
                                                  date: saleDate,
                                                  clientName: clientName,
                                                  saleType: state.paymentMethod,
                                                  subtotal: subtotal,
                                                  discount: discount,
                                                  tax: tax,
                                                  total: total,
                                                  paidAmount: paid,
                                                  balance: balance,
                                                  status: status,
                                                  notes: notes,
                                                  items: items,
                                                ),
                                                buildText: (company) => saleTextSummary(
                                                  companyName: (company['legalName'] ?? company['name'] ?? '').toString(),
                                                  invoiceNumber: invoiceNo,
                                                  date: saleDate,
                                                  clientName: clientName,
                                                  total: total,
                                                  status: status,
                                                ),
                                              ),
                                            );
                                          },
                                  ),
                                ),
                                if (saleId.isNotEmpty) ...[
                                  const SizedBox(height: 8),
                                  SizedBox(
                                    width: double.infinity,
                                    child: ZButton(
                                      text: 'Nota de Crédito',
                                      icon: Icons.assignment_return_outlined,
                                      type: ZButtonType.secondary,
                                      onPressed: () {
                                        Navigator.pop(ctx);
                                        context.push('/credit-notes/new/$saleId');
                                      },
                                    ),
                                  ),
                                ],
                                const SizedBox(height: 8),
                                SizedBox(
                                  width: double.infinity,
                                  child: ZButton(
                                    text: 'Nueva Venta',
                                    icon: Icons.add_shopping_cart_outlined,
                                    type: ZButtonType.secondary,
                                    onPressed: () {
                                      ref.read(posProvider.notifier).clearCart();
                                      ref.read(posProvider.notifier).clearLastSaleResponse();
                                      Navigator.pop(ctx);
                                    },
                                  ),
                                ),
                                const SizedBox(height: 8),
                                SizedBox(
                                  width: double.infinity,
                                  child: ZButton(
                                    text: 'Salir del POS',
                                    icon: Icons.exit_to_app_outlined,
                                    type: ZButtonType.ghost,
                                    onPressed: () {
                                      ref.read(posProvider.notifier).clearLastSaleResponse();
                                      Navigator.pop(ctx);
                                      context.pop();
                                    },
                                  ),
                                ),
                              ],
                            ),
                          ),
                        );
                      });
                    },
              type: periodClosed ? ZButtonType.secondary : ZButtonType.primary,
            ),
          ),
        ],
      ),
    );
  }
}

/// ── Payment method selector ──
class _PaymentMethodSelector extends ConsumerWidget {
  final String selected;
  final ValueChanged<String> onChanged;

  const _PaymentMethodSelector({required this.selected, required this.onChanged});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
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
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 3),
            child: GestureDetector(
              onTap: () => onChanged(m.$1),
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 200),
                padding: const EdgeInsets.symmetric(vertical: 8),
                decoration: BoxDecoration(
                  color: isSelected ? (isDark ? ZColors.brandPrimary.withValues(alpha: 0.2) : ZColors.brandPrimary.withValues(alpha: 0.1)) : Colors.transparent,
                  borderRadius: BorderRadius.circular(ZRadii.sm),
                  border: Border.all(
                    color: isSelected ? ZColors.brandPrimary : (isDark ? ZColors.darkBorder : ZColors.border),
                    width: isSelected ? 1.5 : 0.5,
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
                        fontWeight: isSelected ? FontWeight.w600 : FontWeight.normal,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        );
      }).toList(),
    );
  }
}
