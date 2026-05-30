import 'dart:html' as html;
import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/network/dio_client.dart';

final reportsProvider = Provider<ReportsService>((ref) {
  final dio = ref.read(dioClientProvider);
  return ReportsService(dio);
});

class ReportsService {
  final DioClient _dio;

  ReportsService(this._dio);

  Future<void> downloadReport(String type, {int? year, int? month}) async {
    final response = await _dio.post(
      '/reports/generate',
      data: {
        'reportType': type,
        'year': year,
        'month': month,
      },
      options: Options(responseType: ResponseType.bytes),
    );

    final data = response.data as List<int>;
    final fileName = _getFileName(type, year, month);
    await _saveFile(data, fileName);
  }

  String _getFileName(String type, int? year, int? month) {
    switch (type) {
      case 'vacation':
        return 'reporte_vacaciones_${year ?? DateTime.now().year}.xlsx';
      case 'permission':
        return 'reporte_permisos_${year ?? DateTime.now().year}.xlsx';
      case 'attendance':
        final y = year ?? DateTime.now().year;
        final m = month ?? DateTime.now().month;
        return 'reporte_asistencia_${y}_${m.toString().padLeft(2, '0')}.xlsx';
      case 'balance':
        return 'reporte_saldos_vacaciones.xlsx';
      default:
        return 'reporte.xlsx';
    }
  }

  Future<void> _saveFile(List<int> data, String fileName) async {
    final blob = html.Blob([data]);
    final url = html.Url.createObjectUrlFromBlob(blob);
    html.AnchorElement(href: url)
      ..setAttribute('download', fileName)
      ..click();
    html.Url.revokeObjectUrl(url);
  }
}
