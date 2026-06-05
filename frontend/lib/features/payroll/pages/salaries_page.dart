import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/error/error_notifier.dart';
import '../../../core/network/dio_client.dart';
import '../../../core/widgets/empty_state.dart';
import '../providers/payroll_provider.dart';

class SalariesPage extends ConsumerStatefulWidget {
  const SalariesPage({super.key});

  @override
  ConsumerState<SalariesPage> createState() => _SalariesPageState();
}

class _SalariesPageState extends ConsumerState<SalariesPage> {
  List<dynamic> _salaries = [];
  List<dynamic> _employees = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final svc = ref.read(payrollServiceProvider);
      final dio = ref.read(dioClientProvider);
      _salaries = await svc.getSalaries(null);
      final empRes = await dio.get('employees', params: {'page': 1, 'pageSize': 500});
      _employees = (empRes.data['items'] as List?) ?? [];
    } catch (_) {}
    setState(() => _loading = false);
  }

  Future<void> _create() async {
    final baseCtrl = TextEditingController();
    final type = ValueNotifier<String>('monthly');
    final selectedEmployee = ValueNotifier<String?>(null);

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Nuevo salario'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              ValueListenableBuilder(
                valueListenable: selectedEmployee,
                builder: (_, v, __) => DropdownButtonFormField<String>(
                  value: v,
                  decoration: const InputDecoration(labelText: 'Empleado', isDense: true),
                  items: _employees.map<DropdownMenuItem<String>>((e) => DropdownMenuItem(value: e['id'] as String?, child: Text(e['fullName'] ?? e['firstName'] ?? ''))).toList(),
                  onChanged: (val) => selectedEmployee.value = val,
                ),
              ),
              const SizedBox(height: 8),
              TextField(controller: baseCtrl, decoration: const InputDecoration(labelText: 'Salario base', isDense: true), keyboardType: TextInputType.number),
              const SizedBox(height: 8),
              ValueListenableBuilder(
                valueListenable: type,
                builder: (_, v, __) => DropdownButtonFormField<String>(
                  value: v,
                  decoration: const InputDecoration(labelText: 'Tipo', isDense: true),
                  items: const [
                    DropdownMenuItem(value: 'monthly', child: Text('Mensual')),
                    DropdownMenuItem(value: 'biweekly', child: Text('Quincenal')),
                    DropdownMenuItem(value: 'hourly', child: Text('Por hora')),
                  ],
                  onChanged: (val) => type.value = val ?? 'monthly',
                ),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Crear')),
        ],
      ),
    );
    if (result != true) return;

    try {
      final svc = ref.read(payrollServiceProvider);
      await svc.createSalary({
        'employeeId': selectedEmployee.value,
        'baseSalary': double.tryParse(baseCtrl.text) ?? 0,
        'salaryType': type.value,
        'effectiveDate': DateTime.now().toIso8601String().substring(0, 10),
      });
      if (mounted) ref.read(errorNotifierProvider.notifier).showInfo('Salario creado');
      _load();
    } catch (e) {
      if (mounted) ref.read(errorNotifierProvider.notifier).showError('Error al crear salario');
    }
  }

  Future<void> _deactivate(String id) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Desactivar salario'),
        content: const Text('¿Está seguro de desactivar este salario?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          TextButton(onPressed: () => Navigator.pop(ctx, true), child: Text('Desactivar', style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (confirmed != true) return;
    try {
      final svc = ref.read(payrollServiceProvider);
      await svc.deactivateSalary(id);
      if (mounted) ref.read(errorNotifierProvider.notifier).showInfo('Salario desactivado');
      _load();
    } catch (e) {
      if (mounted) ref.read(errorNotifierProvider.notifier).showError('Error al desactivar salario');
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Salarios'),
        actions: [
          IconButton(icon: const Icon(Icons.add), onPressed: _create),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _salaries.isEmpty
              ? EmptyState(
                  icon: Icons.attach_money,
                  title: 'Sin salarios',
                  subtitle: 'Registre salarios para los empleados',
                  actionLabel: 'Nuevo salario',
                  onAction: _create,
                )
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: _salaries.length,
                  itemBuilder: (_, i) {
                    final s = _salaries[i];
                    final active = s['isActive'] == true;
                    return Card(
                      child: ListTile(
                        leading: CircleAvatar(
                          backgroundColor: active ? Colors.green.shade100 : Colors.grey.shade200,
                          child: Icon(Icons.person, color: active ? Colors.green : Colors.grey),
                        ),
                        title: Text(s['employeeName'] ?? ''),
                        subtitle: Text('C\$${(s['baseSalary'] as num?)?.toStringAsFixed(2) ?? '0.00'} · ${s['salaryType'] ?? ''}'),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Chip(
                              label: Text(active ? 'Activo' : 'Inactivo', style: TextStyle(fontSize: 11, color: active ? Colors.green : Colors.grey)),
                              materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                            ),
                            if (active)
                              IconButton(
                                icon: Icon(Icons.block, size: 18, color: Colors.red.shade400),
                                onPressed: () => _deactivate(s['id']),
                              ),
                          ],
                        ),
                      ),
                    );
                  },
                ),
    );
  }
}