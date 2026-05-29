import 'dart:io';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../auth/auth_provider.dart';
import '../features/dashboard/dashboard_page.dart';
import '../features/login/login_page.dart';
import '../features/onboarding/onboarding_page.dart';

class AppRoute {
  final String path;
  final String name;
  final List<String> allowedRoles;

  const AppRoute({
    required this.path,
    required this.name,
    this.allowedRoles = const [],
  });
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
    ],
  );
});
