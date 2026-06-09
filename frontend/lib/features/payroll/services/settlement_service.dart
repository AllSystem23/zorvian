import 'dart:convert';
import 'dart:typed_data';
import 'package:http/http.dart' as http;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/constants/api_constants.dart';
import '../../../auth/auth_provider.dart';

final settlementServiceProvider = Provider<SettlementService>((ref) {
  return SettlementService(ref.read(authProvider.notifier));
});

class SettlementService {
  final AuthNotifier _auth;

  SettlementService(this._auth);

  Future<Uint8List> generateSettlementPdf(Map<String, dynamic> requestData) async {
    final token = await _auth.getToken();
    final url = Uri.parse('${ApiConstants.baseUrl}/payroll/settlement/generate-pdf');
    
    final response = await http.post(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode(requestData),
    );

    if (response.statusCode == 200) {
      return response.bodyBytes;
    } else {
      throw Exception('Failed to generate PDF: ${response.statusCode}');
    }
  }
}
