import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../models/document_models.dart';

// ── State ──

class DocumentState {
  final List<DocumentTemplate> templates;
  final List<GeneratedDocument> documents;
  final bool loading;
  final String? error;
  final int totalTemplates;

  const DocumentState({
    this.templates = const [],
    this.documents = const [],
    this.loading = false,
    this.error,
    this.totalTemplates = 0,
  });

  DocumentState copyWith({
    List<DocumentTemplate>? templates,
    List<GeneratedDocument>? documents,
    bool? loading,
    String? error,
    int? totalTemplates,
  }) => DocumentState(
    templates: templates ?? this.templates,
    documents: documents ?? this.documents,
    loading: loading ?? this.loading,
    error: error,
    totalTemplates: totalTemplates ?? this.totalTemplates,
  );
}

// ── Notifier ──

class DocumentNotifier extends Notifier<DocumentState> {
  @override
  DocumentState build() => const DocumentState();

  Future<void> loadTemplates({
    String? category,
    String? countryCode,
    String? search,
    String? module,
    int page = 1,
    int pageSize = 20,
  }) async {
    state = state.copyWith(loading: true);
    try {
      final dio = ref.read(dioClientProvider);
      final params = <String, dynamic>{
        'page': page,
        'pageSize': pageSize,
      };
      if (category != null) params['category'] = category;
      if (countryCode != null) params['countryCode'] = countryCode;
      if (search != null) params['search'] = search;
      if (module != null) params['module'] = module;
      final response = await dio.get('documents/templates', params: params);
      final data = response.data;
      final items = (data['items'] as List).map((e) => DocumentTemplate.fromJson(e)).toList();
      state = state.copyWith(
        templates: items,
        totalTemplates: data['total'] as int,
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

  Future<String?> generateDocument({
    required String templateId,
    required String entityId,
    required Map<String, String> variables,
  }) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('documents/generate', data: {
        'templateId': templateId,
        'entityId': entityId,
        'variables': variables,
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
