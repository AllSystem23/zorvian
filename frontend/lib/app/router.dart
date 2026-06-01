import 'dart:io';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../auth/auth_provider.dart';
import '../features/dashboard/dashboard_page.dart';
import '../features/dashboard/pages/absence_calendar_page.dart';
import '../features/profile/profile_page.dart';
import '../features/attendance/pages/attendance_page.dart';
import '../features/attendance/pages/attendance_history_page.dart';
import '../features/attendance/pages/kiosk_page.dart';
import '../features/attendance/pages/qr_checkin_page.dart';
import '../features/reports/pages/reports_page.dart';
import '../features/reports/pages/audit_logs_page.dart';
import '../features/settings/pages/company_settings_page.dart';
import '../features/settings/pages/leave_types_page.dart';
import '../features/admin/pages/user_list_page.dart';
import '../features/admin/pages/invite_user_page.dart';
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
import '../features/unauthorized/unauthorized_page.dart';

final _routeRoles = <String, List<String>>{
  '/dashboard': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
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
      GoRoute(
        path: '/dashboard',
        name: 'dashboard',
        builder: (_, _) => const DashboardPage(),
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
          GoRoute(
            path: 'history',
            name: 'attendance-history',
            builder: (_, _) => const AttendanceHistoryPage(),
          ),
          GoRoute(
            path: 'kiosk',
            name: 'attendance-kiosk',
            builder: (_, _) => const KioskPage(),
          ),
          GoRoute(
            path: 'qr',
            name: 'attendance-qr',
            builder: (_, _) => const QRCheckInPage(),
          ),
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
          GoRoute(
            path: 'new',
            name: 'employee-new',
            builder: (_, _) => const EmployeeFormPage(),
          ),
          GoRoute(
            path: ':employeeId',
            name: 'employee-detail',
            builder: (_, state) => EmployeeDetailPage(employeeId: state.pathParameters['employeeId']!),
            routes: [
              GoRoute(
                path: 'edit',
                name: 'employee-edit',
                builder: (_, state) => EmployeeFormPage(employeeId: state.pathParameters['employeeId']!),
              ),
            ],
          ),
        ],
      ),
      GoRoute(
        path: '/vacations',
        name: 'vacations',
        builder: (_, _) => const VacationListPage(),
        routes: [
          GoRoute(
            path: 'new',
            name: 'vacation-new',
            builder: (_, _) => const VacationFormPage(),
          ),
          GoRoute(
            path: ':vacationId',
            name: 'vacation-detail',
            builder: (_, state) => VacationDetailPage(vacationId: state.pathParameters['vacationId']!),
          ),
        ],
      ),
      GoRoute(
        path: '/permissions',
        name: 'permissions',
        builder: (_, _) => const PermissionListPage(),
        routes: [
          GoRoute(
            path: 'new',
            name: 'permission-new',
            builder: (_, _) => const PermissionFormPage(),
          ),
          GoRoute(
            path: ':permissionId',
            name: 'permission-detail',
            builder: (_, state) => PermissionDetailPage(permissionId: state.pathParameters['permissionId']!),
          ),
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
          GoRoute(
            path: 'new',
            name: 'department-new',
            builder: (_, _) => const DepartmentFormPage(),
          ),
          GoRoute(
            path: ':departmentId/edit',
            name: 'department-edit',
            builder: (_, state) => DepartmentFormPage(departmentId: state.pathParameters['departmentId']!),
          ),
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
            routes: [
              GoRoute(
                path: 'new',
                name: 'payroll-period-new',
                builder: (_, _) => const PayrollPeriodsPage(),
              ),
            ],
          ),
          GoRoute(
            path: 'runs/:runId',
            name: 'payroll-run-detail',
            builder: (_, state) => PayrollRunDetailPage(runId: state.pathParameters['runId']!),
          ),
        ],
      ),
    ],
  );
});
