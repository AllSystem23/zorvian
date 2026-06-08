import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:nexora/shared/ds/ds.dart';

void main() {
  testWidgets('ZTextField renders correctly', (WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ZTextField(
            label: 'Test Label',
            hint: 'Test Hint',
          ),
        ),
      ),
    );

    expect(find.text('Test Label'), findsOneWidget);
    expect(find.byType(TextFormField), findsOneWidget);
  });
}
