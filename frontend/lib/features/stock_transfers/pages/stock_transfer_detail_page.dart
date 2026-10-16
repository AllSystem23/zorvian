import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:share_plus/share_plus.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/stock_transfer_provider.dart';

final class StockTransferDetailPage extends ConsumerStatefulWidget {
  final String stockTransferId;
  const StockTransferDetailPage({super.key, required this.stockTransferId});
  @override
  ConsumerState<StockTransferDetailPage> createState() => _StockTransferDetailPageState();
}

final class _StockTransferDetailPageState extends ConsumerState<StockTransferDetailPage> {
  StockTransferDetail? _data;
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
      final r = await dio.get('stock-transfers/${widget.stockTransferId}');
      setState(() { _data = StockTransferDetail.fromJson(r.data); _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar la transferencia'; _loading = false; });
    }
  }

  String _statusLabel(String s) => switch (s) {
    'draft' => 'Borrador',
    'in_transit' => 'En Tránsito',
    'completed' => 'Completada',
    'cancelled' => 'Anulada',
    _ => s,
  };

  Color _statusColor(String s) => switch (s) {
    'in_transit' => Colors.orange,
    'completed' => Colors.green,
    'cancelled' => Colors.red,
    'draft' => Colors.grey,
    _ => Colors.grey,
  };

  Future<void> _transition(String action, String confirmTitle, String confirmMsg, String successMsg) async {
    final confirm = await ZModal.confirm(context, title: confirmTitle, message: confirmMsg);
    if (!confirm) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('stock-transfers/${widget.stockTransferId}/$action');
      ref.invalidate(stockTransferProvider);
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

  Future<void> _dispatch() => _transition(
    'dispatch',
    'Despachar transferencia',
    'Se descontará el stock de la sucursal ORIGEN y quedará en tránsito. ¿Continuar?',
    'Transferencia despachada',
  );

  Future<void> _receive() => _transition(
    'receive',
    'Recepción de transferencia',
    'Se dará de alta el stock en la sucursal DESTINO. ¿Continuar?',
    'Transferencia recibida',
  );

  Future<void> _cancel() => _transition(
    'cancel',
    'Anular transferencia',
    '¿Está seguro de anular esta transferencia en borrador?',
    'Transferencia anulada',
  );

  /// Genera el PDF de la transferencia vía el motor documental y lo comparte.
  Future<void> _generatePdf() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.post('stock-transfers/${widget.stockTransferId}/pdf/generate');
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
            name: 'transferencia_${_data!.transferNumber}.pdf',
          ),
        ]),
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Transferencia generada (${bytes.length ~/ 1024} KB)'),
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
                      Text(d.transferNumber, style: theme.textTheme.titleLarge),
                      Chip(label: Text(_statusLabel(d.status), style: TextStyle(color: _statusColor(d.status), fontSize: 12))),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      const Icon(Icons.warehouse_outlined, size: 18),
                      const SizedBox(width: 6),
                      Expanded(child: Text(d.fromBranchName, style: theme.textTheme.bodyMedium)),
                      const Icon(Icons.arrow_forward, size: 16),
                      const SizedBox(width: 6),
                      const Icon(Icons.store_mall_directory_outlined, size: 18),
                      const SizedBox(width: 6),
                      Expanded(child: Text(d.toBranchName, style: theme.textTheme.bodyMedium)),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text('Creada: ${d.createdAt}', style: theme.textTheme.bodySmall),
                  if (d.dispatchedAt != null && d.dispatchedAt!.isNotEmpty)
                    Text('Despachada: ${d.dispatchedAt}', style: theme.textTheme.bodySmall),
                  if (d.receivedAt != null && d.receivedAt!.isNotEmpty)
                    Text('Recibida: ${d.receivedAt}', style: theme.textTheme.bodySmall),
                  if (d.notes != null && d.notes!.isNotEmpty) Text('Notas: ${d.notes}', style: theme.textTheme.bodySmall),
                ],
              ),
            ),
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
                          ],
                        ),
                      ),
                      Text('${item.quantity} uds.', style: const TextStyle(fontWeight: FontWeight.w600)),
                      if (item.unitCost > 0) ...[
                        const SizedBox(width: 12),
                        Text('@ ${item.unitCost.toStringAsFixed(2)}', style: theme.textTheme.bodySmall),
                      ],
                    ],
                  ),
                )).toList(),
              ),
            ),
            const SizedBox(height: 24),
            if (d.status == 'draft')
              Row(
                children: [
                  Expanded(child: ZButton(text: 'Anular', onPressed: _cancel, type: ZButtonType.secondary)),
                  const SizedBox(width: 12),
                  Expanded(child: ZButton(text: 'Despachar', onPressed: _dispatch, type: ZButtonType.primary)),
                ],
              ),
            if (d.status == 'in_transit')
              ZButton(text: 'Recibir en Destino', onPressed: _receive, type: ZButtonType.primary),
            const SizedBox(height: 12),
            ZButton(text: 'Generar PDF (Motor Documental)', onPressed: _generatePdf, type: ZButtonType.secondary),
            const SizedBox(height: 80),
          ],
        ),
      ),
    );
  }
}
