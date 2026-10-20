import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:share_plus/share_plus.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/service_payment_provider.dart';

final class ServicePaymentDetailPage extends ConsumerStatefulWidget {
  final String servicePaymentId;
  const ServicePaymentDetailPage({super.key, required this.servicePaymentId});
  @override
  ConsumerState<ServicePaymentDetailPage> createState() => _ServicePaymentDetailPageState();
}

final class _ServicePaymentDetailPageState extends ConsumerState<ServicePaymentDetailPage> {
  ServiceProviderPaymentDetail? _data;
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
      final r = await dio.get('service-provider-payments/${widget.servicePaymentId}');
      if (!mounted) return;
      setState(() {
        _data = ServiceProviderPaymentDetail.fromJson(r.data);
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = 'Error al cargar el pago';
        _loading = false;
      });
    }
  }

  Future<void> _action(String path, {Map<String, dynamic>? data}) async {
    final dio = ref.read(dioClientProvider);
    final label = switch (path) {
      'approve' => 'aprobar este pago',
      'pay' => 'marcar este pago como pagado',
      'cancel' => 'anular este pago',
      _ => 'ejecutar esta acción',
    };
    final ok = await ZConfirmDialog.show(
      context,
      title: 'Confirmar acción',
      message: '¿Desea $label?',
      confirmLabel: 'Aceptar',
      icon: Icons.help_outline,
      isDestructive: path == 'cancel',
    );
    if (!ok || !mounted) return;
    try {
      await dio.post(
        'service-provider-payments/${widget.servicePaymentId}/$path',
        data: data ?? const {},
      );
      ref.invalidate(servicePaymentProvider);
      await _load();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Acción ejecutada'), backgroundColor: Colors.green),
      );
    } on DioException catch (e) {
      final msg = e.response?.data is Map
          ? (e.response!.data['detail'] ?? 'Error en la acción').toString()
          : 'Error en la acción';
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(msg), backgroundColor: Colors.red));
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error en la acción'), backgroundColor: Colors.red),
        );
      }
    }
  }

  Future<void> _generatePdf() async {
    final dio = ref.read(dioClientProvider);
    try {
      final r = await dio.post<Map<String, dynamic>>(
        'service-provider-payments/${widget.servicePaymentId}/pdf/generate',
        options: Options(responseType: ResponseType.json),
      );
      final docId = r.data?['generatedDocumentId'] as String?;
      if (docId == null) throw Exception('No se generó el documento');
      final pdf = await dio.get<List<int>>(
        'documents/$docId/pdf',
        options: Options(responseType: ResponseType.bytes),
      );
      final bytes = Uint8List.fromList(pdf.data!);
      await SharePlus.instance.share(
        ShareParams(
          files: [XFile.fromData(bytes, mimeType: 'application/pdf', name: 'comprobante_pago.pdf')],
        ),
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Comprobante generado'), backgroundColor: Colors.green),
        );
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error al generar el PDF'), backgroundColor: Colors.red),
        );
      }
    }
  }

  String _statusLabel(String s) => switch (s) {
    'draft' => 'Borrador',
    'approved' => 'Aprobado',
    'paid' => 'Pagado',
    'cancelled' => 'Cancelado',
    _ => s,
  };

  Color _statusColor(String s) => switch (s) {
    'approved' => Colors.blue,
    'paid' => Colors.green,
    'cancelled' => Colors.red,
    'draft' => Colors.grey,
    _ => Colors.grey,
  };

  String _dedLabel(String t) => switch (t) {
    'advance_first_half' => 'Adelanto 1ª Quincena',
    'store_credit' => 'Crédito Interno Tienda',
    'salary_advance' => 'Adelanto Salarial',
    'cash_loan' => 'Préstamo en Efectivo',
    'inss' => 'INSS',
    'other' => 'Otra',
    _ => t,
  };

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (_loading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (_error != null) {
      return Scaffold(body: Center(child: Text(_error!, style: TextStyle(color: theme.colorScheme.error))));
    }
    final d = _data!;

    return Scaffold(
      appBar: AppBar(
        title: Text('Pago ${d.paymentNumber}'),
        actions: [
          PopupMenuButton<String>(
            onSelected: (v) {
              if (v == 'pdf') _generatePdf();
              if (v == 'edit') {
                context.push('/service-payments/edit/${d.id}');
              }
            },
            itemBuilder: (_) => [
              const PopupMenuItem(value: 'pdf', child: Text('Generar Comprobante PDF')),
              if (d.status == 'draft')
                const PopupMenuItem(value: 'edit', child: Text('Editar')),
            ],
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 900),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Header
                ZCard(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Text(d.paymentNumber,
                              style: theme.textTheme.titleLarge
                                  ?.copyWith(fontWeight: FontWeight.bold)),
                          const SizedBox(width: 8),
                          Chip(
                            label: Text(_statusLabel(d.status),
                                style: TextStyle(color: _statusColor(d.status))),
                          ),
                          const SizedBox(width: 8),
                          Chip(
                            label: Text(d.goalMet ? 'Meta: SÍ' : 'Meta: NO'),
                            backgroundColor:
                                d.goalMet ? Colors.green.shade50 : Colors.red.shade50,
                          ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      Text('${d.employeeName ?? '—'} (${d.employeeCode ?? '—'})',
                          style: theme.textTheme.titleMedium),
                      const SizedBox(height: 4),
                      Text('${d.periodLabel} · Fecha de pago: ${d.paymentDate}',
                          style: theme.textTheme.bodySmall),
                      if (d.branchName != null) ...[
                        const SizedBox(height: 4),
                        Text('Sucursal: ${d.branchName}',
                            style: theme.textTheme.bodySmall
                                ?.copyWith(fontWeight: FontWeight.w500)),
                      ],
                      if (d.paidAt != null) ...[
                        const SizedBox(height: 4),
                        Text(
                          'Pagado el ${d.paidAt!.substring(0, 10)}'
                          '${d.paymentReference != null ? ' · Ref: ${d.paymentReference}' : ''}',
                          style: const TextStyle(color: Colors.green, fontSize: 12),
                        ),
                      ],
                      if (d.notes != null && d.notes!.isNotEmpty) ...[
                        const SizedBox(height: 4),
                        Text('Notas: ${d.notes}', style: theme.textTheme.bodySmall),
                      ],
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                // Líneas de recaudo
                ZCard(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Recaudo y Comisión', style: theme.textTheme.titleMedium),
                      const SizedBox(height: 8),
                      SingleChildScrollView(
                        scrollDirection: Axis.horizontal,
                        child: DataTable(
                          columns: const [
                            DataColumn(label: Text('Línea')),
                            DataColumn(label: Text('Recaudo en Efectivo'), numeric: true),
                            DataColumn(label: Text('En Contrato'), numeric: true),
                            DataColumn(label: Text('Comisión %'), numeric: true),
                            DataColumn(label: Text('Comisión'), numeric: true),
                            DataColumn(label: Text('Meta Línea')),
                          ],
                          rows: [
                            for (final l in d.lines)
                              DataRow(cells: [
                                DataCell(Text(l.lineName)),
                                DataCell(Text(l.collectedAmount.toStringAsFixed(2))),
                                DataCell(Text(l.contractedAmount.toStringAsFixed(2))),
                                DataCell(Text('${l.commissionRate.toStringAsFixed(2)}%')),
                                DataCell(Text(l.commissionAmount.toStringAsFixed(2),
                                    style: const TextStyle(fontWeight: FontWeight.w600))),
                                DataCell(Icon(
                                  l.lineGoalMet ? Icons.check_circle : Icons.cancel,
                                  size: 18,
                                  color: l.lineGoalMet ? Colors.green : Colors.red,
                                )),
                              ]),
                          ],
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'Recaudo total: ${d.totalCollected.toStringAsFixed(2)}'
                        ' · Contrato: ${d.totalContracted.toStringAsFixed(2)}'
                        ' · Comisiones: ${d.totalCommission.toStringAsFixed(2)}'
                        ' · Modo meta: ${d.goalEvaluationMode == 'individual' ? 'por línea' : 'por total'}',
                        style: theme.textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                // Deducciones
                ZCard(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Deducciones', style: theme.textTheme.titleMedium),
                      const SizedBox(height: 8),
                      if (d.deductions.isEmpty)
                        const Text('Sin deducciones registradas.',
                            style: TextStyle(color: Colors.grey))
                      else
                        SingleChildScrollView(
                          scrollDirection: Axis.horizontal,
                          child: DataTable(
                            columns: const [
                              DataColumn(label: Text('Tipo')),
                              DataColumn(label: Text('Descripción')),
                              DataColumn(label: Text('Monto'), numeric: true),
                              DataColumn(label: Text('Acuerdo')),
                            ],
                            rows: [
                              for (final dd in d.deductions)
                                DataRow(cells: [
                                  DataCell(Text(_dedLabel(dd.deductionType))),
                                  DataCell(Text(dd.description ?? '—')),
                                  DataCell(Text(dd.amount.toStringAsFixed(2))),
                                  DataCell(Text(
                                    dd.agreementReference != null
                                        ? '${dd.agreementReference}'
                                        : '—',
                                    style: const TextStyle(fontSize: 12),
                                  )),
                                ]),
                            ],
                          ),
                        ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                // Totales
                ZCard(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      _totalRow('Total acumulado (bono + comisiones + otros)',
                          d.totalAccumulated),
                      _totalRow('Total deducciones', d.totalDeductions),
                      const Divider(height: 16),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text('NETO A RECIBIR',
                              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
                          Text(d.netAmount.toStringAsFixed(2),
                              style: const TextStyle(
                                  fontWeight: FontWeight.bold,
                                  fontSize: 18,
                                  color: Colors.green)),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 20),
                // Acciones por estado
                Row(
                  children: [
                    if (d.status == 'draft') ...[
                      FilledButton.icon(
                        icon: const Icon(Icons.check),
                        label: const Text('Aprobar'),
                        onPressed: () => _action('approve'),
                      ),
                      const SizedBox(width: 8),
                      OutlinedButton.icon(
                        icon: const Icon(Icons.edit),
                        label: const Text('Editar'),
                        onPressed: () => context.push('/service-payments/edit/${d.id}'),
                      ),
                      const SizedBox(width: 8),
                    ],
                    if (d.status == 'approved') ...[
                      FilledButton.icon(
                        icon: const Icon(Icons.paid),
                        label: const Text('Marcar Pagado'),
                        onPressed: () => _action('pay', data: const {}),
                      ),
                      const SizedBox(width: 8),
                    ],
                    if (d.status == 'draft' || d.status == 'approved')
                      OutlinedButton.icon(
                        icon: const Icon(Icons.cancel_outlined, color: Colors.red),
                        label: const Text('Anular'),
                        onPressed: () => _action('cancel'),
                      ),
                    const SizedBox(width: 8),
                    FilledButton.tonalIcon(
                      icon: const Icon(Icons.picture_as_pdf_outlined),
                      label: const Text('Comprobante PDF'),
                      onPressed: _generatePdf,
                    ),
                  ],
                ),
                const SizedBox(height: 40),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _totalRow(String label, double value) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label),
            Text(value.toStringAsFixed(2),
                style: const TextStyle(fontWeight: FontWeight.w600)),
          ],
        ),
      );
}
