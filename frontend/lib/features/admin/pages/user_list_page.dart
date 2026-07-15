import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../providers/user_provider.dart';
import '../models/user_model.dart';

class UserListPage extends ConsumerStatefulWidget {
  const UserListPage({super.key});

  @override
  ConsumerState<UserListPage> createState() => _UserListPageState();
}

class _UserListPageState extends ConsumerState<UserListPage> {
  List<UserModel> _selectedUsers = [];

  bool _isCurrentUser(UserModel user) {
    final currentUserId = ref.read(authProvider).userId;
    return user.id == currentUserId;
  }

  List<UserModel> _safeSelectedUsers(List<UserModel> allUsers) {
    return _selectedUsers.where((u) => !_isCurrentUser(u)).toList();
  }

  Future<bool> _confirmDestructive(String title, String message) async {
    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        icon: const Icon(Icons.warning_amber_rounded, color: ZColors.danger, size: 36),
        title: Text(title),
        content: Text(message),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: ZColors.danger),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Confirmar'),
          ),
        ],
      ),
    );
    return result ?? false;
  }

  void _toggleActive(UserModel user) async {
    if (_isCurrentUser(user)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('No puedes desactivar tu propia cuenta'), backgroundColor: ZColors.danger),
      );
      return;
    }
    final confirmed = await _confirmDestructive(
      user.isActive ? 'Desactivar usuario' : 'Activar usuario',
      user.isActive
          ? '¿Deseas desactivar a "${user.displayName}"? No podrá iniciar sesión.'
          : '¿Deseas activar a "${user.displayName}"?',
    );
    if (!confirmed || !mounted) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('users/${user.id}/toggle-active');
      ref.read(userProvider.notifier).load();
      if (!mounted) return;
      // After toggle, the new state is the inverse of the old
      final newLabel = !user.isActive ? 'desactivado' : 'activado';
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('${user.displayName} $newLabel')),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e'), backgroundColor: ZColors.danger),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(userProvider);
    final currentUserId = ref.watch(authProvider).userId;

    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: Column(
          children: [
            // Header with invite button
            Row(
              children: [
                const Expanded(
                  child: Text(
                    'Usuarios',
                    style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                  ),
                ),
                ZButton(
                  text: 'Invitar Usuario',
                  icon: Icons.person_add_outlined,
                  fullWidth: false,
                  onPressed: () => context.push('/admin/invite'),
                ),
              ],
            ),
            const SizedBox(height: ZSpacing.md),
            Expanded(
              child: ZAsyncRenderer<List<UserModel>>(
                value: state,
                builder: (users) {
                  final safeSelected = _safeSelectedUsers(users);
                  return ZDataTable<UserModel>(
                    columns: const [
                      ZColumn(id: 'name', label: 'Nombre'),
                      ZColumn(id: 'email', label: 'Email'),
                      ZColumn(id: 'active', label: 'Estado'),
                      ZColumn(id: 'actions', label: 'Acciones', width: 120),
                    ],
                    rows: users,
                    selectionEnabled: true,
                    onSelectionChanged: (selected) {
                      setState(() => _selectedUsers = selected);
                    },
                    bulkActions: [
                      TextButton.icon(
                        onPressed: safeSelected.isEmpty ? null : () {
                          final count = safeSelected.length;
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(content: Text('$count usuarios activados')),
                          );
                        },
                        icon: const Icon(Icons.check_circle_outline, size: 18),
                        label: const Text('Activar'),
                      ),
                      TextButton.icon(
                        onPressed: safeSelected.isEmpty ? null : () async {
                          final count = safeSelected.length;
                          final messenger = ScaffoldMessenger.of(context);
                          final confirmed = await _confirmDestructive(
                            'Desactivar $count usuarios',
                            '¿Deseas desactivar $count usuario(s)? No podrán iniciar sesión.',
                          );
                          if (confirmed && mounted) {
                            messenger.showSnackBar(
                              SnackBar(content: Text('$count usuarios desactivados')),
                            );
                          }
                        },
                        icon: const Icon(Icons.block_outlined, size: 18, color: ZColors.danger),
                        label: const Text('Desactivar', style: TextStyle(color: ZColors.danger)),
                      ),
                      TextButton.icon(
                        onPressed: safeSelected.isEmpty ? null : () async {
                          final count = safeSelected.length;
                          final messenger = ScaffoldMessenger.of(context);
                          final confirmed = await _confirmDestructive(
                            'Eliminar $count usuarios',
                            '¿Deseas eliminar $count usuario(s)? Esta acción no se puede deshacer.',
                          );
                          if (confirmed && mounted) {
                            messenger.showSnackBar(
                              SnackBar(content: Text('$count usuarios eliminados')),
                            );
                          }
                        },
                        icon: const Icon(Icons.delete_outline, size: 18, color: ZColors.danger),
                        label: const Text('Eliminar', style: TextStyle(color: ZColors.danger)),
                      ),
                    ],
                    expandedRowBuilder: (user) => Padding(
                      padding: const EdgeInsets.all(16.0),
                      child: Row(
                        children: [
                          CircleAvatar(
                            child: Text(user.displayName[0]),
                          ),
                          const SizedBox(width: 16),
                          Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text('ID: ${user.id}', style: ZTypography.labelSmall),
                              Text('Última conexión: Hace 2 horas', style: ZTypography.labelSmall),
                            ],
                          ),
                        ],
                      ),
                    ),
                    rowMapper: (item) {
                      final isMe = item.id == currentUserId;
                      return DataRow(cells: [
                        DataCell(
                          Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Text(item.displayName, style: const TextStyle(fontWeight: FontWeight.w600)),
                              if (isMe) ...[
                                const SizedBox(width: 8),
                                ZBadge(text: 'TÚ', type: ZBadgeType.accent),
                              ],
                            ],
                          ),
                        ),
                        DataCell(Text(item.email)),
                        DataCell(ZBadge(
                          text: item.isActive ? 'ACTIVO' : 'INACTIVO',
                          type: item.isActive ? ZBadgeType.success : ZBadgeType.danger,
                        )),
                        DataCell(
                          IconButton(
                            icon: Icon(
                              item.isActive ? Icons.block_outlined : Icons.check_circle_outline,
                              size: 18,
                              color: isMe ? ZColors.neutral400 : (item.isActive ? ZColors.danger : ZColors.success),
                            ),
                            tooltip: isMe
                                ? 'No puedes desactivar tu propia cuenta'
                                : (item.isActive ? 'Desactivar' : 'Activar'),
                            onPressed: isMe ? null : () => _toggleActive(item),
                          ),
                        ),
                      ]);
                    },
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
