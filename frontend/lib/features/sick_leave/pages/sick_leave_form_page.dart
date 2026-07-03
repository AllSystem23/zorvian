import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/sick_leave_provider.dart';

class SickLeaveFormPage extends ConsumerStatefulWidget {
  final String employeeId;
  const SickLeaveFormPage({super.key, required this.employeeId});

  @override
  ConsumerState<SickLeaveFormPage> createState() => _SickLeaveFormPageState();
}

class _SickLeaveFormPageState extends ConsumerState<SickLeaveFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _diagnosisCtrl = TextEditingController();
  DateTime _startDate = DateTime.now();
  DateTime _endDate = DateTime.now().add(const Duration(days: 7));

  @override
  void dispose() {
    _diagnosisCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    final success = await ref.read(sickLeaveProvider.notifier).create({
      'employeeId': widget.employeeId,
      'startDate': _startDate.toIso8601String().substring(0, 10),
      'endDate': _endDate.toIso8601String().substring(0, 10),
      'diagnosisCode': _diagnosisCtrl.text,
    });
    if (success && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Incapacidad registrada')));
      context.pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Nueva Incapacidad')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: ZCard(
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Datos de la Incapacidad', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                const SizedBox(height: 24),
                ZTextField(controller: _diagnosisCtrl, label: 'Código de Diagnóstico'),
                const SizedBox(height: 16),
                Row(
                  children: [
                    Expanded(
                      child: InkWell(
                        onTap: () async {
                          final date = await showDatePicker(
                            context: context,
                            initialDate: _startDate,
                            firstDate: DateTime(2020),
                            lastDate: DateTime.now().add(const Duration(days: 365)),
                          );
                          if (date != null) setState(() => _startDate = date);
                        },
                        child: InputDecorator(
                          decoration: const InputDecoration(labelText: 'Fecha de Inicio'),
                          child: Text('${_startDate.day}/${_startDate.month}/${_startDate.year}'),
                        ),
                      ),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: InkWell(
                        onTap: () async {
                          final date = await showDatePicker(
                            context: context,
                            initialDate: _endDate,
                            firstDate: DateTime(2020),
                            lastDate: DateTime.now().add(const Duration(days: 365)),
                          );
                          if (date != null) setState(() => _endDate = date);
                        },
                        child: InputDecorator(
                          decoration: const InputDecoration(labelText: 'Fecha de Fin'),
                          child: Text('${_endDate.day}/${_endDate.month}/${_endDate.year}'),
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 24),
                ZButton(text: 'Guardar', onPressed: _submit, icon: Icons.save),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
