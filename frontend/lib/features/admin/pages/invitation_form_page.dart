import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/shared/ds/ds.dart';
import 'package:zorvian/auth/auth_provider.dart';

class InvitationFormPage extends ConsumerStatefulWidget {
  const InvitationFormPage({super.key});

  @override
  ConsumerState<InvitationFormPage> createState() => _InvitationFormPageState();
}

class _InvitationFormPageState extends ConsumerState<InvitationFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _emailCtrl = TextEditingController();
  String _role = 'Employee';
  bool _loading = false;

  // Human-readable labels for display
  static const Map<String, String> _roleLabels = {
    'SuperAdmin': 'Super Administrador',
    'CompanyAdmin': 'Administrador de Empresa',
    'Rrhh': 'Recursos Humanos',
    'Supervisor': 'Supervisor',
    'Employee': 'Trabajador',
  };

  Future<void> _invite() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('invitations', data: {'email': _emailCtrl.text.trim(), 'role': _role});
      if (mounted) context.pop(true);
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error al invitar')));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: Form(
          key: _formKey,
          child: Column(
            children: [
              ZTextField(controller: _emailCtrl, label: 'Email', validator: (v) => v!.contains('@') ? null : 'Inválido'),
              const SizedBox(height: ZSpacing.md),
              DropdownButtonFormField<String>(
                initialValue: _role,
                items: _roleLabels.entries.map((e) => DropdownMenuItem(value: e.key, child: Text(e.value))).toList(),
                onChanged: (v) => setState(() => _role = v!),
                decoration: const InputDecoration(labelText: 'Rol', border: OutlineInputBorder()),
              ),
              const SizedBox(height: ZSpacing.xl),
              ZButton(text: 'Enviar Invitación', onPressed: _invite, isLoading: _loading),
            ],
          ),
        ),
      ),
    );
  }
}
