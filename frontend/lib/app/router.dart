import 'dart:io';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../auth/auth_provider.dart';
import '../core/widgets/app_shell.dart';
import '../features/dashboard/dashboard_page.dart';
import '../features/dashboard/pages/absence_calendar_page.dart';
import '../features/executive_dashboard/executive_dashboard_page.dart';
import '../features/profile/profile_page.dart';
import '../features/attendance/pages/attendance_page.dart';
import '../features/attendance/pages/attendance_history_page.dart';
import '../features/attendance/pages/kiosk_page.dart';
import '../features/attendance/pages/qr_checkin_page.dart';
import '../features/reports/pages/reports_page.dart';
import '../features/reports/pages/audit_logs_page.dart';
import '../features/settings/pages/company_settings_page.dart';
import '../features/settings/pages/leave_types_page.dart';
import '../features/settings/pages/leave_type_form_page.dart';
import '../features/admin/pages/user_list_page.dart';
import '../features/admin/pages/invite_user_page.dart';
import '../features/branches/pages/branch_list_page.dart';
import '../features/branches/pages/branch_form_page.dart';
import '../features/departments/pages/department_form_page.dart';
import '../features/departments/pages/department_list_page.dart';
import '../features/employees/pages/employee_detail_page.dart';
import '../features/employees/pages/employee_form_page.dart';
import '../features/employees/pages/employee_list_page.dart';
import '../features/login/register_page.dart';
import '../features/login/login_page.dart';
import '../features/vacations/pages/vacation_detail_page.dart';
import '../features/vacations/pages/vacation_form_page.dart';
import '../features/vacations/pages/vacation_list_page.dart';
import '../features/permissions/pages/permission_detail_page.dart';
import '../features/permissions/pages/permission_form_page.dart';
import '../features/permissions/pages/permission_list_page.dart';
import '../features/onboarding/onboarding_page.dart';
import '../features/payroll/pages/payroll_page.dart';
import '../features/payroll/pages/settlement_form_page.dart';
import '../features/payroll/pages/payroll_run_detail_page.dart';
import '../features/payroll/pages/payroll_periods_page.dart';
import '../features/payroll/pages/salaries_page.dart';
import '../features/payroll/pages/deduction_types_page.dart';
import '../features/goals/configurator/goals_config_screen.dart';
import '../features/goals/dashboard/goals_dashboard_screen.dart';
import '../features/goals/portal/my_goals_screen.dart';
import '../features/providers/pages/provider_list_page.dart';
import '../features/providers/pages/provider_detail_page.dart';
import '../features/providers/pages/service_contract_detail_page.dart';
import '../features/providers/pages/provider_contracts_page.dart';
import '../features/providers/pages/provider_invoices_page.dart';
import '../features/unauthorized/unauthorized_page.dart';
import '../features/clients/pages/client_list_page.dart';
import '../features/clients/pages/client_form_page.dart';
import '../features/clients/pages/client_statement_page.dart';
import '../features/sales/pages/sale_list_page.dart';
import '../features/sales/pages/sale_detail_page.dart';
import '../features/sales/pages/sale_form_page.dart';
import '../features/quotes/pages/quote_list_page.dart';
import '../features/quotes/pages/quote_form_page.dart';
import '../features/quotes/pages/quote_detail_page.dart';
import '../features/quotes/pages/quote_kanban_page.dart';
import '../features/products/pages/product_list_page.dart';
import '../features/products/pages/product_form_page.dart';
import '../features/categories/pages/category_list_page.dart';
import '../features/brands/pages/brand_list_page.dart';
import '../features/cost_centers/pages/cost_center_list_page.dart';
import '../features/cost_centers/pages/cost_center_form_page.dart';
import '../features/budgets/pages/budget_list_page.dart';
import '../features/budgets/pages/budget_form_page.dart';
import '../features/budgets/pages/budget_vs_actual_page.dart';
import '../features/credit_notes/pages/credit_note_list_page.dart';
import '../features/credit_notes/pages/credit_note_form_page.dart';
import '../features/suppliers/pages/supplier_list_page.dart';
import '../features/suppliers/pages/supplier_form_page.dart';
import '../features/inventory_movements/pages/inventory_movement_list_page.dart';
import '../features/credits/pages/credit_list_page.dart';
import '../features/credits/pages/credit_detail_page.dart';
import '../features/cash_registers/pages/cash_register_list_page.dart';
import '../features/cash_registers/pages/cash_register_detail_page.dart';
import '../features/cash_registers/pages/cash_register_arqueo_page.dart';
import '../features/dashboard_v2/dashboard_v2_page.dart';
import '../features/warranties/pages/warranty_list_page.dart';
import '../features/warranties/pages/warranty_form_page.dart';
import '../features/purchases/pages/purchase_list_page.dart';
import '../features/purchases/pages/purchase_form_page.dart';
import '../features/purchases/pages/purchase_detail_page.dart';
import '../features/purchases/pages/inventory_adjustment_page.dart';
import '../features/approval/pages/approval_flow_list_page.dart';
import '../features/approval/pages/approval_flow_form_page.dart';
import '../features/approval/pages/approval_pending_page.dart';
import '../features/credits/pages/credit_refinancing_form_page.dart';
import '../features/credits/pages/overdue_dashboard_page.dart';
import '../features/bi/pages/executive_dashboard_page.dart' as bi;
import '../features/bi/pages/financial_dashboard_page.dart';
import '../features/bi/pages/commercial_dashboard_page.dart';
import '../features/bi/pages/operational_dashboard_page.dart';
import '../features/chat/pages/chat_page.dart';
import '../features/exchange_rates/pages/exchange_rate_list_page.dart';
import '../features/exchange_rates/pages/exchange_rate_form_page.dart';
import '../features/custom_reports/pages/custom_report_list_page.dart';
import '../features/custom_reports/pages/custom_report_builder_page.dart';
import '../features/custom_reports/pages/custom_report_result_page.dart';
import '../features/webhooks/pages/webhook_list_page.dart';
import '../features/webhooks/pages/webhook_form_page.dart';
import '../features/webhooks/pages/webhook_logs_page.dart';
import '../features/accounting/pages/equity_changes_page.dart';
import '../features/accounting/pages/comparative_reports_page.dart';
import '../features/accounting/pages/ai_assistant_page.dart';
import '../features/accounting/pages/trial_balance_page.dart';
import '../features/accounting/pages/income_statement_page.dart';
import '../features/accounting/pages/chart_of_accounts_page.dart';
import '../features/accounting/pages/accounting_periods_page.dart';
import '../features/accounting/pages/accounting_entries_page.dart';
import '../features/accounting/pages/account_links_page.dart';
import '../features/treasury/pages/check_issuance_page.dart';
import '../features/treasury/pages/bank_transfer_page.dart';
import '../features/treasury/pages/bank_deposit_page.dart';
import '../features/treasury/pages/bank_commission_page.dart';
import '../features/treasury/pages/bank_collection_page.dart';
import '../features/documents/pages/document_center_page.dart';
import '../features/documents/pages/template_editor_page.dart';

final _routeRoles = <String, List<String>>{
  '/goals/configurator': ['SuperAdmin', 'CompanyAdmin'],
  '/goals/dashboard': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor'],
  '/goals/my-goals': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/dashboard': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/executive-dashboard': ['SuperAdmin', 'CompanyAdmin'],
  '/employees': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor'],
  '/departments': ['SuperAdmin', 'CompanyAdmin', 'Rrhh'],
  '/vacations': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/permissions': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/attendance': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/reports': ['SuperAdmin', 'CompanyAdmin', 'Rrhh'],
  '/audit-logs': ['SuperAdmin', 'CompanyAdmin'],
  '/admin': ['SuperAdmin', 'CompanyAdmin'],
  '/settings': ['SuperAdmin', 'CompanyAdmin'],
  '/payroll': ['SuperAdmin', 'CompanyAdmin', 'Rrhh'],
  '/clients': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/sales': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/quotes': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/products': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/categories': ['SuperAdmin', 'CompanyAdmin'],
  '/brands': ['SuperAdmin', 'CompanyAdmin'],
  '/cost-centers': ['SuperAdmin', 'CompanyAdmin'],
  '/budgets': ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
  '/budgets/vs-actual': ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
  '/credit-notes': ['SuperAdmin', 'CompanyAdmin', 'Supervisor'],
  '/approval-flows': ['SuperAdmin', 'CompanyAdmin'],
  '/approval-pending': ['SuperAdmin', 'CompanyAdmin', 'Supervisor'],
  '/suppliers': ['SuperAdmin', 'CompanyAdmin', 'Supervisor'],
  '/inventory-movements': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/credits': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/credits/overdue-dashboard': ['SuperAdmin', 'CompanyAdmin', 'Supervisor'],
  '/cash-registers': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/warranties': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/providers': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor'],
  '/branches': ['SuperAdmin', 'CompanyAdmin', 'Supervisor', 'Rrhh', 'Employee'],
  '/bi/executive': ['SuperAdmin', 'CompanyAdmin'],
  '/bi/financial': ['SuperAdmin', 'CompanyAdmin'],
  '/bi/commercial': ['SuperAdmin', 'CompanyAdmin', 'Supervisor', 'Employee'],
  '/dashboard-v2': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee', 'Accountant'],
  '/bi/operational': ['SuperAdmin', 'CompanyAdmin', 'Rrhh'],
  '/chat': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/exchange-rates': ['SuperAdmin', 'CompanyAdmin'],
  '/custom-reports': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/webhooks': ['SuperAdmin', 'CompanyAdmin'],
  '/accounting/reports/equity': ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
  '/accounting/reports/comparative': ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
  '/accounting/ai-assistant': ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
  '/accounting/trial-balance': ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
  '/accounting/income-statement': ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
  '/accounting/chart-of-accounts': ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
  '/accounting/periods': ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
  '/accounting/entries': ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
  '/accounting/account-links': ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
  '/treasury': ['SuperAdmin', 'CompanyAdmin', 'Accountant'],
};

bool _hasAccess(String role, String location) {
  final allowed = _routeRoles.entries.firstWhere(
    (e) => location.startsWith(e.key),
    orElse: () => MapEntry('', ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee']),
  ).value;
  return allowed.contains(role);
}

final routerProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authProvider);

  return GoRouter(
    initialLocation: '/login',
    debugLogDiagnostics: !kIsWeb && Platform.isWindows,
    redirect: (context, state) {
      final status = authState.status;
      final location = state.matchedLocation;
      final isLoginRoute = location == '/login';
      final isOnboardingRoute = location == '/onboarding';

      if (status == AuthStatus.unknown) return null;

      if (status == AuthStatus.unauthenticated && !isLoginRoute) {
        return '/login';
      }

      if (status == AuthStatus.authenticated && isLoginRoute) {
        final hasCompany = authState.tenantId != null && authState.tenantId!.isNotEmpty;
        return hasCompany ? '/dashboard' : '/onboarding';
      }

      if (status == AuthStatus.authenticated && !isLoginRoute && !isOnboardingRoute) {
        if (!_hasAccess(authState.role ?? 'Employee', location)) {
          return '/unauthorized';
        }
      }

      return null;
    },
    routes: [
      GoRoute(
        path: '/login',
        name: 'login',
        builder: (_, _) => const LoginPage(),
      ),
      GoRoute(
        path: '/register',
        name: 'register',
        builder: (_, state) => RegisterPage(inviteCode: state.uri.queryParameters['code']),
      ),
      GoRoute(
        path: '/onboarding',
        name: 'onboarding',
        builder: (_, _) => const OnboardingPage(),
      ),
      ShellRoute(
        builder: (_, _, child) => AppShell(child: child),
        routes: [
          GoRoute(
            path: '/dashboard',
            name: 'dashboard',
            builder: (_, _) => const DashboardPage(),
          ),
          GoRoute(
            path: '/dashboard-v2',
            name: 'dashboard-v2',
            builder: (_, _) => const DashboardV2Page(),
          ),
          GoRoute(
            path: '/executive-dashboard',
            name: 'executive-dashboard',
            builder: (_, _) => const ExecutiveDashboardPage(),
          ),
          GoRoute(
            path: '/profile',
            name: 'profile',
            builder: (_, _) => const ProfilePage(),
          ),
          GoRoute(
            path: '/absence-calendar',
            name: 'absence-calendar',
            builder: (_, _) => const AbsenceCalendarPage(),
          ),
          GoRoute(
            path: '/attendance',
            name: 'attendance',
            builder: (_, _) => const AttendancePage(),
            routes: [
              GoRoute(path: 'history', name: 'attendance-history', builder: (_, _) => const AttendanceHistoryPage()),
              GoRoute(path: 'kiosk', name: 'attendance-kiosk', builder: (_, _) => const KioskPage()),
              GoRoute(path: 'qr', name: 'attendance-qr', builder: (_, _) => const QRCheckInPage()),
            ],
          ),
          GoRoute(
            path: '/goals/configurator',
            name: 'goals-configurator',
            builder: (_, _) => const GoalsConfigScreen(),
          ),
          GoRoute(
            path: '/goals/dashboard',
            name: 'goals-dashboard',
            builder: (_, _) => const GoalsDashboardScreen(),
          ),
          GoRoute(
            path: '/goals/my-goals',
            name: 'goals-my-goals',
            builder: (_, _) => const MyGoalsScreen(),
          ),
          GoRoute(
            path: '/unauthorized',
            name: 'unauthorized',
            builder: (_, _) => const UnauthorizedPage(),
          ),
          GoRoute(
            path: '/employees',
            name: 'employees',
            builder: (_, _) => const EmployeeListPage(),
            routes: [
              GoRoute(path: 'new', name: 'employee-new', builder: (_, _) => const EmployeeFormPage()),
              GoRoute(
                path: ':employeeId',
                name: 'employee-detail',
                builder: (_, state) => EmployeeDetailPage(employeeId: state.pathParameters['employeeId']!),
                routes: [
                  GoRoute(path: 'edit', name: 'employee-edit', builder: (_, state) => EmployeeFormPage(employeeId: state.pathParameters['employeeId']!)),
                ],
              ),
            ],
          ),
          GoRoute(
            path: '/vacations',
            name: 'vacations',
            builder: (_, _) => const VacationListPage(),
            routes: [
              GoRoute(path: 'new', name: 'vacation-new', builder: (_, _) => const VacationFormPage()),
              GoRoute(path: ':vacationId', name: 'vacation-detail', builder: (_, state) => VacationDetailPage(vacationId: state.pathParameters['vacationId']!)),
            ],
          ),
          GoRoute(
            path: '/permissions',
            name: 'permissions',
            builder: (_, _) => const PermissionListPage(),
            routes: [
              GoRoute(path: 'new', name: 'permission-new', builder: (_, _) => const PermissionFormPage()),
              GoRoute(path: ':permissionId', name: 'permission-detail', builder: (_, state) => PermissionDetailPage(permissionId: state.pathParameters['permissionId']!)),
            ],
          ),
          GoRoute(
            path: '/settings',
            name: 'settings',
            builder: (_, _) => const CompanySettingsPage(),
          ),
          GoRoute(
            path: '/admin/users',
            name: 'admin-users',
            builder: (_, _) => const UserListPage(),
          ),
          GoRoute(
            path: '/admin/invite',
            name: 'admin-invite',
            builder: (_, _) => const InviteUserPage(),
          ),
          GoRoute(
            path: '/leave-types',
            name: 'leave-types',
            builder: (_, _) => const LeaveTypesPage(),
            routes: [
              GoRoute(
                path: 'new',
                name: 'leave-type-new',
                builder: (_, _) => const LeaveTypeFormPage(),
              ),
            ],
          ),
          GoRoute(
            path: '/branches',
            name: 'branches',
            builder: (_, _) => const BranchListPage(),
            routes: [
              GoRoute(path: 'new', name: 'branch-new', builder: (_, _) => const BranchFormPage()),
              GoRoute(path: ':branchId', name: 'branch-detail', builder: (_, state) => BranchFormPage(branchId: state.pathParameters['branchId'])),
            ],
          ),
          GoRoute(
            path: '/reports',
            name: 'reports',
            builder: (_, _) => const ReportsPage(),
          ),
          GoRoute(
            path: '/audit-logs',
            name: 'audit-logs',
            builder: (_, _) => const AuditLogsPage(),
          ),
          GoRoute(
            path: '/departments',
            name: 'departments',
            builder: (_, _) => const DepartmentListPage(),
            routes: [
              GoRoute(path: 'new', name: 'department-new', builder: (_, _) => const DepartmentFormPage()),
              GoRoute(path: ':departmentId/edit', name: 'department-edit', builder: (_, state) => DepartmentFormPage(departmentId: state.pathParameters['departmentId']!)),
            ],
          ),
          GoRoute(
            path: '/payroll',
            name: 'payroll',
            builder: (_, _) => const PayrollPage(),
            routes: [
              GoRoute(
                path: 'periods',
                name: 'payroll-periods',
                builder: (_, _) => const PayrollPeriodsPage(),
                routes: [GoRoute(path: 'new', name: 'payroll-period-new', builder: (_, _) => const PayrollPeriodsPage())],
              ),
              GoRoute(path: 'runs/:runId', name: 'payroll-run-detail', builder: (_, state) => PayrollRunDetailPage(runId: state.pathParameters['runId']!)),
              GoRoute(path: 'salaries', name: 'payroll-salaries', builder: (_, _) => const SalariesPage()),
              GoRoute(path: 'deduction-types', name: 'payroll-deduction-types', builder: (_, _) => const DeductionTypesPage()),
              GoRoute(
                path: 'settlement/:employeeId/:companyId',
                name: 'payroll-settlement',
                builder: (_, state) => SettlementFormPage(
                  employeeId: state.pathParameters['employeeId']!,
                  companyId: state.pathParameters['companyId']!,
                ),
              ),
            ],
          ),
          GoRoute(
            path: '/clients',
            name: 'clients',
            builder: (_, _) => const ClientListPage(),
            routes: [
              GoRoute(path: 'new', name: 'client-new', builder: (_, _) => const ClientFormPage()),
              GoRoute(path: ':clientId/edit', name: 'client-edit', builder: (_, state) => ClientFormPage(clientId: state.pathParameters['clientId']!)),
              GoRoute(path: ':clientId/statement', name: 'client-statement', builder: (_, state) => ClientStatementPage(clientId: state.pathParameters['clientId']!)),
            ],
          ),
          GoRoute(
            path: '/sales',
            name: 'sales',
            builder: (_, _) => const SaleListPage(),
          ),
          GoRoute(
            path: '/sales/new',
            name: 'new-sale',
            builder: (_, _) => const NewSalePage(),
          ),
          GoRoute(
            path: '/sales/:id',
            name: 'sale-detail',
            builder: (_, state) => SaleDetailPage(saleId: state.pathParameters['id']!),
          ),
          GoRoute(
            path: '/quotes',
            name: 'quotes',
            builder: (_, _) => const QuoteListPage(),
            routes: [
              GoRoute(path: 'new', name: 'quote-new', builder: (_, _) => const QuoteFormPage()),
              GoRoute(path: 'kanban', name: 'quote-kanban', builder: (_, _) => const QuoteKanbanPage()),
              GoRoute(path: ':quoteId', name: 'quote-detail', builder: (_, state) => QuoteDetailPage(quoteId: state.pathParameters['quoteId']!)),
              GoRoute(path: ':quoteId/edit', name: 'quote-edit', builder: (_, state) => QuoteFormPage(quoteId: state.pathParameters['quoteId'])),
            ],
          ),
          GoRoute(
            path: '/products',
            name: 'products',
            builder: (_, _) => const ProductListPage(),
            routes: [
              GoRoute(path: 'new', name: 'product-new', builder: (_, _) => const ProductFormPage()),
              GoRoute(path: ':productId/edit', name: 'product-edit', builder: (_, state) => ProductFormPage(productId: state.pathParameters['productId']!)),
            ],
          ),
          GoRoute(
            path: '/categories',
            name: 'categories',
            builder: (_, _) => const CategoryListPage(),
          ),
          GoRoute(
            path: '/brands',
            name: 'brands',
            builder: (_, _) => const BrandListPage(),
          ),
          GoRoute(
            path: '/cost-centers',
            name: 'cost-centers',
            builder: (_, _) => const CostCenterListPage(),
            routes: [
              GoRoute(path: 'new', name: 'cost-center-new', builder: (_, _) => const CostCenterFormPage()),
              GoRoute(path: ':costCenterId/edit', name: 'cost-center-edit', builder: (_, state) => CostCenterFormPage(costCenterId: state.pathParameters['costCenterId']!)),
            ],
          ),
          GoRoute(
            path: '/budgets',
            name: 'budgets',
            builder: (_, _) => const BudgetListPage(),
            routes: [
              GoRoute(path: 'new', name: 'budget-new', builder: (_, _) => const BudgetFormPage()),
              GoRoute(path: ':budgetId/edit', name: 'budget-edit', builder: (_, state) => BudgetFormPage(budgetId: state.pathParameters['budgetId']!)),
            ],
          ),
          GoRoute(
            path: '/budgets/vs-actual',
            name: 'budget-vs-actual',
            builder: (_, _) => const BudgetVsActualPage(),
          ),
          GoRoute(
            path: '/credit-notes',
            name: 'credit-notes',
            builder: (_, _) => const CreditNoteListPage(),
            routes: [
              GoRoute(path: 'new/:saleId', name: 'credit-note-new', builder: (_, state) => CreditNoteFormPage(saleId: state.pathParameters['saleId']!)),
            ],
          ),
          GoRoute(
            path: '/approval-flows',
            name: 'approval-flows',
            builder: (_, _) => const ApprovalFlowListPage(),
            routes: [
              GoRoute(path: 'new', name: 'approval-flow-new', builder: (_, _) => const ApprovalFlowFormPage()),
              GoRoute(path: ':flowId/edit', name: 'approval-flow-edit', builder: (_, state) => ApprovalFlowFormPage(flowId: state.pathParameters['flowId']!)),
            ],
          ),
          GoRoute(
            path: '/approval-pending',
            name: 'approval-pending',
            builder: (_, _) => const ApprovalPendingPage(),
          ),
          GoRoute(
            path: '/suppliers',
            name: 'suppliers',
            builder: (_, _) => const SupplierListPage(),
            routes: [
              GoRoute(path: 'new', name: 'supplier-new', builder: (_, _) => const SupplierFormPage()),
              GoRoute(path: ':supplierId/edit', name: 'supplier-edit', builder: (_, state) => SupplierFormPage(supplierId: state.pathParameters['supplierId']!)),
            ],
          ),
          GoRoute(
            path: '/inventory-movements',
            name: 'inventory-movements',
            builder: (_, _) => const InventoryMovementListPage(),
          ),
          GoRoute(
            path: '/credits',
            name: 'credits',
            builder: (_, _) => const CreditListPage(),
            routes: [
              GoRoute(path: ':creditId', name: 'credit-detail', builder: (_, state) => CreditDetailPage(creditId: state.pathParameters['creditId']!)),
              GoRoute(path: ':creditId/refinancing', name: 'credit-refinancing', builder: (_, state) => CreditRefinancingFormPage(creditId: state.pathParameters['creditId']!)),
            ],
          ),
          GoRoute(
            path: '/credits/overdue-dashboard',
            name: 'overdue-dashboard',
            builder: (_, _) => const OverdueDashboardPage(),
          ),
          GoRoute(
            path: '/cash-registers',
            name: 'cash-registers',
            builder: (_, _) => const CashRegisterListPage(),
            routes: [
              GoRoute(path: ':registerId', name: 'cash-register-detail', builder: (_, state) => CashRegisterDetailPage(registerId: state.pathParameters['registerId']!),
                routes: [
                  GoRoute(path: 'arqueo', name: 'cash-register-arqueo', builder: (_, state) => CashRegisterArqueoPage(registerId: state.pathParameters['registerId']!)),
                ],
              ),
            ],
          ),
          GoRoute(
            path: '/treasury/checks',
            name: 'treasury-checks',
            builder: (_, _) => const CheckIssuancePage(),
          ),
          GoRoute(
            path: '/treasury/transfers',
            name: 'treasury-transfers',
            builder: (_, _) => const BankTransferPage(),
          ),
          GoRoute(
            path: '/treasury/deposits',
            name: 'treasury-deposits',
            builder: (_, _) => const BankDepositPage(),
          ),
          GoRoute(
            path: '/treasury/commissions',
            name: 'treasury-commissions',
            builder: (_, _) => const BankCommissionPage(),
          ),
          GoRoute(
            path: '/treasury/collections',
            name: 'treasury-collections',
            builder: (_, _) => const BankCollectionPage(),
          ),
          GoRoute(
            path: '/warranties',
            name: 'warranties',
            builder: (_, _) => const WarrantyListPage(),
            routes: [
              GoRoute(path: 'new', name: 'warranty-new', builder: (_, _) => const WarrantyFormPage()),
              GoRoute(path: ':warrantyId/edit', name: 'warranty-edit', builder: (_, state) => WarrantyFormPage(warrantyId: state.pathParameters['warrantyId']!)),
            ],
          ),
          GoRoute(
            path: '/providers',
            name: 'providers',
            builder: (_, _) => const ProviderListPage(),
            routes: [
              GoRoute(
                path: 'contracts',
                name: 'provider-contracts',
                builder: (_, _) => const ProviderContractsPage(),
              ),
              GoRoute(
                path: ':providerId',
                name: 'provider-detail',
                builder: (_, state) => ProviderDetailPage(id: state.pathParameters['providerId']!),
              ),
              GoRoute(
                path: 'contracts/:contractId',
                name: 'contract-detail',
                builder: (_, state) => ServiceContractDetailPage(id: state.pathParameters['contractId']!),
              ),
              GoRoute(
                path: 'payments',
                name: 'provider-payments',
                builder: (_, _) => const ProviderInvoicesPage(),
              ),
            ],
          ),
          GoRoute(
            path: '/purchases',
            name: 'purchases',
            builder: (_, _) => const PurchaseListPage(),
            routes: [
              GoRoute(path: 'new', name: 'purchase-new', builder: (_, _) => const PurchaseFormPage()),
              GoRoute(path: ':purchaseId', name: 'purchase-detail', builder: (_, state) => PurchaseDetailPage(purchaseId: state.pathParameters['purchaseId']!)),
            ],
          ),
          GoRoute(
            path: '/inventory-adjustment',
            name: 'inventory-adjustment',
            builder: (_, _) => const InventoryAdjustmentPage(),
          ),
          GoRoute(
            path: '/bi/executive',
            name: 'bi-executive',
            builder: (_, _) => const bi.ExecutiveDashboardPage(),
          ),
          GoRoute(
            path: '/bi/financial',
            name: 'bi-financial',
            builder: (_, _) => const FinancialDashboardPage(),
          ),
          GoRoute(
            path: '/bi/commercial',
            name: 'bi-commercial',
            builder: (_, _) => const CommercialDashboardPage(),
          ),
          GoRoute(
            path: '/bi/operational',
            name: 'bi-operational',
            builder: (_, _) => const OperationalDashboardPage(),
          ),
          GoRoute(
            path: '/exchange-rates',
            name: 'exchange-rates',
            builder: (_, _) => const ExchangeRateListPage(),
            routes: [
              GoRoute(
                path: 'new',
                name: 'exchange-rate-new',
                builder: (_, _) => const ExchangeRateFormPage(),
              ),
              GoRoute(
                path: ':exchangeRateId/edit',
                name: 'exchange-rate-edit',
                builder: (_, state) => ExchangeRateFormPage(
                  exchangeRateId: state.pathParameters['exchangeRateId']!,
                ),
              ),
            ],
          ),
          GoRoute(
            path: '/custom-reports',
            name: 'custom-reports',
            builder: (_, _) => const CustomReportListPage(),
            routes: [
              GoRoute(
                path: 'new',
                name: 'custom-report-new',
                builder: (_, _) => const CustomReportBuilderPage(),
              ),
              GoRoute(
                path: ':reportId/edit',
                name: 'custom-report-edit',
                builder: (_, state) => CustomReportBuilderPage(
                  reportId: state.pathParameters['reportId']!,
                ),
              ),
              GoRoute(
                path: 'result',
                name: 'custom-reports-result',
                builder: (_, state) {
                  final extra = state.extra as Map<String, dynamic>;
                  return CustomReportResultPage(
                    reportName: extra['reportName'] as String? ?? '',
                    module: extra['module'] as String? ?? 'sales',
                    fields: extra['fields'] as List<dynamic>? ?? [],
                    columns: (extra['columns'] as List<dynamic>?)?.cast<String>() ?? [],
                    rows: (extra['rows'] as List<dynamic>?)?.cast<Map<String, dynamic>>() ?? [],
                  );
                },
              ),
            ],
          ),
          GoRoute(
            path: '/accounting/reports/equity',
            name: 'accounting-equity',
            builder: (_, _) => const EquityChangesPage(),
          ),
          GoRoute(
            path: '/accounting/reports/comparative',
            name: 'accounting-comparative',
            builder: (_, _) => const ComparativeReportsPage(),
          ),
          GoRoute(
            path: '/accounting/ai-assistant',
            name: 'accounting-ai-assistant',
            builder: (_, _) => const AiAssistantPage(),
          ),
          GoRoute(
            path: '/accounting/trial-balance',
            name: 'accounting-trial-balance',
            builder: (_, _) => const TrialBalancePage(),
          ),
          GoRoute(
            path: '/accounting/income-statement',
            name: 'accounting-income-statement',
            builder: (_, _) => const IncomeStatementPage(),
          ),
          GoRoute(
            path: '/accounting/chart-of-accounts',
            name: 'accounting-chart-of-accounts',
            builder: (_, _) => const ChartOfAccountsPage(),
          ),
          GoRoute(
            path: '/accounting/periods',
            name: 'accounting-periods',
            builder: (_, _) => const AccountingPeriodsPage(),
          ),
          GoRoute(
            path: '/accounting/entries',
            name: 'accounting-entries',
            builder: (_, _) => const AccountingEntriesPage(),
          ),
          GoRoute(
            path: '/accounting/account-links',
            name: 'accounting-account-links',
            builder: (_, _) => const AccountLinksPage(),
          ),
          GoRoute(
            path: '/webhooks',
            name: 'webhooks',
            builder: (_, _) => const WebhookListPage(),
            routes: [
              GoRoute(
                path: 'new',
                name: 'webhook-new',
                builder: (_, _) => const WebhookFormPage(),
              ),
              GoRoute(
                path: ':webhookId/edit',
                name: 'webhook-edit',
                builder: (_, state) => WebhookFormPage(
                  webhookId: state.pathParameters['webhookId']!,
                ),
              ),
              GoRoute(
                path: ':webhookId/logs',
                name: 'webhook-logs',
                builder: (_, state) => WebhookLogsPage(
                  webhookId: state.pathParameters['webhookId']!,
                ),
              ),
            ],
          ),
          // ── Document Management Module ──
          GoRoute(
            path: '/documents',
            name: 'document-center',
            builder: (_, _) => const DocumentCenterPage(),
          ),
          GoRoute(
            path: '/documents/templates/new',
            name: 'template-new',
            builder: (_, _) => const TemplateEditorPage(),
          ),
          GoRoute(
            path: '/documents/templates/:templateId/edit',
            name: 'template-edit',
            builder: (_, state) => TemplateEditorPage(templateId: state.pathParameters['templateId']!),
          ),
        ],
      ),
      GoRoute(
        path: '/chat',
        name: 'chat',
        builder: (_, _) => const ChatPage(),
      ),
    ],
  );
});
