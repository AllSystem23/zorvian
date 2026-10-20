import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/core/widgets/bi/bi.dart';

import 'helpers/viewport.dart';

/// Los gráficos de BI viven en tarjetas de dashboard que a 360 px quedan de
/// ~200 px tras los paddings. Antes desbordaban o partían el texto de la
/// leyenda en columnas de 30 px.

Widget _host(Widget child, {double width = 202, double height = 340}) {
  return MaterialApp(
    home: Scaffold(
      body: Align(
        alignment: Alignment.topLeft,
        child: SizedBox(width: width, height: height, child: child),
      ),
    ),
  );
}

List<PieChartItem> _pieItems() => [
      PieChartItem('Ventas', 50, color: Colors.blue),
      PieChartItem('Administración y Finanzas', 30, color: Colors.orange),
      PieChartItem('Bodega', 20, color: Colors.green),
    ];

void main() {
  group('BiPieChart', () {
    testWidgets('a 202 px baja la leyenda debajo de la dona', (tester) async {
      await pumpAt(tester, 360, _host(BiPieChart(items: _pieItems(), title: 'Por departamento')));

      expect(tester.takeException(), isNull);

      final pieBottom = tester.getBottomLeft(find.byType(PieChart)).dy;
      final legendTop = tester.getTopLeft(find.text('Ventas')).dy;
      expect(legendTop, greaterThan(pieBottom - 1));
    });

    testWidgets('en ancho de escritorio mantiene dona y leyenda en fila',
        (tester) async {
      await pumpAt(
        tester,
        900,
        _host(BiPieChart(items: _pieItems()), width: 520, height: 260),
      );

      expect(tester.takeException(), isNull);

      final pieCenter = tester.getCenter(find.byType(PieChart)).dy;
      final legendCenter = tester.getCenter(find.text('Ventas')).dy;
      // La primera fila de la leyenda arranca arriba, no debajo de la dona.
      expect(legendCenter, lessThan(pieCenter));
    });
  });

  group('Otros gráficos de BI', () {
    testWidgets('BiBarChart horizontal no desborda a 202 px', (tester) async {
      await pumpAt(
        tester,
        360,
        _host(BiBarChart(
          title: 'Ventas por sucursal',
          horizontal: true,
          items: [
            BarChartItem('Sucursal Managua Centro', 1200),
            BarChartItem('Sucursal León', 800),
          ],
        )),
      );
      expect(tester.takeException(), isNull);
    });

    testWidgets('BiLineChart no desborda a 202 px', (tester) async {
      await pumpAt(
        tester,
        360,
        _host(BiLineChart(
          title: 'Tendencia',
          series: [
            LineChartSeries(
              const [FlSpot(0, 10), FlSpot(1, 40), FlSpot(2, 25)],
              color: Colors.blue,
              showArea: true,
            ),
          ],
        )),
      );
      expect(tester.takeException(), isNull);
    });

    testWidgets('BiGauge entra en una tarjeta de 160×140', (tester) async {
      await pumpAt(
        tester,
        360,
        _host(
          BiGauge(value: 0.62, max: 1, label: 'Margen bruto', unit: '%'),
          width: 160,
          height: 140,
        ),
      );
      expect(tester.takeException(), isNull);
    });
  });
}
