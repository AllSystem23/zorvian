import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

class WorkshopDetailPage extends ConsumerStatefulWidget {
  final String workshopId;
  const WorkshopDetailPage({super.key, required this.workshopId});
  @override
  ConsumerState<WorkshopDetailPage> createState() => _WorkshopDetailPageState();
}

class _WorkshopDetailPageState extends ConsumerState<WorkshopDetailPage> {
  Map<String, dynamic>? _workshop;
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
      final r = await dio.get('service-workshops/${widget.workshopId}');
      _workshop = r.data as Map<String, dynamic>;
    } catch (e) {
      setState(() => _error = 'Error al cargar taller');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _delete() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Eliminar taller'),
        content: const Text('¿Está seguro de eliminar este taller? Esta acción no se puede deshacer.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          ZButton(text: 'Eliminar', type: ZButtonType.danger, onPressed: () => Navigator.pop(ctx, true)),
        ],
      ),
    );
    if (confirm != true) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('service-workshops/${widget.workshopId}');
      if (mounted) context.pop();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error al eliminar: $e'), backgroundColor: ZColors.danger),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return Scaffold(
        appBar: AppBar(title: const Text('Taller')),
        body: const Center(child: CircularProgressIndicator(color: ZColors.brandPrimary)),
      );
    }

    if (_error != null || _workshop == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Taller')),
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.error_outline, size: 48, color: ZColors.danger),
              const SizedBox(height: ZSpacing.md),
              Text(_error ?? 'Taller no encontrado'),
              const SizedBox(height: ZSpacing.lg),
              ZButton(text: 'Reintentar', icon: Icons.refresh, onPressed: _load),
            ],
          ),
        ),
      );
    }

    final w = _workshop!;
    final isActive = w['isActive'] as bool? ?? true;
    final isWide = MediaQuery.of(context).size.width > 700;

    return Scaffold(
      appBar: AppBar(
        title: Text(w['name'] ?? 'Taller'),
        actions: [
          IconButton(
            icon: const Icon(Icons.edit),
            tooltip: 'Editar',
            onPressed: () => context.push('/workshops/${widget.workshopId}/edit'),
          ),
          IconButton(
            icon: const Icon(Icons.delete),
            tooltip: 'Eliminar',
            onPressed: _delete,
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header card
            ZCard(
              padding: const EdgeInsets.all(ZSpacing.lg),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 28,
                    backgroundColor: isActive ? ZColors.brandPrimary.withAlpha(25) : ZColors.neutral200,
                    child: Icon(Icons.build, size: 28, color: isActive ? ZColors.brandPrimary : ZColors.neutral400),
                  ),
                  const SizedBox(width: ZSpacing.md),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(w['name'] ?? '', style: ZTypography.titleLarge),
                        const SizedBox(height: 2),
                        Text('${w['code'] ?? ''} · ${w['city'] ?? '—'}', style: ZTypography.bodyMedium),
                      ],
                    ),
                  ),
                  ZBadge(
                    text: isActive ? 'Activo' : 'Inactivo',
                    type: isActive ? ZBadgeType.success : ZBadgeType.neutral,
                  ),
                ],
              ),
            ),
            const SizedBox(height: ZSpacing.lg),

            // SLA Metrics
            _SectionTitle(title: 'Métricas de SLA'),
            const SizedBox(height: ZSpacing.md),
            if (isWide)
              Row(children: [
                Expanded(child: _MetricCard(label: 'Tiempo respuesta', value: '${w['avgResponseHours'] ?? 48}h', icon: Icons.timer_outlined, color: ZColors.brandAccent)),
                const SizedBox(width: ZSpacing.md),
                Expanded(child: _MetricCard(label: 'Tiempo reparación', value: '${w['avgRepairHours'] ?? 72}h', icon: Icons.build_outlined, color: ZColors.brandSecondary)),
                const SizedBox(width: ZSpacing.md),
                Expanded(child: _MetricCard(label: 'Rating', value: (w['rating'] as num?)?.toDouble().toStringAsFixed(1) ?? '0.0', icon: Icons.star_outline, color: ZColors.warning)),
                const SizedBox(width: ZSpacing.md),
                Expanded(child: _MetricCard(label: 'Técnicos', value: '${w['technicianCount'] ?? 0}', icon: Icons.people_outline, color: ZColors.brandTeal)),
              ])
            else ...[
              Row(children: [
                Expanded(child: _MetricCard(label: 'Respuesta', value: '${w['avgResponseHours'] ?? 48}h', icon: Icons.timer_outlined, color: ZColors.brandAccent)),
                const SizedBox(width: ZSpacing.md),
                Expanded(child: _MetricCard(label: 'Reparación', value: '${w['avgRepairHours'] ?? 72}h', icon: Icons.build_outlined, color: ZColors.brandSecondary)),
              ]),
              const SizedBox(height: ZSpacing.md),
              Row(children: [
                Expanded(child: _MetricCard(label: 'Rating', value: (w['rating'] as num?)?.toDouble().toStringAsFixed(1) ?? '0.0', icon: Icons.star_outline, color: ZColors.warning)),
                const SizedBox(width: ZSpacing.md),
                Expanded(child: _MetricCard(label: 'Técnicos', value: '${w['technicianCount'] ?? 0}', icon: Icons.people_outline, color: ZColors.brandTeal)),
              ]),
            ],

            const SizedBox(height: ZSpacing.xl),

            // Contact
            _SectionTitle(title: 'Contacto'),
            const SizedBox(height: ZSpacing.md),
            _InfoRow(icon: Icons.person_outline, label: 'Contacto', value: w['contactName'] ?? '—'),
            _InfoRow(icon: Icons.phone_outlined, label: 'Teléfono', value: w['phone'] ?? '—'),
            _InfoRow(icon: Icons.email_outlined, label: 'Email', value: w['email'] ?? '—'),

            const SizedBox(height: ZSpacing.xl),

            // Location
            _SectionTitle(title: 'Ubicación'),
            const SizedBox(height: ZSpacing.md),
            _InfoRow(icon: Icons.location_on_outlined, label: 'Dirección', value: w['address'] ?? '—'),
            _InfoRow(icon: Icons.location_city, label: 'Ciudad', value: w['city'] ?? '—'),
            _InfoRow(icon: Icons.public, label: 'País', value: w['country'] ?? '—'),

            if (w['legalName'] != null && (w['legalName'] as String).isNotEmpty) ...[
              const SizedBox(height: ZSpacing.xl),
              _SectionTitle(title: 'Datos legales'),
              const SizedBox(height: ZSpacing.md),
              _InfoRow(icon: Icons.business, label: 'Razón social', value: w['legalName']),
              if (w['taxId'] != null) _InfoRow(icon: Icons.receipt, label: 'NIT/RIF', value: w['taxId']),
            ],

            if (w['notes'] != null && (w['notes'] as String).isNotEmpty) ...[
              const SizedBox(height: ZSpacing.xl),
              _SectionTitle(title: 'Notas'),
              const SizedBox(height: ZSpacing.md),
              Text(w['notes'], style: ZTypography.bodyMedium),
            ],
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
  final IconData icon;
  final String label;
  final String value;
  const _InfoRow({required this.icon, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: ZSpacing.sm),
      child: Row(
        children: [
          Icon(icon, size: 18, color: ZColors.neutral500),
          const SizedBox(width: ZSpacing.sm),
          SizedBox(width: 100, child: Text(label, style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500))),
          Expanded(child: Text(value, style: ZTypography.bodyMedium)),
        ],
      ),
    );
  }
}

class _MetricCard extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color color;
  const _MetricCard({required this.label, required this.value, required this.icon, required this.color});

  @override
  Widget build(BuildContext context) {
    return ZCard(
      padding: const EdgeInsets.all(ZSpacing.md),
      child: Column(
        children: [
          Icon(icon, color: color, size: 24),
          const SizedBox(height: ZSpacing.xs),
          Text(value, style: ZTypography.titleLarge.copyWith(color: color)),
          const SizedBox(height: 2),
          Text(label, style: ZTypography.bodySmall, textAlign: TextAlign.center),
        ],
      ),
    );
  }
}
