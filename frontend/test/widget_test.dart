import 'package:flutter_test/flutter_test.dart';
import 'package:nexora/app/app.dart';

void main() {
  testWidgets('App renders login page', (WidgetTester tester) async {
    await tester.pumpWidget(const ZorvianApp());
    expect(find.text('Zorvian ERP'), findsOneWidget);
  });
}
