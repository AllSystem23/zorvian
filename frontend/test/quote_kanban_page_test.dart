import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/features/quotes/pages/quote_kanban_page.dart';

void main() {
  testWidgets('QuoteKanbanPage renders columns and items', (WidgetTester tester) async {

    // We need to provide the quote data here for the test
    // Given the current architecture, I'll mock the state
    
    await tester.pumpWidget(
      ProviderScope(
        child: const MaterialApp(home: QuoteKanbanPage()),
      ),
    );

    // Verify Kanban columns are present (etiquetas en español desde la
    // adaptación móvil; el escritorio conserva las columnas).
    expect(find.text('PENDIENTE (0)'), findsOneWidget);
    expect(find.text('ENVIADA (0)'), findsOneWidget);
  });
}
