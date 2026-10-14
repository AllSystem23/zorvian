import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class BranchListNotifier extends AsyncNotifier<List<Map<String, dynamic>>> {
  @override
  Future<List<Map<String, dynamic>>> build() async {
    final dio = ref.read(dioClientProvider);
    final response = await dio.get('branches');
    return (response.data as List).cast<Map<String, dynamic>>();
  }

  Future<void> refresh() async {
    state = const AsyncValue.loading();
    state = await AsyncValue.guard(() async {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('branches');
      return (response.data as List).cast<Map<String, dynamic>>();
    });
  }
}

final branchListProvider = AsyncNotifierProvider.autoDispose<BranchListNotifier, List<Map<String, dynamic>>>(BranchListNotifier.new);
