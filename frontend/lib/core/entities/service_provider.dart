class ServiceProvider {
  final String id;
  final String employeeId;
  final String businessName;
  final String? fiscalAddress;
  final String? taxRegime;
  final String? professionalLicense;
  final String? specialization;
  final String serviceCategory;
  final String? insurancePolicy;
  final DateTime? insuranceExpiration;
  final String status;

  ServiceProvider({
    required this.id,
    required this.employeeId,
    required this.businessName,
    this.fiscalAddress,
    this.taxRegime,
    this.professionalLicense,
    this.specialization,
    required this.serviceCategory,
    this.insurancePolicy,
    this.insuranceExpiration,
    this.status = 'active',
  });

  factory ServiceProvider.fromJson(Map<String, dynamic> json) {
    return ServiceProvider(
      id: json['id'],
      employeeId: json['employeeId'],
      businessName: json['businessName'],
      fiscalAddress: json['fiscalAddress'],
      taxRegime: json['taxRegime'],
      professionalLicense: json['professionalLicense'],
      specialization: json['specialization'],
      serviceCategory: json['serviceCategory'],
      insurancePolicy: json['insurancePolicy'],
      insuranceExpiration: json['insuranceExpiration'] != null 
          ? DateTime.parse(json['insuranceExpiration']) 
          : null,
      status: json['status'] ?? 'active',
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'employeeId': employeeId,
    'businessName': businessName,
    'fiscalAddress': fiscalAddress,
    'taxRegime': taxRegime,
    'professionalLicense': professionalLicense,
    'specialization': specialization,
    'serviceCategory': serviceCategory,
    'insurancePolicy': insurancePolicy,
    'insuranceExpiration': insuranceExpiration?.toIso8601String().split('T')[0],
    'status': status,
  };
}
