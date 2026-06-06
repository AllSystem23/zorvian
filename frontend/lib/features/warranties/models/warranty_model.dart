class WarrantyModel {
  final String id;
  final String warrantyNumber;
  final String clientName;
  final String productName;
  final String status;

  WarrantyModel({
    required this.id,
    required this.warrantyNumber,
    required this.clientName,
    required this.productName,
    required this.status,
  });

  factory WarrantyModel.fromJson(Map<String, dynamic> json) {
    return WarrantyModel(
      id: json['id'],
      warrantyNumber: json['warrantyNumber'],
      clientName: json['clientName'],
      productName: json['productName'],
      status: json['status'],
    );
  }
}
