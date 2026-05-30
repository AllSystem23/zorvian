import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../providers/user_management_provider.dart';

class UserListPage extends ConsumerWidget {
  const UserListPage({super.key});

  Future<void> _assignRole(WidgetRef ref, String userId, String roleId, BuildContext context) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('/users/$userId/role', data: {'roleId': roleId});
      ref.invalidate(usersProvider);
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Rol actualizado')));
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final usersAsync = ref.watch(usersProvider);
    final rolesAsync = ref.watch(rolesProvider);

    final roles = rolesAsync.asData?.value ?? [];

    return Scaffold(
      appBar: AppBar(title: const Text('Usuarios')),
      body: usersAsync.when(
        data: (users) => ListView.builder(
          itemCount: users.length,
          itemBuilder: (_, i) {
            final u = users[i] as Map<String, dynamic>;
            final userId = u['id'] as String;
            final userRoles = (u['roles'] as List?)?.cast<String>() ?? [];
            final roleStr = userRoles.isNotEmpty ? userRoles.join(', ') : 'Sin rol';

            return Card(
              child: ListTile(
                leading: CircleAvatar(
                  backgroundColor: u['isActive'] == true ? Colors.green : Colors.grey,
                  child: Text((u['displayName'] as String? ?? '?')[0].toUpperCase()),
                ),
                title: Text(u['displayName'] ?? ''),
                subtitle: Text('${u['email']}\n$roleStr'),
                trailing: PopupMenuButton(
                  itemBuilder: (_) => roles.map<PopupMenuItem>((r) {
                    final role = r as Map<String, dynamic>;
                    return PopupMenuItem(
                      value: role['id'],
                      child: Text(role['displayName'] ?? ''),
                    );
                  }).toList(),
                  onSelected: (roleId) => _assignRole(ref, userId, roleId as String, context),
                ),
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
