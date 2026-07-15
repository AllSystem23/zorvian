import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../models/crm_models.dart';
import '../providers/crm_activity_provider.dart';
import '../providers/leads_provider.dart';
import '../utils/whatsapp_utils.dart';

class LeadDetailPage extends ConsumerStatefulWidget {
  final String leadId;

  const LeadDetailPage({super.key, required this.leadId});

  @override
  ConsumerState<LeadDetailPage> createState() => _LeadDetailPageState();
}

class _LeadDetailPageState extends ConsumerState<LeadDetailPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() {
      ref.read(activityProvider.notifier).loadActivities(widget.leadId);
    });
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(leadsProvider);
    final lead = state.leads.where((l) => l.id == widget.leadId).firstOrNull;

    if (lead == null) {
      return Scaffold(
        body: const Center(child: Text('Prospecto no encontrado')),
      );
    }

    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(lead.fullName, style: Theme.of(context).textTheme.titleLarge),
                Row(
                  children: [
                    IconButton(
                      icon: const Icon(Icons.edit_outlined),
                      onPressed: () => context.push('/crm/leads/${widget.leadId}/edit'),
                    ),
                    IconButton(
                      icon: const Icon(Icons.delete_outline, color: ZColors.danger),
                      onPressed: () => _confirmDelete(context, lead),
                    ),
                  ],
                ),
              ],
            ),
            const SizedBox(height: 16),
            _buildHeader(lead),
            const SizedBox(height: 24),
            _buildInfoSection(lead),
            const SizedBox(height: 24),
            _buildContactSection(lead),
            if (lead.notes != null && lead.notes!.isNotEmpty) ...[
              const SizedBox(height: 24),
              _buildNotesSection(lead),
            ],
            const SizedBox(height: 24),
            _buildActivitySection(lead),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader(Lead lead) {
    return Row(
      children: [
        CircleAvatar(
          radius: 32,
          backgroundColor: ZColors.brandPrimary.withAlpha(25),
          child: Text(
            lead.firstName.isNotEmpty ? lead.firstName[0].toUpperCase() : '?',
            style: const TextStyle(
              fontSize: 28,
              fontWeight: FontWeight.bold,
              color: ZColors.brandPrimary,
            ),
          ),
        ),
        const SizedBox(width: 16),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(lead.fullName, style: ZTypography.titleLarge),
              if (lead.companyName != null)
                Text(lead.companyName!, style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral500)),
              const SizedBox(height: 4),
              _buildStatusChip(lead.status),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildInfoSection(Lead lead) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Información', style: ZTypography.titleSmall),
          const SizedBox(height: 12),
          _infoRow(Icons.badge_outlined, 'Origen', lead.source ?? 'Sin origen'),
          if (lead.jobTitle != null)
            _infoRow(Icons.work_outlined, 'Cargo', lead.jobTitle!),
          if (lead.city != null)
            _infoRow(Icons.location_on_outlined, 'Ciudad', lead.city!),
          _infoRow(Icons.flag_outlined, 'País', lead.countryCode),
          _infoRow(Icons.bar_chart_outlined, 'Interés', lead.interestLevel ?? 'Sin definir'),
          _infoRow(Icons.calendar_today_outlined, 'Creado', _formatDate(lead.createdAt)),
        ],
      ),
    );
  }

  Widget _buildContactSection(Lead lead) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Contacto', style: ZTypography.titleSmall),
          const SizedBox(height: 12),
          if (lead.email != null) ...[
            _contactRow(Icons.email_outlined, lead.email!, null),
            const SizedBox(height: 8),
          ],
          if (lead.phone != null) ...[
            _contactRow(Icons.phone_outlined, lead.phone!, null),
            const SizedBox(height: 8),
          ],
          if (lead.whatsapp != null) ...[
            _contactRow(Icons.chat_bubble_outline, lead.whatsapp!, () {
              WhatsAppUtils.launchWhatsApp(
                phone: lead.whatsapp!,
                message: WhatsAppUtils.getLeadWelcomeMessage(lead.firstName),
              );
            }),
          ],
          if (lead.email == null && lead.phone == null && lead.whatsapp == null)
            Text('Sin información de contacto', style: ZTypography.bodySmall.copyWith(color: ZColors.neutral400)),
        ],
      ),
    );
  }

  Widget _buildNotesSection(Lead lead) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Notas', style: ZTypography.titleSmall),
          const SizedBox(height: 8),
          Text(lead.notes!, style: ZTypography.bodyMedium),
        ],
      ),
    );
  }

  Widget _buildActivitySection(Lead lead) {
    final activityState = ref.watch(activityProvider);

    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Actividad', style: ZTypography.titleSmall),
              TextButton.icon(
                onPressed: () => _showAddActivityDialog(lead),
                icon: const Icon(Icons.add, size: 16),
                label: const Text('Nuevo'),
              ),
            ],
          ),
          const SizedBox(height: 8),
          if (activityState.loading)
            const Center(child: Padding(
              padding: EdgeInsets.all(16),
              child: SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2)),
            ))
          else if (activityState.activities.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 16),
              child: Text('Sin actividad registrada', style: ZTypography.bodySmall.copyWith(color: ZColors.neutral400)),
            )
          else
            ...activityState.activities.map((a) => _buildActivityItem(a)),
        ],
      ),
    );
  }

  Widget _buildActivityItem(CrmActivity activity) {
    final icon = switch (activity.type) {
      'call' => Icons.phone_in_talk_outlined,
      'email' => Icons.email_outlined,
      'meeting' => Icons.groups_outlined,
      _ => Icons.note_outlined,
    };
    final color = switch (activity.type) {
      'call' => Colors.green,
      'email' => ZColors.info,
      'meeting' => ZColors.warning,
      _ => ZColors.neutral500,
    };

    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 32,
            height: 32,
            decoration: BoxDecoration(
              color: color.withAlpha(25),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(icon, size: 16, color: color),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(activity.subject, style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
                if (activity.description != null && activity.description!.isNotEmpty)
                  Padding(
                    padding: const EdgeInsets.only(top: 2),
                    child: Text(activity.description!, style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500)),
                  ),
                Padding(
                  padding: const EdgeInsets.only(top: 2),
                  child: Row(
                    children: [
                      Text(_formatDate(activity.createdAt), style: ZTypography.labelSmall.copyWith(color: ZColors.neutral400)),
                      if (activity.createdByUserName != null) ...[
                        const SizedBox(width: 8),
                        Text('por ${activity.createdByUserName}', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral400)),
                      ],
                    ],
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _infoRow(IconData icon, String label, String value) {
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

  Widget _contactRow(IconData icon, String value, VoidCallback? onTap) {
    return InkWell(
      onTap: onTap,
      child: Row(
        children: [
          Icon(icon, size: 16, color: ZColors.neutral500),
          const SizedBox(width: 8),
          Expanded(
            child: Text(value, style: ZTypography.bodyMedium.copyWith(
              color: onTap != null ? ZColors.brandPrimary : null,
            )),
          ),
          if (onTap != null)
            const Icon(Icons.open_in_new, size: 14, color: ZColors.neutral400),
        ],
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

  String _formatDate(DateTime dt) => '${dt.day}/${dt.month}/${dt.year}';

  void _showAddActivityDialog(Lead lead) {
    final typeController = TextEditingController();
    final descController = TextEditingController();
    String selectedType = 'note';

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Nueva actividad'),
        content: StatefulBuilder(
          builder: (ctx, setDialogState) => Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DropdownButtonFormField<String>(
                decoration: const InputDecoration(labelText: 'Tipo'),
                initialValue: selectedType,
                items: const [
                  DropdownMenuItem(value: 'note', child: Text('Nota')),
                  DropdownMenuItem(value: 'call', child: Text('Llamada')),
                  DropdownMenuItem(value: 'email', child: Text('Email')),
                  DropdownMenuItem(value: 'meeting', child: Text('Reunión')),
                ],
                onChanged: (v) => selectedType = v!,
              ),
              const SizedBox(height: 12),
              TextField(
                controller: typeController,
                decoration: const InputDecoration(labelText: 'Asunto', border: OutlineInputBorder()),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: descController,
                decoration: const InputDecoration(labelText: 'Descripción', border: OutlineInputBorder()),
                maxLines: 3,
              ),
            ],
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancelar')),
          FilledButton(onPressed: () async {
            if (typeController.text.isEmpty) return;
            final data = {
              'type': selectedType,
              'subject': typeController.text,
              'description': descController.text.isNotEmpty ? descController.text : null,
            };
            await ref.read(activityProvider.notifier).addActivity(lead.id, data);
            if (ctx.mounted) Navigator.pop(ctx);
          }, child: const Text('Guardar')),
        ],
      ),
    );
  }

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
        context.pop();
      }
    }
  }
}
