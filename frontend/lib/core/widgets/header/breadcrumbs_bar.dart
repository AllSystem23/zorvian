import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';

/// BreadcrumbBar — Generates breadcrumbs automatically from the current route path.
/// Uses the NavConfig module structure to build meaningful breadcrumb labels.
final class BreadcrumbBar extends StatelessWidget {
  const BreadcrumbBar({super.key});

  /// Static mapping of route segments to human-readable labels
  static const Map<String, String> _routeLabels = {
    'dashboard': 'Inicio',
    'executive-dashboard': 'Panel Ejecutivo',
    'bi': 'Inteligencia de Negocios',
    'executive': 'Ejecutivo',
    'financial': 'Financiero',
    'commercial': 'Comercial',
    'operational': 'Operacional',
    'employees': 'Trabajadores',
    'attendance': 'Asistencia',
    'history': 'Historial',
    'kiosk': 'Kiosko',
    'qr': 'Código QR',
    'payroll': 'Nómina',
    'periods': 'Periodos',
    'runs': 'Procesamientos',
    'salaries': 'Salarios',
    'deduction-types': 'Tipos de Deducción',
    'settlement': 'Liquidación',
    'vacations': 'Vacaciones',
    'permissions': 'Permisos',
    'departments': 'Departamentos',
    'clients': 'Clientes',
    'statement': 'Estado de Cuenta',
    'sales': 'Ventas',
    'quotes': 'Cotizaciones',
    'kanban': 'Kanban',
    'products': 'Productos',
    'categories': 'Categorías',
    'brands': 'Marcas',
    'suppliers': 'Proveedores',
    'purchases': 'Compras',
    'inventory-movements': 'Movimientos de Inventario',
    'inventory-adjustment': 'Ajustes de Inventario',
    'credits': 'Créditos',
    'overdue-dashboard': 'Dashboard Vencimientos',
    'refinancing': 'Refinanciamiento',
    'cash-registers': 'Caja',
    'arqueo': 'Arqueo',
    'warranties': 'Garantías',
    'providers': 'Prestadores',
    'contracts': 'Contratos',
    'payments': 'Pagos',
    'accounting': 'Contabilidad',
    'trial-balance': 'Balance de Prueba',
    'income-statement': 'Estado de Resultados',
    'chart-of-accounts': 'Catálogo de Cuentas',
    'entries': 'Asientos Contables',
    'account-links': 'Enlaces Contables',
    'reports': 'Reportes',
    'audit-logs': 'Logs de Auditoría',
    'admin': 'Administración',
    'users': 'Usuarios',
    'invite': 'Invitar Usuario',
    'settings': 'Configuración',
    'branches': 'Sucursales',
    'budgets': 'Presupuestos',
    'vs-actual': 'vs Real',
    'cost-centers': 'Centros de Costo',
    'credit-notes': 'Notas de Crédito',
    'approval-flows': 'Flujos de Aprobación',
    'approval-pending': 'Aprobaciones Pendientes',
    'exchange-rates': 'Tipo de Cambio',
    'custom-reports': 'Reportes Personalizados',
    'webhooks': 'Webhooks',
    'documents': 'Documentos',
    'templates': 'Plantillas',
    'chat': 'Centro de Comunicación',
    'profile': 'Mi Perfil',
    'goals': 'Metas',
    'configurator': 'Configurador',
    'my-goals': 'Mis Metas',
    'treasury': 'Tesorería',
    'checks': 'Cheques',
    'transfers': 'Transferencias',
    'deposits': 'Depósitos',
    'commissions': 'Comisiones',
    'collections': 'Cobros',
    'fixed-assets': 'Activos Fijos',
    'new': 'Nuevo',
    'edit': 'Editar',
    'detail': 'Detalle',
  };

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final location = GoRouterState.of(context).matchedLocation;
    final isDark = theme.brightness == Brightness.dark;

    // Don't show breadcrumbs on dashboard
    if (location == '/dashboard' || location == '/dashboard-v2') {
      return const SizedBox.shrink();
    }

    final segments = location.split('/').where((s) => s.isNotEmpty).toList();

    if (segments.isEmpty) return const SizedBox.shrink();

    final items = <BreadcrumbItem>[];

    // Always start with "Inicio" as home
    items.add(BreadcrumbItem(
      label: 'Inicio',
      icon: Icons.home_outlined,
      onTap: () => context.go('/dashboard'),
    ));

    // Build breadcrumb from segments
    String currentPath = '';
    for (int i = 0; i < segments.length; i++) {
      currentPath += '/${segments[i]}';
      final label = _routeLabels[segments[i]] ?? _capitalize(segments[i]);
      final isLast = i == segments.length - 1;

      if (isLast) {
        items.add(BreadcrumbItem(label: label));
      } else {
        final routePath = currentPath;
        items.add(BreadcrumbItem(
          label: label,
          onTap: () => context.go(routePath),
        ));
      }
    }

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: ZSpacing.lg,
        vertical: 8,
      ),
      decoration: BoxDecoration(
        color: isDark
            ? ZColors.darkSurface.withValues(alpha: 0.5)
            : ZColors.surface,
        border: Border(
          bottom: BorderSide(
            color: isDark
                ? ZColors.darkBorder.withValues(alpha: 0.3)
                : ZColors.border.withValues(alpha: 0.5),
            width: 0.5,
          ),
        ),
      ),
      child: Row(
        children: [
          for (int i = 0; i < items.length; i++) ...[
            if (i > 0)
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: ZSpacing.xs),
                child: Icon(
                  Icons.chevron_right,
                  size: 14,
                  color: isDark ? ZColors.neutral500 : ZColors.neutral400,
                ),
              ),
            if (i == items.length - 1)
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (items[i].icon != null)
                    Padding(
                      padding: const EdgeInsets.only(right: 4),
                      child: Icon(
                        items[i].icon,
                        size: 14,
                        color: isDark ? ZColors.neutral200 : ZColors.neutral700,
                      ),
                    ),
                  Text(
                    items[i].label,
                    style: ZTypography.bodySmall.copyWith(
                      fontWeight: FontWeight.w600,
                      color: isDark ? ZColors.neutral200 : ZColors.neutral700,
                    ),
                  ),
                ],
              )
            else
              InkWell(
                onTap: items[i].onTap,
                borderRadius: BorderRadius.circular(ZRadii.xs),
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 4,
                    vertical: 2,
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      if (items[i].icon != null)
                        Padding(
                          padding: const EdgeInsets.only(right: 4),
                          child: Icon(
                            items[i].icon,
                            size: 14,
                            color: isDark
                                ? ZColors.neutral400
                                : ZColors.neutral500,
                          ),
                        ),
                      Text(
                        items[i].label,
                        style: ZTypography.bodySmall.copyWith(
                          color: isDark
                              ? ZColors.neutral400
                              : ZColors.neutral500,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
          ],
        ],
      ),
    );
  }

  static String _capitalize(String text) {
    if (text.isEmpty) return text;
    // Handle kebab-case
    if (text.contains('-')) {
      return text.split('-').map((w) => _capitalize(w)).join(' ');
    }
    return text[0].toUpperCase() + text.substring(1);
  }
}

/// Simple breadcrumb item data class
class BreadcrumbItem {
  final String label;
  final IconData? icon;
  final VoidCallback? onTap;

  const BreadcrumbItem({required this.label, this.icon, this.onTap});
}