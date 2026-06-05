import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';

final branchListProvider = FutureProvider.autoDispose<List<Map<String, dynamic>>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final response = await dio.get('branches');
  return (response.data as List).cast<Map<String, dynamic>>();
});

class BranchListPage extends ConsumerWidget {
  const BranchListPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final branchesAsync = ref.watch(branchListProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Sucursales'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => ref.invalidate(branchListProvider),
          ),
        ],
      ),
      body: branchesAsync.when(
        data: (branches) => branches.isEmpty
            ? const Center(child: Text('No hay sucursales configuradas'))
            : RefreshIndicator(
                onRefresh: () async => ref.invalidate(branchListProvider),
                child: ListView.builder(
                  itemCount: branches.length,
                  itemBuilder: (_, i) {
                    final b = branches[i];
                    return Card(
                      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                      child: ListTile(
                        leading: CircleAvatar(
                          backgroundColor: b['isActive'] == true
                              ? Colors.green.withValues(alpha: 0.15)
                              : Colors.grey.withValues(alpha: 0.15),
                          child: Icon(
                            Icons.store,
                            color: b['isActive'] == true ? Colors.green : Colors.grey,
                          ),
                        ),
                        title: Text(b['name'] ?? '', style: const TextStyle(fontWeight: FontWeight.w600)),
                        subtitle: Text([
                          if (b['code'] != null) b['code'],
                          if (b['phone'] != null) b['phone'],
                          if (b['address'] != null) b['address'],
                        ].join(' · ')),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            if (b['isActive'] != true)
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                decoration: BoxDecoration(
                                  color: Colors.grey.withValues(alpha: 0.15),
                                  borderRadius: BorderRadius.circular(12),
                                ),
                                child: const Text('Inactiva', style: TextStyle(fontSize: 12, color: Colors.grey)),
                              ),
                            const SizedBox(width: 4),
                            IconButton(
                              icon: const Icon(Icons.edit, size: 20),
                              onPressed: () => context.push('/branches/${b['id']}'),
                            ),
                          ],
                        ),
                        onTap: () => context.push('/branches/${b['id']}'),
                      ),
                    );
                  },
                ),
              ),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Error: $e')),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final result = await context.push<bool>('/branches/new');
          if (result == true) ref.invalidate(branchListProvider);
        },
        child: const Icon(Icons.add),
      ),
    );
  }
}
