import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../auth/auth_provider.dart';
import '../../shared/ds/ds.dart';

class PendingCertificateRequest {
  final String id;
  final String certificateType;
  final String? reason;
  final String status;
  final DateTime requestedAt;
  final String employeeName;
  final String employeeIdentification;

  PendingCertificateRequest({
    required this.id,
    required this.certificateType,
    this.reason,
    required this.status,
    required this.requestedAt,
    required this.employeeName,
    required this.employeeIdentification,
  });

  factory PendingCertificateRequest.fromJson(Map<String, dynamic> json) {
    return PendingCertificateRequest(
      id: json['id'] as String,
      certificateType: json['certificateType'] as String? ?? 'salarial',
      reason: json['reason'] as String?,
      status: json['status'] as String? ?? 'pending',
      requestedAt: DateTime.parse(json['requestedAt'] as String),
      employeeName: json['employeeName'] as String? ?? '',
      employeeIdentification: json['employeeIdentification'] as String? ?? '',
    );
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
        return 'Poder';
      default:
        return 'Salarial';
    }
  }
}

final pendingCertificateRequestsProvider =
    FutureProvider.autoDispose<List<PendingCertificateRequest>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final r = await dio.get('certificate-requests', params: {'status': 'pending'});
  final list = (r.data as List<dynamic>? ?? [])
      .map((e) => PendingCertificateRequest.fromJson(e as Map<String, dynamic>))
      .toList();
  return list;
});

class CertificateRequestsPage extends ConsumerWidget {
  const CertificateRequestsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authProvider);
    // Access is controlled via employee.read permission (route removed from _routeRoles
    // to avoid conflict with _routePermissions). The page checks hasPermission
    // to allow employees with the permission but without HR roles.
    final hasPermission = auth.hasPermission('employee.read');
    final requestsAsync = ref.watch(pendingCertificateRequestsProvider);

    if (!hasPermission) {
      return const Scaffold(
        body: Center(child: Text('No tiene permisos para ver esta página.')),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('Solicitudes de Constancia'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => ref.invalidate(pendingCertificateRequestsProvider),
          ),
        ],
      ),
      body: requestsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Error: $e')),
        data: (requests) {
          if (requests.isEmpty) {
            return const Center(child: Text('No hay solicitudes pendientes.'));
          }
          return ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: requests.length,
            itemBuilder: (ctx, i) {
              final req = requests[i];
              return ZCard(
                margin: const EdgeInsets.only(bottom: 12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            req.employeeName,
                            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                          decoration: BoxDecoration(
                            color: Colors.orange.withValues(alpha: 0.15),
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: Text(_badgeLabel(req.typeLabel),
                              style: const TextStyle(color: Colors.orange, fontSize: 12)),
                        ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text('ID: ${req.employeeIdentification}',
                        style: TextStyle(color: Colors.grey[600], fontSize: 12)),
                    Text('Fecha: ${_formatDate(req.requestedAt)}',
                        style: TextStyle(color: Colors.grey[600], fontSize: 12)),
                    if (req.reason != null && req.reason!.isNotEmpty) ...[
                      const SizedBox(height: 8),
                      Text('Motivo: ${req.reason}', style: const TextStyle(fontStyle: FontStyle.italic)),
                    ],
                    const SizedBox(height: 12),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        OutlinedButton.icon(
                          icon: const Icon(Icons.close, size: 16, color: Colors.red),
                          label: const Text('Rechazar', style: TextStyle(color: Colors.red)),
                          onPressed: () => _rejectRequest(context, ref, req),
                        ),
                        const SizedBox(width: 8),
                        FilledButton.icon(
                          icon: const Icon(Icons.check, size: 16),
                          label: const Text('Aprobar y Emitir'),
                          onPressed: () => _approveRequest(context, ref, req),
                        ),
                      ],
                    ),
                  ],
                ),
              );
            },
          );
        },
      ),
    );
  }

  static String _formatDate(DateTime dt) {
    return '${dt.day.toString().padLeft(2, '0')}/${dt.month.toString().padLeft(2, '0')}/${dt.year} ${dt.hour.toString().padLeft(2, '0')}:${dt.minute.toString().padLeft(2, '0')}';
  }

  static String _badgeLabel(String typeLabel) {
    switch (typeLabel) {
      case 'Salarial':
      case 'Laboral':
        return 'Constancia $typeLabel';
      default:
        return 'Doc. $typeLabel';
    }
  }

  static Future<void> _approveRequest(
      BuildContext context, WidgetRef ref, PendingCertificateRequest req) async {
    final confirmed = await ZConfirmDialog.show(
      context,
      title: 'Aprobar y Emitir',
      message: '¿Aprueba el documento (${req.typeLabel}) de ${req.employeeName}?'
          '\n\nSe generará el PDF y se notificará al trabajador.',
      confirmLabel: 'Aprobar',
      icon: Icons.verified_outlined,
    );

    if (!confirmed) return;

    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('certificate-requests/${req.id}/approve');
      if (!context.mounted) return;
      ref.invalidate(pendingCertificateRequestsProvider);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Constancia de ${req.employeeName} emitida correctamente.')),
      );
    } catch (e) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error al aprobar: $e')),
      );
    }
  }

  static Future<void> _rejectRequest(
      BuildContext context, WidgetRef ref, PendingCertificateRequest req) async {
    final reasonCtrl = TextEditingController();
    final confirmed = await showZAdaptiveDialog<bool>(
      context,
      builder: (ctx) => ZResponsiveDialog(
        title: 'Rechazar Solicitud',
        icon: Icons.block_outlined,
        iconColor: ZColors.danger,
        maxWidth: 460,
        child: TextField(
          controller: reasonCtrl,
          maxLines: 3,
          decoration: const InputDecoration(
            labelText: 'Motivo del rechazo (opcional)',
            border: OutlineInputBorder(),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: ZColors.danger),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Rechazar', style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('certificate-requests/${req.id}/reject', data: {
        'rejectionReason': reasonCtrl.text.trim().isEmpty ? 'Sin motivo' : reasonCtrl.text.trim(),
      });
      if (!context.mounted) return;
      ref.invalidate(pendingCertificateRequestsProvider);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Solicitud de ${req.employeeName} rechazada.')),
      );
    } catch (e) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error al rechazar: $e')),
      );
    }
  }
}
