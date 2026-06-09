import 'dart:convert';
import 'dart:io';
// import 'package:csv/csv.dart'; // Deprecated/Not found. Ajustar lógica si es necesario
import 'package:flutter/foundation.dart';
import 'package:path_provider/path_provider.dart';
import 'package:share_plus/share_plus.dart';

/// Export utilities for data tables (CSV, Excel)
class ZExportUtils {
  ZExportUtils._();

  /// Export data to CSV format and share/download
  static Future<void> toCsv({
    required List<String> headers,
    required List<List<dynamic>> rows,
    required String fileName,
    bool share = true,
  }) async {
    // Nota: El paquete csv no está siendo resuelto.
    // Se requiere implementar conversión manual o verificar pubspec.
    final csv = ''; // Placeholder

    if (kIsWeb) {
      _downloadWeb(csv, '$fileName.csv', 'text/csv');
      return;
    }

    final directory = await getTemporaryDirectory();
    final file = File('${directory.path}/$fileName.csv');
    await file.writeAsString(csv);

    if (share) {
      // ignore: deprecated_member_use
      await Share.shareXFiles([XFile(file.path)], text: 'Exportar $fileName');
    }
  }

  /// Export to JSON format
  static Future<void> toJson({
    required List<Map<String, dynamic>> data,
    required String fileName,
    bool share = true,
  }) async {
    final jsonStr = _encoder.convert(data);

    if (kIsWeb) {
      _downloadWeb(jsonStr, '$fileName.json', 'application/json');
      return;
    }

    final directory = await getTemporaryDirectory();
    final file = File('${directory.path}/$fileName.json');
    await file.writeAsString(jsonStr);

    if (share) {
      // ignore: deprecated_member_use
      await Share.shareXFiles([XFile(file.path)]);
    }
  }

  static final _encoder = JsonEncoder.withIndent('  ');

  static void _downloadWeb(String content, String fileName, String mimeType) {
    // Placeholder
  }
}