import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/accounting/pages/ai_assistant_page.dart';
import 'package:zorvian/features/accounting/providers/assistant_provider.dart';
import 'package:zorvian/features/admin/pages/invitation_form_page.dart';
import 'package:zorvian/features/custom_reports/pages/custom_report_builder_page.dart';
import 'package:zorvian/features/warranties/pages/warranty_list_page.dart';
import 'package:zorvian/features/warranties/providers/warranty_provider.dart';

import 'helpers/viewport.dart';

/// Barrido de pantallas completas a 360/390/430 px.
///
/// Cada página se monta con sus providers falsos y se verifica que no haya
/// overflows (`pumpAcrossPhones` falla con el ancho en el mensaje).

class _WarrantyNotifierMock extends WarrantyNotifier {
  @override
  WarrantyState build() => const WarrantyState(items: []);

  @override
  Future<void> load({int page = 1, int pageSize = 20, String? search, String? status}) async {
    state = const WarrantyState(items: []);
  }
}

class _FakeAssistantService extends AssistantService {
  _FakeAssistantService() : super(null);

  @override
  Future<void> saveFeedback(Map<String, dynamic> feedbackData) async {}
}

void main() {
  testWidgets('InvitationFormPage cabe en teléfono', (tester) async {
    await pumpAcrossPhones(
      tester,
      (_) => const ProviderScope(
        child: MaterialApp(home: InvitationFormPage()),
      ),
    );

    expect(find.byType(InvitationFormPage), findsOneWidget);
  });

  testWidgets('CustomReportBuilderPage cabe en teléfono', (tester) async {
    await pumpAcrossPhones(
      tester,
      (_) => const ProviderScope(
        child: MaterialApp(home: CustomReportBuilderPage()),
      ),
    );

    expect(find.text('Nombre del reporte'), findsOneWidget);
  });

  testWidgets('WarrantyListPage cabe en teléfono', (tester) async {
    await pumpAcrossPhones(
      tester,
      (_) => ProviderScope(
        overrides: [warrantyProvider.overrideWith(_WarrantyNotifierMock.new)],
        child: const MaterialApp(home: WarrantyListPage()),
      ),
    );

    expect(find.byType(WarrantyListPage), findsOneWidget);
  });

  testWidgets('AiAssistantPage cabe en teléfono', (tester) async {
    await pumpAcrossPhones(
      tester,
      (_) => ProviderScope(
        overrides: [
          assistantServiceProvider.overrideWithValue(_FakeAssistantService()),
        ],
        child: const MaterialApp(home: AiAssistantPage()),
      ),
    );

    expect(find.byType(AiAssistantPage), findsOneWidget);
  });
}
