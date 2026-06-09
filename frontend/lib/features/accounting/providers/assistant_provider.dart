import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/auth/auth_provider.dart';

final assistantServiceProvider = Provider((ref) => AssistantService(ref.read(dioClientProvider)));

class AssistantService {
  final dynamic _client;
  AssistantService(this._client);

  Future<void> saveFeedback(Map<String, dynamic> feedbackData) async {
    await _client.post('financial-reports/assistant/feedback', data: feedbackData);
  }
}
