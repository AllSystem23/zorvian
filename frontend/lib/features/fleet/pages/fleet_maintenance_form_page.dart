import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:share_plus/share_plus.dart';
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

  /// Genera un PDF vía el motor documental desde `endpoint`, lo comparte y
  /// muestra confirmación con `docLabel` (usado para OT y Acta de Taller).
  Future<void> _generateAndShare(String endpoint, String fileName, String docLabel) async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.post(endpoint);
      final documentId = r.data['generatedDocumentId'] as String?;
      if (documentId == null) throw Exception('Respuesta sin documento generado');

      final pdf = await dio.get(
        'documents/$documentId/pdf',
        options: Options(responseType: ResponseType.bytes),
      );
      final bytes = pdf.data as List<int>;
      if (!mounted) return;
      await SharePlus.instance.share(
        ShareParams(files: [
          XFile.fromData(
            Uint8List.fromList(bytes),
            mimeType: 'application/pdf',
            name: fileName,
          ),
        ]),
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('$docLabel generado (${bytes.length ~/ 1024} KB)'),
            backgroundColor: Colors.green,
          ),
        );
      }
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error al generar PDF: $e'), backgroundColor: Colors.red),
      );
    }
  }

  /// Genera el PDF de la orden de trabajo vía el motor documental y lo comparte.
  Future<void> _generatePdf() => _generateAndShare(
        'fleet/work-orders/${widget.workOrderId}/pdf/generate',
        'orden_trabajo_${_numberCtrl.text.isNotEmpty ? _numberCtrl.text : widget.workOrderId}.pdf',
        'Orden de trabajo',
      );

  /// Genera el PDF del acta de recepción en taller y lo comparte.
  Future<void> _generateReceptionPdf() => _generateAndShare(
        'fleet/work-orders/${widget.workOrderId}/receipt/pdf/generate',
        'acta_recepcion_taller_${_numberCtrl.text.isNotEmpty ? _numberCtrl.text : widget.workOrderId}.pdf',
        'Acta de recepción',
      );

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
              if (_isEditing) ...[
                const SizedBox(height: 12),
                ZButton(
                  text: 'Generar OT (PDF)',
                  onPressed: _generatePdf,
                  type: ZButtonType.secondary,
                  icon: Icons.picture_as_pdf_outlined,
                ),
                const SizedBox(height: 12),
                ZButton(
                  text: 'Generar Acta Taller (PDF)',
                  onPressed: _generateReceptionPdf,
                  type: ZButtonType.secondary,
                  icon: Icons.assignment_outlined,
                ),
              ],
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
