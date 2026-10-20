import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/core/widgets/bi/bi_kpi_card.dart';
import 'package:zorvian/shared/ds/ds.dart';

Future<void> _pumpAt360(WidgetTester tester, Widget child) async {
  addTearDown(tester.view.reset);
  tester.view.physicalSize = const Size(360, 800);
  tester.view.devicePixelRatio = 1.0;
  await tester.pumpWidget(
    MaterialApp(
      home: Scaffold(
        body: Center(
          // Ancho real de una tarjeta en una grilla de 2 columnas a 360 px.
          child: SizedBox(width: 158, child: child),
        ),
      ),
    ),
  );
}

void main() {
  group('Tarjetas de KPI a 360 px', () {
    testWidgets('ZStatCard encoge un monto largo en vez de desbordar',
        (tester) async {
      await _pumpAt360(
        tester,
        const ZStatCard(
          title: 'Ventas del mes acumuladas',
          value: 'C\$ 1,234,567.89',
          label: 'vs. mes anterior',
          icon: Icons.trending_up,
          trend: 12.5,
        ),
      );

      expect(tester.takeException(), isNull);
      expect(find.text('C\$ 1,234,567.89'), findsOneWidget);
      expect(find.text('Ventas del mes acumuladas'), findsOneWidget);
    });

    testWidgets('BiKpiCard encoge un monto largo en vez de desbordar',
        (tester) async {
      await _pumpAt360(
        tester,
        const BiKpiCard(
          label: 'Cartera total',
          value: 'C\$ 9,876,543.21',
          icon: Icons.account_balance,
          color: Colors.indigo,
          changePercent: -3.4,
        ),
      );

      expect(tester.takeException(), isNull);
      expect(find.text('C\$ 9,876,543.21'), findsOneWidget);
      expect(find.text('-3.4%'), findsOneWidget);
    });
  });
}
