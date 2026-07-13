import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../utils/warranty_utils.dart';

final class WarrantyDetailPage extends ConsumerStatefulWidget {
  final String warrantyId;
  const WarrantyDetailPage({super.key, required this.warrantyId});
  @override
  ConsumerState<WarrantyDetailPage> createState() => _WarrantyDetailPageState();
}

class _WarrantyDetailPageState extends ConsumerState<WarrantyDetailPage>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  Map<String, dynamic>? _warranty;
  List<dynamic> _timeline = [];
  bool _loading = true;
  bool _actionLoading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 4, vsync: this);
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('warranties/${widget.warrantyId}');
      _warranty = r.data as Map<String, dynamic>;

      try {
        final tr = await dio.get('warranties/${widget.warrantyId}/timeline');
        if (tr.data is List) _timeline = tr.data as List;
      } catch (_) {}
    } catch (e) {
      setState(() => _error = 'Error al cargar garantía');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _updateStatus(String status) async {
    setState(() => _actionLoading = true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.patch('warranties/${widget.warrantyId}/status', data: {'status': status});
      _load();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: ${e.toString()}'), backgroundColor: ZColors.danger),
        );
      }
    } finally {
      if (mounted) setState(() => _actionLoading = false);
    }
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return Scaffold(
        appBar: AppBar(title: const Text('Garantía')),
        body: _buildDetailSkeleton(),
      );
    }

    if (_error != null || _warranty == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Garantía')),
        body: ZErrorDisplay(
          message: _error ?? 'Garantía no encontrada',
          onRetry: _load,
        ),
      );
    }

    final w = _warranty!;
    final status = w['status'] as String? ?? '';
    final endDate = w['endDate'] as String?;
    final isExpired = endDate != null && DateTime.parse(endDate).isBefore(DateTime.now());

    return Scaffold(
      appBar: AppBar(
        title: Text(w['warrantyNumber'] ?? 'Garantía'),
        actions: [
          IconButton(
            icon: const Icon(Icons.edit),
            tooltip: 'Editar',
            onPressed: () => context.push('/warranties/${widget.warrantyId}/edit'),
          ),
          PopupMenuButton<String>(
            onSelected: _onMenuAction,
            itemBuilder: (_) => [
              if (status == 'Registered' || status == 'PendingReview')
                const PopupMenuItem(value: 'claim', child: Text('Crear reclamo')),
              if (status == 'SentToWorkshop' || status == 'InRepair')
                const PopupMenuItem(value: 'complete', child: Text('Marcar reparada')),
              if (status == 'Repaired')
                const PopupMenuItem(value: 'deliver', child: Text('Marcar entregada')),
              if (status == 'Delivered')
                const PopupMenuItem(value: 'close', child: Text('Cerrar garantía')),
              if (status != 'Closed' && status != 'Cancelled')
                const PopupMenuItem(value: 'cancel', child: Text('Cancelar')),
            ],
          ),
        ],
        bottom: TabBar(
          controller: _tabController,
          tabs: const [
            Tab(text: 'Info'),
            Tab(text: 'Reclamos'),
            Tab(text: 'Timeline'),
            Tab(text: 'Costos'),
          ],
        ),
      ),
      body: _actionLoading
          ? const Center(child: CircularProgressIndicator())
          : TabBarView(
              controller: _tabController,
              children: [
                _buildInfoTab(w, status, isExpired),
                _buildClaimsTab(w),
                _buildTimelineTab(),
                _buildCostsTab(),
              ],
            ),
    );
  }

  Widget _buildDetailSkeleton() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        children: [
          ZSkeleton.card(height: 80),
          const SizedBox(height: ZSpacing.lg),
          ZSkeleton.header(),
          const SizedBox(height: ZSpacing.md),
          ZSkeleton.listTile(),
          const SizedBox(height: ZSpacing.sm),
          ZSkeleton.listTile(),
          const SizedBox(height: ZSpacing.sm),
          ZSkeleton.listTile(),
        ],
      ),
    );
  }

  Widget _buildInfoTab(Map<String, dynamic> w, String status, bool isExpired) {
    final step = warrantyWorkflowStep(status);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Workflow Stepper ──
          ZStepper(
            currentStep: step,
            steps: warrantyWorkflowSteps,
            orientation: ZStepperOrientation.horizontal,
          ),
          const SizedBox(height: ZSpacing.xl),

          // ── Status Banner ──
          if (isExpired)
            Padding(
              padding: const EdgeInsets.only(bottom: ZSpacing.md),
              child: ZAlertCard(message: 'Esta garantía ha vencido', severity: 'high'),
            ),

          // ── SLA Progress ──
          if (w['slaDeadline'] != null) ...[
            _buildSlaProgress(w),
            const SizedBox(height: ZSpacing.lg),
          ],

          // ── Producto ──
          _SectionTitle(title: 'Producto'),
          const SizedBox(height: ZSpacing.sm),
          _InfoRow(label: 'Nombre', value: w['productName'] ?? '-'),
          _InfoRow(label: 'Código', value: (w['productId'] ?? '-').toString().length > 8
              ? (w['productId'] as String).substring(0, 8)
              : (w['productId'] ?? '-').toString()),
          if (w['brandName'] != null) _InfoRow(label: 'Marca', value: w['brandName']),
          if (w['categoryName'] != null) _InfoRow(label: 'Categoría', value: w['categoryName']),
          if (w['serialNumber'] != null && w['serialNumber'].toString().isNotEmpty)
            _InfoRow(label: 'Serie', value: w['serialNumber']),
          if (w['imei'] != null && w['imei'].toString().isNotEmpty)
            _InfoRow(label: 'IMEI', value: w['imei']),
          if (w['lotNumber'] != null && w['lotNumber'].toString().isNotEmpty)
            _InfoRow(label: 'Lote', value: w['lotNumber']),

          const SizedBox(height: ZSpacing.xl),

          // ── Cliente ──
          _SectionTitle(title: 'Cliente'),
          const SizedBox(height: ZSpacing.sm),
          _InfoRow(label: 'Nombre', value: w['clientName'] ?? '-'),

          const SizedBox(height: ZSpacing.xl),

          // ── Período ──
          _SectionTitle(title: 'Período de garantía'),
          const SizedBox(height: ZSpacing.sm),
          _InfoRow(label: 'Inicio', value: w['startDate'] ?? '-'),
          _InfoRow(label: 'Fin', value: w['endDate'] ?? '-'),
          _InfoRow(label: 'Duración', value: '${w['durationMonths'] ?? '-'} meses'),

          if (w['terms'] != null && w['terms'].toString().isNotEmpty) ...[
            const SizedBox(height: ZSpacing.xl),
            _SectionTitle(title: 'Términos'),
            const SizedBox(height: ZSpacing.sm),
            Text(w['terms'], style: ZTypography.bodyMedium),
          ],
        ],
      ),
    );
  }

  Widget _buildSlaProgress(Map<String, dynamic> w) {
    final slaDeadline = DateTime.tryParse(w['slaDeadline'] ?? '');
    if (slaDeadline == null) return const SizedBox.shrink();

    final now = DateTime.now();
    final total = slaDeadline.difference(now.add(const Duration(days: 30))).inHours;
    final remaining = slaDeadline.difference(now).inHours;
    final progress = total > 0 ? (total - remaining) / total : 1.0;
    final isBreached = now.isAfter(slaDeadline);

    return ZCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(children: [
            Icon(Icons.timer, size: 18, color: isBreached ? ZColors.danger : ZColors.brandAccent),
            const SizedBox(width: ZSpacing.sm),
            Text('SLA', style: ZTypography.titleSmall),
            const Spacer(),
            ZBadge(
              text: isBreached ? 'Vencido' : '${remaining}h restantes',
              type: isBreached ? ZBadgeType.danger : ZBadgeType.success,
            ),
          ]),
          const SizedBox(height: ZSpacing.sm),
          ZProgress(
            label: 'Tiempo de respuesta',
            value: progress.clamp(0.0, 1.0),
            showValue: false,
          ),
        ],
      ),
    );
  }

  Widget _buildClaimsTab(Map<String, dynamic> w) {
    final claims = w['claims'] as List? ?? [];
    if (claims.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.report_problem_outlined, size: 48, color: ZColors.neutral300),
            const SizedBox(height: ZSpacing.md),
            Text('Sin reclamos', style: ZTypography.titleMedium.copyWith(color: ZColors.neutral500)),
            const SizedBox(height: ZSpacing.lg),
            ZButton(
              text: 'Crear reclamo',
              onPressed: () => _showCreateClaimDialog(),
            ),
          ],
        ),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.all(ZSpacing.lg),
      itemCount: claims.length,
      separatorBuilder: (_, _) => const SizedBox(height: ZSpacing.sm),
      itemBuilder: (_, i) {
        final c = claims[i] as Map<String, dynamic>;
        final claimId = c['id'] as String? ?? '';
        final claimStatus = c['status'] as String? ?? '';
        final canAssignWorkshop = claimStatus == 'PendingReview' || claimStatus == 'InDiagnosis';
        return ZCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(children: [
                Icon(Icons.report_problem, size: 18, color: warrantyStatusColor(claimStatus)),
                const SizedBox(width: ZSpacing.sm),
                Expanded(child: Text(c['description'] ?? '-', style: ZTypography.titleMedium)),
                ZBadge(text: warrantyStatusLabel(claimStatus), type: warrantyBadgeType(claimStatus)),
              ]),
              if (c['failureType'] != null) ...[
                const SizedBox(height: ZSpacing.xs),
                Text('Tipo: ${c['failureType']}', style: ZTypography.bodySmall),
              ],
              if (c['workshopName'] != null) ...[
                const SizedBox(height: ZSpacing.xs),
                Text('Taller: ${c['workshopName']}', style: ZTypography.bodySmall),
              ],
              if (c['slaDeadline'] != null) ...[
                const SizedBox(height: ZSpacing.xs),
                Text('SLA: ${c['slaDeadline']}', style: ZTypography.bodySmall.copyWith(
                  color: DateTime.tryParse(c['slaDeadline'])?.isBefore(DateTime.now()) == true
                      ? ZColors.danger : ZColors.neutral600,
                )),
              ],
              if (canAssignWorkshop) ...[
                const SizedBox(height: ZSpacing.sm),
                const SizedBox(height: ZSpacing.sm),
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    if (canAssignWorkshop)
                      ZButton(
                        text: 'Asignar taller',
                        icon: Icons.build,
                        type: ZButtonType.secondary,
                        onPressed: () => _showAssignWorkshopDialog(claimId),
                      ),
                    if (claimStatus != 'ReplacementApproved' && claimStatus != 'Repaired' && claimStatus != 'Closed' && claimStatus != 'Cancelled') ...[
                      const SizedBox(width: ZSpacing.sm),
                      ZButton(
                        text: 'Reemplazar',
                        icon: Icons.swap_horiz,
                        type: ZButtonType.primary,
                        onPressed: () => _showProcessReplacementDialog(claimId),
                      ),
                    ],
                  ],
                ),
              ],
            ],
          ),
        );
      },
    );
  }

  Widget _buildTimelineTab() {
    if (_timeline.isEmpty) {
      return const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.timeline, size: 48, color: ZColors.neutral300),
            SizedBox(height: ZSpacing.md),
            Text('Sin eventos registrados', style: ZTypography.bodyMedium),
          ],
        ),
      );
    }

    // Construir items del timeline correctamente
    final items = _timeline.map((e) {
      final event = e as Map<String, dynamic>;
      return TimelineItem(
        title: event['title'] ?? event['action'] ?? 'Evento',
        subtitle: event['description'] ?? event['details'] ?? '',
        date: event['timestamp'] ?? event['createdAt'] ?? '',
        icon: Icons.circle,
        iconColor: warrantyStatusColor(event['status'] ?? ''),
      );
    }).toList();

    // Marcar el último item
    if (items.isNotEmpty) {
      items[items.length - 1] = TimelineItem(
        title: items[items.length - 1].title,
        subtitle: items[items.length - 1].subtitle,
        date: items[items.length - 1].date,
        icon: items[items.length - 1].icon,
        iconColor: items[items.length - 1].iconColor,
        isLast: true,
      );
    }

    return ZTimeline(items: items);
  }

  Widget _buildCostsTab() {
    final warrantyId = widget.warrantyId;
    return _CostsTabBody(warrantyId: warrantyId);
  }

  void _onMenuAction(String action) async {
    switch (action) {
      case 'claim':
        _showCreateClaimDialog();
        break;
      case 'complete':
        final confirm = await ZConfirmDialog.show(
          context,
          title: 'Marcar como reparada',
          message: '¿La garantía ha sido reparada exitosamente?',
          confirmLabel: 'Confirmar',
        );
        if (confirm == true) _updateStatus('Repaired');
        break;
      case 'deliver':
        final confirm = await ZConfirmDialog.show(
          context,
          title: 'Marcar como entregada',
          message: '¿El producto ha sido entregado al cliente?',
          confirmLabel: 'Confirmar',
        );
        if (confirm == true) _updateStatus('Delivered');
        break;
      case 'close':
        final confirm = await ZConfirmDialog.show(
          context,
          title: 'Cerrar garantía',
          message: '¿Desea cerrar esta garantía? Esta acción no se puede deshacer.',
          confirmLabel: 'Cerrar',
          isDestructive: true,
        );
        if (confirm == true) _updateStatus('Closed');
        break;
      case 'cancel':
        final confirm = await ZConfirmDialog.show(
          context,
          title: 'Cancelar garantía',
          message: '¿Está seguro de cancelar esta garantía? Esta acción no se puede deshacer.',
          confirmLabel: 'Cancelar garantía',
          isDestructive: true,
        );
        if (confirm == true) _updateStatus('Cancelled');
        break;
    }
  }

  void _showCreateClaimDialog() {
    final descCtrl = TextEditingController();
    String failureType = 'defect';
    String priority = 'medium';
    bool saving = false;

    showDialog(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: const Text('Nuevo reclamo'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                ZTextField(controller: descCtrl, label: 'Descripción del problema'),
                const SizedBox(height: ZSpacing.md),
                DropdownButtonFormField<String>(
                  initialValue: failureType,
                  decoration: const InputDecoration(labelText: 'Tipo de falla', border: OutlineInputBorder()),
                  items: const [
                    DropdownMenuItem(value: 'defect', child: Text('Defecto de fabricación')),
                    DropdownMenuItem(value: 'damage', child: Text('Daño físico')),
                    DropdownMenuItem(value: 'malfunction', child: Text('Mal funcionamiento')),
                    DropdownMenuItem(value: 'other', child: Text('Otro')),
                  ],
                  onChanged: (v) => setDialogState(() => failureType = v ?? 'defect'),
                ),
                const SizedBox(height: ZSpacing.md),
                DropdownButtonFormField<String>(
                  initialValue: priority,
                  decoration: const InputDecoration(labelText: 'Prioridad', border: OutlineInputBorder()),
                  items: const [
                    DropdownMenuItem(value: 'low', child: Text('Baja')),
                    DropdownMenuItem(value: 'medium', child: Text('Media')),
                    DropdownMenuItem(value: 'high', child: Text('Alta')),
                    DropdownMenuItem(value: 'critical', child: Text('Crítica')),
                  ],
                  onChanged: (v) => setDialogState(() => priority = v ?? 'medium'),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: saving ? null : () => Navigator.pop(ctx),
              child: const Text('Cancelar'),
            ),
            ZButton(
              text: 'Crear reclamo',
              isLoading: saving,
              onPressed: saving ? () {} : () async {
                if (descCtrl.text.trim().isEmpty) return;
                setDialogState(() => saving = true);
                try {
                  final dio = ref.read(dioClientProvider);
                  await dio.post('warranties/${widget.warrantyId}/claims', data: {
                    'description': descCtrl.text.trim(),
                    'failureType': failureType,
                    'priority': priority,
                  });
                  if (ctx.mounted) Navigator.pop(ctx);
                  _load();
                } catch (e) {
                  setDialogState(() => saving = false);
                  if (ctx.mounted) {
                    ScaffoldMessenger.of(ctx).showSnackBar(
                      SnackBar(content: Text('Error al crear reclamo: $e'), backgroundColor: ZColors.danger),
                    );
                  }
                }
              },
            ),
          ],
        ),
      ),
    );
  }

  void _showAssignWorkshopDialog(String claimId) async {
    List<dynamic> workshops = [];
    bool loading = true;
    String? selectedWorkshopId;
    String? error;

    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('service-workshops');
      workshops = r.data as List;
    } catch (e) {
      error = 'Error al cargar talleres';
    }
    loading = false;

    if (!mounted) return;

    showDialog(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) {
          if (loading) {
            return const AlertDialog(
              content: SizedBox(height: 100, child: Center(child: CircularProgressIndicator())),
            );
          }
          if (error != null) {
            return AlertDialog(
              title: const Text('Error'),
              content: Text(error),
              actions: [TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cerrar'))],
            );
          }
          if (workshops.isEmpty) {
            return AlertDialog(
              title: const Text('Sin talleres'),
              content: const Text('No hay talleres registrados. Cree uno primero.'),
              actions: [
                TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cerrar')),
                ZButton(
                  text: 'Crear taller',
                  type: ZButtonType.primary,
                  onPressed: () {
                    Navigator.pop(ctx);
                    context.push('/workshops/new');
                  },
                ),
              ],
            );
          }
          return AlertDialog(
            title: const Text('Asignar taller'),
            content: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text('Seleccione el taller para este reclamo:', style: ZTypography.bodyMedium),
                  const SizedBox(height: ZSpacing.md),
                  ...workshops.map((w) {
                    final wid = w['id'] as String;
                    final name = w['name'] as String? ?? '';
                    final city = w['city'] as String? ?? '';
                    final avgRepair = w['avgRepairHours'] ?? 72;
                    final isSelected = selectedWorkshopId == wid;
                    return Card(
                      color: isSelected ? ZColors.brandPrimary.withAlpha(15) : null,
                      child: ListTile(
                        leading: CircleAvatar(
                          backgroundColor: isSelected ? ZColors.brandPrimary : ZColors.neutral200,
                          child: Icon(Icons.build, color: isSelected ? Colors.white : ZColors.neutral500),
                        ),
                        title: Text(name),
                        subtitle: Text('$city · ~${avgRepair}h reparación'),
                        trailing: isSelected
                            ? const Icon(Icons.check_circle, color: ZColors.brandPrimary)
                            : null,
                        onTap: () => setDialogState(() => selectedWorkshopId = wid),
                      ),
                    );
                  }),
                ],
              ),
            ),
            actions: [
              TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancelar')),
              ZButton(
                text: 'Asignar',
                type: ZButtonType.primary,
                onPressed: selectedWorkshopId == null
                    ? () {}
                    : () async {
                        Navigator.pop(ctx);
                        setState(() => _actionLoading = true);
                        try {
                          final dio = ref.read(dioClientProvider);
                          await dio.post('warranties/claims/$claimId/assign-workshop', data: {
                            'workshopId': selectedWorkshopId,
                          });
                          _load();
                        } catch (e) {
                          if (mounted) {
                            ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(content: Text('Error: $e'), backgroundColor: ZColors.danger),
                            );
                          }
                        } finally {
                          if (mounted) setState(() => _actionLoading = false);
                        }
                      },
              ),
            ],
          );
        },
      ),
    );
  }

  void _showProcessReplacementDialog(String claimId) {
    final newProductIdCtrl = TextEditingController();
    final newSerialCtrl = TextEditingController();
    final authCodeCtrl = TextEditingController();
    final notesCtrl = TextEditingController();
    String strategy = 'intermediary';
    bool saving = false;

    showDialog(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: const Text('Procesar Reemplazo'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Selecciona la estrategia de inventario:', style: ZTypography.bodyMedium),
                const SizedBox(height: ZSpacing.sm),
                Container(
                  padding: const EdgeInsets.all(ZSpacing.sm),
                  decoration: BoxDecoration(border: Border.all(color: ZColors.neutral200), borderRadius: BorderRadius.circular(8)),
                  child: Column(
                    children: [
                      InkWell(
                        onTap: () => setDialogState(() => strategy = 'intermediary'),
                        child: Padding(
                          padding: const EdgeInsets.symmetric(vertical: 4),
                          child: Row(children: [
                            Radio<String>(
                              value: 'intermediary',
                              // ignore: deprecated_member_use
                              groupValue: strategy,
                              // ignore: deprecated_member_use
                              onChanged: (v) => setDialogState(() => strategy = v!),
                              activeColor: ZColors.brandPrimary,
                            ),
                            const Expanded(child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('Como intermediario'),
                                Text('No afecta tu stock contable', style: TextStyle(fontSize: 12, color: Colors.grey)),
                              ],
                            )),
                          ]),
                        ),
                      ),
                      InkWell(
                        onTap: () => setDialogState(() => strategy = 'store_stock'),
                        child: Padding(
                          padding: const EdgeInsets.symmetric(vertical: 4),
                          child: Row(children: [
                            Radio<String>(
                              value: 'store_stock',
                              // ignore: deprecated_member_use
                              groupValue: strategy,
                              // ignore: deprecated_member_use
                              onChanged: (v) => setDialogState(() => strategy = v!),
                              activeColor: ZColors.brandPrimary,
                            ),
                            const Expanded(child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('Stock propio'),
                                Text('Genera salidas/entradas en tu inventario', style: TextStyle(fontSize: 12, color: Colors.grey)),
                              ],
                            )),
                          ]),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: ZSpacing.md),
                ZTextField(controller: authCodeCtrl, label: 'Cód. Autorización RMA / Marca'),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: newProductIdCtrl, label: 'ID Producto Nuevo (UUID)'),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: newSerialCtrl, label: 'Nuevo N° Serie'),
                const SizedBox(height: ZSpacing.sm),
                ZTextField(controller: notesCtrl, label: 'Notas (Opcional)', maxLines: 2),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: saving ? null : () => Navigator.pop(ctx),
              child: const Text('Cancelar'),
            ),
            ZButton(
              text: 'Aprobar Reemplazo',
              isLoading: saving,
              type: ZButtonType.primary,
              onPressed: saving ? () {} : () async {
                if (newProductIdCtrl.text.trim().isEmpty || authCodeCtrl.text.trim().isEmpty) return;
                setDialogState(() => saving = true);
                try {
                  final dio = ref.read(dioClientProvider);
                  await dio.post('warranties/claims/$claimId/process-replacement', data: {
                    'providerAuthorizationCode': authCodeCtrl.text.trim(),
                    'newProductId': newProductIdCtrl.text.trim(),
                    'newSerialNumber': newSerialCtrl.text.trim(),
                    'strategy': strategy,
                    'notes': notesCtrl.text.trim().isNotEmpty ? notesCtrl.text.trim() : null,
                  });
                  if (ctx.mounted) Navigator.pop(ctx);
                  _load();
                } catch (e) {
                  setDialogState(() => saving = false);
                  if (ctx.mounted) {
                    ScaffoldMessenger.of(ctx).showSnackBar(
                      SnackBar(content: Text('Error: $e'), backgroundColor: ZColors.danger),
                    );
                  }
                }
              },
            ),
          ],
        ),
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  final String title;
  const _SectionTitle({required this.title});

  @override
  Widget build(BuildContext context) {
    return Text(title, style: ZTypography.titleMedium.copyWith(color: ZColors.brandPrimary));
  }
}

class _InfoRow extends StatelessWidget {
  final String label;
  final String value;
  const _InfoRow({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: ZSpacing.xs),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 100,
            child: Text(label, style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500)),
          ),
          Expanded(child: Text(value, style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w500))),
        ],
      ),
    );
  }
}

class _CostsTabBody extends ConsumerStatefulWidget {
  final String warrantyId;
  const _CostsTabBody({required this.warrantyId});

  @override
  ConsumerState<_CostsTabBody> createState() => _CostsTabBodyState();
}

class _CostsTabBodyState extends ConsumerState<_CostsTabBody> {
  List<dynamic> _costs = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('warranty-costs/by-warranty/${widget.warrantyId}');
      _costs = r.data as List;
    } catch (e) {
      setState(() => _error = 'Error al cargar costos');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Center(child: CircularProgressIndicator(color: ZColors.brandPrimary));
    }

    if (_error != null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.error_outline, size: 48, color: ZColors.danger),
            const SizedBox(height: ZSpacing.md),
            Text(_error!, style: ZTypography.bodyMedium),
            const SizedBox(height: ZSpacing.lg),
            ZButton(text: 'Reintentar', icon: Icons.refresh, onPressed: _load),
          ],
        ),
      );
    }

    if (_costs.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.attach_money_outlined, size: 48, color: ZColors.neutral300),
            const SizedBox(height: ZSpacing.md),
            Text('Sin costos registrados', style: ZTypography.titleMedium.copyWith(color: ZColors.neutral500)),
            const SizedBox(height: ZSpacing.xs),
            Text('Los costos de reparación y repuestos aparecerán aquí.', style: ZTypography.bodySmall),
            const SizedBox(height: ZSpacing.lg),
            ZButton(
              text: 'Agregar costo',
              icon: Icons.add,
              type: ZButtonType.primary,
              onPressed: () => _showAddCostDialog(),
            ),
          ],
        ),
      );
    }

    final total = _costs.fold<double>(0, (sum, c) {
      final qty = (c['quantity'] as num?)?.toDouble() ?? 1;
      final unit = (c['unitCost'] as num?)?.toDouble() ?? 0;
      return sum + (qty * unit);
    });

    return Column(
      children: [
        // Summary header
        ZCard(
          margin: const EdgeInsets.all(ZSpacing.md),
          padding: const EdgeInsets.all(ZSpacing.md),
          child: Row(
            children: [
              const Icon(Icons.receipt_long, color: ZColors.brandAccent),
              const SizedBox(width: ZSpacing.sm),
              Text('Total: ', style: ZTypography.titleSmall),
              Text(
                '\$${total.toStringAsFixed(2)}',
                style: ZTypography.titleLarge.copyWith(color: ZColors.brandPrimary, fontWeight: FontWeight.bold),
              ),
              const Spacer(),
              Text('${_costs.length} registros', style: ZTypography.bodySmall),
              const SizedBox(width: ZSpacing.md),
              ZButton(
                text: 'Agregar',
                icon: Icons.add,
                type: ZButtonType.primary,
                onPressed: () => _showAddCostDialog(),
              ),
            ],
          ),
        ),
        // Cost items
        Expanded(
          child: ListView.separated(
            padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md),
            itemCount: _costs.length,
            separatorBuilder: (_, _) => const SizedBox(height: ZSpacing.sm),
            itemBuilder: (_, i) {
              final c = _costs[i] as Map<String, dynamic>;
              final category = c['costCategory'] as String? ?? '';
              final desc = c['description'] as String? ?? '';
              final qty = (c['quantity'] as num?)?.toDouble() ?? 1;
              final unit = (c['unitCost'] as num?)?.toDouble() ?? 0;
              final totalCost = qty * unit;
              final paidBy = c['paidBy'] as String? ?? '';
              final isBilled = c['isBilled'] as bool? ?? false;

              return ZCard(
                padding: const EdgeInsets.all(ZSpacing.md),
                child: Row(
                  children: [
                    Icon(
                      _costCategoryIcon(category),
                      color: _costCategoryColor(category),
                      size: 20,
                    ),
                    const SizedBox(width: ZSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(category, style: ZTypography.titleSmall),
                          if (desc.isNotEmpty)
                            Text(desc, style: ZTypography.bodySmall.copyWith(color: ZColors.neutral600)),
                          Text('Paid by: $paidBy · Qty: ${qty.toStringAsFixed(0)} × \$${unit.toStringAsFixed(2)}',
                            style: ZTypography.bodySmall),
                        ],
                      ),
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text('\$${totalCost.toStringAsFixed(2)}',
                          style: ZTypography.titleMedium.copyWith(fontWeight: FontWeight.bold)),
                        ZBadge(
                          text: isBilled ? 'Facturado' : 'Pendiente',
                          type: isBilled ? ZBadgeType.success : ZBadgeType.warning,
                        ),
                      ],
                    ),
                  ],
                ),
              );
            },
          ),
        ),
      ],
    );
  }

  void _showAddCostDialog() {
    final descCtrl = TextEditingController();
    final qtyCtrl = TextEditingController(text: '1');
    final unitCtrl = TextEditingController();
    final invoiceCtrl = TextEditingController();
    String category = 'Labor';
    String paidBy = 'Company';
    bool saving = false;

    showDialog(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: const Text('Agregar costo'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                DropdownButtonFormField<String>(
                  initialValue: category,
                  decoration: const InputDecoration(labelText: 'Categoría', border: OutlineInputBorder()),
                  items: const [
                    DropdownMenuItem(value: 'Labor', child: Text('Mano de obra')),
                    DropdownMenuItem(value: 'Parts', child: Text('Repuestos')),
                    DropdownMenuItem(value: 'Logistics', child: Text('Logística')),
                    DropdownMenuItem(value: 'Other', child: Text('Otro')),
                  ],
                  onChanged: (v) => setDialogState(() => category = v ?? 'Labor'),
                ),
                const SizedBox(height: ZSpacing.md),
                ZTextField(controller: descCtrl, label: 'Descripción'),
                const SizedBox(height: ZSpacing.md),
                Row(
                  children: [
                    Expanded(
                      child: ZTextField(controller: qtyCtrl, label: 'Cantidad', keyboardType: TextInputType.number),
                    ),
                    const SizedBox(width: ZSpacing.md),
                    Expanded(
                      child: ZTextField(controller: unitCtrl, label: 'Costo unitario', keyboardType: TextInputType.number),
                    ),
                  ],
                ),
                const SizedBox(height: ZSpacing.md),
                DropdownButtonFormField<String>(
                  initialValue: paidBy,
                  decoration: const InputDecoration(labelText: 'Pagado por', border: OutlineInputBorder()),
                  items: const [
                    DropdownMenuItem(value: 'Company', child: Text('Empresa')),
                    DropdownMenuItem(value: 'Client', child: Text('Cliente')),
                    DropdownMenuItem(value: 'Workshop', child: Text('Taller')),
                    DropdownMenuItem(value: 'Warranty', child: Text('Garantía')),
                  ],
                  onChanged: (v) => setDialogState(() => paidBy = v ?? 'Company'),
                ),
                const SizedBox(height: ZSpacing.md),
                ZTextField(controller: invoiceCtrl, label: 'N° Factura (opcional)'),
              ],
            ),
          ),
          actions: [
            TextButton(onPressed: saving ? null : () => Navigator.pop(ctx), child: const Text('Cancelar')),
            ZButton(
              text: 'Guardar',
              isLoading: saving,
              onPressed: saving ? () {} : () async {
                final qty = double.tryParse(qtyCtrl.text) ?? 1;
                final unit = double.tryParse(unitCtrl.text) ?? 0;
                if (unit <= 0) return;
                setDialogState(() => saving = true);
                try {
                  final dio = ref.read(dioClientProvider);
                  await dio.post('warranty-costs', data: {
                    'warrantyId': widget.warrantyId,
                    'costCategory': category,
                    'description': descCtrl.text.trim().isNotEmpty ? descCtrl.text.trim() : null,
                    'quantity': qty,
                    'unitCost': unit,
                    'paidBy': paidBy,
                    'invoiceNumber': invoiceCtrl.text.trim().isNotEmpty ? invoiceCtrl.text.trim() : null,
                  });
                  if (ctx.mounted) Navigator.pop(ctx);
                  _load();
                } catch (e) {
                  setDialogState(() => saving = false);
                  if (ctx.mounted) {
                    ScaffoldMessenger.of(ctx).showSnackBar(
                      SnackBar(content: Text('Error al guardar: $e'), backgroundColor: ZColors.danger),
                    );
                  }
                }
              },
            ),
          ],
        ),
      ),
    );
  }

  IconData _costCategoryIcon(String category) => switch (category) {
    'Labor' => Icons.build,
    'Parts' => Icons.settings,
    'Logistics' => Icons.local_shipping,
    _ => Icons.attach_money,
  };

  Color _costCategoryColor(String category) => switch (category) {
    'Labor' => ZColors.brandAccent,
    'Parts' => ZColors.brandSecondary,
    'Logistics' => ZColors.warning,
    _ => ZColors.neutral500,
  };
}
