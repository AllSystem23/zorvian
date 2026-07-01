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
        final claimStatus = c['status'] as String? ?? '';
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
    return const Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.attach_money, size: 48, color: ZColors.neutral300),
          SizedBox(height: ZSpacing.md),
          Text('Módulo de costos', style: ZTypography.titleMedium),
          SizedBox(height: ZSpacing.xs),
          Text('Próximamente', style: ZTypography.bodySmall),
        ],
      ),
    );
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
