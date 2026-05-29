import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
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
    Future.microtask(() => ref.read(employeeProvider.notifier).load());
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(employeeProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: const Text('Empleados')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: TextField(
              controller: _searchCtrl,
              decoration: InputDecoration(
                hintText: 'Buscar empleado...',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: _searchCtrl.text.isNotEmpty
                    ? IconButton(icon: const Icon(Icons.clear), onPressed: () {
                        _searchCtrl.clear();
                        ref.read(employeeProvider.notifier).load();
                      })
                    : null,
              ),
              onChanged: (v) {
                ref.read(employeeProvider.notifier).load(search: v);
              },
            ),
          ),
          if (state.loading)
            const Expanded(child: Center(child: CircularProgressIndicator()))
          else if (state.error != null)
            Expanded(child: Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error))))
          else if (state.items.isEmpty)
            const Expanded(child: Center(child: Text('No hay empleados registrados')))
          else
            Expanded(
              child: RefreshIndicator(
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
                      onTap: () {},
                    );
                  },
                ),
              ),
            ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {},
        child: const Icon(Icons.add),
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
