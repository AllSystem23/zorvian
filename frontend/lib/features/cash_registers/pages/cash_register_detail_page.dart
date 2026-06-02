import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../providers/cash_register_provider.dart';

final class CashRegisterDetailPage extends ConsumerStatefulWidget {
  final String registerId;
  const CashRegisterDetailPage({super.key, required this.registerId});
  @override
  ConsumerState<CashRegisterDetailPage> createState() => _CashRegisterDetailPageState();
}

final class _CashRegisterDetailPageState extends ConsumerState<CashRegisterDetailPage> with SingleTickerProviderStateMixin {
  Map<String, dynamic>? _data;
  bool _loading = true;
  String? _error;
  List<CashMovement> _movements = [];
  bool _movementsLoading = false;
  late TabController _tabCtrl;

  @override
  void initState() {
    super.initState();
    _tabCtrl = TabController(length: 2, vsync: this);
    _load();
    _loadMovements();
  }

  @override
  void dispose() {
    _tabCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('cash-registers/${widget.registerId}');
      setState(() { _data = r.data as Map<String, dynamic>; _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar registro'; _loading = false; });
    }
  }

  Future<void> _loadMovements() async {
    try {
      setState(() => _movementsLoading = true);
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('cash-registers/${widget.registerId}/movements');
      final data = r.data;
      final list = data is List ? data : [];
      setState(() { _movements = list.map((e) => CashMovement.fromJson(e as Map<String, dynamic>)).toList(); _movementsLoading = false; });
    } catch (_) {
      setState(() => _movementsLoading = false);
    }
  }

  Future<void> _closeRegister() async {
    final balanceCtrl = TextEditingController();
    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Cerrar Caja'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text('Saldo esperado: \$${((_data?['expectedBalance'] as num?)?.toDouble() ?? 0).toStringAsFixed(0)}'),
            const SizedBox(height: 12),
            TextField(controller: balanceCtrl, decoration: const InputDecoration(labelText: 'Saldo Real', border: OutlineInputBorder(), prefixText: '\$ '), keyboardType: TextInputType.number),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Cerrar')),
        ],
      ),
    );
    if (result != true || balanceCtrl.text.isEmpty) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('cash-registers/${widget.registerId}/close', data: {
        'closingBalance': double.tryParse(balanceCtrl.text) ?? 0,
      });
      await _load();
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Caja cerrada')));
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
    }
  }

  Future<void> _addMovement() async {
    final typeCtrl = TextEditingController();
    final amountCtrl = TextEditingController();
    final conceptCtrl = TextEditingController();
    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Agregar Movimiento'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            DropdownButtonFormField<String>(
              initialValue: 'income',
              items: const [
                DropdownMenuItem(value: 'income', child: Text('Ingreso')),
                DropdownMenuItem(value: 'expense', child: Text('Egreso')),
              ],
              onChanged: (v) => typeCtrl.text = v ?? 'income',
              decoration: const InputDecoration(labelText: 'Tipo', border: OutlineInputBorder()),
            ),
            const SizedBox(height: 12),
            TextField(controller: amountCtrl, decoration: const InputDecoration(labelText: 'Monto', border: OutlineInputBorder(), prefixText: '\$ '), keyboardType: TextInputType.number),
            const SizedBox(height: 12),
            TextField(controller: conceptCtrl, decoration: const InputDecoration(labelText: 'Concepto', border: OutlineInputBorder())),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Agregar')),
        ],
      ),
    );
    if (result != true || amountCtrl.text.isEmpty) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('cash-registers/${widget.registerId}/movements', data: {
        'movementType': typeCtrl.text.isEmpty ? 'income' : typeCtrl.text,
        'amount': double.tryParse(amountCtrl.text) ?? 0,
        'concept': conceptCtrl.text,
      });
      await _loadMovements();
      await _load();
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Movimiento registrado')));
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (_loading) return Scaffold(appBar: AppBar(title: const Text('Caja')), body: const Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(appBar: AppBar(title: const Text('Caja')), body: Center(child: Text(_error!)));

    final d = _data!;
    final isOpen = d['status'] == 'open';

    return Scaffold(
      appBar: AppBar(
        title: Text(d['code'] ?? 'Caja'),
        actions: [
          if (isOpen) ...[
            IconButton(icon: const Icon(Icons.payments), onPressed: _addMovement, tooltip: 'Agregar Movimiento'),
            IconButton(icon: const Icon(Icons.lock), onPressed: _closeRegister, tooltip: 'Cerrar Caja'),
          ],
        ],
      ),
      body: Column(
        children: [
          Card(
            margin: const EdgeInsets.all(12),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(children: [
                    Expanded(child: Text(d['code'] ?? '', style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold))),
                    Chip(label: Text(d['status'] ?? '', style: const TextStyle(fontSize: 11, color: Colors.white)), backgroundColor: isOpen ? Colors.green : Colors.grey, materialTapTargetSize: MaterialTapTargetSize.shrinkWrap),
                  ]),
                  const SizedBox(height: 8),
                  _row('Saldo Inicial', '\$${(d['openingBalance'] as num?)?.toStringAsFixed(0) ?? '0'}'),
                  _row('Total Ingresos', '\$${(d['totalIncome'] as num?)?.toStringAsFixed(0) ?? '0'}'),
                  _row('Total Egresos', '\$${(d['totalExpense'] as num?)?.toStringAsFixed(0) ?? '0'}'),
                  const Divider(),
                  _row('Saldo Esperado', '\$${(d['expectedBalance'] as num?)?.toStringAsFixed(0) ?? '0'}'),
                  _row('Saldo Real', '\$${(d['closingBalance'] as num?)?.toStringAsFixed(0) ?? '0'}'),
                  _row('Diferencia', '\$${(d['difference'] as num?)?.toStringAsFixed(0) ?? '0'}',
                    color: ((d['difference'] as num?)?.abs() ?? 0) > 0.01 ? Colors.red : Colors.green, bold: true),
                  if (d['employeeName'] != null) ...[
                    const Divider(),
                    _row('Empleado', d['employeeName'] as String),
                  ],
                ],
              ),
            ),
          ),
          TabBar(
            controller: _tabCtrl,
            tabs: const [
              Tab(text: 'Movimientos'),
              Tab(text: 'Resumen'),
            ],
          ),
          Expanded(
            child: TabBarView(
              controller: _tabCtrl,
              children: [
                _movementsTab(theme),
                _summaryTab(d, theme),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _movementsTab(ThemeData theme) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(12),
          child: SizedBox(
            width: double.infinity,
            child: FilledButton.icon(
              icon: const Icon(Icons.add),
              label: const Text('Agregar Movimiento'),
              onPressed: _addMovement,
            ),
          ),
        ),
        Expanded(
          child: _movementsLoading
              ? const Center(child: CircularProgressIndicator())
              : _movements.isEmpty
                  ? const Center(child: Text('Sin movimientos'))
                  : RefreshIndicator(
                      onRefresh: _loadMovements,
                      child: ListView.builder(
                        itemCount: _movements.length,
                        itemBuilder: (_, i) {
                          final m = _movements[i];
                          final isIncome = m.movementType == 'income';
                          return Card(
                            margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                            child: ListTile(
                              leading: CircleAvatar(
                                backgroundColor: (isIncome ? Colors.green : Colors.red).withAlpha(30),
                                child: Icon(isIncome ? Icons.arrow_upward : Icons.arrow_downward, color: isIncome ? Colors.green : Colors.red),
                              ),
                              title: Text(m.concept ?? m.movementType, style: TextStyle(fontWeight: FontWeight.w600)),
                              subtitle: Text(m.createdAt.length >= 10 ? m.createdAt.substring(0, 16) : m.createdAt),
                              trailing: Text('\$${m.amount.toStringAsFixed(0)}', style: TextStyle(fontWeight: FontWeight.bold, color: isIncome ? Colors.green : Colors.red, fontSize: 15)),
                            ),
                          );
                        },
                      ),
                    ),
        ),
      ],
    );
  }

  Widget _summaryTab(Map<String, dynamic> d, ThemeData theme) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _row('Código', d['code'] ?? ''),
        _row('Estado', d['status'] ?? ''),
        _row('Empleado', d['employeeName'] ?? ''),
        const Divider(),
        _row('Saldo Inicial', '\$${(d['openingBalance'] as num?)?.toStringAsFixed(0) ?? '0'}'),
        _row('Total Ingresos', '\$${(d['totalIncome'] as num?)?.toStringAsFixed(0) ?? '0'}'),
        _row('Total Egresos', '\$${(d['totalExpense'] as num?)?.toStringAsFixed(0) ?? '0'}'),
        const Divider(),
        _row('Saldo Esperado', '\$${(d['expectedBalance'] as num?)?.toStringAsFixed(0) ?? '0'}'),
        _row('Saldo Real', '\$${(d['closingBalance'] as num?)?.toStringAsFixed(0) ?? '0'}'),
        _row('Diferencia', '\$${(d['difference'] as num?)?.toStringAsFixed(0) ?? '0'}',
          bold: true, color: ((d['difference'] as num?)?.abs() ?? 0) > 0.01 ? Colors.red : Colors.green),
        const Divider(),
        _row('Abierto', (d['openedAt'] as String?)?.substring(0, 10) ?? ''),
        if (d['closedAt'] != null) _row('Cerrado', (d['closedAt'] as String).substring(0, 10)),
        if (d['notes'] != null) _row('Notas', d['notes'] as String),
      ],
    );
  }

  Widget _row(String label, String value, {bool bold = false, Color? color}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(color: Colors.grey)),
          Text(value, style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.normal, color: color)),
        ],
      ),
    );
  }
}
