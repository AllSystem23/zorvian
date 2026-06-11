class ProviderInvoice {
  final String id;
  final String paymentMilestoneId;
  final String invoiceNumber;
  final DateTime invoiceDate;
  final double invoiceAmount;
  final double withholdingAmount;
  final double netAmount;
  final String currency;
  final String? invoiceFileUrl;
  final String status;
  final DateTime? paymentDate;
  final String? paymentReference;
  final String? notes;

  ProviderInvoice({
    required this.id,
    required this.paymentMilestoneId,
    required this.invoiceNumber,
    required this.invoiceDate,
    required this.invoiceAmount,
    required this.withholdingAmount,
    required this.netAmount,
    this.currency = 'NIO',
    this.invoiceFileUrl,
    this.status = 'received',
    this.paymentDate,
    this.paymentReference,
    this.notes,
  });

  factory ProviderInvoice.fromJson(Map<String, dynamic> json) {
    return ProviderInvoice(
      id: json['id'],
      paymentMilestoneId: json['paymentMilestoneId'],
      invoiceNumber: json['invoiceNumber'],
      invoiceDate: DateTime.parse(json['invoiceDate']),
      invoiceAmount: (json['invoiceAmount'] as num).toDouble(),
      withholdingAmount: (json['withholdingAmount'] as num).toDouble(),
      netAmount: (json['netAmount'] as num).toDouble(),
      currency: json['currency'] ?? 'NIO',
      invoiceFileUrl: json['invoiceFileUrl'],
      status: json['status'] ?? 'received',
      paymentDate: json['paymentDate'] != null ? DateTime.parse(json['paymentDate']) : null,
      paymentReference: json['paymentReference'],
      notes: json['notes'],
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'paymentMilestoneId': paymentMilestoneId,
    'invoiceNumber': invoiceNumber,
    'invoiceDate': invoiceDate.toIso8601String().split('T')[0],
    'invoiceAmount': invoiceAmount,
    'withholdingAmount': withholdingAmount,
    'netAmount': netAmount,
    'currency': currency,
    'invoiceFileUrl': invoiceFileUrl,
    'status': status,
    'paymentDate': paymentDate?.toIso8601String().split('T')[0],
    'paymentReference': paymentReference,
    'notes': notes,
  };
}
