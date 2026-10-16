import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/permissions/providers/permission_provider.dart';
import 'package:zorvian/features/vacations/providers/vacation_provider.dart';

/// Modelos de las 3 brechas de retenciones de RRHH:
/// - vacaciones pagadas por planilla (`paidByPayrollRunId`) — decide si el detalle muestra
///   "Pagada por Planilla" o el botón de recibo RV / "marcar como tomada".
/// - permisos de medio día (`isHalfDay`) — 0.5 días en saldo y en el prorrateo de nómina.
void main() {
  group('VacationItem.fromJson', () {
    test('parsea paidByPayrollRunId cuando la corrida de nómina la pagó', () {
      final item = VacationItem.fromJson({
        'id': 'v-1',
        'employeeName': 'Ana Test',
        'startDate': '2026-06-10',
        'endDate': '2026-06-14',
        'totalDays': 5,
        'businessDays': 5,
        'status': 'taken',
        'paidByPayrollRunId': 'run-123',
      });

      expect(item.paidByPayrollRunId, 'run-123');
      expect(item.status, 'taken');
      expect(item.businessDays, 5);
    });

    test('paidByPayrollRunId es null para vacaciones pagadas por recibo o marcadas', () {
      final item = VacationItem.fromJson({
        'id': 'v-2',
        'status': 'taken',
        'totalDays': 3,
        'businessDays': 3,
      });

      expect(item.paidByPayrollRunId, isNull);
    });

    test('aprueba approvalSteps vacío cuando no vienen en el JSON', () {
      final item = VacationItem.fromJson({'id': 'v-3', 'status': 'pending'});

      expect(item.approvalSteps, isEmpty);
      expect(item.isAdvanced, isFalse);
    });
  });

  group('PermissionItem.fromJson', () {
    test('parsea isHalfDay con 0.5 días', () {
      final item = PermissionItem.fromJson({
        'id': 'p-1',
        'leaveTypeCode': 'MEDICAL_APPT',
        'leaveTypeName': 'Cita médica',
        'startDate': '2026-06-15',
        'endDate': '2026-06-15',
        'totalDays': 0.5,
        'businessDays': 0.5,
        'status': 'approved',
        'isPaid': true,
        'isHalfDay': true,
      });

      expect(item.isHalfDay, isTrue);
      expect(item.totalDays, 0.5);
      expect(item.businessDays, 0.5);
      expect(item.isPaid, isTrue);
    });

    test('isHalfDay default false cuando el backend no lo envía', () {
      final item = PermissionItem.fromJson({
        'id': 'p-2',
        'leaveTypeCode': 'SICK',
        'leaveTypeName': 'Enfermedad',
        'startDate': '2026-06-10',
        'endDate': '2026-06-12',
        'totalDays': 3,
        'businessDays': 3,
        'status': 'pending',
      });

      expect(item.isHalfDay, isFalse);
      expect(item.isPaid, isFalse);
    });
  });
}
