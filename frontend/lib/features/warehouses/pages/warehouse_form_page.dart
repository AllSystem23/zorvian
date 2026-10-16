import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

final class WarehouseFormPage extends ConsumerStatefulWidget {
  final String? warehouseId;
  const WarehouseFormPage({super.key, this.warehouseId});
  @override
  ConsumerState<WarehouseFormPage> createState() => _WarehouseFormPageState();
}

final class _WarehouseFormPageState extends ConsumerState<WarehouseFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _nameCtrl = TextEditingController();
  final _codeCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();
  bool _isActive = true;
  bool _isDefault = false;
  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.warehouseId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('warehouses/${widget.warehouseId}');
      final d = r.data;
      _nameCtrl.text = d['name'] ?? '';
      _codeCtrl.text = d['code'] ?? '';
      _addressCtrl.text = d['address'] ?? '';
      _isActive = d['isActive'] ?? true;
      _isDefault = d['isDefault'] ?? false;
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar la bodega');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      if (_isEditing) {
        // UpdateRequest no incluye isDefault: la promoción es set-default.
        await dio.put('warehouses/${widget.warehouseId}', data: {
          'name': _nameCtrl.text.trim(),
          'code': _codeCtrl.text.trim().isEmpty ? null : _codeCtrl.text.trim(),
          'address': _addressCtrl.text.trim().isEmpty ? null : _addressCtrl.text.trim(),
          'isActive': _isActive,
        });
      } else {
        // branchId null = sucursal activa del contexto (header X-Branch-Id).
        await dio.post('warehouses', data: {
          'branchId': null,
          'name': _nameCtrl.text.trim(),
          'code': _codeCtrl.text.trim().isEmpty ? null : _codeCtrl.text.trim(),
          'address': _addressCtrl.text.trim().isEmpty ? null : _addressCtrl.text.trim(),
          'isDefault': _isDefault,
        });
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar la bodega');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    _codeCtrl.dispose();
    _addressCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: SingleChildScrollView(
        padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
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
              TextFormField(
                controller: _nameCtrl,
                decoration: const InputDecoration(
                  labelText: 'Nombre *',
                  prefixIcon: Icon(Icons.warehouse_outlined),
                  hintText: 'Ej: Bodega Norte, Almacén Central',
                ),
                validator: (v) => v == null || v.trim().isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _codeCtrl,
                decoration: const InputDecoration(
                  labelText: 'Código',
                  prefixIcon: Icon(Icons.tag),
                  hintText: 'Ej: BN-01',
                ),
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _addressCtrl,
                decoration: const InputDecoration(
                  labelText: 'Dirección',
                  prefixIcon: Icon(Icons.location_on_outlined),
                ),
                maxLines: 2,
              ),
              const SizedBox(height: 12),
              SwitchListTile(
                title: const Text('Activa'),
                subtitle: const Text('Las bodegas inactivas no aceptan productos ni transferencias'),
                value: _isActive,
                onChanged: (v) => setState(() => _isActive = v),
              ),
              if (!_isEditing)
                SwitchListTile(
                  title: const Text('Bodega por defecto de la sucursal'),
                  subtitle: const Text('Será la "Bodega Principal" usada cuando no se elija bodega'),
                  value: _isDefault,
                  onChanged: (v) => setState(() => _isDefault = v),
                ),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear bodega',
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
