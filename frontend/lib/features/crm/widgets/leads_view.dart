import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../models/crm_models.dart';
import '../providers/leads_provider.dart';
import '../utils/whatsapp_utils.dart';

class LeadsView extends ConsumerStatefulWidget {
  const LeadsView({super.key});

  @override
  ConsumerState<LeadsView> createState() => _LeadsViewState();
}

class _LeadsViewState extends ConsumerState<LeadsView> {
  final _searchController = TextEditingController();
  String? _statusFilter;

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(leadsProvider);
    final leads = _filterLeads(state.leads);

    if (state.loading && state.leads.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.error != null) {
      return Center(child: Text(state.error!));
    }

    return Column(
      children: [
        _buildFilterBar(state),
        Expanded(
          child: leads.isEmpty
              ? const ZEmptyState(
                  icon: Icons.person_search_outlined,
                  title: 'No hay prospectos',
                  subtitle: 'Inicia capturando un nuevo lead',
                )
              : RefreshIndicator(
                  onRefresh: () => ref.read(leadsProvider.notifier).loadLeads(),
                  child: ListView.separated(
                    padding: const EdgeInsets.all(16),
                    itemCount: leads.length + (_hasMorePages(state) ? 1 : 0),
                    separatorBuilder: (_, _) => const SizedBox(height: 12),
                    itemBuilder: (context, index) {
                      if (index >= leads.length) {
                        return _buildLoadMore(state);
                      }
                      return _LeadTile(
                        lead: leads[index],
                        onTap: () => context.push('/crm/leads/${leads[index].id}'),
                        onEdit: () => context.push('/crm/leads/${leads[index].id}/edit'),
                        onDelete: () => _confirmDelete(context, leads[index]),
                      );
                    },
                  ),
                ),
        ),
      ],
    );
  }

  Widget _buildFilterBar(LeadState state) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          Expanded(
            child: ZSearchField(
              hintText: 'Buscar leads...',
              controller: _searchController,
              onChanged: (_) => setState(() {}),
            ),
          ),
          const SizedBox(width: 12),
          PopupMenuButton<String?>(
            icon: Icon(
              Icons.filter_list,
              color: _statusFilter != null ? ZColors.brandPrimary : null,
            ),
            onSelected: (v) => setState(() {
              _statusFilter = v;
              if (v != null) {
                ref.read(leadsProvider.notifier).loadLeads(status: v);
              } else {
                ref.read(leadsProvider.notifier).loadLeads();
              }
            }),
            itemBuilder: (_) => [
              PopupMenuItem(value: null, child: Text(_statusFilter == null ? '✓ Todos' : 'Todos')),
              ...['new', 'contacted', 'qualified', 'lost'].map((s) => PopupMenuItem(
                value: s,
                child: Text(s.toUpperCase(), style: TextStyle(
                  fontWeight: _statusFilter == s ? FontWeight.bold : FontWeight.normal,
                  color: _statusFilter == s ? ZColors.brandPrimary : null,
                )),
              )),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildLoadMore(LeadState state) {
    return Center(
      child: TextButton(
        onPressed: state.loading ? null : () {
          ref.read(leadsProvider.notifier).loadLeads(page: state.page + 1, status: _statusFilter);
        },
        child: state.loading
            ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
            : Text('Cargar más (${state.leads.length} de ${state.totalCount})'),
      ),
    );
  }

  List<Lead> _filterLeads(List<Lead> leads) {
    final query = _searchController.text.toLowerCase().trim();
    if (query.isEmpty) return leads;
    return leads.where((l) =>
      l.fullName.toLowerCase().contains(query) ||
      (l.companyName?.toLowerCase().contains(query) ?? false) ||
      (l.email?.toLowerCase().contains(query) ?? false) ||
      (l.phone?.contains(query) ?? false)
    ).toList();
  }

  bool _hasMorePages(LeadState state) => state.leads.length < state.totalCount;

  Future<void> _confirmDelete(BuildContext context, Lead lead) async {
    final ok = await ZModal.confirm(context,
      title: 'Eliminar prospecto',
      message: '¿Eliminar a ${lead.fullName}? Esta acción no se puede deshacer.',
      confirmText: 'Eliminar',
      confirmColor: Colors.red,
    );
    if (ok) {
      final success = await ref.read(leadsProvider.notifier).deleteLead(lead.id);
      if (success && context.mounted) {
        ZToast.success(context, 'Prospecto eliminado');
      }
    }
  }
}

class _LeadTile extends StatelessWidget {
  final Lead lead;
  final VoidCallback onTap;
  final VoidCallback onEdit;
  final VoidCallback onDelete;

  const _LeadTile({
    required this.lead,
    required this.onTap,
    required this.onEdit,
    required this.onDelete,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
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
            PopupMenuButton<String>(
              onSelected: (v) {
                if (v == 'edit') onEdit();
                if (v == 'delete') onDelete();
              },
              itemBuilder: (_) => [
                const PopupMenuItem(value: 'edit', child: ListTile(
                  leading: Icon(Icons.edit_outlined, size: 18),
                  title: Text('Editar'),
                  dense: true,
                  contentPadding: EdgeInsets.zero,
                )),
                const PopupMenuItem(value: 'delete', child: ListTile(
                  leading: Icon(Icons.delete_outlined, size: 18, color: Colors.red),
                  title: Text('Eliminar', style: TextStyle(color: Colors.red)),
                  dense: true,
                  contentPadding: EdgeInsets.zero,
                )),
              ],
            ),
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
