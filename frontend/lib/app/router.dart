import 'dart:io';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../auth/auth_provider.dart';
import '../features/dashboard/dashboard_page.dart';
import '../features/login/login_page.dart';
import '../features/onboarding/onboarding_page.dart';
import '../features/unauthorized/unauthorized_page.dart';

const _roleHierarchy = {
  'SuperAdmin': 100,
  'CompanyAdmin': 80,
  'Rrhh': 60,
  'Supervisor': 40,
  'Employee': 20,
};

final _routeRoles = <String, List<String>>{
  '/dashboard': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/employees': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor'],
  '/departments': ['SuperAdmin', 'CompanyAdmin', 'Rrhh'],
  '/vacations': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/permissions': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'],
  '/attendance': ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor'],
  '/admin': ['SuperAdmin', 'CompanyAdmin'],
  '/settings': ['SuperAdmin', 'CompanyAdmin'],
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
        path: '/unauthorized',
        name: 'unauthorized',
        builder: (_, _) => const UnauthorizedPage(),
      ),
    ],
  );
});
