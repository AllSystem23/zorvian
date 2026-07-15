import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../shared/ds/ds.dart';

final class FleetRouteFormPage extends ConsumerStatefulWidget {
  final String? routeId;
  const FleetRouteFormPage({super.key, this.routeId});

  @override
  ConsumerState<FleetRouteFormPage> createState() => _FleetRouteFormPageState();
}

final class _FleetRouteFormPageState extends ConsumerState<FleetRouteFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _codeCtrl = TextEditingController();
  final _nameCtrl = TextEditingController();
  final _originCtrl = TextEditingController();
  final _destinationCtrl = TextEditingController();
  final _distCtrl = TextEditingController();
  final _durCtrl = TextEditingController();
  final _costCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();

  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.routeId != null;

  String _type = 'Urban';
  DateTime? _scheduledDate;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final d = await dio.get('fleet/routes/${widget.routeId}');
      final dd = d.data;
      _codeCtrl.text = dd['code'] ?? '';
      _nameCtrl.text = dd['name'] ?? '';
      _type = dd['type'] ?? 'Urban';
      _scheduledDate = dd['scheduledDate'] != null ? DateTime.tryParse(dd['scheduledDate'] as String) : null;
      _originCtrl.text = dd['originAddress'] ?? '';
      _destinationCtrl.text = dd['destinationAddress'] ?? '';
      _distCtrl.text = dd['distanceEstKm']?.toString() ?? '';
      _durCtrl.text = dd['durationEstMinutes']?.toString() ?? '';
      _costCtrl.text = dd['costEst']?.toString() ?? '';
      _notesCtrl.text = dd['notes'] ?? '';
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar ruta');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{
        'code': _codeCtrl.text.trim(),
        'name': _nameCtrl.text.trim(),
        'type': _type,
        'scheduledDate': _scheduledDate?.toIso8601String().substring(0, 10),
        'originAddress': _originCtrl.text.trim(),
        'destinationAddress': _destinationCtrl.text.trim().isEmpty ? null : _destinationCtrl.text.trim(),
        'distanceEstKm': double.tryParse(_distCtrl.text.trim()) ?? 0,
        'durationEstMinutes': int.tryParse(_durCtrl.text.trim()) ?? 0,
        'costEst': double.tryParse(_costCtrl.text.trim()) ?? 0,
        'branchId': '00000000-0000-0000-0000-000000000001',
        'notes': _notesCtrl.text.trim().isEmpty ? null : _notesCtrl.text.trim(),
      };
      if (_isEditing) {
        await dio.put('fleet/routes/${widget.routeId}', data: body);
      } else {
        await dio.post('fleet/routes', data: body);
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
    _codeCtrl.dispose();
    _nameCtrl.dispose();
    _originCtrl.dispose();
    _destinationCtrl.dispose();
    _distCtrl.dispose();
    _durCtrl.dispose();
    _costCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
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
              _buildSection('Identificación', Icons.info_outline, [
                ZTextField(controller: _codeCtrl, label: 'Código', prefix: const Icon(Icons.tag), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _nameCtrl, label: 'Nombre', prefix: const Icon(Icons.label_outline), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZDropdownFormField<String>(
                  value: _type,
                  label: 'Tipo',
                  prefixIcon: Icons.category_outlined,
                  items: const [
                    DropdownMenuItem(value: 'Urban', child: Text('Urbana')),
                    DropdownMenuItem(value: 'Intercity', child: Text('Interurbana')),
                    DropdownMenuItem(value: 'LongDistance', child: Text('Larga distancia')),
                  ],
                  onChanged: (v) => setState(() => _type = v ?? 'Urban'),
                ),
              ]),
              const SizedBox(height: 20),
              _buildSection('Planificación', Icons.calendar_month_outlined, [
                InkWell(
                  onTap: () async {
                    final date = await showDatePicker(
                      context: context,
                      initialDate: _scheduledDate ?? DateTime.now(),
                      firstDate: DateTime(2000),
                      lastDate: DateTime.now().add(const Duration(days: 365)),
                    );
                    if (date != null) setState(() => _scheduledDate = date);
                  },
                  child: InputDecorator(
                    decoration: InputDecoration(
                      labelText: 'Fecha programada',
                      prefixIcon: const Icon(Icons.date_range_outlined),
                      suffixIcon: Icon(_scheduledDate != null ? Icons.check_circle : Icons.date_range, color: _scheduledDate != null ? Colors.green : null),
                    ),
                    child: Text(_scheduledDate != null ? '${_scheduledDate!.day}/${_scheduledDate!.month}/${_scheduledDate!.year}' : 'Seleccionar fecha'),
                  ),
                ),
                const SizedBox(height: 12),
                ZTextField(controller: _originCtrl, label: 'Origen', prefix: const Icon(Icons.trip_origin), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _destinationCtrl, label: 'Destino', prefix: const Icon(Icons.location_on_outlined)),
              ]),
              const SizedBox(height: 20),
              _buildSection('Datos operativos', Icons.settings_outlined, [
                ZTextField(controller: _distCtrl, label: 'Distancia estimada (km)', prefix: const Icon(Icons.straighten), keyboardType: TextInputType.number),
                const SizedBox(height: 12),
                ZTextField(controller: _durCtrl, label: 'Duración estimada (min)', prefix: const Icon(Icons.timer_outlined), keyboardType: TextInputType.number),
                const SizedBox(height: 12),
                ZTextField(controller: _costCtrl, label: 'Costo estimado', prefix: const Icon(Icons.attach_money), keyboardType: TextInputType.number),
                const SizedBox(height: 12),
                ZTextField(controller: _notesCtrl, label: 'Notas', prefix: const Icon(Icons.notes_outlined), maxLines: 3),
              ]),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear ruta',
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
