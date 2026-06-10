import 'dart:typed_data';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:dio/dio.dart';
import '../../../auth/auth_provider.dart';

class SettlementService {
  final Ref _ref;

  SettlementService(this._ref);

  Future<Uint8List> generateSettlementPdf({
    required String employeeId,
    required String companyId,
    required String terminationType,
    required DateTime lastDay,
    required double baseSalary,
    required double accruedVacations,
    required double accruedAguinaldo,
    required double indemnization,
  }) async {
    final storage = _ref.read(secureStorageProvider);
    final token = await storage.getAccessToken();
    
    // Base URL is managed by DioClient, but here we might need it for a direct call or use dio instance
    final dio = Dio(); // Using a clean Dio for blob download or configured one
    
    // Getting base URL from environment or hardcoded as in DioClient
    const baseUrl = String.fromEnvironment('API_URL', defaultValue: 'https://nexora-9yal.onrender.com/zorvian/v1/');
    final url = '${baseUrl}payroll/settlement/generate-pdf';

    final response = await dio.post(
      url,
      data: {
        'employeeId': employeeId,
        'companyId': companyId,
        'terminationType': terminationType,
        'lastDay': lastDay.toIso8601String(),
        'baseSalary': baseSalary,
        'accruedVacations': accruedVacations,
        'accruedAguinaldo': accruedAguinaldo,
        'indemnization': indemnization,
      },
      options: Options(
        responseType: ResponseType.bytes,
        headers: {
          if (token != null) 'Authorization': 'Bearer $token',
        },
      ),
    );

    if (response.statusCode == 200) {
      return Uint8List.fromList(response.data);
    } else {
      throw Exception('Error generating PDF: ${response.statusCode}');
    }
  }
}

final settlementServiceProvider = Provider((ref) => SettlementService(ref));
