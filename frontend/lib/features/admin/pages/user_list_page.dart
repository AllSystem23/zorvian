import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/shared/ds/ds.dart';
import '../providers/user_provider.dart';
import '../models/user_model.dart';

class UserListPage extends ConsumerStatefulWidget {
  const UserListPage({super.key});

  @override
  ConsumerState<UserListPage> createState() => _UserListPageState();
}

class _UserListPageState extends ConsumerState<UserListPage> {
  List<UserModel> _selectedUsers = [];

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(userProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Gestión de Usuarios')),
      body: Padding(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: Column(
          children: [
            Expanded(
              child: ZAsyncRenderer<List<UserModel>>(
                value: state,
                builder: (users) => ZDataTable<UserModel>(
                  columns: const [
                    ZColumn(id: 'name', label: 'Nombre'),
                    ZColumn(id: 'email', label: 'Email'),
                    ZColumn(id: 'active', label: 'Estado'),
                  ],
                  rows: users,
                  selectionEnabled: true,
                  onSelectionChanged: (selected) {
                    setState(() => _selectedUsers = selected);
                  },
                  bulkActions: [
                    TextButton.icon(
                      onPressed: () {
                        debugPrint('Activando ${_selectedUsers.length} usuarios');
                      },
                      icon: const Icon(Icons.check_circle_outline, size: 18),
                      label: const Text('Activar'),
                    ),
                    TextButton.icon(
                      onPressed: () {},
                      icon: const Icon(Icons.block_outlined, size: 18, color: ZColors.danger),
                      label: const Text('Desactivar', style: TextStyle(color: ZColors.danger)),
                    ),
                    TextButton.icon(
                      onPressed: () {},
                      icon: const Icon(Icons.delete_outline, size: 18, color: ZColors.danger),
                      label: const Text('Eliminar', style: TextStyle(color: ZColors.danger)),
                    ),
                  ],
                  expandedRowBuilder: (user) => Padding(
                    padding: const EdgeInsets.all(16.0),
                    child: Row(
                      children: [
                        CircleAvatar(child: Text(user.displayName[0])),
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
                  rowMapper: (item) => DataRow(cells: [
                    DataCell(Text(item.displayName, style: const TextStyle(fontWeight: FontWeight.w600))),
                    DataCell(Text(item.email)),
                    DataCell(ZBadge(
                      text: item.isActive ? 'ACTIVO' : 'INACTIVO',
                      type: item.isActive ? ZBadgeType.success : ZBadgeType.danger,
                    )),
                  ]),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
