import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../data/provider_repository.dart';
import '../../../core/entities/service_contract.dart';
import '../../../core/entities/payment_milestone.dart';

final contractDetailsProvider = FutureProvider.family<ServiceContract, String>((ref, id) async {
  final repository = ref.watch(providerRepositoryProvider);
  return repository.getContractById(id);
});

final contractMilestonesProvider = FutureProvider.family<List<PaymentMilestone>, String>((ref, contractId) async {
  final repository = ref.watch(providerRepositoryProvider);
  return repository.getMilestonesByContract(contractId);
});

class ServiceContractDetailPage extends ConsumerWidget {
  final String id;
  const ServiceContractDetailPage({super.key, required this.id});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final contractAsync = ref.watch(contractDetailsProvider(id));
    final milestonesAsync = ref.watch(contractMilestonesProvider(id));

    return Scaffold(
      appBar: AppBar(title: const Text('Gestión de Contrato')),
      body: contractAsync.when(
        data: (contract) => SingleChildScrollView(
          padding: const EdgeInsets.all(ZSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(contract.contractName, style: ZTypography.titleLarge),
              Text('Contrato #${contract.contractNumber}', style: ZTypography.bodySmall),
              const SizedBox(height: ZSpacing.lg),
              
              Row(
                children: [
                  Expanded(child: ZStatCard(label: 'Monto Total', value: '${contract.totalContractAmount} ${contract.currency}')),
                  const SizedBox(width: ZSpacing.md),
                  Expanded(child: ZBadge(label: contract.status, isSuccess: contract.status == 'active')),
                ],
              ),
              const SizedBox(height: ZSpacing.xl),
              
              Text('Hitos de Pago y Entregables', style: ZTypography.titleMedium),
              const SizedBox(height: ZSpacing.md),
              
              milestonesAsync.when(
                data: (milestones) => ListView.separated(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: milestones.length,
                  separatorBuilder: (_, __) => const SizedBox(height: ZSpacing.md),
                  itemBuilder: (context, index) {
                    final milestone = milestones[index];
                    return ZCard(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(milestone.name, style: ZTypography.labelLarge.copyWith(fontWeight: FontWeight.bold)),
                              ZBadge(label: milestone.status),
                            ],
                          ),
                          const SizedBox(height: ZSpacing.sm),
                          Text(milestone.description ?? '', style: ZTypography.bodySmall),
                          const Divider(height: ZSpacing.lg),
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text('Monto: ${milestone.amount} ${contract.currency}'),
                              if (milestone.status == 'pending')
                                ZButton(
                                  label: 'Aprobar Hito',
                                  onPressed: () async {
                                    await ref.read(providerRepositoryProvider).approveMilestone(milestone.id);
                                    ref.invalidate(contractMilestonesProvider(id));
                                  },
                                  size: ZButtonSize.small,
                                ),
                            ],
                          ),
                        ],
                      ),
                    );
                  },
                ),
                loading: () => const ZSkeleton(height: 200),
                error: (err, _) => Text('Error: $err'),
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
