import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../models/crm_models.dart';

class OpportunityState {
  final List<PipelineStage> stages;
  final List<Opportunity> opportunities;
  final bool loading;
  final String? error;

  OpportunityState({
    this.stages = const [],
    this.opportunities = const [],
    this.loading = false,
    this.error,
  });

  OpportunityState copyWith({
    List<PipelineStage>? stages,
    List<Opportunity>? opportunities,
    bool? loading,
    String? error,
  }) {
    return OpportunityState(
      stages: stages ?? this.stages,
      opportunities: opportunities ?? this.opportunities,
      loading: loading ?? this.loading,
      error: error,
    );
  }

  List<Opportunity> getOpportunitiesByStage(String stageId) {
    return opportunities.where((o) => o.stageId == stageId).toList();
  }
}

class OpportunityNotifier extends Notifier<OpportunityState> {
  @override
  OpportunityState build() => OpportunityState();

  Future<void> loadPipeline() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      
      // Load stages first
      final stagesRes = await dio.get('zorvian/v1/crm/pipeline-stages');
      final stages = (stagesRes.data as List)
          .map((e) => PipelineStage.fromJson(e))
          .toList();

      // Load opportunities
      final oppsRes = await dio.get('zorvian/v1/crm/opportunities');
      final opps = (oppsRes.data as List)
          .map((e) => Opportunity.fromJson(e))
          .toList();

      state = state.copyWith(
        stages: stages,
        opportunities: opps,
        loading: false,
      );
    } catch (e) {
      state = state.copyWith(
        error: 'Error cargando el pipeline: $e',
        loading: false,
      );
    }
  }

  Future<bool> createOpportunity(Map<String, dynamic> data) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('zorvian/v1/crm/opportunities', data: data);
      await loadPipeline();
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error creando oportunidad: $e');
      return false;
    }
  }

  Future<bool> updateOpportunityStage(String opportunityId, String newStageId) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('zorvian/v1/crm/opportunities/$opportunityId', data: {
        'stageId': newStageId,
      });
      await loadPipeline();
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error al mover oportunidad: $e');
      return false;
    }
  }

  Future<bool> updateOpportunity(String id, Map<String, dynamic> data) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('zorvian/v1/crm/opportunities/$id', data: data);
      await loadPipeline();
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error actualizando oportunidad: $e');
      return false;
    }
  }

  Future<bool> deleteOpportunity(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('zorvian/v1/crm/opportunities/$id');
      await loadPipeline();
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error eliminando oportunidad: $e');
      return false;
    }
  }
}

final opportunitiesProvider = NotifierProvider<OpportunityNotifier, OpportunityState>(() {
  return OpportunityNotifier();
});
