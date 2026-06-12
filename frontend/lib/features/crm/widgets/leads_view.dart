import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../models/crm_models.dart';
import '../providers/leads_provider.dart';
import '../utils/whatsapp_utils.dart';

class LeadsView extends ConsumerWidget {
  const LeadsView({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(leadsProvider);

    if (state.loading && state.leads.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.error != null) {
      return Center(child: Text(state.error!));
    }

    return Column(
      children: [
        _buildFilterBar(context, ref, state),
        Expanded(
          child: state.leads.isEmpty
              ? const ZEmptyState(
                  icon: Icons.person_search_outlined,
                  title: 'No hay prospectos',
                  subtitle: 'Inicia capturando un nuevo lead',
                )
              : RefreshIndicator(
                  onRefresh: () => ref.read(leadsProvider.notifier).loadLeads(),
                  child: ListView.separated(
                    padding: const EdgeInsets.all(16),
                    itemCount: state.leads.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 12),
                    itemBuilder: (context, index) {
                      return _LeadTile(lead: state.leads[index]);
                    },
                  ),
                ),
        ),
      ],
    );
  }

  Widget _buildFilterBar(BuildContext context, WidgetRef ref, LeadState state) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          Expanded(
            child: ZSearchField(
              hintText: 'Buscar leads...',
              onChanged: (v) {
                // TODO: Implement search
              },
            ),
          ),
          const SizedBox(width: 12),
          IconButton(
            icon: const Icon(Icons.filter_list),
            onPressed: () {
              // TODO: Show filters
            },
          ),
        ],
      ),
    );
  }
}

class _LeadTile extends StatelessWidget {
  final Lead lead;

  const _LeadTile({required this.lead});

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: () {
        // TODO: Show lead detail
      },
      child: ZCard(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            CircleAvatar(
              backgroundColor: ZColors.brandPrimary.withAlpha(25),
              child: Text(
                lead.firstName.isNotEmpty ? lead.firstName[0].toUpperCase() : '?',
                style: const TextStyle(color: ZColors.brandPrimary, fontWeight: FontWeight.bold),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    lead.fullName,
                    style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.bold),
                  ),
                  Text(
                    '${lead.companyName ?? "Independiente"} • ${lead.source ?? "Sin Origen"}',
                    style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500),
                  ),
                ],
              ),
            ),
            if (lead.phone != null)
              IconButton(
                icon: const Icon(Icons.chat_bubble_outline, color: Colors.green, size: 20),
                onPressed: () => WhatsAppUtils.launchWhatsApp(
                  phone: lead.phone!,
                  message: WhatsAppUtils.getLeadWelcomeMessage(lead.firstName),
                ),
              ),
            _buildStatusChip(lead.status),
          ],
        ),
      ),
    );
  }

  Widget _buildStatusChip(String status) {
    final color = switch (status.toLowerCase()) {
      'new' => ZColors.info,
      'contacted' => ZColors.warning,
      'qualified' => ZColors.success,
      'lost' => ZColors.danger,
      _ => ZColors.neutral500,
    };

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withAlpha(25),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        status.toUpperCase(),
        style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: color),
      ),
    );
  }
}
