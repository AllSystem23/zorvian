import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/shared/ds/ds.dart';

/// Drift de los `ListTile` con `onTap` dentro de un `ZCard`.
///
/// `ZCard` pinta su fondo con un `DecoratedBox`; un `ListTile` con `onTap`
/// pinta sus ink splashes sobre el `Material` ancestro más cercano, así que
/// sin un `Material` intermedio el splash queda oculto y Flutter lanza el
/// assert "ListTile background color or ink splashes may be invisible" en
/// debug (y el splash nunca se ve en release).
///
/// Regla: todo `ListTile` con `onTap` dentro de un `ZCard` va envuelto en un
/// `Material(type: MaterialType.transparency)`.
class _Violation {
  final String file;
  final int line;

  const _Violation(this.file, this.line);

  @override
  String toString() => '$file:$line';
}

int _indentOf(String line) => line.length - line.trimLeft().length;

/// Límite de widget: si al subir desde el `ListTile` se cruza otra clase u
/// otro método, el `ZCard` encontrado pertenece a otra pantalla (falso positivo).
final RegExp _classBoundary = RegExp(r'^\s*(final\s+|abstract\s+|base\s+)?class\s+\w+');
final RegExp _memberBoundary =
    RegExp(r'^\s{0,4}(Widget|void|Future<[^>]*>|String|bool|int|double|List<[^>]*>)\s+\w+\s*\(');
/// `Card(` sin el prefijo de `ZCard(` (los dos son ancestros `Material`).
final RegExp _cardAncestor = RegExp(r'(?<![\w])Card\(');

/// Lista los `ListTile` con `onTap` que cuelgan de un `ZCard` sin `Material`
/// intermedio (analiza el código fuente, no hace falta montar la página).
List<_Violation> _scanSource() {
  final violations = <_Violation>[];

  for (final entity in Directory('lib').listSync(recursive: true)) {
    if (entity is! File || !entity.path.endsWith('.dart')) continue;

    final lines = entity.readAsLinesSync();
    for (var i = 0; i < lines.length; i++) {
      if (!lines[i].contains('ListTile(')) continue;

      final tileIndent = _indentOf(lines[i]);

      // ¿El `ListTile` tiene `onTap` (o `onLongPress`)?
      var tappable = false;
      for (var j = i + 1; j < lines.length; j++) {
        final indent = _indentOf(lines[j]);
        if (indent <= tileIndent && lines[j].trim().isNotEmpty) break;
        if (lines[j].contains('onTap:') || lines[j].contains('onLongPress:')) {
          tappable = true;
          break;
        }
      }
      if (!tappable) continue;

      // ¿Hay un `Material` (o `Card`) o un `ZCard` por encima que lo envuelva?
      for (var k = i - 1; k >= 0; k--) {
        final line = lines[k];
        final indent = _indentOf(line);
        if (indent >= tileIndent) continue;
        // `Material` y `Card` son ancestros `Material`: el splash se pinta bien.
        if (line.contains('Material(') || _cardAncestor.hasMatch(line)) break;
        if (_classBoundary.hasMatch(line) || _memberBoundary.hasMatch(line)) break;
        if (line.contains('ZCard(')) {
          violations.add(_Violation(entity.path, i + 1));
          break;
        }
      }
    }
  }

  return violations;
}

void main() {
  test('ningún ListTile con onTap queda sin Material dentro de un ZCard', () {
    final violations = _scanSource();

    expect(
      violations,
      isEmpty,
      reason: 'envuelve el ListTile en Material(type: MaterialType.transparency): $violations',
    );
  });

  testWidgets('ZCard con ListTile no dispara el assert de ink splashes', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ZCard(
            child: Material(
              type: MaterialType.transparency,
              child: ListTile(title: const Text('Item'), onTap: () {}),
            ),
          ),
        ),
      ),
    );

    expect(tester.takeException(), isNull);
  });
}
