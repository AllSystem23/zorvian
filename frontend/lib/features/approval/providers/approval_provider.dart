import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class ApprovalFlowItem {
  final String id;
  final String module;
  final String eventType;
  final String description;
  final bool isActive;
  final List<ApprovalStepItem> steps;

  const ApprovalFlowItem({required this.id, required this.module, required this.eventType, this.description = '', required this.isActive, this.steps = const []});

  factory ApprovalFlowItem.fromJson(Map<String, dynamic> j) => ApprovalFlowItem(
    id: j['id'] as String? ?? '',
    module: j['module'] as String? ?? '',
    eventType: j['eventType'] as String? ?? '',
    description: j['description'] as String? ?? '',
    isActive: j['isActive'] as bool? ?? true,
    steps: (j['steps'] as List?)?.map((s) => ApprovalStepItem.fromJson(s)).toList() ?? [],
  );
}

final class ApprovalStepItem {
  final String id;
  final int stepOrder;
  final String approverRole;
  final double? minAmount;
  final double? maxAmount;
  const ApprovalStepItem({required this.id, required this.stepOrder, required this.approverRole, this.minAmount, this.maxAmount});
  factory ApprovalStepItem.fromJson(Map<String, dynamic> j) => ApprovalStepItem(
    id: j['id'] as String? ?? '', stepOrder: j['stepOrder'] as int? ?? 0,
    approverRole: j['approverRole'] as String? ?? '',
    minAmount: (j['minAmount'] as num?)?.toDouble(),
    maxAmount: (j['maxAmount'] as num?)?.toDouble(),
  );
}

final class ApprovalRequestItem {
  final String id;
  final String module;
  final String eventType;
  final String status;
  final int currentStep;
  final int totalSteps;
  final String requestedBy;
  final String requestedAt;

  const ApprovalRequestItem({required this.id, required this.module, required this.eventType, required this.status, required this.currentStep, required this.totalSteps, required this.requestedBy, required this.requestedAt});

  factory ApprovalRequestItem.fromJson(Map<String, dynamic> j) => ApprovalRequestItem(
    id: j['id'] as String? ?? '', module: j['module'] as String? ?? '',
    eventType: j['eventType'] as String? ?? '', status: j['status'] as String? ?? '',
    currentStep: j['currentStep'] as int? ?? 0, totalSteps: j['totalSteps'] as int? ?? 0,
    requestedBy: j['requestedBy'] as String? ?? '', requestedAt: j['requestedAt'] as String? ?? '',
  );
}

final class ApprovalState {
  final List<ApprovalFlowItem> flows;
  final List<ApprovalRequestItem> pendingRequests;
  final bool loading;
  final String? error;
  const ApprovalState({this.flows = const [], this.pendingRequests = const [], this.loading = false, this.error});
  ApprovalState copyWith({List<ApprovalFlowItem>? flows, List<ApprovalRequestItem>? pendingRequests, bool? loading, String? error}) =>
    ApprovalState(flows: flows ?? this.flows, pendingRequests: pendingRequests ?? this.pendingRequests, loading: loading ?? this.loading, error: error);
}

final class ApprovalNotifier extends Notifier<ApprovalState> {
  @override
  ApprovalState build() => const ApprovalState();

  Future<void> loadFlows() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('approval-flows');
      state = ApprovalState(flows: (r.data as List).map((e) => ApprovalFlowItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar flujos', loading: false);
    }
  }

  Future<void> loadPending() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('approval-requests/pending');
      state = ApprovalState(pendingRequests: (r.data as List).map((e) => ApprovalRequestItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar solicitudes', loading: false);
    }
  }

  Future<void> deleteFlow(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('approval-flows/$id');
      loadFlows();
    } catch (_) {
      state = state.copyWith(error: 'Error al eliminar flujo');
    }
  }
}

final approvalProvider = NotifierProvider<ApprovalNotifier, ApprovalState>(ApprovalNotifier.new);
