import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/services/file_saver_stub.dart' if (dart.library.html) '../../../core/services/file_saver_web.dart';

class CustomReportResultPage extends ConsumerWidget {
  final String reportName;
  final String module;
  final List<dynamic> fields;
  final List<String> columns;
  final List<Map<String, dynamic>> rows;

  const CustomReportResultPage({
    super.key,
    required this.reportName,
    required this.module,
    required this.fields,
    required this.columns,
    required this.rows,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      body: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            mainAxisSize: MainAxisSize.min,
            children: [
              IconButton(
                icon: const Icon(Icons.file_download),
                tooltip: 'Exportar Excel',
                onPressed: () => _exportExcel(ref, context),
              ),
              IconButton(
                icon: const Icon(Icons.picture_as_pdf),
                tooltip: 'Exportar PDF',
                onPressed: () => _exportPdf(ref, context),
              ),
            ],
          ),
          Expanded(child: rows.isEmpty
          ? const Center(child: Text('Sin resultados'))
          : SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: SingleChildScrollView(
                child: DataTable(
                  headingRowColor: WidgetStateProperty.all(Theme.of(context).colorScheme.primaryContainer.withAlpha(80)),
                  columnSpacing: 16,
                  columns: columns.map((c) => DataColumn(label: Text(c, style: const TextStyle(fontWeight: FontWeight.bold)))).toList(),
                  rows: rows.map((row) => DataRow(
                    cells: columns.map((c) => DataCell(Text(_formatValue(row[c])))).toList(),
                  )).toList(),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  String _formatValue(dynamic val) {
    if (val == null) return '';
    if (val is double) return val.toStringAsFixed(2);
    if (val is int) return val.toString();
    return val.toString();
  }

  Future<void> _exportExcel(WidgetRef ref, BuildContext context) async {
    await _export(ref, context, 'excel', 'xlsx');
  }

  Future<void> _exportPdf(WidgetRef ref, BuildContext context) async {
    await _export(ref, context, 'pdf', 'pdf');
  }

  Future<void> _export(WidgetRef ref, BuildContext context, String format, String ext) async {
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.post(
        'custom-reports/export-adhoc/$format',
        queryParameters: {'module': module, 'title': reportName},
        data: {
          'name': reportName,
          'module': module,
          'fields': fields,
          'filters': [],
        },
        options: Options(responseType: ResponseType.bytes),
      );
      final data = response.data as List<int>;
      await saveFile(data, '$reportName.$ext');
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('$reportName.$ext exportado')),
        );
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red),
        );
      }
    }
  }
}
