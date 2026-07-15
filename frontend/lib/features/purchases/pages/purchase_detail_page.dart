import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/purchase_provider.dart';

final class PurchaseDetailPage extends ConsumerStatefulWidget {
  final String purchaseId;
  const PurchaseDetailPage({super.key, required this.purchaseId});
  @override
  ConsumerState<PurchaseDetailPage> createState() => _PurchaseDetailPageState();
}

final class _PurchaseDetailPageState extends ConsumerState<PurchaseDetailPage> {
  PurchaseDetail? _data;
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
      final r = await dio.get('purchases/${widget.purchaseId}');
      setState(() { _data = PurchaseDetail.fromJson(r.data); _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar compra'; _loading = false; });
    }
  }

  Future<void> _cancel() async {
    final confirm = await ZModal.confirm(
      context,
      title: 'Anular compra',
      message: '¿Está seguro de anular esta compra? Se revertirá el stock.',
      confirmText: 'Anular',
      cancelText: 'Cancelar',
    );
    if (!confirm) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('purchases/${widget.purchaseId}/cancel');
      ref.invalidate(purchaseProvider);
      _load();
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error al anular'), backgroundColor: Colors.red));
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (_loading) return Scaffold(body: const Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(body: Center(child: Text(_error!, style: TextStyle(color: theme.colorScheme.error))));
    final d = _data!;

    final statusColor = switch (d.status) {
      'completed' => Colors.green,
      'cancelled' => Colors.red,
      _ => Colors.orange,
    };

    return Scaffold(
      body: Column(
        children: [
          if (d.status != 'cancelled')
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  IconButton(icon: const Icon(Icons.cancel_outlined, size: 20), tooltip: 'Anular', onPressed: _cancel),
                ],
              ),
            ),
          Expanded(
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                ZCard(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          const Text('Estado: ', style: TextStyle(fontWeight: FontWeight.w600)),
                          Chip(label: Text(d.status, style: const TextStyle(fontSize: 11, color: Colors.white)), backgroundColor: statusColor, padding: EdgeInsets.zero, visualDensity: VisualDensity.compact),
                        ],
                      ),
                      const Divider(),
                      _row('Proveedor', d.supplierName),
                      _row('Fecha', d.createdAt.substring(0, 10)),
                      if (d.invoiceReference != null) _row('Ref. Factura', d.invoiceReference!),
                      if (d.purchaseDate != null) _row('Fecha Compra', d.purchaseDate!.substring(0, 10)),
                      const Divider(),
                      _row('Subtotal', '\$${d.subtotal.toStringAsFixed(2)}'),
                      _row('IVA', '\$${d.tax.toStringAsFixed(2)}'),
                      _row('Descuento', '\$${d.discount.toStringAsFixed(2)}'),
                      _row('Total', '\$${d.total.toStringAsFixed(2)}', bold: true),
                      if (d.notes != null) ...[const Divider(), _row('Notas', d.notes!)],
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                Text('Productos (${d.details.length})', style: TextStyle(fontWeight: FontWeight.w600, color: theme.colorScheme.primary)),
                const SizedBox(height: 8),
                ...d.details.map((i) => ZCard(
                  padding: const EdgeInsets.all(12),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(i.productName, style: const TextStyle(fontWeight: FontWeight.w600)),
                            Text('${i.quantity} x \$${i.unitCost.toStringAsFixed(2)}'),
                          ],
                        ),
                      ),
                      Text('\$${i.subtotal.toStringAsFixed(2)}', style: TextStyle(color: theme.colorScheme.primary, fontWeight: FontWeight.bold)),
                    ],
                  ),
                )),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _row(String label, String value, {bool bold = false}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.normal, color: Colors.grey[600])),
          Text(value, style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
        ],
      ),
    );
  }
}
