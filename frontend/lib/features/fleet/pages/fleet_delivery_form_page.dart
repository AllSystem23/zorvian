import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../shared/ds/ds.dart';

final class FleetDeliveryFormPage extends ConsumerStatefulWidget {
  final String? deliveryId;
  const FleetDeliveryFormPage({super.key, this.deliveryId});

  @override
  ConsumerState<FleetDeliveryFormPage> createState() => _FleetDeliveryFormPageState();
}

final class _FleetDeliveryFormPageState extends ConsumerState<FleetDeliveryFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _codeCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();
  final _observationsCtrl = TextEditingController();

  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.deliveryId != null;

  DateTime? _scheduledDate;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final d = await dio.get('fleet/deliveries/${widget.deliveryId}');
      final dd = d.data;
      _codeCtrl.text = dd['code'] ?? '';
      _addressCtrl.text = dd['deliveryAddress'] ?? '';
      _observationsCtrl.text = dd['observations'] ?? '';
      _scheduledDate = dd['scheduledDate'] != null ? DateTime.tryParse(dd['scheduledDate'] as String) : null;
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar entrega');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{
        'code': _codeCtrl.text.trim(),
        'deliveryAddress': _addressCtrl.text.trim(),
        'scheduledDate': _scheduledDate?.toIso8601String().substring(0, 10),
        'observations': _observationsCtrl.text.trim().isEmpty ? null : _observationsCtrl.text.trim(),
      };
      if (_isEditing) {
        await dio.put('fleet/deliveries/${widget.deliveryId}', data: body);
      } else {
        await dio.post('fleet/deliveries', data: body);
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
    _addressCtrl.dispose();
    _observationsCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar entrega' : 'Nueva entrega')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
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
                ZTextField(controller: _addressCtrl, label: 'Dirección de entrega', prefix: const Icon(Icons.location_on_outlined), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
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
              ]),
              const SizedBox(height: 20),
              _buildSection('Notas', Icons.notes_outlined, [
                ZTextField(controller: _observationsCtrl, label: 'Observaciones', prefix: const Icon(Icons.notes_outlined), maxLines: 3),
              ]),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear entrega',
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
