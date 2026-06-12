import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/provider_state.dart';
import '../data/provider_repository.dart';
import '../../../core/entities/provider_invoice.dart';

class ProviderInvoicesPage extends ConsumerWidget {
  const ProviderInvoicesPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final invoicesAsync = ref.watch(allInvoicesProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Pagos y Facturas de Prestadores')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            invoicesAsync.when(
              data: (invoices) {
                final pendingCount = invoices.where((i) => i.status == 'received').length;
                final totalPending = invoices
                    .where((i) => i.status == 'received')
                    .fold(0.0, (sum, item) => sum + item.netAmount);

                return Row(
                  children: [
                    Expanded(child: ZStatCard(title: 'Facturas Pendientes', value: pendingCount.toString())),
                    const SizedBox(width: ZSpacing.md),
                    Expanded(child: ZStatCard(title: 'Total por Pagar', value: 'C\$${totalPending.toStringAsFixed(2)}')),
                  ],
                );
              },
              loading: () => const ZSkeleton(height: 100),
              error: (err, _) => const SizedBox.shrink(),
            ),
            const SizedBox(height: ZSpacing.xl),
            
            Text('Registro de Facturas Recibidas', style: ZTypography.titleLarge),
            const SizedBox(height: ZSpacing.md),
            
            invoicesAsync.when(
              data: (invoices) => invoices.isEmpty
                    ? ZEmptyState(icon: Icons.receipt_long, title: 'Sin facturas', subtitle: 'No se han registrado facturas de prestadores.')
                    : ZDataTable<ProviderInvoice>(
                        columns: const [
                          ZColumn(id: 'invoice', label: 'Factura #'),
                          ZColumn(id: 'amount', label: 'Monto', numeric: true),
                          ZColumn(id: 'status', label: 'Estado'),
                          ZColumn(id: 'due', label: 'Vence'),
                          ZColumn(id: 'actions', label: ''),
                        ],
                        rows: invoices,
                        rowMapper: (i) => DataRow(cells: [
                          DataCell(Text(i.invoiceNumber, style: ZTypography.labelMedium.copyWith(fontWeight: FontWeight.bold))),
                          DataCell(Text('${i.invoiceAmount} ${i.currency}')),
                          DataCell(ZBadge(text: i.status, type: i.status == 'paid' ? ZBadgeType.success : ZBadgeType.neutral)),
                          DataCell(Text(i.invoiceDate.toString().split(' ')[0])),
                          DataCell(IconButton(
                            icon: const Icon(Icons.payment),
                            onPressed: () async {
                              await ref.read(providerRepositoryProvider).programPayment(i.id, DateTime.now(), 'AUTO-PAY');
                              ref.invalidate(allInvoicesProvider);
                            },
                          )),
                        ]),
                      ),
              loading: () => const ZSkeleton(height: 300),
              error: (err, _) => ZAlertCard(message: 'Error al cargar facturas: $err', severity: 'high'),
            ),
            
            const SizedBox(height: ZSpacing.xl),
            const ZAlertCard(
              message: 'Asegúrese de aplicar el 10% de IR a los prestadores de servicios profesionales antes de programar el pago.',
              severity: 'low',
            ),
          ],
        ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () {},
        label: const Text('Registrar Factura'),
        icon: const Icon(Icons.post_add),
      ),
    );
  }
}
