import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/admin/pages/user_list_page.dart';
import 'package:zorvian/features/admin/providers/user_provider.dart';
import 'package:zorvian/features/admin/models/user_model.dart';

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
    // Verify empty state is shown (no users in mock)
    // ZAsyncRenderer shows 'No hay datos disponibles.' by default for empty lists
    expect(find.text('No hay datos disponibles.'), findsOneWidget);
  });
}

class UserNotifierMock extends UserNotifier {
  @override
  Future<List<UserModel>> build() async => [];
  @override
  Future<void> load() async {
    state = const AsyncValue.data([]);
  }
}
