import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../shared/ds/ds.dart';
import '../providers/fleet_catalog_provider.dart';

final class FleetDocumentFormPage extends ConsumerStatefulWidget {
  final String? documentId;
  final Map<String, dynamic>? extra;
  const FleetDocumentFormPage({super.key, this.documentId, this.extra});

  @override
  ConsumerState<FleetDocumentFormPage> createState() => _FleetDocumentFormPageState();
}

final class _FleetDocumentFormPageState extends ConsumerState<FleetDocumentFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _documentNumberCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();

  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.documentId != null;

  String? _entityType;
  final _entityIdCtrl = TextEditingController();
  String? _documentTypeId;
  bool _docTypeHasExpiry = false;
  DateTime? _issueDate;
  DateTime? _expiryDate;

  @override
  void initState() {
    super.initState();
    final extra = widget.extra;
    if (extra != null) {
      if (extra['entityType'] != null) _entityType = extra['entityType'] as String;
      if (extra['entityId'] != null) _entityIdCtrl.text = extra['entityId'] as String;
    }
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/documents/${widget.documentId}');
      final d = r.data;
      _entityType = d['entityType'] as String?;
      _entityIdCtrl.text = (d['entityId'] as String?) ?? '';
      _documentTypeId = d['documentTypeId'] as String?;
      _docTypeHasExpiry = d['documentTypeHasExpiry'] as bool? ?? false;
      _documentNumberCtrl.text = d['documentNumber'] as String? ?? '';
      _issueDate = d['issueDate'] != null ? DateTime.tryParse(d['issueDate'] as String) : null;
      _expiryDate = d['expiryDate'] != null ? DateTime.tryParse(d['expiryDate'] as String) : null;
      _notesCtrl.text = d['notes'] as String? ?? '';
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar documento');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{
        'entityType': _entityType,
        'entityId': _entityIdCtrl.text.trim(),
        'documentTypeId': _documentTypeId,
        'documentNumber': _documentNumberCtrl.text.trim(),
        'issueDate': _issueDate?.toIso8601String().substring(0, 10),
        'expiryDate': _expiryDate?.toIso8601String().substring(0, 10),
        'notes': _notesCtrl.text.trim().isEmpty ? null : _notesCtrl.text.trim(),
      };
      if (_isEditing) {
        await dio.put('fleet/documents/${widget.documentId}', data: body);
      } else {
        await dio.post('fleet/documents', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _documentNumberCtrl.dispose();
    _entityIdCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final docTypes = ref.watch(documentTypeListProvider);
    final filteredDocTypes = docTypes.asData?.value.where((t) => t.entityType == _entityType || _entityType == null).toList() ?? [];
    final selectedDocType = filteredDocTypes.where((t) => t.id == _documentTypeId).firstOrNull;
    if (selectedDocType != null && selectedDocType.hasExpiry != _docTypeHasExpiry) {
      _docTypeHasExpiry = selectedDocType.hasExpiry;
    }

    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar documento' : 'Nuevo documento')),
      body: SingleChildScrollView(
        padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: ZAlertCard(message: _error!, severity: 'high'),
                ),
              _buildSection('Entidad', Icons.link_outlined, [
                ZDropdownFormField<String>(
                  value: _entityType,
                  label: 'Tipo de entidad',
                  prefixIcon: Icons.category_outlined,
                  items: const [
                    DropdownMenuItem(value: 'Vehicle', child: Text('Vehículo')),
                    DropdownMenuItem(value: 'Driver', child: Text('Conductor')),
                  ],
                  onChanged: (v) => setState(() {
                    _entityType = v;
                    _documentTypeId = null;
                  }),
                  validator: (v) => v == null ? 'Requerido' : null,
                ),
                const SizedBox(height: 12),
                ZTextField(
                  controller: _entityIdCtrl,
                  label: 'ID de la entidad',
                  prefix: const Icon(Icons.tag),
                  validator: (v) => v == null || v.trim().isEmpty ? 'Requerido' : null,
                ),
              ]),
              const SizedBox(height: 20),
              _buildSection('Tipo y Número', Icons.description_outlined, [
                ZAsyncRenderer(
                  value: docTypes,
                  builder: (_) => ZDropdownFormField<String>(
                    value: _documentTypeId,
                    label: 'Tipo de documento',
                    prefixIcon: Icons.document_scanner_outlined,
                    items: filteredDocTypes.map((t) => DropdownMenuItem(value: t.id, child: Text(t.name))).toList(),
                    onChanged: (v) => setState(() {
                      _documentTypeId = v;
                      _docTypeHasExpiry = filteredDocTypes.where((t) => t.id == v).firstOrNull?.hasExpiry ?? false;
                    }),
                    validator: (v) => v == null ? 'Requerido' : null,
                  ),
                ),
                const SizedBox(height: 12),
                ZTextField(
                  controller: _documentNumberCtrl,
                  label: 'Número de documento',
                  prefix: const Icon(Icons.numbers),
                  validator: (v) => v == null || v.trim().isEmpty ? 'Requerido' : null,
                ),
              ]),
              const SizedBox(height: 20),
              _buildSection('Fechas', Icons.calendar_month_outlined, [
                InkWell(
                  onTap: () async {
                    final date = await showDatePicker(
                      context: context,
                      initialDate: _issueDate ?? DateTime.now(),
                      firstDate: DateTime(2000),
                      lastDate: DateTime.now().add(const Duration(days: 365)),
                    );
                    if (date != null) setState(() => _issueDate = date);
                  },
                  child: InputDecorator(
                    decoration: InputDecoration(
                      labelText: 'Fecha de emisión',
                      prefixIcon: const Icon(Icons.date_range_outlined),
                      suffixIcon: Icon(_issueDate != null ? Icons.check_circle : Icons.date_range, color: _issueDate != null ? Colors.green : null),
                    ),
                    child: Text(_issueDate != null ? '${_issueDate!.day}/${_issueDate!.month}/${_issueDate!.year}' : 'Seleccionar fecha'),
                  ),
                ),
                if (_docTypeHasExpiry) ...[
                  const SizedBox(height: 12),
                  InkWell(
                    onTap: () async {
                      final date = await showDatePicker(
                        context: context,
                        initialDate: _expiryDate ?? DateTime.now(),
                        firstDate: DateTime(2000),
                        lastDate: DateTime.now().add(const Duration(days: 3650)),
                      );
                      if (date != null) setState(() => _expiryDate = date);
                    },
                    child: InputDecorator(
                      decoration: InputDecoration(
                        labelText: 'Fecha de vencimiento',
                        prefixIcon: const Icon(Icons.event_outlined),
                        suffixIcon: Icon(_expiryDate != null ? Icons.check_circle : Icons.date_range, color: _expiryDate != null ? Colors.green : null),
                      ),
                      child: Text(_expiryDate != null ? '${_expiryDate!.day}/${_expiryDate!.month}/${_expiryDate!.year}' : 'Seleccionar fecha'),
                    ),
                  ),
                ],
              ]),
              const SizedBox(height: 20),
              _buildSection('Notas', Icons.notes_outlined, [
                ZTextField(
                  controller: _notesCtrl,
                  label: 'Notas',
                  prefix: const Icon(Icons.notes_outlined),
                  maxLines: 3,
                ),
              ]),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear documento',
                onPressed: _save,
                isLoading: _loading,
                icon: _isEditing ? Icons.save_outlined : Icons.add_circle_outline,
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSection(String title, IconData icon, List<Widget> children) {
    return ZCard(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(icon, size: 20, color: ZColors.moduleFleet),
              const SizedBox(width: 8),
              Text(title, style: Theme.of(context).textTheme.titleMedium),
            ],
          ),
          const SizedBox(height: 16),
          ...children,
        ],
      ),
    );
  }
}
