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
import '../features/payroll/pages/payroll_run_detail_page.dart';
import '../features/payroll/pages/payroll_periods_page.dart';
import '../features/payroll/pages/salaries_page.dart';
import '../features/payroll/pages/deduction_types_page.dart';
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
import '../features/products/pages/product_list_page.dart';
import '../features/products/pages/product_form_page.dart';
import '../features/categories/pages/category_list_page.dart';
import '../features/brands/pages/brand_list_page.dart';
import '../features/suppliers/pages/supplier_list_page.dart';
import '../features/suppliers/pages/supplier_form_page.dart';
import '../features/inventory_movements/pages/inventory_movement_list_page.dart';
import '../features/credits/pages/credit_list_page.dart';
import '../features/credits/pages/credit_detail_page.dart';
import '../features/cash_registers/pages/cash_register_list_page.dart';
import '../features/cash_registers/pages/cash_register_detail_page.dart';
import '../features/warranties/pages/warranty_list_page.dart';
import '../features/warranties/pages/warranty_form_page.dart';
import '../features/purchases/pages/purchase_list_page.dart';
import '../features/purchases/pages/purchase_form_page.dart';
import '../features/purchases/pages/purchase_detail_page.dart';
import '../features/purchases/pages/inventory_adjustment_page.dart';
import '../features/bi/pages/executive_dashboard_page.dart' as bi;
import '../features/bi/pages/financial_dashboard_page.dart';
import '../features/bi/pages/commercial_dashboard_page.dart';
import '../features/bi/pages/operational_dashboard_page.dart';

final _routeRoles = <String, List<String>>{
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
  '/suppliers': ['SuperAdmin', 'CompanyAdmin', 'Supervisor'],
  '/inventory-movements': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/credits': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/cash-registers': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/warranties': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/branches': ['SuperAdmin', 'CompanyAdmin', 'Supervisor', 'Rrhh', 'Employee'],
  '/bi/executive': ['SuperAdmin', 'CompanyAdmin'],
  '/bi/financial': ['SuperAdmin', 'CompanyAdmin'],
  '/bi/commercial': ['SuperAdmin', 'CompanyAdmin', 'Supervisor', 'Employee'],
  '/bi/operational': ['SuperAdmin', 'CompanyAdmin', 'Rrhh'],
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
            ],
          ),
          GoRoute(
            path: '/cash-registers',
            name: 'cash-registers',
            builder: (_, _) => const CashRegisterListPage(),
            routes: [
              GoRoute(path: ':registerId', name: 'cash-register-detail', builder: (_, state) => CashRegisterDetailPage(registerId: state.pathParameters['registerId']!)),
            ],
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
        ],
      ),
    ],
  );
});
