import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../core/providers/company_branch_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/fleet_catalog_provider.dart';

final class FleetVehicleFormPage extends ConsumerStatefulWidget {
  final String? vehicleId;
  const FleetVehicleFormPage({super.key, this.vehicleId});

  @override
  ConsumerState<FleetVehicleFormPage> createState() => _FleetVehicleFormPageState();
}

final class _FleetVehicleFormPageState extends ConsumerState<FleetVehicleFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _codeCtrl = TextEditingController();
  final _plateCtrl = TextEditingController();
  final _modelCtrl = TextEditingController();
  final _yearCtrl = TextEditingController();
  final _vinCtrl = TextEditingController();
  final _engineCtrl = TextEditingController();
  final _chassisCtrl = TextEditingController();
  final _colorCtrl = TextEditingController();
  final _kmCtrl = TextEditingController();
  final _loadKgCtrl = TextEditingController();
  final _loadM3Ctrl = TextEditingController();
  final _passengerCtrl = TextEditingController();
  final _purchaseValueCtrl = TextEditingController();

  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.vehicleId != null;

  String? _brandId;
  String? _vehicleTypeId;
  String? _fuelTypeId;
  String? _branchId;
  DateTime? _purchaseDate;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/vehicles/${widget.vehicleId}');
      final d = r.data;
      _codeCtrl.text = d['code'] ?? '';
      _plateCtrl.text = d['plate'] ?? '';
      _modelCtrl.text = d['model'] ?? '';
      _yearCtrl.text = d['year']?.toString() ?? '';
      _vinCtrl.text = d['vin'] ?? '';
      _engineCtrl.text = d['engineNumber'] ?? '';
      _chassisCtrl.text = d['chassisNumber'] ?? '';
      _colorCtrl.text = d['color'] ?? '';
      _kmCtrl.text = d['currentKm']?.toString() ?? '';
      _loadKgCtrl.text = d['loadCapacityKg']?.toString() ?? '';
      _loadM3Ctrl.text = d['loadCapacityM3']?.toString() ?? '';
      _passengerCtrl.text = d['passengerCapacity']?.toString() ?? '';
      _purchaseValueCtrl.text = d['purchaseValue']?.toString() ?? '';
      _brandId = d['brandId'] as String?;
      _vehicleTypeId = d['vehicleTypeId'] as String?;
      _fuelTypeId = d['fuelTypeId'] as String?;
      _branchId = d['branchId'] as String?;
      _purchaseDate = d['purchaseDate'] != null ? DateTime.tryParse(d['purchaseDate'] as String) : null;
      setState(() {});
    } catch (e) {
      final msg = _extractError(e) ?? 'Error al cargar vehículo';
      setState(() => _error = msg);
    }
  }

  String? _extractError(dynamic e) {
    if (e is DioException) {
      final data = e.response?.data;
      if (data is Map) {
        return (data['detail'] ?? data['message'] ?? data['title']) as String?;
      }
      return e.message;
    }
    return null;
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    if (_brandId == null || _vehicleTypeId == null || _fuelTypeId == null || _branchId == null) {
      setState(() => _error = 'Seleccione todos los campos requeridos (marca, tipo, combustible, sucursal)');
      return;
    }

    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{
        'code': _codeCtrl.text.trim(),
        'plate': _plateCtrl.text.trim(),
        'model': _modelCtrl.text.trim(),
        'year': int.tryParse(_yearCtrl.text.trim()) ?? DateTime.now().year,
        'vin': _vinCtrl.text.trim().isEmpty ? null : _vinCtrl.text.trim(),
        'engineNumber': _engineCtrl.text.trim().isEmpty ? null : _engineCtrl.text.trim(),
        'chassisNumber': _chassisCtrl.text.trim().isEmpty ? null : _chassisCtrl.text.trim(),
        'color': _colorCtrl.text.trim().isEmpty ? null : _colorCtrl.text.trim(),
        'currentKm': double.tryParse(_kmCtrl.text.trim()) ?? 0,
        'loadCapacityKg': double.tryParse(_loadKgCtrl.text.trim()) ?? 0,
        'loadCapacityM3': double.tryParse(_loadM3Ctrl.text.trim()),
        'passengerCapacity': int.tryParse(_passengerCtrl.text.trim()),
        'purchaseValue': double.tryParse(_purchaseValueCtrl.text.trim()),
        'brandId': _brandId,
        'vehicleTypeId': _vehicleTypeId,
        'fuelTypeId': _fuelTypeId,
        'branchId': _branchId,
        'purchaseDate': _purchaseDate?.toIso8601String(),
      };
      if (_isEditing) {
        await dio.put('fleet/vehicles/${widget.vehicleId}', data: body);
      } else {
        await dio.post('fleet/vehicles', data: body);
      }
      if (mounted) context.pop(true);
    } catch (e) {
      final msg = e is DioException
          ? 'Error: ${e.response?.data?['detail'] ?? e.response?.data?['message'] ?? e.message}'
          : 'Error al guardar';
      setState(() => _error = msg);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _codeCtrl.dispose();
    _plateCtrl.dispose();
    _modelCtrl.dispose();
    _yearCtrl.dispose();
    _vinCtrl.dispose();
    _engineCtrl.dispose();
    _chassisCtrl.dispose();
    _colorCtrl.dispose();
    _kmCtrl.dispose();
    _loadKgCtrl.dispose();
    _loadM3Ctrl.dispose();
    _passengerCtrl.dispose();
    _purchaseValueCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar vehículo' : 'Nuevo vehículo')),
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
              _buildSection('Datos Generales', Icons.info_outline, [
                ZTextField(controller: _codeCtrl, label: 'Código interno', prefix: const Icon(Icons.tag), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _plateCtrl, label: 'Placa', prefix: const Icon(Icons.dialpad), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZAsyncRenderer(
                  value: ref.watch(vehicleBrandListProvider),
                  builder: (items) => ZDropdownFormField<String>(
                    value: _brandId,
                    label: 'Marca',
                    prefixIcon: Icons.directions_car_outlined,
                    items: items.map((e) => DropdownMenuItem(value: e.id, child: Text(e.name))).toList(),
                    onChanged: (v) => setState(() => _brandId = v),
                    validator: (v) => v == null ? 'Requerido' : null,
                  ),
                ),
                const SizedBox(height: 12),
                ZTextField(controller: _modelCtrl, label: 'Modelo', prefix: const Icon(Icons.model_training), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _yearCtrl, label: 'Año', prefix: const Icon(Icons.calendar_today), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZAsyncRenderer(
                  value: ref.watch(vehicleTypeListProvider),
                  builder: (items) => ZDropdownFormField<String>(
                    value: _vehicleTypeId,
                    label: 'Tipo de vehículo',
                    prefixIcon: Icons.category_outlined,
                    items: items.map((e) => DropdownMenuItem(value: e.id, child: Text(e.name))).toList(),
                    onChanged: (v) => setState(() => _vehicleTypeId = v),
                    validator: (v) => v == null ? 'Requerido' : null,
                  ),
                ),
                const SizedBox(height: 12),
                ZAsyncRenderer(
                  value: ref.watch(fuelTypeListProvider),
                  builder: (items) => ZDropdownFormField<String>(
                    value: _fuelTypeId,
                    label: 'Tipo de combustible',
                    prefixIcon: Icons.local_gas_station_outlined,
                    items: items.map((e) => DropdownMenuItem(value: e.id, child: Text(e.name))).toList(),
                    onChanged: (v) => setState(() => _fuelTypeId = v),
                    validator: (v) => v == null ? 'Requerido' : null,
                  ),
                ),
                const SizedBox(height: 12),
                ZTextField(controller: _vinCtrl, label: 'VIN', prefix: const Icon(Icons.qr_code)),
                const SizedBox(height: 12),
                ZTextField(controller: _colorCtrl, label: 'Color', prefix: const Icon(Icons.palette_outlined)),
              ]),
              const SizedBox(height: 20),
              _buildSection('Datos Operativos', Icons.settings_outlined, [
                ZTextField(controller: _kmCtrl, label: 'Kilometraje actual', prefix: const Icon(Icons.speed), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _loadKgCtrl, label: 'Capacidad de carga (kg)', prefix: const Icon(Icons.monitor_weight_outlined), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _loadM3Ctrl, label: 'Capacidad volumétrica (m³)', prefix: const Icon(Icons.view_in_ar_outlined), keyboardType: TextInputType.number),
              ]),
              const SizedBox(height: 20),
              _buildSection('Sucursal y Compra', Icons.business_outlined, [
                ZAsyncRenderer(
                  value: ref.watch(headerBranchListProvider),
                  builder: (items) => ZDropdownFormField<String>(
                    value: _branchId,
                    label: 'Sucursal',
                    prefixIcon: Icons.business_outlined,
                    items: items.map((e) => DropdownMenuItem(value: e['id'] as String, child: Text(e['name'] as String? ?? ''))).toList(),
                    onChanged: (v) => setState(() => _branchId = v),
                    validator: (v) => v == null ? 'Requerido' : null,
                  ),
                ),
                const SizedBox(height: 12),
                InkWell(
                  onTap: () async {
                    final date = await showDatePicker(
                      context: context,
                      initialDate: _purchaseDate ?? DateTime.now(),
                      firstDate: DateTime(2000),
                      lastDate: DateTime.now().add(const Duration(days: 365)),
                    );
                    if (date != null) setState(() => _purchaseDate = date);
                  },
                  child: InputDecorator(
                    decoration: InputDecoration(
                      labelText: 'Fecha de compra',
                      prefixIcon: const Icon(Icons.date_range_outlined),
                      suffixIcon: Icon(_purchaseDate != null ? Icons.check_circle : Icons.date_range, color: _purchaseDate != null ? Colors.green : null),
                    ),
                    child: Text(_purchaseDate != null ? '${_purchaseDate!.day}/${_purchaseDate!.month}/${_purchaseDate!.year}' : 'Seleccionar fecha'),
                  ),
                ),
                const SizedBox(height: 12),
                ZTextField(controller: _purchaseValueCtrl, label: 'Valor de compra', prefix: const Icon(Icons.attach_money), keyboardType: TextInputType.number),
              ]),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear vehículo',
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
