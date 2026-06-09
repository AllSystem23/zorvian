import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/warranties/pages/warranty_list_page.dart';
import 'package:zorvian/features/warranties/providers/warranty_provider.dart';

void main() {
  testWidgets('WarrantyListPage renders correctly', (WidgetTester tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          warrantyProvider.overrideWith(() => WarrantyNotifierMock()),
        ],
        child: const MaterialApp(
          home: WarrantyListPage(),
        ),
      ),
    );

    // Verify AppBar title
    expect(find.text('Garantías'), findsOneWidget);
  });
}

class WarrantyNotifierMock extends WarrantyNotifier {
  @override
  WarrantyState build() => const WarrantyState(items: []);
  @override
  Future<void> load({int page = 1, int pageSize = 20}) async {
    state = const WarrantyState(items: []);
  }
}
