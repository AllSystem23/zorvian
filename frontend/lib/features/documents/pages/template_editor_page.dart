import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_html/flutter_html.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
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
  String? _renderedHtml;

  final _categories = ['HR', 'Sales', 'Legal', 'Finance', 'General'];
  final _modules    = ['Employee', 'Sale', 'Credit', 'Warranty', 'General'];
  final _countries  = ['ALL', 'NI', 'CR', 'PA', 'GT', 'MX', 'CO'];

  final _variables = {
    'Empleado': ['{{ Employee.FullName }}', '{{ Employee.Position }}', '{{ Employee.Salary }}', '{{ Employee.HireDate }}', '{{ Employee.Identification }}'],
    'Empresa':  ['{{ Company.Name }}', '{{ Company.Date }}'],
    'Venta':    ['{{ Sale.Number }}', '{{ Sale.ClientName }}', '{{ Sale.Total }}', '{{ Sale.Date }}'],
  };

  // ── Dynamic variables builder ──
  final List<_VariableDef> _customVars = [];

  @override
  void initState() {
    super.initState();
    if (widget.templateId != null) {
      final templates = ref.read(documentProvider).templates;
      final existing = templates.where((t) => t.id == widget.templateId).firstOrNull;
      if (existing != null) {
        _nameCtrl.text = existing.name;
        _contentCtrl.text = existing.content;
        _category = existing.category;
        _countryCode = existing.countryCode;
        _module = existing.module ?? 'General';
        // Parse existing variables from template
        for (final v in existing.variables) {
          _customVars.add(_VariableDef(
            keyCtrl: TextEditingController(text: v.key),
            label: TextEditingController(text: v.label),
            type: v.type,
            required: v.required,
            options: v.options?.join(', '),
          ));
        }
      }
    } else {
      _contentCtrl.text = _defaultTemplate();
    }

    // Auto-scan content for variables on load (deferred to after first frame)
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) {
        final count = _scanContentForVariables();
        if (count > 0) {
          setState(() {});
          ZToast.show(context, '🔍 $count variables detectadas en el contenido', type: ZToastType.info);
        }
      }
    });
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
    for (final v in _customVars) {
      v.keyCtrl.dispose();
      v.label.dispose();
      v.optionsCtrl.dispose();
    }
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);

    // Build variables JSON from custom vars
    final variablesJson = _customVars
        .where((v) => v.keyCtrl.text.trim().isNotEmpty)
        .map((v) {
          final map = {
            'key': v.keyCtrl.text.trim(),
            'label': v.label.text.trim(),
            'type': v.type,
            'required': v.required,
          };
          if (v.type == 'select' && v.optionsCtrl.text.trim().isNotEmpty) {
            final opts = v.optionsCtrl.text.trim().split(',').map((e) => e.trim()).where((e) => e.isNotEmpty).toList();
            map['options'] = opts;
          }
          return map;
        })
        .toList();

    final error = await ref.read(documentProvider.notifier).saveTemplate({
      'name': _nameCtrl.text.trim(),
      'category': _category,
      'content': _contentCtrl.text,
      'countryCode': _countryCode,
      'module': _module,
      'isActive': true,
      'version': '1.0',
      'variables': variablesJson.isNotEmpty ? jsonEncode(variablesJson) : null,
    });
    if (mounted) {
      setState(() => _loading = false);
      if (error == null) {
        ZToast.show(context, '✅ Plantilla guardada correctamente');
        context.pop();
      } else {
        ZToast.show(context, error, type: ZToastType.error);
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
            onPressed: () => setState(() {
              _previewMode = !_previewMode;
              _renderedHtml = null;
            }),
          ),
          if (_previewMode)
            IconButton(
              icon: _renderedHtml != null
                  ? const Icon(Icons.code_outlined, size: 20)
                  : const Icon(Icons.play_circle_outline, size: 20),
              tooltip: _renderedHtml != null ? 'Ver HTML crudo' : 'Renderizar con datos',
              onPressed: () => _toggleRenderedPreview(),
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
          Text('VARIABLES RÁPIDAS', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500, letterSpacing: 1.2)),
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
          const Divider(height: 32),
          Row(
            children: [
              Expanded(child: Text('VARIABLES PERSONALIZADAS', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500, letterSpacing: 1.2))),
              IconButton(
                icon: const Icon(Icons.add_circle_outline, size: 20),
                tooltip: 'Agregar variable',
                onPressed: _addCustomVariable,
              ),
            ],
          ),
          const SizedBox(height: 8),
          if (_customVars.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 8),
              child: Text('Sin variables definidas. Haz clic en + para agregar.',
                style: ZTypography.bodySmall.copyWith(color: ZColors.neutral400, fontStyle: FontStyle.italic)),
            ),
          for (int i = 0; i < _customVars.length; i++)
            _buildVariableRow(i),
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
            // Rendered preview with real Liquid data
            if (_renderedHtml != null)
              ZCard(
                padding: const EdgeInsets.all(24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        const Icon(Icons.auto_fix_high, size: 18, color: ZColors.brandAccent),
                        const SizedBox(width: 8),
                        Text('VISTA PREVIA RENDERIZADA', style: ZTypography.labelSmall.copyWith(color: ZColors.brandAccent, letterSpacing: 1.2, fontWeight: FontWeight.w600)),
                        const Spacer(),
                        Text('Datos de ejemplo', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral400)),
                      ],
                    ),
                    const Divider(height: 24),
                    Html(data: _renderedHtml!),
                  ],
                ),
              ),
            // Static preview (raw Liquid text)
            if (_renderedHtml == null)
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
                onChanged: (_) => _onContentChanged(),
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
        const SizedBox(width: 8),
        Tooltip(
          message: 'Escanear contenido y detectar variables',
          child: OutlinedButton.icon(
            icon: const Icon(Icons.auto_fix_high, size: 16),
            label: const Text('Detectar'),
            onPressed: _manualScanVariables,
          ),
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

    // Auto-detect: extract key from {{ Key }} and auto-create custom var
    final key = _extractKeyFromVariable(variable);
    if (key != null && !_customVars.any((v) => v.keyCtrl.text == key)) {
      final detectedType = _autoDetectType(key);
      setState(() {
        _customVars.add(_VariableDef(
          keyCtrl: TextEditingController(text: key),
          label: TextEditingController(text: _keyToLabel(key)),
          type: detectedType,
          required: false,
        ));
      });
      ZToast.show(context, 'Variable detectada: ${_keyToLabel(key)} (${_typeLabel(detectedType)})', type: ZToastType.success);
    } else {
      setState(() {});
    }
  }

  /// Extracts the key from a Liquid variable like "{{ Employee.Salary }}" -> "Employee.Salary"
  String? _extractKeyFromVariable(String variable) {
    final match = RegExp(r'\{\{\s*([\w.]+)(?:\s*\|.*)?\s*\}\}').firstMatch(variable);
    return match?.group(1);
  }

  /// Auto-detect variable type from its key name using keyword patterns
  static String _autoDetectType(String key) {
    final lower = key.toLowerCase();

    // Date patterns
    if (lower.contains('date') || lower.contains('fecha') ||
        lower.contains('hired') || lower.contains('created') ||
        lower.contains('updated') || lower.contains('expir') ||
        lower.contains('born') || lower.contains('birth') ||
        lower.contains('start') || lower.contains('end') ||
        lower.contains('deadline') || lower.contains('due')) {
      return 'date';
    }

    // Number patterns
    if (lower.contains('salary') || lower.contains('total') ||
        lower.contains('amount') || lower.contains('price') ||
        lower.contains('cost') || lower.contains('tax') ||
        lower.contains('qty') || lower.contains('quantity') ||
        lower.contains('num') || lower.contains('rate') ||
        lower.contains('percent') || lower.contains('discount') ||
        lower.contains('balance') || lower.contains('payment') ||
        lower.contains('income') || lower.contains('expense') ||
        lower.contains('revenue') || lower.contains('profit') ||
        lower.contains('subtotal') || lower.contains('iva') ||
        lower.contains('saldo') || lower.contains('monto') ||
        lower.contains('precio') || lower.contains('costo') ||
        lower.contains('impuesto')) {
      return 'number';
    }

    // Checkbox patterns
    if (lower.startsWith('is_') || lower.startsWith('has_') ||
        lower.contains('active') || lower.contains('enabled') ||
        lower.contains('flag') || lower.contains('approved') ||
        lower.contains('authorized') || lower.contains('paid') ||
        lower.contains('verified') || lower.contains('activo') ||
        lower.contains('habilitado') || lower.contains('aprobado') ||
        lower.contains('autorizado') || lower.contains('pagado') ||
        lower.contains('verificado')) {
      return 'checkbox';
    }

    return 'text';
  }

  /// Converts a dot-separated key to a human-readable label
  /// e.g. "Employee.Salary" -> "Employee Salary"
  static String _keyToLabel(String key) {
    return key
        .replaceAll('.', ' ')
        .replaceAll('_', ' ')
        .split(' ')
        .map((w) => w.isEmpty ? w : '${w[0].toUpperCase()}${w.substring(1)}')
        .join(' ');
  }

  /// Human-readable type labels
  static String _typeLabel(String type) => switch (type) {
    'date' => '📅 Fecha',
    'number' => '🔢 Numérico',
    'checkbox' => '☑️ Casilla',
    'select' => '📋 Selección',
    'textarea' => '📝 Texto largo',
    _ => '✏️ Texto',
  };

  /// Scans the HTML content for {{ variable }} patterns and auto-creates
  /// custom variable definitions for any that are not yet defined.
  int _scanContentForVariables() {
    final content = _contentCtrl.text;
    final matches = RegExp(r'\{\{\s*([\w.]+)(?:\s*\|.*)?\s*\}\}').allMatches(content);
    final existingKeys = _customVars.map((v) => v.keyCtrl.text).toSet();
    int added = 0;

    for (final match in matches) {
      final key = match.group(1)!;
      if (!existingKeys.contains(key)) {
        final detectedType = _autoDetectType(key);
        _customVars.add(_VariableDef(
          keyCtrl: TextEditingController(text: key),
          label: TextEditingController(text: _keyToLabel(key)),
          type: detectedType,
          required: false,
        ));
        existingKeys.add(key);
        added++;
      }
    }
    return added;
  }

  void _addCustomVariable() {
    setState(() {
      _customVars.add(_VariableDef(
        keyCtrl: TextEditingController(),
        label: TextEditingController(),
        type: 'text',
        required: false,
      ));
    });
  }

  void _removeCustomVariable(int index) {
    setState(() {
      _customVars[index].keyCtrl.dispose();
      _customVars[index].label.dispose();
      _customVars[index].optionsCtrl.dispose();
      _customVars.removeAt(index);
    });
  }

  Widget _buildVariableRow(int index) {
    final v = _customVars[index];
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      elevation: 0,
      color: ZColors.neutral50,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(8),
        side: BorderSide(color: ZColors.neutral200),
      ),
      child: Padding(
        padding: const EdgeInsets.all(8),
        child: Column(
          children: [
            Row(
              children: [
                Expanded(
                  flex: 3,
                  child: TextField(
                    controller: v.keyCtrl,
                    decoration: InputDecoration(
                      hintText: 'key (ej: client_name)',
                      isDense: true,
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(6)),
                      contentPadding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                      errorText: v.keyCtrl.text.contains(' ') ? 'Sin espacios' : null,
                    ),
                    style: const TextStyle(fontSize: 11, fontFamily: 'monospace'),
                  ),
                ),
                const SizedBox(width: 6),
                Expanded(
                  flex: 4,
                  child: TextField(
                    controller: v.label,
                    decoration: InputDecoration(
                      hintText: 'Etiqueta',
                      isDense: true,
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(6)),
                      contentPadding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                    ),
                    style: const TextStyle(fontSize: 11),
                  ),
                ),
                const SizedBox(width: 4),
                IconButton(
                  icon: const Icon(Icons.close, size: 16),
                  padding: EdgeInsets.zero,
                  constraints: const BoxConstraints(minWidth: 28, minHeight: 28),
                  onPressed: () => _removeCustomVariable(index),
                ),
              ],
            ),
            const SizedBox(height: 6),
            Row(
              children: [
                Expanded(
                  child: DropdownButtonFormField<String>(
                    initialValue: v.type,
                    isDense: true,
                    items: const [
                      DropdownMenuItem(value: 'text', child: Text('Texto')),
                      DropdownMenuItem(value: 'number', child: Text('Numérico')),
                      DropdownMenuItem(value: 'date', child: Text('Fecha')),
                      DropdownMenuItem(value: 'textarea', child: Text('Texto largo')),
                      DropdownMenuItem(value: 'select', child: Text('Selección')),
                      DropdownMenuItem(value: 'checkbox', child: Text('Casilla')),
                    ],
                    onChanged: (val) => setState(() => v.type = val ?? 'text'),
                    decoration: InputDecoration(
                      isDense: true,
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(6)),
                      contentPadding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                    ),
                    style: const TextStyle(fontSize: 11),
                  ),
                ),
                const SizedBox(width: 8),
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Checkbox(
                      value: v.required,
                      onChanged: (val) => setState(() => v.required = val ?? false),
                      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                      visualDensity: VisualDensity.compact,
                    ),
                    Text('Req.', style: ZTypography.labelSmall.copyWith(fontSize: 10)),
                  ],
                ),
              ],
            ),
            // Options editor for 'select' type
            if (v.type == 'select') ...[
              const SizedBox(height: 6),
              TextField(
                controller: v.optionsCtrl,
                decoration: InputDecoration(
                  hintText: 'Opciones separadas por coma',
                  isDense: true,
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(6)),
                  contentPadding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                  prefixIcon: const Icon(Icons.list_alt, size: 14),
                  errorText: v.optionsCtrl.text.trim().isEmpty && v.required ? 'Requerido para tipo Seleccion' : null,
                ),
                style: const TextStyle(fontSize: 11),
              ),
            ],
            // Type-specific info row
            if (v.type == 'date')
              Padding(
                padding: const EdgeInsets.only(top: 6),
                child: Text('📅 El usuario verá un selector de fecha nativo',
                  style: ZTypography.labelSmall.copyWith(fontSize: 9, color: ZColors.neutral400, fontStyle: FontStyle.italic)),
              ),
            if (v.type == 'number')
              Padding(
                padding: const EdgeInsets.only(top: 6),
                child: Text('🔢 Campo numérico con formato de miles y decimales',
                  style: ZTypography.labelSmall.copyWith(fontSize: 9, color: ZColors.neutral400, fontStyle: FontStyle.italic)),
              ),
            if (v.type == 'checkbox')
              Padding(
                padding: const EdgeInsets.only(top: 6),
                child: Text('☑️ El usuario verá una casilla de verificación',
                  style: ZTypography.labelSmall.copyWith(fontSize: 9, color: ZColors.neutral400, fontStyle: FontStyle.italic)),
              ),
          ],
        ),
      ),
    );
  }

  // Debounce for content scanning
  DateTime? _lastScanTime;

  void _onContentChanged() {
    setState(() {});
    // Debounced auto-scan: reschedule on each keystroke, fire after 500ms of inactivity
    _lastScanTime = DateTime.now();
    final capturedTime = _lastScanTime!;
    Future.delayed(const Duration(milliseconds: 500), () {
      if (mounted && _lastScanTime == capturedTime) {
        final count = _scanContentForVariables();
        if (count > 0) {
          setState(() {});
          ZToast.show(context, '🔍 $count nuevas variables detectadas', type: ZToastType.info);
        }
      }
    });
  }

  void _manualScanVariables() {
    final count = _scanContentForVariables();
    if (count > 0) {
      setState(() {});
      ZToast.show(context, '🔍 $count variables detectadas y agregadas', type: ZToastType.success);
    } else {
      ZToast.show(context, '✅ No hay variables nuevas por detectar', type: ZToastType.info);
    }
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

  Future<void> _toggleRenderedPreview() async {
    if (_renderedHtml != null) {
      setState(() => _renderedHtml = null);
      return;
    }
    setState(() => _loading = true);
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.post('documents/preview', data: {
        'templateId': widget.templateId,
        'content': _contentCtrl.text,
        'variables': null,
      });
      if (mounted) {
        setState(() {
          _renderedHtml = response.data['html'] as String?;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _loading = false);
        ZToast.show(context, 'Error al renderizar: $e', type: ZToastType.error);
      }
    }
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
          color: ZColors.brandAccent.withValues(alpha: 0.1),
          borderRadius: BorderRadius.circular(6),
          border: Border.all(color: ZColors.brandAccent.withValues(alpha: 0.3)),
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

class _VariableDef {
  final TextEditingController keyCtrl;
  final TextEditingController label;
  final TextEditingController optionsCtrl; // comma-separated for 'select' type
  String type;
  bool required;

  _VariableDef({
    required this.keyCtrl,
    required this.label,
    required this.type,
    required this.required,
    String? options,
  }) : optionsCtrl = TextEditingController(text: options ?? '');
}
