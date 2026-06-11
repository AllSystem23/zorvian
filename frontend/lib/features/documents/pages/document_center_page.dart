import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../models/document_models.dart';
import '../providers/document_provider.dart';

class DocumentCenterPage extends ConsumerStatefulWidget {
  const DocumentCenterPage({super.key});

  @override
  ConsumerState<DocumentCenterPage> createState() => _DocumentCenterPageState();
}

class _DocumentCenterPageState extends ConsumerState<DocumentCenterPage>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    Future.microtask(() {
      ref.read(documentProvider.notifier).loadTemplates();
      ref.read(documentProvider.notifier).loadDocuments();
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(documentProvider);

    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            const Icon(Icons.description_outlined, size: 22),
            const SizedBox(width: 8),
            const Text('Motor Documental'),
          ],
        ),
        actions: [
          FilledButton.icon(
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Nueva Plantilla'),
            onPressed: () => context.push('/documents/templates/new'),
          ),
          const SizedBox(width: 16),
        ],
        bottom: TabBar(
          controller: _tabController,
          tabs: const [
            Tab(icon: Icon(Icons.library_books_outlined, size: 18), text: 'Plantillas'),
            Tab(icon: Icon(Icons.folder_open_outlined, size: 18), text: 'Documentos Generados'),
          ],
        ),
      ),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : TabBarView(
              controller: _tabController,
              children: [
                _TemplateLibraryTab(templates: state.templates),
                _GeneratedDocumentsTab(documents: state.documents),
              ],
            ),
    );
  }
}

// ── Tab 1: Template Library ──

class _TemplateLibraryTab extends StatelessWidget {
  final List<DocumentTemplate> templates;

  const _TemplateLibraryTab({required this.templates});

  @override
  Widget build(BuildContext context) {
    if (templates.isEmpty) {
      return const ZEmptyState(
        icon: Icons.library_books_outlined,
        title: 'Sin Plantillas',
        subtitle: 'Crea tu primera plantilla documental para comenzar.',
      );
    }

    final grouped = <String, List<DocumentTemplate>>{};
    for (final t in templates) {
      grouped.putIfAbsent(t.category, () => []).add(t);
    }

    return ListView(
      padding: const EdgeInsets.all(24),
      children: [
        for (final entry in grouped.entries) ...[
          _CategoryHeader(label: entry.key.categoryLabel),
          const SizedBox(height: 12),
          ...entry.value.map((t) => _TemplateTile(template: t)),
          const SizedBox(height: 24),
        ],
      ],
    );
  }
}

class _CategoryHeader extends StatelessWidget {
  final String label;
  const _CategoryHeader({required this.label});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Text(
          label.toUpperCase(),
          style: ZTypography.labelSmall.copyWith(
            color: ZColors.neutral500,
            letterSpacing: 1.2,
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(width: 8),
        Expanded(child: Divider(color: ZColors.neutral200)),
      ],
    );
  }
}

class _TemplateTile extends ConsumerWidget {
  final DocumentTemplate template;
  const _TemplateTile({required this.template});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return ZCard(
      child: ListTile(
        leading: _categoryIcon(template.category),
        title: Text(template.name, style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
        subtitle: Row(
          children: [
            ZBadge(text: template.category.categoryLabel, type: _badgeType(template.category)),
            const SizedBox(width: 8),
            ZBadge(text: template.countryCode == 'ALL' ? 'Global' : template.countryCode),
            if (template.version != null) ...[
              const SizedBox(width: 8),
              Text('v${template.version}', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral400)),
            ],
          ],
        ),
        trailing: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            IconButton(
              icon: const Icon(Icons.edit_outlined, size: 18),
              tooltip: 'Editar Plantilla',
              onPressed: () => context.push('/documents/templates/${template.id}/edit'),
            ),
            IconButton(
              icon: const Icon(Icons.play_circle_outline, size: 18, color: ZColors.brandAccent),
              tooltip: 'Generar Documento',
              onPressed: () => _generateDialog(context, ref),
            ),
          ],
        ),
      ),
    );
  }

  void _generateDialog(BuildContext context, WidgetRef ref) {
    final entityCtrl = TextEditingController();
    ZModal.show(context,
      title: 'Generar Documento',
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Plantilla: ${template.name}', style: ZTypography.bodyMedium),
          const SizedBox(height: 16),
          ZTextField(controller: entityCtrl, label: 'ID del Empleado / Entidad'),
          const SizedBox(height: 24),
          ZButton(
            text: 'GENERAR',
            onPressed: () async {
              if (entityCtrl.text.isEmpty) return;
              Navigator.pop(context);
              final error = await ref.read(documentProvider.notifier)
                  .quickGenerateForEmployee(entityCtrl.text.trim(), template.id);
              if (context.mounted) {
                if (error == null) {
                  ZToast.show(context, '✅ Documento generado y enviado a firma.');
                } else {
                  ZToast.show(context, error, type: ZToastType.error);
                }
              }
            },
          ),
        ],
      ),
    );
  }

  Widget _categoryIcon(String category) {
    final (icon, color) = switch (category) {
      'HR' => (Icons.badge_outlined, ZColors.brandAccent),
      'Sales' => (Icons.point_of_sale_outlined, const Color(0xFF059669)),
      'Legal' => (Icons.gavel_outlined, const Color(0xFFA855F7)),
      'Finance' => (Icons.account_balance_outlined, const Color(0xFFD97706)),
      _ => (Icons.description_outlined, ZColors.neutral500),
    };
    return Container(
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Icon(icon, color: color, size: 20),
    );
  }

  ZBadgeType _badgeType(String category) => switch (category) {
    'HR' => ZBadgeType.accent,
    'Sales' => ZBadgeType.success,
    'Legal' => ZBadgeType.info,
    'Finance' => ZBadgeType.warning,
    _ => ZBadgeType.neutral,
  };
}

// ── Tab 2: Generated Documents ──

class _GeneratedDocumentsTab extends StatelessWidget {
  final List<GeneratedDocument> documents;

  const _GeneratedDocumentsTab({required this.documents});

  @override
  Widget build(BuildContext context) {
    if (documents.isEmpty) {
      return const ZEmptyState(
        icon: Icons.folder_open_outlined,
        title: 'Sin Documentos Generados',
        subtitle: 'Los documentos generados a través del motor aparecerán aquí.',
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.all(24),
      itemCount: documents.length,
      separatorBuilder: (_, _) => const SizedBox(height: 12),
      itemBuilder: (context, i) => _DocumentTile(document: documents[i]),
    );
  }
}

class _DocumentTile extends StatelessWidget {
  final GeneratedDocument document;

  const _DocumentTile({required this.document});

  @override
  Widget build(BuildContext context) {
    final (statusColor, statusIcon) = switch (document.status) {
      'draft' => (ZColors.neutral400, Icons.edit_note),
      'pending_signature' => (const Color(0xFFD97706), Icons.draw_outlined),
      'signed' => (const Color(0xFF059669), Icons.verified_outlined),
      'archived' => (ZColors.neutral500, Icons.archive_outlined),
      _ => (ZColors.neutral400, Icons.description),
    };

    return ZCard(
      child: ListTile(
        leading: Icon(statusIcon, color: statusColor, size: 28),
        title: Text(document.name, style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 4),
            Row(
              children: [
                ZBadge(text: document.status.statusLabel, type: ZBadgeType.neutral),
                const SizedBox(width: 8),
                Text(document.entityType, style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              'Creado: ${document.createdAt.day}/${document.createdAt.month}/${document.createdAt.year}',
              style: ZTypography.labelSmall.copyWith(color: ZColors.neutral400),
            ),
          ],
        ),
        trailing: document.status == 'pending_signature'
            ? ZButton(
                text: 'Enviar',
                onPressed: () {},
                type: ZButtonType.secondary,
              )
            : null,
        //isThreeLine: true,
      ),
    );
  }
}
