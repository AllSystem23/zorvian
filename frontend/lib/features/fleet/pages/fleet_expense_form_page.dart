import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;
import '../../../core/utils/country_config.dart';
import '../../../shared/ds/ds.dart';

final class FleetExpenseFormPage extends ConsumerStatefulWidget {
  final String? expenseId;
  const FleetExpenseFormPage({super.key, this.expenseId});

  @override
  ConsumerState<FleetExpenseFormPage> createState() => _FleetExpenseFormPageState();
}

final class _FleetExpenseFormPageState extends ConsumerState<FleetExpenseFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _descriptionCtrl = TextEditingController();
  final _amountCtrl = TextEditingController();
  final _exchangeRateCtrl = TextEditingController();

  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.expenseId != null;

  DateTime _expenseDate = DateTime.now();
  String _currency = 'NIO';
  String _paymentMethod = 'Cash';
  bool _reimbursable = false;
  String? _selectedCategoryId;
  String? _selectedSubcategoryId;
  List<Map<String, dynamic>> _categories = [];
  List<Map<String, dynamic>> _subcategories = [];

  @override
  void initState() {
    super.initState();
    _initData();
  }

  Future<void> _initData() async {
    await _loadCategories();
    if (_isEditing) await _load();
  }

  Future<void> _loadCategories() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/expense-categories');
      final data = r.data;
      setState(() {
        _categories = (data is List ? data : (data['items'] as List? ?? []))
            .map((e) => Map<String, dynamic>.from(e as Map))
            .toList();
      });
    } catch (_) {
      // Categories unavailable — user can still save without one
    }
  }

  Future<void> _loadSubcategories(String categoryId) async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/expense-subcategories/by-category/$categoryId');
      final data = r.data;
      setState(() {
        _subcategories = (data is List ? data : (data['items'] as List? ?? []))
            .map((e) => Map<String, dynamic>.from(e as Map))
            .toList();
      });
    } catch (_) {
      setState(() => _subcategories = []);
    }
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final d = await dio.get('fleet/expenses/${widget.expenseId}');
      final dd = d.data;
      _descriptionCtrl.text = dd['description'] ?? '';
      _amountCtrl.text = dd['amount']?.toString() ?? '';
      _exchangeRateCtrl.text = dd['exchangeRate']?.toString() ?? '1';
      _currency = dd['currency'] ?? 'NIO';
      _paymentMethod = dd['paymentMethod'] ?? 'Cash';
      _reimbursable = dd['reimbursable'] ?? false;
      _selectedCategoryId = dd['categoryId'] as String?;
      _selectedSubcategoryId = dd['subcategoryId'] as String?;
      if (_selectedCategoryId != null) {
        await _loadSubcategories(_selectedCategoryId!);
      }
      if (dd['expenseDate'] != null) {
        _expenseDate = DateTime.parse(dd['expenseDate'] as String);
      }
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{
        'expenseDate': _expenseDate.toUtc().toIso8601String(),
        'categoryId': _selectedCategoryId ?? '',
        'subcategoryId': _selectedSubcategoryId,
        'description': _descriptionCtrl.text.trim(),
        'amount': double.tryParse(_amountCtrl.text.trim()) ?? 0,
        'currency': _currency,
        'exchangeRate': double.tryParse(_exchangeRateCtrl.text.trim()) ?? 1,
        'paymentMethod': _paymentMethod,
        'reimbursable': _reimbursable,
      };
      if (_isEditing) {
        await dio.put('fleet/expenses/${widget.expenseId}', data: body);
      } else {
        await dio.post('fleet/expenses', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _descriptionCtrl.dispose();
    _amountCtrl.dispose();
    _exchangeRateCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SingleChildScrollView(
        padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: ZAlertCard(message: _error!, severity: 'high'),
                ),
              _buildSection('Información del Gasto', Icons.receipt_outlined, [
                ZDropdownFormField<String>(
                  value: _selectedCategoryId,
                  items: _categories.map((c) => DropdownMenuItem(
                    value: c['id'] as String?,
                    child: Text(c['name'] as String? ?? ''),
                  )).toList(),
                  label: 'Categoría',
                  prefixIcon: Icons.category_outlined,
                  validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
                  onChanged: (v) {
                    setState(() {
                      _selectedCategoryId = v;
                      _selectedSubcategoryId = null;
                      _subcategories = [];
                    });
                    if (v != null) _loadSubcategories(v);
                  },
                ),
                if (_subcategories.isNotEmpty) ...[const SizedBox(height: 12),
                ZDropdownFormField<String>(
                  value: _selectedSubcategoryId,
                  items: _subcategories.map((s) => DropdownMenuItem(
                    value: s['id'] as String?,
                    child: Text(s['name'] as String? ?? ''),
                  )).toList(),
                  label: 'Subcategoría',
                  prefixIcon: Icons.subdirectory_arrow_right_outlined,
                  onChanged: (v) { setState(() => _selectedSubcategoryId = v); },
                ),],
                const SizedBox(height: 12),
                ZTextField(controller: _descriptionCtrl, label: 'Descripción', prefix: const Icon(Icons.description_outlined), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZTextField(controller: _amountCtrl, label: 'Monto', prefix: const Icon(Icons.monetization_on_outlined), keyboardType: TextInputType.number, validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
                const SizedBox(height: 12),
                ZDropdownFormField<String>(
                  value: _currency,
                  items: CountryConfig.countries.values.map((c) => DropdownMenuItem(value: c.currency, child: Text('${c.currencySymbol} ${c.currency}'))).toList(),
                  label: 'Moneda',
                  prefixIcon: Icons.money_outlined,
                  onChanged: (v) { if (v != null) setState(() => _currency = v); },
                ),
                const SizedBox(height: 12),
                ZTextField(controller: _exchangeRateCtrl, label: 'Tasa de cambio', prefix: const Icon(Icons.currency_exchange_outlined), keyboardType: TextInputType.number),
                const SizedBox(height: 12),
                ZDropdownFormField<String>(
                  value: _paymentMethod,
                  items: ['Cash', 'Card', 'Transfer', 'Check'].map((m) => DropdownMenuItem(value: m, child: Text(m))).toList(),
                  label: 'Método de pago',
                  prefixIcon: Icons.payments_outlined,
                  onChanged: (v) { if (v != null) setState(() => _paymentMethod = v); },
                ),
                const SizedBox(height: 12),
                SwitchListTile(
                  title: const Text('Reembolsable'),
                  value: _reimbursable,
                  onChanged: (v) => setState(() => _reimbursable = v),
                  secondary: const Icon(Icons.money_off_outlined),
                ),
              ]),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Registrar gasto',
                onPressed: _save,
                isLoading: _loading,
                icon: _isEditing ? Icons.save_outlined : Icons.add_circle_outline,
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSection(String title, IconData icon, List<Widget> children) {
    return ZCard(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(icon, size: 20, color: ZColors.moduleFleet),
              const SizedBox(width: 8),
              Text(title, style: Theme.of(context).textTheme.titleMedium),
            ],
          ),
          const SizedBox(height: 16),
          ...children,
        ],
      ),
    );
  }
}
