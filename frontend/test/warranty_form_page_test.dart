import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/warranties/pages/warranty_form_page.dart';
import 'package:zorvian/shared/ds/ds.dart';

void main() {
  testWidgets('WarrantyFormPage renders correctly with section headers', (WidgetTester tester) async {
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp(
          home: Scaffold(
            body: WarrantyFormPage(),
          ),
        ),
      ),
    );
    await tester.pump();

    // Verify presence of section headers (rendered synchronously from build())
    expect(find.text('Cliente'), findsOneWidget);
    expect(find.text('Producto'), findsOneWidget);
    expect(find.text('Identificación del producto'), findsOneWidget);
    
    // Verify ZButton exists
    expect(find.byType(ZButton), findsOneWidget);

    // Let Dio connect timeouts expire so all timers resolve before test teardown
    await tester.pump(const Duration(seconds: 35));
  });
}
