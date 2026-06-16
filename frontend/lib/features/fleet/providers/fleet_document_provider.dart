import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

final class FleetDocumentItem {
  final String id;
  final String entityType;
  final String entityId;
  final String documentTypeId;
  final String documentTypeName;
  final bool documentTypeHasExpiry;
  final String documentNumber;
  final String issueDate;
  final String? expiryDate;
  final String? fileUrl;
  final String? notes;
  final String status;
  final bool alertSent;
  final String createdAt;

  const FleetDocumentItem({
    required this.id,
    required this.entityType,
    required this.entityId,
    required this.documentTypeId,
    required this.documentTypeName,
    required this.documentTypeHasExpiry,
    required this.documentNumber,
    required this.issueDate,
    this.expiryDate,
    this.fileUrl,
    this.notes,
    required this.status,
    required this.alertSent,
    required this.createdAt,
  });

  factory FleetDocumentItem.fromJson(Map<String, dynamic> j) => FleetDocumentItem(
    id: j['id'] as String,
    entityType: j['entityType'] as String? ?? '',
    entityId: j['entityId'] as String,
    documentTypeId: j['documentTypeId'] as String,
    documentTypeName: j['documentTypeName'] as String? ?? '',
    documentTypeHasExpiry: j['documentTypeHasExpiry'] as bool? ?? false,
    documentNumber: j['documentNumber'] as String? ?? '',
    issueDate: j['issueDate'] as String? ?? '',
    expiryDate: j['expiryDate'] as String?,
    fileUrl: j['fileUrl'] as String?,
    notes: j['notes'] as String?,
    status: j['status'] as String? ?? 'Valid',
    alertSent: j['alertSent'] as bool? ?? false,
    createdAt: j['createdAt'] as String? ?? '',
  );

  int get daysToExpiry {
    if (expiryDate == null) return 9999;
    final expiry = DateTime.tryParse(expiryDate!);
    if (expiry == null) return 9999;
    return expiry.difference(DateTime.now()).inDays;
  }
}

final class FleetDocumentState {
  final List<FleetDocumentItem> items;
  final bool loading;
  final String? error;

  const FleetDocumentState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  FleetDocumentState copyWith({
    List<FleetDocumentItem>? items,
    bool? loading,
    String? error,
  }) => FleetDocumentState(
    items: items ?? this.items,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

final class FleetDocumentNotifier extends Notifier<FleetDocumentState> {
  @override
  FleetDocumentState build() => const FleetDocumentState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/documents');
      final data = r.data;
      final items = (data['items'] as List)
          .map((e) => FleetDocumentItem.fromJson(e as Map<String, dynamic>))
          .toList();
      state = FleetDocumentState(items: items);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar documentos', loading: false);
    }
  }

  Future<List<FleetDocumentItem>> loadByEntity(String entityType, String entityId) async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/documents/entity/$entityType/$entityId');
      final data = r.data;
      return (data['items'] as List)
          .map((e) => FleetDocumentItem.fromJson(e as Map<String, dynamic>))
          .toList();
    } catch (_) {
      return [];
    }
  }

  Future<bool> delete(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('fleet/documents/$id');
      await load();
      return true;
    } catch (_) {
      return false;
    }
  }
}

final fleetDocumentProvider = NotifierProvider<FleetDocumentNotifier, FleetDocumentState>(FleetDocumentNotifier.new);
