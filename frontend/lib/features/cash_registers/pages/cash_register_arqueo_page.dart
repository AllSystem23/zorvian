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
    if (_loading) return Scaffold(appBar: AppBar(title: const Text('Arqueo de Caja')), body: const Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(appBar: AppBar(title: const Text('Arqueo de Caja')), body: Center(child: Text(_error!)));

    final isOpen = _register?['status'] == 'open';

    return Scaffold(
      backgroundColor: Theme.of(context).brightness == Brightness.dark ? ZColors.darkBackground : ZColors.neutral50,
      appBar: AppBar(title: const Text('Arqueo Físico de Caja')),
      body: _existingArqueo != null && !isOpen
          ? _buildExistingArqueo()
          : _buildForm(),
    );
  }

  Widget _buildExistingArqueo() {
    final a = _existingArqueo!;
    final denoms = (a['denominations'] as List?) ?? [];
    return ListView(
      padding: const EdgeInsets.all(24),
      children: [
        ZCard(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('Arqueo Realizado', style: ZTypography.titleLarge),
                    ZBadge(text: 'REGISTRADO', type: ZBadgeType.neutral),
                  ],
                ),
                const SizedBox(height: 24),
                _row('Saldo Esperado (Libros)', 'C\$ ${(a['expectedBalance'] as num?)?.toStringAsFixed(2) ?? '0.00'}'),
                _row('Total Contado (Físico)', 'C\$ ${(a['countedTotal'] as num?)?.toStringAsFixed(2) ?? '0.00'}', bold: true),
                _row('Diferencia Final', 'C\$ ${(a['difference'] as num?)?.toStringAsFixed(2) ?? '0.00'}',
                  color: ((a['difference'] as num?)?.abs() ?? 0) > 0.01 ? ZColors.danger : ZColors.success, bold: true),
                const Divider(height: 32),
                if (a['employeeName'] != null) _row('Realizado por', a['employeeName'] as String),
                if (a['notes'] != null) ...[
                  const SizedBox(height: 12),
                  Text('Notas:', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
                  Text(a['notes'] as String, style: ZTypography.bodyMedium),
                ],
                const SizedBox(height: 24),
                Text('DESGLOSE DE DENOMINACIONES', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
                const SizedBox(height: 12),
                ...denoms.map((d) => Padding(
                  padding: const EdgeInsets.only(bottom: 8.0),
                  child: Row(
                    children: [
                      Icon(d['denominationType'] == 'bill' ? Icons.money : Icons.copyright, size: 16, color: ZColors.neutral400),
                      const SizedBox(width: 12),
                      Text(
                        d['denominationType'] == 'bill' ? 'Billete C\$ ${(d['denominationValue'] as num).toStringAsFixed(2)}' : 'Moneda C\$ ${(d['denominationValue'] as num).toStringAsFixed(2)}',
                        style: ZTypography.bodyMedium,
                      ),
                      const Spacer(),
                      Text('x${d['quantity']}', style: ZTypography.monoMedium),
                      const SizedBox(width: 16),
                      Text('C\$ ${(d['total'] as num).toStringAsFixed(2)}', style: ZTypography.titleSmall),
                    ],
                  ),
                )),
              ],
            ),
          ),
      ],
    );
  }

  Widget _buildForm() {
    return Column(
      children: [
        Container(
          color: Theme.of(context).brightness == Brightness.dark ? ZColors.darkSurface : Colors.white,
          padding: const EdgeInsets.all(24),
          child: Column(
            children: [
              Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Total Contado', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
                        Text('C\$ ${_countedTotal.toStringAsFixed(2)}', style: ZTypography.displaySmall.copyWith(fontWeight: FontWeight.bold, color: ZColors.brandPrimary)),
                      ],
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                    decoration: BoxDecoration(
                      color: _difference.abs() > 0.01 ? ZColors.danger.withValues(alpha: 0.1) : ZColors.success.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(ZRadii.md),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text('DIFERENCIA', style: ZTypography.labelSmall.copyWith(color: _difference.abs() > 0.01 ? ZColors.danger : ZColors.success, fontSize: 8)),
                        Text(
                          'C\$ ${_difference.toStringAsFixed(2)}',
                          style: ZTypography.titleMedium.copyWith(fontWeight: FontWeight.bold, color: _difference.abs() > 0.01 ? ZColors.danger : ZColors.success),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.all(24),
            children: [
              _buildDenominationGroup('Billetes en Circulación', _denominations.where((d) => d.type == 'bill').toList()),
              const SizedBox(height: 24),
              _buildDenominationGroup('Monedas y Cambio', _denominations.where((d) => d.type == 'coin').toList()),
              const SizedBox(height: 24),
              ZCard(
                padding: const EdgeInsets.all(24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Observaciones del Arqueo', style: ZTypography.titleMedium),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _notesCtrl,
                        maxLines: 3,
                        decoration: const InputDecoration(
                          hintText: 'Ej: Diferencia por redondeo en cambio, billetes deteriorados...',
                        ),
                      ),
                      const SizedBox(height: 32),
                      ZButton(
                        text: 'Finalizar y Registrar Arqueo',
                        onPressed: _submit,
                        isLoading: _saving,
                        icon: Icons.check_circle_outline,
                      ),
                    ],
                  ),
                ),
              const SizedBox(height: 40),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildDenominationGroup(String title, List<_DenominationInput> items) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 4, bottom: 12),
          child: Text(title.toUpperCase(), style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500, letterSpacing: 1.2)),
        ),
        ZCard(
          padding: EdgeInsets.zero,
          child: Column(
            children: items.map((d) {
              final isLast = items.last == d;
              return Container(
                padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                decoration: BoxDecoration(
                  border: isLast ? null : Border(bottom: BorderSide(color: Theme.of(context).brightness == Brightness.dark ? ZColors.darkBorder : ZColors.border)),
                ),
                child: Row(
                  children: [
                    Icon(d.type == 'bill' ? Icons.money : Icons.copyright, size: 20, color: ZColors.neutral400),
                    const SizedBox(width: 16),
                    SizedBox(
                      width: 120,
                      child: Text(
                        'C\$ ${d.value.toStringAsFixed(d.value >= 1 ? 0 : 2)}',
                        style: ZTypography.bodyLarge.copyWith(fontWeight: FontWeight.w600),
                      ),
                    ),
                    const Spacer(),
                    SizedBox(
                      width: 100,
                      child: TextFormField(
                        controller: d.controller,
                        keyboardType: TextInputType.number,
                        textAlign: TextAlign.center,
                        decoration: InputDecoration(
                          hintText: '0',
                          isDense: true,
                          contentPadding: const EdgeInsets.symmetric(horizontal: 8, vertical: 10),
                          fillColor: Theme.of(context).brightness == Brightness.dark ? ZColors.darkBackground : ZColors.neutral50,
                        ),
                        onChanged: (_) => setState(() {}),
                      ),
                    ),
                    const SizedBox(width: 16),
                    SizedBox(
                      width: 120,
                      child: Text(
                        'C\$ ${((int.tryParse(d.controller.text) ?? 0) * d.value).toStringAsFixed(2)}',
                        textAlign: TextAlign.right,
                        style: ZTypography.titleMedium.copyWith(fontWeight: FontWeight.bold, color: ZColors.brandPrimary),
                      ),
                    ),
                  ],
                ),
              );
            }).toList(),
          ),
        ),
      ],
    );
  }

  Widget _row(String label, String value, {bool bold = false, Color? color}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral500)),
          Text(value, style: ZTypography.bodyLarge.copyWith(
            fontWeight: bold ? FontWeight.bold : FontWeight.normal,
            color: color,
          )),
        ],
      ),
    );
  }
}
