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
  final _folioCtrl = TextEditingController();
  final _descCtrl = TextEditingController();
  String _type = 'manufacturer';
  String _status = 'active';
  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.warrantyId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('warranties/${widget.warrantyId}');
      final d = r.data;
      _folioCtrl.text = d['folio'] ?? '';
      _descCtrl.text = d['description'] ?? '';
      _type = d['type'] ?? 'manufacturer';
      _status = d['status'] ?? 'active';
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar garantía');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'folio': _folioCtrl.text.trim(),
        'description': _descCtrl.text.trim().isEmpty ? null : _descCtrl.text.trim(),
        'type': _type,
        'status': _status,
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
    _folioCtrl.dispose(); _descCtrl.dispose();
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
              TextFormField(controller: _folioCtrl, decoration: const InputDecoration(labelText: 'Folio', prefixIcon: Icon(Icons.tag)), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _descCtrl, decoration: const InputDecoration(labelText: 'Descripción', prefixIcon: Icon(Icons.description)), maxLines: 3),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                initialValue: _type,
                decoration: const InputDecoration(labelText: 'Tipo', prefixIcon: Icon(Icons.category)),
                items: const [
                  DropdownMenuItem(value: 'manufacturer', child: Text('Fabricante')),
                  DropdownMenuItem(value: 'store', child: Text('Tienda')),
                  DropdownMenuItem(value: 'extended', child: Text('Extendida')),
                ],
                onChanged: (v) => setState(() => _type = v ?? 'manufacturer'),
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                initialValue: _status,
                decoration: const InputDecoration(labelText: 'Estado', prefixIcon: Icon(Icons.info)),
                items: const [
                  DropdownMenuItem(value: 'active', child: Text('Activa')),
                  DropdownMenuItem(value: 'expired', child: Text('Expirada')),
                  DropdownMenuItem(value: 'claimed', child: Text('Reclamada')),
                  DropdownMenuItem(value: 'cancelled', child: Text('Cancelada')),
                ],
                onChanged: (v) => setState(() => _status = v ?? 'active'),
              ),
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
