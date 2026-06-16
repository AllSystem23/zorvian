import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../shared/ds/ds.dart';

final class FleetFuelFormPage extends ConsumerStatefulWidget {
  final String? fuelId;
  const FleetFuelFormPage({super.key, this.fuelId});

  @override
  ConsumerState<FleetFuelFormPage> createState() => _FleetFuelFormPageState();
}

final class _FleetFuelFormPageState extends ConsumerState<FleetFuelFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _vehicleCtrl = TextEditingController();
  final _driverCtrl = TextEditingController();
  final _litersCtrl = TextEditingController();
  final _pricePerLiterCtrl = TextEditingController();
  final _totalCostCtrl = TextEditingController();
  final _currentKmCtrl = TextEditingController();
  final _observationsCtrl = TextEditingController();

  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.fuelId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final d = await dio.get('fleet/fuel-refills/${widget.fuelId}');
      final dd = d.data;
      _vehicleCtrl.text = dd['vehiclePlate'] ?? '';
      _driverCtrl.text = dd['driverName'] ?? '';
      _litersCtrl.text = dd['liters']?.toString() ?? '';
      _pricePerLiterCtrl.text = dd['pricePerLiter']?.toString() ?? '';
      _totalCostCtrl.text = dd['totalCost']?.toString() ?? '';
      _currentKmCtrl.text = dd['currentKm']?.toString() ?? '';
      _observationsCtrl.text = dd['observations'] ?? '';
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{
        'refillDateTime': DateTime.now().toUtc().toIso8601String(),
        'vehicleId': _vehicleCtrl.text.trim(),
        'driverId': _driverCtrl.text.trim(),
        'fuelTypeId': _vehicleCtrl.text.trim(),
        'liters': double.tryParse(_litersCtrl.text.trim()) ?? 0,
        'pricePerLiter': double.tryParse(_pricePerLiterCtrl.text.trim()) ?? 0,
        'totalCost': double.tryParse(_totalCostCtrl.text.trim()) ?? 0,
        'currentKm': double.tryParse(_currentKmCtrl.text.trim()) ?? 0,
        'refillType': 'Full',
        'paymentMethod': 'Cash',
        'observations': _observationsCtrl.text.trim().isEmpty ? null : _observationsCtrl.text.trim(),
        'validForCalculation': true,
      };
      if (_isEditing) {
        await dio.put('fleet/fuel-refills/${widget.fuelId}', data: body);
      } else {
        await dio.post('fleet/fuel-refills', data: body);
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
    _vehicleCtrl.dispose();
    _driverCtrl.dispose();
    _litersCtrl.dispose();
    _pricePerLiterCtrl.dispose();
    _totalCostCtrl.dispose();
    _currentKmCtrl.dispose();
    _observationsCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar carga' : 'Nueva carga')),
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
              _buildSection('Vehículo y Conductor', Icons.local_gas_station_outlined, [
                ZTextField(controller: _vehicleCtrl, label: 'Vehículo ID', prefix: const Icon(Icons.time_to_leave_outlined), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _driverCtrl, label: 'Conductor ID', prefix: const Icon(Icons.person_outline), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              ]),
              const SizedBox(height: 20),
              _buildSection('Carga', Icons.local_gas_station_outlined, [
                ZTextField(controller: _litersCtrl, label: 'Litros', prefix: const Icon(Icons.speed), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _pricePerLiterCtrl, label: 'Precio por litro', prefix: const Icon(Icons.monetization_on_outlined), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _totalCostCtrl, label: 'Costo total', prefix: const Icon(Icons.payments_outlined), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _currentKmCtrl, label: 'Kilometraje actual', prefix: const Icon(Icons.speed_outlined), keyboardType: TextInputType.number),
                const SizedBox(height: 12),
                ZTextField(controller: _observationsCtrl, label: 'Observaciones', prefix: const Icon(Icons.notes_outlined), maxLines: 3),
              ]),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Registrar carga',
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
