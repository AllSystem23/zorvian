import 'dart:convert';

class TemplateVariable {
  final String key;
  final String label;
  final String type;
  final bool required;
  final String? placeholder;
  final String? defaultValue;
  final List<String>? options;

  const TemplateVariable({
    required this.key,
    required this.label,
    this.type = 'text',
    this.required = false,
    this.placeholder,
    this.defaultValue,
    this.options,
  });

  factory TemplateVariable.fromJson(Map<String, dynamic> json) {
    final rawOptions = json['options'];
    List<String>? opts;
    if (rawOptions is List) {
      opts = rawOptions.map((e) => e.toString()).toList();
    } else if (rawOptions is String && rawOptions.isNotEmpty) {
      opts = rawOptions.split(',').map((e) => e.trim()).where((e) => e.isNotEmpty).toList();
    }
    return TemplateVariable(
      key: json['key'] as String,
      label: json['label'] as String,
      type: json['type'] as String? ?? 'text',
      required: json['required'] as bool? ?? false,
      placeholder: json['placeholder'] as String?,
      defaultValue: json['default'] as String?,
      options: opts,
    );
  }
}

class DocumentTemplate {
  final String id;
  final String name;
  final String category;
  final String content;
  final String countryCode;
  final String? module;
  final bool isActive;
  final String? version;
  final List<TemplateVariable> variables;

  const DocumentTemplate({
    required this.id,
    required this.name,
    required this.category,
    required this.content,
    required this.countryCode,
    this.module,
    this.isActive = true,
    this.version,
    this.variables = const [],
  });

  factory DocumentTemplate.fromJson(Map<String, dynamic> json) {
    final rawVars = json['variables'];
    List<TemplateVariable> vars = [];
    if (rawVars is String && rawVars.isNotEmpty) {
      try {
        final parsed = _jsonDecode(rawVars);
        if (parsed is List) {
          vars = parsed.map((e) => TemplateVariable.fromJson(e as Map<String, dynamic>)).toList();
        }
      } catch (_) {}
    } else if (rawVars is List) {
      vars = rawVars.map((e) => TemplateVariable.fromJson(e as Map<String, dynamic>)).toList();
    }
    return DocumentTemplate(
      id: json['id'] as String,
      name: json['name'] as String,
      category: json['category'] as String,
      content: json['content'] as String,
      countryCode: json['countryCode'] as String,
      module: json['module'] as String?,
      isActive: json['isActive'] as bool? ?? true,
      version: json['version'] as String?,
      variables: vars,
    );
  }

  static dynamic _jsonDecode(String s) => jsonDecode(s);

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

class GeneratedDocument {
  final String id;
  final String templateId;
  final String? templateName;
  final String entityId;
  final String entityType;
  final String status;
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

class DocumentSignature {
  final String id;
  final String documentId;
  final String signerRole;
  final String status;
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

class EntityContext {
  final String entityType;
  final String entityId;
  final String displayName;
  final Map<String, String> data;

  const EntityContext({
    required this.entityType,
    required this.entityId,
    required this.displayName,
    required this.data,
  });

  factory EntityContext.fromJson(Map<String, dynamic> json) {
    final rawData = json['data'] as Map<String, dynamic>? ?? {};
    return EntityContext(
      entityType: json['entityType'] as String,
      entityId: json['entityId'] as String,
      displayName: json['displayName'] as String,
      data: rawData.map((k, v) => MapEntry(k, v.toString())),
    );
  }
}

class QuickGenerateResult {
  final String documentId;
  final String name;
  final String status;
  final DateTime createdAt;
  final String? pdfUrl;
  final String? signatureToken;

  const QuickGenerateResult({
    required this.documentId,
    required this.name,
    required this.status,
    required this.createdAt,
    this.pdfUrl,
    this.signatureToken,
  });

  factory QuickGenerateResult.fromJson(Map<String, dynamic> json) {
    return QuickGenerateResult(
      documentId: json['documentId'] as String,
      name: json['name'] as String,
      status: json['status'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String),
      pdfUrl: json['pdfUrl'] as String?,
      signatureToken: json['signatureToken'] as String?,
    );
  }
}

class DocumentDetail {
  final String id;
  final String name;
  final String entityType;
  final String status;
  final DateTime createdAt;
  final String? summary;
  final String? templateName;
  final List<DocumentVersionItem> versions;
  final List<DocumentSignatureItem> signatures;

  const DocumentDetail({
    required this.id,
    required this.name,
    required this.entityType,
    required this.status,
    required this.createdAt,
    this.summary,
    this.templateName,
    this.versions = const [],
    this.signatures = const [],
  });

  factory DocumentDetail.fromJson(Map<String, dynamic> json) {
    return DocumentDetail(
      id: json['id'] as String,
      name: json['name'] as String,
      entityType: json['entityType'] as String,
      status: json['status'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String),
      summary: json['summary'] as String?,
      templateName: json['templateName'] as String?,
      versions: (json['versions'] as List<dynamic>?)
          ?.map((v) => DocumentVersionItem.fromJson(v as Map<String, dynamic>))
          .toList() ?? [],
      signatures: (json['signatures'] as List<dynamic>?)
          ?.map((s) => DocumentSignatureItem.fromJson(s as Map<String, dynamic>))
          .toList() ?? [],
    );
  }
}

class DocumentVersionItem {
  final int versionNumber;
  final String? changesSummary;
  final DateTime createdAt;

  const DocumentVersionItem({
    required this.versionNumber,
    this.changesSummary,
    required this.createdAt,
  });

  factory DocumentVersionItem.fromJson(Map<String, dynamic> json) {
    return DocumentVersionItem(
      versionNumber: json['versionNumber'] as int,
      changesSummary: json['changesSummary'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }
}

class DocumentSignatureItem {
  final String id;
  final String signerRole;
  final String status;
  final DateTime? signedAt;

  const DocumentSignatureItem({
    required this.id,
    required this.signerRole,
    required this.status,
    this.signedAt,
  });

  factory DocumentSignatureItem.fromJson(Map<String, dynamic> json) {
    return DocumentSignatureItem(
      id: json['id'] as String,
      signerRole: json['signerRole'] as String,
      status: json['status'] as String,
      signedAt: json['signedAt'] != null ? DateTime.parse(json['signedAt'] as String) : null,
    );
  }
}

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
