import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
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
      appBar: AppBar(title: Text('Factura ${d.invoiceNumber}')),
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
