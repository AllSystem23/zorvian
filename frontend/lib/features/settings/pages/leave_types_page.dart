import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';

final leaveTypesProvider = FutureProvider.autoDispose<List<dynamic>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final response = await dio.get('leave-types');
  return (response.data as List).cast<dynamic>();
});

class LeaveTypesPage extends ConsumerWidget {
  const LeaveTypesPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final typesAsync = ref.watch(leaveTypesProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Tipos de Permiso'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            onPressed: () async {
              final created = await context.push<bool>('/leave-types/new');
              if (created == true) ref.invalidate(leaveTypesProvider);
            },
          ),
        ],
      ),
      body: typesAsync.when(
        data: (types) => types.isEmpty
            ? const Center(child: Text('No hay tipos de permiso configurados'))
            : ListView.builder(
                itemCount: types.length,
                itemBuilder: (_, i) {
                  final t = types[i] as Map<String, dynamic>;
                  return Card(
                    child: ListTile(
                      leading: Icon(
                        t['requiresAttachment'] == true ? Icons.attachment : Icons.event_note,
                        color: t['isPaid'] == true ? Colors.green : Colors.orange,
                      ),
                      title: Text(t['name'] ?? ''),
                      subtitle: Text('${t['code']} · ${t['maxDaysPerYear'] ?? 'Ilimitado'} días/año'),
                      trailing: Text(t['requiresApproval'] == true ? 'Requiere aprobación' : 'Auto-aprobado',
                          style: const TextStyle(fontSize: 12)),
                    ),
                  );
                },
              ),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Error: $e')),
      ),
    );
  }
}
