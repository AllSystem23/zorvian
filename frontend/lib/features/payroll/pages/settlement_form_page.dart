import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:universal_html/html.dart' as html;
import '../../../shared/ds/ds.dart';
import '../services/settlement_service.dart';

class SettlementFormPage extends ConsumerStatefulWidget {
  final String employeeId;
  final String companyId;

  const SettlementFormPage({super.key, required this.employeeId, required this.companyId});

  @override
  ConsumerState<SettlementFormPage> createState() => _SettlementFormPageState();
}

class _SettlementFormPageState extends ConsumerState<SettlementFormPage> {
  final _formKey = GlobalKey<FormState>();
  String _terminationType = 'Resignation';
  DateTime _lastDay = DateTime.now();
  final _baseSalaryController = TextEditingController();
  final _vacationsController = TextEditingController();
  final _aguinaldoController = TextEditingController();
  final _indemnizationController = TextEditingController();
  bool _isGenerating = false;

  @override
  void dispose() {
    _baseSalaryController.dispose();
    _vacationsController.dispose();
    _aguinaldoController.dispose();
    _indemnizationController.dispose();
    super.dispose();
  }

  Future<void> _generatePdf() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isGenerating = true);

    try {
      final pdfBytes = await ref.read(settlementServiceProvider).generateSettlementPdf(
        employeeId: widget.employeeId,
        companyId: widget.companyId,
        terminationType: _terminationType,
        lastDay: _lastDay,
        baseSalary: double.parse(_baseSalaryController.text),
        accruedVacations: double.parse(_vacationsController.text),
        accruedAguinaldo: double.parse(_aguinaldoController.text),
        indemnization: double.parse(_indemnizationController.text),
      );

      final blob = html.Blob([pdfBytes], 'application/pdf');
      final url = html.Url.createObjectUrlFromBlob(blob);
      html.AnchorElement(href: url)
        ..setAttribute('download', 'Liquidacion_${widget.employeeId}.pdf')
        ..click();
      html.Url.revokeObjectUrl(url);

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Documento generado exitosamente')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error al generar el documento: $e'), backgroundColor: ZColors.danger),
        );
      }
    } finally {
      if (mounted) setState(() => _isGenerating = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Generar Liquidación')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('Información de Liquidación', style: ZTypography.headlineSmall),
              const SizedBox(height: ZSpacing.md),
              DropdownButtonFormField<String>(
                value: _terminationType,
                decoration: const InputDecoration(labelText: 'Tipo de Terminación'),
                items: const [
                  DropdownMenuItem(value: 'Resignation', child: Text('Renuncia Voluntaria')),
                  DropdownMenuItem(value: 'DismissalWithCause', child: Text('Despido con Causa')),
                  DropdownMenuItem(value: 'DismissalWithoutCause', child: Text('Despido Injustificado')),
                ],
                onChanged: (val) => setState(() => _terminationType = val!),
              ),
              const SizedBox(height: ZSpacing.md),
              ListTile(
                title: const Text('Último Día Laborado'),
                subtitle: Text(DateFormat('dd/MM/yyyy').format(_lastDay)),
                trailing: const Icon(Icons.calendar_today),
                onTap: () async {
                  final date = await showDatePicker(
                    context: context,
                    initialDate: _lastDay,
                    firstDate: DateTime(2000),
                    lastDate: DateTime.now().add(const Duration(days: 365)),
                  );
                  if (date != null) setState(() => _lastDay = date);
                },
              ),
              const SizedBox(height: ZSpacing.md),
              ZTextField(
                controller: _baseSalaryController,
                label: 'Salario Base (Últimos 6 meses avg)',
                keyboardType: TextInputType.number,
                validator: (v) => v!.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: ZSpacing.md),
              ZTextField(
                controller: _vacationsController,
                label: 'Monto Vacaciones Acumuladas',
                keyboardType: TextInputType.number,
                validator: (v) => v!.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: ZSpacing.md),
              ZTextField(
                controller: _aguinaldoController,
                label: 'Monto Aguinaldo Proporcional',
                keyboardType: TextInputType.number,
                validator: (v) => v!.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: ZSpacing.md),
              ZTextField(
                controller: _indemnizationController,
                label: 'Monto Indemnización (Antigüedad)',
                keyboardType: TextInputType.number,
                validator: (v) => v!.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: ZSpacing.lg),
              ZButton(
                text: 'Generar Finiquito PDF',
                onPressed: _generatePdf,
                isLoading: _isGenerating,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
