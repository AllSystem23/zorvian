import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/features/departments/pages/department_list_page.dart';
import 'package:zorvian/features/departments/providers/department_provider.dart';

/// Gating de ESCRITURA en las páginas que usan su propio `floatingActionButton`
/// (antes solo lo bloqueaba el router → el botón navegaba a `/unauthorized`).
/// La fuente única es `write_permissions.dart`, consumida por el router, el FAB
/// global y la extensión `ref.canWriteLocation`.

class _FixedAuthNotifier extends AuthNotifier {
  final AuthState _fixedState;
  _FixedAuthNotifier(this._fixedState);

  @override
  AuthState build() => _fixedState;
}

class _FakeDepartmentNotifier extends DepartmentNotifier {
  @override
  DepartmentState build() => const DepartmentState();

  @override
  Future<void> load() async {}
}

AuthState _auth(List<String>? permissions, {String role = 'CompanyAdmin'}) =>
    AuthState(
      status: AuthStatus.authenticated,
      displayName: 'Test',
      email: 'test@zorvian.com',
      role: role,
      permissions: permissions,
    );

Future<void> _pumpPage(WidgetTester tester, List<String>? permissions) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        authProvider.overrideWith(() => _FixedAuthNotifier(_auth(permissions))),
        departmentProvider.overrideWith(_FakeDepartmentNotifier.new),
      ],
      child: const MaterialApp(home: DepartmentListPage()),
    ),
  );
  await tester.pumpAndSettle();
}

class _Probe extends ConsumerWidget {
  const _Probe(this.location);

  final String location;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Text('${ref.canWriteLocation(location)}',
        textDirection: TextDirection.ltr);
  }
}

Future<void> _pumpProbe(
  WidgetTester tester,
  String location, {
  List<String>? permissions,
  String role = 'CompanyAdmin',
}) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        authProvider
            .overrideWith(() => _FixedAuthNotifier(_auth(permissions, role: role))),
      ],
      child: MaterialApp(home: _Probe(location)),
    ),
  );
  await tester.pump();
}

void main() {
  group('ref.canWriteLocation', () {
    testWidgets('sin el permiso de escritura devuelve false', (tester) async {
      await _pumpProbe(tester, '/departments/new',
          permissions: ['employee.read']);
      expect(find.text('false'), findsOneWidget);
    });

    testWidgets('con el permiso de escritura devuelve true', (tester) async {
      await _pumpProbe(tester, '/departments/new',
          permissions: ['employee.read', 'employee.write']);
      expect(find.text('true'), findsOneWidget);
    });

    testWidgets('las rutas de lectura no se gatean', (tester) async {
      await _pumpProbe(tester, '/departments', permissions: ['employee.read']);
      expect(find.text('true'), findsOneWidget);
    });

    testWidgets('sin permisos cargados (SSO/compat) no gatea', (tester) async {
      await _pumpProbe(tester, '/departments/new');
      expect(find.text('true'), findsOneWidget);
    });

    testWidgets('SuperAdmin siempre puede', (tester) async {
      await _pumpProbe(tester, '/departments/new',
          permissions: const <String>[], role: 'SuperAdmin');
      expect(find.text('true'), findsOneWidget);
    });
  });

  group('FAB de página (DepartmentListPage)', () {
    testWidgets('visible con el permiso de escritura del módulo',
        (tester) async {
      await _pumpPage(tester, ['employee.read', 'employee.write']);
      expect(find.byType(FloatingActionButton), findsOneWidget);
      expect(find.text('Nuevo departamento'), findsOneWidget);
    });

    testWidgets('oculto sin el permiso de escritura del módulo',
        (tester) async {
      await _pumpPage(tester, ['employee.read']);
      expect(find.byType(FloatingActionButton), findsNothing);
      expect(find.text('Nuevo departamento'), findsNothing);
    });

    testWidgets('sin permisos cargados (SSO/compat) se conserva',
        (tester) async {
      await _pumpPage(tester, null);
      expect(find.byType(FloatingActionButton), findsOneWidget);
    });
  });
}
