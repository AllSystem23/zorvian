import 'dart:ui';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../../core/utils/validators.dart';
import '../../shared/ds/ds.dart';

enum RegisterMode { invite, request }

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
  final _confirmPasswordCtrl = TextEditingController();
  final _nameCtrl = TextEditingController();
  final _companyCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _messageCtrl = TextEditingController();

  RegisterMode _mode = RegisterMode.invite;
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
    _confirmPasswordCtrl.dispose();
    _nameCtrl.dispose();
    _companyCtrl.dispose();
    _phoneCtrl.dispose();
    _messageCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });

    try {
      if (_mode == RegisterMode.invite) {
        // ACTUAL REGISTRATION LOGIC
        final dio = ref.read(dioClientProvider);
        await dio.post('auth/register', data: {
          'inviteCode': _inviteCodeCtrl.text.trim(),
          'email': _emailCtrl.text.trim(),
          'password': _passwordCtrl.text,
          'displayName': _nameCtrl.text.trim(),
        });
        if (mounted) {
          ZToast.show(context, 'Registro exitoso. Bienvenido a Zorvian.');
          context.go('/login');
        }
      } else {
        // SOLICITAR ACCESO: cola de la plataforma. Un administrador revisa la
        // solicitud y emite el código de invitación por correo.
        final dio = ref.read(dioClientProvider);
        await dio.post('auth/request-access', data: {
          'fullName': _nameCtrl.text.trim(),
          'email': _emailCtrl.text.trim(),
          'company': _companyCtrl.text.trim(),
          'phone': _phoneCtrl.text.trim(),
          'message': _messageCtrl.text.trim(),
        });
        if (mounted) {
          ZModal.show(context,
            title: 'Solicitud Enviada',
            child: Column(
              children: [
                const Icon(Icons.mark_email_read_outlined, size: 64, color: ZColors.brandAccent),
                const SizedBox(height: 24),
                Text(
                  'Hemos recibido tu solicitud de acceso corporativo.',
                  textAlign: TextAlign.center,
                  style: ZTypography.bodyLarge,
                ),
                const SizedBox(height: 12),
                Text(
                  'Un administrador revisará tus datos y te enviará un código de invitación a tu correo en un plazo máximo de 24 horas.',
                  textAlign: TextAlign.center,
                  style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral500),
                ),
                const SizedBox(height: 32),
                ZButton(text: 'ENTENDIDO', onPressed: () => context.go('/login')),
              ],
            ),
          );
        }
      }
    } on DioException catch (e) {
      setState(() => _error = _extractError(e) ?? 'Ocurrió un error inesperado. Por favor, intenta de nuevo.');
    } catch (_) {
      setState(() => _error = 'Ocurrió un error inesperado. Por favor, intenta de nuevo.');
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
          // PREMIUM BACKGROUND (Consistent with Login)
          Positioned.fill(
            child: Container(
              decoration: const BoxDecoration(
                gradient: RadialGradient(
                  center: Alignment(-0.7, 0.6),
                  radius: 1.5,
                  colors: [
                    ZColors.neutral800,
                    ZColors.darkBackground,
                  ],
                ),
              ),
            ),
          ),
          
          SafeArea(
            child: Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 48),
                child: Column(
                  children: [
                    Image.asset('assets/Zorvian.png', height: 80, fit: BoxFit.contain),
                    const SizedBox(height: 48),
                    _buildGlassContainer(),
                  ],
                ),
              ),
            ),
          ),
          
          // BACK BUTTON
          Positioned(
            top: 24,
            left: 24,
            child: IconButton(
              icon: const Icon(Icons.arrow_back_ios_new_rounded, color: Colors.white70),
              onPressed: () => context.pop(),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildGlassContainer() {
    return ClipRRect(
      borderRadius: BorderRadius.circular(28),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 20, sigmaY: 20),
        child: Container(
          constraints: const BoxConstraints(maxWidth: 500),
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 40),
          decoration: BoxDecoration(
            color: Colors.white.withValues(alpha: 0.03),
            borderRadius: BorderRadius.circular(28),
            border: Border.all(color: Colors.white.withValues(alpha: 0.08), width: 1.5),
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                Colors.white.withValues(alpha: 0.05),
                Colors.white.withValues(alpha: 0.01),
              ],
            ),
          ),
          child: _buildForm(),
        ),
      ),
    );
  }

  Widget _buildForm() {
    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            _mode == RegisterMode.invite ? 'Registro Empresarial' : 'Solicitar Acceso',
            style: ZTypography.displaySmall.copyWith(color: Colors.white, fontSize: 32),
          ),
          const SizedBox(height: 8),
          Text(
            _mode == RegisterMode.invite 
              ? 'Únete a la infraestructura de tu organización' 
              : 'Completa los datos para validar tu identidad',
            style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral400),
          ),
          const SizedBox(height: 32),
          
          // MODE SWITCHER
          Container(
            padding: const EdgeInsets.all(4),
            decoration: BoxDecoration(
              color: Colors.black26,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Row(
              children: [
                _buildModeTab(RegisterMode.invite, 'Tengo un Código'),
                _buildModeTab(RegisterMode.request, 'Solicitar Acceso'),
              ],
            ),
          ),
          const SizedBox(height: 32),

          if (_error != null) _buildErrorBanner(),

          if (_mode == RegisterMode.invite) ...[
            _buildTextField(
              controller: _inviteCodeCtrl,
              label: 'Código de Invitación',
              icon: Icons.qr_code_rounded,
            ),
            const SizedBox(height: 20),
          ],
          
          _buildTextField(
            controller: _nameCtrl,
            label: 'Nombre Completo',
            icon: Icons.badge_outlined,
          ),
          const SizedBox(height: 20),
          
          _buildTextField(
            controller: _emailCtrl,
            label: 'Correo Electrónico',
            icon: Icons.alternate_email_rounded,
            validator: ZValidators.email,
          ),
          const SizedBox(height: 20),

          if (_mode == RegisterMode.request) ...[
            _buildTextField(
              controller: _companyCtrl,
              label: 'Empresa / Institución',
              icon: Icons.business_rounded,
            ),
            const SizedBox(height: 20),
            _buildTextField(
              controller: _phoneCtrl,
              label: 'Teléfono (opcional)',
              icon: Icons.phone_rounded,
              required: false,
              keyboardType: TextInputType.phone,
            ),
            const SizedBox(height: 20),
            _buildTextField(
              controller: _messageCtrl,
              label: 'Mensaje (opcional)',
              icon: Icons.chat_bubble_outline_rounded,
              required: false,
              maxLines: 3,
            ),
            const SizedBox(height: 20),
          ],

          if (_mode == RegisterMode.invite) ...[
            _buildTextField(
              controller: _passwordCtrl,
              label: 'Contraseña del Sistema',
              icon: Icons.lock_person_outlined,
              isPassword: true,
              validator: ZValidators.password,
            ),
            const SizedBox(height: 20),
            _buildTextField(
              controller: _confirmPasswordCtrl,
              label: 'Confirmar Contraseña',
              icon: Icons.lock_reset_rounded,
              isPassword: true,
              validator: (v) {
                if (v == null || v.isEmpty) return 'Confirma tu contraseña';
                if (v != _passwordCtrl.text) return 'Las contraseñas no coinciden';
                return null;
              },
            ),
          ],

          const SizedBox(height: 40),
          ZButton(
            text: _mode == RegisterMode.invite ? 'CREAR CUENTA' : 'ENVIAR SOLICITUD',
            onPressed: _submit,
            isLoading: _loading,
            gradient: ZColors.accentGradient,
          ),
        ],
      ),
    );
  }

  Widget _buildModeTab(RegisterMode mode, String label) {
    final active = _mode == mode;
    return Expanded(
      child: GestureDetector(
        onTap: () => setState(() => _mode = mode),
        child: Container(
          padding: const EdgeInsets.symmetric(vertical: 12),
          decoration: BoxDecoration(
            color: active ? Colors.white.withValues(alpha: 0.08) : Colors.transparent,
            borderRadius: BorderRadius.circular(8),
          ),
          child: Text(
            label,
            textAlign: TextAlign.center,
            style: ZTypography.labelSmall.copyWith(
              color: active ? ZColors.brandAccent : ZColors.neutral500,
              fontWeight: active ? FontWeight.bold : FontWeight.normal,
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String label,
    required IconData icon,
    bool isPassword = false,
    bool required = true,
    TextInputType keyboardType = TextInputType.text,
    int maxLines = 1,
    FormFieldValidator<String>? validator,
  }) {
    return ZTextField(
      controller: controller,
      label: label,
      obscureText: isPassword,
      keyboardType: keyboardType,
      maxLines: maxLines,
      prefix: Icon(icon),
      validator: validator ??
          ((v) => required && (v == null || v.isEmpty) ? 'Este campo es requerido' : null),
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
