import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/core/providers/company_branch_provider.dart';
import 'package:zorvian/shared/ds/ds.dart';

import '../providers/access_request_provider.dart';

/// Revisión de las solicitudes de acceso ("Solicitar Acceso") del registro
/// público. Al aprobar, el backend emite el código de invitación para la
/// compañía elegida y lo envía al solicitante por correo.
class AccessRequestsPage extends ConsumerStatefulWidget {
  const AccessRequestsPage({super.key});

  @override
  ConsumerState<AccessRequestsPage> createState() => _AccessRequestsPageState();
}

class _AccessRequestsPageState extends ConsumerState<AccessRequestsPage> {
  String _filter = 'all';

  static const _roles = [
    'Employee',
    'Supervisor',
    'Rrhh',
    'Accountant',
    'CompanyAdmin',
  ];

  @override
  Widget build(BuildContext context) {
    final requests = ref.watch(accessRequestProvider);

    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Expanded(
                  child: Text(
                    'Solicitudes de Acceso',
                    style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                  ),
                ),
                ZButton(
                  text: 'Actualizar',
                  icon: Icons.refresh_rounded,
                  fullWidth: false,
                  type: ZButtonType.secondary,
                  onPressed: () =>
                      ref.read(accessRequestProvider.notifier).load(),
                ),
              ],
            ),
            const SizedBox(height: ZSpacing.md),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _buildChip('Todas', 'all'),
                _buildChip('Pendientes', 'pending'),
                _buildChip('Aprobadas', 'approved'),
                _buildChip('Rechazadas', 'rejected'),
              ],
            ),
            const SizedBox(height: ZSpacing.md),
            Expanded(
              child: requests.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (error, _) => Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.error_outline, size: 48, color: ZColors.danger),
                      const SizedBox(height: 12),
                      Text(
                        'No se pudieron cargar las solicitudes',
                        style: const TextStyle(color: ZColors.danger),
                      ),
                      const SizedBox(height: 12),
                      ZButton(
                        text: 'Reintentar',
                        fullWidth: false,
                        onPressed: () =>
                            ref.read(accessRequestProvider.notifier).load(),
                      ),
                    ],
                  ),
                ),
                data: (items) {
                  final filtered = _applyFilter(items);
                  if (filtered.isEmpty) {
                    return Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.inbox_outlined, size: 64, color: ZColors.neutral400),
                          const SizedBox(height: 12),
                          const Text(
                            'No hay solicitudes de acceso en este estado',
                            style: TextStyle(color: ZColors.neutral500),
                          ),
                        ],
                      ),
                    );
                  }
                  return _buildTable(filtered);
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  List<AccessRequestModel> _applyFilter(List<AccessRequestModel> items) {
    if (_filter == 'all') return items;
    return items.where((r) => r.status == _filter).toList();
  }

  Widget _buildChip(String label, String value) {
    final selected = _filter == value;
    return FilterChip(
      label: Text(
        label,
        style: TextStyle(
          fontSize: 12,
          fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
        ),
      ),
      selected: selected,
      onSelected: (_) => setState(() => _filter = value),
      selectedColor: ZColors.brandAccent.withValues(alpha: 0.15),
      checkmarkColor: ZColors.brandAccent,
      side: BorderSide(
        color: selected ? ZColors.brandAccent.withValues(alpha: 0.4) : ZColors.neutral300,
      ),
    );
  }

  Widget _buildTable(List<AccessRequestModel> items) {
    return ZDataTable<AccessRequestModel>(
      columns: const [
        ZColumn(id: 'name', label: 'Solicitante'),
        ZColumn(id: 'email', label: 'Correo'),
        ZColumn(id: 'company', label: 'Empresa', width: 160),
        ZColumn(id: 'phone', label: 'Teléfono', width: 130),
        ZColumn(id: 'status', label: 'Estado', width: 120),
        ZColumn(id: 'created', label: 'Recibida', width: 140),
        ZColumn(id: 'code', label: 'Invitación', width: 120),
        ZColumn(id: 'actions', label: '', width: 110),
      ],
      rows: items,
      rowMapper: (item) => DataRow(cells: [
        DataCell(
          Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(item.fullName),
              if ((item.message ?? '').isNotEmpty)
                Text(
                  item.message!,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 11, color: ZColors.neutral500),
                ),
            ],
          ),
        ),
        DataCell(Text(item.email)),
        DataCell(Text(item.company.isEmpty ? '—' : item.company)),
        DataCell(Text(item.phone ?? '—')),
        DataCell(_buildStatusBadge(item)),
        DataCell(Text(_formatDate(item.createdAt))),
        DataCell(
          item.invitationCode == null
              ? const Text('—')
              : Text(
                  item.invitationCode!,
                  style: const TextStyle(
                    fontFamily: 'monospace',
                    fontWeight: FontWeight.w600,
                  ),
                ),
        ),
        DataCell(
          item.isPending
              ? Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    IconButton(
                      icon: const Icon(Icons.check_circle_outline_rounded,
                          size: 18, color: ZColors.success),
                      tooltip: 'Aprobar',
                      onPressed: () => _openApproveDialog(item),
                    ),
                    IconButton(
                      icon: const Icon(Icons.cancel_outlined,
                          size: 18, color: ZColors.danger),
                      tooltip: 'Rechazar',
                      onPressed: () => _reject(item),
                    ),
                  ],
                )
              : const SizedBox.shrink(),
        ),
      ]),
    );
  }

  Widget _buildStatusBadge(AccessRequestModel item) {
    if (item.isApproved) {
      return const ZBadge(text: 'APROBADA', type: ZBadgeType.success);
    }
    if (item.isRejected) {
      return const ZBadge(text: 'RECHAZADA', type: ZBadgeType.danger);
    }
    return const ZBadge(text: 'PENDIENTE', type: ZBadgeType.warning);
  }

  String _formatDate(DateTime? date) {
    if (date == null) return '—';
    final d = date.toLocal();
    return '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';
  }

  Future<void> _openApproveDialog(AccessRequestModel item) async {
    final companies = await ref.read(companyListProvider.future);
    if (!mounted) return;

    if (companies.isEmpty) {
      ZToast.error(context, 'No hay compañías disponibles para asignar.');
      return;
    }

    String selectedCompanyId = (companies.first['id'] as String?) ?? '';
    String selectedRole = 'Employee';

    final approved = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setState) => AlertDialog(
          title: Text('Aprobar solicitud — ${item.fullName}'),
          content: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  'Se generará un código de invitación para ${item.email} en la compañía elegida.',
                  style: const TextStyle(fontSize: 13),
                ),
                const SizedBox(height: 16),
                ZDropdownFormField<String>(
                  label: 'Compañía destino',
                  value: selectedCompanyId.isEmpty ? null : selectedCompanyId,
                  prefixIcon: Icons.business_rounded,
                  items: companies
                      .map((c) => DropdownMenuItem<String>(
                            value: c['id'] as String? ?? '',
                            child: Text(c['name'] as String? ?? '—'),
                          ))
                      .toList(),
                  onChanged: (v) => setState(() => selectedCompanyId = v ?? ''),
                ),
                const SizedBox(height: 12),
                ZDropdownFormField<String>(
                  label: 'Rol inicial',
                  value: selectedRole,
                  prefixIcon: Icons.badge_outlined,
                  items: _roles
                      .map((r) => DropdownMenuItem<String>(value: r, child: Text(r)))
                      .toList(),
                  onChanged: (v) => setState(() => selectedRole = v ?? 'Employee'),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(false),
              child: const Text('Cancelar'),
            ),
            ZButton(
              text: 'APROBAR',
              fullWidth: false,
              onPressed: () => Navigator.of(dialogContext).pop(true),
            ),
          ],
        ),
      ),
    );

    if (approved != true || !mounted) return;
    if (selectedCompanyId.isEmpty) {
      ZToast.error(context, 'Selecciona la compañía destino.');
      return;
    }

    try {
      await ref.read(accessRequestProvider.notifier).approve(
            item.id,
            companyId: selectedCompanyId,
            role: selectedRole,
          );
      if (mounted) {
        ZToast.success(context, 'Solicitud aprobada. Código enviado por correo.');
      }
    } catch (e) {
      if (mounted) ZToast.error(context, 'No se pudo aprobar la solicitud.');
    }
  }

  Future<void> _reject(AccessRequestModel item) async {
    // El backend acepta un motivo de rechazo (`RejectAccessRequest.Reason`) y lo
    // persiste en `AccessRequest.RejectionReason`: se captura aqui para que el
    // revisor deje constancia del por que.
    final reasonCtrl = TextEditingController();

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Rechazar solicitud'),
        content: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 420),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Se rechazará la solicitud de ${item.email}.',
                style: const TextStyle(fontSize: 13),
              ),
              const SizedBox(height: 16),
              ZTextField(
                label: 'Motivo del rechazo (opcional)',
                controller: reasonCtrl,
                maxLines: 2,
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          ZButton(
            text: 'RECHAZAR',
            fullWidth: false,
            type: ZButtonType.danger,
            onPressed: () => Navigator.of(dialogContext).pop(true),
          ),
        ],
      ),
    );

    final reason = reasonCtrl.text.trim();
    reasonCtrl.dispose();

    if (confirmed != true || !mounted) return;

    try {
      await ref
          .read(accessRequestProvider.notifier)
          .reject(item.id, reason: reason.isEmpty ? null : reason);
      if (mounted) ZToast.success(context, 'Solicitud rechazada.');
    } catch (e) {
      if (mounted) ZToast.error(context, 'No se pudo rechazar la solicitud.');
    }
  }
}
