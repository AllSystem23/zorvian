import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
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
    final controllers = <String, TextEditingController>{};
    final boolValues = <String, bool>{};
    final selectedValues = <String, String?>{};
    for (final v in template.variables) {
      controllers[v.key] = TextEditingController(text: v.defaultValue ?? '');
      if (v.type == 'checkbox') {
        boolValues[v.key] = v.defaultValue == 'true';
      }
      if (v.type == 'select' && v.options != null && v.options!.isNotEmpty) {
        selectedValues[v.key] = v.defaultValue ?? v.options!.first;
      }
    }

    ZModal.show(context, title: 'Generar Documento',
      child: StatefulBuilder(
        builder: (ctx, setModalState) => Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ZTextField(controller: entityCtrl, label: 'ID Entidad'),
            if (template.variables.isNotEmpty) ...[
              const SizedBox(height: 16),
              Align(
                alignment: Alignment.centerLeft,
                child: Text('Variables de la Plantilla',
                  style: ZTypography.labelMedium.copyWith(color: ZColors.brandAccent)),
              ),
              const SizedBox(height: 8),
              for (final v in template.variables)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: _buildTypeWidget(v, controllers, boolValues, selectedValues, setModalState),
                ),
            ],
            const SizedBox(height: 16),
            ZButton(text: 'Generar', onPressed: () async {
              final variables = <String, String>{};
              for (final v in template.variables) {
                String val;
                if (v.type == 'checkbox') {
                  val = (boolValues[v.key] ?? false).toString();
                } else if (v.type == 'select') {
                  val = selectedValues[v.key] ?? '';
                } else {
                  val = controllers[v.key]?.text.trim() ?? '';
                }
                if (v.required && val.isEmpty) {
                  if (ctx.mounted) {
                    ScaffoldMessenger.of(ctx).showSnackBar(
                      SnackBar(content: Text('El campo "${v.label}" es requerido')));
                  }
                  return;
                }
                variables[v.key] = val;
              }
              Navigator.pop(ctx);
              await ref.read(documentProvider.notifier).generateDocument(
                templateId: template.id,
                entityId: entityCtrl.text.trim(),
                variables: variables,
              );
            }),
          ],
        ),
      ),
    );
  }

  /// Builds the appropriate input widget based on variable type
  Widget _buildTypeWidget(
    TemplateVariable v,
    Map<String, TextEditingController> controllers,
    Map<String, bool> boolValues,
    Map<String, String?> selectedValues,
    StateSetter setModalState,
  ) {
    final label = '${v.label}${v.required ? ' *' : ''}';

    switch (v.type) {
      case 'date':
        return _DateVariableField(
          label: label,
          controller: controllers[v.key]!,
          onChanged: () => setModalState(() {}),
        );

      case 'number':
        return _NumberVariableField(
          label: label,
          controller: controllers[v.key]!,
        );

      case 'select':
        return _SelectVariableField(
          label: label,
          options: v.options ?? [],
          selectedValue: selectedValues[v.key],
          onChanged: (val) => setModalState(() => selectedValues[v.key] = val),
        );

      case 'checkbox':
        return _CheckboxVariableField(
          label: v.label,
          value: boolValues[v.key] ?? false,
          onChanged: (val) => setModalState(() => boolValues[v.key] = val),
        );

      case 'textarea':
        return ZTextField(
          controller: controllers[v.key],
          label: label,
          maxLines: 3,
        );

      default: // text
        return ZTextField(
          controller: controllers[v.key],
          label: label,
        );
    }
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

// ── Type-specific variable input widgets ──

class _DateVariableField extends StatelessWidget {
  final String label;
  final TextEditingController controller;
  final VoidCallback onChanged;

  const _DateVariableField({
    required this.label,
    required this.controller,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: () async {
        final now = DateTime.now();
        final picked = await showDatePicker(
          context: context,
          initialDate: controller.text.isNotEmpty
              ? DateTime.tryParse(controller.text) ?? now
              : now,
          firstDate: DateTime(2000),
          lastDate: DateTime(2100),
        );
        if (picked != null) {
          controller.text = '${picked.year}-${picked.month.toString().padLeft(2, '0')}-${picked.day.toString().padLeft(2, '0')}';
          onChanged();
        }
      },
      borderRadius: BorderRadius.circular(8),
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          isDense: true,
          border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
          prefixIcon: const Icon(Icons.calendar_today, size: 18),
          suffixIcon: controller.text.isNotEmpty
              ? IconButton(
                  icon: const Icon(Icons.clear, size: 16),
                  onPressed: () {
                    controller.clear();
                    onChanged();
                  },
                )
              : null,
        ),
        child: Text(
          controller.text.isNotEmpty ? controller.text : 'Seleccionar fecha...',
          style: TextStyle(
            color: controller.text.isNotEmpty ? null : ZColors.neutral400,
          ),
        ),
      ),
    );
  }
}

class _NumberVariableField extends StatelessWidget {
  final String label;
  final TextEditingController controller;

  const _NumberVariableField({
    required this.label,
    required this.controller,
  });

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      keyboardType: const TextInputType.numberWithOptions(decimal: true, signed: false),
      inputFormatters: [
        FilteringTextInputFormatter.allow(RegExp(r'^\d*\.?\d{0,2}$')),
      ],
      decoration: InputDecoration(
        labelText: label,
        isDense: true,
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
        prefixIcon: const Icon(Icons.numbers, size: 18),
        hintText: '0.00',
      ),
      style: const TextStyle(fontSize: 14, fontFamily: 'monospace'),
    );
  }
}

class _SelectVariableField extends StatelessWidget {
  final String label;
  final List<String> options;
  final String? selectedValue;
  final ValueChanged<String?> onChanged;

  const _SelectVariableField({
    required this.label,
    required this.options,
    required this.selectedValue,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<String>(
      initialValue: selectedValue,
      isDense: true,
      decoration: InputDecoration(
        labelText: label,
        isDense: true,
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
        prefixIcon: const Icon(Icons.arrow_drop_down_circle_outlined, size: 18),
      ),
      items: options.map((opt) => DropdownMenuItem(
        value: opt,
        child: Text(opt, style: const TextStyle(fontSize: 14)),
      )).toList(),
      onChanged: onChanged,
    );
  }
}

class _CheckboxVariableField extends StatelessWidget {
  final String label;
  final bool value;
  final ValueChanged<bool> onChanged;

  const _CheckboxVariableField({
    required this.label,
    required this.value,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: () => onChanged(!value),
      borderRadius: BorderRadius.circular(8),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        decoration: BoxDecoration(
          border: Border.all(
            color: value ? ZColors.brandAccent : ZColors.neutral200,
            width: 1.5,
          ),
          borderRadius: BorderRadius.circular(8),
          color: value ? ZColors.brandAccent.withValues(alpha: 0.05) : null,
        ),
        child: Row(
          children: [
            Checkbox(
              value: value,
              onChanged: (_) {},
              materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
              activeColor: ZColors.brandAccent,
            ),
            const SizedBox(width: 8),
            Text(label, style: ZTypography.bodyMedium),
          ],
        ),
      ),
    );
  }
}
