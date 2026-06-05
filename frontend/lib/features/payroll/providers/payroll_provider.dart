import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/network/dio_client.dart';
import '../../../core/services/file_saver_stub.dart' if (dart.library.html) '../../../core/services/file_saver_web.dart';

class PayrollService {
  final DioClient _dio;
  PayrollService(this._dio);

  Future<List<dynamic>> getDeductionTypes() async {
    final res = await _dio.get('payroll/deduction-types');
    return res.data as List<dynamic>;
  }

  Future<Map<String, dynamic>> createDeductionType(Map<String, dynamic> data) async {
    final res = await _dio.post('payroll/deduction-types', data: data);
    return res.data as Map<String, dynamic>;
  }

  Future<void> updateDeductionType(String id, Map<String, dynamic> data) async {
    await _dio.put('payroll/deduction-types/$id', data: data);
  }

  Future<void> deleteDeductionType(String id) async {
    await _dio.delete('payroll/deduction-types/$id');
  }

  Future<List<dynamic>> getSalaries(String? employeeId) async {
    final params = <String, dynamic>{};
    if (employeeId != null) params['employeeId'] = employeeId;
    final res = await _dio.get('payroll/salaries', params: params);
    return res.data as List<dynamic>;
  }

  Future<Map<String, dynamic>> createSalary(Map<String, dynamic> data) async {
    final res = await _dio.post('payroll/salaries', data: data);
    return res.data as Map<String, dynamic>;
  }

  Future<void> deactivateSalary(String id) async {
    await _dio.delete('payroll/salaries/$id');
  }

  Future<List<dynamic>> getPeriods(int? year) async {
    final params = <String, dynamic>{};
    if (year != null) params['year'] = year;
    final res = await _dio.get('payroll/periods', params: params);
    return res.data as List<dynamic>;
  }

  Future<Map<String, dynamic>> createPeriod(Map<String, dynamic> data) async {
    final res = await _dio.post('payroll/periods', data: data);
    return res.data as Map<String, dynamic>;
  }

  Future<List<dynamic>> getRuns(String? periodId) async {
    final params = <String, dynamic>{};
    if (periodId != null) params['periodId'] = periodId;
    final res = await _dio.get('payroll/runs', params: params);
    return res.data as List<dynamic>;
  }

  Future<Map<String, dynamic>> getRunById(String id) async {
    final res = await _dio.get('payroll/runs/$id');
    return res.data as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> generateRun(Map<String, dynamic> data) async {
    final res = await _dio.post('payroll/runs/generate', data: data);
    return res.data as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> approveRun(String id) async {
    final res = await _dio.post('payroll/runs/$id/approve');
    return res.data as Map<String, dynamic>;
  }

  Future<void> deleteRun(String id) async {
    await _dio.delete('payroll/runs/$id');
  }

  Future<Map<String, dynamic>> cancelRun(String id) async {
    final res = await _dio.post('payroll/runs/$id/cancel');
    return res.data as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> markAsPaid(String id) async {
    final res = await _dio.post('payroll/runs/$id/mark-paid');
    return res.data as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> updateDetail(String detailId, Map<String, dynamic> data) async {
    final res = await _dio.put('payroll/details/$detailId', data: data);
    return res.data as Map<String, dynamic>;
  }

  Future<void> exportAchFile(String id) async {
    final response = await _dio.get(
      'payroll/runs/$id/export-ach',
      options: Options(responseType: ResponseType.bytes),
    );
    final data = response.data as List<int>;
    await saveFile(data, 'nomina_export.csv');
  }
}

final payrollServiceProvider = Provider<PayrollService>((ref) {
  final dio = ref.read(dioClientProvider);
  return PayrollService(dio);
});
