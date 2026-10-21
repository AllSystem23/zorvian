import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/auth/auth_provider.dart';

/// Solicitud de acceso enviada desde el registro público ("Solicitar Acceso").
class AccessRequestModel {
  final String id;
  final String fullName;
  final String email;
  final String company;
  final String? phone;
  final String? message;
  final String status;
  final DateTime? createdAt;
  final DateTime? reviewedAt;
  final String? reviewedBy;
  final String? rejectionReason;
  final String? invitationCode;

  const AccessRequestModel({
    required this.id,
    required this.fullName,
    required this.email,
    required this.company,
    this.phone,
    this.message,
    required this.status,
    this.createdAt,
    this.reviewedAt,
    this.reviewedBy,
    this.rejectionReason,
    this.invitationCode,
  });

  factory AccessRequestModel.fromJson(Map<String, dynamic> json) => AccessRequestModel(
        id: json['id'] as String? ?? '',
        fullName: json['fullName'] as String? ?? '',
        email: json['email'] as String? ?? '',
        company: json['company'] as String? ?? '',
        phone: json['phone'] as String?,
        message: json['message'] as String?,
        status: json['status'] as String? ?? 'pending',
        createdAt: DateTime.tryParse(json['createdAt'] as String? ?? ''),
        reviewedAt: DateTime.tryParse(json['reviewedAt'] as String? ?? ''),
        reviewedBy: json['reviewedBy'] as String?,
        rejectionReason: json['rejectionReason'] as String?,
        invitationCode: json['invitationCode'] as String?,
      );

  bool get isPending => status == 'pending';
  bool get isApproved => status == 'approved';
  bool get isRejected => status == 'rejected';

  String get statusLabel {
    if (isApproved) return 'Aprobada';
    if (isRejected) return 'Rechazada';
    return 'Pendiente';
  }
}

class AccessRequestNotifier extends AsyncNotifier<List<AccessRequestModel>> {
  @override
  Future<List<AccessRequestModel>> build() => _fetch();

  Future<void> load() async {
    state = const AsyncValue.loading();
    state = await AsyncValue.guard(_fetch);
  }

  Future<List<AccessRequestModel>> _fetch() async {
    final dio = ref.read(dioClientProvider);
    final response = await dio.get('access-requests');
    final data = response.data;
    if (data is! List) return [];
    return data
        .map((e) => AccessRequestModel.fromJson(Map<String, dynamic>.from(e as Map)))
        .toList();
  }

  /// Aprueba la solicitud: el backend emite el código de invitación para la
  /// compañía indicada y se lo envía al solicitante por correo.
  Future<void> approve(String id, {required String companyId, String role = 'Employee'}) async {
    final dio = ref.read(dioClientProvider);
    await dio.post('access-requests/$id/approve', data: {
      'companyId': companyId,
      'role': role,
    });
    state = await AsyncValue.guard(_fetch);
  }

  Future<void> reject(String id, {String? reason}) async {
    final dio = ref.read(dioClientProvider);
    await dio.post('access-requests/$id/reject', data: {'reason': reason});
    state = await AsyncValue.guard(_fetch);
  }
}

final accessRequestProvider =
    AsyncNotifierProvider<AccessRequestNotifier, List<AccessRequestModel>>(
        AccessRequestNotifier.new);
