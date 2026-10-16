import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/stock_transfers/providers/stock_transfer_provider.dart';
import 'package:zorvian/features/warehouses/providers/warehouse_provider.dart';

/// Modelos de bodegas (Compañía → Sucursales → Bodegas) y del flujo de
/// transferencias con bodegas + recepción parcial.
void main() {
  group('WarehouseItem', () {
    test('fromJson parses full payload', () {
      final w = WarehouseItem.fromJson({
        'id': 'w-1',
        'branchId': 'b-1',
        'branchName': 'Central',
        'name': 'Bodega Principal',
        'code': 'BP-01',
        'address': 'Calle 1',
        'isActive': true,
        'isDefault': true,
      });

      expect(w.id, 'w-1');
      expect(w.branchId, 'b-1');
      expect(w.branchName, 'Central');
      expect(w.name, 'Bodega Principal');
      expect(w.code, 'BP-01');
      expect(w.address, 'Calle 1');
      expect(w.isActive, isTrue);
      expect(w.isDefault, isTrue);
    });

    test('fromJson tolerates missing optionals with safe defaults', () {
      final w = WarehouseItem.fromJson({'id': 'w-2', 'name': 'Norte'});

      expect(w.branchId, '');
      expect(w.branchName, isNull);
      expect(w.code, isNull);
      expect(w.address, isNull);
      expect(w.isActive, isTrue, reason: 'una bodega sin dato se asume activa');
      expect(w.isDefault, isFalse);
    });
  });

  group('StockTransferItem (con bodegas)', () {
    test('fromJson parses warehouse names', () {
      final t = StockTransferItem.fromJson({
        'id': 't-1',
        'transferNumber': 'TR-20261016-0001',
        'fromBranchName': 'Central',
        'toBranchName': 'Norte',
        'fromWarehouseName': 'Bodega Central',
        'toWarehouseName': 'Bodega Norte',
        'status': 'partially_received',
        'createdAt': '2026-10-16T10:00:00Z',
      });

      expect(t.fromWarehouseName, 'Bodega Central');
      expect(t.toWarehouseName, 'Bodega Norte');
      expect(t.status, 'partially_received', reason: 'estado nuevo de recepción parcial');
    });
  });

  group('StockTransferDetailItem', () {
    test('fromJson parses quantityReceived', () {
      final d = StockTransferDetailItem.fromJson({
        'id': 'd-1',
        'productId': 'p-1',
        'productName': 'Refacción',
        'productCode': 'PRD-1',
        'quantity': 10,
        'quantityReceived': 4,
        'unitCost': 150,
      });

      expect(d.id, 'd-1');
      expect(d.quantity, 10);
      expect(d.quantityReceived, 4);
      expect(d.unitCost, 150);
    });

    test('fromJson quantityReceived defaults to 0', () {
      final d = StockTransferDetailItem.fromJson({'id': 'd-2', 'quantity': 5});
      expect(d.quantityReceived, 0);
    });
  });

  group('Wire format de transferencias', () {
    test('CreateStockTransferRequest sends warehouse ids', () {
      final body = CreateStockTransferRequest(
        fromWarehouseId: 'w-from',
        toWarehouseId: 'w-to',
        notes: 'n1',
        details: const [CreateStockTransferLine(productId: 'p-1', quantity: 2)],
      ).toJson();

      expect(body['fromWarehouseId'], 'w-from');
      expect(body['toWarehouseId'], 'w-to');
      expect(body['notes'], 'n1');
      expect(body['details'], [
        {'productId': 'p-1', 'quantity': 2},
      ]);
    });

    test('ReceiveStockTransferRequest sends detailId + quantity per line', () {
      final body = ReceiveStockTransferRequest(lines: const [
        ReceiveStockTransferLine(detailId: 'd-1', quantity: 3),
        ReceiveStockTransferLine(detailId: 'd-2', quantity: 0),
      ]).toJson();

      expect(body['lines'], [
        {'detailId': 'd-1', 'quantity': 3},
        {'detailId': 'd-2', 'quantity': 0},
      ]);
    });
  });
}
