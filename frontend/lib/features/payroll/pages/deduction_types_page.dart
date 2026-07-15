import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/error/error_notifier.dart';
import '../providers/payroll_provider.dart';
import '../../../shared/ds/ds.dart';

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

    final result = await ZModal.show<bool>(context,
      title: 'Nuevo tipo de deducción',
      confirmText: 'Crear',
      cancelText: 'Cancelar',
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ZTextField(controller: codeCtrl, label: 'Código'),
            const SizedBox(height: 8),
            ZTextField(controller: nameCtrl, label: 'Nombre'),
            const SizedBox(height: 8),
            ValueListenableBuilder(
              valueListenable: method,
              builder: (_, v, _) => DropdownButtonFormField<String>(
                initialValue: v,
                decoration: const InputDecoration(labelText: 'Método', isDense: true),
                items: const [
                  DropdownMenuItem(value: 'percentage', child: Text('Porcentaje')),
                  DropdownMenuItem(value: 'fixed', child: Text('Monto fijo')),
                ],
                onChanged: (val) => method.value = val ?? 'percentage',
              ),
            ),
            if (method.value == 'percentage')
              ZTextField(controller: rateCtrl, label: 'Tasa (%)', keyboardType: TextInputType.number),
          ],
        ),
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
    final confirmed = await ZModal.confirm(context,
      title: 'Eliminar deducción',
      message: '¿Eliminar "$name"?',
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
      confirmColor: Colors.red,
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
                      ZButton(text: 'Crear primera', onPressed: _create, icon: Icons.add, fullWidth: false),
                    ],
                  ),
                )
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: _items.length,
                  itemBuilder: (_, i) {
                    final item = _items[i];
                    return ZCard(
                      padding: EdgeInsets.zero,
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