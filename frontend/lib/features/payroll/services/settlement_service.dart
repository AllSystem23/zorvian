import 'dart:typed_data';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:dio/dio.dart';
import '../../../auth/auth_provider.dart';

class SettlementService {
  final Ref _ref;

  SettlementService(this._ref);

  Future<Uint8List> generateSettlementPdf({
    required String terminationRecordId,
  }) async {
    final dio = _ref.read(dioClientProvider);

    final response = await dio.post<dynamic>(
      'payroll/settlement/generate-pdf',
      data: {'terminationRecordId': terminationRecordId},
      options: Options(responseType: ResponseType.bytes),
    );

    if (response.statusCode == 200) {
      return Uint8List.fromList(response.data);
    } else {
      throw Exception('Error generating PDF: ${response.statusCode}');
    }
  }
}

final settlementServiceProvider = Provider((ref) => SettlementService(ref));
