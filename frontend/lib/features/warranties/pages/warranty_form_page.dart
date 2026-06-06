import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
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
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar garantía' : 'Nueva garantía')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_error != null) Container(
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(color: theme.colorScheme.errorContainer, borderRadius: BorderRadius.circular(8)),
                child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
              ),
              TextFormField(controller: _clientIdCtrl, decoration: const InputDecoration(labelText: 'ID Cliente', prefixIcon: Icon(Icons.person)), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _productIdCtrl, decoration: const InputDecoration(labelText: 'ID Producto', prefixIcon: Icon(Icons.inventory)), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _serialCtrl, decoration: const InputDecoration(labelText: 'Número de Serie', prefixIcon: Icon(Icons.qr_code))),
              const SizedBox(height: 12),
              TextFormField(controller: _imeiCtrl, decoration: const InputDecoration(labelText: 'IMEI', prefixIcon: Icon(Icons.phone_android)), maxLength: 20),
              const SizedBox(height: 12),
              TextFormField(controller: _lotCtrl, decoration: const InputDecoration(labelText: 'Lote', prefixIcon: Icon(Icons.batch_prediction))),
              const SizedBox(height: 12),
              DropdownButtonFormField<int>(
                value: _durationMonths,
                decoration: const InputDecoration(labelText: 'Duración (meses)', prefixIcon: Icon(Icons.timer)),
                items: [3, 6, 12, 24, 36].map((m) => DropdownMenuItem(value: m, child: Text('$m meses'))).toList(),
                onChanged: (v) => setState(() => _durationMonths = v ?? 12),
              ),
              const SizedBox(height: 12),
              TextFormField(controller: _termsCtrl, decoration: const InputDecoration(labelText: 'Términos y condiciones', prefixIcon: Icon(Icons.description)), maxLines: 3),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _loading ? null : _save,
                child: _loading
                    ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : Text(_isEditing ? 'Actualizar' : 'Crear garantía'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
