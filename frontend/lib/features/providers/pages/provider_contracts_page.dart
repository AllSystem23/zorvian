import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/providers/company_currency_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/provider_state.dart';
import '../../../core/entities/service_contract.dart';

class ProviderContractsPage extends ConsumerWidget {
  const ProviderContractsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final contractsAsync = ref.watch(allContractsProvider);
    final fmt = ref.watch(currencyFormatServiceProvider);

    return Scaffold(
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
                    Expanded(child: ZStatCard(title: 'Contratos Activos', value: activeCount.toString())),
                    const SizedBox(width: ZSpacing.md),
                    Expanded(child: ZStatCard(title: 'Compromiso Total', value: fmt.currency(totalAmount))),
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
                    ? ZEmptyState(icon: Icons.assignment, title: 'Sin contratos', subtitle: 'No hay contratos de servicios registrados.')
                    : ZDataTable<ServiceContract>(
                        columns: const [
                          ZColumn(id: 'contract', label: 'Contrato #'),
                          ZColumn(id: 'provider', label: 'Prestador'),
                          ZColumn(id: 'amount', label: 'Monto', numeric: true),
                          ZColumn(id: 'status', label: 'Estado'),
                          ZColumn(id: 'actions', label: ''),
                        ],
                        rows: contracts,
                        rowMapper: (c) => DataRow(cells: [
                          DataCell(Text(c.contractNumber, style: ZTypography.labelMedium.copyWith(fontWeight: FontWeight.bold))),
                          DataCell(Text(c.serviceProvider?.businessName ?? 'N/A')),
                          DataCell(Text('${c.totalContractAmount} ${c.currency}')),
                          DataCell(ZBadge(text: c.status, type: c.status == 'active' ? ZBadgeType.success : ZBadgeType.neutral)),
                          DataCell(IconButton(
                            icon: const Icon(Icons.chevron_right),
                            onPressed: () => context.push('/providers/contracts/${c.id}'),
                          )),
                        ]),
                      ),
              loading: () => const ZSkeleton(height: 400),
              error: (err, _) => ZAlertCard(message: 'Error al cargar contratos: $err', severity: 'high'),
            ),
          ],
        ),
      ),
    );
  }
}
