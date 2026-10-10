import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:share_plus/share_plus.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../models/document_models.dart';
import '../providers/document_provider.dart';
import '../widgets/entity_picker_dialog.dart';

class QuickGenerateWizardPage extends ConsumerStatefulWidget {
  final String? entityType;
  final String? entityId;
  final String? entityDisplayName;
  final String? preselectedTemplateId;

  const QuickGenerateWizardPage({
    super.key,
    this.entityType,
    this.entityId,
    this.entityDisplayName,
    this.preselectedTemplateId,
  });

  @override
  ConsumerState<QuickGenerateWizardPage> createState() => _QuickGenerateWizardPageState();
}

class _QuickGenerateWizardPageState extends ConsumerState<QuickGenerateWizardPage> {
  final _entityIdCtrl = TextEditingController();
  String _selectedEntityType = 'employee';
  List<DocumentTemplate> _filteredTemplates = [];
  bool _templatesLoaded = false;

  static final _uuidRegex = RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$',
  );

  @override
  void initState() {
    super.initState();
    if (widget.entityType != null) _selectedEntityType = widget.entityType!;
    if (widget.entityId != null) _entityIdCtrl.text = widget.entityId!;
    Future.microtask(_initWizard);
  }

  Future<void> _initWizard() async {
    final notifier = ref.read(documentProvider.notifier);
    await notifier.loadTemplates(pageSize: 50);
    if (!mounted) return;
    final wizard = ref.read(wizardProvider.notifier);

    wizard.start();
    _applyPreselectedTemplate();
    _filterByEntityType();

    if (widget.entityType != null && widget.entityId != null) {
      await wizard.selectEntity(
        entityType: widget.entityType!,
        entityId: widget.entityId!,
        entityDisplayName: widget.entityDisplayName,
      );
      if (!mounted) return;
      final err = ref.read(wizardProvider).error;
      if (err != null) ZToast.error(context, err);
    }
  }

  void _applyPreselectedTemplate() {
    final id = widget.preselectedTemplateId;
    if (id == null || id.isEmpty) return;
    final all = ref.read(documentProvider).templates;
    for (final t in all) {
      if (t.id == id) {
        // Alinear el tipo de entidad con el módulo de la plantilla para que
        // aparezca en la lista filtrada del paso 1.
        if (widget.entityType == null) {
          setState(() {
            _selectedEntityType = switch (t.module) {
              'HR' => 'employee',
              'Sales' => _selectedEntityType == 'employee' ? 'sale' : _selectedEntityType,
              _ => _selectedEntityType,
            };
          });
        }
        ref.read(wizardProvider.notifier).preselectTemplate(t);
        break;
      }
    }
  }

  void _filterByEntityType() {
    final allTemplates = ref.read(documentProvider).templates;
    final module = switch (_selectedEntityType) {
      'employee' => 'HR',
      'sale' => 'Sales',
      'client' => 'Sales',
      _ => null,
    };
    setState(() {
      _filteredTemplates = allTemplates
          .where((t) => module == null || t.module == module)
          .toList();
      _templatesLoaded = true;
    });
  }

  Future<void> _openEntityPicker() async {
    final result = await showEntityPickerDialog(
      context,
      entityType: _selectedEntityType,
    );
    if (result == null || !mounted) return;
    await _selectEntity(result.id, result.displayName);
  }

  Future<void> _selectEntity(String entityId, String? displayName) async {
    final wiz = ref.read(wizardProvider.notifier);
    await wiz.selectEntity(
      entityType: _selectedEntityType,
      entityId: entityId,
      entityDisplayName: displayName,
    );
    if (!mounted) return;
    final err = ref.read(wizardProvider).error;
    if (err != null) ZToast.error(context, err);
  }

  void _clearEntitySelection() {
    _entityIdCtrl.clear();
    ref.read(wizardProvider.notifier).clearEntity();
  }

  Future<void> _selectTemplate(DocumentTemplate template) async {
    final wizard = ref.read(wizardProvider);
    final wiz = ref.read(wizardProvider.notifier);
    if (wizard.entityContext == null) {
      await wiz.loadEntityContext();
      if (!mounted) return;
      if (ref.read(wizardProvider).error != null) return;
    }
    wiz.selectTemplate(template);
  }

  String _entityTypeLabel([String? type]) {
    switch (type ?? _selectedEntityType) {
      case 'employee':
        return 'trabajador';
      case 'sale':
        return 'venta';
      case 'client':
        return 'cliente';
      default:
        return 'entidad';
    }
  }

  IconData get _entityTypeIcon => switch (_selectedEntityType) {
        'employee' => Icons.person_outline,
        'sale' => Icons.receipt_outlined,
        _ => Icons.people_outline,
      };

  @override
  void dispose() {
    _entityIdCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final wizard = ref.watch(wizardProvider);
    final docState = ref.watch(documentProvider);

    return Scaffold(
      backgroundColor: theme.brightness == Brightness.dark ? ZColors.darkBackground : ZColors.neutral50,
      body: docState.loading && !_templatesLoaded
          ? const Center(child: CircularProgressIndicator())
          : _buildWizard(context, wizard),
    );
  }

  Widget _buildWizard(BuildContext context, WizardState wizard) {
    return Column(
      children: [
        _buildStepper(context, wizard.step),
        Expanded(
          child: AnimatedSwitcher(
            duration: const Duration(milliseconds: 300),
            child: _buildStepContent(context, wizard),
          ),
        ),
      ],
    );
  }

  Widget _buildStepper(BuildContext context, int currentStep) {
    final steps = ['Seleccionar', 'Confirmar', 'Listo'];
    final icons = [Icons.description_outlined, Icons.visibility_outlined, Icons.check_circle_outline];
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final inactiveColor = isDark ? ZColors.darkBorder : ZColors.neutral200;

    return Container(
      padding: const EdgeInsets.symmetric(vertical: 24, horizontal: 32),
      decoration: BoxDecoration(
        color: Theme.of(context).cardColor,
        border: Border(bottom: BorderSide(color: inactiveColor, width: 1)),
      ),
      child: Row(
        children: List.generate(steps.length, (i) {
          final isActive = i + 1 == currentStep;
          final isDone = i + 1 < currentStep;
          return Expanded(
            child: Row(
              children: [
                if (i > 0)
                  Expanded(
                    child: Container(
                      height: 2,
                      color: isDone ? ZColors.brandAccent : inactiveColor,
                    ),
                  ),
                const SizedBox(width: 8),
                AnimatedContainer(
                  duration: const Duration(milliseconds: 300),
                  width: 36,
                  height: 36,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: isDone
                        ? ZColors.success
                        : isActive
                            ? ZColors.brandAccent
                            : inactiveColor,
                  ),
                  child: Icon(
                    isDone ? Icons.check : icons[i],
                    size: 18,
                    color: isDone || isActive ? Colors.white : ZColors.neutral500,
                  ),
                ),
                const SizedBox(width: 8),
                Text(
                  steps[i],
                  style: ZTypography.labelMedium.copyWith(
                    fontWeight: isActive ? FontWeight.w700 : FontWeight.w500,
                    color: isActive
                        ? ZColors.brandAccent
                        : isDone
                            ? ZColors.success
                            : ZColors.neutral500,
                  ),
                ),
                const SizedBox(width: 8),
              ],
            ),
          );
        }),
      ),
    );
  }

  Widget _buildStepContent(BuildContext context, WizardState wizard) {
    switch (wizard.step) {
      case 1:
        return _buildStep1(context, wizard);
      case 2:
        return _buildStep2(context, wizard);
      case 3:
        return _buildStep3(context, wizard);
      default:
        return const SizedBox.shrink();
    }
  }

  Widget _buildStep1(BuildContext context, WizardState wizard) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Paso 1: Selecciona el tipo de documento',
            style: ZTypography.titleMedium),
          const SizedBox(height: 8),
          Text('Elige la entidad y la plantilla que deseas generar.',
            style: ZTypography.bodySmall),
          const SizedBox(height: 24),

          _buildEntityTypeSelector(),
          const SizedBox(height: 8),
          _buildEntitySelector(wizard),

          if (wizard.error != null) ...[
            const SizedBox(height: 12),
            _buildErrorBanner(context, wizard.error!),
          ],

          const SizedBox(height: 20),

          if (wizard.entityId == null || wizard.entityId!.isEmpty)
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: Text(
                'Selecciona primero una entidad para poder elegir una plantilla.',
                style: ZTypography.labelSmall.copyWith(color: ZColors.neutral400),
              ),
            ),

          if (_filteredTemplates.isEmpty && _templatesLoaded)
            ZEmptyState(
              icon: Icons.library_books_outlined,
              title: 'Sin plantillas para ${_selectedEntityType == 'employee' ? 'RRHH' : 'Ventas'}',
              subtitle: 'Crea una plantilla en el Centro Documental primero.',
            ),

          ..._filteredTemplates.map((t) => _buildTemplateCard(context, t, wizard)),
        ],
      ),
    );
  }

  Widget _buildEntityTypeSelector() {
    return ZCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Tipo de Entidad', style: ZTypography.labelMedium),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _entityTypeChip('employee', 'Trabajador', Icons.person_outline),
                _entityTypeChip('sale', 'Venta', Icons.receipt_outlined),
                _entityTypeChip('client', 'Cliente', Icons.people_outline),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _entityTypeChip(String type, String label, IconData icon) {
    final selected = _selectedEntityType == type;
    return FilterChip(
      selected: selected,
      label: Text(label),
      avatar: Icon(icon, size: 18),
      onSelected: (_) {
        setState(() => _selectedEntityType = type);
        _clearEntitySelection();
        _filterByEntityType();
      },
      selectedColor: ZColors.brandAccent.withValues(alpha: 0.15),
      checkmarkColor: ZColors.brandAccent,
    );
  }

  Widget _buildEntitySelector(WizardState wizard) {
    final hasEntity = wizard.entityId != null && wizard.entityId!.isNotEmpty;
    return ZCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Entidad', style: ZTypography.labelMedium),
            const SizedBox(height: 4),
            Text('Busca el ${_entityTypeLabel()} por nombre, factura o cédula.',
              style: ZTypography.labelSmall),
            const SizedBox(height: 12),
            if (!hasEntity) ...[
              FilledButton.tonalIcon(
                onPressed: _openEntityPicker,
                icon: const Icon(Icons.search, size: 18),
                label: Text('Buscar ${_entityTypeLabel()}'),
              ),
              const SizedBox(height: 4),
              Theme(
                data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
                child: ExpansionTile(
                  tilePadding: EdgeInsets.zero,
                  childrenPadding: const EdgeInsets.only(bottom: 8),
                  title: Text(
                    'O ingresa el UUID manualmente',
                    style: ZTypography.labelSmall.copyWith(color: ZColors.neutral400),
                  ),
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: ZTextField(
                            controller: _entityIdCtrl,
                            label: 'UUID',
                            hint: '00000000-0000-0000-0000-000000000000',
                          ),
                        ),
                        const SizedBox(width: 8),
                        FilledButton.tonalIcon(
                          onPressed: _entityIdCtrl.text.trim().isEmpty
                              ? null
                              : () {
                                  final id = _entityIdCtrl.text.trim();
                                  if (!_uuidRegex.hasMatch(id)) {
                                    ZToast.error(context, 'Ingresa un UUID válido (con guiones).');
                                    return;
                                  }
                                  _selectEntity(id, null);
                                },
                          icon: const Icon(Icons.arrow_forward, size: 18),
                          label: const Text('Cargar'),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ] else
              _buildSelectedEntityCard(wizard),
          ],
        ),
      ),
    );
  }

  Widget _buildSelectedEntityCard(WizardState wizard) {
    final loadingContext = wizard.loading && wizard.entityContext == null;
    return Row(
      children: [
        CircleAvatar(
          backgroundColor: ZColors.brandAccent.withValues(alpha: 0.1),
          child: Icon(_entityTypeIcon, color: ZColors.brandAccent, size: 20),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                wizard.entityDisplayName ?? '${_entityTypeLabel().toUpperCase()} seleccionado',
                style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600),
                overflow: TextOverflow.ellipsis,
              ),
              Text(
                loadingContext ? 'Cargando datos...' : 'ID: ${wizard.entityId}',
                style: ZTypography.labelSmall.copyWith(
                  color: loadingContext ? ZColors.brandAccent : ZColors.neutral400,
                ),
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
        if (loadingContext)
          const SizedBox(
            width: 18,
            height: 18,
            child: CircularProgressIndicator(strokeWidth: 2),
          )
        else
          TextButton.icon(
            onPressed: _clearEntitySelection,
            icon: const Icon(Icons.swap_horiz, size: 18),
            label: const Text('Cambiar'),
          ),
      ],
    );
  }

  Widget _buildErrorBanner(BuildContext context, String message) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: ZColors.danger.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: ZColors.danger.withValues(alpha: 0.3)),
      ),
      child: Row(
        children: [
          const Icon(Icons.error_outline, color: ZColors.danger, size: 20),
          const SizedBox(width: 10),
          Expanded(
            child: Text(message, style: ZTypography.bodySmall.copyWith(color: ZColors.danger)),
          ),
        ],
      ),
    );
  }

  Widget _buildTemplateCard(BuildContext context, DocumentTemplate template, WizardState wizard) {
    final isSelected = wizard.selectedTemplate?.id == template.id;
    final canSelect = wizard.entityId != null && wizard.entityId!.isNotEmpty;

    return InkWell(
      onTap: canSelect ? () => _selectTemplate(template) : null,
      borderRadius: BorderRadius.circular(16),
      child: Container(
        margin: const EdgeInsets.only(bottom: 12),
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Theme.of(context).cardColor,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: isSelected ? ZColors.brandAccent : (Theme.of(context).brightness == Brightness.dark ? ZColors.darkBorder : ZColors.border),
            width: isSelected ? 2 : 0.8,
          ),
        ),
        child: Row(
          children: [
            Container(
              width: 48,
              height: 48,
              decoration: BoxDecoration(
                color: ZColors.brandAccent.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(Icons.description_outlined, color: ZColors.brandAccent),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(template.name, style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      ZBadge(text: template.category, type: ZBadgeType.neutral),
                      const SizedBox(width: 8),
                      Text(template.countryCode, style: ZTypography.labelSmall),
                    ],
                  ),
                ],
              ),
            ),
            if (isSelected)
              const Icon(Icons.check_circle, color: ZColors.success, size: 24),
          ],
        ),
      ),
    );
  }

  Widget _buildStep2(BuildContext context, WizardState wizard) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Paso 2: Confirma los datos',
            style: ZTypography.titleMedium),
          const SizedBox(height: 8),
          Text('Revisa la información que se usará para generar el documento.',
            style: ZTypography.bodySmall),
          const SizedBox(height: 24),

          if (wizard.loading)
            const Center(child: Padding(
              padding: EdgeInsets.all(48),
              child: CircularProgressIndicator(),
            ))
          else ...[
            _buildEntitySummaryCard(context, wizard),
            const SizedBox(height: 16),
            if (wizard.entityContext != null)
              _buildDataPreviewCard(context, wizard.entityContext!),
            const SizedBox(height: 16),
            if (wizard.selectedTemplate != null)
              _buildTemplateSummaryCard(context, wizard.selectedTemplate!),
            if (wizard.error != null) ...[
              const SizedBox(height: 16),
              _buildErrorBanner(context, wizard.error!),
            ],
            const SizedBox(height: 32),
            Row(
              children: [
                Expanded(
                  child: ZButton(
                    text: ' ← Atrás',
                    type: ZButtonType.secondary,
                    onPressed: () => ref.read(wizardProvider.notifier).backToSelection(),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  flex: 2,
                  child: wizard.entityContext != null
                      ? ZButton(
                          text: wizard.error != null ? ' Reintentar →' : ' Generar Documento →',
                          isLoading: wizard.loading,
                          onPressed: () => ref.read(wizardProvider.notifier).executeQuickGenerate(),
                        )
                      : ZButton(
                          text: ' Cargando datos...',
                          type: ZButtonType.secondary,
                          onPressed: () {},
                        ),
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildEntitySummaryCard(BuildContext context, WizardState wizard) {
    return ZCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            CircleAvatar(
              backgroundColor: ZColors.brandAccent.withValues(alpha: 0.1),
              child: Icon(Icons.business, color: ZColors.brandAccent),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(wizard.entityDisplayName ?? wizard.entityId ?? '',
                    style: ZTypography.bodyLarge.copyWith(fontWeight: FontWeight.w600)),
                  Text('Tipo: ${wizard.entityType?.toUpperCase() ?? ''}',
                    style: ZTypography.labelSmall),
                ],
              ),
            ),
            ZBadge(text: wizard.entityType?.toUpperCase() ?? '', type: ZBadgeType.info),
          ],
        ),
      ),
    );
  }

  Widget _buildDataPreviewCard(BuildContext context, EntityContext ctx) {
    final previewKeys = ctx.data.keys.take(12).toList();
    return ZCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.data_array, size: 18, color: ZColors.brandAccent),
                const SizedBox(width: 8),
                Text('Datos disponibles (${ctx.data.length} variables)',
                  style: ZTypography.labelMedium),
              ],
            ),
            const SizedBox(height: 12),
            ...previewKeys.map((key) => Padding(
              padding: const EdgeInsets.symmetric(vertical: 3),
              child: Row(
                children: [
                  SizedBox(
                    width: 140,
                    child: Text(key, style: ZTypography.labelSmall.copyWith(
                      color: ZColors.neutral500, fontFamily: 'monospace', fontSize: 11)),
                  ),
                  Expanded(
                    child: Text(ctx.data[key] ?? '',
                      style: ZTypography.bodySmall.copyWith(fontSize: 12)),
                  ),
                ],
              ),
            )),
            if (ctx.data.length > 12)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text('... y ${ctx.data.length - 12} variables más',
                  style: ZTypography.labelSmall.copyWith(color: ZColors.neutral400)),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildTemplateSummaryCard(BuildContext context, DocumentTemplate template) {
    return ZCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Icon(Icons.description_outlined, color: ZColors.brandAccent),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(template.name, style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
                  Text('Plantilla • ${template.category}',
                    style: ZTypography.labelSmall),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStep3(BuildContext context, WizardState wizard) {
    final result = wizard.result;
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const SizedBox(height: 32),
            Container(
              width: 96,
              height: 96,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: ZColors.success.withValues(alpha: 0.1),
              ),
              child: const Icon(Icons.check_circle, size: 56, color: ZColors.success),
            ),
            const SizedBox(height: 24),
            Text('Documento Generado', style: ZTypography.titleLarge),
            const SizedBox(height: 8),
            Text(result?.name ?? '',
              style: ZTypography.bodyMedium,
              textAlign: TextAlign.center),
            const SizedBox(height: 4),
            ZBadge(text: (result?.status ?? '').statusLabel, type: ZBadgeType.success),
            const SizedBox(height: 32),

            ZCard(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: [
                    _actionRow(
                      context,
                      icon: Icons.visibility_outlined,
                      label: 'Ver Detalles',
                      subtitle: 'Revisa el documento y su estado de firma',
                      onTap: () {
                        ref.read(wizardProvider.notifier).reset();
                        context.push('/documents/${result!.documentId}');
                      },
                    ),
                    const Divider(height: 1),
                    _actionRow(
                      context,
                      icon: Icons.download_outlined,
                      label: 'Descargar PDF',
                      subtitle: 'Descarga el documento en formato PDF',
                      onTap: result != null ? () => _downloadPdf(context, result.documentId) : null,
                    ),
                    const Divider(height: 1),
                    _actionRow(
                      context,
                      icon: Icons.draw_outlined,
                      label: 'Gestionar Firmas',
                      subtitle: result?.signatureToken != null
                          ? 'Token: ${result!.signatureToken!.substring(0, 8)}...'
                          : 'Solicitar firmas adicionales',
                      onTap: () {
                        ref.read(wizardProvider.notifier).reset();
                        context.push('/documents/${result!.documentId}');
                      },
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 32),
            SizedBox(
              width: double.infinity,
              child: ZButton(
                text: 'Volver al Centro Documental',
                type: ZButtonType.secondary,
                onPressed: () {
                  ref.read(wizardProvider.notifier).reset();
                  context.pop();
                },
              ),
            ),
            const SizedBox(height: 48),
          ],
        ),
      ),
    );
  }

  Widget _actionRow(BuildContext context, {
    required IconData icon,
    required String label,
    required String subtitle,
    VoidCallback? onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(8),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 8),
        child: Row(
          children: [
            Icon(icon, size: 22, color: ZColors.brandAccent),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(label, style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
                  Text(subtitle, style: ZTypography.labelSmall),
                ],
              ),
            ),
            const Icon(Icons.chevron_right, size: 20, color: ZColors.neutral400),
          ],
        ),
      ),
    );
  }

  Future<void> _downloadPdf(BuildContext context, String documentId) async {
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get(
        'documents/$documentId/pdf',
        options: Options(responseType: ResponseType.bytes),
      );
      final bytes = response.data as List<int>;
      await SharePlus.instance.share(
        ShareParams(files: [
          XFile.fromData(
            Uint8List.fromList(bytes),
            mimeType: 'application/pdf',
            name: 'documento_$documentId.pdf',
          ),
        ]),
      );
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('PDF listo para compartir (${bytes.length ~/ 1024} KB)'),
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
