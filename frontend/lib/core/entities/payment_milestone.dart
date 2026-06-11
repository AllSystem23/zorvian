class PaymentMilestone {
  final String id;
  final String serviceContractId;
  final String name;
  final String? description;
  final double amount;
  final String? deliverableDescription;
  final DateTime estimatedDate;
  final DateTime? completionDate;
  final String status;
  final String? deliverableFileUrl;
  final String? approvalNotes;

  PaymentMilestone({
    required this.id,
    required this.serviceContractId,
    required this.name,
    this.description,
    required this.amount,
    this.deliverableDescription,
    required this.estimatedDate,
    this.completionDate,
    this.status = 'pending',
    this.deliverableFileUrl,
    this.approvalNotes,
  });

  factory PaymentMilestone.fromJson(Map<String, dynamic> json) {
    return PaymentMilestone(
      id: json['id'],
      serviceContractId: json['serviceContractId'],
      name: json['name'],
      description: json['description'],
      amount: (json['amount'] as num).toDouble(),
      deliverableDescription: json['deliverableDescription'],
      estimatedDate: DateTime.parse(json['estimatedDate']),
      completionDate: json['completionDate'] != null 
          ? DateTime.parse(json['completionDate']) 
          : null,
      status: json['status'] ?? 'pending',
      deliverableFileUrl: json['deliverableFileUrl'],
      approvalNotes: json['approvalNotes'],
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'serviceContractId': serviceContractId,
    'name': name,
    'description': description,
    'amount': amount,
    'deliverableDescription': deliverableDescription,
    'estimatedDate': estimatedDate.toIso8601String().split('T')[0],
    'completionDate': completionDate?.toIso8601String().split('T')[0],
    'status': status,
    'deliverableFileUrl': deliverableFileUrl,
    'approvalNotes': approvalNotes,
  };
}
