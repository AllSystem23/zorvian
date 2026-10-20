import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/shared/ds/ds.dart';

Future<void> _setSize(WidgetTester tester, Size size) async {
  addTearDown(tester.view.reset);
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1.0;
}

Widget _confirmHost(void Function(bool? value) onResult) {
  return MaterialApp(
    home: Scaffold(
      body: Builder(
        builder: (context) => Center(
          child: ElevatedButton(
            onPressed: () async {
              final result = await ZConfirmDialog.show(
                context,
                title: 'Eliminar producto',
                message: 'Esta acción no se puede deshacer.',
                isDestructive: true,
              );
              onResult(result);
            },
            child: const Text('Abrir'),
          ),
        ),
      ),
    ),
  );
}

void main() {
  group('ZConfirmDialog adaptativo', () {
    testWidgets('en teléfono se abre como hoja inferior a pantalla completa',
        (tester) async {
      await _setSize(tester, const Size(390, 844));
      bool? result;
      await tester.pumpWidget(_confirmHost((value) => result = value));

      await tester.tap(find.text('Abrir'));
      await tester.pumpAndSettle();

      expect(find.byType(Dialog), findsNothing);
      expect(find.byType(BottomSheet), findsOneWidget);
      expect(find.text('Eliminar producto'), findsOneWidget);

      await tester.tap(find.text('Confirmar'));
      await tester.pumpAndSettle();

      expect(result, isTrue);
      expect(find.text('Eliminar producto'), findsNothing);
    });

    testWidgets('en escritorio mantiene el diálogo centrado', (tester) async {
      await _setSize(tester, const Size(1200, 900));
      bool? result;
      await tester.pumpWidget(_confirmHost((value) => result = value));

      await tester.tap(find.text('Abrir'));
      await tester.pumpAndSettle();

      expect(find.byType(Dialog), findsOneWidget);
      expect(find.byType(BottomSheet), findsNothing);

      await tester.tap(find.text('Cancelar'));
      await tester.pumpAndSettle();

      expect(result, isFalse);
    });
  });

  group('ZResponsiveDialog', () {
    testWidgets('muestra título, contenido y cierra con la X', (tester) async {
      await _setSize(tester, const Size(390, 844));
      var closed = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => Center(
                child: ElevatedButton(
                  onPressed: () async {
                    await ZResponsiveDialog.show<void>(
                      context,
                      title: 'Detalle de factura',
                      icon: Icons.receipt_long_outlined,
                      child: const Text('Contenido del documento'),
                    );
                    closed = true;
                  },
                  child: const Text('Abrir'),
                ),
              ),
            ),
          ),
        ),
      );

      await tester.tap(find.text('Abrir'));
      await tester.pumpAndSettle();

      expect(find.text('Detalle de factura'), findsOneWidget);
      expect(find.text('Contenido del documento'), findsOneWidget);

      await tester.tap(find.byTooltip('Cerrar'));
      await tester.pumpAndSettle();

      expect(closed, isTrue);
    });

    testWidgets('en teléfono las acciones se apilan con la primaria arriba',
        (tester) async {
      await _setSize(tester, const Size(390, 844));
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ZStickyActionBar(
              stacked: true,
              actions: [
                TextButton(onPressed: () {}, child: const Text('Cancelar')),
                FilledButton(onPressed: () {}, child: const Text('Guardar')),
              ],
            ),
          ),
        ),
      );

      final guardarY = tester.getTopLeft(find.text('Guardar')).dy;
      final cancelarY = tester.getTopLeft(find.text('Cancelar')).dy;
      expect(guardarY, lessThan(cancelarY));
    });

    testWidgets('la barra de acciones sube con el teclado', (tester) async {
      await _setSize(tester, const Size(390, 844));
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => MediaQuery(
                data: MediaQuery.of(context).copyWith(
                  viewInsets: const EdgeInsets.only(bottom: 300),
                ),
                child: ZStickyActionBar(
                  includeViewInsets: true,
                  actions: [
                    FilledButton(
                      onPressed: () {},
                      child: const Text('Guardar'),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      );

      final animated = tester.widget<AnimatedPadding>(
        find.byType(AnimatedPadding),
      );
      expect((animated.padding as EdgeInsets).bottom, 300);
    });
  });
}
