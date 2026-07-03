import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class SickLeaveState {
  final List<dynamic> records;
  final bool loading;
  final String? error;

  SickLeaveState({this.records = const [], this.loading = false, this.error});

  SickLeaveState copyWith({List<dynamic>? records, bool? loading, String? error}) =>
      SickLeaveState(records: records ?? this.records, loading: loading ?? this.loading, error: error);
}

class SickLeaveNotifier extends Notifier<SickLeaveState> {
  @override
  SickLeaveState build() => SickLeaveState();

  Future<void> loadByEmployee(String employeeId) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final res = await dio.get('sickleave/employee/$employeeId');
      state = state.copyWith(records: res.data as List? ?? [], loading: false);
    } catch (e) {
      state = state.copyWith(error: 'Error: $e', loading: false);
    }
  }

  Future<bool> create(Map<String, dynamic> data) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('sickleave', data: data);
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error al crear: $e');
      return false;
    }
  }

  Future<bool> approve(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('sickleave/$id/approve');
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error al aprobar: $e');
      return false;
    }
  }
}

final sickLeaveProvider = NotifierProvider<SickLeaveNotifier, SickLeaveState>(SickLeaveNotifier.new);
