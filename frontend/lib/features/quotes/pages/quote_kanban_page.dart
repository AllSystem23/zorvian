import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/quote_provider.dart';

final class QuoteKanbanPage extends ConsumerWidget {
  const QuoteKanbanPage({super.key});

  static const statuses = [
    'pending',
    'sent',
    'accepted',
    'rejected',
    'expired',
    'converted',
  ];

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(quoteProvider);
    final theme = Theme.of(context);

    final grouped = <String, List<QuoteItem>>{};
    for (final item in state.items) {
      grouped.putIfAbsent(item.status, () => []).add(item);
    }

    // Teléfono: las columnas de 300 px con drag & drop no caben. Se usa una
    // lista vertical por estado y el estado se cambia desde el menú de la fila.
    if (MediaQuery.sizeOf(context).width < ZBreakpoints.phone) {
      return Scaffold(
        body: state.loading
            ? const Center(child: CircularProgressIndicator())
            : ListView(
                padding: const EdgeInsets.all(ZSpacing.lg),
                children: [
                  for (final status in statuses)
                    _MobileStatusSection(
                      status: status,
                      items: grouped[status] ?? const [],
                      allStatuses: statuses,
                    ),
                ],
              ),
      );
    }

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
                        child: Text('${_statusLabel(status)} (${items.length})', style: const TextStyle(fontWeight: FontWeight.bold)),
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

String _statusLabel(String status) => switch (status) {
      'pending' => 'PENDIENTE',
      'sent' => 'ENVIADA',
      'accepted' => 'ACEPTADA',
      'rejected' => 'RECHAZADA',
      'expired' => 'VENCIDA',
      'converted' => 'CONVERTIDA',
      _ => status.toUpperCase(),
    };

/// Sección vertical por estado para teléfono (reemplaza la columna de 300 px).
class _MobileStatusSection extends ConsumerWidget {
  final String status;
  final List<QuoteItem> items;
  final List<String> allStatuses;

  const _MobileStatusSection({
    required this.status,
    required this.items,
    required this.allStatuses,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Padding(
      padding: const EdgeInsets.only(bottom: ZSpacing.xl),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            '${_statusLabel(status)} (${items.length})',
            style: ZTypography.titleSmall.copyWith(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: ZSpacing.sm),
          if (items.isEmpty)
            Text(
              'Sin cotizaciones',
              style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500),
            )
          else
            for (final quote in items)
              Card(
                margin: const EdgeInsets.only(bottom: ZSpacing.sm),
                child: ListTile(
                  title: Text(
                    quote.clientName,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  subtitle: Text('\$${quote.total.toStringAsFixed(0)}'),
                  trailing: PopupMenuButton<String>(
                    tooltip: 'Mover a otro estado',
                    onSelected: (next) =>
                        ref.read(quoteProvider.notifier).updateStatus(quote.id, next),
                    itemBuilder: (_) => [
                      for (final option
                          in allStatuses.where((s) => s != status))
                        PopupMenuItem<String>(
                          value: option,
                          child: Text(_statusLabel(option)),
                        ),
                    ],
                  ),
                ),
              ),
        ],
      ),
    );
  }
}
