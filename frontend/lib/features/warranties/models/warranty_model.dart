class WarrantyTrackingModel {
  final String id;
  final String warrantyNumber;
  final String status;
  final String clientName;
  final String productName;
  final String? serialNumber;
  final String? imei;
  final String? lot;
  final DateTime? startDate;
  final DateTime? endDate;
  final int? durationDays;
  final String? termsAndConditions;
  final List<WarrantyClaimSummary> claims;
  final List<WarrantyEventSummary> timeline;

  const WarrantyTrackingModel({
    required this.id,
    required this.warrantyNumber,
    required this.status,
    required this.clientName,
    required this.productName,
    this.serialNumber,
    this.imei,
    this.lot,
    this.startDate,
    this.endDate,
    this.durationDays,
    this.termsAndConditions,
    this.claims = const [],
    this.timeline = const [],
  });

  factory WarrantyTrackingModel.fromJson(Map<String, dynamic> j) {
    return WarrantyTrackingModel(
      id: j['id'] as String? ?? '',
      warrantyNumber: j['warrantyNumber'] as String? ?? '',
      status: j['status'] as String? ?? '',
      clientName: j['clientName'] as String? ?? '',
      productName: j['productName'] as String? ?? '',
      serialNumber: j['serialNumber'] as String?,
      imei: j['imei'] as String?,
      lot: j['lot'] as String?,
      startDate: j['startDate'] != null ? DateTime.tryParse(j['startDate']) : null,
      endDate: j['endDate'] != null ? DateTime.tryParse(j['endDate']) : null,
      durationDays: j['durationDays'] as int?,
      termsAndConditions: j['termsAndConditions'] as String?,
      claims: (j['claims'] as List?)?.map((c) => WarrantyClaimSummary.fromJson(c)).toList() ?? [],
      timeline: (j['timeline'] as List?)?.map((t) => WarrantyEventSummary.fromJson(t)).toList() ?? [],
    );
  }
}

class WarrantyClaimSummary {
  final String id;
  final String claimNumber;
  final String type;
  final String status;
  final String? description;
  final DateTime? createdAt;

  const WarrantyClaimSummary({
    required this.id,
    required this.claimNumber,
    required this.type,
    required this.status,
    this.description,
    this.createdAt,
  });

  factory WarrantyClaimSummary.fromJson(Map<String, dynamic> j) => WarrantyClaimSummary(
    id: j['id'] as String? ?? '',
    claimNumber: j['claimNumber'] as String? ?? '',
    type: j['type'] as String? ?? '',
    status: j['status'] as String? ?? '',
    description: j['description'] as String?,
    createdAt: j['createdAt'] != null ? DateTime.tryParse(j['createdAt']) : null,
  );
}

class WarrantyEventSummary {
  final String id;
  final String eventType;
  final String? description;
  final DateTime? eventDate;

  const WarrantyEventSummary({required this.id, required this.eventType, this.description, this.eventDate});

  factory WarrantyEventSummary.fromJson(Map<String, dynamic> j) => WarrantyEventSummary(
    id: j['id'] as String? ?? '',
    eventType: j['eventType'] as String? ?? '',
    description: j['description'] as String?,
    eventDate: j['eventDate'] != null ? DateTime.tryParse(j['eventDate']) : null,
  );
}
