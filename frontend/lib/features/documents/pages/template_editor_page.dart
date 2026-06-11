import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../models/document_models.dart';
import '../providers/document_provider.dart';

/// No-Code Template Editor — "Impeccable" design
class TemplateEditorPage extends ConsumerStatefulWidget {
  final String? templateId;
  const TemplateEditorPage({super.key, this.templateId});

  @override
  ConsumerState<TemplateEditorPage> createState() => _TemplateEditorPageState();
}

class _TemplateEditorPageState extends ConsumerState<TemplateEditorPage> {
  final _formKey = GlobalKey<FormState>();
  final _nameCtrl = TextEditingController();
  final _contentCtrl = TextEditingController();
  String _category = 'HR';
  String _countryCode = 'ALL';
  String _module = 'Employee';
  bool _loading = false;
  bool _previewMode = false;

  final _categories = ['HR', 'Sales', 'Legal', 'Finance', 'General'];
  final _modules    = ['Employee', 'Sale', 'Credit', 'Warranty', 'General'];
  final _countries  = ['ALL', 'NI', 'CR', 'PA', 'GT', 'MX', 'CO'];

  final _variables = {
    'Empleado': ['{{ Employee.FullName }}', '{{ Employee.Position }}', '{{ Employee.Salary }}', '{{ Employee.HireDate }}', '{{ Employee.Identification }}'],
    'Empresa':  ['{{ Company.Name }}', '{{ Company.Date }}'],
    'Venta':    ['{{ Sale.Number }}', '{{ Sale.ClientName }}', '{{ Sale.Total }}', '{{ Sale.Date }}'],
  };

  @override
  void initState() {
    super.initState();
    // If editing, pre-fill from state
    if (widget.templateId != null) {
      final templates = ref.read(documentProvider).templates;
      final existing = templates.where((t) => t.id == widget.templateId).firstOrNull;
      if (existing != null) {
        _nameCtrl.text = existing.name;
        _contentCtrl.text = existing.content;
        _category = existing.category;
        _countryCode = existing.countryCode;
        _module = existing.module ?? 'General';
      }
    } else {
      _contentCtrl.text = _defaultTemplate();
    }
  }

  String _defaultTemplate() => '''<div style="font-family: Arial, sans-serif; line-height: 1.6;">
  <h1 style="color: #1e3a8a;">{{ Company.Name }}</h1>
  <hr>
  <p><strong>DESTINATARIO:</strong> {{ Employee.FullName }}</p>
  <p><strong>CARGO:</strong> {{ Employee.Position }}</p>
  
  <!-- Escribe el contenido de tu documento aquí -->
  <p>Cuerpo del documento...</p>
  
  <div style="margin-top: 80px;">
    <p>_________________________</p>
    <p>Firma Autorizada</p>
  </div>
</div>''';

  @override
  void dispose() {
    _nameCtrl.dispose();
    _contentCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    final error = await ref.read(documentProvider.notifier).saveTemplate({
      'name': _nameCtrl.text.trim(),
      'category': _category,
      'content': _contentCtrl.text,
      'countryCode': _countryCode,
      'module': _module,
      'isActive': true,
      'version': '1.0',
    });
    if (mounted) {
      setState(() => _loading = false);
      if (error == null) {
        ZToast.show(context, '✅ Plantilla guardada correctamente');
        context.pop();
      } else {
        ZToast.show(context, error, isError: true);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isWide = MediaQuery.sizeOf(context).width > 900;

    return Scaffold(
      appBar: AppBar(
        title: Text(widget.templateId != null ? 'Editar Plantilla' : 'Nueva Plantilla'),
        actions: [
          IconButton(
            icon: Icon(_previewMode ? Icons.edit_outlined : Icons.preview_outlined),
            tooltip: _previewMode ? 'Modo Edición' : 'Vista Previa',
            onPressed: () => setState(() => _previewMode = !_previewMode),
          ),
          const SizedBox(width: 8),
          FilledButton.icon(
            icon: _loading
                ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                : const Icon(Icons.save_outlined, size: 18),
            label: const Text('Guardar'),
            onPressed: _loading ? null : _save,
          ),
          const SizedBox(width: 16),
        ],
      ),
      body: Form(
        key: _formKey,
        child: isWide
            ? Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Sidebar: properties + variables
                  SizedBox(
                    width: 280,
                    child: _buildSidebar(),
                  ),
                  const VerticalDivider(width: 1),
                  // Main: editor or preview
                  Expanded(child: _buildEditorArea()),
                ],
              )
            : _buildMobileLayout(),
      ),
    );
  }

  Widget _buildSidebar() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('PROPIEDADES', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500, letterSpacing: 1.2)),
          const SizedBox(height: 16),
          ZTextField(
            controller: _nameCtrl,
            label: 'Nombre de la Plantilla',
            validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
          ),
          const SizedBox(height: 16),
          ZSelect<String>(
            label: 'Categoría',
            value: _category,
            items: _categories.map((c) => DropdownMenuItem(value: c, child: Text(c.categoryLabel))).toList(),
            onChanged: (v) => setState(() => _category = v!),
          ),
          const SizedBox(height: 16),
          ZSelect<String>(
            label: 'Módulo ERP',
            value: _module,
            items: _modules.map((m) => DropdownMenuItem(value: m, child: Text(m))).toList(),
            onChanged: (v) => setState(() => _module = v!),
          ),
          const SizedBox(height: 16),
          ZSelect<String>(
            label: 'País',
            value: _countryCode,
            items: _countries.map((c) => DropdownMenuItem(value: c, child: Text(c == 'ALL' ? 'Global (Todos)' : c))).toList(),
            onChanged: (v) => setState(() => _countryCode = v!),
          ),
          const SizedBox(height: 32),
          Text('VARIABLES DISPONIBLES', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500, letterSpacing: 1.2)),
          const SizedBox(height: 12),
          for (final entry in _variables.entries) ...[
            Text(entry.key, style: ZTypography.labelMedium.copyWith(fontWeight: FontWeight.w600)),
            const SizedBox(height: 6),
            Wrap(
              spacing: 6,
              runSpacing: 6,
              children: entry.value.map((v) => _VariableChip(
                variable: v,
                onTap: () => _insertVariable(v),
              )).toList(),
            ),
            const SizedBox(height: 16),
          ],
        ],
      ),
    );
  }

  Widget _buildEditorArea() {
    if (_previewMode) {
      return SingleChildScrollView(
        padding: const EdgeInsets.all(32),
        child: Column(
          children: [
            ZCard(
              padding: const EdgeInsets.all(40),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Image.asset('assets/Zorvian.png', height: 36),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(_nameCtrl.text.isNotEmpty ? _nameCtrl.text : 'Sin título',
                                style: ZTypography.titleLarge.copyWith(fontWeight: FontWeight.w700)),
                            Text(_category.categoryLabel,
                                style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
                          ],
                        ),
                      ),
                      Text('v1.0 | ${_countryCode == "ALL" ? "Global" : _countryCode}',
                          style: ZTypography.labelSmall.copyWith(color: ZColors.neutral400)),
                    ],
                  ),
                  const Divider(height: 32),
                  SelectableText(
                    _contentCtrl.text
                        .replaceAll(RegExp(r'<[^>]*>'), '')
                        .replaceAll('&nbsp;', ' '),
                    style: ZTypography.bodyMedium.copyWith(height: 1.8),
                  ),
                ],
              ),
            ),
          ],
        ),
      );
    }

    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildEditorToolbar(),
          const SizedBox(height: 12),
          Expanded(
            child: ZCard(
              padding: EdgeInsets.zero,
              child: TextField(
                controller: _contentCtrl,
                maxLines: null,
                expands: true,
                onChanged: (_) => setState(() {}),
                style: const TextStyle(fontFamily: 'monospace', fontSize: 13),
                decoration: const InputDecoration(
                  contentPadding: EdgeInsets.all(16),
                  border: InputBorder.none,
                  hintText: '<!-- Escribe tu plantilla HTML + Liquid aquí -->\n<h1>{{ Company.Name }}</h1>',
                ),
              ),
            ),
          ),
          const SizedBox(height: 8),
          Text(
            '${_contentCtrl.text.length} caracteres · Usa variables como {{ Employee.FullName }}',
            style: ZTypography.labelSmall.copyWith(color: ZColors.neutral400),
          ),
        ],
      ),
    );
  }

  Widget _buildEditorToolbar() {
    return Row(
      children: [
        Text('EDITOR HTML + LIQUID', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500, letterSpacing: 1.2)),
        const Spacer(),
        OutlinedButton.icon(
          icon: const Icon(Icons.format_bold, size: 16),
          label: const Text('<b>'),
          onPressed: () => _wrapSelection('<b>', '</b>'),
        ),
        const SizedBox(width: 8),
        OutlinedButton.icon(
          icon: const Icon(Icons.format_italic, size: 16),
          label: const Text('<i>'),
          onPressed: () => _wrapSelection('<i>', '</i>'),
        ),
        const SizedBox(width: 8),
        OutlinedButton.icon(
          icon: const Icon(Icons.table_chart_outlined, size: 16),
          label: const Text('Tabla'),
          onPressed: _insertTable,
        ),
      ],
    );
  }

  Widget _buildMobileLayout() {
    return DefaultTabController(
      length: 2,
      child: Column(
        children: [
          const TabBar(tabs: [
            Tab(text: 'Editor'),
            Tab(text: 'Propiedades'),
          ]),
          Expanded(
            child: TabBarView(children: [
              _buildEditorArea(),
              _buildSidebar(),
            ]),
          ),
        ],
      ),
    );
  }

  void _insertVariable(String variable) {
    final currentText = _contentCtrl.text;
    final cursorPos = _contentCtrl.selection.baseOffset;
    final newText = cursorPos >= 0
        ? currentText.substring(0, cursorPos) + variable + currentText.substring(cursorPos)
        : currentText + variable;
    _contentCtrl.text = newText;
    setState(() {});
  }

  void _wrapSelection(String open, String close) {
    final sel = _contentCtrl.selection;
    if (!sel.isValid) return;
    final text = _contentCtrl.text;
    final selected = sel.textInside(text);
    _contentCtrl.text = text.replaceRange(sel.start, sel.end, '$open$selected$close');
    setState(() {});
  }

  void _insertTable() {
    _contentCtrl.text += '''
<table style="width:100%; border-collapse: collapse;">
  <tr>
    <th style="border: 1px solid #ddd; padding: 8px;">Columna 1</th>
    <th style="border: 1px solid #ddd; padding: 8px;">Columna 2</th>
  </tr>
  <tr>
    <td style="border: 1px solid #ddd; padding: 8px;">Dato</td>
    <td style="border: 1px solid #ddd; padding: 8px;">Dato</td>
  </tr>
</table>''';
    setState(() {});
  }
}

class _VariableChip extends StatelessWidget {
  final String variable;
  final VoidCallback onTap;

  const _VariableChip({required this.variable, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
        decoration: BoxDecoration(
          color: ZColors.brandAccent.withOpacity(0.1),
          borderRadius: BorderRadius.circular(6),
          border: Border.all(color: ZColors.brandAccent.withOpacity(0.3)),
        ),
        child: Text(
          variable.replaceAll('{{ ', '').replaceAll(' }}', ''),
          style: ZTypography.labelSmall.copyWith(
            color: ZColors.brandAccent,
            fontFamily: 'monospace',
          ),
        ),
      ),
    );
  }
}
