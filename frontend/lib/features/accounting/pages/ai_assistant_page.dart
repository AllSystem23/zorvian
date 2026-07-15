import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/features/accounting/models/enhanced_report_models.dart';
import 'package:zorvian/features/accounting/providers/enhanced_reports_provider.dart';
import 'package:zorvian/features/accounting/providers/assistant_provider.dart';
import 'package:zorvian/shared/ds/ds.dart';

final class AiAssistantPage extends ConsumerStatefulWidget {
  const AiAssistantPage({super.key});
  @override
  ConsumerState<AiAssistantPage> createState() => _AiAssistantPageState();
}

final class _MessageEntry {
  final String query;
  final bool isUser;
  final FinancialAssistantResponse? response;
  _MessageEntry({required this.query, required this.isUser, this.response});
}

final class _AiAssistantPageState extends ConsumerState<AiAssistantPage> {
  final _queryController = TextEditingController();
  final _messages = <_MessageEntry>[];
  final _scrollController = ScrollController();
  String _lastQuery = '';

  @override
  void dispose() {
    _queryController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  void _submitFeedback(_MessageEntry message, bool helpful) async {
    final service = ref.read(assistantServiceProvider);
    try {
      await service.saveFeedback({
        'query': message.query,
        'response': message.response?.answer,
        'isHelpful': helpful,
        'comments': null
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Gracias por tu feedback')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e')),
        );
      }
    }
  }

  void sendQuery() {
    final query = _queryController.text.trim();
    if (query.isEmpty) return;
    _queryController.clear();

    // Construir contexto con las últimas 5 interacciones
    final context = _messages.reversed.take(5).map((m) => m.query).toList().reversed.join(' | ');
    final fullQuery = context.isNotEmpty ? '$context | $query' : query;

    setState(() {
      _lastQuery = fullQuery;
      _messages.add(_MessageEntry(query: query, isUser: true));
    });
    
    Future.delayed(const Duration(milliseconds: 300), () {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(_scrollController.position.maxScrollExtent,
            duration: const Duration(milliseconds: 200), curve: Curves.easeOut);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      body: Column(
        children: [
          Expanded(
            child: ListView.builder(
              controller: _scrollController,
              padding: const EdgeInsets.all(ZSpacing.md),
              itemCount: _messages.length,
              itemBuilder: (_, i) {
                final msg = _messages[i];
                if (msg.isUser) {
                  return Align(
                    alignment: Alignment.centerRight,
                    child: Container(
                      margin: const EdgeInsets.only(bottom: 8),
                      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                      decoration: BoxDecoration(
                        color: theme.colorScheme.primaryContainer,
                        borderRadius: BorderRadius.circular(16).copyWith(bottomRight: Radius.zero),
                      ),
                      child: Text(msg.query, style: TextStyle(color: theme.colorScheme.onPrimaryContainer)),
                    ),
                  );
                }
                return _AssistantBubble(
                  response: msg.response!,
                  theme: theme,
                  onFeedback: (helpful) => _submitFeedback(msg, helpful),
                );
              },
            ),
          ),
          if (_lastQuery.isNotEmpty)
            ref.watch(enhancedAssistantProvider(_lastQuery)).when(
              loading: () => const Padding(
                padding: EdgeInsets.all(8),
                child: ZInlineLoading(message: 'Analizando...'),
              ),
              error: (e, _) => Padding(
                padding: const EdgeInsets.all(8),
                child: ZBadge(text: 'Error: $e', type: ZBadgeType.danger),
              ),
              data: (data) {
                WidgetsBinding.instance.addPostFrameCallback((_) {});
                if (_messages.isNotEmpty && _messages.last.response == null && !_messages.last.isUser) {
                  return const SizedBox.shrink();
                }
                setState(() {
                  if (_messages.isNotEmpty && _messages.last.isUser) {
                    _messages.add(_MessageEntry(query: _lastQuery, isUser: false, response: data));
                  }
                });
                return const SizedBox.shrink();
              },
            ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.sm),
            decoration: BoxDecoration(
              color: theme.colorScheme.surface,
              border: Border(top: BorderSide(color: theme.dividerColor)),
            ),
            child: Row(
              children: [
                Expanded(
                  child: ZTextField(
                    controller: _queryController,
                    label: 'Consulta',
                    hint: 'Ej: balance general período 01-2025',
                    onChanged: (_) {},
                  ),
                ),
                const SizedBox(width: ZSpacing.sm),
                IconButton.filled(
                  onPressed: sendQuery,
                  icon: const Icon(Icons.send),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _AssistantBubble extends StatelessWidget {
  final FinancialAssistantResponse response;
  final ThemeData theme;
  final Function(bool) onFeedback;
  const _AssistantBubble({required this.response, required this.theme, required this.onFeedback});

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(ZSpacing.md),
      decoration: BoxDecoration(
        color: const Color(0xFFF5F5F5),
        borderRadius: BorderRadius.circular(16).copyWith(bottomLeft: Radius.zero),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(response.answer, style: const TextStyle(fontSize: 13)),
          if (response.confidence != null) ...[
            const SizedBox(height: ZSpacing.xs),
            ZBadge(
              text: 'Confianza: ${response.confidence}',
              type: response.confidence == 'high' ? ZBadgeType.success : ZBadgeType.warning,
            ),
          ],
          if (response.supportingData != null && response.supportingData!.isNotEmpty) ...[
            const SizedBox(height: ZSpacing.sm),
            const Text('Datos:', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 11)),
            const SizedBox(height: ZSpacing.xs),
            ...response.supportingData!.map((d) => Padding(
              padding: const EdgeInsets.symmetric(vertical: 1),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(child: Text(d.label, style: const TextStyle(fontSize: 11))),
                  Text(
                    _fmt(d.value, d.format),
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.bold,
                      color: d.format?.contains('negative') == true
                          ? Colors.red
                          : d.format?.contains('positive') == true
                              ? Colors.green
                              : null,
                    ),
                  ),
                ],
              ),
            )),
          ],
          const SizedBox(height: ZSpacing.sm),
          Row(
            children: [
              IconButton(icon: const Icon(Icons.thumb_up_outlined, size: 16), onPressed: () => onFeedback(true)),
              IconButton(icon: const Icon(Icons.thumb_down_outlined, size: 16), onPressed: () => onFeedback(false)),
            ],
          ),
        ],
      ),
    );
  }

  String _fmt(double v, String? f) {
    if (f == 'percentage') return '${v.toStringAsFixed(2)}%';
    if (f == 'integer') return v.toInt().toString();
    if (f == 'text') return '';
    return '\$${v.toStringAsFixed(2)}';
  }
}
