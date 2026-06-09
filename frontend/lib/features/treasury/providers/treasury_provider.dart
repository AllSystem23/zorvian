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

  Future<void> issueCheck(Map<String, dynamic> checkData) async {
    await _client.post('treasury/issue', data: checkData);
  }
}
