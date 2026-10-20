import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/crm/models/crm_models.dart';
import 'package:zorvian/features/crm/providers/opportunities_provider.dart';
import 'package:zorvian/features/crm/widgets/kanban_board.dart';

class _FakeOpportunities extends OpportunityNotifier {
  _FakeOpportunities(this.seed);

  final OpportunityState seed;
  final List<({String id, String stageId})> moves = [];

  @override
  OpportunityState build() => seed;

  @override
  Future<bool> updateOpportunityStage(
    String opportunityId,
    String newStageId,
  ) async {
    moves.add((id: opportunityId, stageId: newStageId));
    return true;
  }
}

PipelineStage _stage(String id, String name, String color) =>
    PipelineStage(id: id, name: name, order: 1, color: color);

Opportunity _opportunity({
  required String id,
  required String title,
  required String stageId,
  double value = 1000,
}) =>
    Opportunity(
      id: id,
      title: title,
      expectedValue: value,
      currencyCode: 'NIO',
      expectedCloseDate: DateTime(2026, 11, 15),
      probability: 60,
      stageId: stageId,
      status: 'open',
      priority: 'high',
      clientName: 'Cliente $id',
      createdAt: DateTime(2026, 10, 1),
    );

OpportunityState _seed() => OpportunityState(
      stages: [
        _stage('s1', 'Prospección', '#00E5FF'),
        _stage('s2', 'Negociación', '#7C4DFF'),
        _stage('s3', 'Ganada', '#2EE59D'),
      ],
      opportunities: [
        _opportunity(id: 'o1', title: 'Licencia anual', stageId: 's1'),
        _opportunity(id: 'o2', title: 'Ampliación', stageId: 's2'),
      ],
    );

Future<_FakeOpportunities> _pumpBoard(
  WidgetTester tester, {
  required Size size,
}) async {
  addTearDown(tester.view.reset);
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1.0;

  final fake = _FakeOpportunities(_seed());
  await tester.pumpWidget(
    ProviderScope(
      overrides: [opportunitiesProvider.overrideWith(() => fake)],
      child: const MaterialApp(home: Scaffold(body: KanbanBoard())),
    ),
  );
  await tester.pumpAndSettle();
  return fake;
}

void main() {
  group('KanbanBoard en teléfono', () {
    testWidgets('lista vertical por etapa, sin drag & drop', (tester) async {
      await _pumpBoard(tester, size: const Size(390, 844));

      // Etapas como secciones (no columnas de 280 px).
      expect(find.text('Prospección'), findsOneWidget);
      expect(find.text('Negociación'), findsOneWidget);
      expect(find.text('Ganada'), findsOneWidget);

      // La etapa vacía lo dice.
      expect(find.text('Sin oportunidades'), findsOneWidget);

      // Las tarjetas se ven completas.
      expect(find.text('Licencia anual'), findsOneWidget);
      expect(find.text('Ampliación'), findsOneWidget);

      // Sin arrastre en teléfono.
      expect(find.byType(LongPressDraggable<Opportunity>), findsNothing);

      // Pero se puede cambiar de etapa desde el menú.
      expect(find.byTooltip('Mover a otra etapa'), findsNWidgets(2));
    });

    testWidgets('el menú de la tarjeta mueve la oportunidad de etapa',
        (tester) async {
      final fake = await _pumpBoard(tester, size: const Size(390, 844));

      await tester.tap(find.byTooltip('Mover a otra etapa').first);
      await tester.pumpAndSettle();

      // La etapa actual no se ofrece como destino.
      expect(find.text('Prospección'), findsNWidgets(1));

      await tester.tap(find.text('Ganada').last);
      await tester.pumpAndSettle();

      expect(fake.moves, hasLength(1));
      expect(fake.moves.single.id, 'o1');
      expect(fake.moves.single.stageId, 's3');
    });
  });

  group('KanbanBoard en escritorio', () {
    testWidgets('conserva las columnas con drag & drop', (tester) async {
      await _pumpBoard(tester, size: const Size(1280, 900));

      expect(find.byType(LongPressDraggable<Opportunity>), findsNWidgets(2));
      expect(find.text('Sin oportunidades'), findsNothing);
    });
  });
}
