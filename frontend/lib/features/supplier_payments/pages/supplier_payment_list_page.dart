import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
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

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(supplierPaymentProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(widget.purchaseId != null ? 'Pagos de la Compra' : 'Pagos a Proveedores'),
      ),
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
                            trailing: Text(
                              '\$${p.amount.toStringAsFixed(2)}',
                              style: TextStyle(
                                fontWeight: FontWeight.bold,
                                color: theme.colorScheme.primary,
                              ),
                            ),
                            onTap: () => context.push('/purchases/${p.purchaseId}'),
                          );
                        },
                      ),
                    ),
      floatingActionButton: widget.purchaseId == null
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
