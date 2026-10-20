import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/features/branches/pages/branch_form_page.dart';
import 'package:zorvian/features/clients/pages/client_form_page.dart';
import 'package:zorvian/features/employees/pages/employee_form_page.dart';
import 'package:zorvian/features/products/pages/product_form_page.dart';
import 'package:zorvian/features/suppliers/pages/supplier_form_page.dart';
import 'package:zorvian/features/warehouses/pages/warehouse_form_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/viewport.dart';

/// Formularios de catálogo en modo creación (sin `id`). Todos piden sus
/// catálogos a la API en `initState`, así que basta un `dioClientProvider`
/// falso que devuelva listas vacías.
void main() {
  final forms = <String, Widget Function()>{
    'producto': () => const ProductFormPage(),
    'cliente': () => const ClientFormPage(),
    'proveedor': () => const SupplierFormPage(),
    'bodega': () => const WarehouseFormPage(),
    'sucursal': () => const BranchFormPage(),
    'empleado': () => const EmployeeFormPage(),
  };

  for (final entry in forms.entries) {
    testWidgets('formulario de ${entry.key} cabe en teléfono', (tester) async {
      final api = FakeApi.routes({
        'countries': [],
        'tax-categories': [],
      });

      await pumpAcrossPhones(
        tester,
        (_) => ProviderScope(
          overrides: [dioClientProvider.overrideWithValue(api.client)],
          child: MaterialApp(home: entry.value()),
        ),
        height: 2000,
      );

      // Al menos 3 campos: confirma que el cuerpo del formulario se montó
      // (y no un estado de error/carga que pasaría el test sin probar nada).
      expect(find.byType(TextFormField), findsAtLeastNWidgets(3));
    });
  }
}
