import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/accounting_provider.dart';

const _typeLabels = {
  'Asset': 'Activo',
  'Liability': 'Pasivo',
  'Equity': 'Patrimonio',
  'Income': 'Ingreso',
  'Cost': 'Costo',
  'Expense': 'Gasto',
};

const _sideLabels = {
  'Debit': 'Débito',
  'Credit': 'Crédito',
};

final class ChartOfAccountsPage extends ConsumerStatefulWidget {
  const ChartOfAccountsPage({super.key});
  @override
  ConsumerState<ChartOfAccountsPage> createState() => _ChartOfAccountsPageState();
}

final class _ChartOfAccountsPageState extends ConsumerState<ChartOfAccountsPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(accountingProvider.notifier).loadAccounts());
  }

  Future<void> _deleteAccount(AccountItem account) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Eliminar cuenta'),
        content: Text('¿Eliminar "${account.code} - ${account.name}"?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Eliminar', style: TextStyle(color: Colors.white))),
        ],
      ),
    );
    if (confirmed == true) {
      try {
        await ref.read(accountingProvider.notifier).deleteAccount(account.id);
        if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Cuenta "${account.code}" eliminada')));
      } catch (e) {
        if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red));
      }
    }
  }

  void _showAccountDialog({AccountItem? account, AccountItem? parent}) {
    showDialog(
      context: context,
      builder: (ctx) => _AccountFormDialog(
        account: account,
        parent: parent,
        onSave: () => ref.read(accountingProvider.notifier).loadAccounts(),
      ),
    );
  }

  Widget _buildAccountNode(AccountItem a, int depth) {
    final hasChildren = a.children.isNotEmpty;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        InkWell(
          onTap: () => _showAccountDialog(account: a),
          child: Padding(
            padding: EdgeInsets.only(left: depth * 24.0),
            child: ListTile(
              dense: true,
              leading: Icon(_typeIcon(a.type), size: 18, color: _typeColor(a.type)),
              title: Text('${a.code} - ${a.name}', style: TextStyle(fontWeight: depth <= 1 ? FontWeight.bold : FontWeight.normal, fontSize: depth <= 1 ? 14 : 13)),
              subtitle: Text(_typeLabels[a.type] ?? a.type, style: TextStyle(fontSize: 11, color: _typeColor(a.type))),
              trailing: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text('\$${a.currentBalance.toStringAsFixed(2)}', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
                  if (depth < 3)
                    IconButton(
                      icon: const Icon(Icons.add_circle_outline, size: 18),
                      tooltip: 'Agregar subcuenta',
                      onPressed: () => _showAccountDialog(parent: a),
                      padding: EdgeInsets.zero,
                      constraints: const BoxConstraints(),
                    ),
                  if (!a.isSystem)
                    IconButton(
                      icon: const Icon(Icons.delete_outline, size: 18),
                      tooltip: 'Eliminar',
                      onPressed: () => _deleteAccount(a),
                      padding: EdgeInsets.zero,
                      constraints: const BoxConstraints(),
                    ),
                ],
              ),
            ),
          ),
        ),
        if (hasChildren) ...a.children.map((c) => _buildAccountNode(c, depth + 1)),
      ],
    );
  }

  Color _typeColor(String type) => switch (type) {
    'Asset' => Colors.blue,
    'Liability' => Colors.orange,
    'Equity' => Colors.green,
    'Income' => Colors.teal,
    'Cost' => Colors.red,
    'Expense' => Colors.purple,
    _ => Colors.grey,
  };

  IconData _typeIcon(String type) => switch (type) {
    'Asset' => Icons.account_balance,
    'Liability' => Icons.credit_card,
    'Equity' => Icons.savings,
    'Income' => Icons.trending_up,
    'Cost' => Icons.shopping_cart,
    'Expense' => Icons.money_off,
    _ => Icons.book,
  };

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(accountingProvider);
    final theme = Theme.of(context);

    return Scaffold(
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                IconButton(
                  icon: const Icon(Icons.auto_fix_high),
                  tooltip: 'Sembrar catálogo por defecto',
                  onPressed: () async {
                    await ref.read(accountingProvider.notifier).seedAccounts();
                    if (!context.mounted) return;
                    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Catálogo sembrado')));
                  },
                ),
              ],
            ),
          ),
          Expanded(
            child: state.loading
                ? const Center(child: CircularProgressIndicator())
                : state.error != null
                    ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
                    : state.accounts.isEmpty
                        ? const Center(child: Text('No hay cuentas. Presione el botón de sembrar para crear el catálogo por defecto.'))
                        : RefreshIndicator(
                            onRefresh: () => ref.read(accountingProvider.notifier).loadAccounts(),
                            child: ListView(children: state.accounts.map((a) => _buildAccountNode(a, 0)).toList()),
                          ),
          ),
        ],
      ),
    );
  }
}

final class _AccountFormDialog extends ConsumerStatefulWidget {
  final AccountItem? account;
  final AccountItem? parent;
  final VoidCallback onSave;

  const _AccountFormDialog({this.account, this.parent, required this.onSave});

  @override
  ConsumerState<_AccountFormDialog> createState() => _AccountFormDialogState();
}

final class _AccountFormDialogState extends ConsumerState<_AccountFormDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _codeCtrl;
  late final TextEditingController _nameCtrl;
  late final TextEditingController _descCtrl;
  late final TextEditingController _balanceCtrl;

  String _type = 'Asset';
  String _normalSide = 'Debit';
  bool _isActive = true;
  bool _saving = false;

  bool get _isEditing => widget.account != null;

  @override
  void initState() {
    super.initState();
    final a = widget.account;
    _codeCtrl = TextEditingController(text: a?.code ?? _suggestCode());
    _nameCtrl = TextEditingController(text: a?.name ?? '');
    _descCtrl = TextEditingController(text: a?.description ?? '');
    _balanceCtrl = TextEditingController(text: a?.openingBalance.toString() ?? '0');
    _type = a?.type ?? _suggestType();
    _normalSide = a?.normalSide ?? (_suggestType() == 'Asset' || _suggestType() == 'Cost' ? 'Debit' : 'Credit');
    _isActive = a?.isActive ?? true;
  }

  String _suggestCode() {
    if (widget.parent != null) return '${widget.parent!.code}.01';
    return '';
  }

  String _suggestType() {
    if (widget.parent != null) {
      if (widget.parent!.code.startsWith('1')) return 'Asset';
      if (widget.parent!.code.startsWith('2')) return 'Liability';
      if (widget.parent!.code.startsWith('3')) return 'Equity';
      if (widget.parent!.code.startsWith('4')) return 'Income';
      if (widget.parent!.code.startsWith('5')) return 'Cost';
      if (widget.parent!.code.startsWith('6')) return 'Expense';
    }
    return 'Asset';
  }

  @override
  void dispose() {
    _codeCtrl.dispose();
    _nameCtrl.dispose();
    _descCtrl.dispose();
    _balanceCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    try {
      final notifier = ref.read(accountingProvider.notifier);
      if (_isEditing) {
        await notifier.updateAccount(widget.account!.id, {
          if (_codeCtrl.text != widget.account!.code) 'code': _codeCtrl.text,
          if (_nameCtrl.text != widget.account!.name) 'name': _nameCtrl.text,
          'description': _descCtrl.text.isEmpty ? null : _descCtrl.text,
          if (_type != widget.account!.type) 'type': _type,
          if (_normalSide != widget.account!.normalSide) 'normalSide': _normalSide,
          'isActive': _isActive,
          'openingBalance': double.tryParse(_balanceCtrl.text) ?? 0,
        });
      } else {
        await notifier.createAccount({
          'code': _codeCtrl.text,
          'name': _nameCtrl.text,
          'description': _descCtrl.text.isEmpty ? null : _descCtrl.text,
          'type': _type,
          'normalSide': _normalSide,
          'parentId': widget.parent?.id,
          'level': (widget.parent?.level ?? -1) + 1,
          'openingBalance': double.tryParse(_balanceCtrl.text) ?? 0,
        });
      }
      widget.onSave();
      if (mounted) Navigator.pop(context);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final types = ['Asset', 'Liability', 'Equity', 'Income', 'Cost', 'Expense'];
    final sides = ['Debit', 'Credit'];

    return AlertDialog(
      title: Text(_isEditing ? 'Editar cuenta' : 'Nueva cuenta'),
      content: Form(
        key: _formKey,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextFormField(
                controller: _codeCtrl,
                decoration: const InputDecoration(labelText: 'Código', hintText: '1.1.01'),
                validator: (v) => (v == null || v.isEmpty) ? 'Requerido' : null,
              ),
              TextFormField(
                controller: _nameCtrl,
                decoration: const InputDecoration(labelText: 'Nombre'),
                validator: (v) => (v == null || v.isEmpty) ? 'Requerido' : null,
              ),
              TextFormField(
                controller: _descCtrl,
                decoration: const InputDecoration(labelText: 'Descripción'),
                maxLines: 2,
              ),
              DropdownButtonFormField<String>(
                initialValue: _type,
                decoration: const InputDecoration(labelText: 'Tipo'),
                items: types.map((t) => DropdownMenuItem(value: t, child: Text(_typeLabels[t] ?? t))).toList(),
                onChanged: (v) {
                  if (v != null) setState(() => _type = v);
                },
              ),
              DropdownButtonFormField<String>(
                initialValue: _normalSide,
                decoration: const InputDecoration(labelText: 'Naturaleza'),
                items: sides.map((s) => DropdownMenuItem(value: s, child: Text(_sideLabels[s] ?? s))).toList(),
                onChanged: (v) {
                  if (v != null) setState(() => _normalSide = v);
                },
              ),
              if (_isEditing)
                SwitchListTile(
                  title: const Text('Activa'),
                  value: _isActive,
                  onChanged: (v) => setState(() => _isActive = v),
                ),
              TextFormField(
                controller: _balanceCtrl,
                decoration: const InputDecoration(labelText: 'Saldo inicial'),
                keyboardType: TextInputType.number,
              ),
              if (widget.parent != null)
                Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: Text('Padre: ${widget.parent!.code} - ${widget.parent!.name}',
                    style: TextStyle(color: Theme.of(context).colorScheme.primary, fontSize: 12)),
                ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancelar')),
        FilledButton(
          onPressed: _saving ? null : _save,
          child: _saving ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)) : const Text('Guardar'),
        ),
      ],
    );
  }
}
