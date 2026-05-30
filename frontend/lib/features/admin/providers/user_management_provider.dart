import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final usersProvider = FutureProvider.autoDispose<List<dynamic>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final response = await dio.get('/users');
  return (response.data as List).cast<dynamic>();
});

final rolesProvider = FutureProvider.autoDispose<List<dynamic>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final response = await dio.get('/users/roles');
  return (response.data as List).cast<dynamic>();
});
