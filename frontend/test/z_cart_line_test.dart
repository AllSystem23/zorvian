import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/shared/ds/ds.dart';

import 'helpers/viewport.dart';

const _longName = 'Aceite de cocina vegetal premium 3.78 litros';

Widget _buildLine({
  String name = _longName,
  Widget? extra,
  VoidCallback? onRemove,
}) {
  return MaterialApp(
    home: Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(12),
        child: ZCartLine(
          title: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(name, style: const TextStyle(fontWeight: FontWeight.w600)),
              const Text('C\$ 120.00 c/u', style: TextStyle(fontSize: 11)),
            ],
          ),
          quantity: const SizedBox(
            width: 84,
            child: TextField(
              decoration: InputDecoration(labelText: 'Cant', isDense: true),
            ),
          ),
          extra: extra,
          total: const Text('C\$ 240', style: TextStyle(fontWeight: FontWeight.bold)),
          onRemove: onRemove ?? () {},
        ),
      ),
    ),
  );
}

void main() {
  group('ZCartLine', () {
    testWidgets('en teléfono el borrar va arriba y la cantidad abajo',
        (tester) async {
      await pumpAt(tester, 360, _buildLine());

      final titleBottom = tester.getBottomLeft(find.text(_longName)).dy;
      final removeCenter = tester.getCenter(find.byIcon(Icons.close)).dy;
      final qtyTop = tester.getTopLeft(find.text('Cant')).dy;
      final totalTop = tester.getTopLeft(find.text('C\$ 240')).dy;

      // El botón de borrar acompaña al nombre, no al resto de controles.
      expect(removeCenter, lessThan(titleBottom));
      // Cantidad y total forman la segunda fila.
      expect(qtyTop, greaterThan(titleBottom));
      expect(totalTop, greaterThan(titleBottom));
      // Las dos filas quedan compactas (el campo no se estira al alto libre).
      expect(tester.getSize(find.byType(ZCartLine)).height, lessThan(200));
    });

    testWidgets('en escritorio comparte una sola fila', (tester) async {
      await pumpAt(tester, 900, _buildLine());

      final titleCenter = tester.getCenter(find.text(_longName)).dy;
      final qtyCenter = tester.getCenter(find.text('Cant')).dy;
      final totalCenter = tester.getCenter(find.text('C\$ 240')).dy;
      final removeCenter = tester.getCenter(find.byIcon(Icons.close)).dy;

      expect(qtyCenter, closeTo(titleCenter, 20));
      expect(totalCenter, closeTo(titleCenter, 20));
      expect(removeCenter, closeTo(titleCenter, 20));
      // La línea no se estira al alto disponible (`IntrinsicHeight`).
      expect(tester.getSize(find.byType(ZCartLine)).height, lessThan(120));
    });

    testWidgets('no desborda en 360, 390 ni 430 px', (tester) async {
      await pumpAcrossPhones(tester, (_) => _buildLine());
    });

    testWidgets('con control extra tampoco desborda en teléfono',
        (tester) async {
      await pumpAcrossPhones(
        tester,
        (_) => _buildLine(
          extra: const SizedBox(
            width: 80,
            child: TextField(
              decoration: InputDecoration(labelText: 'Desc', isDense: true),
            ),
          ),
        ),
      );

      await pumpAt(
        tester,
        360,
        _buildLine(extra: const SizedBox(width: 80, child: Text('extra'))),
      );
      final titleBottom = tester.getBottomLeft(find.text(_longName)).dy;
      expect(
        tester.getCenter(find.text('extra')).dy,
        greaterThan(titleBottom),
      );
    });

    testWidgets('el borrar dispara onRemove', (tester) async {
      var removed = 0;
      await pumpAt(tester, 360, _buildLine(onRemove: () => removed++));

      await tester.tap(find.byIcon(Icons.close));
      await tester.pump();

      expect(removed, 1);
    });

    testWidgets('un nombre muy largo no desborda a 360 px', (tester) async {
      await pumpAt(
        tester,
        360,
        _buildLine(name: 'Producto con un nombre absurdamente largo que no '
            'cabe en una sola línea de teléfono ni de lejos'),
      );

      final titleWidth = tester.getSize(find.textContaining('absurdamente')).width;
      expect(titleWidth, lessThanOrEqualTo(360));
    });
  });
}
