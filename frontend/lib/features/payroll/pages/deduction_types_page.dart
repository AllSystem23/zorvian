import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/error/error_notifier.dart';
import '../providers/payroll_provider.dart';

class DeductionTypesPage extends ConsumerStatefulWidget {
  const DeductionTypesPage({super.key});

  @override
  ConsumerState<DeductionTypesPage> createState() => _DeductionTypesPageState();
}

class _DeductionTypesPageState extends ConsumerState<DeductionTypesPage> {
  List<dynamic> _items = [];
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
      _items = await svc.getDeductionTypes();
    } catch (_) {}
    setState(() => _loading = false);
  }

  Future<void> _create() async {
    final nameCtrl = TextEditingController();
    final codeCtrl = TextEditingController();
    final rateCtrl = TextEditingController();
    final method = ValueNotifier<String>('percentage');

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Nuevo tipo de deducción'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(controller: codeCtrl, decoration: const InputDecoration(labelText: 'Código', isDense: true)),
              const SizedBox(height: 8),
              TextField(controller: nameCtrl, decoration: const InputDecoration(labelText: 'Nombre', isDense: true)),
              const SizedBox(height: 8),
              ValueListenableBuilder(
                valueListenable: method,
                builder: (_, v, __) => DropdownButtonFormField<String>(
                  value: v,
                  decoration: const InputDecoration(labelText: 'Método', isDense: true),
                  items: const [
                    DropdownMenuItem(value: 'percentage', child: Text('Porcentaje')),
                    DropdownMenuItem(value: 'fixed', child: Text('Monto fijo')),
                  ],
                  onChanged: (val) => method.value = val ?? 'percentage',
                ),
              ),
              if (method.value == 'percentage')
                TextField(controller: rateCtrl, decoration: const InputDecoration(labelText: 'Tasa (%)', isDense: true), keyboardType: TextInputType.number),
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
      await svc.createDeductionType({
        'code': codeCtrl.text,
        'name': nameCtrl.text,
        'calculationMethod': method.value,
        'rate': double.tryParse(rateCtrl.text),
      });
      if (mounted) ref.read(errorNotifierProvider.notifier).showInfo('Deducción creada');
      _load();
    } catch (e) {
      if (mounted) ref.read(errorNotifierProvider.notifier).showError('Error al crear');
    }
  }

  Future<void> _delete(String id, String name) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Eliminar deducción'),
        content: Text('¿Eliminar "$name"?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          TextButton(onPressed: () => Navigator.pop(ctx, true), child: Text('Eliminar', style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (confirmed != true) return;

    try {
      final svc = ref.read(payrollServiceProvider);
      await svc.deleteDeductionType(id);
      if (mounted) ref.read(errorNotifierProvider.notifier).showInfo('Deducción eliminada');
      _load();
    } catch (e) {
      if (mounted) ref.read(errorNotifierProvider.notifier).showError('Error al eliminar');
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Tipos de Deducción'),
        actions: [
          IconButton(icon: const Icon(Icons.add), onPressed: _create),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _items.isEmpty
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.category, size: 48, color: theme.colorScheme.primary.withValues(alpha: 0.5)),
                      const SizedBox(height: 16),
                      const Text('No hay tipos de deducción'),
                      const SizedBox(height: 8),
                      FilledButton.tonalIcon(onPressed: _create, icon: const Icon(Icons.add), label: const Text('Crear primera')),
                    ],
                  ),
                )
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: _items.length,
                  itemBuilder: (_, i) {
                    final item = _items[i];
                    return Card(
                      child: ListTile(
                        title: Text(item['name'] ?? ''),
                        subtitle: Text('${item['code'] ?? ''} · ${item['calculationMethod'] == 'percentage' ? '${item['rate']}%' : 'Monto fijo'}'),
                        trailing: IconButton(
                          icon: Icon(Icons.delete_outline, color: Colors.red.shade400, size: 20),
                          onPressed: () => _delete(item['id'], item['name'] ?? ''),
                        ),
                      ),
                    );
                  },
                ),
    );
  }
}