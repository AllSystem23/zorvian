import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../../shared/ds/ds.dart';


class OnboardingPage extends ConsumerStatefulWidget {
  const OnboardingPage({super.key});

  @override
  ConsumerState<OnboardingPage> createState() => _OnboardingPageState();
}

class _OnboardingPageState extends ConsumerState<OnboardingPage> {
  final _formKey = GlobalKey<FormState>();
  final _companyNameCtrl = TextEditingController();
  final _legalNameCtrl = TextEditingController();
  final _taxIdCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();
  final _employeesCtrl = TextEditingController(text: '10');
  bool _loading = false;
  String? _error;
  @override
  void dispose() {
    _companyNameCtrl.dispose();
    _legalNameCtrl.dispose();
    _taxIdCtrl.dispose();
    _phoneCtrl.dispose();
    _addressCtrl.dispose();
    _employeesCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });

    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('companies', data: {
        'name': _companyNameCtrl.text.trim(),
        'legalName': _legalNameCtrl.text.trim(),
        'taxId': _taxIdCtrl.text.trim(),
        'phone': _phoneCtrl.text.trim(),
        'address': _addressCtrl.text.trim(),
        'maxEmployees': int.tryParse(_employeesCtrl.text) ?? 10,
        'country': 'Nicaragua',
        'currency': 'NIO',
        'timezone': 'America/Managua',
      });

      await ref.read(authProvider.notifier).checkAuth();
      if (mounted) context.go('/dashboard');
    } catch (e) {
      setState(() => _error = 'Error al crear la empresa');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: const Text('Configurar empresa')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Icon(Icons.business, size: 48, color: theme.colorScheme.primary),
                const SizedBox(height: 16),
                Text(
                  'Bienvenido a Zorvian ERP',
                  textAlign: TextAlign.center,
                  style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 8),
                Text(
                  'Configure los datos de su empresa para comenzar',
                  textAlign: TextAlign.center,
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
                const SizedBox(height: 32),
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
                TextFormField(
                  controller: _companyNameCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Nombre comercial',
                    prefixIcon: Icon(Icons.badge),
                  ),
                  validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _legalNameCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Razón social',
                    prefixIcon: Icon(Icons.description),
                  ),
                  validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _taxIdCtrl,
                  decoration: const InputDecoration(
                    labelText: 'RUC / NIT',
                    prefixIcon: Icon(Icons.numbers),
                  ),
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _phoneCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Teléfono',
                    prefixIcon: Icon(Icons.phone),
                  ),
                  keyboardType: TextInputType.phone,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _addressCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Dirección',
                    prefixIcon: Icon(Icons.location_on),
                  ),
                  maxLines: 2,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _employeesCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Empleados estimados',
                    prefixIcon: Icon(Icons.people),
                  ),
                  keyboardType: TextInputType.number,
                  validator: (v) {
                    final n = int.tryParse(v ?? '');
                    return n == null || n < 1 ? 'Mínimo 1' : null;
                  },
                ),
                const SizedBox(height: 32),
                ZButton(
                  text: 'Crear empresa',
                  onPressed: _submit,
                  isLoading: _loading,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
