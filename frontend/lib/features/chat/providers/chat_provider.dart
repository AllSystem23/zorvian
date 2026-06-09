import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/auth/auth_provider.dart';

class ChatMessage {
  final String role;
  final String content;
  final DateTime timestamp;
  ChatMessage({required this.role, required this.content, DateTime? timestamp})
      : timestamp = timestamp ?? DateTime.now();
}

class ChatState {
  final List<ChatMessage> messages;
  final bool loading;
  final String? error;
  const ChatState({this.messages = const [], this.loading = false, this.error});
  ChatState copyWith({List<ChatMessage>? messages, bool? loading, String? error}) =>
      ChatState(messages: messages ?? this.messages, loading: loading ?? this.loading, error: error);
}

class ChatNotifier extends Notifier<ChatState> {
  @override
  ChatState build() => const ChatState();

  Future<void> sendMessage(String text) async {
    final userMsg = ChatMessage(role: 'user', content: text);
    state = state.copyWith(messages: [...state.messages, userMsg], loading: true, error: null);

    try {
      final dio = ref.read(dioClientProvider);
      final res = await dio.post('chat/ask', data: {'question': text});
      final answer = res.data['answer'] ?? '';
      state = state.copyWith(
        messages: [...state.messages, ChatMessage(role: 'assistant', content: answer)],
        loading: false,
      );
    } catch (e) {
      state = state.copyWith(
        messages: [...state.messages, ChatMessage(role: 'assistant', content: 'Error: No se pudo obtener respuesta. Verifica que el servicio de IA esté configurado.')],
        loading: false,
        error: e.toString(),
      );
    }
  }
}

final chatProvider = NotifierProvider<ChatNotifier, ChatState>(ChatNotifier.new);
