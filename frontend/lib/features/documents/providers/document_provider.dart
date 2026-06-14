import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../models/document_models.dart';

// ── State ──

class DocumentState {
  final List<DocumentTemplate> templates;
  final List<GeneratedDocument> documents;
  final bool loading;
  final String? error;

  const DocumentState({
    this.templates = const [],
    this.documents = const [],
    this.loading = false,
    this.error,
  });

  DocumentState copyWith({
    List<DocumentTemplate>? templates,
    List<GeneratedDocument>? documents,
    bool? loading,
    String? error,
  }) => DocumentState(
    templates: templates ?? this.templates,
    documents: documents ?? this.documents,
    loading: loading ?? this.loading,
    error: error,
  );
}

// ── Notifier ──

class DocumentNotifier extends Notifier<DocumentState> {
  @override
  DocumentState build() => const DocumentState();

  Future<void> loadTemplates() async {
    state = state.copyWith(loading: true);
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('documents/templates');
      final data = response.data as List;
      final templates = data.map((e) => DocumentTemplate.fromJson(e)).toList();
      state = state.copyWith(
        templates: templates,
        loading: false,
        error: null,
      );
    } catch (e) {
      state = state.copyWith(
        loading: false,
        error: e.toString(),
      );
    }
  }

  Future<void> loadDocuments() async {
    state = state.copyWith(loading: true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.get('documents');
      state = state.copyWith(documents: [], loading: false);
    } catch (e) {
      state = state.copyWith(documents: [], loading: false);
    }
  }

  Future<String?> saveTemplate(Map<String, dynamic> data) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('documents/templates', data: data);
      await loadTemplates();
      return null;
    } catch (e) {
      return 'Error al guardar la plantilla';
    }
  }

  Future<String?> quickGenerateForEmployee(String employeeId, String templateId) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('documents/quick-generate/employee-contract', data: {
        'entityId': employeeId,
        'templateId': templateId,
      });
      await loadDocuments();
      return null;
    } catch (e) {
      return 'Error al generar el documento';
    }
  }
}

// ── Providers ──

final documentProvider = NotifierProvider<DocumentNotifier, DocumentState>(
  DocumentNotifier.new,
);
