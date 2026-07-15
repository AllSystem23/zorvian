import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../shared/ds/ds.dart';

final class FleetMaintenanceFormPage extends ConsumerStatefulWidget {
  final String? workOrderId;
  const FleetMaintenanceFormPage({super.key, this.workOrderId});

  @override
  ConsumerState<FleetMaintenanceFormPage> createState() => _FleetMaintenanceFormPageState();
}

final class _FleetMaintenanceFormPageState extends ConsumerState<FleetMaintenanceFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _numberCtrl = TextEditingController();
  final _vehicleCtrl = TextEditingController();
  final _driverCtrl = TextEditingController();
  final _problemCtrl = TextEditingController();
  final _diagnosisCtrl = TextEditingController();
  final _mechanicCtrl = TextEditingController();
  final _costEstCtrl = TextEditingController();

  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.workOrderId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final d = await dio.get('fleet/work-orders/${widget.workOrderId}');
      final dd = d.data;
      _numberCtrl.text = dd['number'] ?? '';
      _vehicleCtrl.text = dd['vehiclePlate'] ?? '';
      _driverCtrl.text = dd['driverName'] ?? '';
      _problemCtrl.text = dd['problemDescription'] ?? '';
      _diagnosisCtrl.text = dd['diagnosis'] ?? '';
      _mechanicCtrl.text = dd['mechanicResponsible'] ?? '';
      _costEstCtrl.text = dd['costEst']?.toString() ?? '';
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
        'number': _numberCtrl.text.trim(),
        'vehicleId': _vehicleCtrl.text.trim(),
        'driverId': _driverCtrl.text.trim().isEmpty ? null : _driverCtrl.text.trim(),
        'reportDateTime': DateTime.now().toUtc().toIso8601String(),
        'problemDescription': _problemCtrl.text.trim().isEmpty ? null : _problemCtrl.text.trim(),
        'diagnosis': _diagnosisCtrl.text.trim().isEmpty ? null : _diagnosisCtrl.text.trim(),
        'priority': 'Medium',
        'mechanicResponsible': _mechanicCtrl.text.trim().isEmpty ? null : _mechanicCtrl.text.trim(),
        'costEst': double.tryParse(_costEstCtrl.text.trim()) ?? 0,
        'costLabor': 0,
        'costParts': 0,
        'costTotal': 0,
      };
      if (_isEditing) {
        await dio.put('fleet/work-orders/${widget.workOrderId}', data: body);
      } else {
        await dio.post('fleet/work-orders', data: body);
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
    _numberCtrl.dispose();
    _vehicleCtrl.dispose();
    _driverCtrl.dispose();
    _problemCtrl.dispose();
    _diagnosisCtrl.dispose();
    _mechanicCtrl.dispose();
    _costEstCtrl.dispose();
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
              _buildSection('Identificación', Icons.build_outlined, [
                ZTextField(controller: _numberCtrl, label: 'Número OT', prefix: const Icon(Icons.tag), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _vehicleCtrl, label: 'Vehículo ID', prefix: const Icon(Icons.time_to_leave_outlined), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _driverCtrl, label: 'Conductor ID', prefix: const Icon(Icons.person_outline)),
              ]),
              const SizedBox(height: 20),
              _buildSection('Diagnóstico', Icons.medical_services_outlined, [
                ZTextField(controller: _problemCtrl, label: 'Descripción del problema', prefix: const Icon(Icons.report_problem_outlined), maxLines: 3),
                const SizedBox(height: 12),
                ZTextField(controller: _diagnosisCtrl, label: 'Diagnóstico', prefix: const Icon(Icons.search_outlined), maxLines: 3),
              ]),
              const SizedBox(height: 20),
              _buildSection('Taller y Costos', Icons.build_circle_outlined, [
                ZTextField(controller: _mechanicCtrl, label: 'Mecánico responsable', prefix: const Icon(Icons.engineering_outlined)),
                const SizedBox(height: 12),
                ZTextField(controller: _costEstCtrl, label: 'Costo estimado', prefix: const Icon(Icons.attach_money_outlined), keyboardType: TextInputType.number),
              ]),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear OT',
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
