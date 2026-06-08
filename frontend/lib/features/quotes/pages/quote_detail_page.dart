import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../../../shared/printing/pdf_generator.dart';
import '../../../shared/printing/print_share_sheet.dart';
import '../../../shared/printing/print_utils.dart';
import '../../../shared/printing/qr_code_dialog.dart';
import '../../../shared/printing/thermal_template.dart';
import '../providers/quote_provider.dart';

class QuoteDetailPage extends ConsumerStatefulWidget {
  final String quoteId;
  const QuoteDetailPage({super.key, required this.quoteId});

  @override
  ConsumerState<QuoteDetailPage> createState() => _QuoteDetailPageState();
}

class _QuoteDetailPageState extends ConsumerState<QuoteDetailPage> {
  QuoteDetail? _data;
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
      final r = await dio.get('quotes/${widget.quoteId}');
      setState(() { _data = QuoteDetail.fromJson(r.data as Map<String, dynamic>); _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar cotización'; _loading = false; });
    }
  }

  Future<void> _delete() async {
    final confirm = await ZModal.confirm(context,
      title: 'Eliminar cotización',
      message: '¿Está seguro?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
    );
    if (confirm != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('quotes/${widget.quoteId}');
      if (mounted) context.pop(true);
    } catch (_) {
      if (mounted) {
        ZToast.error(context, 'Error al eliminar');
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (_loading) return Scaffold(appBar: AppBar(title: const Text('Cotización')), body: const Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(appBar: AppBar(title: const Text('Cotización')), body: Center(child: Text(_error!)));

    final d = _data!;
    final stColor = switch (d.status) {
      'approved' => Colors.green,
      'rejected' => Colors.red,
      'converted' => Colors.blue,
      _ => Colors.orange,
    };

    return Scaffold(
      appBar: AppBar(
        title: Text('Cotización ${d.quoteNumber}'),
        actions: [
          IconButton(
            icon: const Icon(Icons.qr_code, size: 20),
            tooltip: 'Código QR',
            onPressed: () => showQrCodeDialog(context, ref,
              title: 'Cotización ${d.quoteNumber}',
              number: d.quoteNumber,
              clientName: d.clientName,
              total: d.total,
              date: d.quoteDate,
            ),
          ),
          IconButton(
            icon: const Icon(Icons.share, size: 20),
            tooltip: 'Compartir',
            onPressed: () => showModalBottomSheet(
              context: context,
              builder: (_) => PrintShareSheet(
                title: 'Cotización ${d.quoteNumber}',
                filename: 'cotizacion_${d.quoteNumber}',
                buildPdf: (c, s) => generateQuotePdf(quote: d, company: c, settings: s),
                buildThermal: (c) => quoteThermalHtml(
                  company: c,
                  quoteNumber: d.quoteNumber,
                  date: d.quoteDate,
                  expirationDate: d.expirationDate,
                  clientName: d.clientName,
                  employeeName: d.employeeName,
                  subtotal: d.subtotal,
                  discount: d.discount,
                  tax: d.tax,
                  total: d.total,
                  status: d.status,
                  notes: d.notes,
                  items: d.details.map((i) => {
                    'productName': i.productName,
                    'quantity': i.quantity,
                    'unitPrice': i.unitPrice,
                    'discount': i.discount,
                    'subtotal': i.subtotal,
                  }).toList(),
                ),
                buildText: (c) => quoteTextSummary(
                  companyName: c['legalName'] as String? ?? c['name'] as String? ?? '',
                  quoteNumber: d.quoteNumber,
                  date: d.quoteDate,
                  clientName: d.clientName,
                  total: d.total,
                  status: d.status,
                ),
              ),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.edit, size: 20),
            onPressed: () async {
              final result = await context.push<bool>('/quotes/${widget.quoteId}/edit');
              if (result == true) _load();
            },
          ),
          IconButton(
            icon: const Icon(Icons.delete, size: 20, color: Colors.red),
            onPressed: _delete,
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          ZCard(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(children: [
                  Expanded(child: Text(d.clientName, style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold))),
                  Chip(label: Text(d.status, style: const TextStyle(fontSize: 11, color: Colors.white)), backgroundColor: stColor),
                ]),
                const SizedBox(height: 8),
                _row('Folio', d.quoteNumber),
                _row('Fecha', d.quoteDate.length >= 10 ? d.quoteDate.substring(0, 10) : d.quoteDate),
                if (d.expirationDate != null) _row('Vence', d.expirationDate!.length >= 10 ? d.expirationDate!.substring(0, 10) : d.expirationDate!),
                if (d.employeeName != null) _row('Creado por', d.employeeName!),
                const Divider(),
                _row('Subtotal', '\$${d.subtotal.toStringAsFixed(2)}'),
                _row('Descuento', '\$${d.discount.toStringAsFixed(2)}'),
                _row('IVA (15%)', '\$${d.tax.toStringAsFixed(2)}'),
                _row('Total', '\$${d.total.toStringAsFixed(2)}', bold: true),
                if (d.notes != null && d.notes!.isNotEmpty) ...[
                  const Divider(),
                  _row('Notas', d.notes!),
                ],
              ],
            ),
          ),
          const SizedBox(height: 16),
          Text('Productos', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          ...d.details.map((item) => ZCard(
            child: ListTile(
              title: Text(item.productName),
              subtitle: Text('${item.quantity} x \$${item.unitPrice.toStringAsFixed(2)}'),
              trailing: Text('\$${item.subtotal.toStringAsFixed(2)}', style: const TextStyle(fontWeight: FontWeight.w600)),
            ),
          )),
        ],
      ),
    );
  }

  Widget _row(String label, String value, {bool bold = false, Color? color}) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(children: [
        Text('$label: ', style: TextStyle(color: theme.colorScheme.onSurfaceVariant, fontSize: 13)),
        Expanded(child: Text(value, style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.normal, color: color, fontSize: 13))),
      ]),
    );
  }
}
