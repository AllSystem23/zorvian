import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/core/widgets/responsive_layout.dart';

const _firstNameKey = Key('field-1');
const _secondKey = Key('field-2');
const _thirdKey = Key('field-3');

Future<void> _pump(WidgetTester tester, double width) async {
  addTearDown(tester.view.reset);
  tester.view.physicalSize = Size(width, 900);
  tester.view.devicePixelRatio = 1.0;
  await tester.pumpWidget(
    const MaterialApp(
      home: Scaffold(
        body: Padding(
          padding: EdgeInsets.all(12),
          child: ResponsiveFormRow(
            children: [
              Text('Campo A', key: _firstNameKey),
              Text('Campo B', key: _secondKey),
              Text('Campo C', key: _thirdKey),
            ],
          ),
        ),
      ),
    ),
  );
}

double _top(WidgetTester tester, Key key) =>
    tester.getTopLeft(find.byKey(key)).dy;

double _width(WidgetTester tester, Key key) =>
    tester.getSize(find.byKey(key)).width;

void main() {
  group('ResponsiveFormRow', () {
    testWidgets('en teléfono (360 px) apila los campos en una columna',
        (tester) async {
      await _pump(tester, 360);

      // Sin fila: cada campo vive en su propia línea.
      expect(find.byType(Row), findsNothing);
      expect(find.byType(Column), findsOneWidget);

      final a = _top(tester, _firstNameKey);
      final b = _top(tester, _secondKey);
      final c = _top(tester, _thirdKey);
      expect(b, greaterThan(a));
      expect(c, greaterThan(b));

      // A ancho completo (Column con stretch), sin quedar a media pantalla.
      expect(_width(tester, _firstNameKey), closeTo(336, 0.5));
    });

    testWidgets('en escritorio (900 px) reparte el ancho en una fila',
        (tester) async {
      await _pump(tester, 900);

      expect(find.byType(Row), findsOneWidget);

      final a = _top(tester, _firstNameKey);
      final b = _top(tester, _secondKey);
      final c = _top(tester, _thirdKey);
      expect(b, a);
      expect(c, a);

      // Los tres campos comparten el ancho disponible por igual.
      final wa = _width(tester, _firstNameKey);
      final wb = _width(tester, _secondKey);
      final wc = _width(tester, _thirdKey);
      expect(wa, closeTo(wb, 0.5));
      expect(wb, closeTo(wc, 0.5));
      expect(wa, greaterThan(180));
    });

    testWidgets('respeta el espaciado vertical configurado en teléfono',
        (tester) async {
      addTearDown(tester.view.reset);
      tester.view.physicalSize = const Size(360, 900);
      tester.view.devicePixelRatio = 1.0;
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ResponsiveFormRow(
              spacing: 20,
              children: [
                SizedBox(height: 40, key: _firstNameKey),
                SizedBox(height: 40, key: _secondKey),
              ],
            ),
          ),
        ),
      );

      final firstBottom = tester.getBottomLeft(find.byKey(_firstNameKey)).dy;
      final secondTop = tester.getTopLeft(find.byKey(_secondKey)).dy;
      expect(secondTop - firstBottom, 20);
    });
  });
}
