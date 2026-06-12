import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../core/widgets/responsive_layout.dart';
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
      backgroundColor: Theme.of(context).brightness == Brightness.dark ? ZColors.darkBackground : ZColors.neutral50,
      appBar: AppBar(
        title: const Text('Centro Documental'),
        actions: [
          IconButton(icon: const Icon(Icons.search), onPressed: () => ZCommandPalette.show(context)),
          const SizedBox(width: 8),
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
            Tab(text: 'Plantillas'),
            Tab(text: 'Documentos Generados'),
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
    if (templates.isEmpty) return const ZEmptyState(icon: Icons.library_books_outlined, title: 'Sin Plantillas');
    
    return ListView(
      padding: const EdgeInsets.all(24),
      children: templates.map((t) => _TemplateTile(template: t)).toList(),
    );
  }
}

class _TemplateTile extends ConsumerWidget {
  final DocumentTemplate template;
  const _TemplateTile({required this.template});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return ZCard(
      margin: const EdgeInsets.only(bottom: 12),
      child: ListTile(
        leading: Icon(Icons.description_outlined, color: ZColors.brandPrimary),
        title: Text(template.name, style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
        subtitle: Row(
          children: [
            ZBadge(text: template.category.toUpperCase(), type: ZBadgeType.neutral),
            const SizedBox(width: 8),
            Text(template.countryCode, style: ZTypography.labelSmall),
          ],
        ),
        trailing: IconButton(
          icon: const Icon(Icons.play_circle_outline, color: ZColors.brandAccent),
          onPressed: () => _generateDialog(context, ref),
        ),
      ),
    );
  }

  void _generateDialog(BuildContext context, WidgetRef ref) {
    final entityCtrl = TextEditingController();
    ZModal.show(context, title: 'Generar Documento',
      child: Column(children: [
        ZTextField(controller: entityCtrl, label: 'ID Entidad'),
        const SizedBox(height: 16),
        ZButton(text: 'Generar', onPressed: () async {
          Navigator.pop(context);
          await ref.read(documentProvider.notifier).quickGenerateForEmployee(entityCtrl.text.trim(), template.id);
        })
      ]),
    );
  }
}

// ── Tab 2: Generated Documents ──
class _GeneratedDocumentsTab extends StatelessWidget {
  final List<GeneratedDocument> documents;
  const _GeneratedDocumentsTab({required this.documents});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        children: [
          // Stat Overview
          ResponsiveGrid(
            mobileColumns: 1,
            tabletColumns: 3,
            desktopColumns: 3,
            children: [
              ZStatCard(title: 'Borradores', value: '${documents.where((d) => d.status == 'draft').length}', icon: Icons.edit_note),
              ZStatCard(title: 'Pendientes de Firma', value: '${documents.where((d) => d.status == 'pending_signature').length}', icon: Icons.draw_outlined, variant: ZStatVariant.warning),
              ZStatCard(title: 'Firmados', value: '${documents.where((d) => d.status == 'signed').length}', icon: Icons.verified_outlined, variant: ZStatVariant.success),
            ],
          ),
          const SizedBox(height: 32),
          Expanded(
            child: ZDataTable<GeneratedDocument>(
              columns: const [
                ZColumn(id: 'name', label: 'Documento'),
                ZColumn(id: 'type', label: 'Tipo'),
                ZColumn(id: 'date', label: 'Fecha'),
                ZColumn(id: 'status', label: 'Estado'),
              ],
              rows: documents,
              rowMapper: (d) => DataRow(cells: [
                DataCell(Text(d.name, style: const TextStyle(fontWeight: FontWeight.w600))),
                DataCell(Text(d.entityType)),
                DataCell(Text('${d.createdAt.day}/${d.createdAt.month}/${d.createdAt.year}')),
                DataCell(ZBadge(text: d.status.toUpperCase(), type: _badgeType(d.status))),
              ]),
            ),
          ),
        ],
      ),
    );
  }

  ZBadgeType _badgeType(String status) => switch (status) {
    'signed' => ZBadgeType.success,
    'pending_signature' => ZBadgeType.warning,
    _ => ZBadgeType.neutral,
  };
}
