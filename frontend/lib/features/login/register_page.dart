import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';

class RegisterPage extends ConsumerStatefulWidget {
  final String? inviteCode;
  const RegisterPage({super.key, this.inviteCode});

  @override
  ConsumerState<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends ConsumerState<RegisterPage> {
  final _formKey = GlobalKey<FormState>();
  final _inviteCodeCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  final _nameCtrl = TextEditingController();
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    if (widget.inviteCode != null) {
      _inviteCodeCtrl.text = widget.inviteCode!;
    }
  }

  @override
  void dispose() {
    _inviteCodeCtrl.dispose();
    _emailCtrl.dispose();
    _passwordCtrl.dispose();
    _nameCtrl.dispose();
    super.dispose();
  }

  Future<void> _register() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });

    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('auth/register', data: {
        'inviteCode': _inviteCodeCtrl.text.trim(),
        'email': _emailCtrl.text.trim(),
        'password': _passwordCtrl.text,
        'displayName': _nameCtrl.text.trim(),
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Registro exitoso. Ahora puedes iniciar sesión.')));
        context.go('/login');
      }
    } catch (e) {
      setState(() => _error = 'Error al registrar: $e');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Registro de empleado')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Form(
          key: _formKey,
          child: Column(
            children: [
              if (_error != null) Text(_error!, style: const TextStyle(color: Colors.red)),
              TextFormField(controller: _inviteCodeCtrl, decoration: const InputDecoration(labelText: 'Código de invitación')),
              TextFormField(controller: _nameCtrl, decoration: const InputDecoration(labelText: 'Nombre completo')),
              TextFormField(controller: _emailCtrl, decoration: const InputDecoration(labelText: 'Correo')),
              TextFormField(controller: _passwordCtrl, decoration: const InputDecoration(labelText: 'Contraseña'), obscureText: true),
              const SizedBox(height: 24),
              ElevatedButton(onPressed: _loading ? null : _register, child: const Text('Registrarse')),
            ],
          ),
        ),
      ),
    );
  }
}
