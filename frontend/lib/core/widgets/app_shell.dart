import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../auth/auth_provider.dart';
import '../../features/admin/pages/super_admin_companies_page.dart';
import '../../features/cash_registers/providers/cash_register_provider.dart';
import '../../features/fiscal/pages/country_tax_config_page.dart';
import '../../features/fiscal/pages/regional_tax_config_page.dart';
import '../../features/sales/providers/sale_provider.dart';
import '../../features/crm/widgets/crm_forms.dart';
import '../../features/fleet/providers/fleet_document_provider.dart';
import '../../features/providers/pages/provider_invoices_page.dart';
import '../../features/providers/providers/provider_state.dart';
import '../../shared/ds/ds.dart';
import '../navigation/nav_provider.dart';
import '../providers/company_branch_provider.dart';
import '../theme/theme_provider.dart';
import 'responsive_layout.dart';
import 'sidebar/sidebar.dart';
import 'header/global_header.dart';
import 'header/breadcrumbs_bar.dart';
import 'header/mobile_bottom_nav.dart';

final class AppShell extends ConsumerStatefulWidget {
  final Widget child;

  const AppShell({super.key, required this.child});

  @override
  ConsumerState<AppShell> createState() => _AppShellState();
}

final class _AppShellState extends ConsumerState<AppShell> {
  @override
  void initState() {
    super.initState();
    // Listen for auth completion to auto-select company for ALL users
    // (not just SuperAdmin). Runs eagerly, before the header widget mounts.
    ref.listen(authProvider, (_, next) {
      if (next.status == AuthStatus.authenticated) {
        WidgetsBinding.instance.addPostFrameCallback((_) {
          _selectFirstCompany();
        });
      }
    });
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _trackNavigation();
      // Also try immediately in case auth is already ready
      _selectFirstCompany();
    });
  }

  /// Auto-select the first available company for ANY user role.
  /// Used to ensure the company context is set before pages render.
  /// Retries up to 3 times for cold-backend tolerance (Render.com cold starts).
  Future<void> _selectFirstCompany() async {
    final auth = ref.read(authProvider);
    if (auth.status != AuthStatus.authenticated) return;
    if (ref.read(companyBranchProvider).companyId != null) return;

    for (var attempt = 0; attempt < 3; attempt++) {
      try {
        final companies = await ref.read(companyListProvider.future);
        if (companies.isNotEmpty) {
          final first = companies.first;
          final companyId = first['id'] as String? ?? '';
          final companyName = first['name'] as String? ?? first['legalName'] as String? ?? 'Empresa';
          final tenantId = first['tenantId'] as String? ?? '';
          if (companyId.isNotEmpty) {
            ref.read(companyBranchProvider.notifier).selectCompany(companyId, companyName);
            if (auth.role == 'SuperAdmin' && tenantId.isNotEmpty) {
              final success = await ref.read(authProvider.notifier).switchTenant(tenantId);
              if (success) ref.invalidate(headerBranchListProvider);
            }
            return;
          }
        }
      } catch (_) {}
      if (attempt < 2) await Future.delayed(const Duration(seconds: 2));
    }
  }

  @override
  void didUpdateWidget(AppShell oldWidget) {
    super.didUpdateWidget(oldWidget);
    _trackNavigation();
  }

  void _trackNavigation() {
    final location = GoRouterState.of(context).matchedLocation;
    if (location != '/login' && location != '/') {
      ref.read(recentItemsProvider.notifier).add(location);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = ref.watch(authProvider);
    final role = auth.role ?? 'Employee';
    final location = GoRouterState.of(context).matchedLocation;

    // Load favorites from preferences
    ref.listen(authProvider, (_, next) {
      if (next.role != null) {
        try {
          final prefs = ref.read(preferencesServiceProvider);
          final saved = prefs.favoriteRoutes;
          if (saved.isNotEmpty) {
            ref.read(favoritesProvider.notifier).toggle(saved.first);
          }
        } catch (_) {}
      }
    });

    return ResponsiveBuilder(
      builder: (context, size) {
        switch (size) {
          case ScreenSize.desktop:
            return _DesktopLayout(
              role: role,
              location: location,
              child: widget.child,
            );
          case ScreenSize.tablet:
            return _TabletLayout(
              role: role,
              location: location,
              child: widget.child,
            );
          case ScreenSize.mobile:
            return _MobileLayout(
              role: role,
              location: location,
              child: widget.child,
            );
        }
      },
    );
  }
}

/// Tracks currently open modals to prevent duplicate dialogs/sheets from FAB actions.
final _activeModals = <String>{};

/// Shows a modal dialog/sheet with duplicate guard.
/// [id] is a unique key for this modal type.
/// [show] is the function that shows the modal (returns a Future).
void _showModalOnce(String id, BuildContext context, Future<void> Function() show) {
  if (_activeModals.contains(id)) return;
  _activeModals.add(id);
  show().whenComplete(() => _activeModals.remove(id));
}

Map<String, VoidCallback> _buildFabCallbacks(
  BuildContext context,
  WidgetRef ref,
  String location,
) {
  final callbacks = <String, VoidCallback>{};

  if (location.startsWith('/cash-registers')) {
    callbacks['cash_open'] = () => _showModalOnce('cash_open', context, () => _openCashRegister(context, ref));
  }

  if (location.startsWith('/crm')) {
    callbacks['crm_lead'] = () => _showModalOnce('crm_lead', context, () =>
      showModalBottomSheet(
        context: context,
        isScrollControlled: true,
        builder: (_) => const AddLeadSheet(),
      ),
    );
    callbacks['crm_opportunity'] = () => _showModalOnce('crm_opportunity', context, () =>
      showModalBottomSheet(
        context: context,
        isScrollControlled: true,
        builder: (_) => const AddOpportunitySheet(),
      ),
    );
  }

  if (location.startsWith('/providers/payments')) {
    callbacks['provider_invoice_register'] = () => _showModalOnce('provider_invoice', context, () =>
      showDialog(
        context: context,
        builder: (_) => RegisterInvoiceDialog(
          onSaved: () => ref.invalidate(allInvoicesProvider),
        ),
      ),
    );
  }

  if (location.startsWith('/fleet/documents')) {
    callbacks['fleet_documents_new'] = () {
      final current = GoRouterState.of(context).matchedLocation;
      if (current == '/fleet/documents/new') return;
      _openFleetDocumentForm(context, ref);
    };
  }

  // Navigate to sale creation and refresh list when returning
  if (location.startsWith('/sales')) {
    callbacks['new_sale'] = () {
      final current = GoRouterState.of(context).matchedLocation;
      if (current == '/sales/new') return;
      context.push('/sales/new').then((_) {
        if (context.mounted) ref.read(saleProvider.notifier).load();
      });
    };
  }

  // Open fiscal config form (country or regional based on route)
  if (location.startsWith('/admin/country-tax-configs')) {
    callbacks['new_fiscal_config'] = () => _showModalOnce('tax_config_country', context, () =>
      showModalBottomSheet(
        context: context,
        isScrollControlled: true,
        useSafeArea: true,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        ),
        builder: (_) => CountryTaxConfigForm(
          onSaved: () => ref.read(countryTaxConfigProvider.notifier).load(),
        ),
      ),
    );
  }

  if (location.startsWith('/admin/regional-tax-configs')) {
    callbacks['new_fiscal_config'] = () => _showModalOnce('tax_config_regional', context, () =>
      showModalBottomSheet(
        context: context,
        isScrollControlled: true,
        useSafeArea: true,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        ),
        builder: (_) => RegionalTaxConfigForm(
          onSaved: () => ref.read(regionalTaxConfigProvider.notifier).load(),
        ),
      ),
    );
  }

  // Open company create dialog (SuperAdmin)
  if (location.startsWith('/admin/companies') ||
      location.startsWith('/admin/subscription-plans')) {
    callbacks['new_company'] = () => _showModalOnce('new_company', context, () =>
      showDialog(
        context: context,
        builder: (_) => CompanyCreateDialog(
          onCreated: () {
            ref.invalidate(companyListProvider);
          },
        ),
      ),
    );
  }

  return callbacks;
}

/// Wrapped in _showModalOnce via callback — safe for double-click.
Future<void> _openCashRegister(BuildContext context, WidgetRef ref) async {
  final codeCtrl = TextEditingController();
  final balanceCtrl = TextEditingController();

  try {
    final result = await ZModal.show<bool>(
      context,
      title: 'Abrir Caja',
      confirmText: 'Abrir',
      cancelText: 'Cancelar',
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ZTextField(controller: codeCtrl, label: 'Código'),
          const SizedBox(height: 12),
          ZTextField(
            controller: balanceCtrl,
            label: 'Saldo Inicial',
            keyboardType: TextInputType.number,
            prefix: const Text(r'C$ '),
          ),
        ],
      ),
    );

    if (result != true || codeCtrl.text.isEmpty) return;

    final dio = ref.read(dioClientProvider);
    await dio.post(
      'cash-registers/open',
      data: {
        'code': codeCtrl.text,
        'openingBalance': double.tryParse(balanceCtrl.text) ?? 0,
        'branchId': '00000000-0000-0000-0000-000000000000',
      },
    );
    await ref.read(cashRegisterProvider.notifier).load();

    if (context.mounted) ZToast.success(context, 'Caja abierta');
  } catch (e) {
    if (context.mounted) ZToast.error(context, 'Error: $e');
  } finally {
    codeCtrl.dispose();
    balanceCtrl.dispose();
  }
}

Future<void> _openFleetDocumentForm(BuildContext context, WidgetRef ref) async {
  final result = await context.push<bool>('/fleet/documents/new');
  if (result == true) ref.read(fleetDocumentProvider.notifier).load();
}

/// ── Desktop Layout (>= 992px) ──
/// Full sidebar (280px) by default. Auto-collapses on narrow desktop (< 1200px).
/// User can manually expand/collapse; auto-collapse disables after manual expand.
final class _DesktopLayout extends ConsumerStatefulWidget {
  final String role;
  final String location;
  final Widget child;

  const _DesktopLayout({
    required this.role,
    required this.location,
    required this.child,
  });

  @override
  ConsumerState<_DesktopLayout> createState() => _DesktopLayoutState();
}

final class _DesktopLayoutState extends ConsumerState<_DesktopLayout> {
  /// When true, auto-collapse has already been applied — don't re-collapse after user manual expand.
  bool _autoCollapseApplied = false;

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.of(context).size.width;
    final isNarrow = width < 1200;
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final hasFab = ZQuickActionsFAB.routeHasActions(widget.location);
    final callbacks = _buildFabCallbacks(context, ref, widget.location);
    final bottomPadding = hasFab ? 72.0 : ZSpacing.lg;
    final userCollapsed = ref.watch(sidebarCollapsedProvider);

    // ── Auto-collapse on narrow desktop (only once, before user interacts) ──
    if (isNarrow && !userCollapsed && !_autoCollapseApplied) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted && ref.read(sidebarCollapsedProvider) == false) {
          ref.read(sidebarCollapsedProvider.notifier).setCollapsed(true);
          if (mounted) setState(() => _autoCollapseApplied = true);
        }
      });
    }

    // Reset the flag when window returns to wide desktop
    if (!isNarrow && _autoCollapseApplied) {
      _autoCollapseApplied = false;
    }

    return Material(
      color: isDark ? ZColors.darkBackground : ZColors.background,
      child: Row(
        children: [
          // Sidebar reads from provider — auto-collapse modified the provider directly
          ZorvianSidebar(
            role: widget.role,
            location: widget.location,
            shellRef: ref,
          ),
          Container(
            width: 1,
            color: isDark
                ? ZColors.darkBorder.withValues(alpha: 0.5)
                : ZColors.border,
          ),
          // ── Right Side: Header + Breadcrumbs + Content ──
          Expanded(
            child: Stack(
              children: [
                Column(
                  children: [
                    // ── Global Header ──
                    const GlobalHeader(),
                    // ── Breadcrumbs Bar ──
                    const BreadcrumbBar(),
                    // ── Page Content ──
                    Expanded(
                      child: AnimatedContainer(
                        duration: const Duration(milliseconds: 300),
                        padding: EdgeInsets.fromLTRB(
                          ZSpacing.xl,
                          ZSpacing.lg,
                          ZSpacing.xl,
                          bottomPadding,
                        ),
                        child: widget.child,
                      ),
                    ),
                  ],
                ),
                // ── Floating Quick Actions FAB ──
                Positioned(
                  bottom: ZSpacing.xl,
                  right: ZSpacing.xl,
                  child: ZQuickActionsFAB(
                    currentRoute: widget.location,
                    callbacks: callbacks,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// ── Tablet Layout (576 – 991px) ──
/// Collapsed sidebar (64px) always visible on the left.
/// Full sidebar opens as a drawer overlay via hamburger menu in GlobalHeader.
final class _TabletLayout extends ConsumerStatefulWidget {
  final String role;
  final String location;
  final Widget child;

  const _TabletLayout({
    required this.role,
    required this.location,
    required this.child,
  });

  @override
  ConsumerState<_TabletLayout> createState() => _TabletLayoutState();
}

final class _TabletLayoutState extends ConsumerState<_TabletLayout> {
  final GlobalKey<ScaffoldState> _scaffoldKey = GlobalKey<ScaffoldState>();
  Listenable? _routeListener;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) {
        _routeListener = GoRouter.of(context).routerDelegate;
        _routeListener!.addListener(_closeDrawerOnNav);
      }
    });
  }

  @override
  void dispose() {
    _routeListener?.removeListener(_closeDrawerOnNav);
    super.dispose();
  }

  /// Closes the drawer when the route changes (e.g. user taps collapsed sidebar).
  void _closeDrawerOnNav() {
    final state = _scaffoldKey.currentState;
    if (state != null && state.isDrawerOpen) {
      state.closeDrawer();
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final hasFab = ZQuickActionsFAB.routeHasActions(widget.location);
    final callbacks = _buildFabCallbacks(context, ref, widget.location);
    final bottomPadding = hasFab ? 72.0 : ZSpacing.lg;

    return Material(
      color: isDark ? ZColors.darkBackground : ZColors.background,
      child: Row(
        children: [
          // ── Collapsed sidebar (always visible in tablet) ──
          // Wrap in GestureDetector to allow swipe-left to close the drawer
          // when user drags on the collapsed sidebar area.
          GestureDetector(
            onHorizontalDragEnd: (details) {
              // primaryVelocity < 0 = swipe left
              if (details.primaryVelocity != null && details.primaryVelocity! < -300) {
                final state = _scaffoldKey.currentState;
                if (state != null && state.isDrawerOpen) {
                  state.closeDrawer();
                }
              }
            },
            // translucent: observe events without consuming them from children
            behavior: HitTestBehavior.translucent,
            child: ZorvianSidebar(
              role: widget.role,
              location: widget.location,
              shellRef: ref,
              collapsedOverride: true,
            ),
          ),
          // ── Divider ──
          Container(
            width: 1,
            color: isDark
                ? ZColors.darkBorder.withValues(alpha: 0.5)
                : ZColors.border,
          ),
          // ── Content area (Scaffold with drawer for full sidebar) ──
          Expanded(
            child: Scaffold(
              key: _scaffoldKey,
              drawer: Drawer(
                width: 304,
                elevation: 24,
                shape: const RoundedRectangleBorder(
                  borderRadius: BorderRadius.only(
                    topRight: Radius.circular(16),
                    bottomRight: Radius.circular(16),
                  ),
                ),
                child: Stack(
                  children: [
                    SafeArea(
                      child: ZorvianSidebar(
                        role: widget.role,
                        location: widget.location,
                        shellRef: ref,
                        collapsedOverride: false,
                      ),
                    ),
                    // ── Drag handle hint on right edge ──
                    Positioned(
                      right: 0,
                      top: 0,
                      bottom: 0,
                      child: _DrawerDragHandle(isDark: isDark),
                    ),
                  ],
                ),
              ),
              drawerScrimColor: isDark
                  ? ZColors.brandPrimary.withValues(alpha: 0.35)
                  : Colors.black.withValues(alpha: 0.25),
              body: Stack(
                children: [
                  Column(
                    children: [
                      // ── Global Header with hamburger ──
                      GlobalHeader(
                        onMenuTap: () => _scaffoldKey.currentState?.openDrawer(),
                      ),
                      // ── Breadcrumbs ──
                      const BreadcrumbBar(),
                      // ── Page Content ──
                      Expanded(
                        child: AnimatedContainer(
                          duration: const Duration(milliseconds: 300),
                          padding: EdgeInsets.fromLTRB(
                            ZSpacing.xl,
                            ZSpacing.lg,
                            ZSpacing.xl,
                            bottomPadding,
                          ),
                          child: widget.child,
                        ),
                      ),
                    ],
                  ),
                  // ── Floating Quick Actions FAB ──
                  Positioned(
                    bottom: ZSpacing.xl,
                    right: ZSpacing.xl,
                    child: ZQuickActionsFAB(
                      currentRoute: widget.location,
                      callbacks: callbacks,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Subtle drag handle on the right edge of the tablet drawer.
/// Indicates the drawer can be swiped left to close.
final class _DrawerDragHandle extends StatelessWidget {
  final bool isDark;

  const _DrawerDragHandle({required this.isDark});

  @override
  Widget build(BuildContext context) {
    return IgnorePointer(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 80),
        child: SizedBox(
          width: 24,
          child: Center(
            child: Container(
              width: 20,
              height: 52,
              decoration: BoxDecoration(
                color: (isDark ? ZColors.neutral400 : ZColors.neutral300)
                    .withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(
                  color: (isDark ? ZColors.neutral500 : ZColors.neutral300)
                      .withValues(alpha: 0.15),
                  width: 0.5,
                ),
              ),
              child: Center(
                child: Icon(
                  Icons.chevron_left,
                  size: 14,
                  color: (isDark ? ZColors.neutral500 : ZColors.neutral400)
                      .withValues(alpha: 0.6),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

final class _MobileLayout extends ConsumerWidget {
  final String role;
  final String location;
  final Widget child;

  const _MobileLayout({
    required this.role,
    required this.location,
    required this.child,
  });

  /// Returns a contextual page title based on the current location
  static String _getPageTitle(String location) {
    // ── Sub‑page detection (detail / edit / new) ──
    if (location.endsWith('/new')) return 'Nuevo';
    if (location.endsWith('/edit')) return 'Editar';

    // Detect detail pages (route ends with a UUID or numeric ID)
    final segments = location.split('/');
    if (segments.length >= 3) {
      final last = segments.last;
      if (RegExp(r'^[0-9a-f\-]{36}$').hasMatch(last) || RegExp(r'^\d+$').hasMatch(last)) {
        final parentPath = segments.sublist(0, segments.length - 1).join('/');
        final parentTitle = _getPageTitle('/$parentPath');
        if (parentTitle != 'Zorvian ERP') return '$parentTitle • Detalle';
      }
    }

    const Map<String, String> titles = {
      '/dashboard': 'Dashboard',
      '/dashboard-v2': 'Dashboard',
      '/executive-dashboard': 'Panel Ejecutivo',
      '/employees': 'Trabajadores',
      '/attendance': 'Asistencia',
      '/payroll': 'Nómina',
      '/payroll/periods': 'Períodos de Nómina',
      '/payroll/salaries': 'Salarios',
      '/payroll/deduction-types': 'Tipos de Deducción',
      '/payroll/settlement': 'Liquidación de Contrato',
      '/vacations': 'Vacaciones',
      '/permissions': 'Permisos',
      '/departments': 'Departamentos',
      '/clients': 'Clientes',
      '/sales': 'Ventas',
      '/sales/new': 'Nueva Venta',
      '/quotes': 'Cotizaciones',
      '/products': 'Productos',
      '/products/new': 'Nuevo Producto',
      '/products/kardex': 'Kardex',
      '/products/valuation': 'Valoración de Inventario',
      '/products/dashboard': 'Inventario y Costeo',
      '/products/adjustments': 'Ajuste de Inventario',
      '/categories': 'Categorías',
      '/brands': 'Marcas',
      '/suppliers': 'Proveedores',
      '/suppliers/new': 'Nuevo Proveedor',
      '/purchases': 'Compras',
      '/purchases/new': 'Nueva Compra',
      '/purchases/payments': 'Pagos a Proveedores',
      '/purchases/payments/new': 'Registrar Pago',
      '/purchases/credit-notes': 'Notas de Crédito',
      '/purchases/credit-notes/new': 'Nueva Nota de Crédito',
      '/inventory-movements': 'Inventario',
      '/inventory-adjustment': 'Ajuste Inventario',
      '/credits': 'Créditos',
      '/credits/overdue-dashboard': 'Dashboard de Mora',
      '/cash-registers': 'Caja',
      '/warranties': 'Garantías',
      '/providers': 'Prestadores',
      '/providers/payments': 'Pagos de Prestadores',
      '/budgets': 'Presupuestos',
      '/cost-centers': 'Centros de Costo',
      '/credit-notes': 'Notas de Crédito',
      '/approval-pending': 'Aprobaciones',
      '/exchange-rates': 'Tipo de Cambio',
      '/custom-reports': 'Reportes',
      '/webhooks': 'Webhooks',
      '/documents': 'Documentos',
      '/chat': 'Comunicación',
      '/profile': 'Mi Perfil',
      '/settings': 'Configuración',
      '/audit-logs': 'Auditoría',
      '/admin/users': 'Usuarios',
      '/admin/companies': 'Empresas',
      '/admin/country-tax-configs': 'Configuración Fiscal',
      '/admin/regional-tax-configs': 'Configuración Regional',
      '/bi/executive': 'BI Ejecutivo',
      '/bi/financial': 'BI Financiero',
      '/bi/commercial': 'BI Comercial',
      '/bi/operational': 'BI Operacional',
      '/accounting/trial-balance': 'Balance de Prueba',
      '/accounting/income-statement': 'Estado de Resultados',
      '/accounting/chart-of-accounts': 'Catálogo de Cuentas',
      '/accounting/entries': 'Asientos Contables',
      '/accounting/periods': 'Periodos Contables',
      '/treasury/checks': 'Cheques',
      '/treasury/transfers': 'Transferencias',
      '/treasury/deposits': 'Depósitos',
      '/goals/dashboard': 'Metas',
      '/goals/configurator': 'Configurador de Metas',
      '/absence-calendar': 'Calendario',
      '/fleet': 'Flotilla',
    };

    // Try exact match first, then prefix match
    return titles[location] ??
        titles.entries
            .where((e) => location.startsWith(e.key))
            .map((e) => e.value)
            .firstOrNull ??
        'Zorvian ERP';
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final pageTitle = _getPageTitle(location);
    final hasFab = ZQuickActionsFAB.routeHasActions(location);
    final callbacks = _buildFabCallbacks(context, ref, location);
    final bottomPadding = hasFab ? 72.0 : ZSpacing.lg;

    return Scaffold(
      appBar: AppBar(
        title: Text(pageTitle),
        leading: Builder(
          builder: (ctx) {
            final router = GoRouter.of(ctx);
            if (router.canPop()) {
              return IconButton(
                icon: const Icon(Icons.arrow_back),
                tooltip: 'Atrás',
                onPressed: () => router.pop(),
              );
            }
            return IconButton(
              icon: const Icon(Icons.menu),
              tooltip: 'Menú principal',
              onPressed: () => Scaffold.of(ctx).openDrawer(),
            );
          },
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.search),
            onPressed: () => ZCommandPalette.show(context),
          ),
          Consumer(
            builder: (_, ref, _) {
              final mode = ref.watch(themeModeProvider);
              return IconButton(
                icon: Icon(
                  mode == ThemeMode.dark ? Icons.light_mode : Icons.dark_mode,
                ),
                onPressed: () => ref.read(themeModeProvider.notifier).toggle(),
              );
            },
          ),
          IconButton(
            icon: const Icon(Icons.person_outline),
            tooltip: 'Perfil',
            onPressed: () => context.push('/profile'),
          ),
        ],
      ),
      drawer: Drawer(
        child: SafeArea(
          child: ZorvianSidebar(role: role, location: location, shellRef: ref),
        ),
      ),
      bottomNavigationBar: const MobileBottomNav(),
      floatingActionButton: ZQuickActionsFAB(
        currentRoute: location,
        callbacks: callbacks,
      ),
      body: Padding(
        padding: EdgeInsets.only(bottom: bottomPadding),
        child: child,
      ),
    );
  }
}
