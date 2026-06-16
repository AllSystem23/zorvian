import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../models/crm_models.dart';

class ActivityState {
  final List<CrmActivity> activities;
  final bool loading;

  ActivityState({this.activities = const [], this.loading = false});

  ActivityState copyWith({List<CrmActivity>? activities, bool? loading}) {
    return ActivityState(
      activities: activities ?? this.activities,
      loading: loading ?? this.loading,
    );
  }
}

class ActivityNotifier extends Notifier<ActivityState> {
  @override
  ActivityState build() => ActivityState();

  Future<void> loadActivities(String leadId) async {
    state = state.copyWith(loading: true);
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('crm/leads/$leadId/activities');
      final data = (response.data as List)
          .map((e) => CrmActivity.fromJson(e as Map<String, dynamic>))
          .toList();
      state = state.copyWith(activities: data, loading: false);
    } catch (_) {
      state = state.copyWith(loading: false);
    }
  }

  Future<bool> addActivity(String leadId, Map<String, dynamic> data) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('crm/leads/$leadId/activities', data: data);
      await loadActivities(leadId);
      return true;
    } catch (_) {
      return false;
    }
  }
}

final activityProvider = NotifierProvider<ActivityNotifier, ActivityState>(() {
  return ActivityNotifier();
});
