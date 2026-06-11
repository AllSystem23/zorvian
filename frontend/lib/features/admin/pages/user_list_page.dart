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
                    DataColumn(label: Text('Nombre')),
                    DataColumn(label: Text('Email')),
                    DataColumn(label: Text('Activo')),
                  ],
                  rows: users,
                  rowMapper: (item) => DataRow(cells: [
                    DataCell(Text(item.displayName)),
                    DataCell(Text(item.email)),
                    DataCell(Icon(item.isActive ? Icons.check_circle : Icons.cancel, color: item.isActive ? ZColors.success : ZColors.danger)),
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
