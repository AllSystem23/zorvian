import 'service_provider.dart';

class ServiceContract {
  final String id;
  final String serviceProviderId;
  final ServiceProvider? serviceProvider;
  final String contractNumber;
  final String contractName;
  final String? scope;
  final double totalContractAmount;
  final String currency;
  final String? paymentTerms;
  final DateTime startDate;
  final DateTime? endDate;
  final String status;
  final String? contractFileUrl;
  final String? notes;

  ServiceContract({
    required this.id,
    required this.serviceProviderId,
    this.serviceProvider,
    required this.contractNumber,
    required this.contractName,
    this.scope,
    required this.totalContractAmount,
    this.currency = 'NIO',
    this.paymentTerms,
    required this.startDate,
    this.endDate,
    this.status = 'draft',
    this.contractFileUrl,
    this.notes,
  });

  factory ServiceContract.fromJson(Map<String, dynamic> json) {
    return ServiceContract(
      id: json['id'],
      serviceProviderId: json['serviceProviderId'],
      serviceProvider: json['serviceProvider'] != null 
          ? ServiceProvider.fromJson(json['serviceProvider']) 
          : null,
      contractNumber: json['contractNumber'],
      contractName: json['contractName'],
      scope: json['scope'],
      totalContractAmount: (json['totalContractAmount'] as num).toDouble(),
      currency: json['currency'] ?? 'NIO',
      paymentTerms: json['paymentTerms'],
      startDate: DateTime.parse(json['startDate']),
      endDate: json['endDate'] != null ? DateTime.parse(json['endDate']) : null,
      status: json['status'] ?? 'draft',
      contractFileUrl: json['contractFileUrl'],
      notes: json['notes'],
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'serviceProviderId': serviceProviderId,
    'contractNumber': contractNumber,
    'contractName': contractName,
    'scope': scope,
    'totalContractAmount': totalContractAmount,
    'currency': currency,
    'paymentTerms': paymentTerms,
    'startDate': startDate.toIso8601String().split('T')[0],
    'endDate': endDate?.toIso8601String().split('T')[0],
    'status': status,
    'contractFileUrl': contractFileUrl,
    'notes': notes,
  };
}
