import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../../cost_centers/providers/cost_center_provider.dart';
import '../providers/budget_provider.dart';

final class BudgetFormPage extends ConsumerStatefulWidget {
  final String? budgetId;
  const BudgetFormPage({super.key, this.budgetId});
  @override
  ConsumerState<BudgetFormPage> createState() => _BudgetFormPageState();
}

final class _BudgetFormPageState extends ConsumerState<BudgetFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _yearCtrl = TextEditingController();
  final _monthCtrl = TextEditingController();
  final _amountCtrl = TextEditingController();
  String? _accountId;
  String? _costCenterId;
  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.budgetId != null;

  @override
  void initState() {
    super.initState();
    ref.read(accountListProvider.notifier).load();
    ref.read(costCenterProvider.notifier).load();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('budgets/${widget.budgetId}');
      final d = r.data;
      _yearCtrl.text = d['year'].toString();
      _monthCtrl.text = d['month'].toString();
      _accountId = d['accountId'] as String?;
      _costCenterId = d['costCenterId'] as String?;
      _amountCtrl.text = (d['budgetedAmount'] as num?)?.toStringAsFixed(2) ?? '';
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar presupuesto');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (_accountId == null) {
      setState(() => _error = 'Seleccione una cuenta');
      return;
    }
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'year': int.parse(_yearCtrl.text.trim()),
        'month': int.parse(_monthCtrl.text.trim()),
        'accountId': _accountId,
        'costCenterId': _costCenterId,
        'budgetedAmount': double.parse(_amountCtrl.text.trim()),
      };
      if (_isEditing) {
        await dio.put('budgets/${widget.budgetId}', data: body);
      } else {
        await dio.post('budgets', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar presupuesto');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _yearCtrl.dispose(); _monthCtrl.dispose(); _amountCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final accounts = ref.watch(accountListProvider);
    final costCenters = ref.watch(costCenterProvider);
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar presupuesto' : 'Nuevo presupuesto')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null) Container(
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(color: theme.colorScheme.errorContainer, borderRadius: BorderRadius.circular(8)),
                child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
              ),
              Row(
                children: [
                  Expanded(child: TextFormField(controller: _yearCtrl, decoration: const InputDecoration(labelText: 'Año', prefixIcon: Icon(Icons.calendar_today)), keyboardType: TextInputType.number, validator: (v) => v == null || int.tryParse(v) == null ? 'Inválido' : null)),
                  const SizedBox(width: 12),
                  Expanded(child: TextFormField(controller: _monthCtrl, decoration: const InputDecoration(labelText: 'Mes (1-12)', prefixIcon: Icon(Icons.date_range)), keyboardType: TextInputType.number, validator: (v) => v == null || int.tryParse(v) == null ? 'Inválido' : null)),
                ],
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                decoration: const InputDecoration(labelText: 'Cuenta', prefixIcon: Icon(Icons.account_balance)),
                initialValue: _accountId,
                items: accounts.map((a) => DropdownMenuItem(value: a.id, child: Text('${a.code} - ${a.name}'))).toList(),
                onChanged: (v) => setState(() => _accountId = v),
                validator: (v) => v == null ? 'Requerido' : null,
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                decoration: const InputDecoration(labelText: 'Centro de costo (opcional)', prefixIcon: Icon(Icons.account_tree)),
                initialValue: _costCenterId,
                items: [
                  const DropdownMenuItem(value: null, child: Text('Sin centro de costo')),
                  ...costCenters.items.map((c) => DropdownMenuItem(value: c.id, child: Text('${c.code} - ${c.name}'))),
                ],
                onChanged: (v) => setState(() => _costCenterId = v),
              ),
              const SizedBox(height: 12),
              TextFormField(controller: _amountCtrl, decoration: const InputDecoration(labelText: 'Monto presupuestado', prefixIcon: Icon(Icons.attach_money)), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear presupuesto',
                onPressed: _save,
                isLoading: _loading,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
