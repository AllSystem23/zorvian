import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../../shared/ds/ds.dart';

class ForgotPasswordPage extends ConsumerStatefulWidget {
  const ForgotPasswordPage({super.key});

  @override
  ConsumerState<ForgotPasswordPage> createState() => _ForgotPasswordPageState();
}

class _ForgotPasswordPageState extends ConsumerState<ForgotPasswordPage> {
  final _emailCtrl = TextEditingController();
  final _formKey = GlobalKey<FormState>();
  bool _loading = false;
  bool _sent = false;
  String? _error;

  @override
  void dispose() {
    _emailCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('auth/forgot-password', data: { 'email': _emailCtrl.text.trim() });
      if (mounted) setState(() => _sent = true);
    } catch (e) {
      if (mounted) setState(() => _error = 'Error al enviar la solicitud. Intenta de nuevo.');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: ZColors.darkBackground,
      body: Stack(
        children: [
          Positioned.fill(
            child: Container(
              decoration: const BoxDecoration(
                gradient: RadialGradient(
                  center: Alignment(-0.3, -0.5),
                  radius: 1.5,
                  colors: [ZColors.neutral800, ZColors.darkBackground],
                ),
              ),
            ),
          ),
          Positioned(
            top: -80,
            left: -80,
            child: Container(
              width: 300, height: 300,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: ZColors.brandAccent.withValues(alpha: 0.05),
              ),
              child: BackdropFilter(filter: ImageFilter.blur(sigmaX: 80, sigmaY: 80), child: Container()),
            ),
          ),
          SafeArea(
            child: Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(24),
                child: Column(
                  children: [
                    Image.asset('assets/Zorvian.png', height: 80, fit: BoxFit.contain),
                    const SizedBox(height: 48),
                    if (_sent)
                      _buildSuccessCard()
                    else
                      _buildFormCard(),
                  ],
                ),
              ),
            ),
          ),
          Positioned(
            top: 24, left: 24,
            child: IconButton(
              icon: const Icon(Icons.arrow_back_ios_new_rounded, color: Colors.white70),
              onPressed: () => context.pop(),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFormCard() {
    return ClipRRect(
      borderRadius: BorderRadius.circular(28),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 20, sigmaY: 20),
        child: Container(
          constraints: const BoxConstraints(maxWidth: 460),
          padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 40),
          decoration: BoxDecoration(
            color: Colors.white.withValues(alpha: 0.03),
            borderRadius: BorderRadius.circular(28),
            border: Border.all(color: Colors.white.withValues(alpha: 0.08), width: 1.5),
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [Colors.white.withValues(alpha: 0.05), Colors.white.withValues(alpha: 0.01)],
            ),
          ),
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('¿Olvidaste tu\nContraseña?', style: ZTypography.displaySmall.copyWith(color: Colors.white, fontSize: 32)),
                const SizedBox(height: 12),
                Text(
                  'Ingresa tu correo electrónico registrado. Te enviaremos instrucciones para recuperar el acceso.',
                  style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral400),
                ),
                const SizedBox(height: 40),
                if (_error != null)
                  Container(
                    padding: const EdgeInsets.all(12),
                    margin: const EdgeInsets.only(bottom: 24),
                    decoration: BoxDecoration(
                      color: ZColors.danger.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: ZColors.danger.withValues(alpha: 0.2)),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.error_outline, size: 18, color: ZColors.danger),
                        const SizedBox(width: 12),
                        Expanded(child: Text(_error!, style: const TextStyle(color: ZColors.danger, fontSize: 13))),
                      ],
                    ),
                  ),
                ZTextField(
                  controller: _emailCtrl,
                  label: 'Correo Electrónico',
                  prefix: const Icon(Icons.alternate_email_rounded),
                  keyboardType: TextInputType.emailAddress,
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Ingresa tu correo';
                    if (!v.contains('@')) return 'Correo inválido';
                    return null;
                  },
                ),
                const SizedBox(height: 32),
                ZButton(
                  text: 'ENVIAR INSTRUCCIONES',
                  onPressed: _submit,
                  isLoading: _loading,
                  gradient: ZColors.accentGradient,
                ),
                const SizedBox(height: 16),
                TextButton(
                  onPressed: () => context.pop(),
                  child: Text(
                    'Volver a Inicio de Sesión',
                    style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildSuccessCard() {
    return ClipRRect(
      borderRadius: BorderRadius.circular(28),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 20, sigmaY: 20),
        child: Container(
          constraints: const BoxConstraints(maxWidth: 460),
          padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 40),
          decoration: BoxDecoration(
            color: Colors.white.withValues(alpha: 0.03),
            borderRadius: BorderRadius.circular(28),
            border: Border.all(color: Colors.white.withValues(alpha: 0.08), width: 1.5),
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 64, height: 64,
                decoration: BoxDecoration(
                  color: ZColors.success.withValues(alpha: 0.1),
                  shape: BoxShape.circle,
                ),
                child: const Icon(Icons.mark_email_read_outlined, size: 32, color: ZColors.success),
              ),
              const SizedBox(height: 24),
              Text('Correo Enviado', style: ZTypography.displaySmall.copyWith(color: Colors.white, fontSize: 28)),
              const SizedBox(height: 12),
              Text(
                'Si el correo ${_emailCtrl.text.trim()} está registrado en nuestro sistema, recibirás instrucciones en unos minutos.',
                textAlign: TextAlign.center,
                style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral400),
              ),
              const SizedBox(height: 32),
              ZButton(
                text: 'VOLVER AL INICIO',
                onPressed: () => context.go('/login'),
                gradient: ZColors.accentGradient,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
