import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/core/offline/sync_engine.dart' show appDatabaseProvider;
import 'package:zorvian/features/admin/pages/invitation_form_page.dart';
import 'package:zorvian/features/approval/pages/approval_flow_form_page.dart';
import 'package:zorvian/features/brands/pages/brand_form_page.dart';
import 'package:zorvian/features/budgets/pages/budget_form_page.dart';
import 'package:zorvian/features/categories/pages/category_form_page.dart';
import 'package:zorvian/features/cost_centers/pages/cost_center_form_page.dart';
import 'package:zorvian/features/crm/pages/lead_form_page.dart';
import 'package:zorvian/features/crm/pages/opportunity_form_page.dart';
import 'package:zorvian/features/departments/pages/department_form_page.dart';
import 'package:zorvian/features/exchange_rates/pages/exchange_rate_form_page.dart';
import 'package:zorvian/features/providers/pages/contract_form_page.dart';
import 'package:zorvian/features/providers/pages/provider_form_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/offline_db.dart';
import 'helpers/viewport.dart';

/// Formularios de administración, catálogos menores, CRM y proveedores en
/// modo creación a 360/390/430 px.
void main() {
  final forms = <String, Widget Function()>{
    'presupuesto': () => const BudgetFormPage(),
    'flujo de aprobación': () => const ApprovalFlowFormPage(),
    'centro de costo': () => const CostCenterFormPage(),
    'departamento': () => const DepartmentFormPage(),
    'categoría': () => const CategoryFormPage(),
    'marca': () => const BrandFormPage(),
    'tipo de cambio': () => const ExchangeRateFormPage(),
    'invitación': () => const InvitationFormPage(),
    'contrato de proveedor': () => const ContractFormPage(),
    'proveedor': () => const ProviderFormPage(),
    'lead': () => const LeadFormPage(),
    'oportunidad': () => const OpportunityFormPage(),
  };

  for (final entry in forms.entries) {
    testWidgets('formulario de ${entry.key} cabe en teléfono', (tester) async {
      final api = FakeApi.routes(
        const {},
        fallback: const {
          'items': [],
          'totalCount': 0,
          'page': 1,
          'pageSize': 50,
          'totalPages': 0,
        },
      );
      final db = memoryDb();
      addTearDown(db.close);

      await pumpAcrossPhones(
        tester,
        (_) => ProviderScope(
          overrides: [
            dioClientProvider.overrideWithValue(api.client),
            appDatabaseProvider.overrideWithValue(db),
          ],
          child: MaterialApp(home: entry.value()),
        ),
        height: 1600,
      );

      // Al menos un campo: descarta que solo se haya montado el encabezado o
      // un estado de error (flujo de aprobación e invitación tienen uno solo).
      expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
    });
  }
}
