import 'package:flutter/material.dart';

import '../../../shared/ds/ds.dart';
import '../models/document_models.dart';

/// Se invoca cuando el usuario edita una variable.
typedef OnVariableEdited = void Function(String key, String value);

/// Formulario editable de las `Variables` que declara una plantilla.
///
/// Es la "previa" que faltaba en el paso 2 del wizard del Centro Documental:
/// antes solo se mostraban ~12 claves de solo lectura, así que no había nada
/// que validar ni corregir y el PDF salía con las casillas vacías.
///
/// Los campos se rellenan con [initialData] — el contexto de entidad, que para
/// la "Liquidación Final" trae `neto_a_recibir`, `indemnizacion_monto`,
/// `inss_tasa`, `neto_en_letras`, etc. El (re)sembrado **solo completa campos
/// vacíos**: lo que el usuario ya escribió a mano no se pisa cuando llega el
/// contexto asíncrono.
///
/// El estado vive aquí, pero el padre conserva su propio mapa de ediciones vía
/// [onEdited] — así el botón "Generar" no necesita un `GlobalKey` (que
/// `AnimatedSwitcher` del wizard duplicaría durante la transición de pasos).
class TemplateVariablesForm extends StatefulWidget {
  const TemplateVariablesForm({
    super.key,
    required this.variables,
    this.initialData = const <String, String>{},
    this.onEdited,
  });

  /// Variables declaradas por la plantilla (JSON de `DocumentTemplate.Variables`).
  final List<TemplateVariable> variables;

  /// Contexto de la entidad (`GET documents/entity-context`).
  final Map<String, String> initialData;

  /// Notifica cada cambio hecho por el usuario.
  final OnVariableEdited? onEdited;

  @override
  State<TemplateVariablesForm> createState() => _TemplateVariablesFormState();
}

class _TemplateVariablesFormState extends State<TemplateVariablesForm> {
  final _controllers = <String, TextEditingController>{};
  final _bools = <String, bool>{};
  final _selects = <String, String?>{};

  @override
  void initState() {
    super.initState();
    _seed();
  }

  @override
  void didUpdateWidget(covariant TemplateVariablesForm oldWidget) {
    super.didUpdateWidget(oldWidget);
    // Un mapa nuevo = el contexto de la entidad se volvió a cargar.
    if (!identical(oldWidget.initialData, widget.initialData) ||
        !identical(oldWidget.variables, widget.variables)) {
      _seed();
    }
  }

  @override
  void dispose() {
    for (final c in _controllers.values) {
      c.dispose();
    }
    super.dispose();
  }

  /// Rellena los campos vacíos con el contexto/defaults. Nunca pisa ediciones.
  void _seed() {
    for (final v in widget.variables) {
      final c = _controllers.putIfAbsent(v.key, () => TextEditingController());
      if (c.text.isEmpty) {
        final value = _initialValue(v);
        if (value.isNotEmpty) c.text = value;
      }
      if (v.type == 'select') {
        final options = v.options ?? const <String>[];
        final current = _selects[v.key];
        if (current == null || !options.contains(current)) {
          _selects[v.key] = _initialValue(v, optionsOnly: true).isEmpty
              ? (options.isNotEmpty ? options.first : null)
              : _initialValue(v, optionsOnly: true);
        }
      }
      if (v.type == 'checkbox' && _bools[v.key] == null) {
        _bools[v.key] = _truthy(_initialValue(v));
      }
    }
  }

  /// Contexto de la entidad → default de la plantilla → ''.
  String _initialValue(TemplateVariable v, {bool optionsOnly = false}) {
    if (optionsOnly) {
      final fromCtx = widget.initialData[v.key];
      if (fromCtx != null &&
          fromCtx.isNotEmpty &&
          (v.options ?? const <String>[]).contains(fromCtx)) {
        return fromCtx;
      }
      final def = v.defaultValue ?? '';
      if (def.isNotEmpty && (v.options ?? const <String>[]).contains(def)) return def;
      return '';
    }
    final fromCtx = widget.initialData[v.key];
    if (fromCtx != null && fromCtx.isNotEmpty) return fromCtx;
    return v.defaultValue ?? '';
  }

  static bool _truthy(String value) {
    final v = value.trim().toLowerCase();
    return v == 'true' || v == '1' || v == 'sí' || v == 'si' || v == 'x';
  }

  void _edit(String key, String value) => widget.onEdited?.call(key, value);

  String _label(TemplateVariable v) => v.required ? '${v.label} *' : v.label;

  @override
  Widget build(BuildContext context) {
    if (widget.variables.isEmpty) return const SizedBox.shrink();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (final v in widget.variables)
          Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: _buildField(v),
          ),
      ],
    );
  }

  Widget _buildField(TemplateVariable v) {
    switch (v.type) {
      case 'date':
        return _dateField(v);
      case 'number':
        return _numberField(v);
      case 'select':
        return _selectField(v);
      case 'checkbox':
        return _checkboxField(v);
      case 'textarea':
        return ZTextField(
          controller: _controllers[v.key],
          label: _label(v),
          maxLines: 3,
          onChanged: (val) => _edit(v.key, val.trim()),
        );
      default:
        return ZTextField(
          controller: _controllers[v.key],
          label: _label(v),
          prefix: const Icon(Icons.chevron_right, size: 18),
          onChanged: (val) => _edit(v.key, val.trim()),
        );
    }
  }

  /// Cifras de la liquidación llegan con formato `N2` ("12,345.00"): sin
  /// formatter para que la coma de miles se pueda editar.
  Widget _numberField(TemplateVariable v) {
    return ZTextField(
      controller: _controllers[v.key],
      label: _label(v),
      prefix: const Icon(Icons.numbers, size: 18),
      onChanged: (val) => _edit(v.key, val.trim()),
    );
  }

  Widget _dateField(TemplateVariable v) {
    final c = _controllers[v.key]!;
    return TextField(
      controller: c,
      readOnly: true,
      onTap: () async {
        final initial = DateTime.tryParse(c.text);
        final picked = await showDatePicker(
          context: context,
          initialDate: initial ?? DateTime.now(),
          firstDate: DateTime(1970),
          lastDate: DateTime(2100),
        );
        if (picked == null || !mounted) return;
        final text =
            '${picked.year}-${picked.month.toString().padLeft(2, '0')}-${picked.day.toString().padLeft(2, '0')}';
        c.text = text;
        _edit(v.key, text);
        setState(() {});
      },
      decoration: InputDecoration(
        labelText: _label(v),
        isDense: true,
        prefixIcon: const Icon(Icons.calendar_today, size: 18),
        suffixIcon: c.text.isEmpty
            ? null
            : IconButton(
                icon: const Icon(Icons.clear, size: 16),
                onPressed: () {
                  c.clear();
                  _edit(v.key, '');
                  setState(() {});
                },
              ),
      ),
      style: ZTypography.bodyMedium,
    );
  }

  /// `DropdownButton` (no `DropdownButtonFormField`) para que el valor sea
  /// controlado por este estado: el contexto de la entidad puede llegar después
  /// y un FormField no acepta cambios de `initialValue` tras montarse.
  Widget _selectField(TemplateVariable v) {
    final options = v.options ?? const <String>[];
    return InputDecorator(
      decoration: InputDecoration(
        labelText: _label(v),
        isDense: true,
        prefixIcon: const Icon(Icons.arrow_drop_down_circle_outlined, size: 18),
      ),
      child: DropdownButtonHideUnderline(
        child: DropdownButton<String>(
          isExpanded: true,
          isDense: true,
          value: _selects[v.key],
          items: [
            for (final opt in options)
              DropdownMenuItem<String>(value: opt, child: Text(opt, style: ZTypography.bodyMedium)),
          ],
          onChanged: options.isEmpty
              ? null
              : (val) {
                  setState(() => _selects[v.key] = val);
                  _edit(v.key, val ?? '');
                },
        ),
      ),
    );
  }

  /// `Row + Checkbox` en vez de `CheckboxListTile`: dentro de un `ZCard` el
  /// `ListTile` pinta sus ink splashes sobre el `Material` ancestro y dispara
  /// el assert de debug del proyecto (ver AGENTS.md → "ListTile dentro de ZCard").
  Widget _checkboxField(TemplateVariable v) {
    final value = _bools[v.key] ?? false;
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return InkWell(
      borderRadius: BorderRadius.circular(8),
      onTap: () {
        setState(() => _bools[v.key] = !value);
        _edit(v.key, (!value).toString());
      },
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        decoration: BoxDecoration(
          border: Border.all(
            color: value
                ? ZColors.brandAccent
                : (isDark ? ZColors.darkBorder : ZColors.neutral200),
            width: 1.5,
          ),
          borderRadius: BorderRadius.circular(8),
          color: value ? ZColors.brandAccent.withValues(alpha: 0.05) : null,
        ),
        child: Row(
          children: [
            Checkbox(
              value: value,
              onChanged: (val) {
                setState(() => _bools[v.key] = val ?? false);
                _edit(v.key, (val ?? false).toString());
              },
              materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
              activeColor: ZColors.brandAccent,
            ),
            const SizedBox(width: 8),
            Expanded(child: Text(v.label, style: ZTypography.bodyMedium)),
          ],
        ),
      ),
    );
  }
}
