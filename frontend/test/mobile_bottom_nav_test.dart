import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/widgets/header/mobile_bottom_nav.dart';

class _FixedAuthNotifier extends AuthNotifier {
  final AuthState _fixedState;
  _FixedAuthNotifier(this._fixedState);

  @override
  AuthState build() => _fixedState;
}

const _paths = [
  '/dashboard',
  '/sales',
  '/products',
  '/employees',
  '/documents',
  '/payroll',
];

GoRouter _buildRouter({String initialLocation = '/dashboard'}) {
  return GoRouter(
    initialLocation: initialLocation,
    routes: [
      for (final path in _paths)
        GoRoute(
          path: path,
          builder: (context, state) => Scaffold(
            body: Center(child: Text('page:$path')),
            bottomNavigationBar: const MobileBottomNav(),
          ),
        ),
    ],
  );
}

Widget _app(GoRouter router) {
  return ProviderScope(
    overrides: [
      authProvider.overrideWith(
        () => _FixedAuthNotifier(
          const AuthState(
            status: AuthStatus.authenticated,
            displayName: 'Test User',
            role: 'SuperAdmin',
          ),
        ),
      ),
    ],
    child: MaterialApp.router(routerConfig: router),
  );
}

Future<void> _setPhoneViewport(WidgetTester tester) async {
  addTearDown(tester.view.reset);
  tester.view.physicalSize = const Size(390, 844);
  tester.view.devicePixelRatio = 1.0;
}

void main() {
  testWidgets('navega por hubs y "Más" queda activo fuera de ellos',
      (tester) async {
    await _setPhoneViewport(tester);
    await tester.pumpWidget(_app(_buildRouter()));
    await tester.pumpAndSettle();

    NavigationBar bar() =>
        tester.widget<NavigationBar>(find.byType(NavigationBar));

    expect(bar().destinations.length, 5);
    expect(bar().selectedIndex, 0);

    await tester.tap(find.text('Ventas'));
    await tester.pumpAndSettle();

    expect(find.text('page:/sales'), findsOneWidget);
    expect(bar().selectedIndex, 1);

    GoRouter.of(tester.element(find.text('page:/sales'))).go('/documents');
    await tester.pumpAndSettle();

    // Regresión: con 5 destinos el índice no se recorta a 3; "Más" (4) queda
    // seleccionado en rutas que no pertenecen a ningún hub.
    expect(bar().selectedIndex, 4);
  });

  testWidgets('"Más" abre la hoja de módulos con buscador y navega',
      (tester) async {
    await _setPhoneViewport(tester);
    await tester.pumpWidget(_app(_buildRouter()));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Más'));
    await tester.pumpAndSettle();

    expect(find.text('Todos los módulos'), findsOneWidget);
    expect(find.text('OPERACIONES'), findsOneWidget);

    await tester.enterText(find.byType(TextField), 'Nómina');
    await tester.pumpAndSettle();

    expect(find.text('Gestión de Nómina'), findsOneWidget);
    expect(find.text('Productos'), findsNothing);

    await tester.tap(find.text('Gestión de Nómina'));
    await tester.pumpAndSettle();

    expect(find.text('page:/payroll'), findsOneWidget);
    expect(find.text('Todos los módulos'), findsNothing);
  });

  testWidgets('la barra respeta los módulos del plan', (tester) async {
    await _setPhoneViewport(tester);
    final router = _buildRouter();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authProvider.overrideWith(
            () => _FixedAuthNotifier(
              const AuthState(
                status: AuthStatus.authenticated,
                role: 'CompanyAdmin',
                enabledModules: ['Sales'],
                permissions: ['sale.read', 'sale.write'],
              ),
            ),
          ),
        ],
        child: MaterialApp.router(routerConfig: router),
      ),
    );
    await tester.pumpAndSettle();

    final bar = tester.widget<NavigationBar>(find.byType(NavigationBar));
    expect(bar.destinations.length, 3); // Inicio + Ventas + Más
    expect(find.text('Ventas'), findsOneWidget);
    expect(find.text('Inventario'), findsNothing);
  });
}
