import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../providers/department_provider.dart';

class DepartmentListPage extends ConsumerStatefulWidget {
  const DepartmentListPage({super.key});

  @override
  ConsumerState<DepartmentListPage> createState() => _DepartmentListPageState();
}

class _DepartmentListPageState extends ConsumerState<DepartmentListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(departmentProvider.notifier).load());
  }

  Future<void> _confirmDelete(String id, String name) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Eliminar departamento'),
        content: Text('¿Eliminar $name? Los empleados asignados quedarán sin departamento.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancelar')),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text('Eliminar', style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (ok != true) return;

    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('departments/$id');
      ref.read(departmentProvider.notifier).load();
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error al eliminar departamento')));
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(departmentProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: const Text('Departamentos')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : state.items.isEmpty
                  ? const Center(child: Text('No hay departamentos'))
                  : RefreshIndicator(
                      onRefresh: () => ref.read(departmentProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final d = state.items[i];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: theme.colorScheme.primaryContainer,
                              child: Text(d.code, style: TextStyle(fontSize: 12, color: theme.colorScheme.onPrimaryContainer)),
                            ),
                            title: Text(d.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text('${d.employeeCount} empleados · ${d.description}'),
                            trailing: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                if (!d.isActive)
                                  Container(
                                    margin: const EdgeInsets.only(right: 8),
                                    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                    decoration: BoxDecoration(
                                      color: Colors.grey.withAlpha(25),
                                      borderRadius: BorderRadius.circular(8),
                                    ),
                                    child: const Text('Inactivo', style: TextStyle(fontSize: 11, color: Colors.grey)),
                                  ),
                                PopupMenuButton<String>(
                                  onSelected: (v) {
                                    if (v == 'edit') context.push('/departments/${d.id}/edit');
                                    if (v == 'delete') _confirmDelete(d.id, d.name);
                                  },
                                  itemBuilder: (_) => [
                                    const PopupMenuItem(value: 'edit', child: Text('Editar')),
                                    const PopupMenuItem(value: 'delete', child: Text('Eliminar', style: TextStyle(color: Colors.red))),
                                  ],
                                ),
                              ],
                            ),
                          );
                        },
                      ),
                    ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final result = await context.push<bool>('/departments/new');
          if (result == true) ref.read(departmentProvider.notifier).load();
        },
        child: const Icon(Icons.add),
      ),
    );
  }
}
