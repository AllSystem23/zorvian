import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/shared/ds/ds.dart';

import 'helpers/viewport.dart';

Widget _card(String title) => ZStatCard(
      title: title,
      value: '1.234.567,89',
      icon: Icons.local_shipping_outlined,
      variant: ZStatVariant.module,
      moduleColor: ZColors.moduleFleet,
    );

Widget _grid({double aspectRatio = 2.2}) => MaterialApp(
      home: Scaffold(
        body: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: ZStatGrid(
            phoneColumns: 2,
            tabletColumns: 4,
            desktopColumns: 5,
            aspectRatio: aspectRatio,
            children: [
              _card('Total Vehículos'),
              _card('Vencidos'),
              _card('Por Vencer'),
              _card('Riesgo Promedio'),
            ],
          ),
        ),
      ),
    );

void main() {
  testWidgets('en teléfono usa alto intrínseco y no desborda', (tester) async {
    await pumpAcrossPhones(tester, (_) => _grid());

    expect(find.byType(GridView), findsNothing);

    // Con 2 columnas a 360 px la tarjeta mide ~158 px y un `childAspectRatio`
    // de 2.2 la dejaría en 72 px, muy por debajo de los ~92 px que necesita la
    // tarjeta compacta: la rejilla le da el alto de su contenido.
    await pumpAt(tester, 360, _grid());
    final card = tester.getSize(find.byType(ZStatCard).first);
    expect(card.width, lessThan(200));
    expect(card.height, greaterThanOrEqualTo(90));
  });

  testWidgets('en escritorio conserva el GridView con aspectRatio',
      (tester) async {
    await pumpAt(tester, 1200, _grid());

    final grid = tester.widget<GridView>(find.byType(GridView));
    final delegate = grid.gridDelegate as SliverGridDelegateWithFixedCrossAxisCount;
    expect(delegate.crossAxisCount, 5);
    expect(delegate.childAspectRatio, 2.2);
    expect(tester.takeException(), isNull);
  });

  testWidgets('columnas tan angostas como para desbordar vuelven al Wrap',
      (tester) async {
    // 4 columnas a 800 px dejan la tarjeta en ~190 px: el modo compacto mide
    // ~92 px y el `childAspectRatio` 2.2 la cortaría a 87 px, así que la
    // rejilla no fuerza alto.
    await pumpAt(tester, 800, _grid());

    expect(find.byType(GridView), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('sin hijos no ocupa espacio', (tester) async {
    await pumpAt(
      tester,
      360,
      const MaterialApp(
        home: Scaffold(body: ZStatGrid(children: [])),
      ),
    );

    expect(tester.getSize(find.byType(ZStatGrid)).height, 0);
  });
}
