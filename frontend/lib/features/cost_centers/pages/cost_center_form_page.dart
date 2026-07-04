import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

final class CostCenterFormPage extends ConsumerStatefulWidget {
  final String? costCenterId;
  const CostCenterFormPage({super.key, this.costCenterId});
  @override
  ConsumerState<CostCenterFormPage> createState() => _CostCenterFormPageState();
}

final class _CostCenterFormPageState extends ConsumerState<CostCenterFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _codeCtrl = TextEditingController();
  final _nameCtrl = TextEditingController();
  final _descCtrl = TextEditingController();
  bool _isActive = true;
  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.costCenterId != null;

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('cost-centers/${widget.costCenterId}');
      final d = r.data;
      _codeCtrl.text = d['code'] ?? '';
      _nameCtrl.text = d['name'] ?? '';
      _descCtrl.text = d['description'] ?? '';
      _isActive = d['isActive'] ?? true;
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar centro de costo');
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'code': _codeCtrl.text.trim(),
        'name': _nameCtrl.text.trim(),
        'description': _descCtrl.text.trim().isEmpty ? null : _descCtrl.text.trim(),
        'isActive': _isActive,
      };
      if (_isEditing) {
        await dio.put('cost-centers/${widget.costCenterId}', data: body);
      } else {
        await dio.post('cost-centers', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar centro de costo');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _codeCtrl.dispose(); _nameCtrl.dispose(); _descCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(_isEditing ? 'Editar centro de costo' : 'Nuevo centro de costo')),
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
              TextFormField(controller: _codeCtrl, decoration: const InputDecoration(labelText: 'Código', prefixIcon: Icon(Icons.tag)), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _nameCtrl, decoration: const InputDecoration(labelText: 'Nombre', prefixIcon: Icon(Icons.account_balance)), validator: (v) => v == null || v.isEmpty ? 'Requerido' : null),
              const SizedBox(height: 12),
              TextFormField(controller: _descCtrl, decoration: const InputDecoration(labelText: 'Descripción', prefixIcon: Icon(Icons.description)), maxLines: 3),
              const SizedBox(height: 12),
              SwitchListTile(title: const Text('Activo'), value: _isActive, onChanged: (v) => setState(() => _isActive = v)),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear centro de costo',
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
