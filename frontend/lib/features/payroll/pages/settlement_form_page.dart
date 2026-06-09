import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:universal_html/html.dart' as html;
import '../../../shared/ds/components/z_text_field.dart';
import '../../../shared/ds/components/z_select.dart';
import '../../../shared/ds/components/z_button.dart';
import '../services/settlement_service.dart';

class SettlementFormPage extends ConsumerStatefulWidget {
  final Guid employeeId;
  final Guid companyId;

  const SettlementFormPage({super.key, required this.employeeId, required this.companyId});

  @override
  ConsumerState<SettlementFormPage> createState() => _SettlementFormPageState();
}

class _SettlementFormPageState extends ConsumerState<SettlementFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _salaryController = TextEditingController();
  DateTime? _hireDate;
  DateTime? _terminationDate;
  String _terminationType = 'Resignation';

  final List<String> _terminationTypes = ['Resignation', 'UnjustifiedDismissal', 'JustifiedDismissal', 'EndOfContract'];

  Future<void> _selectDate(BuildContext context, bool isHireDate) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: DateTime.now(),
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
    );
    if (picked != null) {
      setState(() {
        if (isHireDate) _hireDate = picked; else _terminationDate = picked;
      });
    }
  }

  Future<void> _generatePdf() async {
    if (!_formKey.currentState!.validate() || _hireDate == null || _terminationDate == null) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Por favor complete todos los campos')));
      return;
    }

    try {
      final requestData = {
        'companyId': widget.companyId.toString(),
        'employeeId': widget.employeeId.toString(),
        'terminationType': _terminationType,
        'hireDate': _hireDate!.toIso8601String(),
        'terminationDate': _terminationDate!.toIso8601String(),
        'salary': double.parse(_salaryController.text),
      };

      final pdfBytes = await ref.read(settlementServiceProvider).generateSettlementPdf(requestData);
      
      // Download
      final blob = html.Blob([pdfBytes], 'application/pdf');
      final url = html.Url.createObjectUrlFromBlob(blob);
      html.AnchorElement(href: url)
        ..setAttribute("download", "Liquidacion_${widget.employeeId}.pdf")
        ..click();
      html.Url.revokeObjectUrl(url);

    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Cálculo de Liquidación')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Form(
          key: _formKey,
          child: Column(
            children: [
              ZTextField(
                label: 'Salario Mensual',
                controller: _salaryController,
                keyboardType: TextInputType.number,
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: 16),
              ListTile(
                title: Text(_hireDate == null ? 'Fecha Contratación' : 'Contratación: ${DateFormat('yyyy-MM-dd').format(_hireDate!)}'),
                trailing: const Icon(Icons.calendar_today),
                onTap: () => _selectDate(context, true),
              ),
              ListTile(
                title: Text(_terminationDate == null ? 'Fecha Salida' : 'Salida: ${DateFormat('yyyy-MM-dd').format(_terminationDate!)}'),
                trailing: const Icon(Icons.calendar_today),
                onTap: () => _selectDate(context, false),
              ),
              const SizedBox(height: 16),
              DropdownButtonFormField<String>(
                value: _terminationType,
                items: _terminationTypes.map((t) => DropdownMenuItem(value: t, child: Text(t))).toList(),
                onChanged: (v) => setState(() => _terminationType = v!),
                decoration: const InputDecoration(labelText: 'Tipo de Terminación'),
              ),
              const SizedBox(height: 20),
              ZButton(
                label: 'Generar Finiquito PDF',
                onPressed: _generatePdf,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
