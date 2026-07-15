import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/supplier_credit_note_provider.dart';

final class SupplierCreditNoteFormPage extends ConsumerStatefulWidget {
  final String? purchaseId;
  const SupplierCreditNoteFormPage({super.key, this.purchaseId});

  @override
  ConsumerState<SupplierCreditNoteFormPage> createState() => _SupplierCreditNoteFormPageState();
}

final class _SupplierCreditNoteFormPageState extends ConsumerState<SupplierCreditNoteFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _subtotalCtrl = TextEditingController();
  final _taxCtrl = TextEditingController();
  final _totalCtrl = TextEditingController();
  final _reasonCtrl = TextEditingController();
  String? _selectedSupplierId;
  String? _selectedSupplierName;
  DateTime _creditNoteDate = DateTime.now();
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    if (widget.purchaseId != null) {
      Future.microtask(() => _loadPurchaseSupplier(widget.purchaseId!));
    }
  }

  Future<void> _loadPurchaseSupplier(String purchaseId) async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('purchases/$purchaseId');
      final d = r.data;
      setState(() {
        _selectedSupplierId = d['supplierId'] as String?;
        _selectedSupplierName = d['supplierName'] as String?;
      });
    } catch (_) {}
  }

  void _updateTotal() {
    final subtotal = double.tryParse(_subtotalCtrl.text) ?? 0;
    final tax = double.tryParse(_taxCtrl.text) ?? 0;
    _totalCtrl.text = (subtotal + tax).toStringAsFixed(2);
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedSupplierId == null) {
      setState(() => _error = 'Debe seleccionar un proveedor');
      return;
    }

    setState(() { _loading = true; _error = null; });
    try {
      await ref.read(supplierCreditNoteProvider.notifier).create(
        supplierId: _selectedSupplierId!,
        creditNoteDate: _creditNoteDate.toIso8601String().substring(0, 10),
        reason: _reasonCtrl.text.trim().isEmpty ? null : _reasonCtrl.text.trim(),
        subtotal: double.tryParse(_subtotalCtrl.text) ?? 0,
        tax: double.tryParse(_taxCtrl.text) ?? 0,
        total: double.tryParse(_totalCtrl.text) ?? 0,
      );
      if (mounted) {
        ZToast.success(context, 'Nota de crédito registrada exitosamente');
        context.pop(true);
      }
    } catch (e) {
      setState(() => _error = 'Error al registrar nota de crédito: $e');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _subtotalCtrl.dispose();
    _taxCtrl.dispose();
    _totalCtrl.dispose();
    _reasonCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
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
                  child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
                ),
              _SupplierSelector(
                selectedId: _selectedSupplierId,
                onChanged: (id) => setState(() {
                  _selectedSupplierId = id;
                  _selectedSupplierName = null;
                }),
              ),
              const SizedBox(height: 16),
              if (_selectedSupplierName != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: Text('Proveedor: $_selectedSupplierName',
                    style: TextStyle(fontWeight: FontWeight.w600, color: theme.colorScheme.primary)),
                ),
              InkWell(
                onTap: () async {
                  final picked = await showDatePicker(
                    context: context,
                    initialDate: _creditNoteDate,
                    firstDate: DateTime(2020),
                    lastDate: DateTime.now(),
                    locale: const Locale('es'),
                  );
                  if (picked != null) setState(() => _creditNoteDate = picked);
                },
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Fecha de Nota de Crédito',
                    prefixIcon: Icon(Icons.calendar_today_outlined),
                  ),
                  child: Text(
                    '${_creditNoteDate.day}/${_creditNoteDate.month}/${_creditNoteDate.year}',
                  ),
                ),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _reasonCtrl,
                decoration: const InputDecoration(
                  labelText: 'Motivo (opcional)',
                  prefixIcon: Icon(Icons.description),
                ),
                maxLines: 2,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _subtotalCtrl,
                decoration: const InputDecoration(
                  labelText: 'Subtotal',
                  prefixIcon: Icon(Icons.attach_money),
                ),
                keyboardType: TextInputType.number,
                onChanged: (_) => _updateTotal(),
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _taxCtrl,
                decoration: const InputDecoration(
                  labelText: 'IVA/Impuesto',
                  prefixIcon: Icon(Icons.receipt_outlined),
                ),
                keyboardType: TextInputType.number,
                onChanged: (_) => _updateTotal(),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _totalCtrl,
                decoration: const InputDecoration(
                  labelText: 'Total',
                  prefixIcon: Icon(Icons.calculate),
                ),
                keyboardType: TextInputType.number,
                readOnly: true,
                style: TextStyle(fontWeight: FontWeight.bold, color: theme.colorScheme.primary),
              ),
              const SizedBox(height: 24),
              ZButton(
                text: 'Registrar Nota de Crédito',
                onPressed: _save,
                isLoading: _loading,
                icon: Icons.check,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

final class _SupplierSelector extends ConsumerStatefulWidget {
  final String? selectedId;
  final ValueChanged<String?> onChanged;
  const _SupplierSelector({
    required this.selectedId,
    required this.onChanged,
  });

  @override
  ConsumerState<_SupplierSelector> createState() => _SupplierSelectorState();
}

final class _SupplierSelectorState extends ConsumerState<_SupplierSelector> {
  List<Map<String, dynamic>> _suppliers = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('suppliers');
      _suppliers = ((r.data as List?) ?? []).cast<Map<String, dynamic>>();
    } catch (_) {}
    setState(() => _loading = false);
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return const SizedBox(height: 80, child: Center(child: CircularProgressIndicator()));

    return DropdownButtonFormField<String>(
      initialValue: widget.selectedId,
      decoration: const InputDecoration(
        labelText: 'Proveedor',
        prefixIcon: Icon(Icons.factory_outlined),
      ),
      items: _suppliers.map((s) => DropdownMenuItem(
        value: s['id'] as String,
        child: Text(s['name'] as String? ?? ''),
      )).toList(),
      onChanged: widget.onChanged,
      validator: (v) => v == null ? 'Requerido' : null,
    );
  }
}
