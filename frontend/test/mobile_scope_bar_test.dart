import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/providers/company_branch_provider.dart';
import 'package:zorvian/core/widgets/header/mobile_scope_bar.dart';

class _FixedAuthNotifier extends AuthNotifier {
  final AuthState _fixedState;
  _FixedAuthNotifier(this._fixedState);

  @override
  AuthState build() => _fixedState;
}

const _branches = [
  {'id': 'b1', 'name': 'Sucursal A'},
  {'id': 'b2', 'name': 'Sucursal B'},
];

const _warehouses = [
  {'id': 'w1', 'name': 'Bodega Central', 'isDefault': true},
];

Widget _harness(ProviderContainer container) {
  return UncontrolledProviderScope(
    container: container,
    child: MaterialApp(
      home: Scaffold(
        appBar: AppBar(
          title: const Text('Ventas'),
          bottom: const PreferredSize(
            preferredSize: Size.fromHeight(44),
            child: MobileScopeBar(),
          ),
        ),
        body: const SizedBox.expand(),
      ),
    ),
  );
}

ProviderContainer _createContainer() {
  return ProviderContainer(
    overrides: [
      authProvider.overrideWith(
        () => _FixedAuthNotifier(
          const AuthState(
            status: AuthStatus.authenticated,
            displayName: 'Test User',
            role: 'Supervisor',
          ),
        ),
      ),
      companyListProvider.overrideWith((ref) => Future.value(const [])),
      headerBranchListProvider
          .overrideWith((ref) => Future.value(List<Map<String, dynamic>>.from(_branches))),
      headerWarehouseListProvider.overrideWith(
        (ref) => Future.value(List<Map<String, dynamic>>.from(_warehouses)),
      ),
    ],
  );
}

Future<void> _setPhoneViewport(WidgetTester tester) async {
  addTearDown(tester.view.reset);
  tester.view.physicalSize = const Size(390, 844);
  tester.view.devicePixelRatio = 1.0;
}

void main() {
  testWidgets('muestra compañía y auto-selecciona la primera sucursal',
      (tester) async {
    await _setPhoneViewport(tester);
    final container = _createContainer();
    addTearDown(container.dispose);
    container
        .read(companyBranchProvider.notifier)
        .selectCompany('c1', 'Acme');

    await tester.pumpWidget(_harness(container));
    await tester.pump();
    await tester.pump();
    await tester.pump();

    expect(find.text('Acme · Sucursal A'), findsOneWidget);
    expect(container.read(companyBranchProvider).branchId, 'b1');
  });

  testWidgets('la hoja de contexto cambia de sucursal y de bodega',
      (tester) async {
    await _setPhoneViewport(tester);
    final container = _createContainer();
    addTearDown(container.dispose);
    container
        .read(companyBranchProvider.notifier)
        .selectCompany('c1', 'Acme');

    await tester.pumpWidget(_harness(container));
    await tester.pump();
    await tester.pump();
    await tester.pump();

    await tester.tap(find.byType(MobileScopeBar));
    await tester.pumpAndSettle();

    expect(find.text('Contexto de trabajo'), findsOneWidget);
    expect(find.text('Todas las sucursales'), findsOneWidget);
    expect(find.text('Todas las bodegas'), findsOneWidget);

    await tester.tap(find.text('Sucursal B'));
    await tester.pumpAndSettle();
    expect(container.read(companyBranchProvider).branchId, 'b2');

    await tester.tap(find.text('Bodega Central (principal)'));
    await tester.pumpAndSettle();
    expect(container.read(companyBranchProvider).warehouseId, 'w1');

    await tester.tap(find.text('Listo'));
    await tester.pumpAndSettle();

    expect(find.textContaining('Sucursal B'), findsOneWidget);
    expect(find.textContaining('Bodega Central'), findsOneWidget);
  });

  testWidgets('"Todas las sucursales" es una elección explícita', (tester) async {
    await _setPhoneViewport(tester);
    final container = _createContainer();
    addTearDown(container.dispose);
    container
        .read(companyBranchProvider.notifier)
        .selectCompany('c1', 'Acme');

    await tester.pumpWidget(_harness(container));
    await tester.pump();
    await tester.pump();
    await tester.pump();

    await tester.tap(find.byType(MobileScopeBar));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Todas las sucursales'));
    await tester.pumpAndSettle();

    final scope = container.read(companyBranchProvider);
    expect(scope.allBranches, isTrue);
    expect(scope.branchId, isNull);
  });

  testWidgets('scopeLabel resume compañía, sucursal y bodega', (tester) async {
    expect(
      MobileScopeBar.scopeLabel(
        const CompanyBranchState(companyName: 'Acme', branchName: 'Central'),
      ),
      'Acme · Central',
    );
    expect(
      MobileScopeBar.scopeLabel(
        const CompanyBranchState(
          companyName: 'Acme',
          allBranches: true,
          warehouseName: 'Bodega 1',
        ),
      ),
      'Acme · Todas las sucursales · Bodega 1',
    );
    expect(
      MobileScopeBar.scopeLabel(const CompanyBranchState()),
      'Empresa · Sucursal',
    );
  });
}
