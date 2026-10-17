import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/service_payment_provider.dart';

final class ServicePaymentFormPage extends ConsumerStatefulWidget {
  /// null = crear; con valor = editar pago existente (solo borrador).
  final String? paymentId;
  const ServicePaymentFormPage({super.key, this.paymentId});
  @override
  ConsumerState<ServicePaymentFormPage> createState() => _ServicePaymentFormPageState();
}

final class _EmployeeOption {
  final String id;
  final String name;
  const _EmployeeOption(this.id, this.name);
}

final class _LineRow {
  final nameCtrl = TextEditingController(text: 'OTROS');
  final collectedCtrl = TextEditingController();
  final contractedCtrl = TextEditingController();
  final rateCtrl = TextEditingController(text: '4');
  void dispose() {
    nameCtrl.dispose();
    collectedCtrl.dispose();
    contractedCtrl.dispose();
    rateCtrl.dispose();
  }
}

final class _DeductionRow {
  String type = 'advance_first_half';
  final descCtrl = TextEditingController();
  final amountCtrl = TextEditingController();
  final agreementCtrl = TextEditingController();
  DateTime? agreementDate;
  void dispose() {
    descCtrl.dispose();
    amountCtrl.dispose();
    agreementCtrl.dispose();
  }
}

final class _ServicePaymentFormPageState extends ConsumerState<ServicePaymentFormPage> {
  static const _agreementTypes = {
    'advance_first_half', 'store_credit', 'salary_advance', 'cash_loan',
  };

  static const _deductionLabels = <String, String>{
    'advance_first_half': 'Adelanto 1ª Quincena',
    'store_credit': 'Crédito Interno Tienda',
    'salary_advance': 'Adelanto Salarial',
    'cash_loan': 'Préstamo en Efectivo',
    'inss': 'INSS',
    'other': 'Otra Deducción',
  };

  static const _monthNames = [
    'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
    'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre',
  ];

  bool _loading = true;
  bool _saving = false;
  String? _error;
  String? _status; // solo draft editable

  List<_EmployeeOption> _employees = const [];
  String? _employeeId;

  int _year = DateTime.now().year;
  int _month = DateTime.now().month;
  int _half = 1;
  DateTime _paymentDate = DateTime.now();

  String _goalMode = 'total';
  final _bonusCtrl = TextEditingController(text: '1000');
  final _otherCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();

  final List<_LineRow> _lines = [_LineRow()];
  final List<_DeductionRow> _deductions = [];

  @override
  void initState() {
    super.initState();
    _bootstrap();
  }

  @override
  void dispose() {
    _bonusCtrl.dispose();
    _otherCtrl.dispose();
    _notesCtrl.dispose();
    for (final r in _lines) {
      r.dispose();
    }
    for (final r in _deductions) {
      r.dispose();
    }
    super.dispose();
  }

  Future<void> _bootstrap() async {
    try {
      final dio = ref.read(dioClientProvider);
      final empRes = await dio.get('employees', params: {'page': 1, 'pageSize': 500});
      final raw = empRes.data['items'] as List? ?? empRes.data as List? ?? [];
      _employees = [
        for (final e in raw)
          if ((e['collaboratorType'] ?? '') == 'service_provider')
            _EmployeeOption(
              e['id'] as String,
              '${e['firstName'] ?? ''} ${e['lastName'] ?? ''}'.trim(),
            ),
      ];

      if (widget.paymentId != null) {
        final r = await dio.get('service-provider-payments/${widget.paymentId}');
        _populateFrom(r.data);
      }

      setState(() => _loading = false);
    } catch (e) {
      setState(() {
        _error = 'Error al cargar datos del formulario';
        _loading = false;
      });
    }
  }

  void _populateFrom(Map<String, dynamic> j) {
    _status = j['status'] as String?;
    _employeeId = j['employeeId'] as String?;
    _year = (j['periodYear'] as num?)?.toInt() ?? _year;
    _month = (j['periodMonth'] as num?)?.toInt() ?? _month;
    _half = (j['periodHalf'] as num?)?.toInt() ?? 1;
    _goalMode = j['goalEvaluationMode'] as String? ?? 'total';
    _bonusCtrl.text = _fmt((j['goalBonusAmount'] as num?)?.toDouble() ?? 0);
    _otherCtrl.text = _fmt((j['otherIncome'] as num?)?.toDouble() ?? 0);
    _notesCtrl.text = j['notes'] as String? ?? '';
    final dateStr = j['paymentDate'] as String?;
    if (dateStr != null && dateStr.length >= 10) {
      _paymentDate = DateTime.tryParse(dateStr.substring(0, 10)) ?? _paymentDate;
    }

    _lines.clear();
    for (final l in (j['lines'] as List? ?? [])) {
      final row = _LineRow();
      row.nameCtrl.text = l['lineName'] as String? ?? '';
      row.collectedCtrl.text = _fmt((l['collectedAmount'] as num?)?.toDouble() ?? 0);
      row.contractedCtrl.text = _fmt((l['contractedAmount'] as num?)?.toDouble() ?? 0);
      row.rateCtrl.text = _fmt((l['commissionRate'] as num?)?.toDouble() ?? 0);
      _lines.add(row);
    }
    if (_lines.isEmpty) _lines.add(_LineRow());

    _deductions.clear();
    for (final d in (j['deductions'] as List? ?? [])) {
      final row = _DeductionRow();
      row.type = d['deductionType'] as String? ?? 'other';
      row.descCtrl.text = d['description'] as String? ?? '';
      row.amountCtrl.text = _fmt((d['amount'] as num?)?.toDouble() ?? 0);
      row.agreementCtrl.text = d['agreementReference'] as String? ?? '';
      final aDate = d['agreementDate'] as String?;
      if (aDate != null && aDate.length >= 10) {
        row.agreementDate = DateTime.tryParse(aDate.substring(0, 10));
      }
      _deductions.add(row);
    }
  }

  String _fmt(double v) => v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toStringAsFixed(2);

  double? _num(String raw) {
    final v = raw.replaceAll(',', '').replaceAll(' ', '');
    if (v.isEmpty) return null;
    return double.tryParse(v);
  }

  // ── Estimado en vivo (espejo del cálculo del servidor) ──

  ({double collected, double contracted, double commission, bool goalMet,
      double bonus, double accumulated, double deductions, double net}) _estimate() {
    double collected = 0, contracted = 0, commission = 0;
    var anyContract = false;
    var allLinesMet = true;
    for (final r in _lines) {
      final c = _num(r.collectedCtrl.text) ?? 0;
      final t = _num(r.contractedCtrl.text) ?? 0;
      final rate = _num(r.rateCtrl.text) ?? 0;
      collected += c;
      contracted += t;
      commission += (c * rate / 100);
      if (t > 0) {
        anyContract = true;
        if (c < t) allLinesMet = false;
      }
    }
    final goalMet = _goalMode == 'individual'
        ? (anyContract && allLinesMet)
        : (contracted > 0 && collected >= contracted);
    final bonus = goalMet ? (_num(_bonusCtrl.text) ?? 0.0) : 0.0;
    final other = _num(_otherCtrl.text) ?? 0.0;
    final accumulated = bonus + commission + other;
    double deductions = 0;
    for (final d in _deductions) {
      deductions += _num(d.amountCtrl.text) ?? 0;
    }
    return (
      collected: collected,
      contracted: contracted,
      commission: commission,
      goalMet: goalMet,
      bonus: bonus,
      accumulated: accumulated,
      deductions: deductions,
      net: accumulated - deductions,
    );
  }

  // ── Guardar ──

  String? _validate() {
    if (_employeeId == null) return 'Seleccione el prestador de servicios.';
    final validLines = _lines
        .where((l) => l.nameCtrl.text.trim().isNotEmpty && _num(l.collectedCtrl.text) != null)
        .length;
    if (validLines == 0) return 'Defina al menos una línea de recaudo con su nombre y monto.';
    for (final l in _lines) {
      if (l.nameCtrl.text.trim().isEmpty) continue;
      final rate = _num(l.rateCtrl.text);
      if (rate == null || rate < 0 || rate > 100) {
        return 'La comisión de "${l.nameCtrl.text.trim()}" debe estar entre 0 y 100%.';
      }
      if ((_num(l.collectedCtrl.text) ?? -1) < 0) {
        return 'El recaudo de "${l.nameCtrl.text.trim()}" no puede ser negativo.';
      }
      if ((_num(l.contractedCtrl.text) ?? 0) < 0) {
        return 'El monto en contrato de "${l.nameCtrl.text.trim()}" no puede ser negativo.';
      }
    }
    for (final d in _deductions) {
      final raw = d.amountCtrl.text.trim();
      if (raw.isEmpty) continue; // fila sin monto: se ignora en el payload
      final amount = _num(raw);
      if (amount == null || amount <= 0) {
        return 'Toda deducción debe ser un monto mayor a cero.';
      }
      if (_agreementTypes.contains(d.type) && d.agreementCtrl.text.trim().isEmpty) {
        return '"${_deductionLabels[d.type]}" exige acuerdo previo: indique la referencia del acuerdo.';
      }
    }
    return null;
  }

  Map<String, dynamic> _payload() => {
    'employeeId': _employeeId,
    'periodYear': _year,
    'periodMonth': _month,
    'periodHalf': _half,
    'paymentDate': _dateOnly(_paymentDate),
    'goalEvaluationMode': _goalMode,
    'goalBonusAmount': _num(_bonusCtrl.text) ?? 0,
    'otherIncome': _num(_otherCtrl.text) ?? 0,
    'notes': _notesCtrl.text.trim().isEmpty ? null : _notesCtrl.text.trim(),
    'lines': [
      for (final l in _lines)
        if (l.nameCtrl.text.trim().isNotEmpty)
          ServicePaymentLineInput(
            lineName: l.nameCtrl.text.trim(),
            collectedAmount: _num(l.collectedCtrl.text) ?? 0,
            contractedAmount: _num(l.contractedCtrl.text) ?? 0,
            commissionRate: _num(l.rateCtrl.text) ?? 0,
          ).toJson(),
    ],
    'deductions': [
      for (final d in _deductions)
        if ((_num(d.amountCtrl.text) ?? 0) > 0)
          ServicePaymentDeductionInput(
            deductionType: d.type,
            description: d.descCtrl.text.trim().isEmpty ? null : d.descCtrl.text.trim(),
            amount: _num(d.amountCtrl.text) ?? 0,
            agreementReference:
                d.agreementCtrl.text.trim().isEmpty ? null : d.agreementCtrl.text.trim(),
            agreementDate: d.agreementDate == null ? null : _dateOnly(d.agreementDate!),
          ).toJson(),
    ],
  };

  String _dateOnly(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  Future<void> _save() async {
    final problem = _validate();
    if (problem != null) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(problem), backgroundColor: Colors.orange),
      );
      return;
    }
    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      final isEdit = widget.paymentId != null;
      final r = isEdit
          ? await dio.put(
              'service-provider-payments/${widget.paymentId}',
              data: _payload(),
            )
          : await dio.post('service-provider-payments', data: _payload());
      final id = r.data['id'] as String;
      ref.invalidate(servicePaymentProvider);
      if (!mounted) return;
      context.pushReplacement('/service-payments/$id');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(isEdit ? 'Pago actualizado' : 'Pago creado en borrador'),
          backgroundColor: Colors.green,
        ),
      );
    } on DioException catch (e) {
      final data = e.response?.data;
      String msg = 'Error al guardar el pago';
      if (data is Map) {
        msg = (data['detail'] ?? data['error'] ?? msg)?.toString() ?? msg;
      }
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(msg), backgroundColor: Colors.red));
        setState(() => _saving = false);
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error al guardar el pago'), backgroundColor: Colors.red),
        );
        setState(() => _saving = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (_loading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (_error != null) {
      return Scaffold(
        body: Center(child: Text(_error!, style: TextStyle(color: theme.colorScheme.error))),
      );
    }

    final readonly = _status != null && _status != 'draft';
    final est = _estimate();

    return Scaffold(
      appBar: AppBar(
        title: Text(widget.paymentId == null ? 'Nuevo Pago a Prestador' : 'Editar Pago'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 900),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (readonly) ...[
                  ZCard(
                    padding: const EdgeInsets.all(12),
                    child: Row(
                      children: [
                        const Icon(Icons.lock_outline, color: Colors.orange),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            'Solo se pueden editar pagos en borrador; este pago está '
                            'en estado "$_status".',
                            style: const TextStyle(color: Colors.orange),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 12),
                ],
                ZCard(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Prestador y Período', style: theme.textTheme.titleMedium),
                      const SizedBox(height: 12),
                      if (_employees.isEmpty)
                        const Text(
                          'No hay prestadores de servicios registrados. '
                          'Cree un colaborador con tipo "Prestador de servicios" en Capital Humano.',
                          style: TextStyle(color: Colors.orange),
                        )
                      else ...[
                        DropdownButtonFormField<String?>(
                          initialValue: _employeeId,
                          isExpanded: true,
                          decoration: const InputDecoration(
                            labelText: 'Prestador de servicios *',
                            border: OutlineInputBorder(),
                          ),
                          items: [
                            const DropdownMenuItem<String?>(
                                value: null, child: Text('Seleccione...')),
                            ..._employees.map((e) => DropdownMenuItem<String?>(
                                  value: e.id,
                                  child: Text(e.name),
                                )),
                          ],
                          onChanged: (v) => setState(() => _employeeId = v),
                        ),
                        const SizedBox(height: 16),
                      ],
                      Row(
                        children: [
                          Expanded(
                            child: DropdownButtonFormField<int>(
                              initialValue: _year,
                              decoration: const InputDecoration(
                                labelText: 'Año *',
                                border: OutlineInputBorder(),
                                isDense: true,
                              ),
                              items: [
                                for (final y in [
                                  DateTime.now().year - 1,
                                  DateTime.now().year,
                                  DateTime.now().year + 1,
                                ])
                                  DropdownMenuItem(value: y, child: Text('$y')),
                              ],
                              onChanged: (v) => setState(() => _year = v ?? _year),
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: DropdownButtonFormField<int>(
                              initialValue: _month,
                              decoration: const InputDecoration(
                                labelText: 'Mes *',
                                border: OutlineInputBorder(),
                                isDense: true,
                              ),
                              items: [
                                for (var i = 1; i <= 12; i++)
                                  DropdownMenuItem(value: i, child: Text(_monthNames[i - 1])),
                              ],
                              onChanged: (v) => setState(() => _month = v ?? _month),
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: SegmentedButton<int>(
                              segments: const [
                                ButtonSegment(value: 1, label: Text('1ª quincena')),
                                ButtonSegment(value: 2, label: Text('2ª quincena')),
                              ],
                              selected: {_half},
                              onSelectionChanged: (s) => setState(() => _half = s.first),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),
                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton.icon(
                              icon: const Icon(Icons.calendar_today_outlined, size: 16),
                              label: Text(
                                'Fecha de pago: ${_paymentDate.day.toString().padLeft(2, '0')}/'
                                '${_paymentDate.month.toString().padLeft(2, '0')}/${_paymentDate.year}',
                              ),
                              onPressed: readonly
                                  ? null
                                  : () async {
                                      final picked = await showDatePicker(
                                        context: context,
                                        initialDate: _paymentDate,
                                        firstDate: DateTime(2024),
                                        lastDate: DateTime(2031),
                                      );
                                      if (picked != null) {
                                        setState(() => _paymentDate = picked);
                                      }
                                    },
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                ZCard(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Líneas de Recaudo y Comisión', style: theme.textTheme.titleMedium),
                      const SizedBox(height: 4),
                      const Text(
                        'La comisión se calcula por el servidor: recaudo × tasa %. '
                        'Ej: 495,782 × 4% = 19,831.28',
                        style: TextStyle(fontSize: 12, color: Colors.grey),
                      ),
                      const SizedBox(height: 12),
                      for (var i = 0; i < _lines.length; i++)
                        _lineRow(theme, _lines[i], i, readonly),
                      const SizedBox(height: 8),
                      Align(
                        alignment: Alignment.centerLeft,
                        child: TextButton.icon(
                          icon: const Icon(Icons.add),
                          label: const Text('Agregar línea'),
                          onPressed: readonly
                              ? null
                              : () => setState(() => _lines.add(_LineRow())),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                ZCard(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Meta e Incentivo', style: theme.textTheme.titleMedium),
                      const SizedBox(height: 12),
                      SegmentedButton<String>(
                        segments: const [
                          ButtonSegment(
                            value: 'total',
                            label: Text('Meta por TOTAL'),
                            icon: Icon(Icons.calculate_outlined, size: 16),
                          ),
                          ButtonSegment(
                            value: 'individual',
                            label: Text('Meta por LÍNEA'),
                            icon: Icon(Icons.checklist, size: 16),
                          ),
                        ],
                        selected: {_goalMode},
                        onSelectionChanged: readonly
                            ? null
                            : (s) => setState(() => _goalMode = s.first),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        _goalMode == 'total'
                            ? 'La meta se cumple si el recaudo TOTAL alcanza el contrato TOTAL.'
                            : 'La meta se cumple solo si TODAS las líneas alcanzan su monto en contrato.',
                        style: const TextStyle(fontSize: 12, color: Colors.grey),
                      ),
                      const SizedBox(height: 16),
                      Row(
                        children: [
                          Expanded(
                            child: TextFormField(
                              controller: _bonusCtrl,
                              enabled: !readonly,
                              keyboardType: const TextInputType.numberWithOptions(decimal: true),
                              decoration: const InputDecoration(
                                labelText: 'Bono fijo por meta (si se cumple)',
                                border: OutlineInputBorder(),
                                isDense: true,
                              ),
                              onChanged: (_) => setState(() {}),
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: TextFormField(
                              controller: _otherCtrl,
                              enabled: !readonly,
                              keyboardType: const TextInputType.numberWithOptions(decimal: true),
                              decoration: const InputDecoration(
                                labelText: 'Otros ingresos',
                                border: OutlineInputBorder(),
                                isDense: true,
                              ),
                              onChanged: (_) => setState(() {}),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _notesCtrl,
                        enabled: !readonly,
                        maxLines: 2,
                        decoration: const InputDecoration(
                          labelText: 'Notas',
                          border: OutlineInputBorder(),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                ZCard(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Deducciones (con acuerdo previo)', style: theme.textTheme.titleMedium),
                      const SizedBox(height: 12),
                      for (var i = 0; i < _deductions.length; i++)
                        _deductionRow(theme, _deductions[i], i, readonly),
                      if (_deductions.isEmpty)
                        const Text(
                          'Sin deducciones. Los adelantos, créditos de tienda y préstamos '
                          'requieren referencia del acuerdo firmado.',
                          style: TextStyle(fontSize: 12, color: Colors.grey),
                        ),
                      const SizedBox(height: 8),
                      Align(
                        alignment: Alignment.centerLeft,
                        child: TextButton.icon(
                          icon: const Icon(Icons.remove_shopping_cart_outlined),
                          label: const Text('Agregar deducción'),
                          onPressed: readonly
                              ? null
                              : () => setState(() => _deductions.add(_DeductionRow())),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                ZCard(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Estimado del Comprobante', style: theme.textTheme.titleMedium),
                      const SizedBox(height: 12),
                      _estRow('Recaudo en efectivo', est.collected),
                      _estRow('Total en contrato', est.contracted),
                      _estRow('Comisiones', est.commission),
                      Padding(
                        padding: const EdgeInsets.symmetric(vertical: 4),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            const Text('Meta cumplida', style: TextStyle(fontWeight: FontWeight.w600)),
                            Text(
                              est.goalMet ? 'SÍ (+${est.bonus.toStringAsFixed(2)})' : 'NO',
                              style: TextStyle(
                                fontWeight: FontWeight.bold,
                                color: est.goalMet ? Colors.green : Colors.red,
                              ),
                            ),
                          ],
                        ),
                      ),
                      _estRow('Total acumulado', est.accumulated),
                      _estRow('Total deducciones', -est.deductions),
                      const Divider(height: 16),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text('NETO A RECIBIR',
                              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                          Text(
                            est.net.toStringAsFixed(2),
                            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 20),
                Row(
                  children: [
                    Expanded(
                      child: ZButton(
                        text: 'Cancelar',
                        type: ZButtonType.secondary,
                        onPressed: () => context.pop(),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: ZButton(
                        text: _saving
                            ? 'Guardando...'
                            : (widget.paymentId == null ? 'Crear Pago' : 'Guardar Cambios'),
                        onPressed: readonly || _saving ? () {} : _save,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 40),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _estRow(String label, double value) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label),
            Text(value.toStringAsFixed(2), style: const TextStyle(fontWeight: FontWeight.w600)),
          ],
        ),
      );

  Widget _lineRow(ThemeData theme, _LineRow row, int index, bool readonly) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 130,
            child: TextFormField(
              controller: row.nameCtrl,
              enabled: !readonly,
              decoration: const InputDecoration(
                labelText: 'Línea',
                hintText: 'OTROS / TELÉFONO',
                border: OutlineInputBorder(),
                isDense: true,
              ),
              onChanged: (_) => setState(() {}),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: TextFormField(
              controller: row.collectedCtrl,
              enabled: !readonly,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(
                labelText: 'Recaudo en efectivo',
                border: OutlineInputBorder(),
                isDense: true,
              ),
              onChanged: (_) => setState(() {}),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: TextFormField(
              controller: row.contractedCtrl,
              enabled: !readonly,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(
                labelText: 'Monto en contrato',
                border: OutlineInputBorder(),
                isDense: true,
              ),
              onChanged: (_) => setState(() {}),
            ),
          ),
          const SizedBox(width: 8),
          SizedBox(
            width: 90,
            child: TextFormField(
              controller: row.rateCtrl,
              enabled: !readonly,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(
                labelText: 'Comisión %',
                border: OutlineInputBorder(),
                isDense: true,
              ),
              onChanged: (_) => setState(() {}),
            ),
          ),
          const SizedBox(width: 8),
          SizedBox(
            width: 110,
            child: Padding(
              padding: const EdgeInsets.only(top: 20),
              child: Text(
                '= ${((_num(row.collectedCtrl.text) ?? 0) * (_num(row.rateCtrl.text) ?? 0) / 100).toStringAsFixed(2)}',
                style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 12),
                textAlign: TextAlign.end,
              ),
            ),
          ),
          if (!readonly)
            SizedBox(
              width: 36,
              child: IconButton(
                visualDensity: VisualDensity.compact,
                icon: const Icon(Icons.close, size: 18, color: Colors.red),
                tooltip: 'Quitar línea',
                onPressed: _lines.length <= 1
                    ? null
                    : () => setState(() {
                          row.dispose();
                          _lines.removeAt(index);
                        }),
              ),
            ),
        ],
      ),
    );
  }

  Widget _deductionRow(ThemeData theme, _DeductionRow row, int index, bool readonly) {
    final needsAgreement = _agreementTypes.contains(row.type);
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Column(
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              SizedBox(
                width: 190,
                child: DropdownButtonFormField<String>(
                  initialValue: row.type,
                  isExpanded: true,
                  decoration: const InputDecoration(
                    labelText: 'Tipo',
                    border: OutlineInputBorder(),
                    isDense: true,
                  ),
                  items: [
                    for (final e in _deductionLabels.entries)
                      DropdownMenuItem(value: e.key, child: Text(e.value, overflow: TextOverflow.ellipsis)),
                  ],
                  onChanged: readonly
                      ? null
                      : (v) => setState(() => row.type = v ?? row.type),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: TextFormField(
                  controller: row.descCtrl,
                  enabled: !readonly,
                  decoration: const InputDecoration(
                    labelText: 'Descripción (ej. LAVADORA FRIGIDAIRE SALDO 4692)',
                    border: OutlineInputBorder(),
                    isDense: true,
                  ),
                ),
              ),
              const SizedBox(width: 8),
              SizedBox(
                width: 130,
                child: TextFormField(
                  controller: row.amountCtrl,
                  enabled: !readonly,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  decoration: const InputDecoration(
                    labelText: 'Monto *',
                    border: OutlineInputBorder(),
                    isDense: true,
                  ),
                  onChanged: (_) => setState(() {}),
                ),
              ),
              if (!readonly)
                SizedBox(
                  width: 36,
                  child: IconButton(
                    visualDensity: VisualDensity.compact,
                    icon: const Icon(Icons.close, size: 18, color: Colors.red),
                    tooltip: 'Quitar deducción',
                    onPressed: () => setState(() {
                          row.dispose();
                          _deductions.removeAt(index);
                        }),
                  ),
                ),
            ],
          ),
          if (needsAgreement)
            Padding(
              padding: const EdgeInsets.only(top: 6),
              child: Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      controller: row.agreementCtrl,
                      enabled: !readonly,
                      decoration: const InputDecoration(
                        labelText: 'Referencia del acuerdo previo *',
                        hintText: 'Contrato, acta o nota firmada',
                        border: OutlineInputBorder(),
                        isDense: true,
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  OutlinedButton(
                    onPressed: readonly
                        ? null
                        : () async {
                            final picked = await showDatePicker(
                              context: context,
                              initialDate: row.agreementDate ?? DateTime.now(),
                              firstDate: DateTime(2020),
                              lastDate: DateTime(2031),
                            );
                            if (picked != null) setState(() => row.agreementDate = picked);
                          },
                    child: Text(
                      row.agreementDate == null
                          ? 'Fecha acuerdo'
                          : '${row.agreementDate!.day.toString().padLeft(2, '0')}/'
                              '${row.agreementDate!.month.toString().padLeft(2, '0')}/'
                              '${row.agreementDate!.year}',
                      style: const TextStyle(fontSize: 12),
                    ),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
