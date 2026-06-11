import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class DashboardRoleKpi {
  final String id;
  final String label;
  final String value;
  final double? changePercent;
  final IconData icon;
  final Color color;
  final List<double>? sparklineData;
  final String? drillDownRoute;

  const DashboardRoleKpi({
    required this.id,
    required this.label,
    required this.value,
    this.changePercent,
    required this.icon,
    required this.color,
    this.sparklineData,
    this.drillDownRoute,
  });
}

class DashboardRoleKpiGroup {
  final String sectionLabel;
  final List<DashboardRoleKpi> kpis;

  const DashboardRoleKpiGroup({required this.sectionLabel, required this.kpis});
}

class RoleDashboardNotifier extends AsyncNotifier<List<DashboardRoleKpiGroup>> {
  @override
  Future<List<DashboardRoleKpiGroup>> build() async {
    final role = ref.watch(authProvider).role ?? 'Employee';
    return _kpisForRole(role);
  }

  Future<void> load() async {
    state = const AsyncValue.loading();
    final role = ref.read(authProvider).role ?? 'Employee';
    state = await AsyncValue.guard(() async => _kpisForRole(role));
  }

  void reorderGroup(int oldIndex, int newIndex) {
    state.whenData((groups) {
      final newGroups = [...groups];
      final group = newGroups.removeAt(oldIndex);
      newGroups.insert(newIndex, group);
      state = AsyncValue.data(newGroups);
    });
  }


  List<DashboardRoleKpiGroup> _kpisForRole(String role) {
    switch (role) {
      case 'SuperAdmin':
      case 'CompanyAdmin':
        return _executiveKpis();
      case 'Rrhh':
        return _hrKpis();
      case 'Supervisor':
        return _supervisorKpis();
      case 'Accountant':
        return _accountantKpis();
      default:
        return _employeeKpis();
    }
  }

  List<DashboardRoleKpiGroup> _executiveKpis() => [
        DashboardRoleKpiGroup(
          sectionLabel: 'Ventas',
          kpis: [
            DashboardRoleKpi(
                id: 'sales_today',
                label: 'Ventas Hoy',
                value: r'$0',
                icon: Icons.today,
                color: const Color(0xFF059669),
                drillDownRoute: '/sales'),
            DashboardRoleKpi(
                id: 'sales_month',
                label: 'Ventas del Mes',
                value: r'$0',
                icon: Icons.monetization_on,
                color: const Color(0xFF0891B2),
                drillDownRoute: '/sales'),
            DashboardRoleKpi(
                id: 'avg_ticket',
                label: 'Ticket Promedio',
                value: r'$0',
                icon: Icons.receipt,
                color: const Color(0xFF7C3AED),
                drillDownRoute: '/sales'),
          ],
        ),
        DashboardRoleKpiGroup(
          sectionLabel: 'Créditos',
          kpis: [
            DashboardRoleKpi(
                id: 'active_credits',
                label: 'Créditos Activos',
                value: '0',
                icon: Icons.credit_card,
                color: const Color(0xFFD97706),
                drillDownRoute: '/credits'),
            DashboardRoleKpi(
                id: 'overdue',
                label: 'Vencidos',
                value: '0',
                icon: Icons.warning,
                color: const Color(0xFFDC2626),
                drillDownRoute: '/credits/overdue-dashboard'),
            DashboardRoleKpi(
                id: 'portfolio',
                label: 'Cartera Total',
                value: r'$0',
                icon: Icons.account_balance,
                color: const Color(0xFF4F46E5),
                drillDownRoute: '/credits'),
          ],
        ),
        DashboardRoleKpiGroup(
          sectionLabel: 'Inventario',
          kpis: [
            DashboardRoleKpi(
                id: 'products',
                label: 'Productos',
                value: '0',
                icon: Icons.inventory_2,
                color: const Color(0xFF7C3AED),
                drillDownRoute: '/products'),
            DashboardRoleKpi(
                id: 'low_stock',
                label: 'Stock Bajo',
                value: '0',
                icon: Icons.warning,
                color: const Color(0xFFD97706),
                drillDownRoute: '/products'),
            DashboardRoleKpi(
                id: 'out_of_stock',
                label: 'Agotados',
                value: '0',
                icon: Icons.dangerous,
                color: const Color(0xFFDC2626),
                drillDownRoute: '/products'),
          ],
        ),
        DashboardRoleKpiGroup(
          sectionLabel: 'Caja',
          kpis: [
            DashboardRoleKpi(
                id: 'income_today',
                label: 'Ingresos Hoy',
                value: r'$0',
                icon: Icons.arrow_downward,
                color: const Color(0xFF059669),
                drillDownRoute: '/cash-registers'),
            DashboardRoleKpi(
                id: 'expenses_today',
                label: 'Egresos Hoy',
                value: r'$0',
                icon: Icons.arrow_upward,
                color: const Color(0xFFDC2626),
                drillDownRoute: '/cash-registers'),
            DashboardRoleKpi(
                id: 'open_registers',
                label: 'Cajas Abiertas',
                value: '0',
                icon: Icons.monetization_on,
                color: const Color(0xFF0891B2),
                drillDownRoute: '/cash-registers'),
          ],
        ),
        DashboardRoleKpiGroup(
          sectionLabel: 'RRHH',
          kpis: [
            DashboardRoleKpi(
                id: 'active_employees',
                label: 'Activos',
                value: '0',
                icon: Icons.people,
                color: const Color(0xFF4F46E5),
                drillDownRoute: '/employees'),
            DashboardRoleKpi(
                id: 'pending_vacations',
                label: 'Vac. Pendientes',
                value: '0',
                icon: Icons.beach_access,
                color: const Color(0xFFD97706),
                drillDownRoute: '/vacations'),
            DashboardRoleKpi(
                id: 'pending_permissions',
                label: 'Permisos Pend.',
                value: '0',
                icon: Icons.description,
                color: const Color(0xFFDC2626),
                drillDownRoute: '/permissions'),
          ],
        ),
      ];

  List<DashboardRoleKpiGroup> _hrKpis() => [
        DashboardRoleKpiGroup(
          sectionLabel: 'Personal',
          kpis: [
            DashboardRoleKpi(
                id: 'active_employees',
                label: 'Activos',
                value: '0',
                icon: Icons.people,
                color: const Color(0xFF4F46E5),
                drillDownRoute: '/employees'),
            DashboardRoleKpi(
                id: 'total_employees',
                label: 'Total',
                value: '0',
                icon: Icons.people_outline,
                color: const Color(0xFF64748B),
                drillDownRoute: '/employees'),
          ],
        ),
        DashboardRoleKpiGroup(
          sectionLabel: 'Solicitudes',
          kpis: [
            DashboardRoleKpi(
                id: 'pending_vacations',
                label: 'Vacaciones Pend.',
                value: '0',
                icon: Icons.beach_access,
                color: const Color(0xFFD97706),
                drillDownRoute: '/vacations'),
            DashboardRoleKpi(
                id: 'pending_permissions',
                label: 'Permisos Pend.',
                value: '0',
                icon: Icons.description,
                color: const Color(0xFFDC2626),
                drillDownRoute: '/permissions'),
          ],
        ),
        DashboardRoleKpiGroup(
          sectionLabel: 'Eventos',
          kpis: [
            DashboardRoleKpi(
                id: 'birthdays',
                label: 'Cumpleaños',
                value: '0',
                icon: Icons.cake,
                color: const Color(0xFF0891B2),
                drillDownRoute: '/employees'),
            DashboardRoleKpi(
                id: 'anniversaries',
                label: 'Aniversarios',
                value: '0',
                icon: Icons.work_history,
                color: const Color(0xFF059669),
                drillDownRoute: '/employees'),
          ],
        ),
        DashboardRoleKpiGroup(
          sectionLabel: 'Asistencia',
          kpis: [
            DashboardRoleKpi(
                id: 'present_today',
                label: 'Presentes Hoy',
                value: '0',
                icon: Icons.schedule,
                color: const Color(0xFF059669),
                drillDownRoute: '/attendance'),
            DashboardRoleKpi(
                id: 'absences',
                label: 'Ausencias',
                value: '0',
                icon: Icons.person_off,
                color: const Color(0xFFDC2626),
                drillDownRoute: '/attendance'),
          ],
        ),
      ];

  List<DashboardRoleKpiGroup> _supervisorKpis() => [
        DashboardRoleKpiGroup(
          sectionLabel: 'Comercial',
          kpis: [
            DashboardRoleKpi(
                id: 'sales_today',
                label: 'Ventas Hoy',
                value: r'$0',
                icon: Icons.today,
                color: const Color(0xFF059669),
                drillDownRoute: '/sales'),
            DashboardRoleKpi(
                id: 'sales_month',
                label: 'Ventas del Mes',
                value: r'$0',
                icon: Icons.monetization_on,
                color: const Color(0xFF0891B2),
                drillDownRoute: '/sales'),
          ],
        ),
        DashboardRoleKpiGroup(
          sectionLabel: 'Personal',
          kpis: [
            DashboardRoleKpi(
                id: 'active_employees',
                label: 'Activos',
                value: '0',
                icon: Icons.people,
                color: const Color(0xFF4F46E5),
                drillDownRoute: '/employees'),
            DashboardRoleKpi(
                id: 'pending_vacations',
                label: 'Vac. Pendientes',
                value: '0',
                icon: Icons.beach_access,
                color: const Color(0xFFD97706),
                drillDownRoute: '/vacations'),
          ],
        ),
        DashboardRoleKpiGroup(
          sectionLabel: 'Créditos',
          kpis: [
            DashboardRoleKpi(
                id: 'active_credits',
                label: 'Créditos Activos',
                value: '0',
                icon: Icons.credit_card,
                color: const Color(0xFFD97706),
                drillDownRoute: '/credits'),
            DashboardRoleKpi(
                id: 'overdue',
                label: 'Vencidos',
                value: '0',
                icon: Icons.warning,
                color: const Color(0xFFDC2626),
                drillDownRoute: '/credits/overdue-dashboard'),
          ],
        ),
      ];

  List<DashboardRoleKpiGroup> _accountantKpis() => [
        DashboardRoleKpiGroup(
          sectionLabel: 'Finanzas',
          kpis: [
            DashboardRoleKpi(
                id: 'month_income',
                label: 'Ingresos del Mes',
                value: r'$0',
                icon: Icons.arrow_downward,
                color: const Color(0xFF059669),
                drillDownRoute: '/credits'),
            DashboardRoleKpi(
                id: 'month_expenses',
                label: 'Gastos del Mes',
                value: r'$0',
                icon: Icons.arrow_upward,
                color: const Color(0xFFDC2626),
                drillDownRoute: '/purchases'),
            DashboardRoleKpi(
                id: 'receivables',
                label: 'Ctas por Cobrar',
                value: r'$0',
                icon: Icons.account_balance,
                color: const Color(0xFFD97706),
                drillDownRoute: '/credits'),
            DashboardRoleKpi(
                id: 'payables',
                label: 'Ctas por Pagar',
                value: r'$0',
                icon: Icons.payments,
                color: const Color(0xFF7C3AED),
                drillDownRoute: '/purchases'),
          ],
        ),
        DashboardRoleKpiGroup(
          sectionLabel: 'Presupuestos',
          kpis: [
            DashboardRoleKpi(
                id: 'budget_vs_actual',
                label: 'Presupuesto vs Real',
                value: '0%',
                icon: Icons.compare_arrows,
                color: const Color(0xFF0891B2),
                drillDownRoute: '/budgets/vs-actual'),
            DashboardRoleKpi(
                id: 'total_budget',
                label: 'Presupuesto Total',
                value: r'$0',
                icon: Icons.account_balance_wallet,
                color: const Color(0xFF4F46E5),
                drillDownRoute: '/budgets'),
          ],
        ),
      ];

  List<DashboardRoleKpiGroup> _employeeKpis() => [
        DashboardRoleKpiGroup(
          sectionLabel: 'Mis Datos',
          kpis: [
            DashboardRoleKpi(
                id: 'my_vacations',
                label: 'Mis Vacaciones',
                value: '0',
                icon: Icons.beach_access,
                color: const Color(0xFFD97706),
                drillDownRoute: '/vacations'),
            DashboardRoleKpi(
                id: 'my_permissions',
                label: 'Mis Permisos',
                value: '0',
                icon: Icons.description,
                color: const Color(0xFFDC2626),
                drillDownRoute: '/permissions'),
            DashboardRoleKpi(
                id: 'my_attendance',
                label: 'Asistencia Hoy',
                value: '--',
                icon: Icons.schedule,
                color: const Color(0xFF059669),
                drillDownRoute: '/attendance'),
          ],
        ),
      ];
}

final roleDashboardProvider = AsyncNotifierProvider<RoleDashboardNotifier, List<DashboardRoleKpiGroup>>(
  RoleDashboardNotifier.new,
);
