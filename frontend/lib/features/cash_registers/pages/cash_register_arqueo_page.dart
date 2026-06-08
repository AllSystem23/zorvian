import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

final class CashRegisterArqueoPage extends ConsumerStatefulWidget {
  final String registerId;
  const CashRegisterArqueoPage({super.key, required this.registerId});
  @override
  ConsumerState<CashRegisterArqueoPage> createState() => _CashRegisterArqueoPageState();
}

final class _DenominationInput {
  final String type;
  final double value;
  final TextEditingController controller;
  _DenominationInput({required this.type, required this.value, required this.controller});
}

final class _CashRegisterArqueoPageState extends ConsumerState<CashRegisterArqueoPage> {
  Map<String, dynamic>? _register;
  bool _loading = true;
  String? _error;
  bool _saving = false;
  final List<_DenominationInput> _denominations = [];
  final _notesCtrl = TextEditingController();
  Map<String, dynamic>? _existingArqueo;

  static const _billValues = [1000.0, 500.0, 200.0, 100.0, 50.0, 25.0, 10.0, 5.0, 1.0];
  static const _coinValues = [1.0, 0.50, 0.25, 0.10, 0.05];

  @override
  void initState() {
    super.initState();
    for (final v in _billValues) {
      _denominations.add(_DenominationInput(type: 'bill', value: v, controller: TextEditingController()));
    }
    for (final v in _coinValues) {
      _denominations.add(_DenominationInput(type: 'coin', value: v, controller: TextEditingController()));
    }
    _load();
  }

  @override
  void dispose() {
    for (final d in _denominations) {
      d.controller.dispose();
    }
    _notesCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('cash-registers/${widget.registerId}');
      setState(() { _register = r.data as Map<String, dynamic>; _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar registro'; _loading = false; });
    }
    try {
      final dio = ref.read(dioClientProvider);
      final ar = await dio.get('cash-registers/${widget.registerId}/arqueo');
      setState(() => _existingArqueo = ar.data as Map<String, dynamic>?);
    } catch (_) {}
  }

  double get _countedTotal {
    double sum = 0;
    for (final d in _denominations) {
      final qty = int.tryParse(d.controller.text) ?? 0;
      sum += d.value * qty;
    }
    return sum;
  }

  double get _expectedBalance => ((_register?['expectedBalance'] as num?)?.toDouble() ?? 0);

  double get _difference => _countedTotal - _expectedBalance;

  Future<void> _submit() async {
    if (_countedTotal <= 0) {
      ZToast.warning(context, 'Ingrese al menos una denominación');
      return;
    }
    setState(() => _saving = true);
    try {
      final denoms = <Map<String, dynamic>>[];
      for (final d in _denominations) {
        final qty = int.tryParse(d.controller.text) ?? 0;
        if (qty > 0) {
          denoms.add({'denominationType': d.type, 'denominationValue': d.value, 'quantity': qty});
        }
      }
      final dio = ref.read(dioClientProvider);
      await dio.post('cash-registers/${widget.registerId}/arqueo', data: {
        'denominations': denoms,
        'notes': _notesCtrl.text.isEmpty ? null : _notesCtrl.text,
      });
      if (mounted) {
        ZToast.success(context, 'Arqueo realizado');
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (mounted) {
        ZToast.error(context, 'Error: $e');
        setState(() => _saving = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (_loading) return Scaffold(appBar: AppBar(title: const Text('Arqueo de Caja')), body: const Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(appBar: AppBar(title: const Text('Arqueo de Caja')), body: Center(child: Text(_error!)));

    final isOpen = _register?['status'] == 'open';

    return Scaffold(
      appBar: AppBar(title: const Text('Arqueo de Caja')),
      body: _existingArqueo != null && !isOpen
          ? _buildExistingArqueo(theme)
          : _buildForm(theme),
    );
  }

  Widget _buildExistingArqueo(ThemeData theme) {
    final a = _existingArqueo!;
    final denoms = (a['denominations'] as List?) ?? [];
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        ZCard(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Arqueo Realizado', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                const SizedBox(height: 12),
                _row('Saldo Esperado', '\$${(a['expectedBalance'] as num?)?.toStringAsFixed(2) ?? '0.00'}'),
                _row('Total Contado', '\$${(a['countedTotal'] as num?)?.toStringAsFixed(2) ?? '0.00'}'),
                _row('Diferencia', '\$${(a['difference'] as num?)?.toStringAsFixed(2) ?? '0.00'}',
                  color: ((a['difference'] as num?)?.abs() ?? 0) > 0.01 ? Colors.red : Colors.green),
                if (a['employeeName'] != null) _row('Realizado por', a['employeeName'] as String),
                if (a['notes'] != null) _row('Notas', a['notes'] as String),
                const Divider(),
                Text('Denominaciones', style: theme.textTheme.titleSmall),
                const SizedBox(height: 8),
                ...denoms.map((d) => _row(
                  '${d['denominationType'] == 'bill' ? 'Billete' : 'Moneda'} \$${(d['denominationValue'] as num).toStringAsFixed(2)}',
                  '${d['quantity']} x \$${(d['denominationValue'] as num).toStringAsFixed(2)} = \$${(d['total'] as num).toStringAsFixed(2)}',
                )),
              ],
            ),
          ),
      ],
    );
  }

  Widget _buildForm(ThemeData theme) {
    return Column(
      children: [
        ZCard(
          margin: const EdgeInsets.all(12),
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Arqueo de Caja', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                const SizedBox(height: 8),
                _row('Código', _register?['code'] ?? ''),
                _row('Saldo Esperado', '\$${_expectedBalance.toStringAsFixed(2)}', bold: true),
                const Divider(),
                Text('Denominaciones', style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
                const SizedBox(height: 8),
              ],
            ),
          ),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            children: [
              ..._buildDenominationGroup('Billetes', _denominations.where((d) => d.type == 'bill').toList(), theme),
              const SizedBox(height: 8),
              ..._buildDenominationGroup('Monedas', _denominations.where((d) => d.type == 'coin').toList(), theme),
              const SizedBox(height: 12),
              ZCard(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      _row('Total Contado', '\$${_countedTotal.toStringAsFixed(2)}', bold: true),
                      _row('Saldo Esperado', '\$${_expectedBalance.toStringAsFixed(2)}'),
                      _row('Diferencia', '\$${_difference.toStringAsFixed(2)}',
                        bold: true, color: _difference.abs() > 0.01 ? Colors.red : Colors.green),
                      const SizedBox(height: 12),
                      ZTextField(
                        controller: _notesCtrl,
                        label: 'Notas',
                        maxLines: 2,
                      ),
                      const SizedBox(height: 16),
                      ZButton(
                        text: 'Realizar Arqueo',
                        onPressed: _submit,
                        isLoading: _saving,
                        icon: Icons.check,
                      ),
                    ],
                  ),
                ),
              const SizedBox(height: 24),
            ],
          ),
        ),
      ],
    );
  }

  List<Widget> _buildDenominationGroup(String title, List<_DenominationInput> items, ThemeData theme) {
    return [
      Padding(
        padding: const EdgeInsets.only(bottom: 4),
        child: Text(title, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
      ),
      ZCard(
        child: Column(
          children: items.map((d) => Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            child: Row(
              children: [
                SizedBox(width: 100, child: Text(d.value >= 1 ? '\$${d.value.toStringAsFixed(0)}' : '\$${d.value.toStringAsFixed(2)}', style: const TextStyle(fontWeight: FontWeight.w500))),
                const Text('x', style: TextStyle(color: Colors.grey)),
                const SizedBox(width: 8),
                Expanded(
                  child: TextField(
                    controller: d.controller,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      hintText: 'Cantidad',
                      border: OutlineInputBorder(),
                      isDense: true,
                      contentPadding: EdgeInsets.symmetric(horizontal: 8, vertical: 8),
                    ),
                    onChanged: (_) => setState(() {}),
                  ),
                ),
                SizedBox(
                  width: 100,
                  child: Text(
                    '= \$${((int.tryParse(d.controller.text) ?? 0) * d.value).toStringAsFixed(2)}',
                    textAlign: TextAlign.right,
                    style: const TextStyle(fontWeight: FontWeight.w500),
                  ),
                ),
              ],
            ),
          )).toList(),
        ),
      ),
    ];
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
