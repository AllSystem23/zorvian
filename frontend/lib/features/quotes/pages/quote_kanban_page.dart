import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/quote_provider.dart';

final class QuoteKanbanPage extends ConsumerWidget {
  const QuoteKanbanPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(quoteProvider);
    final theme = Theme.of(context);

    final grouped = <String, List<QuoteItem>>{};
    for (final item in state.items) {
      grouped.putIfAbsent(item.status, () => []).add(item);
    }

    final statuses = ['pending', 'sent', 'accepted', 'rejected', 'expired', 'converted'];

    return Scaffold(
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : ListView.builder(
              scrollDirection: Axis.horizontal,
              itemCount: statuses.length,
              itemBuilder: (context, index) {
                final status = statuses[index];
                final items = grouped[status] ?? [];
                return Container(
                  width: 300,
                  margin: const EdgeInsets.all(8),
                  decoration: BoxDecoration(color: theme.colorScheme.surfaceContainer, borderRadius: BorderRadius.circular(12)),
                  child: Column(
                    children: [
                      Padding(
                        padding: const EdgeInsets.all(16),
                        child: Text('${status.toUpperCase()} (${items.length})', style: const TextStyle(fontWeight: FontWeight.bold)),
                      ),
                      Expanded(
                        child: DragTarget<QuoteItem>(
                          onAcceptWithDetails: (details) {
                            ref.read(quoteProvider.notifier).updateStatus(details.data.id, status);
                          },
                          builder: (context, candidateData, rejectedData) => ListView.builder(
                            itemCount: items.length,
                            itemBuilder: (context, i) {
                              final q = items[i];
                              return Draggable<QuoteItem>(
                                data: q,
                                feedback: Material(
                                  child: Container(
                                    width: 280,
                                    padding: const EdgeInsets.all(8),
                                    decoration: BoxDecoration(color: theme.colorScheme.surface, border: Border.all(color: theme.colorScheme.primary)),
                                    child: Text(q.clientName),
                                  ),
                                ),
                                child: Card(
                                  margin: const EdgeInsets.all(8),
                                  child: ListTile(
                                    title: Text(q.clientName),
                                    subtitle: Text('\$${q.total.toStringAsFixed(0)}'),
                                  ),
                                ),
                              );
                            },
                          ),
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),
    );
  }
}
