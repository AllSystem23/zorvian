import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:share_plus/share_plus.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/purchase_request_provider.dart';

final class PurchaseRequestDetailPage extends ConsumerStatefulWidget {
  final String purchaseRequestId;
  const PurchaseRequestDetailPage({super.key, required this.purchaseRequestId});
  @override
  ConsumerState<PurchaseRequestDetailPage> createState() => _PurchaseRequestDetailPageState();
}

final class _PurchaseRequestDetailPageState extends ConsumerState<PurchaseRequestDetailPage> {
  PurchaseRequestDetail? _data;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('purchase-requests/${widget.purchaseRequestId}');
      setState(() { _data = PurchaseRequestDetail.fromJson(r.data); _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar la solicitud'; _loading = false; });
    }
  }

  String _statusLabel(String s) => switch (s) {
    'draft' => 'Borrador',
    'submitted' => 'Enviada',
    'approved' => 'Aprobada',
    'rejected' => 'Rechazada',
    'cancelled' => 'Anulada',
    _ => s,
  };

  Color _statusColor(String s) => switch (s) {
    'submitted' => Colors.blue,
    'approved' => Colors.green,
    'rejected' => Colors.red,
    'cancelled' => Colors.red,
    'draft' => Colors.grey,
    _ => Colors.grey,
  };

  String _priorityLabel(String p) => switch (p) {
    'low' => 'Baja',
    'high' => 'Alta',
    'urgent' => 'Urgente',
    _ => 'Normal',
  };

  Future<void> _transition(String action, String successMsg) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('purchase-requests/${widget.purchaseRequestId}/$action');
      ref.invalidate(purchaseRequestProvider);
      _load();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(successMsg), backgroundColor: Colors.green));
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error en la operación'), backgroundColor: Colors.red));
      }
    }
  }

  Future<void> _submit() async {
    final confirm = await ZModal.confirm(context, title: 'Enviar solicitud', message: '¿Enviar la solicitud a Compras para su aprobación?');
    if (!confirm) return;
    await _transition('submit', 'Solicitud enviada');
  }

  Future<void> _approve() async {
    final confirm = await ZModal.confirm(context, title: 'Aprobar solicitud', message: '¿Está seguro de aprobar esta solicitud de cotización?');
    if (!confirm) return;
    await _transition('approve', 'Solicitud aprobada');
  }

  Future<void> _reject() async {
    final reason = await showDialog<String>(
      context: context,
      builder: (_) => const _RejectDialog(),
    );
    if (reason == null) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post(
        'purchase-requests/${widget.purchaseRequestId}/reject',
        data: {'reason': reason.isEmpty ? null : reason},
      );
      ref.invalidate(purchaseRequestProvider);
      _load();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Solicitud rechazada'), backgroundColor: Colors.green));
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error al rechazar'), backgroundColor: Colors.red));
      }
    }
  }

  Future<void> _cancel() async {
    final confirm = await ZModal.confirm(context, title: 'Anular solicitud', message: '¿Está seguro de anular esta solicitud?');
    if (!confirm) return;
    await _transition('cancel', 'Solicitud anulada');
  }

  /// Genera el PDF de la solicitud vía el motor documental y lo comparte.
  Future<void> _generatePdf() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.post('purchase-requests/${widget.purchaseRequestId}/pdf/generate');
      final documentId = r.data['generatedDocumentId'] as String?;
      if (documentId == null) throw Exception('Respuesta sin documento generado');

      final pdf = await dio.get(
        'documents/$documentId/pdf',
        options: Options(responseType: ResponseType.bytes),
      );
      final bytes = pdf.data as List<int>;
      if (!mounted) return;
      await SharePlus.instance.share(
        ShareParams(files: [
          XFile.fromData(
            Uint8List.fromList(bytes),
            mimeType: 'application/pdf',
            name: 'solicitud_${_data!.requestNumber}.pdf',
          ),
        ]),
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Solicitud generada (${bytes.length ~/ 1024} KB)'),
            backgroundColor: Colors.green,
          ),
        );
      }
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error al generar PDF: $e'), backgroundColor: Colors.red),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (_loading) return const Scaffold(body: Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(body: Center(child: Text(_error!, style: TextStyle(color: theme.colorScheme.error))));
    final d = _data!;

    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ZCard(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Flexible(child: Text(d.requestNumber, style: theme.textTheme.titleLarge)),
                      Chip(label: Text(_statusLabel(d.status), style: TextStyle(color: _statusColor(d.status), fontSize: 12))),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Text(d.title, style: theme.textTheme.titleMedium),
                  const SizedBox(height: 4),
                  Text('Prioridad: ${_priorityLabel(d.priority)}', style: theme.textTheme.bodyMedium),
                  Text('Sucursal: ${d.branchName ?? '—'}', style: theme.textTheme.bodyMedium),
                  if (d.neededByDate != null && d.neededByDate!.isNotEmpty)
                    Text('Requerida para: ${d.neededByDate}', style: theme.textTheme.bodySmall),
                  Text('Solicitante: ${d.requestedBy}', style: theme.textTheme.bodySmall),
                  Text('Creada: ${d.createdAt}', style: theme.textTheme.bodySmall),
                  if (d.notes != null && d.notes!.isNotEmpty) Text('Notas: ${d.notes}', style: theme.textTheme.bodySmall),
                ],
              ),
            ),
            if (d.status == 'rejected' && d.rejectionReason != null && d.rejectionReason!.isNotEmpty) ...[
              const SizedBox(height: 12),
              ZCard(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    Icon(Icons.block, color: theme.colorScheme.error),
                    const SizedBox(width: 8),
                    Expanded(child: Text('Motivo de rechazo: ${d.rejectionReason}', style: TextStyle(color: theme.colorScheme.error))),
                  ],
                ),
              ),
            ],
            const SizedBox(height: 16),
            Text('Detalle', style: theme.textTheme.titleSmall),
            const SizedBox(height: 8),
            ZCard(
              padding: const EdgeInsets.all(16),
              child: Column(
                children: d.details.map((item) => Padding(
                  padding: const EdgeInsets.symmetric(vertical: 6),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(item.productName, style: const TextStyle(fontWeight: FontWeight.w500)),
                            if (item.productCode.isNotEmpty) Text('Cód: ${item.productCode}', style: theme.textTheme.bodySmall),
                            Text('${item.quantity} × ${item.estimatedUnitPrice.toStringAsFixed(2)}', style: theme.textTheme.bodySmall),
                          ],
                        ),
                      ),
                      Text(item.subtotal.toStringAsFixed(2), style: const TextStyle(fontWeight: FontWeight.w600)),
                    ],
                  ),
                )).toList(),
              ),
            ),
            const SizedBox(height: 16),
            ZCard(
              padding: const EdgeInsets.all(16),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Total estimado', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                  Text(d.estimatedTotal.toStringAsFixed(2), style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                ],
              ),
            ),
            const SizedBox(height: 24),
            if (d.status == 'draft')
              Row(
                children: [
                  Expanded(child: ZButton(text: 'Anular', onPressed: _cancel, type: ZButtonType.secondary)),
                  const SizedBox(width: 12),
                  Expanded(child: ZButton(text: 'Enviar', onPressed: _submit, type: ZButtonType.primary)),
                ],
              ),
            if (d.status == 'submitted')
              Row(
                children: [
                  Expanded(child: ZButton(text: 'Rechazar', onPressed: _reject, type: ZButtonType.secondary)),
                  const SizedBox(width: 12),
                  Expanded(child: ZButton(text: 'Aprobar', onPressed: _approve, type: ZButtonType.primary)),
                ],
              ),
            if (d.status == 'submitted') ...[
              const SizedBox(height: 12),
              ZButton(text: 'Anular', onPressed: _cancel, type: ZButtonType.secondary),
            ],
            const SizedBox(height: 12),
            ZButton(text: 'Generar PDF (Motor Documental)', onPressed: _generatePdf, type: ZButtonType.secondary),
            const SizedBox(height: 80),
          ],
        ),
      ),
    );
  }
}

final class _RejectDialog extends StatefulWidget {
  const _RejectDialog();
  @override
  State<_RejectDialog> createState() => _RejectDialogState();
}

final class _RejectDialogState extends State<_RejectDialog> {
  final _reasonCtrl = TextEditingController();
  final _formKey = GlobalKey<FormState>();

  @override
  void dispose() {
    _reasonCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Rechazar Solicitud'),
      content: Form(
        key: _formKey,
        child: SizedBox(
          width: 400,
          child: TextFormField(
            controller: _reasonCtrl,
            maxLines: 3,
            decoration: const InputDecoration(
              labelText: 'Motivo del rechazo',
              hintText: 'Ej: Presupuesto no disponible este mes',
              border: OutlineInputBorder(),
            ),
            validator: (v) => (v == null || v.trim().isEmpty) ? 'Indique el motivo' : null,
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancelar')),
        ZButton(
          text: 'Rechazar',
          onPressed: () {
            if (!_formKey.currentState!.validate()) return;
            Navigator.pop(context, _reasonCtrl.text.trim());
          },
        ),
      ],
    );
  }
}
