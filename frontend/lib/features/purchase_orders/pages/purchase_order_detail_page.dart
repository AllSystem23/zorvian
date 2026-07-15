import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/purchase_order_provider.dart';

final class PurchaseOrderDetailPage extends ConsumerStatefulWidget {
  final String purchaseOrderId;
  const PurchaseOrderDetailPage({super.key, required this.purchaseOrderId});
  @override
  ConsumerState<PurchaseOrderDetailPage> createState() => _PurchaseOrderDetailPageState();
}

final class _PurchaseOrderDetailPageState extends ConsumerState<PurchaseOrderDetailPage> {
  PurchaseOrderDetail? _data;
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
      final r = await dio.get('purchase-orders/${widget.purchaseOrderId}');
      setState(() { _data = PurchaseOrderDetail.fromJson(r.data); _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar orden de compra'; _loading = false; });
    }
  }

  String _statusLabel(String s) => switch (s) {
    'draft' => 'Borrador',
    'pending_approval' => 'Pendiente',
    'approved' => 'Aprobada',
    'partially_received' => 'Recibida Parcial',
    'completed' => 'Completada',
    'cancelled' => 'Anulada',
    _ => s,
  };

  Color _statusColor(String s) => switch (s) {
    'completed' => Colors.green,
    'cancelled' => Colors.red,
    'approved' => Colors.blue,
    'draft' => Colors.grey,
    'partially_received' => Colors.orange,
    _ => Colors.grey,
  };

  Future<void> _approve() async {
    final confirm = await ZModal.confirm(context, title: 'Aprobar orden', message: '¿Está seguro de aprobar esta orden de compra?');
    if (!confirm) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('purchase-orders/${widget.purchaseOrderId}/approve');
      ref.invalidate(purchaseOrderProvider);
      _load();
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error al aprobar'), backgroundColor: Colors.red));
    }
  }

  Future<void> _cancel() async {
    final confirm = await ZModal.confirm(context, title: 'Anular orden', message: '¿Está seguro de anular esta orden de compra?');
    if (!confirm) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('purchase-orders/${widget.purchaseOrderId}/cancel');
      ref.invalidate(purchaseOrderProvider);
      _load();
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error al anular'), backgroundColor: Colors.red));
    }
  }

  Future<void> _receive() async {
    final result = await showDialog<ReceivePurchaseOrderRequest>(
      context: context,
      builder: (_) => _ReceiveDialog(detail: _data!),
    );
    if (result == null) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('purchase-orders/${widget.purchaseOrderId}/receive', data: result.toJson());
      ref.invalidate(purchaseOrderProvider);
      _load();
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error al recibir'), backgroundColor: Colors.red));
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
                      Text(d.orderNumber, style: theme.textTheme.titleLarge),
                      Chip(label: Text(_statusLabel(d.status), style: TextStyle(color: _statusColor(d.status), fontSize: 12))),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Text('Proveedor: ${d.supplierName}', style: theme.textTheme.bodyMedium),
                  Text('Creada: ${d.createdAt}', style: theme.textTheme.bodySmall),
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
                            Text('${item.quantity} × ${item.unitCost.toStringAsFixed(2)} | Recibido: ${item.quantityReceived}', style: theme.textTheme.bodySmall),
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
                  const Text('Total', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                  Text('${d.currencyCode} ${d.total.toStringAsFixed(2)}', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                ],
              ),
            ),
            const SizedBox(height: 24),
            if (d.status == 'draft')
              Row(
                children: [
                  Expanded(child: ZButton(text: 'Anular', onPressed: _cancel, type: ZButtonType.secondary)),
                  const SizedBox(width: 12),
                  Expanded(child: ZButton(text: 'Aprobar', onPressed: _approve, type: ZButtonType.primary)),
                ],
              ),
            if (d.status == 'approved')
              Row(
                children: [
                  Expanded(child: ZButton(text: 'Anular', onPressed: _cancel, type: ZButtonType.secondary)),
                  const SizedBox(width: 12),
                  Expanded(child: ZButton(text: 'Recibir', onPressed: _receive, type: ZButtonType.primary)),
                ],
              ),
            if (d.status == 'partially_received')
              Center(child: ZButton(text: 'Recibir Restante', onPressed: _receive, type: ZButtonType.primary)),
            const SizedBox(height: 80),
          ],
        ),
      ),
    );
  }
}

final class _ReceiveDialog extends StatefulWidget {
  final PurchaseOrderDetail detail;
  const _ReceiveDialog({required this.detail});
  @override
  State<_ReceiveDialog> createState() => _ReceiveDialogState();
}

final class _ReceiveDialogState extends State<_ReceiveDialog> {
  late final List<_ReceiveLine> _lines;
  final _formKey = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    _lines = widget.detail.details.map((d) => _ReceiveLine(
      productId: d.productId,
      productName: d.productName,
      pending: d.quantity - d.quantityReceived,
      quantity: (d.quantity - d.quantityReceived).toString(),
      unitCost: d.unitCost.toString(),
    )).toList();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return AlertDialog(
      title: const Text('Recibir Orden de Compra'),
      content: SizedBox(
        width: 400,
        child: Form(
          key: _formKey,
          child: ListView(
            shrinkWrap: true,
            children: _lines.map((l) => Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(l.productName, style: const TextStyle(fontWeight: FontWeight.w500)),
                  Text('Pendiente: ${l.pending}', style: theme.textTheme.bodySmall),
                  Row(
                    children: [
                      Expanded(
                        child: TextFormField(
                          initialValue: l.quantity,
                          decoration: const InputDecoration(labelText: 'Cantidad', isDense: true),
                          keyboardType: TextInputType.number,
                          validator: (v) => (v == null || v.isEmpty) ? 'Requerido' : null,
                          onChanged: (v) => l.quantity = v,
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: TextFormField(
                          initialValue: l.unitCost,
                          decoration: const InputDecoration(labelText: 'Costo U.', isDense: true),
                          keyboardType: TextInputType.number,
                          onChanged: (v) => l.unitCost = v,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            )).toList(),
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancelar')),
        ZButton(text: 'Recibir', onPressed: () {
          if (!_formKey.currentState!.validate()) return;
          Navigator.pop(context, ReceivePurchaseOrderRequest(
            lines: _lines.where((l) => (int.tryParse(l.quantity) ?? 0) > 0).map((l) => ReceivePurchaseOrderLine(
              productId: l.productId,
              quantity: int.tryParse(l.quantity) ?? 0,
              unitCost: double.tryParse(l.unitCost) ?? 0,
            )).toList(),
          ));
        }),
      ],
    );
  }
}

final class _ReceiveLine {
  final String productId;
  final String productName;
  final int pending;
  String quantity;
  String unitCost;
  _ReceiveLine({required this.productId, required this.productName, required this.pending, required this.quantity, required this.unitCost});
}
