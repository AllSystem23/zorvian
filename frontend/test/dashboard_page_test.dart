import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/storage/secure_storage.dart';
import 'package:zorvian/features/dashboard/dashboard_page.dart';
import 'package:zorvian/features/dashboard/providers/dashboard_provider.dart';

/// A test notifier that returns a fixed [AuthState] from build().
///
/// Reproduces the production reload scenario: SuperAdmin authenticated,
/// with a tenants list that contains records with null/empty tenantId or
/// name (backend may return incomplete records right after a reload).
class _FixedAuthNotifier extends AuthNotifier {
  final AuthState _fixedState;
  final List<Map<String, dynamic>> _tenants;

  _FixedAuthNotifier(this._fixedState, this._tenants);

  @override
  AuthState build() => _fixedState;

  @override
  Future<List<Map<String, dynamic>>> getMyTenants() async => _tenants;
}

/// A dashboard notifier that starts in a loaded state and never hits the API.
class _FixedDashboardNotifier extends DashboardNotifier {
  final DashboardState _state;
  _FixedDashboardNotifier(this._state);

  @override
  DashboardState build() => _state;

  @override
  Future<void> loadAll() async {}

  @override
  Future<void> refresh() async {}
}

/// Fake secure storage that does nothing (avoids platform errors).
class _FakeSecureStorage extends SecureStorage {
  @override
  Future<String?> getAccessToken() async => null;

  @override
  Future<String?> getRefreshToken() async => null;

  @override
  Future<void> saveTokens(String access, String refresh) async {}

  @override
  Future<void> clearTokens() async {}

  @override
  Future<String?> getCurrencyCode() async => null;

  @override
  Future<void> saveCurrencyCode(String code) async {}
}

Widget buildTestApp({
  required AuthState authState,
  required List<Map<String, dynamic>> tenants,
  DashboardState? dashboardState,
}) {
  return ProviderScope(
    overrides: [
      authProvider.overrideWith(() => _FixedAuthNotifier(authState, tenants)),
      dashboardProvider.overrideWith(
        () => _FixedDashboardNotifier(
          dashboardState ??
              const DashboardState(kpis: null, recentRequests: []),
        ),
      ),
      secureStorageProvider.overrideWith((ref) => _FakeSecureStorage()),
    ],
    // En producción el DashboardPage vive dentro del Material del
    // _DesktopLayout (AppShell); replicamos ese ancestro aquí.
    child: const MaterialApp(home: Material(child: DashboardPage())),
  );
}

const _emptyDashboard = DashboardState(kpis: null, recentRequests: []);

void main() {
  final superAdmin = AuthState(
    status: AuthStatus.authenticated,
    userId: 'u1',
    email: 'admin@brizuela.com',
    displayName: 'Super Admin',
    role: 'SuperAdmin',
    tenantId: 'tenant-1',
    currencyCode: 'NIO',
  );

  testWidgets(
    'Dashboard renders for SuperAdmin even when a tenant has null tenantId',
    (tester) async {
      await tester.pumpWidget(buildTestApp(
        authState: superAdmin,
        tenants: const [
          {'tenantId': null, 'name': 'Empresa rota'},
          {'tenantId': 'tenant-1', 'name': 'Tienda Brizuela Romero'},
        ],
        dashboardState: _emptyDashboard,
      ));
      // Permite que el FutureBuilder del ZCompanySwitcher resuelva.
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      expect(find.text('Bienvenido de nuevo,'), findsOneWidget);
      expect(find.text('Super Admin'), findsOneWidget);
    },
  );

  testWidgets(
    'Dashboard renders when every tenant lacks name/tenantId fields',
    (tester) async {
      await tester.pumpWidget(buildTestApp(
        authState: superAdmin,
        tenants: const [
          {'name': null},
          {'tenantId': ''},
        ],
        dashboardState: _emptyDashboard,
      ));
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      expect(find.text('Bienvenido de nuevo,'), findsOneWidget);
    },
  );

  testWidgets(
    'Dashboard renders populated KPIs and charts for SuperAdmin',
    (tester) async {
      const kpis = DashboardKpis(
        totalEmployees: 12,
        activeEmployees: 10,
        inactiveEmployees: 2,
        activeEmployeesTrend: 4.2,
        pendingRequestsTrend: -1.5,
        birthdaysTrend: 0.0,
        anniversariesTrend: 2.0,
        employeesByDepartment: [
          DepartmentCount(departmentName: 'Ventas', count: 5),
          DepartmentCount(departmentName: 'Bodega', count: 4),
          DepartmentCount(departmentName: 'Admin', count: 3),
        ],
        pendingVacationRequests: 3,
        pendingPermissionRequests: 1,
        birthdaysThisMonth: 2,
        workAnniversariesThisMonth: 1,
      );
      const state = DashboardState(
        kpis: kpis,
        recentRequests: [
          RecentRequestItem(
            id: 'r1',
            requestType: 'vacacion',
            employeeName: 'Juan Pérez',
            status: 'pending',
            createdAt: '2026-10-01',
          ),
        ],
      );

      await tester.pumpWidget(buildTestApp(
        authState: superAdmin,
        tenants: const [
          {'tenantId': 'tenant-1', 'name': 'Tienda Brizuela Romero'},
          {'tenantId': 'tenant-2', 'name': 'Sucursal Norte'},
        ],
        dashboardState: state,
      ));
      await tester.pumpAndSettle();

      expect(find.text('Bienvenido de nuevo,'), findsOneWidget);
      expect(find.text('Resumen Ejecutivo'), findsOneWidget);
      expect(find.text('Tienda Brizuela Romero'), findsOneWidget);
    },
  );

  testWidgets(
    'Dashboard renders when getMyTenants responds with an unexpected shape',
    (tester) async {
      // Simula la respuesta defensiva del provider: lista vacía.
      await tester.pumpWidget(buildTestApp(
        authState: superAdmin,
        tenants: const [],
        dashboardState: _emptyDashboard,
      ));
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      expect(find.text('Bienvenido de nuevo,'), findsOneWidget);
    },
  );
}
