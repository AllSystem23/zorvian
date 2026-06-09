import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/network/dio_client.dart';

// Provider para la lógica de tesorería
final treasuryServiceProvider = Provider<TreasuryService>((ref) {
  final dioClient = ref.watch(dioClientProvider);
  return TreasuryService(dioClient);
});

class TreasuryService {
  final DioClient _client;
  TreasuryService(this._client);

  Future<dynamic> issueCheck(Map<String, dynamic> checkData) async {
    return await _client.post('treasury/checks/issue', data: checkData);
  }

  Future<void> generateBankDepositEntry(Map<String, dynamic> entryData) async {
    await _client.post('treasury/bank-deposits/accounting-entry', data: entryData);
  }

  Future<void> generateCheckEntry(Map<String, dynamic> entryData) async {
    await _client.post('treasury/checks/accounting-entry', data: entryData);
  }

  Future<void> generateBankTransferEntry(Map<String, dynamic> entryData) async {
    await _client.post('treasury/bank-transfers/accounting-entry', data: entryData);
  }

  Future<void> generateBankCommissionEntry(Map<String, dynamic> entryData) async {
    await _client.post('treasury/bank-commissions/accounting-entry', data: entryData);
  }

  Future<void> generateCollectionEntry(Map<String, dynamic> entryData) async {
    await _client.post('treasury/collections/accounting-entry', data: entryData);
  }
}
