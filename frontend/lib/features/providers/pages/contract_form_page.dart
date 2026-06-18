import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/provider_state.dart';

final class ContractFormPage extends ConsumerStatefulWidget {
  final String? contractId;
  final String? providerId;
  const ContractFormPage({super.key, this.contractId, this.providerId});
  @override
  ConsumerState<ContractFormPage> createState() => _ContractFormPageState();
}

final class _ContractFormPageState extends ConsumerState<ContractFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _contractNameCtrl = TextEditingController();
  final _contractNumberCtrl = TextEditingController();
  final _scopeCtrl = TextEditingController();
  final _amountCtrl = TextEditingController();
  final _paymentTermsCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  String _currency = 'NIO';
  DateTime _startDate = DateTime.now();
  DateTime? _endDate;
  bool _loading = false;
  String? _error;
  String? _selectedProviderId;
  bool get _isEditing => widget.contractId != null;

  @override
  void initState() {
    super.initState();
    _selectedProviderId = widget.providerId;
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('providers/contracts/${widget.contractId}');
      final d = r.data;
      _contractNameCtrl.text = d['contractName'] ?? '';
      _contractNumberCtrl.text = d['contractNumber'] ?? '';
      _scopeCtrl.text = d['scope'] ?? '';
      _amountCtrl.text = (d['totalContractAmount'] ?? 0).toString();
      _paymentTermsCtrl.text = d['paymentTerms'] ?? '';
      _notesCtrl.text = d['notes'] ?? '';
      _currency = d['currency'] ?? 'NIO';
      _selectedProviderId = d['serviceProviderId'];
      if (d['startDate'] != null) _startDate = DateTime.parse(d['startDate']);
      if (d['endDate'] != null) _endDate = DateTime.parse(d['endDate']);
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar contrato');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedProviderId == null) {
      setState(() => _error = 'Debe seleccionar un prestador');
      return;
    }
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'serviceProviderId': _selectedProviderId,
        'contractNumber': _contractNumberCtrl.text.trim(),
        'contractName': _contractNameCtrl.text.trim(),
        'scope': _scopeCtrl.text.trim().isEmpty ? null : _scopeCtrl.text.trim(),
        'totalContractAmount': double.tryParse(_amountCtrl.text) ?? 0,
        'currency': _currency,
        'paymentTerms': _paymentTermsCtrl.text.trim().isEmpty ? null : _paymentTermsCtrl.text.trim(),
        'startDate': _startDate.toIso8601String().split('T')[0],
        'endDate': _endDate?.toIso8601String().split('T')[0],
        'notes': _notesCtrl.text.trim().isEmpty ? null : _notesCtrl.text.trim(),
      };
      if (_isEditing) {
        await dio.put('providers/contracts/${widget.contractId}', data: body);
      } else {
        await dio.post('providers/contracts', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar contrato');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _contractNameCtrl.dispose();
    _contractNumberCtrl.dispose();
    _scopeCtrl.dispose();
    _amountCtrl.dispose();
    _paymentTermsCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar Contrato' : 'Nuevo Contrato')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
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
                    color: Theme.of(context).colorScheme.errorContainer,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                ),
              if (widget.providerId == null)
                ...[
                  _ProviderDropdown(
                    value: _selectedProviderId,
                    onChanged: (v) => setState(() => _selectedProviderId = v),
                  ),
                  const SizedBox(height: ZSpacing.md),
                ],
              TextFormField(
                controller: _contractNumberCtrl,
                decoration: const InputDecoration(
                  labelText: 'Número de Contrato',
                  prefixIcon: Icon(Icons.tag),
                ),
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: ZSpacing.md),
              TextFormField(
                controller: _contractNameCtrl,
                decoration: const InputDecoration(
                  labelText: 'Nombre del Contrato',
                  prefixIcon: Icon(Icons.description),
                ),
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: ZSpacing.md),
              TextFormField(
                controller: _scopeCtrl,
                decoration: const InputDecoration(
                  labelText: 'Alcance',
                  prefixIcon: Icon(Icons.explore),
                ),
                maxLines: 3,
              ),
              const SizedBox(height: ZSpacing.md),
              Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      controller: _amountCtrl,
                      decoration: const InputDecoration(
                        labelText: 'Monto Total',
                        prefixIcon: Icon(Icons.attach_money),
                      ),
                      keyboardType: TextInputType.number,
                      validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
                    ),
                  ),
                  const SizedBox(width: ZSpacing.md),
                  Expanded(
                    child: DropdownButtonFormField<String>(
                      initialValue: _currency,
                      decoration: const InputDecoration(
                        labelText: 'Moneda',
                        prefixIcon: Icon(Icons.monetization_on),
                      ),
                      items: const [
                        DropdownMenuItem(value: 'NIO', child: Text('NIO')),
                        DropdownMenuItem(value: 'USD', child: Text('USD')),
                      ],
                      onChanged: (v) => setState(() => _currency = v ?? 'NIO'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: ZSpacing.md),
              TextFormField(
                controller: _paymentTermsCtrl,
                decoration: const InputDecoration(
                  labelText: 'Términos de Pago',
                  prefixIcon: Icon(Icons.payment),
                ),
              ),
              const SizedBox(height: ZSpacing.md),
              InkWell(
                onTap: () async {
                  final date = await showDatePicker(
                    context: context,
                    initialDate: _startDate,
                    firstDate: DateTime.now().subtract(const Duration(days: 365)),
                    lastDate: DateTime.now().add(const Duration(days: 365 * 5)),
                  );
                  if (date != null) {
                    setState(() => _startDate = date);
                  }
                },
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Fecha de Inicio',
                    prefixIcon: Icon(Icons.calendar_today),
                  ),
                  child: Text('${_startDate.day}/${_startDate.month}/${_startDate.year}'),
                ),
              ),
              const SizedBox(height: ZSpacing.md),
              InkWell(
                onTap: () async {
                  final date = await showDatePicker(
                    context: context,
                    initialDate: _endDate ?? _startDate.add(const Duration(days: 365)),
                    firstDate: _startDate,
                    lastDate: _startDate.add(const Duration(days: 365 * 5)),
                  );
                  if (date != null) {
                    setState(() => _endDate = date);
                  }
                },
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Fecha de Fin (opcional)',
                    prefixIcon: Icon(Icons.calendar_today),
                  ),
                  child: Text(_endDate != null
                      ? '${_endDate!.day}/${_endDate!.month}/${_endDate!.year}'
                      : 'Indefinido'),
                ),
              ),
              const SizedBox(height: ZSpacing.md),
              TextFormField(
                controller: _notesCtrl,
                decoration: const InputDecoration(
                  labelText: 'Notas',
                  prefixIcon: Icon(Icons.notes),
                ),
                maxLines: 3,
              ),
              const SizedBox(height: ZSpacing.xl),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear Contrato',
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

final class _ProviderDropdown extends ConsumerWidget {
  final String? value;
  final ValueChanged<String?> onChanged;
  const _ProviderDropdown({required this.value, required this.onChanged});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final providersAsync = ref.watch(serviceProvidersProvider);
    return providersAsync.when(
      data: (providers) => DropdownButtonFormField<String>(
        initialValue: value,
        decoration: const InputDecoration(
          labelText: 'Prestador',
          prefixIcon: Icon(Icons.business),
        ),
        items: providers.map((p) => DropdownMenuItem(
          value: p.id,
          child: Text(p.businessName),
        )).toList(),
        onChanged: onChanged,
        validator: (v) => v == null ? 'Seleccione un prestador' : null,
      ),
      loading: () => const SizedBox(height: 56, child: Center(child: LinearProgressIndicator())),
      error: (_, _) => const Text('Error al cargar prestadores'),
    );
  }
}
