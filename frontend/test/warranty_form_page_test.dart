import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/warranties/pages/warranty_form_page.dart';
import 'package:zorvian/shared/ds/ds.dart';

void main() {
  testWidgets('WarrantyFormPage renders correctly with ZTextFields', (WidgetTester tester) async {
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp(
          home: Scaffold(
            body: WarrantyFormPage(),
          ),
        ),
      ),
    );

    // Verify presence of ZTextField labels (using text finders)
    expect(find.text('ID Cliente'), findsOneWidget);
    expect(find.text('ID Producto'), findsOneWidget);
    expect(find.text('Número de Serie'), findsOneWidget);
    
    // Verify ZButton exists
    expect(find.byType(ZButton), findsOneWidget);
  });
}
