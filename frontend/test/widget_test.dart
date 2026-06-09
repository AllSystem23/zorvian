import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/app/app.dart';

void main() {
  testWidgets('App renders login page', (WidgetTester tester) async {
    await tester.pumpWidget(const ProviderScope(child: ZorvianApp()));
    await tester.pump();
    expect(find.text('Iniciar Sesión'), findsOneWidget);
  });
}
