import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Anchos lógicos de los teléfonos objetivo del plan móvil.
///
/// 360 = Android de gama baja (el más estrecho soportado), 390 = iPhone
/// moderno, 430 = iPhone/Android grande. Cada pantalla nueva debería probarse
/// en los tres.
const List<double> phoneWidths = [360, 390, 430];

/// Monta [child] con el ancho lógico indicado.
///
/// Fija `devicePixelRatio = 1` para que `physicalSize` sea el tamaño lógico y
/// restaura la vista al terminar el test. Cualquier `RenderFlex overflow` que
/// aparezca durante el `pump` se reporta como excepción y hace fallar el test
/// automáticamente.
Future<void> pumpAt(
  WidgetTester tester,
  double width,
  Widget child, {
  double height = 900,
  bool settle = true,
}) async {
  addTearDown(tester.view.reset);
  tester.view.devicePixelRatio = 1.0;
  tester.view.physicalSize = Size(width, height);
  await tester.pumpWidget(child);
  if (settle) await tester.pumpAndSettle();
}

/// Monta [child] en cada ancho de [phoneWidths], uno por uno.
///
/// Útil para barrer una pantalla completa: si alguno de los tres anchos
/// desborda, el test falla indicando el ancho en el mensaje.
Future<void> pumpAcrossPhones(
  WidgetTester tester,
  Widget Function(double width) build, {
  double height = 900,
}) async {
  for (final width in phoneWidths) {
    await pumpAt(tester, width, build(width), height: height);
    expect(
      tester.takeException(),
      isNull,
      reason: 'Desborde de layout a ${width.toInt()} px',
    );
  }
}
