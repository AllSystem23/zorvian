import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final companySettingsProvider = FutureProvider.autoDispose<Map<String, dynamic>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final response = await dio.get('/companies/settings');
  return response.data as Map<String, dynamic>;
});

final companyInfoProvider = FutureProvider.autoDispose<Map<String, dynamic>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final response = await dio.get('/companies/current');
  return response.data as Map<String, dynamic>;
});
