import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/network/dio_client.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/entities/service_provider.dart';
import '../../../core/entities/service_contract.dart';
import '../../../core/entities/payment_milestone.dart';
import '../../../core/entities/provider_invoice.dart';

class ProviderRepository {
  final DioClient _apiClient;

  ProviderRepository(this._apiClient);

  Future<List<ServiceProvider>> getProviders() async {
    final response = await _apiClient.get('providers');
    return (response.data as List).map((e) => ServiceProvider.fromJson(e)).toList();
  }

  Future<ServiceProvider> createProvider(Map<String, dynamic> data) async {
    final response = await _apiClient.post('providers', data: data);
    return ServiceProvider.fromJson(response.data);
  }

  Future<ServiceProvider> updateProvider(String id, Map<String, dynamic> data) async {
    final response = await _apiClient.put('providers/$id', data: data);
    return ServiceProvider.fromJson(response.data);
  }

  Future<ServiceContract> createContract(Map<String, dynamic> data) async {
    final providerId = data['serviceProviderId'] as String? ?? '';
    final response = await _apiClient.post('providers/$providerId/contracts', data: data);
    return ServiceContract.fromJson(response.data);
  }

  Future<List<ServiceContract>> getContracts() async {
    final response = await _apiClient.get('providers/contracts');
    return (response.data as List).map((e) => ServiceContract.fromJson(e)).toList();
  }

  Future<ServiceProvider> getProviderById(String id) async {
    final response = await _apiClient.get('providers/$id');
    return ServiceProvider.fromJson(response.data);
  }

  Future<List<ServiceContract>> getContractsByProvider(String providerId) async {
    final response = await _apiClient.get('providers/$providerId/contracts');
    return (response.data as List).map((e) => ServiceContract.fromJson(e)).toList();
  }

  Future<ServiceContract> getContractById(String id) async {
    final response = await _apiClient.get('providers/contracts/$id');
    return ServiceContract.fromJson(response.data);
  }

  Future<List<PaymentMilestone>> getMilestonesByContract(String contractId) async {
    final response = await _apiClient.get('providers/contracts/$contractId/milestones');
    return (response.data as List).map((e) => PaymentMilestone.fromJson(e)).toList();
  }

  Future<void> approveMilestone(String milestoneId) async {
    await _apiClient.put('providers/milestones/$milestoneId/approve');
  }

  Future<void> completeMilestone(String milestoneId) async {
    await _apiClient.put('providers/milestones/$milestoneId/complete');
  }

  Future<ProviderInvoice> registerInvoice(String milestoneId, ProviderInvoice invoice) async {
    final response = await _apiClient.post('providers/milestones/$milestoneId/invoice', data: invoice.toJson());
    return ProviderInvoice.fromJson(response.data);
  }

  Future<List<ProviderInvoice>> getInvoices() async {
    final response = await _apiClient.get('providers/invoices');
    return (response.data as List).map((e) => ProviderInvoice.fromJson(e)).toList();
  }

  Future<void> programPayment(String invoiceId, DateTime date, String? reference) async {
    await _apiClient.post('providers/invoices/$invoiceId/program-payment', data: {
      'paymentDate': date.toIso8601String().split('T')[0],
      'paymentReference': reference,
    });
  }
}

final providerRepositoryProvider = Provider<ProviderRepository>((ref) {
  final dio = ref.watch(dioClientProvider);
  return ProviderRepository(dio);
});
