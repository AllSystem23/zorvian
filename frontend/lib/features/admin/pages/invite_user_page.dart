import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';

class InviteUserPage extends ConsumerStatefulWidget {
  const InviteUserPage({super.key});

  @override
  ConsumerState<InviteUserPage> createState() => _InviteUserPageState();
}

class _InviteUserPageState extends ConsumerState<InviteUserPage> {
  final _formKey = GlobalKey<FormState>();
  final _emailCtrl = TextEditingController();
  String _selectedRole = 'Employee';
  bool _loading = false;

  // Role keys sent to the API
  final List<String> _roles = ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'];

  // Human-readable labels for display
  static const Map<String, String> _roleLabels = {
    'SuperAdmin': 'Super Administrador',
    'CompanyAdmin': 'Administrador de Empresa',
    'Rrhh': 'Recursos Humanos',
    'Supervisor': 'Supervisor',
    'Employee': 'Trabajador',
  };

  @override
  void dispose() {
    _emailCtrl.dispose();
    super.dispose();
  }

  Future<void> _generateInvite() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);

    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.post('invitations', data: {
        'email': _emailCtrl.text.trim(),
        'role': _selectedRole,
      });

      if (mounted) {
        final code = response.data['code'];
        _showCodeDialog(code);
      }
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _showCodeDialog(String code) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        icon: const Icon(Icons.check_circle_outline, color: ZColors.success, size: 48),
        title: const Text('Invitación creada'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text('Comparte este código con el usuario:'),
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: ZColors.brandAccent.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: ZColors.brandAccent.withValues(alpha: 0.3)),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    code,
                    style: const TextStyle(
                      fontFamily: 'monospace',
                      fontSize: 24,
                      fontWeight: FontWeight.bold,
                      letterSpacing: 2,
                    ),
                  ),
                  const SizedBox(width: 12),
                  IconButton(
                    icon: const Icon(Icons.copy_rounded, size: 20),
                    tooltip: 'Copiar código',
                    onPressed: () {
                      Clipboard.setData(ClipboardData(text: code));
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('Código copiado al portapapeles')),
                      );
                    },
                  ),
                ],
              ),
            ),
          ],
        ),
        actions: [
          ZButton(
            text: 'CERRAR',
            fullWidth: false,
            onPressed: () => Navigator.pop(ctx),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Form(
          key: _formKey,
          child: Column(
            children: [
              TextFormField(
                controller: _emailCtrl,
                decoration: const InputDecoration(labelText: 'Correo electrónico'),
                validator: (v) => v == null || !v.contains('@') ? 'Correo inválido' : null,
              ),
              DropdownButtonFormField<String>(
                initialValue: _selectedRole,
                items: _roles.map((r) => DropdownMenuItem(value: r, child: Text(_roleLabels[r] ?? r))).toList(),
                onChanged: (v) => setState(() => _selectedRole = v!),
                decoration: const InputDecoration(labelText: 'Rol'),
              ),
              const SizedBox(height: 24),
              ZButton(
                text: 'Generar invitación',
                onPressed: _generateInvite,
                isLoading: _loading,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
