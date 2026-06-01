import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';

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
  final List<String> _roles = ['SuperAdmin', 'CompanyAdmin', 'Rrhh', 'Supervisor', 'Employee'];

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
        showDialog(
          context: context,
          builder: (_) => AlertDialog(
            title: const Text('Invitación creada'),
            content: SelectableText('Código: $code\n\nComparte este código con el usuario.'),
            actions: [
              TextButton(onPressed: () => context.pop(), child: const Text('Aceptar')),
            ],
          ),
        );
      }
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Invitar usuario')),
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
                value: _selectedRole,
                items: _roles.map((r) => DropdownMenuItem(value: r, child: Text(r))).toList(),
                onChanged: (v) => setState(() => _selectedRole = v!),
                decoration: const InputDecoration(labelText: 'Rol'),
              ),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _loading ? null : _generateInvite,
                child: _loading ? const CircularProgressIndicator() : const Text('Generar invitación'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
