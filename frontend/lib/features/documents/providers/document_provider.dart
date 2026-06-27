import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../models/document_models.dart';

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

class WizardState {
  final bool active;
  final int step;
  final String? entityType;
  final String? entityId;
  final String? entityDisplayName;
  final DocumentTemplate? selectedTemplate;
  final EntityContext? entityContext;
  final QuickGenerateResult? result;
  final bool loading;
  final String? error;

  const WizardState({
    this.active = false,
    this.step = 1,
    this.entityType,
    this.entityId,
    this.entityDisplayName,
    this.selectedTemplate,
    this.entityContext,
    this.result,
    this.loading = false,
    this.error,
  });

  WizardState copyWith({
    bool? active,
    int? step,
    String? entityType,
    String? entityId,
    String? entityDisplayName,
    DocumentTemplate? selectedTemplate,
    EntityContext? entityContext,
    QuickGenerateResult? result,
    bool? loading,
    String? error,
  }) => WizardState(
    active: active ?? this.active,
    step: step ?? this.step,
    entityType: entityType ?? this.entityType,
    entityId: entityId ?? this.entityId,
    entityDisplayName: entityDisplayName ?? this.entityDisplayName,
    selectedTemplate: selectedTemplate ?? this.selectedTemplate,
    entityContext: entityContext ?? this.entityContext,
    result: result ?? this.result,
    loading: loading ?? this.loading,
    error: error,
  );
}

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
      state = state.copyWith(loading: false, error: e.toString());
    }
  }

  Future<void> loadDocuments({int page = 1, int pageSize = 50}) async {
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('documents', params: {
        'page': page,
        'pageSize': pageSize,
      });
      final data = response.data;
      final items = (data['items'] as List)
          .map((e) => GeneratedDocument.fromJson(e))
          .toList();
      state = state.copyWith(documents: items, loading: false);
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

final documentProvider = NotifierProvider<DocumentNotifier, DocumentState>(
  DocumentNotifier.new,
);

class WizardNotifier extends Notifier<WizardState> {
  @override
  WizardState build() => const WizardState();

  void start({String? entityType, String? entityId, String? entityDisplayName}) {
    state = WizardState(
      active: true,
      step: 1,
      entityType: entityType,
      entityId: entityId,
      entityDisplayName: entityDisplayName,
    );
  }

  void cancel() => state = const WizardState();

  void selectTemplate(DocumentTemplate template) {
    state = state.copyWith(selectedTemplate: template, step: 2);
  }

  Future<void> loadEntityContext() async {
    if (state.entityType == null || state.entityId == null) return;
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('documents/entity-context', params: {
        'entityType': state.entityType,
        'entityId': state.entityId,
      });
      final ctx = EntityContext.fromJson(response.data as Map<String, dynamic>);
      state = state.copyWith(entityContext: ctx, loading: false);
    } catch (e) {
      state = state.copyWith(loading: false, error: 'Error al cargar contexto: $e');
    }
  }

  Future<void> executeQuickGenerate() async {
    if (state.selectedTemplate == null || state.entityType == null || state.entityId == null) return;
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.post('documents/quick-generate', data: {
        'entityType': state.entityType,
        'entityId': state.entityId,
        'templateId': state.selectedTemplate!.id,
      });
      final result = QuickGenerateResult.fromJson(response.data as Map<String, dynamic>);
      state = state.copyWith(result: result, step: 3, loading: false);
    } catch (e) {
      state = state.copyWith(loading: false, error: 'Error al generar: $e');
    }
  }

  void reset() => state = const WizardState();
}

final wizardProvider = NotifierProvider<WizardNotifier, WizardState>(
  WizardNotifier.new,
);
