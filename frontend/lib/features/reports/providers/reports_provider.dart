import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/network/dio_client.dart';
import '../../../core/services/file_saver_stub.dart' if (dart.library.html) '../../../core/services/file_saver_web.dart';

final reportsProvider = Provider<ReportsService>((ref) {
  final dio = ref.read(dioClientProvider);
  return ReportsService(dio);
});

class ReportsService {
  final DioClient _dio;

  ReportsService(this._dio);

  Future<void> downloadReport(String type, {int? year, int? month, String format = 'xlsx', String? periodId}) async {
    final response = await _dio.post(
      'reports/generate',
      data: {
        'reportType': type,
        'year': year,
        'month': month,
        'format': format,
        'periodId': periodId,
      },
      options: Options(responseType: ResponseType.bytes),
    );

    final data = response.data as List<int>;
    final fileName = _getFileName(type, year, month, format, periodId);
    await saveFile(data, fileName);
  }

  String _getFileName(String type, int? year, int? month, String format, String? periodId) {
    final extension = format == 'pdf' ? 'pdf' : 'xlsx';
    final timestamp = DateTime.now().millisecondsSinceEpoch;
    
    switch (type) {
      case 'cash-flow':
        return 'flujo_efectivo_${periodId ?? timestamp}.$extension';
      case 'equity-changes':
        return 'cambios_patrimonio_${periodId ?? timestamp}.$extension';
      case 'vacation':
        return 'reporte_vacaciones_${year ?? DateTime.now().year}.$extension';
      case 'permission':
        return 'reporte_permisos_${year ?? DateTime.now().year}.$extension';
      case 'attendance':
        final y = year ?? DateTime.now().year;
        final m = month ?? DateTime.now().month;
        return 'reporte_asistencia_${y}_${m.toString().padLeft(2, '0')}.$extension';
      case 'balance':
        return 'reporte_saldos_vacaciones.$extension';
      default:
        return 'reporte_$type.$extension';
    }
  }
}
