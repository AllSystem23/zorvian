import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/shared/ds/ds.dart';

import 'helpers/viewport.dart';

Widget _buildHeader() {
  return MaterialApp(
    home: Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(12),
        child: ZPageHeader(
          title: 'Nueva Transferencia entre Bodegas',
          subtitle: 'Origen: Bodega Principal · Destino: Sucursal Centro',
          actions: [
            TextButton(onPressed: () {}, child: const Text('Cancelar')),
            TextButton(onPressed: () {}, child: const Text('Guardar')),
          ],
        ),
      ),
    ),
  );
}

void main() {
  group('ZPageHeader', () {
    testWidgets('en teléfono apila las acciones debajo del título',
        (tester) async {
      await pumpAt(tester, 360, _buildHeader());

      // El título parte en varias líneas en vez de desbordar a 360 px.
      final titleSize = tester.getSize(find.text('Nueva Transferencia entre Bodegas'));
      expect(titleSize.height, greaterThan(24));

      // Las acciones quedan por debajo del título/subtítulo.
      final titleBottom =
          tester.getBottomLeft(find.textContaining('Sucursal Centro')).dy;
      final actionTop =
          tester.getTopLeft(find.widgetWithText(TextButton, 'Guardar')).dy;
      expect(actionTop, greaterThan(titleBottom));
    });

    testWidgets('en escritorio comparte la fila con las acciones',
        (tester) async {
      await pumpAt(tester, 900, _buildHeader());

      final titleTop = tester.getTopLeft(
        find.text('Nueva Transferencia entre Bodegas'),
      ).dy;
      final actionTop =
          tester.getTopLeft(find.widgetWithText(TextButton, 'Guardar')).dy;
      expect(actionTop, lessThan(titleTop + 30));
    });

    testWidgets('sin acciones el título ocupa todo el ancho', (tester) async {
      await pumpAt(
        tester,
        360,
        const MaterialApp(
          home: Scaffold(
            body: Padding(
              padding: EdgeInsets.all(12),
              child: ZPageHeader(title: 'Bandeja de aprobaciones'),
            ),
          ),
        ),
      );

      expect(find.byType(TextButton), findsNothing);
      expect(
        tester.getSize(find.text('Bandeja de aprobaciones')).width,
        greaterThan(150),
      );
    });

    testWidgets('no desborda en 360, 390 ni 430 px', (tester) async {
      await pumpAcrossPhones(tester, (_) => _buildHeader());
    });
  });
}
