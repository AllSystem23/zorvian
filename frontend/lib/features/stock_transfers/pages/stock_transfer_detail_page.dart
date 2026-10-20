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
    'partially_received' => 'Recepción Parcial',
    'completed' => 'Completada',
    'cancelled' => 'Anulada',
    _ => s,
  };

  Color _statusColor(String s) => switch (s) {
    'in_transit' => Colors.orange,
    'partially_received' => Colors.amber.shade800,
    'completed' => Colors.green,
    'cancelled' => Colors.red,
    'draft' => Colors.grey,
    _ => Colors.grey,
  };

  bool _canReceive(String s) => s == 'in_transit' || s == 'partially_received';

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
    'Se descontará el stock de la bodega ORIGEN y quedará en tránsito. ¿Continuar?',
    'Transferencia despachada',
  );

  /// Recepción TOTAL: sin body el backend recibe todo lo pendiente.
  Future<void> _receiveAll() => _transition(
    'receive',
    'Recepción total',
    'Se dará de alta el stock pendiente en la bodega DESTINO. ¿Continuar?',
    'Transferencia recibida',
  );

  /// Recepción PARCIAL: diálogo con cantidades recibidas por línea (0..pendiente).
  Future<void> _receivePartial() async {
    final d = _data!;
    final received = <String, TextEditingController>{};
    final pending = <String, int>{};
    for (final item in d.details) {
      final p = item.quantity - item.quantityReceived;
      if (p <= 0 || item.id.isEmpty) continue;
      pending[item.id] = p;
      received[item.id] = TextEditingController(text: '$p');
    }
    if (pending.isEmpty) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('No hay cantidades pendientes por recibir'), backgroundColor: Colors.orange),
      );
      return;
    }

    final ok = await showZAdaptiveDialog<bool>(
      context,
      builder: (ctx) => ZResponsiveDialog(
        title: 'Recepción parcial',
        icon: Icons.inventory_2_outlined,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Indique cuántas unidades recibe de cada línea (puede dejar 0). '
              'Lo no recibido queda pendiente en la transferencia.',
              style: Theme.of(ctx).textTheme.bodySmall,
            ),
            const SizedBox(height: 12),
            ...d.details.where((i) => pending.containsKey(i.id)).map((i) {
              return Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: Row(
                  children: [
                    Expanded(
                      child: Text(
                        i.productName.isNotEmpty ? i.productName : i.productCode,
                        style: Theme.of(ctx).textTheme.bodyMedium,
                      ),
                    ),
                    Text('pend. ${pending[i.id]}', style: Theme.of(ctx).textTheme.bodySmall),
                    const SizedBox(width: 8),
                    SizedBox(
                      width: 72,
                      child: TextField(
                        controller: received[i.id],
                        keyboardType: TextInputType.number,
                        textAlign: TextAlign.center,
                        decoration: const InputDecoration(isDense: true, hintText: '0'),
                      ),
                    ),
                  ],
                ),
              );
            }),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Confirmar recepción')),
        ],
      ),
    );
    if (ok != true) return;

    final lines = <ReceiveStockTransferLine>[];
    for (final entry in received.entries) {
      final raw = entry.value.text.trim();
      final qty = int.tryParse(raw) ?? 0;
      final max = pending[entry.key]!;
      lines.add(ReceiveStockTransferLine(
        detailId: entry.key,
        quantity: qty < 0 ? 0 : (qty > max ? max : qty),
      ));
    }

    try {
      final dio = ref.read(dioClientProvider);
      await receiveStockTransfer(dio, widget.stockTransferId, lines: lines);
      ref.invalidate(stockTransferProvider);
      _load();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Recepción parcial registrada'), backgroundColor: Colors.green),
        );
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error en la recepción'), backgroundColor: Colors.red),
        );
      }
    }
  }

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
                      // `Expanded`: el número + el chip de estado no caben a
                      // 360 px (desbordaba 27 px).
                      Expanded(
                        child: Text(d.transferNumber, style: theme.textTheme.titleLarge),
                      ),
                      const SizedBox(width: 8),
                      Chip(label: Text(_statusLabel(d.status), style: TextStyle(color: _statusColor(d.status), fontSize: 12))),
                    ],
                  ),
                  const SizedBox(height: 8),
                  // Origen (sucursal + bodega) → Destino (sucursal + bodega).
                  Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              d.fromWarehouseName.isNotEmpty ? d.fromWarehouseName : 'Bodega origen',
                              style: const TextStyle(fontWeight: FontWeight.w600),
                            ),
                            if (d.fromBranchName.isNotEmpty)
                              Text(d.fromBranchName, style: theme.textTheme.bodySmall),
                          ],
                        ),
                      ),
                      const Icon(Icons.arrow_forward, size: 16),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.end,
                          children: [
                            Text(
                              d.toWarehouseName.isNotEmpty ? d.toWarehouseName : 'Bodega destino',
                              textAlign: TextAlign.end,
                              style: const TextStyle(fontWeight: FontWeight.w600),
                            ),
                            if (d.toBranchName.isNotEmpty)
                              Text(d.toBranchName, textAlign: TextAlign.end, style: theme.textTheme.bodySmall),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
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
                children: d.details.map((item) {
                  final pending = item.quantity - item.quantityReceived;
                  final showReceived = _canReceive(d.status) || d.status == 'completed';
                  return Padding(
                    padding: const EdgeInsets.symmetric(vertical: 6),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          flex: 3,
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(item.productName, style: const TextStyle(fontWeight: FontWeight.w500)),
                              if (item.productCode.isNotEmpty) Text('Cód: ${item.productCode}', style: theme.textTheme.bodySmall),
                            ],
                          ),
                        ),
                        const SizedBox(width: 8),
                        // Las marcas de recibido/cantidad/costo no caben junto
                        // al nombre a 360 px (desbordaban 39 px): toman el 40 %
                        // y se parten en varias líneas.
                        Expanded(
                          flex: 2,
                          child: Wrap(
                            alignment: WrapAlignment.end,
                            crossAxisAlignment: WrapCrossAlignment.center,
                            spacing: 8,
                            runSpacing: 2,
                            children: [
                              if (showReceived)
                                Text(
                                  '${item.quantityReceived}/${item.quantity} rec.',
                                  style: TextStyle(
                                    fontSize: 12,
                                    color: pending > 0 && d.status != 'completed'
                                        ? Colors.amber.shade800
                                        : Colors.green,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              Text('${item.quantity} uds.', style: const TextStyle(fontWeight: FontWeight.w600)),
                              if (item.unitCost > 0)
                                Text('@ ${item.unitCost.toStringAsFixed(2)}', style: theme.textTheme.bodySmall),
                            ],
                          ),
                        ),
                      ],
                    ),
                  );
                }).toList(),
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
            if (_canReceive(d.status))
              Row(
                children: [
                  Expanded(child: ZButton(text: 'Recibir todo', onPressed: _receiveAll, type: ZButtonType.primary)),
                  const SizedBox(width: 12),
                  Expanded(child: ZButton(text: 'Recepción parcial', onPressed: _receivePartial, type: ZButtonType.secondary)),
                ],
              ),
            const SizedBox(height: 12),
            ZButton(text: 'Generar PDF (Motor Documental)', onPressed: _generatePdf, type: ZButtonType.secondary),
            const SizedBox(height: 80),
          ],
        ),
      ),
    );
  }
}
