import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';

class ExchangeRateFormPage extends ConsumerStatefulWidget {
  final String? exchangeRateId;
  const ExchangeRateFormPage({super.key, this.exchangeRateId});

  @override
  ConsumerState<ExchangeRateFormPage> createState() =>
      _ExchangeRateFormPageState();
}

class _ExchangeRateFormPageState extends ConsumerState<ExchangeRateFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _fromCtrl = TextEditingController();
  final _toCtrl = TextEditingController();
  final _rateCtrl = TextEditingController();
  DateTime _effectiveDate = DateTime.now();
  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.exchangeRateId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('exchange-rates/${widget.exchangeRateId}');
      final data = r.data;
      _fromCtrl.text = data['fromCurrency'] ?? '';
      _toCtrl.text = data['toCurrency'] ?? '';
      _rateCtrl.text = (data['rate'] as num?)?.toString() ?? '';
      _effectiveDate = DateTime.parse((data['effectiveDate'] as String?) ?? '');
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar tipo de cambio');
    }
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _effectiveDate,
      firstDate: DateTime(2020),
      lastDate: DateTime(2100),
    );
    if (picked != null) setState(() => _effectiveDate = picked);
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'fromCurrency': _fromCtrl.text.trim().toUpperCase(),
        'toCurrency': _toCtrl.text.trim().toUpperCase(),
        'rate': double.parse(_rateCtrl.text.trim()),
        'effectiveDate': _effectiveDate.toIso8601String(),
      };

      if (_isEditing) {
        await dio.put('exchange-rates/${widget.exchangeRateId}', data: {
          'rate': body['rate'],
          'effectiveDate': body['effectiveDate'],
        });
      } else {
        await dio.post('exchange-rates', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar tipo de cambio');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _fromCtrl.dispose();
    _toCtrl.dispose();
    _rateCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
          title:
              Text(_isEditing ? 'Editar tipo de cambio' : 'Nuevo tipo de cambio')),
      body: SingleChildScrollView(
        padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null)
                Container(
                  padding: const EdgeInsets.all(12),
                  margin: const EdgeInsets.only(bottom: 16),
                  decoration: BoxDecoration(
                    color: theme.colorScheme.errorContainer,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(_error!,
                      style: TextStyle(color: theme.colorScheme.error)),
                ),
              TextFormField(
                controller: _fromCtrl,
                decoration: const InputDecoration(
                  labelText: 'Moneda origen',
                  prefixIcon: Icon(Icons.arrow_forward),
                  hintText: 'Ej: USD',
                ),
                enabled: !_isEditing,
                validator: (v) =>
                    v == null || v.trim().isEmpty ? 'Requerido' : null,
                textCapitalization: TextCapitalization.characters,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _toCtrl,
                decoration: const InputDecoration(
                  labelText: 'Moneda destino',
                  prefixIcon: Icon(Icons.arrow_back),
                  hintText: 'Ej: NIO',
                ),
                enabled: !_isEditing,
                validator: (v) =>
                    v == null || v.trim().isEmpty ? 'Requerido' : null,
                textCapitalization: TextCapitalization.characters,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _rateCtrl,
                decoration: const InputDecoration(
                  labelText: 'Tasa de cambio',
                  prefixIcon: Icon(Icons.attach_money),
                  hintText: 'Ej: 36.5',
                ),
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                validator: (v) {
                  if (v == null || v.trim().isEmpty) return 'Requerido';
                  if (double.tryParse(v.trim()) == null) return 'Número inválido';
                  return null;
                },
              ),
              const SizedBox(height: 16),
              InkWell(
                onTap: _pickDate,
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Fecha de vigencia',
                    prefixIcon: Icon(Icons.calendar_today),
                  ),
                  child: Text(
                    '${_effectiveDate.year}-${_effectiveDate.month.toString().padLeft(2, '0')}-${_effectiveDate.day.toString().padLeft(2, '0')}',
                  ),
                ),
              ),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear tipo de cambio',
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
