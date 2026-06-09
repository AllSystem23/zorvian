import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/features/custom_reports/pages/custom_report_builder_page.dart';

void main() {
  testWidgets('CustomReportBuilderPage renders initial form fields', (WidgetTester tester) async {
    await tester.pumpWidget(
      const ProviderScope(
        child: MaterialApp(home: CustomReportBuilderPage()),
      ),
    );

    // Verify basic form fields
    expect(find.text('Nombre del reporte'), findsOneWidget);
    expect(find.text('Módulo'), findsOneWidget);
    // Use widget printer to see what is actually rendered if needed
    // debugDumpApp();
  });
}
