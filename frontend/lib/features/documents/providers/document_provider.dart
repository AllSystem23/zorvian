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
      await dio.get('documents/templates');
      state = state.copyWith(
        templates: _mockTemplates,
        loading: false,
        error: null,
      );
    } catch (e) {
      state = state.copyWith(
        templates: _mockTemplates,
        loading: false,
        error: null,
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

// ── Mock Data for Offline/Dev ──
final _mockTemplates = [
  const DocumentTemplate(
    id: '11111111-1111-1111-1111-111111111111',
    name: 'Contrato Laboral Estándar',
    category: 'HR',
    content: '<h1>Contrato Individual de Trabajo</h1><p><strong>TRABAJADOR:</strong> {{ Employee.FullName }}</p><p><strong>CARGO:</strong> {{ Employee.Position }}</p><p><strong>SALARIO:</strong> {{ Employee.Salary }}</p>',
    countryCode: 'ALL',
    module: 'Employee',
    version: '1.0',
  ),
  const DocumentTemplate(
    id: '22222222-2222-2222-2222-222222222222',
    name: 'Factura de Venta Corporativa',
    category: 'Sales',
    content: '<h2>FACTURA: {{ Sale.Number }}</h2><p>Cliente: {{ Sale.ClientName }}</p><p>Total: {{ Sale.Total }}</p>',
    countryCode: 'ALL',
    module: 'Sale',
    version: '1.0',
  ),
];

// ── Providers ──

final documentProvider = NotifierProvider<DocumentNotifier, DocumentState>(
  DocumentNotifier.new,
);
