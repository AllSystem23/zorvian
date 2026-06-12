import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../models/crm_models.dart';
import '../providers/opportunities_provider.dart';
import '../utils/whatsapp_utils.dart';

class KanbanBoard extends ConsumerWidget {
  const KanbanBoard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(opportunitiesProvider);

    if (state.loading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.error != null) {
      return Center(child: Text(state.error!));
    }

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.all(16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: state.stages.map((stage) {
          final opportunities = state.getOpportunitiesByStage(stage.id);
          return _KanbanColumn(stage: stage, opportunities: opportunities);
        }).toList(),
      ),
    );
  }
}

class _KanbanColumn extends ConsumerWidget {
  final PipelineStage stage;
  final List<Opportunity> opportunities;

  const _KanbanColumn({
    required this.stage,
    required this.opportunities,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final color = Color(int.parse(stage.color.replaceAll('#', '0xFF')));

    return DragTarget<Opportunity>(
      onWillAccept: (data) => data?.stageId != stage.id,
      onAcceptWithDetails: (details) {
        final opportunity = details.data;
        ref.read(opportunitiesProvider.notifier).updateOpportunityStage(opportunity.id, stage.id);
      },
      builder: (context, candidateData, rejectedData) {
        return Container(
          width: 280,
          margin: const EdgeInsets.only(right: 16),
          decoration: BoxDecoration(
            color: candidateData.isNotEmpty
                ? color.withAlpha(25)
                : ZColors.neutral100.withAlpha(12),
            borderRadius: BorderRadius.circular(ZRadii.lg),
            border: candidateData.isNotEmpty ? Border.all(color: color, width: 2) : null,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Column Header
              Padding(
                padding: const EdgeInsets.all(12),
                child: Row(
                  children: [
                    Container(
                      width: 12,
                      height: 12,
                      decoration: BoxDecoration(
                        color: color,
                        shape: BoxShape.circle,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        stage.name,
                        style: ZTypography.titleSmall.copyWith(fontWeight: FontWeight.w700),
                      ),
                    ),
                    ZBadge(
                      text: '${opportunities.length}',
                      type: ZBadgeType.neutral,
                    ),
                  ],
                ),
              ),
              const Divider(height: 1),
              // Scrollable Cards
              Expanded(
                child: ListView.builder(
                  padding: const EdgeInsets.all(8),
                  itemCount: opportunities.length,
                  itemBuilder: (context, index) {
                    return _OpportunityCard(opportunity: opportunities[index]);
                  },
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _OpportunityCard extends StatelessWidget {
  final Opportunity opportunity;

  const _OpportunityCard({required this.opportunity});

  @override
  Widget build(BuildContext context) {
    return LongPressDraggable<Opportunity>(
      data: opportunity,
      feedback: Transform.rotate(
        angle: 0.05,
        child: SizedBox(
          width: 260,
          child: Material(
            elevation: 8,
            borderRadius: BorderRadius.circular(ZRadii.lg),
            child: _buildCardContent(),
          ),
        ),
      ),
      childWhenDragging: Opacity(
        opacity: 0.3,
        child: _buildCardContent(),
      ),
      child: _buildCardContent(),
    );
  }

  Widget _buildCardContent() {
    return ZCard(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                '${opportunity.currencyCode} ${opportunity.expectedValue.toStringAsFixed(0)}',
                style: ZTypography.labelMedium.copyWith(
                  fontWeight: FontWeight.w700,
                  color: ZColors.brandPrimary,
                ),
              ),
              _buildPriorityBadge(opportunity.priority),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            opportunity.title,
            style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600),
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              const Icon(Icons.person_outline, size: 14, color: ZColors.neutral500),
              const SizedBox(width: 4),
              Expanded(
                child: Text(
                  opportunity.clientName ?? 'Sin Cliente',
                  style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            ],
          ),
          const SizedBox(height: 4),
          Row(
            children: [
              const Icon(Icons.calendar_today_outlined, size: 14, color: ZColors.neutral500),
              const SizedBox(width: 4),
              Text(
                'Cierre: ${opportunity.expectedCloseDate.day}/${opportunity.expectedCloseDate.month}',
                style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500),
              ),
              const Spacer(),
              if (opportunity.contactPhone != null)
                Padding(
                  padding: const EdgeInsets.only(right: 8),
                  child: IconButton(
                    constraints: const BoxConstraints(),
                    padding: EdgeInsets.zero,
                    icon: const Icon(Icons.chat_bubble_outline, color: Colors.green, size: 18),
                    onPressed: () => WhatsAppUtils.launchWhatsApp(
                      phone: opportunity.contactPhone!,
                      message: WhatsAppUtils.getOpportunityFollowUpMessage(
                        opportunity.clientName ?? 'Cliente',
                        opportunity.title,
                      ),
                    ),
                  ),
                ),
              _buildProbability(opportunity.probability),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildPriorityBadge(String priority) {
    final color = switch (priority.toLowerCase()) {
      'high' => ZColors.danger,
      'medium' => ZColors.warning,
      'low' => ZColors.success,
      _ => ZColors.neutral500,
    };
    return Container(
      width: 8,
      height: 8,
      decoration: BoxDecoration(color: color, shape: BoxShape.circle),
    );
  }

  Widget _buildProbability(int prob) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: ZColors.brandPrimary.withAlpha(25),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        '$prob%',
        style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w600, color: ZColors.brandPrimary),
      ),
    );
  }
}
