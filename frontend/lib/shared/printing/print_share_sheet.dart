import 'dart:html' as html;
import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../features/settings/providers/company_settings_provider.dart';
import 'print_utils.dart';
import 'thermal_template.dart';

class PrintShareSheet extends ConsumerWidget {
  final String title;
  final String filename;
  final Future<Uint8List> Function(Map<String, dynamic> company, Map<String, dynamic> settings) buildPdf;
  final String Function(Map<String, dynamic> company) buildThermal;
  final String Function(Map<String, dynamic> company) buildText;
  final Future<Uint8List> Function()? buildCsv;

  const PrintShareSheet({
    super.key,
    required this.title,
    required this.filename,
    required this.buildPdf,
    required this.buildThermal,
    required this.buildText,
    this.buildCsv,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final companyAsync = ref.watch(companyInfoProvider);
    final settingsAsync = ref.watch(companySettingsProvider);
    final loading = companyAsync.isLoading || settingsAsync.isLoading;
    final error = companyAsync.hasError || settingsAsync.hasError;
    final ready = companyAsync.asData?.value != null && settingsAsync.asData?.value != null;

    return Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.of(context).viewInsets.bottom),
      child: SizedBox(
        width: double.infinity,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                  width: 40, height: 4,
                  decoration: BoxDecoration(color: Colors.grey.shade300, borderRadius: BorderRadius.circular(2)),
                ),
              ),
              const SizedBox(height: 12),
              Text('Compartir $title', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
              const SizedBox(height: 4),
              if (loading)
                const Padding(padding: EdgeInsets.all(16), child: Center(child: CircularProgressIndicator(strokeWidth: 2))),
              if (error)
                Padding(
                  padding: const EdgeInsets.all(16),
                  child: Text('Error al cargar datos de la empresa', style: TextStyle(color: theme.colorScheme.error, fontSize: 13)),
                ),
              if (ready) ...[
                _OptionTile(
                  icon: Icons.picture_as_pdf,
                  label: 'Descargar PDF',
                  subtitle: 'Documento profesional para imprimir o enviar',
                  onTap: () async {
                    final company = companyAsync.asData!.value;
                    final settings = settingsAsync.asData!.value;
                    final pdf = await buildPdf(company, settings);
                    downloadPdf(pdf, '$filename.pdf');
                    if (context.mounted) Navigator.pop(context);
                    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('PDF descargado: $filename.pdf')));
                  },
                ),
                const Divider(height: 1),
                _OptionTile(
                  icon: Icons.print,
                  label: 'Imprimir (A4 / Oficio)',
                  subtitle: 'Abrir diálogo de impresión del navegador',
                  onTap: () async {
                    final company = companyAsync.asData!.value;
                    final settings = settingsAsync.asData!.value;
                    final pdf = await buildPdf(company, settings);
                    final blob = html.Blob([pdf], 'application/pdf');
                    final url = html.Url.createObjectUrl(blob);
                    html.window.open(url, '_blank');
                    html.Url.revokeObjectUrl(url);
                    if (context.mounted) Navigator.pop(context);
                  },
                ),
                const Divider(height: 1),
                _OptionTile(
                  icon: Icons.receipt_long,
                  label: 'Ticket Térmico (80mm)',
                  subtitle: 'Formato compacto para impresora térmica',
                  onTap: () {
                    final company = companyAsync.asData!.value;
                    final htmlContent = buildThermal(company);
                    printThermal(htmlContent);
                    if (context.mounted) Navigator.pop(context);
                  },
                ),
                const Divider(height: 1),
                _OptionTile(
                  icon: Icons.share,
                  label: 'WhatsApp',
                  subtitle: 'Compartir resumen por WhatsApp',
                  onTap: () async {
                    final company = companyAsync.asData!.value;
                    final text = buildText(company);
                    await shareWhatsApp(text);
                    if (context.mounted) Navigator.pop(context);
                  },
                ),
                const Divider(height: 1),
                _OptionTile(
                  icon: Icons.email_outlined,
                  label: 'Correo Electrónico',
                  subtitle: 'Enviar resumen por email',
                  onTap: () async {
                    final company = companyAsync.asData!.value;
                    final text = buildText(company);
                    final name = company['legalName'] as String? ?? company['name'] as String? ?? '';
                    await shareEmail(subject: '$title - $name', body: text);
                    if (context.mounted) Navigator.pop(context);
                  },
                ),
                if (buildCsv != null) ...[
                  const Divider(height: 1),
                  _OptionTile(
                    icon: Icons.table_chart,
                    label: 'Exportar CSV',
                    subtitle: 'Datos en formato de hoja de cálculo',
                    onTap: () async {
                      final bytes = await buildCsv!();
                      downloadCsv(bytes, '$filename.csv');
                      if (context.mounted) Navigator.pop(context);
                      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('CSV descargado: $filename.csv')));
                    },
                  ),
                ],
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _OptionTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final String subtitle;
  final VoidCallback onTap;

  const _OptionTile({required this.icon, required this.label, required this.subtitle, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 12),
        child: Row(
          children: [
            Icon(icon, size: 24, color: theme.colorScheme.primary),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(label, style: const TextStyle(fontWeight: FontWeight.w500)),
                  Text(subtitle, style: TextStyle(fontSize: 12, color: Colors.grey.shade600)),
                ],
              ),
            ),
            Icon(Icons.chevron_right, size: 20, color: Colors.grey.shade400),
          ],
        ),
      ),
    );
  }
}
