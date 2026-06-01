import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final companySettingsProvider = FutureProvider.autoDispose<Map<String, dynamic>>((ref) async {
  final dio = ref.read(dioClientProvider);
  print('DEBUG: Fetching company settings...');
  try {
    final response = await dio.get('companies/settings').timeout(const Duration(seconds: 5));
    print('DEBUG: Settings response received. Status: ${response.statusCode}');
    if (response.data is Map) {
      return response.data as Map<String, dynamic>;
    }
    throw Exception('Formato de datos inválido');
  } catch (e) {
    print('DEBUG: Error fetching settings: $e');
    throw Exception('Error cargando configuración: $e');
  }
});

final companyInfoProvider = FutureProvider.autoDispose<Map<String, dynamic>>((ref) async {
  final dio = ref.read(dioClientProvider);
  print('DEBUG: Fetching company info...');
  try {
    final response = await dio.get('companies/current').timeout(const Duration(seconds: 5));
    print('DEBUG: Company info response received. Status: ${response.statusCode}');
    if (response.data is Map) {
      return response.data as Map<String, dynamic>;
    }
    throw Exception('Formato de datos inválido');
  } catch (e) {
    print('DEBUG: Error fetching info: $e');
    throw Exception('Error cargando empresa: $e');
  }
});
