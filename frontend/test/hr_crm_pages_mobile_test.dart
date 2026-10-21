import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/features/attendance/pages/attendance_history_page.dart';
import 'package:zorvian/features/attendance/pages/attendance_page.dart';
import 'package:zorvian/features/crm/pages/crm_page.dart';
import 'package:zorvian/features/documents/pages/document_center_page.dart';
import 'package:zorvian/features/permissions/pages/permission_form_page.dart';
import 'package:zorvian/features/permissions/pages/permission_list_page.dart';
import 'package:zorvian/features/reconciliations/pages/reconciliation_form_page.dart';
import 'package:zorvian/features/reconciliations/pages/reconciliation_list_page.dart';
import 'package:zorvian/features/sick_leave/pages/sick_leave_form_page.dart';
import 'package:zorvian/features/sick_leave/pages/sick_leave_list_page.dart';
import 'package:zorvian/features/vacations/pages/vacation_form_page.dart';
import 'package:zorvian/features/vacations/pages/vacation_list_page.dart';
import 'package:zorvian/features/vacations/pages/vacation_receipts_list_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/viewport.dart';

/// Página vacía para los endpoints secundarios.
const _page = {
  'items': [],
  'totalCount': 0,
  'page': 1,
  'pageSize': 20,
  'totalPages': 0,
};

/// Vacaciones, permisos, incapacidades, CRM, conciliaciones, documentos y
/// asistencia.
///
/// Los `fromJson` de estas pantallas castean varias claves sin fallback
/// (`json['id'] as String`, `response.data['total'] as int`…): las respuestas
/// llevan todas las claves para que la página pinte su layout real y no su
/// estado de error.
Map<String, Object?> _routes() => const {
      // ── Empleados (formularios de RRHH) ─────────────────────────────────
      'employees': {
        'items': [
          {
            'id': 'e1',
            'employeeCode': 'EMP-001',
            'firstName': 'Juan',
            'lastName': 'Pérez',
            'position': 'Vendedor',
            'departmentName': 'Ventas',
            'status': 'active',
          },
        ],
        'total': 1,
        'page': 1,
        'pageSize': 200,
        'totalPages': 1,
      },
      // ── Vacaciones ──────────────────────────────────────────────────────
      'vacations/balance': {
        'totalDaysPerYear': 15.0,
        'accruedDays': 12.5,
        'takenDays': 5.0,
        'pendingDays': 2.0,
        'availableDays': 5.5,
      },
      'vacations/receipts': {
        'items': [
          {
            'id': 'rv1',
            'vacationRequestId': 'v1',
            'employeeName': 'Juan Pérez',
            'receiptNumber': 'RV-20261015-0001',
            'startDate': '2026-10-05',
            'endDate': '2026-10-09',
            'businessDays': 5.0,
            'netPay': 2325.0,
            'status': 'paid',
            'paymentDate': '2026-10-15',
            'countryCode': 'NIC',
            'currency': 'NIO',
          },
        ],
        'totalCount': 1,
        'page': 1,
        'pageSize': 20,
        'totalPages': 1,
      },
      'vacations': {
        'items': [
          {
            'id': 'v1',
            'employeeId': 'e1',
            'employeeName': 'Juan Pérez',
            'employeeCode': 'EMP-001',
            'startDate': '2026-10-05T00:00:00',
            'endDate': '2026-10-09T00:00:00',
            'totalDays': 5.0,
            'businessDays': 5.0,
            'status': 'approved',
            'isAdvanced': false,
            'createdAt': '2026-09-28T10:00:00',
            'approvalSteps': [
              {
                'step': 1,
                'approverName': 'María López',
                'status': 'approved',
                'approvedAt': '2026-09-30T10:00:00',
              },
            ],
          },
        ],
        'total': 1,
        'page': 1,
        'pageSize': 20,
        'totalPages': 1,
      },
      // ── Permisos ────────────────────────────────────────────────────────
      'leave-permissions/types': [
        {
          'id': 'lt1',
          'code': 'PERSONAL',
          'name': 'Asunto personal',
          'isPaid': false,
          'requiresAttachment': false,
          'requiresApproval': true,
          'maxDaysPerRequest': 3,
          'maxDaysPerYear': 10,
        },
      ],
      'leave-permissions': {
        'items': [
          {
            'id': 'perm1',
            'employeeName': 'Juan Pérez',
            'employeeCode': 'EMP-001',
            'leaveTypeCode': 'PERSONAL',
            'leaveTypeName': 'Asunto personal',
            'startDate': '2026-10-12T00:00:00',
            'endDate': '2026-10-13T00:00:00',
            'totalDays': 2.0,
            'businessDays': 2.0,
            'reason': 'Trámite personal',
            'status': 'approved',
            'isPaid': false,
            'isHalfDay': false,
          },
        ],
        'total': 1,
        'page': 1,
        'pageSize': 20,
        'totalPages': 1,
      },
      // ── Incapacidades ───────────────────────────────────────────────────
      'sickleave/employee/e1': [
        {
          'id': 'sl1',
          'employeeId': 'e1',
          'startDate': '2026-09-01',
          'endDate': '2026-09-03',
          'days': 3,
          'reason': 'Gripe',
          'status': 'approved',
          'inssSubsidy': 1200.0,
        },
      ],
      // ── CRM ─────────────────────────────────────────────────────────────
      'crm/pipeline-stages': [
        {
          'id': 'st1',
          'name': 'Prospección',
          'order': 1,
          'description': 'Primer contacto',
          'color': '#2196F3',
        },
        {
          'id': 'st2',
          'name': 'Negociación',
          'order': 2,
          'description': 'Propuesta enviada',
          'color': '#FF9800',
        },
      ],
      'crm/opportunities': [
        {
          'id': 'o1',
          'title': 'Renovación de flota',
          'description': 'Doce camiones',
          'expectedValue': 250000.0,
          'currencyCode': 'NIO',
          'expectedCloseDate': '2026-11-30T00:00:00',
          'probability': 60,
          'stageId': 'st2',
          'stageName': 'Negociación',
          'status': 'open',
          'priority': 'high',
          'createdAt': '2026-10-01T09:00:00',
        },
      ],
      'crm/leads': {
        'data': [
          {
            'id': 'l1',
            'firstName': 'Ana',
            'lastName': 'Martínez',
            'companyName': 'Distribuidora del Sur',
            'jobTitle': 'Gerente de compras',
            'email': 'ana@example.com',
            'phone': '8888-1111',
            'whatsapp': '8888-1111',
            'city': 'Managua',
            'countryCode': 'NI',
            'source': 'referral',
            'interestLevel': 'high',
            'status': 'new',
            'notes': 'Interesada en el módulo de inventario',
          },
        ],
        'total': 1,
      },
      // ── Conciliaciones ──────────────────────────────────────────────────
      'reconciliations': {
        'items': [
          {
            'id': 'rec1',
            'bankAccountName': 'Banco de Prueba del Norte',
            'dateFrom': '2026-10-01',
            'dateTo': '2026-10-31',
            'status': 'pending',
            'totalTransactions': 12,
            'matchedCount': 8,
            'unmatchedCount': 4,
            'difference': 1500.0,
            'createdAt': '2026-10-20T09:00:00',
          },
        ],
        'total': 1,
        'page': 1,
        'pageSize': 20,
        'totalPages': 1,
      },
      'bank-accounts': [
        {
          'id': 'ba1',
          'bankName': 'Banco de Prueba del Norte',
          'accountNumber': '001-234567-0001',
          'currency': 'NIO',
          'balance': 125000.0,
          'isActive': true,
        },
      ],
      // ── Documentos ──────────────────────────────────────────────────────
      'documents/templates': {
        'items': [
          {
            'id': 't1',
            'name': 'Contrato de Trabajo',
            'category': 'Contract',
            'content': '<p>Contrato</p>',
            'countryCode': 'ALL',
            'module': 'Employee',
            'isActive': true,
            'version': '2.1',
          },
        ],
        'total': 1,
        'totalCount': 1,
        'page': 1,
        'pageSize': 50,
        'totalPages': 1,
      },
      'documents': {
        'items': [
          {
            'id': 'doc1',
            'templateId': 't1',
            'templateName': 'Contrato de Trabajo',
            'entityId': 'e1',
            'entityType': 'Employee',
            'status': 'signed',
            'name': 'Contrato de Trabajo - Juan Pérez',
            'createdAt': '2026-10-01T09:00:00',
          },
        ],
        'totalCount': 1,
        'page': 1,
        'pageSize': 50,
        'totalPages': 1,
      },
      // ── Asistencia ──────────────────────────────────────────────────────
      'attendance/my': {
        'presentDays': 18,
        'lateDays': 2,
        'absentDays': 1,
        'totalHours': 152.5,
        'averageHoursPerDay': 8.0,
        'records': [
          {
            'id': 'ar1',
            'date': '2026-10-20',
            'checkInTime': '08:00',
            'checkOutTime': '17:00',
            'status': 'present',
            'totalHours': 9.0,
          },
          {
            'id': 'ar2',
            'date': '2026-10-21',
            'checkInTime': '08:45',
            'checkOutTime': '17:10',
            'status': 'late',
            'totalHours': 8.4,
          },
        ],
      },
    };

/// RRHH, CRM, conciliaciones, documentos y asistencia a 360/390/430 px.
void main() {
  Future<void> mount(WidgetTester tester, Widget page) async {
    final api = FakeApi.routes(_routes(), fallback: _page);

    await pumpAcrossPhones(
      tester,
      (_) => ProviderScope(
        overrides: [dioClientProvider.overrideWithValue(api.client)],
        child: MaterialApp(home: page),
      ),
      height: 1600,
    );
  }

  testWidgets('listado de vacaciones cabe en teléfono', (tester) async {
    await mount(tester, const VacationListPage());

    expect(find.textContaining('Juan Pérez'), findsWidgets);
  });

  testWidgets('formulario de vacaciones cabe en teléfono', (tester) async {
    await mount(tester, const VacationFormPage());

    expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
  });

  testWidgets('recibos de vacaciones caben en teléfono', (tester) async {
    await mount(tester, const VacationReceiptsListPage());

    expect(find.textContaining('RV-20261015-0001'), findsWidgets);
  });

  testWidgets('listado de permisos cabe en teléfono', (tester) async {
    await mount(tester, const PermissionListPage());

    expect(find.textContaining('Asunto personal'), findsWidgets);
  });

  testWidgets('formulario de permisos cabe en teléfono', (tester) async {
    await mount(tester, const PermissionFormPage());

    expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
  });

  testWidgets('listado de incapacidades cabe en teléfono', (tester) async {
    await mount(tester, const SickLeaveListPage(employeeId: 'e1'));

    expect(find.textContaining('2026-09-01'), findsWidgets);
  });

  testWidgets('formulario de incapacidades cabe en teléfono', (tester) async {
    await mount(tester, const SickLeaveFormPage(employeeId: 'e1'));

    expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
  });

  testWidgets('pipeline de CRM cabe en teléfono', (tester) async {
    await mount(tester, const CRMPage());

    // La pestaña inicial es el Kanban, que en teléfono se pinta como lista
    // vertical agrupada por etapa (los leads viven en la segunda pestaña).
    expect(find.textContaining('Renovación de flota'), findsWidgets);
  });

  testWidgets('listado de conciliaciones cabe en teléfono', (tester) async {
    await mount(tester, const ReconciliationListPage());

    expect(find.textContaining('Banco de Prueba del Norte'), findsWidgets);
  });

  testWidgets('formulario de conciliación cabe en teléfono', (tester) async {
    await mount(tester, const ReconciliationFormPage());

    expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
  });

  testWidgets('centro de documentos cabe en teléfono', (tester) async {
    await mount(tester, const DocumentCenterPage());

    expect(find.textContaining('Contrato de Trabajo'), findsWidgets);
  });

  testWidgets('historial de asistencia cabe en teléfono', (tester) async {
    await mount(tester, const AttendanceHistoryPage());

    expect(find.text('Registros Diarios'), findsOneWidget);
  });

  testWidgets('marcación de asistencia cabe en teléfono', (tester) async {
    await mount(tester, const AttendancePage());

    expect(find.text('Resumen del Mes'), findsOneWidget);
  });
}
