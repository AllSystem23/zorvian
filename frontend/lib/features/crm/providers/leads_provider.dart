import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../models/crm_models.dart';

class LeadState {
  final List<Lead> leads;
  final int totalCount;
  final bool loading;
  final String? error;
  final int page;
  final String? statusFilter;

  LeadState({
    this.leads = const [],
    this.totalCount = 0,
    this.loading = false,
    this.error,
    this.page = 1,
    this.statusFilter,
  });

  LeadState copyWith({
    List<Lead>? leads,
    int? totalCount,
    bool? loading,
    String? error,
    int? page,
    String? statusFilter,
  }) {
    return LeadState(
      leads: leads ?? this.leads,
      totalCount: totalCount ?? this.totalCount,
      loading: loading ?? this.loading,
      error: error,
      page: page ?? this.page,
      statusFilter: statusFilter ?? this.statusFilter,
    );
  }
}

class LeadNotifier extends Notifier<LeadState> {
  @override
  LeadState build() => LeadState();

  Future<void> loadLeads({int page = 1, String? status}) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('crm/leads', params: {
        'page': page,
        'status': status,
        'pageSize': 20,
      });

      final data = (response.data['data'] as List)
          .map((e) => Lead.fromJson(e))
          .toList();
      final total = response.data['total'] as int;

      state = state.copyWith(
        leads: data,
        totalCount: total,
        loading: false,
        page: page,
        statusFilter: status,
      );
    } catch (e) {
      state = state.copyWith(
        error: 'Error cargando prospectos: $e',
        loading: false,
      );
    }
  }

  Future<bool> createLead(Map<String, dynamic> data) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('crm/leads', data: data);
      await loadLeads();
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error creando prospecto: $e');
      return false;
    }
  }

  Future<bool> updateLead(String id, Map<String, dynamic> data) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('zorvian/v1/crm/leads/$id', data: data);
      await loadLeads();
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error actualizando prospecto: $e');
      return false;
    }
  }

  Future<bool> deleteLead(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('zorvian/v1/crm/leads/$id');
      await loadLeads();
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error eliminando prospecto: $e');
      return false;
    }
  }
}

final leadsProvider = NotifierProvider<LeadNotifier, LeadState>(() {
  return LeadNotifier();
});
