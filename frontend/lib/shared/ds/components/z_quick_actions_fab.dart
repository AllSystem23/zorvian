import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../ds.dart';

/// ZQuickActionsFAB — Floating Action Button with contextual quick actions.
/// Shows different actions depending on the current route context.
/// This is the SINGLE source of FAB actions — pages should NOT define their own.
class ZQuickActionsFAB extends StatelessWidget {
  final String currentRoute;
  final Map<String, VoidCallback> callbacks;

  const ZQuickActionsFAB({
    super.key,
    required this.currentRoute,
    this.callbacks = const {},
  });

  /// Returns the appropriate quick actions based on the current route
  List<_QuickAction> _getActions() {
    // Default actions available everywhere
    final defaults = [
      _QuickAction(
        icon: Icons.person_add_outlined,
        label: 'Nuevo Empleado',
        route: '/employees/new',
        color: ZColors.brandPrimary,
      ),
    ];

    // ── Sales & Quotes ──
    if (currentRoute.startsWith('/sales') ||
        currentRoute.startsWith('/quotes')) {
      return [
        _QuickAction(
          icon: Icons.add_shopping_cart_outlined,
          label: 'Nueva Venta',
          route: '/sales/new',
          color: ZColors.success,
        ),
        _QuickAction(
          icon: Icons.request_quote_outlined,
          label: 'Nueva Cotización',
          route: '/quotes/new',
          color: ZColors.info,
        ),
        ...defaults,
      ];
    }

    // ── Products, Categories, Brands, Inventory ──
    if (currentRoute.startsWith('/products') ||
        currentRoute.startsWith('/categories') ||
        currentRoute.startsWith('/brands') ||
        currentRoute.startsWith('/inventory')) {
      return [
        _QuickAction(
          icon: Icons.add_box_outlined,
          label: 'Nuevo Producto',
          route: '/products/new',
          color: ZColors.success,
        ),
        _QuickAction(
          icon: Icons.category_outlined,
          label: 'Nueva Categoría',
          route: '/categories',
          color: ZColors.warning,
        ),
      ];
    }

    // ── Purchases & Suppliers ──
    if (currentRoute.startsWith('/purchases') ||
        currentRoute.startsWith('/suppliers')) {
      return [
        _QuickAction(
          icon: Icons.add_shopping_cart_outlined,
          label: 'Nueva Compra',
          route: '/purchases/new',
          color: ZColors.success,
        ),
        _QuickAction(
          icon: Icons.local_shipping_outlined,
          label: 'Nuevo Proveedor',
          route: '/suppliers/new',
          color: ZColors.info,
        ),
      ];
    }

    // ── Clients & Credits ──
    if (currentRoute.startsWith('/clients') ||
        currentRoute.startsWith('/credits')) {
      return [
        _QuickAction(
          icon: Icons.person_add_outlined,
          label: 'Nuevo Cliente',
          route: '/clients/new',
          color: ZColors.success,
        ),
        _QuickAction(
          icon: Icons.credit_card_outlined,
          label: 'Nuevo Crédito',
          route: '/credits',
          color: ZColors.info,
        ),
      ];
    }

    // ── HR: Employees, Attendance, Payroll, Vacations, Departments, Permissions ──
    if (currentRoute.startsWith('/employees') ||
        currentRoute.startsWith('/attendance') ||
        currentRoute.startsWith('/payroll') ||
        currentRoute.startsWith('/vacations') ||
        currentRoute.startsWith('/departments') ||
        currentRoute.startsWith('/permissions')) {
      return [
        _QuickAction(
          icon: Icons.person_add_outlined,
          label: 'Nuevo Empleado',
          route: '/employees/new',
          color: ZColors.success,
        ),
        _QuickAction(
          icon: Icons.beach_access_outlined,
          label: 'Nueva Solicitud',
          route: '/vacations/new',
          color: ZColors.warning,
        ),
      ];
    }

    // ── Budgets & Cost Centers ──
    if (currentRoute.startsWith('/budgets') ||
        currentRoute.startsWith('/cost-centers')) {
      return [
        _QuickAction(
          icon: Icons.savings_outlined,
          label: 'Nuevo Presupuesto',
          route: '/budgets/new',
          color: ZColors.success,
        ),
        _QuickAction(
          icon: Icons.pie_chart_outline,
          label: 'Nuevo Centro de Costo',
          route: '/cost-centers/new',
          color: ZColors.info,
        ),
      ];
    }

    // ── Cash Registers ──
    if (currentRoute.startsWith('/cash-registers')) {
      return [
        _QuickAction(
          icon: Icons.monetization_on_outlined,
          label: 'Abrir Caja',
          route: '/cash-registers',
          color: ZColors.success,
          actionId: 'cash_open',
        ),
      ];
    }

    // ── Webhooks ──
    if (currentRoute.startsWith('/webhooks')) {
      return [
        _QuickAction(
          icon: Icons.webhook_outlined,
          label: 'Nuevo Webhook',
          route: '/webhooks/new',
          color: ZColors.success,
        ),
      ];
    }

    // ── Accounting ──
    if (currentRoute.startsWith('/accounting')) {
      return [
        _QuickAction(
          icon: Icons.account_balance_outlined,
          label: 'Nueva Cuenta',
          route: '/accounting/chart-of-accounts',
          color: ZColors.success,
        ),
      ];
    }

    // ── Approval Flows ──
    if (currentRoute.startsWith('/approval')) {
      return [
        _QuickAction(
          icon: Icons.approval_outlined,
          label: 'Nuevo Flujo',
          route: '/approval-flows/new',
          color: ZColors.success,
        ),
      ];
    }

    // ── Branches ──
    if (currentRoute.startsWith('/branches')) {
      return [
        _QuickAction(
          icon: Icons.store_outlined,
          label: 'Nueva Sucursal',
          route: '/branches/new',
          color: ZColors.success,
        ),
      ];
    }

    // ── CRM ──
    if (currentRoute.startsWith('/crm')) {
      return [
        _QuickAction(
          icon: Icons.person_add_alt_1_outlined,
          label: 'Nuevo Lead',
          route: '/crm',
          color: ZColors.success,
          actionId: 'crm_lead',
        ),
        _QuickAction(
          icon: Icons.add_chart_outlined,
          label: 'Nueva Oportunidad',
          route: '/crm',
          color: ZColors.info,
          actionId: 'crm_opportunity',
        ),
      ];
    }

    // ── Custom Reports ──
    if (currentRoute.startsWith('/custom-reports')) {
      return [
        _QuickAction(
          icon: Icons.assessment_outlined,
          label: 'Nuevo Reporte',
          route: '/custom-reports/new',
          color: ZColors.success,
        ),
      ];
    }

    // ── Exchange Rates ──
    if (currentRoute.startsWith('/exchange-rates')) {
      return [
        _QuickAction(
          icon: Icons.currency_exchange_outlined,
          label: 'Nuevo Tipo de Cambio',
          route: '/exchange-rates/new',
          color: ZColors.success,
        ),
      ];
    }

    // ── Fleet ──
    if (currentRoute.startsWith('/fleet')) {
      return [
        _QuickAction(
          icon: Icons.local_shipping_outlined,
          label: 'Nuevo Vehículo',
          route: '/fleet/vehicles/new',
          color: ZColors.success,
        ),
        _QuickAction(
          icon: Icons.route_outlined,
          label: 'Nuevo Viaje',
          route: '/fleet/trips/new',
          color: ZColors.info,
        ),
      ];
    }

    // ── Provider Contracts & Invoices ──
    if (currentRoute.startsWith('/providers')) {
      return [
        _QuickAction(
          icon: Icons.handshake_outlined,
          label: 'Nuevo Contrato',
          route: '/providers/contracts/new',
          color: ZColors.success,
        ),
        _QuickAction(
          icon: Icons.receipt_outlined,
          label: 'Registrar Factura',
          route: '/providers/payments',
          color: ZColors.info,
          actionId: 'provider_invoice_register',
        ),
      ];
    }

    // ── Warranties ──
    if (currentRoute.startsWith('/warranties')) {
      return [
        _QuickAction(
          icon: Icons.verified_outlined,
          label: 'Nueva Garantía',
          route: '/warranties/new',
          color: ZColors.success,
        ),
      ];
    }

    // ── Documents ──
    if (currentRoute.startsWith('/documents')) {
      return [
        _QuickAction(
          icon: Icons.upload_file_outlined,
          label: 'Subir Documento',
          route: '/documents',
          color: ZColors.success,
        ),
      ];
    }

    return [];
  }

  @override
  Widget build(BuildContext context) {
    final actions = _getActions();
    final isDark = Theme.of(context).brightness == Brightness.dark;

    if (actions.length == 1) {
      final action = actions.first;
      final callback = action.actionId != null
          ? callbacks[action.actionId!]
          : null;
      return FloatingActionButton(
        onPressed: callback ?? () => context.push(action.route),
        backgroundColor: isDark ? ZColors.brandAccent : ZColors.brandPrimary,
        foregroundColor: isDark ? ZColors.brandPrimary : Colors.white,
        tooltip: action.label,
        child: Icon(action.icon, size: 24),
      );
    }

    return _ExpandableFAB(
      actions: actions,
      callbacks: callbacks,
      isDark: isDark,
    );
  }
}

class _QuickAction {
  final IconData icon;
  final String label;
  final String route;
  final String? actionId;
  final Color color;

  const _QuickAction({
    required this.icon,
    required this.label,
    required this.route,
    this.actionId,
    required this.color,
  });
}

class _ExpandableFAB extends StatefulWidget {
  final List<_QuickAction> actions;
  final Map<String, VoidCallback> callbacks;
  final bool isDark;

  const _ExpandableFAB({
    required this.actions,
    required this.callbacks,
    required this.isDark,
  });

  @override
  State<_ExpandableFAB> createState() => _ExpandableFABState();
}

class _ExpandableFABState extends State<_ExpandableFAB>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _animation;
  bool _isOpen = false;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 200),
    );
    _animation = CurvedAnimation(
      parent: _controller,
      curve: Curves.easeOutBack,
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _toggle() {
    setState(() {
      _isOpen = !_isOpen;
      if (_isOpen) {
        _controller.forward();
      } else {
        _controller.reverse();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        // Action buttons (revealed on expand)
        ...List.generate(widget.actions.length, (index) {
          final action = widget.actions[index];
          final delay = (widget.actions.length - 1 - index) * 0.1;
          return AnimatedBuilder(
            animation: _animation,
            builder: (context, child) {
              final value = _animation.value.clamp(0.0, 1.0);
              final adjustedValue = (value - delay).clamp(0.0, 1.0);
              return Transform.scale(
                scale: adjustedValue,
                child: Opacity(opacity: adjustedValue, child: child),
              );
            },
            child: Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  // Label
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 6,
                    ),
                    decoration: BoxDecoration(
                      color: widget.isDark ? ZColors.darkSurface : Colors.white,
                      borderRadius: BorderRadius.circular(ZRadii.md),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.1),
                          blurRadius: 4,
                          offset: const Offset(0, 2),
                        ),
                      ],
                    ),
                    child: Text(
                      action.label,
                      style: ZTypography.labelMedium.copyWith(
                        color: widget.isDark
                            ? ZColors.neutral200
                            : ZColors.neutral700,
                        fontSize: 12,
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  // Mini FAB
                  FloatingActionButton.small(
                    onPressed: () {
                      _toggle();
                      final callback = widget.callbacks[action.actionId];
                      if (callback != null) {
                        callback();
                      } else {
                        context.push(action.route);
                      }
                    },
                    backgroundColor: action.color,
                    foregroundColor: Colors.white,
                    heroTag: 'fab_${action.actionId ?? action.route}',
                    child: Icon(action.icon, size: 18),
                  ),
                ],
              ),
            ),
          );
        }),

        // Main FAB
        FloatingActionButton(
          onPressed: _toggle,
          backgroundColor: widget.isDark
              ? ZColors.brandAccent
              : ZColors.brandPrimary,
          foregroundColor: widget.isDark ? ZColors.brandPrimary : Colors.white,
          child: AnimatedRotation(
            turns: _isOpen ? 0.125 : 0,
            duration: const Duration(milliseconds: 200),
            child: const Icon(Icons.add, size: 24),
          ),
        ),
      ],
    );
  }
}
