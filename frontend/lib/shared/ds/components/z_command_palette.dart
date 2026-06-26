import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';
import '../ds.dart';

/// Command Palette (Cmd+K) — Global search and quick actions
class ZCommandPalette extends StatefulWidget {
  final List<ZCommandItem> items;
  final String? hintText;

  const ZCommandPalette({
    super.key,
    required this.items,
    this.hintText,
  });

  static Future<void> show(BuildContext context, {List<ZCommandItem>? items}) {
    final commands = items ?? _defaultCommands();
    return showGeneralDialog(
      context: context,
      barrierDismissible: true,
      barrierLabel: 'Command Palette',
      barrierColor: Colors.black54,
      transitionDuration: const Duration(milliseconds: 200),
      pageBuilder: (_, _, _) => Center(
        child: ZCommandPalette(items: commands),
      ),
      transitionBuilder: (_, anim, _, child) {
        return FadeTransition(
          opacity: CurvedAnimation(parent: anim, curve: Curves.easeOut),
          child: ScaleTransition(
            scale: CurvedAnimation(parent: anim, curve: Curves.easeOutCubic),
            child: child,
          ),
        );
      },
    );
  }

  static List<ZCommandItem> _defaultCommands() => [
    const ZCommandItem(
      label: 'Dashboard',
      icon: Icons.dashboard,
      route: '/dashboard',
      category: 'Navegación',
    ),
    const ZCommandItem(
      label: 'Trabajadores',
      icon: Icons.badge,
      route: '/employees',
      category: 'RRHH',
    ),
    const ZCommandItem(
      label: 'Nómina',
      icon: Icons.receipt_long,
      route: '/payroll',
      category: 'RRHH',
    ),
    const ZCommandItem(
      label: 'Asistencia',
      icon: Icons.schedule,
      route: '/attendance',
      category: 'RRHH',
    ),
    const ZCommandItem(
      label: 'Vacaciones',
      icon: Icons.beach_access,
      route: '/vacations',
      category: 'RRHH',
    ),
    const ZCommandItem(
      label: 'Clientes',
      icon: Icons.people,
      route: '/clients',
      category: 'Comercial',
    ),
    const ZCommandItem(
      label: 'Ventas',
      icon: Icons.point_of_sale,
      route: '/sales',
      category: 'Comercial',
    ),
    const ZCommandItem(
      label: 'Cotizaciones',
      icon: Icons.description,
      route: '/quotes',
      category: 'Comercial',
    ),
    const ZCommandItem(
      label: 'Productos',
      icon: Icons.inventory_2,
      route: '/products',
      category: 'Inventario',
    ),
    const ZCommandItem(
      label: 'Compras',
      icon: Icons.shopping_bag,
      route: '/purchases',
      category: 'Inventario',
    ),
    const ZCommandItem(
      label: 'Proveedores',
      icon: Icons.local_shipping,
      route: '/suppliers',
      category: 'Inventario',
    ),
    const ZCommandItem(
      label: 'Créditos',
      icon: Icons.credit_card,
      route: '/credits',
      category: 'Finanzas',
    ),
    const ZCommandItem(
      label: 'Caja',
      icon: Icons.account_balance,
      route: '/cash-registers',
      category: 'Finanzas',
    ),
    const ZCommandItem(
      label: 'Contabilidad',
      icon: Icons.account_balance_wallet,
      route: '/cost-centers',
      category: 'Finanzas',
    ),
    const ZCommandItem(
      label: 'Garantías',
      icon: Icons.verified,
      route: '/warranties',
      category: 'Servicio',
    ),
    const ZCommandItem(
      label: 'Reportes',
      icon: Icons.assessment,
      route: '/reports',
      category: 'Administración',
    ),
    const ZCommandItem(
      label: 'Auditoría',
      icon: Icons.history,
      route: '/audit-logs',
      category: 'Administración',
    ),
    const ZCommandItem(
      label: 'Configuración',
      icon: Icons.settings,
      route: '/settings',
      category: 'Administración',
    ),
    const ZCommandItem(
      label: 'Asistente IA',
      icon: Icons.smart_toy,
      route: '/chat',
      category: 'IA',
    ),
  ];

  @override
  State<ZCommandPalette> createState() => _ZCommandPaletteState();
}

class _ZCommandPaletteState extends State<ZCommandPalette> {
  final _controller = TextEditingController();
  final _focusNode = FocusNode();
  int _selectedIndex = 0;
  List<ZCommandItem> _filtered = [];

  @override
  void initState() {
    super.initState();
    _filtered = widget.items;
    _focusNode.requestFocus();
  }

  @override
  void dispose() {
    _controller.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  void _filter(String query) {
    setState(() {
      _filtered = widget.items
          .where((item) =>
              item.label.toLowerCase().contains(query.toLowerCase()) ||
              (item.category?.toLowerCase().contains(query.toLowerCase()) ?? false))
          .toList();
      _selectedIndex = 0;
    });
  }

  void _select(ZCommandItem item) {
    Navigator.of(context).pop();
    context.go(item.route);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final maxH = MediaQuery.of(context).size.height * 0.6;

    return KeyboardListener(
      focusNode: FocusNode(),
      onKeyEvent: (event) {
        if (event is KeyDownEvent) {
          if (event.logicalKey == LogicalKeyboardKey.arrowDown) {
            setState(() => _selectedIndex = (_selectedIndex + 1).clamp(0, _filtered.length - 1));
          } else if (event.logicalKey == LogicalKeyboardKey.arrowUp) {
            setState(() => _selectedIndex = (_selectedIndex - 1).clamp(0, _filtered.length - 1));
          } else if (event.logicalKey == LogicalKeyboardKey.enter && _filtered.isNotEmpty) {
            _select(_filtered[_selectedIndex]);
          }
        }
      },
      child: Material(
        color: Colors.transparent,
        child: Center(
          child: Container(
            width: 560,
            constraints: BoxConstraints(maxHeight: maxH),
            margin: const EdgeInsets.symmetric(horizontal: 24),
            decoration: BoxDecoration(
              color: theme.colorScheme.surface,
              borderRadius: BorderRadius.circular(ZRadii.xl),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.2),
                  blurRadius: 24,
                  offset: const Offset(0, 8),
                ),
              ],
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                // Search input
                Padding(
                  padding: const EdgeInsets.all(ZSpacing.lg),
                  child: TextField(
                    controller: _controller,
                    focusNode: _focusNode,
                    onChanged: _filter,
                    style: theme.textTheme.bodyLarge,
                    decoration: InputDecoration(
                      hintText: widget.hintText ?? 'Buscar módulos, páginas...',
                      prefixIcon: const Icon(Icons.search, size: 20),
                      suffixIcon: KeyboardHint(text: 'ESC'),
                      border: InputBorder.none,
                      enabledBorder: InputBorder.none,
                      focusedBorder: InputBorder.none,
                      filled: false,
                    ),
                  ),
                ),
                const Divider(height: 1),
                // Results
                Flexible(
                  child: _filtered.isEmpty
                      ? const Padding(
                          padding: EdgeInsets.all(ZSpacing.xl),
                          child: Text('Sin resultados', style: TextStyle(color: ZColors.neutral400)),
                        )
                      : ListView.builder(
                          shrinkWrap: true,
                          padding: const EdgeInsets.symmetric(vertical: ZSpacing.sm),
                          itemCount: _filtered.length,
                          itemBuilder: (_, i) {
                            final item = _filtered[i];
                            final isSelected = i == _selectedIndex;
                            return ListTile(
                              leading: Icon(item.icon, size: 20, color: isSelected ? theme.colorScheme.primary : ZColors.neutral500),
                              title: Text(item.label, style: TextStyle(fontWeight: isSelected ? FontWeight.w600 : FontWeight.normal)),
                              subtitle: item.category != null ? Text(item.category!, style: const TextStyle(fontSize: 11, color: ZColors.neutral400)) : null,
                              selected: isSelected,
                              selectedTileColor: theme.colorScheme.primaryContainer.withValues(alpha: 0.3),
                              dense: true,
                              onTap: () => _select(item),
                            );
                          },
                        ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class KeyboardHint extends StatelessWidget {
  final String text;
  const KeyboardHint({super.key, required this.text});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      margin: const EdgeInsets.only(right: 8),
      decoration: BoxDecoration(
        color: ZColors.neutral100,
        borderRadius: BorderRadius.circular(4),
        border: Border.all(color: ZColors.neutral200),
      ),
      child: Text(text, style: const TextStyle(fontSize: 10, color: ZColors.neutral500, fontFamily: 'monospace')),
    );
  }
}

class ZCommandItem {
  final String label;
  final IconData icon;
  final String route;
  final String? category;

  const ZCommandItem({
    required this.label,
    required this.icon,
    required this.route,
    this.category,
  });
}