import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/reports_provider.dart';
import '../../../shared/ds/ds.dart';

class ReportsPage extends ConsumerStatefulWidget {
  const ReportsPage({super.key});

  @override
  ConsumerState<ReportsPage> createState() => _ReportsPageState();
}

class _ReportsPageState extends ConsumerState<ReportsPage> {
  final _yearController = TextEditingController(text: DateTime.now().year.toString());
  final _monthController = TextEditingController(text: DateTime.now().month.toString());
  bool _isLoading = false;

  @override
  void dispose() {
    _yearController.dispose();
    _monthController.dispose();
    super.dispose();
  }

  Future<void> _downloadReport(String type) async {
    setState(() => _isLoading = true);
    try {
      final year = int.tryParse(_yearController.text);
      final month = int.tryParse(_monthController.text);
      await ref.read(reportsProvider).downloadReport(
        type,
        year: year,
        month: type == 'attendance' ? month : null,
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Reporte descargado exitosamente')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error al descargar: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Reportes')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  ZCard(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Filtros', style: Theme.of(context).textTheme.titleMedium),
                        const SizedBox(height: 12),
                        Row(
                          children: [
                            Expanded(
                              child: ZTextField(
                                controller: _yearController,
                                label: 'Año',
                                keyboardType: TextInputType.number,
                              ),
                            ),
                            const SizedBox(width: 16),
                            Expanded(
                              child: ZTextField(
                                controller: _monthController,
                                label: 'Mes (1-12)',
                                keyboardType: TextInputType.number,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  Text('Generar Reporte', style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 12),
                  Expanded(
                    child: GridView.count(
                      crossAxisCount: 2,
                      mainAxisSpacing: 12,
                      crossAxisSpacing: 12,
                      childAspectRatio: 1.5,
                      children: [
                        _ReportCard(
                          icon: Icons.beach_access,
                          label: 'Vacaciones',
                          color: Colors.blue,
                          onTap: () => _downloadReport('vacation'),
                        ),
                        _ReportCard(
                          icon: Icons.event_note,
                          label: 'Permisos',
                          color: Colors.green,
                          onTap: () => _downloadReport('permission'),
                        ),
                        _ReportCard(
                          icon: Icons.access_time,
                          label: 'Asistencia',
                          color: Colors.orange,
                          onTap: () => _downloadReport('attendance'),
                        ),
                        _ReportCard(
                          icon: Icons.account_balance,
                          label: 'Saldos',
                          color: Colors.purple,
                          onTap: () => _downloadReport('balance'),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
    );
  }
}

class _ReportCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final Color color;
  final VoidCallback onTap;

  const _ReportCard({
    required this.icon,
    required this.label,
    required this.color,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return ZCard(child: Semantics(
      label: label,
      button: true,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, size: 40, color: color),
              const SizedBox(height: 8),
              Text(label, style: TextStyle(fontSize: 16, fontWeight: FontWeight.w500, color: color)),
            ],
          ),
        ),
      ),
      ),
    );
  }
}
