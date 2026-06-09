import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/shared/ds/components/z_alert_card.dart';

void main() {
  testWidgets('ZAlertCard renders message and icon', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: ZAlertCard(message: 'Test Alert', severity: 'high'),
        ),
      ),
    );
    
    expect(find.text('Test Alert'), findsOneWidget);
    expect(find.byIcon(Icons.warning_amber_rounded), findsOneWidget);
  });
}
