import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/features/admin/models/user_model.dart';
import 'package:zorvian/features/admin/pages/user_list_page.dart';
import 'package:zorvian/features/admin/providers/user_management_provider.dart';
import 'package:zorvian/features/admin/providers/user_provider.dart';

/// `UserListPage` (: asignación de rol — incluye los roles PERSONALIZADOS
/// creados en `/admin/roles` — y gating por permisos granulares).

class _FixedAuthNotifier extends AuthNotifier {
  final AuthState _fixedState;
  _FixedAuthNotifier(this._fixedState);

  @override
  AuthState build() => _fixedState;
}

class _MockUserNotifier extends UserNotifier {
  _MockUserNotifier(this.users);

  final List<UserModel> users;
  final List<(String userId, String roleId)> roleUpdates = [];

  @override
  Future<List<UserModel>> build() async => users;

  @override
  Future<void> load() async {
    state = AsyncValue.data(users);
  }

  @override
  Future<void> updateUserRole(String userId, String roleId) async {
    roleUpdates.add((userId, roleId));
    state = AsyncValue.data(users);
  }
}

class _MockRolesNotifier extends RolesNotifier {
  @override
  Future<List<RoleOption>> build() async => const [
        RoleOption(
          id: 'r-admin',
          displayName: 'Admin',
          name: 'CompanyAdmin',
          isSystem: true,
        ),
        RoleOption(
          id: 'r-custom',
          displayName: 'Auditor',
          name: 'Custom',
          isSystem: false,
        ),
        RoleOption(
          id: 'r-super',
          displayName: 'Super Admin',
          name: 'SuperAdmin',
          isSystem: true,
        ),
      ];
}

final _users = [
  UserModel(
    id: 'u-1',
    displayName: 'Ana Pérez',
    email: 'ana@zorvian.com',
    isActive: true,
    roles: const ['Admin'],
  ),
];

AuthState _auth(List<String>? permissions, {String role = 'CompanyAdmin'}) =>
    AuthState(
      status: AuthStatus.authenticated,
      userId: 'me',
      displayName: 'Test',
      email: 'test@zorvian.com',
      role: role,
      permissions: permissions,
    );

Future<_MockUserNotifier> _pump(
  WidgetTester tester,
  List<String>? permissions, {
  String role = 'CompanyAdmin',
  Size size = const Size(1600, 1200),
}) async {
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  final mock = _MockUserNotifier(_users);
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        authProvider
            .overrideWith(() => _FixedAuthNotifier(_auth(permissions, role: role))),
        userProvider.overrideWith(() => mock),
        rolesProvider.overrideWith(_MockRolesNotifier.new),
      ],
      child: const MaterialApp(home: UserListPage()),
    ),
  );
  await tester.pumpAndSettle();
  return mock;
}

void main() {
  testWidgets('renders correctly', (tester) async {
    await _pump(tester, ['user.read', 'user.manage', 'user.write']);
    expect(find.byType(UserListPage), findsOneWidget);
    // Columna "Rol" con los roles actuales del usuario.
    expect(find.text('Admin'), findsOneWidget);
  });

  testWidgets('la acción de asignar rol exige user.manage', (tester) async {
    await _pump(tester, ['user.read']);
    expect(find.byIcon(Icons.manage_accounts_outlined), findsNothing);
    // Y el botón de invitar exige user.write.
    expect(find.text('Invitar Usuario'), findsNothing);
  });

  testWidgets('con user.manage + user.write se ofrecen ambas acciones',
      (tester) async {
    await _pump(tester, ['user.read', 'user.manage', 'user.write']);
    expect(find.byIcon(Icons.manage_accounts_outlined), findsOneWidget);
    expect(find.text('Invitar Usuario'), findsOneWidget);
  });

  testWidgets('asigna un rol personalizado al usuario', (tester) async {
    final mock = await _pump(tester, ['user.read', 'user.manage']);

    await tester.tap(find.byIcon(Icons.manage_accounts_outlined));
    await tester.pumpAndSettle();

    expect(find.text('Asignar rol'), findsOneWidget);
    // El desplegable arranca en el rol ACTUAL del usuario.
    expect(find.text('Admin'), findsWidgets);

    await tester.tap(find.byType(DropdownButtonFormField<String>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Auditor (personalizado)').last);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Guardar'));
    await tester.pumpAndSettle();

    expect(mock.roleUpdates, contains(('u-1', 'r-custom')));
    // El SnackBar de confirmación deja un timer: dejarlo expirar.
    await tester.pump(const Duration(seconds: 5));
  });

  testWidgets('un no-SuperAdmin no puede asignar el rol de plataforma',
      (tester) async {
    await _pump(tester, ['user.read', 'user.manage']);

    await tester.tap(find.byIcon(Icons.manage_accounts_outlined));
    await tester.pumpAndSettle();

    // El desplegable no ofrece SuperAdmin (el backend lo rechazaría).
    await tester.tap(find.byType(DropdownButtonFormField<String>));
    await tester.pumpAndSettle();
    expect(find.text('Super Admin (sistema)'), findsNothing);
    expect(find.text('Super Admin'), findsNothing);
  });

  testWidgets('a 360 px el listado pasa a tarjetas sin desbordar',
      (tester) async {
    await _pump(
      tester,
      ['user.read', 'user.manage', 'user.write'],
      size: const Size(360, 900),
    );

    expect(find.byType(UserListPage), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
