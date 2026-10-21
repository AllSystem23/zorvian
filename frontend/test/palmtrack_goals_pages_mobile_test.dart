import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/auth/auth_provider.dart' show dioClientProvider;
import 'package:zorvian/core/offline/sync_engine.dart' show appDatabaseProvider;
import 'package:zorvian/features/documents/pages/quick_generate_wizard_page.dart';
import 'package:zorvian/features/documents/pages/template_editor_page.dart';
import 'package:zorvian/features/goals/configurator/goals_config_screen.dart';
import 'package:zorvian/features/goals/dashboard/goals_dashboard_screen.dart';
import 'package:zorvian/features/goals/portal/my_goals_screen.dart';
import 'package:zorvian/features/palmtrack/pages/palmtrack_complete_profile_page.dart';
import 'package:zorvian/features/palmtrack/pages/palmtrack_conflicts_page.dart';
import 'package:zorvian/features/palmtrack/pages/palmtrack_farms_page.dart';
import 'package:zorvian/features/palmtrack/pages/palmtrack_inventory_page.dart';
import 'package:zorvian/features/palmtrack/pages/palmtrack_lots_page.dart';
import 'package:zorvian/features/palmtrack/pages/palmtrack_producers_page.dart';
import 'package:zorvian/features/palmtrack/pages/palmtrack_production_page.dart';
import 'package:zorvian/features/palmtrack/pages/palmtrack_sales_page.dart';
import 'package:zorvian/features/palmtrack/pages/palmtrack_settings_page.dart';
import 'package:zorvian/features/palmtrack/pages/palmtrack_sync_page.dart';
import 'package:zorvian/features/warranties/pages/warranty_dashboard_page.dart';
import 'package:zorvian/features/warranties/pages/warranty_form_page.dart';

import 'helpers/fake_api.dart';
import 'helpers/offline_db.dart';
import 'helpers/viewport.dart';

/// Página vacía para los endpoints secundarios.
const _page = {
  'items': [],
  'total': 0,
  'totalCount': 0,
  'page': 1,
  'pageSize': 20,
  'totalPages': 0,
  'pagination': {'hasMore': false, 'nextCursor': null},
};

/// PalmTrack, objetivos, garantías y motor documental.
///
/// Los modelos de PalmTrack son defensivos (`as String? ?? ''`), así que basta
/// con una fila por recurso para que la pantalla pinte su layout real. Los de
/// objetivos y el formulario de garantía sí castean varias claves sin fallback.
Map<String, Object?> _routes() => const {
      // ── PalmTrack ───────────────────────────────────────────────────────
      'palmtrack/farms': {
        'items': [
          {
            'id': 'f1',
            'name': 'Finca La Esperanza',
            'location': 'León, Nicaragua',
            'totalLots': 12,
            'activeLots': 9,
            'organizationId': 'org1',
          },
        ],
        'pagination': {'hasMore': false, 'nextCursor': null},
      },
      'palmtrack/lots': {
        'items': [
          {
            'id': 'lot1',
            'name': 'Lote Norte 3',
            'farmId': 'f1',
            'farmName': 'Finca La Esperanza',
            'cropVariety': 'Híbrido INTA',
            'areaHectares': 24,
            'organizationId': 'org1',
          },
        ],
        'pagination': {'hasMore': false, 'nextCursor': null},
      },
      'palmtrack/producers': {
        'items': [
          {
            'id': 'pr1',
            'name': 'Cooperativa San Juan',
            'code': 'PR-001',
            'phone': '8888-2222',
            'email': 'contacto@coopsanjuan.example',
            'totalFarms': 3,
            'organizationId': 'org1',
          },
        ],
        'pagination': {'hasMore': false, 'nextCursor': null},
      },
      'palmtrack/inventory': {
        'items': [
          {
            'id': 'inv1',
            'productName': 'Fertilizante foliar',
            'code': 'FERT-01',
            'unit': 'saco',
            'stock': 120.0,
            'unitCost': 18.5,
            'organizationId': 'org1',
          },
        ],
        'pagination': {'hasMore': false, 'nextCursor': null},
      },
      'palmtrack/production/summary': {
        'totalBunches': 1840,
        'totalWeight': 21500.5,
        'avgWeight': 11.7,
        'totalBags': 240,
        'avgBunchWeight': 11.7,
        'activeFarms': 4,
        'activeLots': 18,
        'totalProducers': 7,
        'byFarm': [
          {'farmName': 'Finca La Esperanza', 'bunches': 900, 'weight': 10500.0},
          {'farmName': 'Finca El Carmen', 'bunches': 940, 'weight': 11000.5},
        ],
        'dailyTrend': [
          {'date': '2026-10-19', 'bunches': 420, 'weight': 4900.0},
          {'date': '2026-10-20', 'bunches': 480, 'weight': 5600.0},
        ],
      },
      'palmtrack/sales-logs': {
        'items': [
          {
            'id': 'sl1',
            'clientName': 'Exportadora del Pacífico',
            'productName': 'Banano de exportación',
            'quantity': 960.0,
            'unitPrice': 6.75,
            'totalAmount': 6480.0,
            'currency': 'USD',
            'paymentMethod': 'transfer',
            'paymentStatus': 'paid',
            'date': '2026-10-18',
            'organizationId': 'org1',
          },
        ],
        'pagination': {'hasMore': false, 'nextCursor': null},
      },
      'fleet/palmtrack/stats': {
        'totalReferences': 42,
        'syncedCount': 35,
        'pendingCount': 4,
        'conflictCount': 2,
        'errorCount': 1,
        'lastSyncAt': '2026-10-20T18:30:00',
        'vehicleCount': 12,
        'driverAliasCount': 9,
      },
      'fleet/palmtrack/references': [
        {
          'id': 'ref1',
          'entityType': 'Vehicle',
          'entityId': 'veh1',
          'externalId': 'PT-0001',
          'syncDirection': 'bidirectional',
          'status': 'synced',
          'lastSyncAt': '2026-10-20T18:30:00',
          'consecutiveFailures': 0,
        },
      ],
      'palm/webhooks/dlq': [
        {
          'id': 'wh1',
          'event': 'palmtrack.weighing.created',
          'receivedAt': '2026-10-20T18:31:00',
          'success': false,
          'attempt': 3,
          'maxRetries': 3,
          'httpStatusCode': 500,
          'errorMessage': 'Timeout del destino',
          'durationMs': 2500,
        },
      ],
      'fleet/palmtrack/conflicts': [
        {
          'id': 'cf1',
          'entityType': 'Vehicle',
          'entityId': 'veh1',
          'externalId': 'PT-0001',
          'status': 'conflict',
          'consecutiveFailures': 3,
          'lastError': 'El registro externo cambió en ambos lados',
          'entityTypeLabel': 'Vehículo',
          'entityIdLabel': 'Camión 12',
        },
      ],
      'settings/palmtrack/feature-flags': {
        'moduleEnabled': true,
        'ssoEnabled': true,
        'ssoAutoCreateUsers': false,
        'ssoPropagateRoles': false,
        'ssoSharedProject': false,
      },
      // ── Objetivos ───────────────────────────────────────────────────────
      'goals/definitions': [
        {
          'id': 'gd1',
          'name': 'Ventas mensuales',
          'description': 'Meta de ventas del mes',
          'goalType': 'sales',
          'metricType': 'amount',
          'frequency': 'monthly',
          'evaluationPeriodDays': 30,
          'dataSource': 'sales',
          'calculationFormula': 'sum(total)',
          'status': 'active',
        },
      ],
      'goals/assignments': [
        {
          'id': 'ga1',
          'goalDefinitionId': 'gd1',
          'goalDefinition': {
            'id': 'gd1',
            'name': 'Ventas mensuales',
            'description': 'Meta de ventas del mes',
            'goalType': 'sales',
            'metricType': 'amount',
            'frequency': 'monthly',
            'evaluationPeriodDays': 30,
            'dataSource': 'sales',
            'calculationFormula': 'sum(total)',
            'status': 'active',
          },
          'employeeId': 'e1',
          'targetValue': 250000.0,
          'stretchValue': 300000.0,
          'baseLine': 100000.0,
          'weight': 40.0,
          'minimumGate': 0.5,
          'effectiveDate': '2026-10-01T00:00:00',
          'status': 'active',
        },
      ],
      'goals/dashboard': {
        'totalGoals': 6,
        'activeGoals': 4,
        'globalCompliance': 72.5,
        'incentiveBudget': 18000.0,
      },
      'goals/incentives': [],
      // ── Garantías ───────────────────────────────────────────────────────
      'warranty-dashboard/metrics': {
        'totalActive': 12,
        'registeredCount': 8,
        'inRepairCount': 3,
        'totalBreachedSla': 1,
      },
      'clients': {
        'items': [
          {'id': 'c1', 'name': 'Cliente de Prueba', 'identificationNumber': '001-010190-0001A'},
        ],
        'total': 1,
        'page': 1,
        'pageSize': 200,
        'totalPages': 1,
      },
      'products': {
        'items': [
          {'id': 'p1', 'code': 'PRD-001', 'name': 'Producto de prueba', 'sellingPrice': 120.0},
        ],
        'total': 1,
        'page': 1,
        'pageSize': 200,
        'totalPages': 1,
      },
      // ── Motor documental ────────────────────────────────────────────────
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
        'page': 1,
        'pageSize': 50,
        'totalPages': 1,
      },
    };

/// PalmTrack, objetivos, garantías y motor documental a 360/390/430 px.
void main() {
  Future<void> mount(WidgetTester tester, Widget page) async {
    final api = FakeApi.routes(_routes(), fallback: _page);
    // Catálogo de productos y clientes = offline-first (Drift): sin una BD en
    // memoria el repositorio se queda esperando y `pumpAndSettle` expira.
    final db = memoryDb();
    addTearDown(db.close);

    await pumpAcrossPhones(
      tester,
      (_) => ProviderScope(
        overrides: [
          dioClientProvider.overrideWithValue(api.client),
          appDatabaseProvider.overrideWithValue(db),
        ],
        child: MaterialApp(home: page),
      ),
      height: 1600,
    );
  }

  testWidgets('fincas de PalmTrack caben en teléfono', (tester) async {
    await mount(tester, const PalmTrackFarmsPage());

    expect(find.textContaining('Finca La Esperanza'), findsWidgets);
  });

  testWidgets('lotes de PalmTrack caben en teléfono', (tester) async {
    await mount(tester, const PalmTrackLotsPage());

    expect(find.textContaining('Lote Norte 3'), findsWidgets);
  });

  testWidgets('productores de PalmTrack caben en teléfono', (tester) async {
    await mount(tester, const PalmTrackProducersPage());

    expect(find.textContaining('Cooperativa San Juan'), findsWidgets);
  });

  testWidgets('inventario de PalmTrack cabe en teléfono', (tester) async {
    await mount(tester, const PalmTrackInventoryPage());

    expect(find.textContaining('Fertilizante foliar'), findsWidgets);
  });

  testWidgets('producción de PalmTrack cabe en teléfono', (tester) async {
    await mount(tester, const PalmTrackProductionPage());

    expect(find.text('Racimos Totales'), findsOneWidget);
    expect(find.textContaining('Producción por Finca'), findsWidgets);
  });

  testWidgets('ventas de PalmTrack caben en teléfono', (tester) async {
    await mount(tester, const PalmTrackSalesPage());

    expect(find.textContaining('Exportadora del Pacífico'), findsWidgets);
  });

  testWidgets('conflictos de sincronización caben en teléfono', (tester) async {
    await mount(tester, const PalmTrackConflictsPage());

    expect(find.textContaining('Camión 12'), findsWidgets);
  });

  testWidgets('sincronización de PalmTrack cabe en teléfono', (tester) async {
    await mount(tester, const PalmTrackSyncPage());

    expect(find.textContaining('PT-0001'), findsWidgets);
  });

  testWidgets('ajustes de PalmTrack caben en teléfono', (tester) async {
    await mount(tester, const PalmTrackSettingsPage());

    expect(find.textContaining('PalmTrack'), findsWidgets);
  });

  testWidgets('perfil de PalmTrack cabe en teléfono', (tester) async {
    // La pantalla lee `GoRouterState.of(context)` en `initState` para las
    // claves del SSO, así que necesita un router real (no un `MaterialApp`).
    final api = FakeApi.routes(_routes(), fallback: _page);

    await pumpAcrossPhones(
      tester,
      (_) => ProviderScope(
        overrides: [dioClientProvider.overrideWithValue(api.client)],
        child: MaterialApp.router(
          routerConfig: GoRouter(
            routes: [
              GoRoute(
                path: '/',
                builder: (_, _) => const PalmTrackCompleteProfilePage(),
              ),
              GoRoute(path: '/dashboard', builder: (_, _) => const SizedBox.shrink()),
            ],
          ),
        ),
      ),
      height: 1600,
    );

    expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
  });

  testWidgets('dashboard de objetivos cabe en teléfono', (tester) async {
    await mount(tester, const GoalsDashboardScreen());

    expect(find.text('Cumplimiento Global'), findsOneWidget);
    expect(find.text('Desempeño por Meta'), findsOneWidget);
  });

  testWidgets('mis objetivos caben en teléfono', (tester) async {
    await mount(tester, const MyGoalsScreen());

    expect(find.text('Cumplimiento Global'), findsOneWidget);
    expect(find.textContaining('Ventas mensuales'), findsWidgets);
  });

  testWidgets('configurador de objetivos cabe en teléfono', (tester) async {
    await mount(tester, const GoalsConfigScreen());

    expect(find.textContaining('Ventas mensuales'), findsWidgets);
  });

  testWidgets('dashboard de garantías cabe en teléfono', (tester) async {
    await mount(tester, const WarrantyDashboardPage());

    expect(find.textContaining('12'), findsWidgets);
  });

  testWidgets('formulario de garantía cabe en teléfono', (tester) async {
    await mount(tester, const WarrantyFormPage());

    expect(find.byType(TextFormField), findsAtLeastNWidgets(1));
  });

  testWidgets('asistente de generación rápida cabe en teléfono', (tester) async {
    await mount(tester, const QuickGenerateWizardPage());

    expect(find.textContaining('Contrato de Trabajo'), findsWidgets);
  });

  testWidgets('editor de plantillas cabe en teléfono', (tester) async {
    await mount(tester, const TemplateEditorPage());

    expect(find.text('Nueva Plantilla'), findsWidgets);
    expect(find.byType(TextField), findsAtLeastNWidgets(1));
  });
}
