import 'dart:ui';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../../core/utils/validators.dart';
import '../../shared/ds/ds.dart';

/// Pantalla de restablecimiento de contraseña.
///
/// Recibe el token por query string (`/reset-password?token=...`), lo que
/// permite abrirla directamente desde el enlace que llega por correo.
///
/// Contrato con el backend: `POST zorvian/v1/auth/reset-password` con
/// `{ token, newPassword, confirmPassword }`. La política de contraseña es la
/// misma del backend (`PasswordPolicy`): 8+ caracteres, una mayúscula y un
/// número.
class ResetPasswordPage extends ConsumerStatefulWidget {
  final String? token;

  const ResetPasswordPage({super.key, this.token});

  @override
  ConsumerState<ResetPasswordPage> createState() => _ResetPasswordPageState();
}

class _ResetPasswordPageState extends ConsumerState<ResetPasswordPage> {
  final _passwordCtrl = TextEditingController();
  final _confirmCtrl = TextEditingController();
  final _formKey = GlobalKey<FormState>();

  bool _loading = false;
  bool _obscurePassword = true;
  bool _obscureConfirm = true;
  bool _done = false;
  String? _error;

  String get _token => (widget.token ?? '').trim();

  bool get _hasToken => _token.isNotEmpty;

  @override
  void dispose() {
    _passwordCtrl.dispose();
    _confirmCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('auth/reset-password', data: {
        'token': _token,
        'newPassword': _passwordCtrl.text,
        'confirmPassword': _confirmCtrl.text,
      });
      if (mounted) setState(() => _done = true);
    } on DioException catch (e) {
      if (mounted) {
        setState(() => _error = _extractError(e) ?? 'No se pudo restablecer la contraseña. Intenta de nuevo.');
      }
    } catch (_) {
      if (mounted) {
        setState(() => _error = 'No se pudo restablecer la contraseña. Intenta de nuevo.');
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  /// El backend responde `{ "error": "..." }` en los 400 del controller.
  String? _extractError(DioException e) {
    final data = e.response?.data;
    if (data is Map) {
      final error = data['error'];
      if (error is String && error.isNotEmpty) return error;
      final detail = data['detail'];
      if (detail is String && detail.isNotEmpty) return detail;
      final message = data['message'];
      if (message is String && message.isNotEmpty) return message;
    }
    return null;
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
              width: 300,
              height: 300,
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
                    if (!_hasToken)
                      _buildInvalidLinkCard()
                    else if (_done)
                      _buildSuccessCard()
                    else
                      _buildFormCard(),
                  ],
                ),
              ),
            ),
          ),
          Positioned(
            top: 24,
            left: 24,
            child: IconButton(
              icon: const Icon(Icons.arrow_back_ios_new_rounded, color: Colors.white70),
              onPressed: () => context.go('/login'),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildInvalidLinkCard() {
    return _glassContainer(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 64,
            height: 64,
            decoration: BoxDecoration(
              color: ZColors.danger.withValues(alpha: 0.1),
              shape: BoxShape.circle,
            ),
            child: const Icon(Icons.link_off_rounded, size: 32, color: ZColors.danger),
          ),
          const SizedBox(height: 24),
          Text(
            'Enlace inválido',
            style: ZTypography.displaySmall.copyWith(color: Colors.white, fontSize: 28),
          ),
          const SizedBox(height: 12),
          Text(
            'El enlace de restablecimiento no trae un token válido. Solicita uno nuevo desde la pantalla de inicio de sesión.',
            textAlign: TextAlign.center,
            style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral400),
          ),
          const SizedBox(height: 32),
          ZButton(
            text: 'SOLICITAR NUEVO ENLACE',
            onPressed: () => context.go('/forgot-password'),
            gradient: ZColors.accentGradient,
          ),
        ],
      ),
    );
  }

  Widget _buildFormCard() {
    return _glassContainer(
      child: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Nueva Contraseña',
              style: ZTypography.displaySmall.copyWith(color: Colors.white, fontSize: 32),
            ),
            const SizedBox(height: 12),
            Text(
              'Elige una contraseña nueva para tu cuenta de Zorvian ERP.',
              style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral400),
            ),
            const SizedBox(height: 40),
            if (_error != null) _buildErrorBanner(),
            ZTextField(
              controller: _passwordCtrl,
              label: 'Contraseña Nueva',
              obscureText: _obscurePassword,
              prefix: const Icon(Icons.lock_outline_rounded),
              suffix: IconButton(
                icon: Icon(
                  _obscurePassword ? Icons.visibility_off : Icons.visibility,
                  semanticLabel: _obscurePassword ? 'Mostrar contraseña' : 'Ocultar contraseña',
                ),
                onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
                tooltip: _obscurePassword ? 'Mostrar contraseña' : 'Ocultar contraseña',
              ),
              validator: ZValidators.password,
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 16),
            _buildPasswordRules(),
            const SizedBox(height: 20),
            ZTextField(
              controller: _confirmCtrl,
              label: 'Confirmar Contraseña',
              obscureText: _obscureConfirm,
              prefix: const Icon(Icons.lock_reset_rounded),
              suffix: IconButton(
                icon: Icon(
                  _obscureConfirm ? Icons.visibility_off : Icons.visibility,
                  semanticLabel: _obscureConfirm ? 'Mostrar contraseña' : 'Ocultar contraseña',
                ),
                onPressed: () => setState(() => _obscureConfirm = !_obscureConfirm),
                tooltip: _obscureConfirm ? 'Mostrar contraseña' : 'Ocultar contraseña',
              ),
              validator: (v) {
                if (v == null || v.isEmpty) return 'Confirma tu contraseña';
                if (v != _passwordCtrl.text) return 'Las contraseñas no coinciden';
                return null;
              },
            ),
            const SizedBox(height: 32),
            ZButton(
              text: 'GUARDAR NUEVA CONTRASEÑA',
              onPressed: _submit,
              isLoading: _loading,
              gradient: ZColors.accentGradient,
            ),
            const SizedBox(height: 16),
            TextButton(
              onPressed: () => context.go('/login'),
              child: Text(
                'Volver a Inicio de Sesión',
                style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500),
              ),
            ),
          ],
        ),
      ),
    );
  }

  /// Checklist de la política de contraseña (misma regla que el backend).
  Widget _buildPasswordRules() {
    final value = _passwordCtrl.text;
    final rules = <(String, bool)>[
      ('Al menos 8 caracteres', value.length >= 8),
      ('Una letra mayúscula', value.contains(RegExp(r'[A-Z]'))),
      ('Un número', value.contains(RegExp(r'[0-9]'))),
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (final (label, ok) in rules)
          Padding(
            padding: const EdgeInsets.only(bottom: 4),
            child: Row(
              children: [
                Icon(
                  ok ? Icons.check_circle_rounded : Icons.radio_button_unchecked_rounded,
                  size: 14,
                  color: ok ? ZColors.success : ZColors.neutral600,
                ),
                const SizedBox(width: 8),
                Text(
                  label,
                  style: TextStyle(
                    fontSize: 12,
                    color: ok ? ZColors.success : ZColors.neutral500,
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }

  Widget _buildSuccessCard() {
    return _glassContainer(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 64,
            height: 64,
            decoration: BoxDecoration(
              color: ZColors.success.withValues(alpha: 0.1),
              shape: BoxShape.circle,
            ),
            child: const Icon(Icons.verified_user_outlined, size: 32, color: ZColors.success),
          ),
          const SizedBox(height: 24),
          Text(
            'Contraseña actualizada',
            textAlign: TextAlign.center,
            style: ZTypography.displaySmall.copyWith(color: Colors.white, fontSize: 28),
          ),
          const SizedBox(height: 12),
          Text(
            'Tu contraseña fue cambiada correctamente. Por seguridad, las sesiones abiertas se cerraron: inicia sesión de nuevo.',
            textAlign: TextAlign.center,
            style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral400),
          ),
          const SizedBox(height: 32),
          ZButton(
            text: 'IR A INICIAR SESIÓN',
            onPressed: () => context.go('/login'),
            gradient: ZColors.accentGradient,
          ),
        ],
      ),
    );
  }

  Widget _glassContainer({required Widget child}) {
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
          child: child,
        ),
      ),
    );
  }

  Widget _buildErrorBanner() {
    return Container(
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
    );
  }
}
