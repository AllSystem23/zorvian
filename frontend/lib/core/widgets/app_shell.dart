import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../../features/cash_registers/providers/cash_register_provider.dart';
import '../../features/crm/widgets/crm_forms.dart';
import '../../features/fleet/providers/fleet_document_provider.dart';
import '../../features/providers/pages/provider_invoices_page.dart';
import '../../features/providers/providers/provider_state.dart';
import '../../shared/ds/ds.dart';
import '../navigation/nav_provider.dart';
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
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _trackNavigation();
    });
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
        if (size == ScreenSize.desktop) {
          return _DesktopLayout(
            role: role,
            location: location,
            child: widget.child,
          );
        }
        return _MobileLayout(
          role: role,
          location: location,
          child: widget.child,
        );
      },
    );
  }
}

Map<String, VoidCallback> _buildFabCallbacks(
  BuildContext context,
  WidgetRef ref,
  String location,
) {
  final callbacks = <String, VoidCallback>{};

  if (location.startsWith('/cash-registers')) {
    callbacks['cash_open'] = () => _openCashRegister(context, ref);
  }

  if (location.startsWith('/crm')) {
    callbacks['crm_lead'] = () => showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (_) => const AddLeadSheet(),
    );
    callbacks['crm_opportunity'] = () => showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (_) => const AddOpportunitySheet(),
    );
  }

  if (location.startsWith('/providers/payments')) {
    callbacks['provider_invoice_register'] = () => showDialog(
      context: context,
      builder: (_) => RegisterInvoiceDialog(
        onSaved: () => ref.invalidate(allInvoicesProvider),
      ),
    );
  }

  if (location.startsWith('/fleet/documents')) {
    callbacks['fleet_documents_new'] = () =>
        _openFleetDocumentForm(context, ref);
  }

  return callbacks;
}

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

final class _DesktopLayout extends ConsumerWidget {
  final String role;
  final String location;
  final Widget child;

  const _DesktopLayout({
    required this.role,
    required this.location,
    required this.child,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final callbacks = _buildFabCallbacks(context, ref, location);

    return Material(
      color: isDark ? ZColors.darkBackground : ZColors.background,
      child: Row(
        children: [
          ZorvianSidebar(role: role, location: location, shellRef: ref),
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
                          ZSpacing.lg,
                        ),
                        child: child,
                      ),
                    ),
                  ],
                ),
                // ── Floating Quick Actions FAB ──
                Positioned(
                  bottom: ZSpacing.xl,
                  right: ZSpacing.xl,
                  child: ZQuickActionsFAB(
                    currentRoute: location,
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
    const Map<String, String> titles = {
      '/dashboard': 'Dashboard',
      '/dashboard-v2': 'Dashboard',
      '/executive-dashboard': 'Panel Ejecutivo',
      '/employees': 'Empleados',
      '/attendance': 'Asistencia',
      '/payroll': 'Nómina',
      '/vacations': 'Vacaciones',
      '/permissions': 'Permisos',
      '/departments': 'Departamentos',
      '/clients': 'Clientes',
      '/sales': 'Ventas',
      '/quotes': 'Cotizaciones',
      '/products': 'Productos',
      '/categories': 'Categorías',
      '/brands': 'Marcas',
      '/suppliers': 'Proveedores',
      '/purchases': 'Compras',
      '/inventory-movements': 'Inventario',
      '/inventory-adjustment': 'Ajuste Inventario',
      '/credits': 'Créditos',
      '/cash-registers': 'Caja',
      '/warranties': 'Garantías',
      '/providers': 'Prestadores',
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
      '/absence-calendar': 'Calendario',
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
    final callbacks = _buildFabCallbacks(context, ref, location);

    return Scaffold(
      appBar: AppBar(
        title: Text(pageTitle),
        leading: Builder(
          builder: (ctx) => IconButton(
            icon: const Icon(Icons.menu),
            onPressed: () => Scaffold.of(ctx).openDrawer(),
          ),
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
      body: child,
    );
  }
}
