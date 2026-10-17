import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/service_payments/providers/service_payment_provider.dart';

/// Modelos del Pago a Prestadores de Servicios: historial (listado), detalle
/// con líneas de recaudo y deducciones con acuerdo previo, y los payloads de
/// creación/edición que viajan al backend.
void main() {
  group('ServiceProviderPaymentItem.fromJson', () {
    test('parsea el historial con período, meta y totales', () {
      final item = ServiceProviderPaymentItem.fromJson({
        'id': 'p-1',
        'paymentNumber': 'PP-20261015-0001',
        'employeeId': 'e-1',
        'employeeName': 'María Fernández',
        'employeeCode': 'PS-001',
        'branchId': 'b-1',
        'branchName': 'Sucursal Norte',
        'periodYear': 2026,
        'periodMonth': 10,
        'periodHalf': 1,
        'periodLabel': 'Quincena 1 · Oct 2026',
        'paymentDate': '2026-10-15',
        'status': 'paid',
        'goalMet': true,
        'totalAccumulated': 23026.76,
        'totalDeductions': 13923.36,
        'netAmount': 9103.40,
        'createdAt': '2026-10-15T08:00:00Z',
      });

      expect(item.paymentNumber, 'PP-20261015-0001');
      expect(item.periodLabel, 'Quincena 1 · Oct 2026');
      expect(item.status, 'paid');
      expect(item.goalMet, isTrue);
      expect(item.totalAccumulated, 23026.76);
      expect(item.netAmount, 9103.40);
      expect(item.employeeName, 'María Fernández');
      expect(item.branchId, 'b-1', reason: 'reportería por sucursal');
      expect(item.branchName, 'Sucursal Norte');
    });

    test('aporta valores por defecto cuando faltan campos numéricos', () {
      final item = ServiceProviderPaymentItem.fromJson({'id': 'p-2'});

      expect(item.periodYear, 0);
      expect(item.goalMet, isFalse);
      expect(item.netAmount, 0);
      expect(item.employeeName, isNull);
      expect(item.branchId, isNull);
      expect(item.branchName, isNull);
    });
  });

  group('ServiceProviderPaymentDetail.fromJson', () {
    test('parsea líneas, deducciones y totales del comprobante', () {
      final detail = ServiceProviderPaymentDetail.fromJson({
        'id': 'p-3',
        'paymentNumber': 'PP-20261015-0002',
        'employeeId': 'e-1',
        'employeeName': 'María Fernández',
        'employeeCode': 'PS-001',
        'branchId': 'b-1',
        'branchName': 'Sucursal Norte',
        'periodYear': 2026,
        'periodMonth': 10,
        'periodHalf': 1,
        'periodLabel': 'Quincena 1 · Oct 2026',
        'paymentDate': '2026-10-15',
        'status': 'paid',
        'goalEvaluationMode': 'individual',
        'goalMet': true,
        'goalBonusAmount': 1000,
        'otherIncome': 0,
        'totalCollected': 550669,
        'totalContracted': 550669,
        'totalCommission': 22026.76,
        'totalAccumulated': 23026.76,
        'totalDeductions': 13923.36,
        'netAmount': 9103.40,
        'notes': 'Quincena conforme',
        'paidAt': '2026-10-15T16:00:00Z',
        'paymentReference': 'ACH-778',
        'lines': [
          {
            'id': 'l-1',
            'lineName': 'OTROS',
            'collectedAmount': 495782,
            'contractedAmount': 495782,
            'commissionRate': 4,
            'commissionAmount': 19831.28,
            'lineGoalMet': true,
          },
          {
            'id': 'l-2',
            'lineName': 'TELÉFONO',
            'collectedAmount': 54887,
            'contractedAmount': 54887,
            'commissionRate': 4,
            'commissionAmount': 2195.48,
            'lineGoalMet': true,
          },
        ],
        'deductions': [
          {
            'id': 'd-1',
            'deductionType': 'store_credit',
            'description': 'LAVADORA FRIGIDAIRE SALDO 4692',
            'amount': 5000,
            'agreementReference': 'AC-2026-02',
            'agreementDate': '2026-10-01',
          },
          {
            'id': 'd-2',
            'deductionType': 'inss',
            'amount': 350,
          },
        ],
      });

      expect(detail.lines, hasLength(2));
      expect(detail.lines[0].lineName, 'OTROS');
      expect(detail.lines[0].commissionAmount, 19831.28);
      expect(detail.lines[1].lineGoalMet, isTrue);
      expect(detail.deductions, hasLength(2));
      expect(detail.deductions[0].deductionType, 'store_credit');
      expect(detail.deductions[0].agreementReference, 'AC-2026-02');
      expect(detail.deductions[1].agreementReference, isNull);
      expect(detail.goalEvaluationMode, 'individual');
      expect(detail.paymentReference, 'ACH-778');
      expect(detail.netAmount, 9103.40);
      expect(detail.branchName, 'Sucursal Norte', reason: 'reportería por sucursal');
    });

    test('aporta listas vacías y modo total cuando no vienen', () {
      final detail = ServiceProviderPaymentDetail.fromJson({'id': 'p-4'});

      expect(detail.lines, isEmpty);
      expect(detail.deductions, isEmpty);
      expect(detail.goalEvaluationMode, 'total');
      expect(detail.goalMet, isFalse);
      expect(detail.branchName, isNull);
    });
  });

  group('Payloads de creación/edición', () {
    test('ServicePaymentLineInput serializa los campos del formulario', () {
      final input = ServicePaymentLineInput(
        lineName: 'OTROS',
        collectedAmount: 495782,
        contractedAmount: 495782,
        commissionRate: 4,
      );

      expect(input.toJson(), {
        'lineName': 'OTROS',
        'collectedAmount': 495782.0,
        'contractedAmount': 495782.0,
        'commissionRate': 4.0,
      });
    });

    test('ServicePaymentDeductionInput serializa acuerdo opcional como null', () {
      final input = ServicePaymentDeductionInput(
        deductionType: 'inss',
        description: 'APORTE INSS',
        amount: 350,
      );

      final json = input.toJson();
      expect(json['deductionType'], 'inss');
      expect(json['amount'], 350.0);
      expect(json['agreementReference'], isNull);
      expect(json['agreementDate'], isNull);
    });
  });
}
