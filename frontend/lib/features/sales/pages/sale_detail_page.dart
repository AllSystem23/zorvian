import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/printing/pdf_generator.dart';
import '../../../shared/printing/print_share_sheet.dart';
import '../../../shared/printing/print_utils.dart';
import '../../../shared/printing/qr_code_dialog.dart';
import '../../../shared/printing/thermal_template.dart';
import '../providers/sale_provider.dart';

final class SaleDetailPage extends ConsumerStatefulWidget {
  final String saleId;
  const SaleDetailPage({super.key, required this.saleId});
  @override
  ConsumerState<SaleDetailPage> createState() => _SaleDetailPageState();
}

final class _SaleDetailPageState extends ConsumerState<SaleDetailPage> {
  SaleDetail? _data;
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
      final r = await dio.get('sales/${widget.saleId}');
      setState(() { _data = SaleDetail.fromJson(r.data as Map<String, dynamic>); _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar venta'; _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (_loading) return Scaffold(appBar: AppBar(title: const Text('Venta')), body: const Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(appBar: AppBar(title: const Text('Venta')), body: Center(child: Text(_error!)));

    final d = _data!;
    final isCash = d.saleType == 'cash';
    final stColor = switch (d.status) { 'completed' => Colors.green, 'cancelled' => Colors.red, _ => Colors.orange };

    return Scaffold(
      appBar: AppBar(
        title: Text('Factura ${d.invoiceNumber}'),
        actions: [
          IconButton(
            icon: const Icon(Icons.qr_code, size: 20),
            tooltip: 'Código QR',
            onPressed: () => showQrCodeDialog(context, ref,
              title: 'Factura ${d.invoiceNumber}',
              number: d.invoiceNumber,
              clientName: d.clientName,
              total: d.total,
              date: d.saleDate,
            ),
          ),
          IconButton(
            icon: const Icon(Icons.share, size: 20),
            tooltip: 'Compartir',
            onPressed: () => showModalBottomSheet(
              context: context,
              builder: (_) => PrintShareSheet(
                title: 'Factura ${d.invoiceNumber}',
                filename: 'factura_${d.invoiceNumber}',
                buildPdf: (c, s) => generateSalePdf(sale: d, company: c, settings: s),
                buildThermal: (c) => saleThermalHtml(
                  company: c,
                  invoiceNumber: d.invoiceNumber,
                  date: d.saleDate,
                  clientName: d.clientName,
                  saleType: d.saleType,
                  subtotal: d.subtotal,
                  discount: d.discount,
                  tax: d.tax,
                  total: d.total,
                  paidAmount: d.paidAmount,
                  balance: d.balance,
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
                buildText: (c) => saleTextSummary(
                  companyName: c['legalName'] as String? ?? c['name'] as String? ?? '',
                  invoiceNumber: d.invoiceNumber,
                  date: d.saleDate,
                  clientName: d.clientName,
                  total: d.total,
                  status: d.status,
                ),
                buildCsv: () async => saleCsvBytes(
                  companyName: '',
                  invoiceNumber: d.invoiceNumber,
                  date: d.saleDate,
                  clientName: d.clientName,
                  saleType: d.saleType,
                  subtotal: d.subtotal,
                  discount: d.discount,
                  tax: d.tax,
                  total: d.total,
                  paidAmount: d.paidAmount,
                  balance: d.balance,
                  status: d.status,
                  items: d.details.map((i) => {
                    'productName': i.productName,
                    'quantity': i.quantity,
                    'unitPrice': i.unitPrice,
                    'discount': i.discount,
                    'subtotal': i.subtotal,
                  }).toList(),
                ),
              ),
            ),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(children: [
                    Expanded(child: Text(d.clientName, style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold))),
                    Chip(label: Text(d.status, style: const TextStyle(fontSize: 11, color: Colors.white)), backgroundColor: stColor, materialTapTargetSize: MaterialTapTargetSize.shrinkWrap),
                  ]),
                  const SizedBox(height: 8),
                  _row('Factura', d.invoiceNumber),
                  _row('Tipo', isCash ? 'Contado' : 'Crédito'),
                  _row('Fecha', d.saleDate.length >= 10 ? d.saleDate.substring(0, 10) : d.saleDate),
                  const Divider(),
                  _row('Subtotal', '\$${d.subtotal.toStringAsFixed(2)}'),
                  _row('Descuento', '\$${d.discount.toStringAsFixed(2)}'),
                  _row('IVA (15%)', '\$${d.tax.toStringAsFixed(2)}'),
                  _row('Total', '\$${d.total.toStringAsFixed(2)}', bold: true),
                  if (!isCash) ...[
                    const Divider(),
                    _row('Pagado', '\$${d.paidAmount.toStringAsFixed(2)}'),
                    _row('Saldo', '\$${d.balance.toStringAsFixed(2)}', bold: true, color: Colors.red),
                    if (d.creditId != null)
                      InkWell(
                        child: _row('Crédito', d.creditId!, color: Colors.blue),
                        onTap: () {},
                      ),
                  ],
                  if (d.notes != null && d.notes!.isNotEmpty) ...[
                    const Divider(),
                    _row('Notas', d.notes!),
                  ],
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Text('Productos', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          ...d.details.map((item) => Card(
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(item.productName, style: const TextStyle(fontWeight: FontWeight.w600)),
                        Text('${item.quantity} x \$${item.unitPrice.toStringAsFixed(2)}', style: const TextStyle(color: Colors.grey, fontSize: 12)),
                      ],
                    ),
                  ),
                  Text('\$${item.subtotal.toStringAsFixed(2)}', style: const TextStyle(fontWeight: FontWeight.bold)),
                ],
              ),
            ),
          )),
        ],
      ),
    );
  }

  Widget _row(String label, String value, {bool bold = false, Color? color}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(color: Colors.grey)),
          Text(value, style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.normal, color: color)),
        ],
      ),
    );
  }
}
