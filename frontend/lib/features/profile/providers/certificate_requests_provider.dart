import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class CertificateRequestItem {
  final String id;
  final String certificateType;
  final String? reason;
  final String status;
  final String? rejectionReason;
  final String? generatedDocumentId;
  final DateTime requestedAt;
  final DateTime? processedAt;

  CertificateRequestItem({
    required this.id,
    required this.certificateType,
    this.reason,
    required this.status,
    this.rejectionReason,
    this.generatedDocumentId,
    required this.requestedAt,
    this.processedAt,
  });

  factory CertificateRequestItem.fromJson(Map<String, dynamic> json) {
    return CertificateRequestItem(
      id: json['id'] as String,
      certificateType: json['certificateType'] as String? ?? 'salarial',
      reason: json['reason'] as String?,
      status: json['status'] as String? ?? 'pending',
      rejectionReason: json['rejectionReason'] as String?,
      generatedDocumentId: json['generatedDocumentId'] as String?,
      requestedAt: DateTime.parse(json['requestedAt'] as String),
      processedAt: json['processedAt'] != null
          ? DateTime.parse(json['processedAt'] as String)
          : null,
    );
  }

  bool get isPending => status == 'pending';
  bool get isApproved => status == 'approved';
  bool get isRejected => status == 'rejected';
  bool get canDownload => isApproved && generatedDocumentId != null;

  String get statusLabel {
    switch (status) {
      case 'pending':
        return 'Pendiente';
      case 'approved':
        return 'Aprobada';
      case 'rejected':
        return 'Rechazada';
      default:
        return status;
    }
  }

  String get typeLabel {
    switch (certificateType) {
      case 'laboral':
        return 'Laboral';
      case 'contrato':
        return 'Contrato';
      case 'liquidacion':
        return 'Liquidación';
      case 'poder':
        return 'Poder Notarial';
      default:
        return 'Salarial';
    }
  }
}

final myCertificateRequestsProvider =
    FutureProvider.autoDispose<List<CertificateRequestItem>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final r = await dio.get('certificate-requests/employees/me/certificate-requests');
  final list = (r.data as List<dynamic>? ?? [])
      .map((e) => CertificateRequestItem.fromJson(e as Map<String, dynamic>))
      .toList();
  return list;
});

final createCertificateRequestProvider =
    FutureProvider.family.autoDispose<void, Map<String, String>>((ref, data) async {
  final dio = ref.read(dioClientProvider);
  await dio.post('certificate-requests/employees/me/certificate-requests', data: data);
  ref.invalidate(myCertificateRequestsProvider);
});
