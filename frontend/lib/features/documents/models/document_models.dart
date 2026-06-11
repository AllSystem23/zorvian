/// Model for document templates from the backend
class DocumentTemplate {
  final String id;
  final String name;
  final String category;
  final String content;
  final String countryCode;
  final String? module;
  final bool isActive;
  final String? version;

  const DocumentTemplate({
    required this.id,
    required this.name,
    required this.category,
    required this.content,
    required this.countryCode,
    this.module,
    this.isActive = true,
    this.version,
  });

  factory DocumentTemplate.fromJson(Map<String, dynamic> json) {
    return DocumentTemplate(
      id: json['id'] as String,
      name: json['name'] as String,
      category: json['category'] as String,
      content: json['content'] as String,
      countryCode: json['countryCode'] as String,
      module: json['module'] as String?,
      isActive: json['isActive'] as bool? ?? true,
      version: json['version'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'name': name,
    'category': category,
    'content': content,
    'countryCode': countryCode,
    'module': module,
    'isActive': isActive,
    'version': version,
  };
}

/// A generated document instance
class GeneratedDocument {
  final String id;
  final String templateId;
  final String? templateName;
  final String entityId;
  final String entityType;
  final String status; // draft, pending_signature, signed, archived
  final String name;
  final DateTime createdAt;
  final List<DocumentSignature> signatures;

  const GeneratedDocument({
    required this.id,
    required this.templateId,
    this.templateName,
    required this.entityId,
    required this.entityType,
    required this.status,
    required this.name,
    required this.createdAt,
    this.signatures = const [],
  });

  factory GeneratedDocument.fromJson(Map<String, dynamic> json) {
    return GeneratedDocument(
      id: json['id'] as String,
      templateId: json['templateId'] as String,
      templateName: json['templateName'] as String?,
      entityId: json['entityId'] as String,
      entityType: json['entityType'] as String,
      status: json['status'] as String? ?? 'draft',
      name: json['name'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String),
      signatures: (json['signatures'] as List<dynamic>?)
          ?.map((s) => DocumentSignature.fromJson(s as Map<String, dynamic>))
          .toList() ?? [],
    );
  }
}

/// Signature status for a document
class DocumentSignature {
  final String id;
  final String documentId;
  final String signerRole;
  final String status; // pending, signed, rejected
  final DateTime? signedAt;

  const DocumentSignature({
    required this.id,
    required this.documentId,
    required this.signerRole,
    required this.status,
    this.signedAt,
  });

  factory DocumentSignature.fromJson(Map<String, dynamic> json) {
    return DocumentSignature(
      id: json['id'] as String,
      documentId: json['documentId'] as String,
      signerRole: json['signerRole'] as String,
      status: json['status'] as String,
      signedAt: json['signedAt'] != null ? DateTime.parse(json['signedAt'] as String) : null,
    );
  }
}

/// Categorize documents by their visual badge color
extension DocumentStatusExtension on String {
  String get statusLabel => switch (this) {
    'draft' => 'Borrador',
    'pending_signature' => 'Firma Pendiente',
    'signed' => 'Firmado',
    'archived' => 'Archivado',
    _ => this,
  };
}

extension DocumentCategoryExtension on String {
  String get categoryLabel => switch (this) {
    'HR' => 'Recursos Humanos',
    'Sales' => 'Ventas',
    'Legal' => 'Legal',
    'Finance' => 'Finanzas',
    _ => this,
  };
}
