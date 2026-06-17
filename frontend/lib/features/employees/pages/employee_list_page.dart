import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:file_picker/file_picker.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/employee_provider.dart';

class EmployeeListPage extends ConsumerStatefulWidget {
  const EmployeeListPage({super.key});

  @override
  ConsumerState<EmployeeListPage> createState() => _EmployeeListPageState();
}

class _EmployeeListPageState extends ConsumerState<EmployeeListPage> {
  final _searchCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    // Removed explicit load() as AsyncNotifier does it in build()
    _searchCtrl.addListener(() {
      ref.read(employeeProvider.notifier).load(search: _searchCtrl.text);
    });
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _importExcel() async {
    final result = await FilePicker.pickFiles(
      type: FileType.custom,
      allowedExtensions: ['xlsx'],
    );
    if (result == null || result.files.single.path == null) return;

    try {
      final dio = ref.read(dioClientProvider);
      final file = await MultipartFile.fromFile(
        result.files.single.path!,
        filename: result.files.single.name,
      );
      final form = FormData.fromMap({'file': file});
      final r = await dio.post('employees/import', data: form);
      final data = r.data;
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('${data['imported']} importados, ${data['failed']} fallaron'),
          ),
        );
        ref.read(employeeProvider.notifier).load();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error al importar Excel')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(employeeProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Empleados'),
        actions: [
          IconButton(
            icon: const Icon(Icons.upload_file),
            tooltip: 'Importar Excel',
            onPressed: _importExcel,
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: ZTextField(
              controller: _searchCtrl,
              label: 'Buscar',
              hint: 'Buscar empleado...',
              prefix: const Icon(Icons.search),
            ),
          ),
          Expanded(
            child: ZAsyncRenderer<EmployeeListState>(
              value: state,
              builder: (state) {
                return state.items.isEmpty
                    ? ZEmptyState.list(
                        itemType: 'empleados',
                        actionLabel: 'Nuevo Empleado',
                        onAction: () => context.push('/employees/new'),
                      )
                    : RefreshIndicator(
                        onRefresh: () => ref.read(employeeProvider.notifier).load(),
                        child: ListView.separated(
                          itemCount: state.items.length,
                          separatorBuilder: (_, _) => const Divider(height: 1),
                          itemBuilder: (_, i) {
                            final e = state.items[i];
                            return ListTile(
                              title: Text(e.fullName, style: const TextStyle(fontWeight: FontWeight.w600)),
                              subtitle: Text('${e.position} · ${e.departmentName}'),
                              trailing: _StatusBadge(e.status),
                              onTap: () => context.push('/employees/${e.id}'),
                            );
                          },
                        ),
                      );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _StatusBadge extends StatelessWidget {
  final String status;
  const _StatusBadge(this.status);

  @override
  Widget build(BuildContext context) {
    final isActive = status == 'active';
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: isActive ? Colors.green.withAlpha(25) : Colors.orange.withAlpha(25),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        isActive ? 'Activo' : 'Inactivo',
        style: TextStyle(
          fontSize: 12,
          color: isActive ? Colors.green : Colors.orange,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}
