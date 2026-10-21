import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/features/admin/pages/invitation_history_page.dart';
import 'package:zorvian/features/admin/pages/invite_user_page.dart';
import 'package:zorvian/features/admin/pages/super_admin_companies_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/viewport.dart';

/// Barrido de administración a 360/390/430 px: invitaciones (crear e historial)
/// y el panel de compañías del SuperAdmin.
///
/// Los tres leen de `dioClientProvider` → `FakeApi.routes(...)`.

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

List<Map<String, Object?>> _invitations() => [
      {
        'id': 'i1',
        'code': 'INV-2026-8F2A',
        'email': 'jose.castillo@tienda-brizuela.com.ni',
        'role': 'Supervisor',
        'isUsed': false,
        'createdAt': '2026-10-18T09:15:00',
        'expiresAt': '2026-10-25T09:15:00',
        'usedAt': null,
      },
      {
        'id': 'i2',
        'code': 'INV-2026-4B71',
        'email': 'maria.lopez@tienda-brizuela.com.ni',
        'role': 'Rrhh',
        'isUsed': true,
        'createdAt': '2026-10-10T14:30:00',
        'expiresAt': '2026-10-17T14:30:00',
        'usedAt': '2026-10-11T08:05:00',
      },
      {
        'id': 'i3',
        'code': 'INV-2026-19CC',
        'email': 'bodega.central@tienda-brizuela.com.ni',
        'role': 'Employee',
        'isUsed': false,
        'createdAt': '2026-09-01T10:00:00',
        'expiresAt': '2026-09-08T10:00:00',
        'usedAt': null,
      },
    ];

List<Map<String, Object?>> _companies() => [
      {
        'id': '00000000-0000-0000-0000-0000000000a1',
        'tenantId': '00000000-0000-0000-0000-0000000000a1',
        'name': 'Tienda Brizuela Romero',
        'legalName': 'Comercial Brizuela Romero S.A.',
        'email': 'admin@tienda-brizuela.com.ni',
        'phone': '+505 8888 4455',
        'country': 'Nicaragua',
        'currency': 'NIO',
        'subscriptionPlan': 'professional',
        'isActive': true,
        'logoUrl': null,
      },
      {
        'id': '00000000-0000-0000-0000-0000000000a2',
        'tenantId': '00000000-0000-0000-0000-0000000000a2',
        'name': 'Transportes del Norte',
        'legalName': 'Transportes del Norte S.A.',
        'email': 'contacto@transportesnorte.hn',
        'phone': '+504 2233 7788',
        'country': 'Honduras',
        'currency': 'HNL',
        'subscriptionPlan': 'enterprise',
        'isActive': false,
        'logoUrl': null,
      },
    ];

// ---------------------------------------------------------------------------
// Harness
// ---------------------------------------------------------------------------

Future<void> _pumpPage(WidgetTester tester, Widget app) async {
  await pumpAcrossPhones(tester, (_) => app, height: 4000);
}

Widget _app(Map<String, Object?> routes, Widget page) {
  final api = FakeApi.routes(routes);
  return ProviderScope(
    overrides: [dioClientProvider.overrideWithValue(api.client)],
    child: MaterialApp(home: page),
  );
}

void main() {
  testWidgets('InviteUserPage cabe en teléfono', (tester) async {
    await _pumpPage(tester, _app(const {}, const InviteUserPage()));

    expect(find.byType(InviteUserPage), findsOneWidget);
  });

  testWidgets('InvitationHistoryPage cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _app({'invitations': _invitations()}, const InvitationHistoryPage()),
    );

    expect(find.byType(InvitationHistoryPage), findsOneWidget);
  });

  testWidgets('SuperAdminCompaniesPage cabe en teléfono', (tester) async {
    await _pumpPage(
      tester,
      _app({'companies/all': _companies()}, const SuperAdminCompaniesPage()),
    );

    expect(find.byType(SuperAdminCompaniesPage), findsOneWidget);
  });
}
