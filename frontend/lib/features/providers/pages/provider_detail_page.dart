import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/provider_state.dart';

class ProviderDetailPage extends ConsumerWidget {
  final String id;
  const ProviderDetailPage({super.key, required this.id});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final providerAsync = ref.watch(providerDetailsProvider(id));
    final contractsAsync = ref.watch(providerContractsProvider(id));

    return Scaffold(
      appBar: AppBar(title: const Text('Detalle del Prestador')),
      body: providerAsync.when(
        data: (provider) => SingleChildScrollView(
          padding: const EdgeInsets.all(ZSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Cabecera con Info Principal
              ZCard(
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    CircleAvatar(
                      radius: 32,
                      backgroundColor: ZColors.brandPrimary.withValues(alpha: 0.1),
                      child: Icon(Icons.business, size: 32, color: ZColors.brandPrimary),
                    ),
                    const SizedBox(width: ZSpacing.lg),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(provider.businessName, style: ZTypography.titleLarge),
                          Text(provider.serviceCategory, style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral500)),
                          const SizedBox(height: ZSpacing.sm),
                          ZBadge(text: provider.status, type: provider.status == 'active' ? ZBadgeType.success : ZBadgeType.neutral),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: ZSpacing.xl),
              
              // Detalles Fiscales y Contacto
              Text('Información Legal y Fiscal', style: ZTypography.titleMedium),
              const SizedBox(height: ZSpacing.md),
              ZCard(
                child: Column(
                  children: [
                    _InfoRow(label: 'Régimen Fiscal', value: provider.taxRegime ?? 'N/A'),
                    const Divider(height: ZSpacing.lg),
                    _InfoRow(label: 'Dirección Fiscal', value: provider.fiscalAddress ?? 'No registrada'),
                    const Divider(height: ZSpacing.lg),
                    _InfoRow(label: 'Licencia Profesional', value: provider.professionalLicense ?? 'N/A'),
                  ],
                ),
              ),
              const SizedBox(height: ZSpacing.xl),
              
              // Contratos
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('Contratos de Servicios', style: ZTypography.titleMedium),
                  ZButton(
                    text: 'Nuevo Contrato',
                    onPressed: () {},
                    type: ZButtonType.secondary,
                    fullWidth: false,
                  ),
                ],
              ),
              const SizedBox(height: ZSpacing.md),
              contractsAsync.when(
                data: (contracts) => contracts.isEmpty
                    ? ZEmptyState(icon: Icons.assignment, title: 'Sin contratos', subtitle: 'Este prestador no tiene contratos activos.')
                    : ListView.separated(
                        shrinkWrap: true,
                        physics: const NeverScrollableScrollPhysics(),
                        itemCount: contracts.length,
                        separatorBuilder: (_, _) => const SizedBox(height: ZSpacing.md),
                        itemBuilder: (context, index) {
                          final contract = contracts[index];
                          return ZCard(
                            child: ListTile(
                              leading: const Icon(Icons.description_outlined),
                              title: Text(contract.contractName),
                              subtitle: Text('Monto: ${contract.totalContractAmount} ${contract.currency}'),
                              trailing: ZBadge(text: contract.status),
                              onTap: () {
                                // TODO: Navegar a detalle de contrato
                              },
                            ),
                          );
                        },
                      ),
                loading: () => const ZSkeleton(height: 100),
                error: (err, _) => Text('Error al cargar contratos: $err'),
              ),
            ],
          ),
        ),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (err, _) => Center(child: Text('Error: $err')),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final String label;
  final String value;
  const _InfoRow({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label, style: ZTypography.labelMedium.copyWith(color: ZColors.neutral500)),
        const SizedBox(width: ZSpacing.md),
        Expanded(child: Text(value, style: ZTypography.bodyMedium, textAlign: TextAlign.right)),
      ],
    );
  }
}
