import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../data/provider_repository.dart';
import '../../../core/entities/service_provider.dart';
import '../../../core/entities/service_contract.dart';
import '../../../core/entities/provider_invoice.dart';

final serviceProvidersProvider = FutureProvider<List<ServiceProvider>>((ref) async {
  final repository = ref.watch(providerRepositoryProvider);
  return repository.getProviders();
});

final providerDetailsProvider = FutureProvider.family<ServiceProvider, String>((ref, id) async {
  final repository = ref.watch(providerRepositoryProvider);
  return repository.getProviderById(id);
});

final providerContractsProvider = FutureProvider.family<List<ServiceContract>, String>((ref, providerId) async {
  final repository = ref.watch(providerRepositoryProvider);
  return repository.getContractsByProvider(providerId);
});

final allContractsProvider = FutureProvider<List<ServiceContract>>((ref) async {
  final repository = ref.watch(providerRepositoryProvider);
  return repository.getContracts();
});

final allInvoicesProvider = FutureProvider<List<ProviderInvoice>>((ref) async {
  final repository = ref.watch(providerRepositoryProvider);
  return repository.getInvoices();
});

final providerStatsProvider = Provider((ref) {
  final providers = ref.watch(serviceProvidersProvider).value ?? [];
  if (providers.isEmpty) return {'total': 0, 'active': 0};
  
  return {
    'total': providers.length,
    'active': providers.where((p) => p.status == 'active').length,
  };
});
