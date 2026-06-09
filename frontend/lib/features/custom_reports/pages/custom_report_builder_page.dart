import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../models/report_designer_models.dart';
import '../providers/custom_report_provider.dart';
import '../widgets/module_fields.dart';

class CustomReportBuilderPage extends ConsumerStatefulWidget {
  final String? reportId;

  const CustomReportBuilderPage({super.key, this.reportId});

  @override
  ConsumerState<CustomReportBuilderPage> createState() => _CustomReportBuilderPageState();
}

class _CustomReportBuilderPageState extends ConsumerState<CustomReportBuilderPage> {
  final _formKey = GlobalKey<FormState>();
  final _nameCtrl = TextEditingController();
  final _descCtrl = TextEditingController();

  String _module = 'sales';
  bool _isPublic = false;
  String _sortOrder = 'asc';
  String? _groupByField;
  String? _sortByField;

  List<ReportField> _fields = [];
  List<ReportFilter> _filters = [];
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    if (widget.reportId != null) _loadReport();
  }

  Future<void> _loadReport() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('custom-reports/${widget.reportId}');
      final json = r.data as Map<String, dynamic>;
      final item = CustomReportItem.fromJson(json);
      _nameCtrl.text = item.name;
      _descCtrl.text = item.description ?? '';
      _module = item.module;
      _isPublic = item.isPublic;
      _sortOrder = item.sortOrder;
      _groupByField = item.groupByField;
      _sortByField = item.sortByField;
      _fields = List.from(item.fields);
      _filters = List.from(item.filters);
      setState(() {});
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error al cargar reporte'), backgroundColor: Colors.red),
        );
      }
    }
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    _descCtrl.dispose();
    super.dispose();
  }

  List<AvailableField> get _availableFields => moduleFields[_module] ?? [];

  List<AvailableField> get _unselectedFields =>
      _availableFields.where((af) => !_fields.any((f) => f.source == af.field)).toList();

  void _addField(AvailableField af) {
    setState(() {
      _fields.add(ReportField(
        name: af.label,
        source: af.field,
        dataType: af.type,
        visible: true,
        order: _fields.length,
      ));
    });
  }

  void _removeField(int index) {
    setState(() {
      _fields.removeAt(index);
    });
  }

  void _toggleVisible(int index) {
    setState(() {
      final f = _fields[index];
      _fields[index] = ReportField(
        name: f.name,
        source: f.source,
        dataType: f.dataType,
        visible: !f.visible,
        order: f.order,
        aggregate: f.aggregate,
      );
    });
  }

  void _setAggregate(int index, String? agg) {
    setState(() {
      final f = _fields[index];
      _fields[index] = ReportField(
        name: f.name,
        source: f.source,
        dataType: f.dataType,
        visible: f.visible,
        order: f.order,
        aggregate: agg,
      );
    });
  }

  void _addFilter() {
    setState(() {
      _filters.add(ReportFilter(field: _availableFields.first.field, operator: 'eq', value: ''));
    });
  }

  void _removeFilter(int index) {
    setState(() {
      _filters.removeAt(index);
    });
  }

  void _updateFilter(int index, {String? field, String? operator, dynamic value}) {
    setState(() {
      _filters[index] = ReportFilter(
        field: field ?? _filters[index].field,
        operator: operator ?? _filters[index].operator,
        value: value ?? _filters[index].value,
      );
    });
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'name': _nameCtrl.text.trim(),
        'description': _descCtrl.text.trim(),
        'module': _module,
        'fields': _fields.map((f) => f.toJson()).toList(),
        'filters': _filters.map((f) => f.toJson()).toList(),
        'groupByField': _groupByField,
        'sortByField': _sortByField,
        'sortOrder': _sortOrder,
        'isPublic': _isPublic,
      };

      if (widget.reportId != null) {
        await dio.put('custom-reports/${widget.reportId}', data: body);
      } else {
        await dio.post('custom-reports', data: body);
      }

      if (mounted) {
        ref.read(customReportProvider.notifier).load();
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(widget.reportId != null ? 'Reporte actualizado' : 'Reporte creado')),
        );
        context.pop();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.reportId != null ? 'Editar Reporte' : 'Nuevo Reporte'),
        actions: [
          TextButton.icon(
            icon: const Icon(Icons.play_arrow),
            label: const Text('Vista Previa'),
            onPressed: _preview,
          ),
        ],
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            TextFormField(
              controller: _nameCtrl,
              decoration: const InputDecoration(labelText: 'Nombre del reporte', border: OutlineInputBorder()),
              validator: (v) => (v == null || v.trim().isEmpty) ? 'Requerido' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _descCtrl,
              decoration: const InputDecoration(labelText: 'Descripción', border: OutlineInputBorder()),
              maxLines: 2,
            ),
            const SizedBox(height: 16),

            DropdownButtonFormField<String>(
              initialValue: _module,
              decoration: const InputDecoration(labelText: 'Módulo', border: OutlineInputBorder()),
              items: const [
                DropdownMenuItem(value: 'sales', child: Text('Ventas')),
                DropdownMenuItem(value: 'purchases', child: Text('Compras')),
                DropdownMenuItem(value: 'products', child: Text('Productos')),
                DropdownMenuItem(value: 'clients', child: Text('Clientes')),
                DropdownMenuItem(value: 'suppliers', child: Text('Proveedores')),
                DropdownMenuItem(value: 'employees', child: Text('Empleados')),
              ],
              onChanged: (v) {
                if (v == null) return;
                setState(() {
                  _module = v;
                  _fields.clear();
                  _filters.clear();
                  _groupByField = null;
                  _sortByField = null;
                });
              },
            ),
            const SizedBox(height: 24),

            _sectionTitle('Campos del Reporte'),
            const SizedBox(height: 8),
            if (_availableFields.isNotEmpty)
              Wrap(
                spacing: 6,
                runSpacing: 4,
                children: _unselectedFields.map((af) => ActionChip(
                  avatar: const Icon(Icons.add, size: 16),
                  label: Text(af.label, style: const TextStyle(fontSize: 12)),
                  onPressed: () => _addField(af),
                )).toList(),
              ),
            const SizedBox(height: 8),
            if (_fields.isEmpty)
              const Padding(padding: EdgeInsets.all(8), child: Text('Selecciona campos del módulo arriba', style: TextStyle(color: Colors.grey)))
            else
              ReorderableListView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: _fields.length,
                onReorderItem: (oldI, newI) {
                  setState(() {
                    final f = _fields.removeAt(oldI);
                    _fields.insert(newI, f);
                  });
                },
                itemBuilder: (_, i) {
                  final f = _fields[i];
                  return Card(
                    key: ValueKey('field_$i'),
                    margin: const EdgeInsets.symmetric(vertical: 2),
                    child: ListTile(
                      dense: true,
                      leading: ReorderableDragStartListener(
                        index: i,
                        child: const Icon(Icons.drag_handle),
                      ),
                      title: Text(f.name, style: const TextStyle(fontSize: 13)),
                      subtitle: Row(
                        children: [
                          _smallBtn(f.visible ? Icons.visibility : Icons.visibility_off, () => _toggleVisible(i)),
                          const SizedBox(width: 4),
                          SizedBox(
                            width: 90,
                            child: DropdownButtonFormField<String?>(
                              initialValue: f.aggregate,
                              decoration: const InputDecoration(contentPadding: EdgeInsets.symmetric(horizontal: 6), border: OutlineInputBorder(), isDense: true),
                              items: aggregateOptions.map((a) => DropdownMenuItem(value: a, child: Text(a ?? '---', style: const TextStyle(fontSize: 11)))).toList(),
                              onChanged: (v) => _setAggregate(i, v),
                            ),
                          ),
                        ],
                      ),
                      trailing: IconButton(
                        icon: const Icon(Icons.close, size: 18),
                        onPressed: () => _removeField(i),
                      ),
                    ),
                  );
                },
              ),
            const SizedBox(height: 24),

            _sectionTitle('Filtros'),
            const SizedBox(height: 8),
            ..._filters.asMap().entries.map((e) {
              final i = e.key;
              final ft = e.value;
              return Card(
                key: ValueKey('filter_$i'),
                margin: const EdgeInsets.symmetric(vertical: 2),
                child: Padding(
                  padding: const EdgeInsets.all(8),
                  child: Row(
                    children: [
                      Expanded(
                        child: DropdownButtonFormField<String>(
                          initialValue: ft.field,
                          decoration: const InputDecoration(isDense: true, contentPadding: EdgeInsets.symmetric(horizontal: 6), border: OutlineInputBorder(), labelText: 'Campo'),
                          items: _availableFields.map((af) => DropdownMenuItem(value: af.field, child: Text(af.label, style: const TextStyle(fontSize: 11)))).toList(),
                          onChanged: (v) => _updateFilter(i, field: v),
                        ),
                      ),
                      const SizedBox(width: 4),
                      SizedBox(
                        width: 80,
                        child: DropdownButtonFormField<String>(
                          initialValue: ft.operator,
                          decoration: const InputDecoration(isDense: true, contentPadding: EdgeInsets.symmetric(horizontal: 4), border: OutlineInputBorder()),
                          items: operatorOptions.map((o) => DropdownMenuItem(value: o, child: Text(operatorLabel(o), style: const TextStyle(fontSize: 11)))).toList(),
                          onChanged: (v) => _updateFilter(i, operator: v),
                        ),
                      ),
                      const SizedBox(width: 4),
                      SizedBox(
                        width: 70,
                        child: TextFormField(
                          initialValue: ft.value,
                          decoration: const InputDecoration(isDense: true, contentPadding: EdgeInsets.symmetric(horizontal: 4), border: OutlineInputBorder(), labelText: 'Valor'),
                          onChanged: (v) => _updateFilter(i, value: v),
                        ),
                      ),
                      IconButton(
                        icon: const Icon(Icons.close, size: 18),
                        onPressed: () => _removeFilter(i),
                      ),
                    ],
                  ),
                ),
              );
            }),
            TextButton.icon(
              icon: const Icon(Icons.add),
              label: const Text('Agregar Filtro'),
              onPressed: _addFilter,
            ),
            const SizedBox(height: 24),

            _sectionTitle('Agrupación y Orden'),
            const SizedBox(height: 8),
            DropdownButtonFormField<String?>(
              initialValue: _groupByField,
              decoration: const InputDecoration(labelText: 'Agrupar por', border: OutlineInputBorder()),
              items: [
                const DropdownMenuItem(value: null, child: Text('Ninguno')),
                ..._fields.where((f) => f.visible).map((f) {
                  return DropdownMenuItem(value: f.source, child: Text(f.name));
                }),
              ],
              onChanged: (v) => setState(() => _groupByField = v),
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String?>(
              initialValue: _sortByField,
              decoration: const InputDecoration(labelText: 'Ordenar por', border: OutlineInputBorder()),
              items: [
                const DropdownMenuItem(value: null, child: Text('Ninguno')),
                ..._fields.where((f) => f.visible).map((f) {
                  return DropdownMenuItem(value: f.source, child: Text(f.name));
                }),
              ],
              onChanged: (v) => setState(() => _sortByField = v),
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              initialValue: _sortOrder,
              decoration: const InputDecoration(labelText: 'Orden', border: OutlineInputBorder()),
              items: const [
                DropdownMenuItem(value: 'asc', child: Text('Ascendente')),
                DropdownMenuItem(value: 'desc', child: Text('Descendente')),
              ],
              onChanged: (v) => setState(() => _sortOrder = v!),
            ),
            const SizedBox(height: 12),

            SwitchListTile(
              title: const Text('Reporte público'),
              subtitle: const Text('Visible para todos los usuarios de la empresa'),
              value: _isPublic,
              onChanged: (v) => setState(() => _isPublic = v),
            ),
            const SizedBox(height: 24),

            FilledButton.icon(
              icon: _saving ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white)) : const Icon(Icons.save),
              label: Text(_saving ? 'Guardando...' : 'Guardar Reporte'),
              onPressed: _saving ? null : _save,
            ),
            const SizedBox(height: 32),
          ],
        ),
      ),
    );
  }

  Padding _sectionTitle(String text) {
    return Padding(
      padding: const EdgeInsets.only(top: 8, bottom: 4),
      child: Text(text, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
    );
  }

  Widget _smallBtn(IconData icon, VoidCallback onTap) {
    return InkWell(
      onTap: onTap,
      child: Padding(padding: const EdgeInsets.all(2), child: Icon(icon, size: 16)),
    );
  }

  Future<void> _preview() async {
    if (_fields.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Agrega al menos un campo'), backgroundColor: Colors.orange),
      );
      return;
    }

    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'name': _nameCtrl.text.trim().isEmpty ? 'Vista Previa' : _nameCtrl.text.trim(),
        'description': _descCtrl.text.trim(),
        'module': _module,
        'fields': _fields.map((f) => f.toJson()).toList(),
        'filters': _filters.map((f) => f.toJson()).toList(),
        'groupByField': _groupByField,
        'sortByField': _sortByField,
        'sortOrder': _sortOrder,
        'isPublic': _isPublic,
      };
      final response = await dio.post('custom-reports/execute-adhoc?module=$_module', data: body);
      final result = response.data as Map<String, dynamic>;
      if (mounted) {
        context.push('/custom-reports/result', extra: {
          'reportName': _nameCtrl.text.trim().isEmpty ? 'Vista Previa' : _nameCtrl.text.trim(),
          'module': _module,
          'fields': _fields.map((f) => f.toJson()).toList(),
          'columns': result['columns'] as List,
          'rows': result['rows'] as List,
        });
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red),
        );
      }
    }
  }
}
