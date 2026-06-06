class WarrantyDashboardModel {
  final int totalActive;
  final int totalBreachedSla;
  final int registeredCount;
  final int inDiagnosisCount;
  final int inRepairCount;
  final int readyForDeliveryCount;

  WarrantyDashboardModel({
    required this.totalActive,
    required this.totalBreachedSla,
    required this.registeredCount,
    required this.inDiagnosisCount,
    required this.inRepairCount,
    required this.readyForDeliveryCount,
  });

  factory WarrantyDashboardModel.fromJson(Map<String, dynamic> json) {
    return WarrantyDashboardModel(
      totalActive: json['totalActive'],
      totalBreachedSla: json['totalBreachedSla'],
      registeredCount: json['registeredCount'],
      inDiagnosisCount: json['inDiagnosisCount'],
      inRepairCount: json['inRepairCount'],
      readyForDeliveryCount: json['readyForDeliveryCount'],
    );
  }
}
