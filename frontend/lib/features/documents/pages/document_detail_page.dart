import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../models/document_models.dart';

class DocumentDetailPage extends ConsumerStatefulWidget {
  final String documentId;
  const DocumentDetailPage({super.key, required this.documentId});

  @override
  ConsumerState<DocumentDetailPage> createState() => _DocumentDetailPageState();
}

class _DocumentDetailPageState extends ConsumerState<DocumentDetailPage> {
  DocumentDetail? _detail;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('documents/${widget.documentId}');
      setState(() {
        _detail = DocumentDetail.fromJson(r.data as Map<String, dynamic>);
        _loading = false;
      });
    } catch (e) {
      setState(() { _error = 'Error al cargar el documento'; _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (_loading) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }
    if (_error != null) {
      return Scaffold(
        body: Center(child: Text(_error!)),
      );
    }

    final d = _detail!;
    final stColor = switch (d.status) {
      'signed' => ZColors.success,
      'pending_signature' => ZColors.warning,
      _ => ZColors.neutral500,
    };

    return Scaffold(
      backgroundColor: theme.brightness == Brightness.dark ? ZColors.darkBackground : ZColors.neutral50,
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text(d.name, style: ZTypography.titleMedium)),
                IconButton(
                  icon: const Icon(Icons.download_outlined),
                  tooltip: 'Descargar PDF',
                  onPressed: () => _downloadPdf(context),
                ),
              ],
            ),
            const SizedBox(height: 16),
            _buildHeader(context, d, stColor),
            const SizedBox(height: 24),
            if (d.summary != null && d.summary!.isNotEmpty)
              _buildSummaryCard(context, d.summary!),
            if (d.summary != null && d.summary!.isNotEmpty)
              const SizedBox(height: 16),
            _buildVersionsCard(context, d.versions),
            const SizedBox(height: 16),
            _buildSignaturesCard(context, d.signatures),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader(BuildContext context, DocumentDetail d, Color stColor) {
    return ZCard(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: stColor.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(Icons.description_outlined, color: stColor),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(d.name, style: ZTypography.titleMedium),
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        ZBadge(text: d.entityType.toUpperCase(), type: ZBadgeType.info),
                        const SizedBox(width: 8),
                        ZBadge(text: d.status.statusLabel, type: _badgeType(d.status)),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          const Divider(height: 1),
          const SizedBox(height: 16),
          _infoRow('Plantilla', d.templateName ?? '—'),
          _infoRow('Creado', '${d.createdAt.day}/${d.createdAt.month}/${d.createdAt.year}'),
          _infoRow('ID', d.id.substring(0, 8).toUpperCase()),
        ],
      ),
    );
  }

  Widget _infoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          SizedBox(
            width: 100,
            child: Text(label, style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
          ),
          Expanded(child: Text(value, style: ZTypography.bodySmall)),
        ],
      ),
    );
  }

  Widget _buildSummaryCard(BuildContext context, String summary) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.auto_awesome, size: 18, color: ZColors.warning),
              const SizedBox(width: 8),
              Text('Resumen IA', style: ZTypography.labelMedium),
            ],
          ),
          const SizedBox(height: 12),
          Text(summary, style: ZTypography.bodySmall.copyWith(height: 1.6)),
        ],
      ),
    );
  }

  Widget _buildVersionsCard(BuildContext context, List<DocumentVersionItem> versions) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.history, size: 18, color: ZColors.brandAccent),
              const SizedBox(width: 8),
              Text('Versiones (${versions.length})', style: ZTypography.labelMedium),
            ],
          ),
          const SizedBox(height: 12),
          if (versions.isEmpty)
            Text('Sin versiones', style: ZTypography.labelSmall)
          else
            ...versions.map((v) => Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Row(
                children: [
                  Container(
                    width: 28,
                    height: 28,
                    decoration: BoxDecoration(
                      color: ZColors.brandAccent.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Center(
                      child: Text('v${v.versionNumber}',
                        style: ZTypography.labelSmall.copyWith(fontWeight: FontWeight.w700, color: ZColors.brandAccent)),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(v.changesSummary ?? 'Sin descripción',
                          style: ZTypography.bodySmall),
                        Text('${v.createdAt.day}/${v.createdAt.month}/${v.createdAt.year}',
                          style: ZTypography.labelSmall),
                      ],
                    ),
                  ),
                ],
              ),
            )),
        ],
      ),
    );
  }

  Widget _buildSignaturesCard(BuildContext context, List<DocumentSignatureItem> signatures) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.draw_outlined, size: 18, color: ZColors.brandAccent),
              const SizedBox(width: 8),
              Text('Firmas (${signatures.length})', style: ZTypography.labelMedium),
            ],
          ),
          const SizedBox(height: 12),
          if (signatures.isEmpty)
            Text('Sin solicitudes de firma', style: ZTypography.labelSmall)
          else
            ...signatures.map((s) => Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Row(
                children: [
                  Icon(
                    s.status == 'signed' ? Icons.verified : Icons.schedule,
                    size: 20,
                    color: s.status == 'signed' ? ZColors.success : ZColors.warning,
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(s.signerRole, style: ZTypography.bodySmall.copyWith(fontWeight: FontWeight.w600)),
                        Text(s.status.statusLabel, style: ZTypography.labelSmall),
                      ],
                    ),
                  ),
                  if (s.signedAt != null)
                    Text('${s.signedAt!.day}/${s.signedAt!.month}/${s.signedAt!.year}',
                      style: ZTypography.labelSmall),
                ],
              ),
            )),
        ],
      ),
    );
  }

  ZBadgeType _badgeType(String status) => switch (status) {
    'signed' => ZBadgeType.success,
    'pending_signature' => ZBadgeType.warning,
    _ => ZBadgeType.neutral,
  };

  Future<void> _downloadPdf(BuildContext context) async {
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get(
        'documents/${widget.documentId}/pdf',
        options: Options(responseType: ResponseType.bytes),
      );
      final bytes = response.data as List<int>;
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('PDF descargado (${bytes.length ~/ 1024} KB)'),
            backgroundColor: ZColors.success,
          ),
        );
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error al descargar PDF: $e')),
        );
      }
    }
  }
}
