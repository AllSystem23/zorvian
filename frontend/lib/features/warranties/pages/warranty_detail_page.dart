import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

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
        _timeline = tr.data as List? ?? [];
      } catch (_) {}

      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar garantía');
    } finally {
      setState(() => _loading = false);
    }
  }

  Color _statusColor(String status) => switch (status) {
    'Registered' => ZColors.brandPrimary,
    'PendingReview' => ZColors.warning,
    'InDiagnosis' => ZColors.brandSecondary,
    'SentToWorkshop' => ZColors.brandSecondary,
    'InRepair' => ZColors.brandAccent,
    'Repaired' => ZColors.success,
    'ReplacementApproved' => ZColors.success,
    'ReadyForDelivery' => ZColors.brandAccent,
    'Delivered' => ZColors.success,
    'Closed' => ZColors.neutral900,
    'Rejected' => ZColors.danger,
    'Cancelled' => ZColors.danger,
    _ => ZColors.brandSecondary,
  };

  String _statusLabel(String status) => switch (status) {
    'Registered' => 'Registrada',
    'PendingReview' => 'Revisión',
    'InDiagnosis' => 'Diagnóstico',
    'SentToWorkshop' => 'En taller',
    'InRepair' => 'Reparando',
    'PendingParts' => 'Repuestos',
    'Repaired' => 'Reparada',
    'ReplacementApproved' => 'Reemplazo',
    'ReadyForDelivery' => 'Lista para entregar',
    'Delivered' => 'Entregada',
    'Closed' => 'Cerrada',
    'Rejected' => 'Rechazada',
    'Cancelled' => 'Cancelada',
    _ => status,
  };

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
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    if (_error != null || _warranty == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Garantía')),
        body: Center(child: Text(_error ?? 'No encontrada')),
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
            onSelected: (v) => _onMenuAction(v),
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
      body: TabBarView(
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

  Widget _buildInfoTab(Map<String, dynamic> w, String status, bool isExpired) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(ZSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Status Banner ──
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(ZSpacing.lg),
            decoration: BoxDecoration(
              color: _statusColor(status).withAlpha(20),
              borderRadius: BorderRadius.circular(ZRadii.lg),
              border: Border.all(color: _statusColor(status).withAlpha(50)),
            ),
            child: Row(children: [
              Icon(Icons.shield, color: _statusColor(status), size: 28),
              const SizedBox(width: ZSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(_statusLabel(status), style: ZTypography.titleLarge.copyWith(color: _statusColor(status))),
                    if (isExpired)
                      const Text('Garantía vencida', style: TextStyle(color: ZColors.danger, fontSize: 12, fontWeight: FontWeight.bold)),
                  ],
                ),
              ),
            ]),
          ),
          const SizedBox(height: ZSpacing.xl),

          // ── Producto ──
          _SectionTitle(title: 'Producto'),
          const SizedBox(height: ZSpacing.sm),
          _InfoRow(label: 'Nombre', value: w['productName'] ?? '-'),
          _InfoRow(label: 'Código', value: w['productId']?.toString().substring(0, 8) ?? '-'),
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

  Widget _buildClaimsTab(Map<String, dynamic> w) {
    final claims = w['claims'] as List? ?? [];
    if (claims.isEmpty) {
      return ZEmptyState.list(
        itemType: 'reclamos',
        actionLabel: 'Crear Reclamo',
        onAction: () => _showCreateClaimDialog(),
      );
    }
    return ListView.separated(
      padding: const EdgeInsets.all(ZSpacing.lg),
      itemCount: claims.length,
      separatorBuilder: (_, _) => const Divider(),
      itemBuilder: (_, i) {
        final c = claims[i] as Map<String, dynamic>;
        final claimStatus = c['status'] as String? ?? '';
        return ZCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(children: [
                Icon(Icons.report_problem, size: 18, color: _statusColor(claimStatus)),
                const SizedBox(width: ZSpacing.sm),
                Expanded(child: Text(c['description'] ?? '-', style: ZTypography.titleMedium)),
                Chip(
                  label: Text(_statusLabel(claimStatus), style: const TextStyle(fontSize: 10)),
                  backgroundColor: _statusColor(claimStatus).withAlpha(30),
                  padding: EdgeInsets.zero,
                ),
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
                  color: DateTime.parse(c['slaDeadline']).isBefore(DateTime.now())
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
      return const Center(child: Text('Sin eventos registrados', style: ZTypography.bodyMedium));
    }
    return ListView.builder(
      padding: const EdgeInsets.all(ZSpacing.lg),
      itemCount: _timeline.length,
      itemBuilder: (_, i) {
        final event = _timeline[i] as Map<String, dynamic>;
        return ZTimeline(
          items: [
            TimelineItem(
              title: event['title'] ?? event['action'] ?? 'Evento',
              subtitle: event['description'] ?? event['details'] ?? '',
              date: event['timestamp'] ?? event['createdAt'] ?? '',
              icon: Icons.circle,
              iconColor: _statusColor(event['status'] ?? ''),
            ),
          ],
        );
      },
    );
  }

  Widget _buildCostsTab() {
    return const Center(child: Text('Módulo de costos en desarrollo', style: ZTypography.bodyMedium));
  }

  void _onMenuAction(String action) async {
    final dio = ref.read(dioClientProvider);
    switch (action) {
      case 'claim':
        _showCreateClaimDialog();
        break;
      case 'complete':
        await dio.patch('warranties/${widget.warrantyId}/status', data: {'status': 'Repaired'});
        _load();
        break;
      case 'deliver':
        await dio.patch('warranties/${widget.warrantyId}/status', data: {'status': 'Delivered'});
        _load();
        break;
      case 'close':
        await dio.patch('warranties/${widget.warrantyId}/status', data: {'status': 'Closed'});
        _load();
        break;
      case 'cancel':
        final confirm = await ZConfirmDialog.show(
          context,
          title: 'Cancelar garantía',
          message: '¿Estás seguro de cancelar esta garantía?',
          confirmLabel: 'Cancelar garantía',
          isDestructive: true,
        );
        if (confirm == true) {
          await dio.patch('warranties/${widget.warrantyId}/status', data: {'status': 'Cancelled'});
          _load();
        }
        break;
    }
  }

  void _showCreateClaimDialog() {
    final descCtrl = TextEditingController();
    String failureType = 'defect';
    String priority = 'medium';

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
            TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancelar')),
            ZButton(
              text: 'Crear reclamo',
              onPressed: () async {
                if (descCtrl.text.trim().isEmpty) return;
                final dio = ref.read(dioClientProvider);
                await dio.post('warranties/${widget.warrantyId}/claims', data: {
                  'description': descCtrl.text.trim(),
                  'failureType': failureType,
                  'priority': priority,
                });
                if (ctx.mounted) Navigator.pop(ctx);
                _load();
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
            width: 120,
            child: Text(label, style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500)),
          ),
          Expanded(child: Text(value, style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w500))),
        ],
      ),
    );
  }
}
