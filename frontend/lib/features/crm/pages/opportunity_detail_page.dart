import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../models/crm_models.dart';
import '../providers/opportunities_provider.dart';
import '../utils/whatsapp_utils.dart';

class OpportunityDetailPage extends ConsumerWidget {
  final String opportunityId;

  const OpportunityDetailPage({super.key, required this.opportunityId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(opportunitiesProvider);
    final opp = state.opportunities.where((o) => o.id == opportunityId).firstOrNull;

    if (opp == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Oportunidad')),
        body: const Center(child: Text('Oportunidad no encontrada')),
      );
    }

    final stage = state.stages.where((s) => s.id == opp.stageId).firstOrNull;

    return Scaffold(
      appBar: AppBar(
        title: Text(opp.title),
        actions: [
          IconButton(
            icon: const Icon(Icons.edit_outlined),
            onPressed: () => context.push('/crm/opportunities/$opportunityId/edit'),
          ),
          IconButton(
            icon: const Icon(Icons.delete_outline, color: ZColors.danger),
            onPressed: () => _confirmDelete(context, ref, opp),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildHeader(opp, stage),
            const SizedBox(height: 24),
            _buildValueSection(opp),
            const SizedBox(height: 24),
            _buildDetailsSection(opp),
            if (opp.description != null && opp.description!.isNotEmpty) ...[
              const SizedBox(height: 24),
              _buildDescriptionSection(opp),
            ],
            if (opp.contactPhone != null) ...[
              const SizedBox(height: 24),
              _buildContactSection(opp, context),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildHeader(Opportunity opp, PipelineStage? stage) {
    final stageColor = stage != null
        ? Color(int.parse(stage.color.replaceAll('#', '0xFF')))
        : ZColors.neutral500;

    return Row(
      children: [
        Container(
          width: 48,
          height: 48,
          decoration: BoxDecoration(
            color: stageColor.withAlpha(25),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Icon(Icons.trending_up, color: stageColor),
        ),
        const SizedBox(width: 16),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(opp.title, style: ZTypography.titleLarge),
              const SizedBox(height: 4),
              if (stage != null)
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  decoration: BoxDecoration(
                    color: stageColor.withAlpha(25),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(stage.name, style: TextStyle(fontSize: 12, color: stageColor)),
                ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildValueSection(Opportunity opp) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Valor', style: ZTypography.titleSmall),
          const SizedBox(height: 8),
          Text(
            '${opp.currencyCode} ${opp.expectedValue.toStringAsFixed(2)}',
            style: ZTypography.headlineMedium.copyWith(color: ZColors.brandPrimary),
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Text('Probabilidad: ', style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral500)),
              _buildProbabilityBar(opp.probability),
              const SizedBox(width: 8),
              Text('${opp.probability}%', style: ZTypography.bodyMedium),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildProbabilityBar(int prob) {
    final color = switch (prob) {
      >= 75 => ZColors.success,
      >= 50 => ZColors.warning,
      _ => ZColors.neutral500,
    };
    return Expanded(
      child: ClipRRect(
        borderRadius: BorderRadius.circular(4),
        child: LinearProgressIndicator(
          value: prob / 100,
          backgroundColor: color.withAlpha(25),
          color: color,
          minHeight: 8,
        ),
      ),
    );
  }

  Widget _buildDetailsSection(Opportunity opp) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Detalles', style: ZTypography.titleSmall),
          const SizedBox(height: 12),
          _detailRow(Icons.person_outlined, 'Cliente', opp.clientName ?? 'Sin cliente'),
          _detailRow(Icons.info_outlined, 'Estado', opp.status.toUpperCase()),
          _detailRow(Icons.flag_outlined, 'Prioridad', opp.priority.toUpperCase()),
          _detailRow(Icons.calendar_today_outlined, 'Cierre estimado',
              '${opp.expectedCloseDate.day}/${opp.expectedCloseDate.month}/${opp.expectedCloseDate.year}'),
          _detailRow(Icons.calendar_today_outlined, 'Creado', _formatDate(opp.createdAt)),
        ],
      ),
    );
  }

  Widget _buildDescriptionSection(Opportunity opp) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Descripción', style: ZTypography.titleSmall),
          const SizedBox(height: 8),
          Text(opp.description!, style: ZTypography.bodyMedium),
        ],
      ),
    );
  }

  Widget _buildContactSection(Opportunity opp, BuildContext context) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Contacto', style: ZTypography.titleSmall),
          const SizedBox(height: 8),
          InkWell(
            onTap: () => WhatsAppUtils.launchWhatsApp(
              phone: opp.contactPhone!,
              message: WhatsAppUtils.getOpportunityFollowUpMessage(
                opp.clientName ?? 'Cliente',
                opp.title,
              ),
            ),
            child: Row(
              children: [
                const Icon(Icons.chat_bubble_outline, color: Colors.green, size: 20),
                const SizedBox(width: 8),
                Text(opp.contactPhone!, style: ZTypography.bodyMedium.copyWith(color: ZColors.brandPrimary)),
                const Spacer(),
                const Icon(Icons.open_in_new, size: 14, color: ZColors.neutral400),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _detailRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        children: [
          Icon(icon, size: 16, color: ZColors.neutral500),
          const SizedBox(width: 8),
          Text('$label: ', style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral500)),
          Expanded(child: Text(value, style: ZTypography.bodyMedium)),
        ],
      ),
    );
  }

  String _formatDate(DateTime dt) => '${dt.day}/${dt.month}/${dt.year}';

  Future<void> _confirmDelete(BuildContext context, WidgetRef ref, Opportunity opp) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar oportunidad',
      message: '¿Eliminar "${opp.title}"? Esta acción no se puede deshacer.',
      confirmText: 'Eliminar',
      confirmColor: Colors.red,
    );
    if (ok) {
      final success = await ref.read(opportunitiesProvider.notifier).deleteOpportunity(opp.id);
      if (success && context.mounted) {
        ZToast.success(context, 'Oportunidad eliminada');
        context.pop();
      }
    }
  }
}
