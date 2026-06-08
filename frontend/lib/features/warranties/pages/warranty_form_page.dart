import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:nexora/shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

final class WarrantyFormPage extends ConsumerStatefulWidget {
  final String? warrantyId;
  const WarrantyFormPage({super.key, this.warrantyId});
  @override
  ConsumerState<WarrantyFormPage> createState() => _WarrantyFormPageState();
}

final class _WarrantyFormPageState extends ConsumerState<WarrantyFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _clientIdCtrl = TextEditingController();
  final _productIdCtrl = TextEditingController();
  final _serialCtrl = TextEditingController();
  final _imeiCtrl = TextEditingController();
  final _lotCtrl = TextEditingController();
  final _termsCtrl = TextEditingController();
  int _durationMonths = 12;
  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.warrantyId != null;

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'clientId': _clientIdCtrl.text.trim(),
        'productId': _productIdCtrl.text.trim(),
        'durationMonths': _durationMonths,
        'terms': _termsCtrl.text.trim().isEmpty ? null : _termsCtrl.text.trim(),
        'serialNumber': _serialCtrl.text.trim().isEmpty ? null : _serialCtrl.text.trim(),
        'imei': _imeiCtrl.text.trim().isEmpty ? null : _imeiCtrl.text.trim(),
        'lotNumber': _lotCtrl.text.trim().isEmpty ? null : _lotCtrl.text.trim(),
      };
      if (_isEditing) {
        await dio.put('warranties/${widget.warrantyId}', data: body);
      } else {
        await dio.post('warranties', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar garantía');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _clientIdCtrl.dispose(); _productIdCtrl.dispose();
    _serialCtrl.dispose(); _imeiCtrl.dispose(); _lotCtrl.dispose();
    _termsCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar garantía' : 'Nueva garantía')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null) Container(
                padding: const EdgeInsets.all(ZSpacing.md),
                margin: const EdgeInsets.only(bottom: ZSpacing.md),
                decoration: BoxDecoration(color: ZColors.danger.withAlpha(30), borderRadius: BorderRadius.circular(8)),
                child: Text(_error!, style: const TextStyle(color: ZColors.danger)),
              ),
              ZTextField(controller: _clientIdCtrl, label: 'ID Cliente', validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: ZSpacing.md),
              ZTextField(controller: _productIdCtrl, label: 'ID Producto', validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: ZSpacing.md),
              ZTextField(controller: _serialCtrl, label: 'Número de Serie'),
              const SizedBox(height: ZSpacing.md),
              ZTextField(controller: _imeiCtrl, label: 'IMEI'),
              const SizedBox(height: ZSpacing.md),
              ZTextField(controller: _lotCtrl, label: 'Lote'),
              const SizedBox(height: ZSpacing.md),
              DropdownButtonFormField<int>(
                initialValue: _durationMonths,
                decoration: const InputDecoration(labelText: 'Duración (meses)', border: OutlineInputBorder()),
                items: [3, 6, 12, 24, 36].map((m) => DropdownMenuItem(value: m, child: Text('$m meses'))).toList(),
                onChanged: (v) => setState(() => _durationMonths = v ?? 12),
              ),
              const SizedBox(height: ZSpacing.md),
              ZTextField(controller: _termsCtrl, label: 'Términos y condiciones'),
              const SizedBox(height: ZSpacing.xl),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear garantía',
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
