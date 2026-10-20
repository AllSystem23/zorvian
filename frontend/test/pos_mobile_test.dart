import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/pos/pages/pos_page.dart';
import 'package:zorvian/features/pos/providers/pos_provider.dart';

import 'helpers/viewport.dart';

// ── Fixtures ──

const _products = <Map<String, dynamic>>[
  {
    'id': 'p1',
    'name': 'Aceite de motor 20W-50 sintético',
    'code': 'ACE-20W50',
    'category': 'Lubricantes',
    'price': 485.75,
    'stock': 12.0,
    'minStock': 3.0,
    'taxRate': 0.15,
  },
  {
    'id': 'p2',
    'name': 'Filtro de aire para camión NPR',
    'code': 'FIL-NPR-01',
    'category': 'Repuestos',
    'price': 1250.00,
    'stock': 4.0,
    'minStock': 2.0,
    'taxRate': 0.15,
  },
  {
    'id': 'p3',
    'name': 'Batería 12V 90Ah libre de mantenimiento',
    'code': 'BAT-12V-90',
    'category': 'Repuestos',
    'price': 4890.50,
    'stock': 0.0,
    'minStock': 1.0,
    'taxRate': 0.15,
  },
  {
    'id': 'p4',
    'name': 'Grasa multipropósito 1 lb',
    'code': 'GRA-1LB',
    'category': 'Lubricantes',
    'price': 95.00,
    'stock': 40.0,
    'minStock': 10.0,
    'taxRate': 0.15,
  },
];

const _cartItems = [
  PosCartItem(
    productId: 'p1',
    name: 'Aceite de motor 20W-50 sintético',
    code: 'ACE-20W50',
    price: 485.75,
    quantity: 2,
    discount: 25.50,
    taxRate: 0.15,
    stock: 12,
  ),
  PosCartItem(
    productId: 'p2',
    name: 'Filtro de aire para camión NPR',
    code: 'FIL-NPR-01',
    price: 1250.00,
    quantity: 1,
    taxRate: 0.15,
    stock: 4,
  ),
];

// ── Notifier de prueba (carrito prellenado, sin red) ──

class _PosNotifier extends PosNotifier {
  _PosNotifier(this.initial);

  final PosState initial;

  @override
  PosState build() => initial;
}

// ── Harness ──

Widget _posApp({PosState state = const PosState()}) => ProviderScope(
      overrides: [
        posProvider.overrideWith(() => _PosNotifier(state)),
        posProductsProvider.overrideWith((ref) async => _products),
        posClientsProvider.overrideWith((ref) async => const []),
        posCashRegistersProvider
            .overrideWith((ref) async => const [{'id': 'c1', 'code': 'CAJA-01'}]),
        posCurrentPeriodProvider.overrideWith(
          (ref) async => const {'status': 'open', 'name': 'Octubre 2026', 'month': 10, 'year': 2026},
        ),
      ],
      child: const MaterialApp(home: PosPage()),
    );

void main() {
  group('PosPage', () {
    testWidgets('grilla de productos y barra del carrito caben en teléfono',
        (tester) async {
      await pumpAcrossPhones(tester, (_) => _posApp(), height: 900);

      expect(find.text('Aceite de motor 20W-50 sintético'), findsOneWidget);
      expect(find.text('Busca y agrega productos'), findsOneWidget);
      // Sin carrito la barra no muestra el total.
      expect(find.text('Cobrar \$0.00'), findsNothing);
    });

    testWidgets('agregar un producto actualiza la barra sin desbordar',
        (tester) async {
      for (final width in phoneWidths) {
        await pumpAt(tester, width, _posApp());
        expect(tester.takeException(), isNull,
            reason: 'Desborde a ${width.toInt()} px');

        await tester.tap(find.text('Aceite de motor 20W-50 sintético'));
        await tester.pumpAndSettle();

        expect(tester.takeException(), isNull,
            reason: 'Desborde al agregar a ${width.toInt()} px');
        expect(find.text('1 producto'), findsOneWidget);
      }
    });

    testWidgets('hoja del carrito con líneas y totales cabe en teléfono',
        (tester) async {
      for (final width in phoneWidths) {
        await pumpAt(
          tester,
          width,
          _posApp(state: const PosState(items: _cartItems)),
          height: 900,
        );
        expect(tester.takeException(), isNull,
            reason: 'Desborde a ${width.toInt()} px');

        await tester.tap(find.text('2 productos'));
        await tester.pumpAndSettle();

        expect(tester.takeException(), isNull,
            reason: 'Desborde de la hoja del carrito a ${width.toInt()} px');

        expect(find.text('Carrito (2)'), findsOneWidget);
        expect(find.textContaining('Cobrar'), findsOneWidget);

        // Cierre de la hoja para el siguiente ancho.
        await tester.tapAt(const Offset(10, 10));
        await tester.pumpAndSettle();
      }
    });

    testWidgets('método Crédito muestra sus campos sin desbordar',
        (tester) async {
      for (final width in phoneWidths) {
        await pumpAt(
          tester,
          width,
          _posApp(
            state: const PosState(items: _cartItems, paymentMethod: 'credit'),
          ),
          height: 900,
        );

        await tester.tap(find.text('2 productos'));
        await tester.pumpAndSettle();

        expect(tester.takeException(), isNull,
            reason: 'Desborde con crédito a ${width.toInt()} px');
        expect(find.text('Pago inicial'), findsOneWidget);
        expect(find.text('Cuotas'), findsOneWidget);
        expect(find.text('Interés %'), findsOneWidget);

        await tester.tapAt(const Offset(10, 10));
        await tester.pumpAndSettle();
      }
    });
  });
}
