import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/accounting/pages/comparative_reports_page.dart';
import 'package:zorvian/features/accounting/providers/accounting_provider.dart';

// Definimos una clase similar pero sin el network call
class FakeAccountingNotifier extends AccountingNotifier {
  @override
  AccountingState build() => const AccountingState(periods: []);
  @override
  Future<void> loadPeriods() async {}
}

final fakeAccountingProvider = NotifierProvider<FakeAccountingNotifier, AccountingState>(FakeAccountingNotifier.new);

void main() {
  testWidgets('ComparativeReportsPage renders without crashing', (WidgetTester tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          accountingProvider.overrideWith(() => FakeAccountingNotifier()),
        ],
        child: const MaterialApp(
          home: ComparativeReportsPage(),
        ),
      ),
    );
    
    // Pump and settle to allow post-frame callback
    await tester.pumpAndSettle();
    
    expect(find.text('Reportes Comparativos'), findsOneWidget);
    expect(find.text('Seleccione dos períodos diferentes para comparar'), findsOneWidget);
  });
}
