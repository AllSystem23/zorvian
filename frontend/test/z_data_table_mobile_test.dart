import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/shared/ds/ds.dart';

class _Item {
  final String name;
  final String total;
  final String status;

  const _Item(this.name, this.total, this.status);
}

const _columns = [
  ZColumn(id: 'name', label: 'Nombre'),
  ZColumn(id: 'total', label: 'Total', numeric: true),
  ZColumn(id: 'status', label: 'Estado'),
];

const _rows = [
  _Item('Ana Pérez', '120.00', 'Activo'),
  _Item('Luis Gómez', '80.50', 'Inactivo'),
];

DataRow _map(_Item item) => DataRow(
      cells: [
        DataCell(Text(item.name)),
        DataCell(Text(item.total)),
        DataCell(Text(item.status)),
      ],
    );

Widget _app({
  bool showColumnToggle = false,
  bool showPagination = false,
  ValueChanged<int>? onPageChanged,
  Widget Function(_Item item)? mobileCardBuilder,
  List<ZColumn> columns = _columns,
}) {
  return MaterialApp(
    home: Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: ZDataTable<_Item>(
          columns: columns,
          rows: _rows,
          rowMapper: _map,
          showColumnToggle: showColumnToggle,
          mobileCardBuilder: mobileCardBuilder,
          totalCount: showPagination ? 340 : null,
          pageSize: 20,
          currentPage: 1,
          onPageChanged: onPageChanged,
        ),
      ),
    ),
  );
}

Future<void> _setSize(WidgetTester tester, Size size) async {
  addTearDown(tester.view.reset);
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1.0;
}

void main() {
  group('ZDataTable en teléfono', () {
    testWidgets('renderiza tarjetas con etiqueta y valor, sin cabecera de tabla',
        (tester) async {
      await _setSize(tester, const Size(390, 844));
      await tester.pumpWidget(_app());

      // Primera columna = título de la tarjeta (sin etiqueta).
      expect(find.text('Ana Pérez'), findsOneWidget);
      expect(find.text('Luis Gómez'), findsOneWidget);
      expect(find.text('NOMBRE'), findsNothing);

      // El resto de columnas se muestran como etiqueta + valor.
      expect(find.text('TOTAL'), findsNWidgets(2));
      expect(find.text('ESTADO'), findsNWidgets(2));
      expect(find.text('120.00'), findsOneWidget);
      expect(find.text('Activo'), findsOneWidget);
    });

    testWidgets('ocultar una columna no desalinea las celdas restantes',
        (tester) async {
      await _setSize(tester, const Size(390, 844));
      await tester.pumpWidget(_app(showColumnToggle: true));

      expect(find.text('120.00'), findsOneWidget);

      await tester.tap(find.byTooltip('Columnas'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Total').last);
      await tester.pumpAndSettle();

      // Regresión: antes las celdas se indexaban por la posición FILTRADA, así
      // que "Estado" mostraba el valor de "Total" (120.00).
      expect(find.text('TOTAL'), findsNothing);
      expect(find.text('120.00'), findsNothing);
      expect(find.text('Activo'), findsOneWidget);
      expect(find.text('Inactivo'), findsOneWidget);
    });

    testWidgets('mobileCardBuilder reemplaza el renderer automático',
        (tester) async {
      await _setSize(tester, const Size(390, 844));
      await tester.pumpWidget(
        _app(mobileCardBuilder: (item) => Text('card:${item.name}')),
      );

      expect(find.text('card:Ana Pérez'), findsOneWidget);
      expect(find.text('TOTAL'), findsNothing);
      expect(find.text('120.00'), findsNothing);
    });

    testWidgets('ignora ZColumn.width (no hay scroll horizontal)', (tester) async {
      await _setSize(tester, const Size(390, 844));
      await tester.pumpWidget(
        _app(
          columns: const [
            ZColumn(id: 'name', label: 'Nombre', width: 400),
            ZColumn(id: 'total', label: 'Total', numeric: true, width: 400),
            ZColumn(id: 'status', label: 'Estado', width: 400),
          ],
        ),
      );

      // Si los anchos se aplicaran, el Row desbordaría y el test fallaría con
      // "A RenderFlex overflowed".
      expect(find.text('Ana Pérez'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('paginación compacta con rango y navegación', (tester) async {
      await _setSize(tester, const Size(390, 844));
      var page = 1;
      await tester.pumpWidget(
        _app(
          showPagination: true,
          onPageChanged: (value) => page = value,
        ),
      );

      expect(find.text('1–20 de 340'), findsOneWidget);

      await tester.tap(find.byTooltip('Siguiente'));
      await tester.pumpAndSettle();
      expect(page, 2);
    });
  });

  group('ZDataTable en escritorio', () {
    testWidgets('conserva la cabecera de columnas y la paginación por página',
        (tester) async {
      await _setSize(tester, const Size(1200, 900));
      await tester.pumpWidget(_app(showPagination: true));

      expect(find.text('NOMBRE'), findsOneWidget);
      expect(find.text('TOTAL'), findsOneWidget);
      expect(find.text('ESTADO'), findsOneWidget);
      expect(find.text('1 / 17'), findsOneWidget);

      // Los valores aparecen una sola vez (no se duplican como en tarjetas).
      expect(find.text('120.00'), findsOneWidget);
    });
  });

  group('ZDataTable sin altura acotada', () {
    testWidgets('dentro de un ListView se dibuja completo y sin scroll propio',
        (tester) async {
      await _setSize(tester, const Size(390, 844));
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                // Caso real: comparativo por sucursal del dashboard ejecutivo.
                ZDataTable<_Item>(
                  columns: _columns,
                  rows: _rows,
                  rowMapper: _map,
                ),
              ],
            ),
          ),
        ),
      );

      // Antes lanzaba "RenderFlex children have non-zero flex but incoming
      // height constraints are unbounded".
      expect(tester.takeException(), isNull);
      expect(find.text('Ana Pérez'), findsOneWidget);
      expect(find.text('Luis Gómez'), findsOneWidget);

      // La lista interna no scrollea por su cuenta (el scroll lo lleva el
      // ListView de la página).
      final innerList = tester.widget<ListView>(
        find.descendant(
          of: find.byType(ZDataTable<_Item>),
          matching: find.byType(ListView),
        ),
      );
      expect(innerList.shrinkWrap, isTrue);
      expect(innerList.physics, isA<NeverScrollableScrollPhysics>());
    });
  });
}
