import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/crm_provider.dart';

/// CRMPage — Customer Relationship Management.
/// Shows contacts with pipeline stages (Lead → Prospect → Client).
class CRMPage extends ConsumerStatefulWidget {
  const CRMPage({super.key});

  @override
  ConsumerState<CRMPage> createState() => _CRMPageState();
}

class _CRMPageState extends ConsumerState<CRMPage> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(crmProvider.notifier).loadContacts();
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final state = ref.watch(crmProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('CRM — Gestión de Contactos'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => ref.read(crmProvider.notifier).loadContacts(),
          ),
        ],
      ),
      body: Column(
        children: [
          // ── Pipeline Stats ──
          _buildPipelineStats(state, isDark),

          // ── Filter Chips ──
          _buildFilterChips(state, isDark),

          // ── Contact List ──
          Expanded(
            child: state.loading
                ? const Center(child: CircularProgressIndicator())
                : state.error != null
                    ? Center(child: Text(state.error!))
                    : state.filteredContacts.isEmpty
                        ? const ZEmptyState(
                            icon: Icons.people_outline,
                            title: 'Sin contactos',
                            subtitle: 'Agrega tu primer contacto CRM',
                          )
                        : RefreshIndicator(
                            onRefresh: () => ref.read(crmProvider.notifier).loadContacts(),
                            child: ListView.separated(
                              padding: const EdgeInsets.symmetric(horizontal: 16),
                              itemCount: state.filteredContacts.length,
                              separatorBuilder: (_, _) => const Divider(height: 1),
                              itemBuilder: (_, i) {
                                final contact = state.filteredContacts[i];
                                return _ContactTile(
                                  contact: contact,
                                  isDark: isDark,
                                  onTap: () => _showContactDetail(context, contact),
                                );
                              },
                            ),
                          ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => _showAddContactSheet(context),
        child: const Icon(Icons.person_add_outlined),
      ),
    );
  }

  Widget _buildPipelineStats(CrmState state, bool isDark) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: Row(
        children: [
          _StatPill(
            label: 'Leads',
            count: state.totalLeads,
            color: ZColors.info,
            isDark: isDark,
          ),
          const SizedBox(width: 8),
          _StatPill(
            label: 'Prospectos',
            count: state.totalProspects,
            color: ZColors.warning,
            isDark: isDark,
          ),
          const SizedBox(width: 8),
          _StatPill(
            label: 'Clientes',
            count: state.totalClients,
            color: ZColors.success,
            isDark: isDark,
          ),
        ],
      ),
    );
  }

  Widget _buildFilterChips(CrmState state, bool isDark) {
    final filters = [
      ('all', 'Todos'),
      ('lead', 'Leads'),
      ('prospect', 'Prospectos'),
      ('client', 'Clientes'),
    ];

    return SizedBox(
      height: 40,
      child: ListView.separated(
        padding: const EdgeInsets.symmetric(horizontal: 16),
        scrollDirection: Axis.horizontal,
        itemCount: filters.length,
        separatorBuilder: (_, _) => const SizedBox(width: 8),
        itemBuilder: (_, i) {
          final f = filters[i];
          final isSelected = state.filterStatus == f.$1;
          return FilterChip(
            label: Text(f.$2, style: TextStyle(fontSize: 12)),
            selected: isSelected,
            onSelected: (_) => ref.read(crmProvider.notifier).setFilter(f.$1),
            selectedColor: ZColors.brandPrimary.withValues(alpha: 0.15),
            checkmarkColor: ZColors.brandPrimary,
            side: BorderSide(
              color: isSelected ? ZColors.brandPrimary : (isDark ? ZColors.darkBorder : ZColors.border),
            ),
          );
        },
      ),
    );
  }

  void _showContactDetail(BuildContext context, CrmContact contact) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (ctx) => DraggableScrollableSheet(
        initialChildSize: 0.7,
        minChildSize: 0.4,
        maxChildSize: 0.95,
        expand: false,
        builder: (ctx, controller) => _ContactDetailSheet(
          contact: contact,
          controller: controller,
        ),
      ),
    );
  }

  void _showAddContactSheet(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (ctx) => Padding(
        padding: EdgeInsets.only(
          bottom: MediaQuery.of(ctx).viewInsets.bottom,
        ),
        child: const _AddContactSheet(),
      ),
    );
  }
}

class _StatPill extends StatelessWidget {
  final String label;
  final int count;
  final Color color;
  final bool isDark;

  const _StatPill({
    required this.label,
    required this.count,
    required this.color,
    required this.isDark,
  });

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        decoration: BoxDecoration(
          color: color.withValues(alpha: isDark ? 0.15 : 0.1),
          borderRadius: BorderRadius.circular(ZRadii.md),
          border: Border.all(color: color.withValues(alpha: 0.3)),
        ),
        child: Column(
          children: [
            Text(
              '$count',
              style: ZTypography.titleMedium.copyWith(
                fontWeight: FontWeight.w700,
                color: color,
              ),
            ),
            Text(
              label,
              style: ZTypography.labelSmall.copyWith(
                color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                fontSize: 10,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ContactTile extends StatelessWidget {
  final CrmContact contact;
  final bool isDark;
  final VoidCallback onTap;

  const _ContactTile({
    required this.contact,
    required this.isDark,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final statusColor = switch (contact.status) {
      'lead' => ZColors.info,
      'prospect' => ZColors.warning,
      'client' => ZColors.success,
      _ => ZColors.neutral500,
    };

    final statusLabel = switch (contact.status) {
      'lead' => 'Lead',
      'prospect' => 'Prospecto',
      'client' => 'Cliente',
      _ => contact.status,
    };

    final initial = contact.name.isNotEmpty ? contact.name[0].toUpperCase() : '?';

    return ListTile(
      onTap: onTap,
      leading: CircleAvatar(
        backgroundColor: statusColor.withValues(alpha: 0.15),
        child: Text(
          initial,
          style: TextStyle(
            color: statusColor,
            fontWeight: FontWeight.w600,
            fontSize: 16,
          ),
        ),
      ),
      title: Text(
        contact.name,
        style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600),
      ),
      subtitle: Text(
        contact.company ?? contact.email ?? contact.phone ?? 'Sin datos',
        style: ZTypography.bodySmall.copyWith(
          color: isDark ? ZColors.neutral400 : ZColors.neutral500,
        ),
      ),
      trailing: Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(
          color: statusColor.withValues(alpha: 0.1),
          borderRadius: BorderRadius.circular(12),
        ),
        child: Text(
          statusLabel,
          style: TextStyle(
            fontSize: 10,
            color: statusColor,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
    );
  }
}

class _ContactDetailSheet extends StatelessWidget {
  final CrmContact contact;
  final ScrollController controller;

  const _ContactDetailSheet({
    required this.contact,
    required this.controller,
  });

  @override
  Widget build(BuildContext context) {
    final statusColor = switch (contact.status) {
      'lead' => ZColors.info,
      'prospect' => ZColors.warning,
      'client' => ZColors.success,
      _ => ZColors.neutral500,
    };

    return Column(
      children: [
        // Handle
        Container(
          margin: const EdgeInsets.only(top: 8),
          width: 40,
          height: 4,
          decoration: BoxDecoration(
            color: ZColors.neutral300,
            borderRadius: BorderRadius.circular(2),
          ),
        ),
        // Header
        Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              CircleAvatar(
                radius: 24,
                backgroundColor: statusColor.withValues(alpha: 0.15),
                child: Text(
                  contact.name.isNotEmpty ? contact.name[0].toUpperCase() : '?',
                  style: TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w700,
                    color: statusColor,
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      contact.name,
                      style: ZTypography.titleMedium.copyWith(fontWeight: FontWeight.w600),
                    ),
                    if (contact.company != null)
                      Text(
                        contact.company!,
                        style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500),
                      ),
                  ],
                ),
              ),
            ],
          ),
        ),
        const Divider(height: 1),
        // Details
        Expanded(
          child: ListView(
            controller: controller,
            padding: const EdgeInsets.all(16),
            children: [
              if (contact.email != null)
                _DetailRow(icon: Icons.email_outlined, label: 'Email', value: contact.email!),
              if (contact.phone != null)
                _DetailRow(icon: Icons.phone_outlined, label: 'Teléfono', value: contact.phone!),
              _DetailRow(
                icon: Icons.flag_outlined,
                label: 'Estado',
                value: contact.status.toUpperCase(),
                valueColor: statusColor,
              ),
              if (contact.notes != null && contact.notes!.isNotEmpty) ...[
                const SizedBox(height: 16),
                Text('Notas', style: ZTypography.labelMedium.copyWith(fontWeight: FontWeight.w600)),
                const SizedBox(height: 8),
                Text(
                  contact.notes!,
                  style: ZTypography.bodySmall.copyWith(
                    color: ZColors.neutral500,
                  ),
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

class _DetailRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color? valueColor;

  const _DetailRow({
    required this.icon,
    required this.label,
    required this.value,
    this.valueColor,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Icon(icon, size: 18, color: ZColors.neutral400),
          const SizedBox(width: 12),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
              Text(
                value,
                style: ZTypography.bodySmall.copyWith(
                  fontWeight: FontWeight.w500,
                  color: valueColor,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _AddContactSheet extends ConsumerStatefulWidget {
  const _AddContactSheet();

  @override
  ConsumerState<_AddContactSheet> createState() => _AddContactSheetState();
}

class _AddContactSheetState extends ConsumerState<_AddContactSheet> {
  final _formKey = GlobalKey<FormState>();
  final _nameCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _companyCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  String _status = 'lead';

  @override
  void dispose() {
    _nameCtrl.dispose();
    _emailCtrl.dispose();
    _phoneCtrl.dispose();
    _companyCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      child: Form(
        key: _formKey,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'Nuevo Contacto CRM',
                style: ZTypography.titleMedium.copyWith(fontWeight: FontWeight.w600),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _nameCtrl,
                decoration: const InputDecoration(labelText: 'Nombre *'),
                validator: (v) => v?.isEmpty ?? true ? 'Requerido' : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _emailCtrl,
                decoration: const InputDecoration(labelText: 'Email'),
                keyboardType: TextInputType.emailAddress,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _phoneCtrl,
                decoration: const InputDecoration(labelText: 'Teléfono'),
                keyboardType: TextInputType.phone,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _companyCtrl,
                decoration: const InputDecoration(labelText: 'Empresa'),
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                value: _status,
                decoration: const InputDecoration(labelText: 'Estado'),
                items: const [
                  DropdownMenuItem(value: 'lead', child: Text('Lead')),
                  DropdownMenuItem(value: 'prospect', child: Text('Prospecto')),
                  DropdownMenuItem(value: 'client', child: Text('Cliente')),
                ],
                onChanged: (v) => setState(() => _status = v ?? 'lead'),
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _notesCtrl,
                decoration: const InputDecoration(labelText: 'Notas'),
                maxLines: 2,
              ),
              const SizedBox(height: 20),
              FilledButton(
                onPressed: () async {
                  if (!_formKey.currentState!.validate()) return;
                  final data = {
                    'name': _nameCtrl.text.trim(),
                    'email': _emailCtrl.text.trim().isNotEmpty ? _emailCtrl.text.trim() : null,
                    'phone': _phoneCtrl.text.trim().isNotEmpty ? _phoneCtrl.text.trim() : null,
                    'company': _companyCtrl.text.trim().isNotEmpty ? _companyCtrl.text.trim() : null,
                    'status': _status,
                    'notes': _notesCtrl.text.trim().isNotEmpty ? _notesCtrl.text.trim() : null,
                  };
                  final success = await ref.read(crmProvider.notifier).addContact(data);
                  if (success && context.mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Contacto creado'), backgroundColor: ZColors.success),
                    );
                    Navigator.pop(context);
                  }
                },
                child: const Text('Guardar Contacto'),
              ),
              const SizedBox(height: 8),
            ],
          ),
        ),
      ),
    );
  }
}