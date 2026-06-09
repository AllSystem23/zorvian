import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';

class WebhookFormPage extends ConsumerStatefulWidget {
  final String? webhookId;
  const WebhookFormPage({super.key, this.webhookId});

  @override
  ConsumerState<WebhookFormPage> createState() => _WebhookFormPageState();
}

class _WebhookFormPageState extends ConsumerState<WebhookFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _urlCtrl = TextEditingController();
  final _descCtrl = TextEditingController();
  String _eventType = 'sale.created';
  bool _isActive = true;
  int _maxRetries = 3;
  int _retryInterval = 60;
  bool _loading = false;
  String? _error;
  bool get _isEditing => widget.webhookId != null;

  final _eventOptions = [
    ('sale.created', 'Venta creada'),
    ('purchase.created', 'Compra creada'),
    ('purchase.pending_approval', 'Compra pendiente aprobación'),
    ('payroll.submitted', 'Nómina enviada'),
    ('payroll.approved', 'Nómina aprobada'),
    ('payroll.paid', 'Nómina pagada'),
  ];

  @override
  void initState() {
    super.initState();
    if (_isEditing) _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('webhooks/${widget.webhookId}');
      final data = r.data;
      _urlCtrl.text = data['targetUrl'] ?? '';
      _descCtrl.text = data['description'] ?? '';
      _eventType = data['eventType'] ?? 'sale.created';
      _isActive = data['isActive'] ?? true;
      _maxRetries = data['maxRetries'] ?? 3;
      _retryInterval = data['retryIntervalSeconds'] ?? 60;
      setState(() {});
    } catch (_) {
      setState(() => _error = 'Error al cargar webhook');
    }
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
        'eventType': _eventType,
        'targetUrl': _urlCtrl.text.trim(),
        'description': _descCtrl.text.trim().isEmpty
            ? null
            : _descCtrl.text.trim(),
        'isActive': _isActive,
        'maxRetries': _maxRetries,
        'retryIntervalSeconds': _retryInterval,
      };

      if (_isEditing) {
        await dio.put('webhooks/${widget.webhookId}', data: body);
      } else {
        await dio.post('webhooks', data: body);
      }
      if (mounted) context.pop(true);
    } catch (_) {
      setState(() => _error = 'Error al guardar webhook');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _urlCtrl.dispose();
    _descCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar:
          AppBar(title: Text(_isEditing ? 'Editar webhook' : 'Nuevo webhook')),
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
                  child: Text(_error!,
                      style: TextStyle(color: theme.colorScheme.error)),
                ),
              DropdownButtonFormField<String>(
                initialValue: _eventType,
                decoration: const InputDecoration(
                  labelText: 'Evento',
                  prefixIcon: Icon(Icons.category),
                ),
                items: _eventOptions.map((e) {
                  return DropdownMenuItem(
                      value: e.$1, child: Text(e.$2));
                }).toList(),
                onChanged: (v) {
                  if (v != null) setState(() => _eventType = v);
                },
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _urlCtrl,
                decoration: const InputDecoration(
                  labelText: 'URL de destino',
                  prefixIcon: Icon(Icons.link),
                  hintText: 'https://ejemplo.com/webhook',
                ),
                keyboardType: TextInputType.url,
                validator: (v) {
                  if (v == null || v.trim().isEmpty) return 'Requerido';
                  final uri = Uri.tryParse(v.trim());
                  if (uri == null || !uri.isAbsolute) return 'URL inválida';
                  return null;
                },
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _descCtrl,
                decoration: const InputDecoration(
                  labelText: 'Descripción (opcional)',
                  prefixIcon: Icon(Icons.description),
                ),
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      initialValue: _maxRetries.toString(),
                      decoration: const InputDecoration(
                        labelText: 'Reintentos máx.',
                        prefixIcon: Icon(Icons.replay),
                      ),
                      keyboardType: TextInputType.number,
                      onChanged: (v) =>
                          _maxRetries = int.tryParse(v) ?? 3,
                    ),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: TextFormField(
                      initialValue: _retryInterval.toString(),
                      decoration: const InputDecoration(
                        labelText: 'Intervalo (seg)',
                        prefixIcon: Icon(Icons.timer),
                      ),
                      keyboardType: TextInputType.number,
                      onChanged: (v) =>
                          _retryInterval = int.tryParse(v) ?? 60,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              SwitchListTile(
                title: const Text('Activo'),
                value: _isActive,
                onChanged: (v) => setState(() => _isActive = v),
              ),
              const SizedBox(height: 24),
              ZButton(
                text: _isEditing ? 'Actualizar' : 'Crear webhook',
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
