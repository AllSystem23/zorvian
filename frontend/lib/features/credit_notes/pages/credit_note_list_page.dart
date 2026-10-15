import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:share_plus/share_plus.dart';
import '../../../auth/auth_provider.dart';
import '../providers/credit_note_provider.dart';

final class CreditNoteListPage extends ConsumerStatefulWidget {
  const CreditNoteListPage({super.key});
  @override
  ConsumerState<CreditNoteListPage> createState() => _CreditNoteListPageState();
}

final class _CreditNoteListPageState extends ConsumerState<CreditNoteListPage> {
  @override
  void initState() {
    super.initState();
    _ensureCompanyAndLoad();
  }

  void _ensureCompanyAndLoad() {
    Future.microtask(() => ref.read(creditNoteProvider.notifier).load());
  }

  /// Genera el PDF de la nota de crédito vía el motor documental y lo comparte.
  Future<void> _generatePdf(CreditNoteItem cn) async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.post('credit-notes/${cn.id}/pdf/generate');
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
            name: 'nota_credito_${cn.creditNoteNumber}.pdf',
          ),
        ]),
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Nota de crédito generada (${bytes.length ~/ 1024} KB)'),
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
    final state = ref.watch(creditNoteProvider);
    final theme = Theme.of(context);
    return Scaffold(
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : state.items.isEmpty
                  ? const Center(child: Text('No hay notas de crédito'))
                  : RefreshIndicator(
                      onRefresh: () => ref.read(creditNoteProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final cn = state.items[i];
                          return ListTile(
                            title: Text(cn.creditNoteNumber, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('Factura ${cn.invoiceNumber}  |  \$${cn.total.toStringAsFixed(2)}'),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Column(
                                  crossAxisAlignment: CrossAxisAlignment.end,
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Chip(label: Text(cn.status, style: const TextStyle(fontSize: 11, color: Colors.white)), backgroundColor: Colors.orange, materialTapTargetSize: MaterialTapTargetSize.shrinkWrap),
                                    Text(cn.issueDate.length >= 10 ? cn.issueDate.substring(0, 10) : cn.issueDate, style: const TextStyle(fontSize: 11, color: Colors.grey)),
                                  ],
                                ),
                                PopupMenuButton<String>(
                                  tooltip: 'Acciones',
                                  icon: const Icon(Icons.more_vert, size: 20),
                                  onSelected: (value) {
                                    if (value == 'pdf') _generatePdf(cn);
                                  },
                                  itemBuilder: (_) => [
                                    const PopupMenuItem(
                                      value: 'pdf',
                                      child: ListTile(
                                        leading: Icon(Icons.picture_as_pdf_outlined),
                                        title: Text('Generar PDF'),
                                      ),
                                    ),
                                  ],
                                ),
                              ],
                            ),
                          );
                        },
                      ),
                    ),
    );
  }
}
