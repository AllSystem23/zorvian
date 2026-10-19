import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../providers/user_provider.dart';
import '../providers/user_management_provider.dart';
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

  /// Asigna un rol al usuario — incluye los roles PERSONALIZADOS creados en
  /// `/admin/roles` (`GET users/roles` los devuelve junto a los del sistema).
  Future<void> _assignRole(UserModel user, List<RoleOption> options) async {
    if (_isCurrentUser(user)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('No puedes cambiar tu propio rol: solicítalo a otro administrador'),
          backgroundColor: ZColors.danger,
        ),
      );
      return;
    }

    final isSuperAdmin = ref.read(authProvider).role == 'SuperAdmin';
    final assignable =
        options.where((o) => isSuperAdmin || !o.isPlatform).toList();
    if (assignable.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('No se pudieron cargar los roles'),
          backgroundColor: ZColors.danger,
        ),
      );
      return;
    }

    RoleOption? current;
    for (final option in assignable) {
      if (user.roles.contains(option.displayName)) {
        current = option;
        break;
      }
    }

    final selected = await showDialog<String>(
      context: context,
      builder: (_) => _RolePickerDialog(
        userName: user.displayName,
        options: assignable,
        initialRoleId: current?.id,
      ),
    );
    if (selected == null || selected == current?.id || !mounted) return;

    try {
      await ref.read(userProvider.notifier).updateUserRole(user.id, selected);
      if (!mounted) return;
      final label =
          assignable.firstWhere((o) => o.id == selected).label;
      ZToast.show(
        context,
        'Rol de "${user.displayName}" actualizado a "$label".',
        type: ZToastType.success,
      );
    } catch (e) {
      if (!mounted) return;
      final data = e is DioException ? e.response?.data : null;
      final message = data is Map && data['error'] != null
          ? '${data['error']}'
          : 'No se pudo asignar el rol';
      ZToast.show(context, message, type: ZToastType.error);
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(userProvider);
    final currentUserId = ref.watch(authProvider).userId;
    final auth = ref.watch(authProvider);
    final canManageUsers = auth.hasPermission('user.manage');
    final canInvite = auth.hasPermission('user.write');
    // Mantiene vivo el provider (autoDispose) que alimenta el diálogo de roles.
    final rolesAsync = ref.watch(rolesProvider);

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
                if (canInvite)
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
                      ZColumn(id: 'role', label: 'Rol'),
                      ZColumn(id: 'active', label: 'Estado'),
                      ZColumn(id: 'actions', label: 'Acciones', width: 160),
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
                        DataCell(
                          Text(
                            item.roles.isEmpty ? '—' : item.roles.join(', '),
                            style: ZTypography.bodySmall,
                          ),
                        ),
                        DataCell(ZBadge(
                          text: item.isActive ? 'ACTIVO' : 'INACTIVO',
                          type: item.isActive ? ZBadgeType.success : ZBadgeType.danger,
                        )),
                        DataCell(
                          Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              if (canManageUsers)
                                IconButton(
                                  icon: Icon(
                                    Icons.manage_accounts_outlined,
                                    size: 18,
                                    color: isMe ? ZColors.neutral400 : ZColors.brandAccent,
                                  ),
                                  tooltip: isMe
                                      ? 'No puedes cambiar tu propio rol'
                                      : 'Asignar rol',
                                  onPressed: isMe
                                      ? null
                                      : () => _assignRole(
                                            item,
                                            rolesAsync.asData?.value ??
                                                const <RoleOption>[],
                                          ),
                                ),
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
                            ],
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

/// Selector de rol de un usuario. Devuelve el `id` del rol elegido, o `null`
/// si se cancela.
class _RolePickerDialog extends StatefulWidget {
  const _RolePickerDialog({
    required this.userName,
    required this.options,
    this.initialRoleId,
  });

  final String userName;
  final List<RoleOption> options;
  final String? initialRoleId;

  @override
  State<_RolePickerDialog> createState() => _RolePickerDialogState();
}

class _RolePickerDialogState extends State<_RolePickerDialog> {
  String? _selected;

  @override
  void initState() {
    super.initState();
    _selected = widget.initialRoleId ?? widget.options.first.id;
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Asignar rol'),
      content: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 380),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Usuario: ${widget.userName}', style: ZTypography.bodySmall),
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              initialValue: _selected,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Rol'),
              items: widget.options
                  .map((o) => DropdownMenuItem<String>(
                        value: o.id,
                        child: Text(o.label, overflow: TextOverflow.ellipsis),
                      ))
                  .toList(),
              onChanged: (value) => setState(() => _selected = value),
            ),
            const SizedBox(height: 8),
            Text(
              'El usuario verá el nuevo rol al refrescar su sesión.',
              style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Cancelar'),
        ),
        ZButton(
          text: 'Guardar',
          fullWidth: false,
          onPressed: () => Navigator.pop(context, _selected),
        ),
      ],
    );
  }
}
