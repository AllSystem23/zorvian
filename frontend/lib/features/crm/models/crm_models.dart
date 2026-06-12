class Lead {
  final String id;
  final String firstName;
  final String lastName;
  final String? companyName;
  final String? jobTitle;
  final String? email;
  final String? phone;
  final String? whatsapp;
  final String? city;
  final String countryCode;
  final String? source;
  final String? interestLevel;
  final String status;
  final String? assignedToId;
  final String? notes;
  final DateTime createdAt;

  Lead({
    required this.id,
    required this.firstName,
    required this.lastName,
    this.companyName,
    this.jobTitle,
    this.email,
    this.phone,
    this.whatsapp,
    this.city,
    required this.countryCode,
    this.source,
    this.interestLevel,
    required this.status,
    this.assignedToId,
    this.notes,
    required this.createdAt,
  });

  String get fullName => '$firstName $lastName'.trim();

  factory Lead.fromJson(Map<String, dynamic> json) {
    return Lead(
      id: json['id'],
      firstName: json['firstName'],
      lastName: json['lastName'],
      companyName: json['companyName'],
      jobTitle: json['jobTitle'],
      email: json['email'],
      phone: json['phone'],
      whatsapp: json['whatsapp'],
      city: json['city'],
      countryCode: json['countryCode'] ?? 'NI',
      source: json['source'],
      interestLevel: json['interestLevel'],
      status: json['status'],
      assignedToId: json['assignedToId'],
      notes: json['notes'],
      createdAt: DateTime.parse(json['createdAt']),
    );
  }
}

class PipelineStage {
  final String id;
  final String name;
  final int order;
  final String? description;
  final String color;

  PipelineStage({
    required this.id,
    required this.name,
    required this.order,
    this.description,
    required this.color,
  });

  factory PipelineStage.fromJson(Map<String, dynamic> json) {
    return PipelineStage(
      id: json['id'],
      name: json['name'],
      order: json['order'],
      description: json['description'],
      color: json['color'] ?? '#9E9E9E',
    );
  }
}

class CrmActivity {
  final String id;
  final String leadId;
  final String type;
  final String subject;
  final String? description;
  final DateTime createdAt;

  CrmActivity({
    required this.id,
    required this.leadId,
    required this.type,
    required this.subject,
    this.description,
    required this.createdAt,
  });

  factory CrmActivity.fromJson(Map<String, dynamic> json) {
    return CrmActivity(
      id: json['id'],
      leadId: json['leadId'] ?? '',
      type: json['type'] ?? 'note',
      subject: json['subject'] ?? '',
      description: json['description'],
      createdAt: DateTime.parse(json['createdAt']),
    );
  }
}

class Opportunity {
  final String id;
  final String title;
  final String? description;
  final double expectedValue;
  final String currencyCode;
  final DateTime expectedCloseDate;
  final int probability;
  final String stageId;
  final String? stageName;
  final String status;
  final String priority;
  final String? contactPhone;
  final String? leadId;
  final String? clientId;
  final String? clientName;
  final DateTime createdAt;

  Opportunity({
    required this.id,
    required this.title,
    this.description,
    required this.expectedValue,
    required this.currencyCode,
    required this.expectedCloseDate,
    required this.probability,
    required this.stageId,
    this.stageName,
    required this.status,
    required this.priority,
    this.contactPhone,
    this.leadId,
    this.clientId,
    this.clientName,
    required this.createdAt,
  });

  factory Opportunity.fromJson(Map<String, dynamic> json) {
    return Opportunity(
      id: json['id'],
      title: json['title'],
      description: json['description'],
      expectedValue: (json['expectedValue'] as num).toDouble(),
      currencyCode: json['currencyCode'] ?? 'USD',
      expectedCloseDate: DateTime.parse(json['expectedCloseDate']),
      probability: json['probability'] ?? 0,
      stageId: json['stageId'],
      stageName: json['stageName'],
      status: json['status'] ?? 'open',
      priority: json['priority'] ?? 'medium',
      contactPhone: json['contactPhone'],
      leadId: json['leadId'],
      clientId: json['clientId'],
      clientName: json['clientName'],
      createdAt: DateTime.parse(json['createdAt']),
    );
  }
}
