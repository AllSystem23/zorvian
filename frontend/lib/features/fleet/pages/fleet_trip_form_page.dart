import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../shared/ds/ds.dart';

final class FleetTripFormPage extends ConsumerStatefulWidget {
  final String? tripId;
  const FleetTripFormPage({super.key, this.tripId});

  @override
  ConsumerState<FleetTripFormPage> createState() => _FleetTripFormPageState();
}

final class _FleetTripFormPageState extends ConsumerState<FleetTripFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _codeCtrl = TextEditingController();
  final _vehicleCtrl = TextEditingController();
  final _driverCtrl = TextEditingController();
  final _originCtrl = TextEditingController();
  final _destinationCtrl = TextEditingController();
  final _startKmCtrl = TextEditingController();
  final _endKmCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();

  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.tripId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final d = await dio.get('fleet/trips/${widget.tripId}');
      final dd = d.data;
      _codeCtrl.text = dd['code'] ?? '';
      _vehicleCtrl.text = dd['vehiclePlate'] ?? '';
      _driverCtrl.text = dd['driverName'] ?? '';
      _originCtrl.text = dd['origin'] ?? '';
      _destinationCtrl.text = dd['destination'] ?? '';
      _startKmCtrl.text = dd['startKm']?.toString() ?? '';
      _endKmCtrl.text = dd['endKm']?.toString() ?? '';
      _notesCtrl.text = dd['notes'] ?? '';
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar viaje');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{
        'code': _codeCtrl.text.trim(),
        'vehicleId': _vehicleCtrl.text.trim(),
        'driverId': _driverCtrl.text.trim(),
        'origin': _originCtrl.text.trim(),
        'destination': _destinationCtrl.text.trim(),
        'startKm': double.tryParse(_startKmCtrl.text.trim()) ?? 0,
        'endKm': double.tryParse(_endKmCtrl.text.trim()),
        'startDateTime': DateTime.now().toUtc().toIso8601String(),
        'notes': _notesCtrl.text.trim().isEmpty ? null : _notesCtrl.text.trim(),
      };
      if (_isEditing) {
        await dio.put('fleet/trips/${widget.tripId}', data: body);
      } else {
        await dio.post('fleet/trips', data: body);
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
    _vehicleCtrl.dispose();
    _driverCtrl.dispose();
    _originCtrl.dispose();
    _destinationCtrl.dispose();
    _startKmCtrl.dispose();
    _endKmCtrl.dispose();
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
                ZTextField(controller: _vehicleCtrl, label: 'Vehículo ID', prefix: const Icon(Icons.time_to_leave_outlined), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _driverCtrl, label: 'Conductor ID', prefix: const Icon(Icons.person_outline), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              ]),
              const SizedBox(height: 20),
              _buildSection('Recorrido', Icons.route_outlined, [
                ZTextField(controller: _originCtrl, label: 'Origen', prefix: const Icon(Icons.trip_origin), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _destinationCtrl, label: 'Destino', prefix: const Icon(Icons.location_on_outlined), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              ]),
              const SizedBox(height: 20),
              _buildSection('Kilometraje', Icons.speed_outlined, [
                ZTextField(controller: _startKmCtrl, label: 'Km inicial', prefix: const Icon(Icons.exposure_zero), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _endKmCtrl, label: 'Km final', prefix: const Icon(Icons.exposure_plus_1), keyboardType: TextInputType.number),
                const SizedBox(height: 12),
                ZTextField(controller: _notesCtrl, label: 'Notas', prefix: const Icon(Icons.notes_outlined), maxLines: 3),
              ]),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear viaje',
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
