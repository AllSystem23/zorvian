import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nexora/features/admin/pages/user_list_page.dart';
import 'package:nexora/features/admin/providers/user_provider.dart';

void main() {
  testWidgets('UserListPage renders correctly', (WidgetTester tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          userProvider.overrideWith(() => UserNotifierMock()),
        ],
        child: const MaterialApp(
          home: UserListPage(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    // Verify AppBar title
    expect(find.text('Gestión de Usuarios'), findsOneWidget);
    // Verify DataTable is rendered (internally used by ZDataTable)
    expect(find.byType(DataTable), findsOneWidget);
  });
}

class UserNotifierMock extends UserNotifier {
  @override
  UserState build() => const UserState(users: []);
  @override
  Future<void> load() async {
    state = const UserState(users: []);
  }
}
