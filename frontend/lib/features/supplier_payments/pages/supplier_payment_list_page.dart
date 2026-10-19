import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:share_plus/share_plus.dart';
import '../../../auth/auth_provider.dart';
import '../providers/supplier_payment_provider.dart';

final class SupplierPaymentListPage extends ConsumerStatefulWidget {
  final String? purchaseId;
  const SupplierPaymentListPage({super.key, this.purchaseId});

  @override
  ConsumerState<SupplierPaymentListPage> createState() => _SupplierPaymentListPageState();
}

final class _SupplierPaymentListPageState extends ConsumerState<SupplierPaymentListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(supplierPaymentProvider.notifier).load(purchaseId: widget.purchaseId));
  }

  String _methodLabel(String method) {
    return switch (method) {
      'cash' => 'Efectivo',
      'transfer' => 'Transferencia',
      'check' => 'Cheque',
      'card' => 'Tarjeta',
      _ => method,
    };
  }

  /// Genera el recibo de pago al proveedor vía el motor documental y lo comparte.
  Future<void> _generatePdf(String paymentId) async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.post('supplier-payments/$paymentId/pdf/generate');
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
            name: 'recibo_pago_proveedor_$paymentId.pdf',
          ),
        ]),
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Recibo generado (${bytes.length ~/ 1024} KB)'),
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
    final state = ref.watch(supplierPaymentProvider);
    final theme = Theme.of(context);

    return Scaffold(
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : state.items.isEmpty
                  ? const Center(child: Text('No hay pagos registrados'))
                  : RefreshIndicator(
                      onRefresh: () => ref.read(supplierPaymentProvider.notifier).load(purchaseId: widget.purchaseId),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final p = state.items[i];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: theme.colorScheme.primaryContainer,
                              child: Icon(Icons.payments_outlined, color: theme.colorScheme.onPrimaryContainer),
                            ),
                            title: Text(p.purchaseNumber, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text(
                              '${_methodLabel(p.paymentMethod)} · \$${p.amount.toStringAsFixed(2)} · ${p.paymentDate.substring(0, 10)}',
                            ),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Text(
                                  '\$${p.amount.toStringAsFixed(2)}',
                                  style: TextStyle(
                                    fontWeight: FontWeight.bold,
                                    color: theme.colorScheme.primary,
                                  ),
                                ),
                                PopupMenuButton<String>(
                                  tooltip: 'Más opciones',
                                  onSelected: (v) {
                                    if (v == 'pdf') _generatePdf(p.id);
                                  },
                                  itemBuilder: (_) => const [
                                    PopupMenuItem(
                                      value: 'pdf',
                                      child: Row(
                                        children: [
                                          Icon(Icons.picture_as_pdf_outlined, size: 18),
                                          SizedBox(width: 8),
                                          Text('Generar Recibo PDF'),
                                        ],
                                      ),
                                    ),
                                  ],
                                ),
                              ],
                            ),
                            onTap: () => context.push('/purchases/${p.purchaseId}'),
                          );
                        },
                      ),
                    ),
      floatingActionButton: widget.purchaseId == null ||
              !ref.canWriteLocation('/purchases/payments/new')
          ? null
          : FloatingActionButton.extended(
              heroTag: 'add-payment',
              onPressed: () => context.push('/purchases/payments/new', extra: {'purchaseId': widget.purchaseId}),
              icon: const Icon(Icons.add),
              label: const Text('Registrar Pago'),
            ),
    );
  }
}
