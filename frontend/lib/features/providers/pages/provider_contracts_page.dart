import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/provider_state.dart';
import '../../../core/entities/service_contract.dart';

class ProviderContractsPage extends ConsumerWidget {
  const ProviderContractsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final contractsAsync = ref.watch(allContractsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Contratos de Servicios')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            contractsAsync.when(
              data: (contracts) {
                final activeCount = contracts.where((c) => c.status == 'active').length;
                final totalAmount = contracts.fold(0.0, (sum, item) => sum + item.totalContractAmount);

                return Row(
                  children: [
                    Expanded(child: ZStatCard(label: 'Contratos Activos', value: activeCount.toString())),
                    const SizedBox(width: ZSpacing.md),
                    Expanded(child: ZStatCard(label: 'Compromiso Total', value: 'C$ ${totalAmount.toStringAsFixed(2)}')),
                  ],
                );
              },
              loading: () => const ZSkeleton(height: 100),
              error: (err, _) => const SizedBox.shrink(),
            ),
            const SizedBox(height: ZSpacing.xl),
            
            Text('Catálogo de Contratos Vigentes', style: ZTypography.titleLarge),
            const SizedBox(height: ZSpacing.md),
            
            contractsAsync.when(
              data: (contracts) => contracts.isEmpty
                  ? const ZEmptyState(title: 'Sin contratos', message: 'No hay contratos de servicios registrados.')
                  : ZDataTable(
                      columns: const ['Contrato #', 'Prestador', 'Monto', 'Estado', 'Acciones'],
                      rows: contracts.map((c) => {
                        'Contrato #': Text(c.contractNumber, style: ZTypography.labelMedium.copyWith(fontWeight: FontWeight.bold)),
                        'Prestador': Text(c.serviceProvider?.businessName ?? 'N/A'),
                        'Monto': Text('${c.totalContractAmount} ${c.currency}'),
                        'Estado': ZBadge(label: c.status, isSuccess: c.status == 'active'),
                        'Acciones': IconButton(
                          icon: const Icon(Icons.chevron_right),
                          onPressed: () => context.push('/providers/contracts/${c.id}'),
                        ),
                      }).toList(),
                    ),
              loading: () => const ZSkeleton(height: 400),
              error: (err, _) => ZAlertCard(title: 'Error', message: 'Error al cargar contratos: $err', isError: true),
            ),
          ],
        ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () {},
        label: const Text('Nuevo Contrato'),
        icon: const Icon(Icons.add),
      ),
    );
  }
}
