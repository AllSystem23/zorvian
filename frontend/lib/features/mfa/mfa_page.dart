import 'dart:async';
import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:qr_flutter/qr_flutter.dart';
import '../../auth/auth_provider.dart';
import '../../core/widgets/particle_background.dart';
import '../../core/widgets/responsive_layout.dart';
import '../../shared/ds/ds.dart';

/// MFA verification page — shown after password login when MFA is enabled.
class MfaLoginPage extends ConsumerStatefulWidget {
  final String mfaToken;

  const MfaLoginPage({super.key, required this.mfaToken});

  @override
  ConsumerState<MfaLoginPage> createState() => _MfaLoginPageState();
}

class _MfaLoginPageState extends ConsumerState<MfaLoginPage> {
  final _codeController = TextEditingController();
  final _formKey = GlobalKey<FormState>();
  String? _error;
  bool _loading = false;

  @override
  void dispose() {
    _codeController.dispose();
    super.dispose();
  }

  void _onVerify() {
    unawaited(_verify());
  }

  Future<void> _verify() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final success = await ref.read(authProvider.notifier).completeMfaLogin(
            widget.mfaToken,
            _codeController.text.trim(),
          );
      if (!success && mounted) {
        setState(() => _error = 'Código MFA inválido. Intenta de nuevo.');
      }
    } catch (e) {
      if (mounted) {
        setState(() => _error = 'Error al verificar MFA: ${e.toString().replaceFirst('Exception: ', '')}');
      }
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
          const Positioned.fill(
            child: ParticleBackground(particleCount: 30, speed: 0.4),
          ),
          Positioned.fill(
            child: Container(
              decoration: BoxDecoration(
                gradient: RadialGradient(
                  center: const Alignment(0.7, -0.6),
                  radius: 1.5,
                  colors: [
                    ZColors.neutral800.withValues(alpha: 0.7),
                    ZColors.darkBackground.withValues(alpha: 0.3),
                    Colors.transparent,
                  ],
                  stops: const [0.0, 0.5, 1.0],
                ),
              ),
            ),
          ),
          SafeArea(
            child: ResponsiveBuilder(
              builder: (_, size) => Center(
                child: SingleChildScrollView(
                  padding: const EdgeInsets.all(32),
                  child: _buildMfaCard(),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMfaCard() {
    final theme = Theme.of(context);
    return ClipRRect(
      borderRadius: BorderRadius.circular(28),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 20, sigmaY: 20),
        child: Container(
          constraints: const BoxConstraints(maxWidth: 440),
          padding: const EdgeInsets.symmetric(horizontal: 40, vertical: 48),
          decoration: BoxDecoration(
            color: Colors.white.withValues(alpha: 0.03),
            borderRadius: BorderRadius.circular(28),
            border: Border.all(color: Colors.white.withValues(alpha: 0.08), width: 1.5),
          ),
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Center(
                  child: Container(
                    width: 72,
                    height: 72,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: ZColors.brandAccent.withValues(alpha: 0.15),
                      border: Border.all(color: ZColors.brandAccent.withValues(alpha: 0.3)),
                    ),
                    child: const Icon(Icons.shield_outlined, size: 36, color: ZColors.brandAccent),
                  ),
                ),
                const SizedBox(height: 24),
                Text(
                  'Verificación MFA',
                  style: ZTypography.displaySmall.copyWith(color: Colors.white, letterSpacing: -0.5),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 12),
                Text(
                  'Ingresa el código de 6 dígitos de tu\naplicación de autenticación.',
                  style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral400),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 40),
                if (_error != null) _buildErrorBanner(theme),
                _buildCodeField(),
                const SizedBox(height: 32),
                ZButton(
                  text: 'VERIFICAR',
                  onPressed: _onVerify,
                  isLoading: _loading,
                  gradient: ZColors.accentGradient,
                ),
                const SizedBox(height: 16),
                TextButton(
                  onPressed: () => context.go('/login'),
                  child: Text(
                    'Volver al login',
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

  Widget _buildCodeField() {
    return ZTextField(
      controller: _codeController,
      label: 'Código de verificación',
      prefix: const Icon(Icons.pin_outlined),
      keyboardType: TextInputType.number,
      validator: (v) {
        if (v == null || v.isEmpty) return 'El código es requerido';
        if (v.length != 6) return 'El código debe tener 6 dígitos';
        return null;
      },
    );
  }

  Widget _buildErrorBanner(ThemeData theme) {
    return Container(
      padding: const EdgeInsets.all(12),
      margin: const EdgeInsets.only(bottom: 24),
      decoration: BoxDecoration(
        color: theme.colorScheme.error.withValues(alpha: 0.2),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: theme.colorScheme.error.withValues(alpha: 0.3)),
      ),
      child: Row(
        children: [
          Icon(Icons.error_outline, size: 20, color: theme.colorScheme.error),
          const SizedBox(width: 12),
          Expanded(
            child: Text(_error!, style: TextStyle(color: theme.colorScheme.error, fontSize: 13)),
          ),
        ],
      ),
    );
  }
}

/// MFA settings page — manage MFA from the user profile.
class MfaSettingsPage extends ConsumerStatefulWidget {
  const MfaSettingsPage({super.key});

  @override
  ConsumerState<MfaSettingsPage> createState() => _MfaSettingsPageState();
}

class _MfaSettingsPageState extends ConsumerState<MfaSettingsPage> {
  bool _mfaEnabled = false;
  bool _loading = true;
  String? _qrUri;
  String? _secretKey;
  final _codeController = TextEditingController();
  final _passwordController = TextEditingController();
  String? _error;
  String? _success;
  bool _submitting = false;

  @override
  void initState() {
    super.initState();
    unawaited(_loadStatus());
  }

  Future<void> _loadStatus() async {
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('auth/mfa/status');
      setState(() {
        _mfaEnabled = response.data['mfa_enabled'] ?? false;
        _loading = false;
      });
    } catch (_) {
      setState(() => _loading = false);
    }
  }

  void _onEnableMfa() {
    unawaited(_enableMfa());
  }

  Future<void> _enableMfa() async {
    setState(() {
      _submitting = true;
      _error = null;
      _success = null;
    });
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.post('auth/mfa/generate');
      setState(() {
        _qrUri = response.data['uri'];
        _secretKey = response.data['secretKey'];
      });
    } catch (_) {
      setState(() => _error = 'Error al generar código MFA');
    } finally {
      setState(() => _submitting = false);
    }
  }

  void _onVerifyAndEnable() {
    unawaited(_verifyAndEnable());
  }

  Future<void> _verifyAndEnable() async {
    if (_codeController.text.length != 6) return;
    setState(() {
      _submitting = true;
      _error = null;
      _success = null;
    });
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('auth/mfa/verify', data: {'code': _codeController.text});
      setState(() {
        _mfaEnabled = true;
        _qrUri = null;
        _secretKey = null;
        _success = 'MFA activado correctamente';
      });
      _codeController.clear();
    } catch (_) {
      setState(() => _error = 'Código inválido. Verifica e intenta de nuevo.');
    } finally {
      setState(() => _submitting = false);
    }
  }

  void _onDisableMfa() {
    unawaited(_disableMfa());
  }

  Future<void> _disableMfa() async {
    if (_passwordController.text.isEmpty || _codeController.text.length != 6) return;
    setState(() {
      _submitting = true;
      _error = null;
      _success = null;
    });
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('auth/mfa/disable', data: {
        'password': _passwordController.text,
        'code': _codeController.text,
      });
      setState(() {
        _mfaEnabled = false;
        _success = 'MFA desactivado correctamente';
      });
      _passwordController.clear();
      _codeController.clear();
    } catch (_) {
      setState(() => _error = 'Contraseña o código inválido');
    } finally {
      setState(() => _submitting = false);
    }
  }

  @override
  void dispose() {
    _codeController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (_loading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Autenticación de Dos Factores')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Status card
            ZCard(
              child: Row(
                children: [
                  Icon(
                    _mfaEnabled ? Icons.shield : Icons.shield_outlined,
                    size: 40,
                    color: _mfaEnabled ? ZColors.brandTeal : ZColors.neutral400,
                  ),
                  const SizedBox(width: ZSpacing.lg),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          _mfaEnabled ? 'MFA Activado' : 'MFA Desactivado',
                          style: theme.textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.bold,
                            color: _mfaEnabled ? ZColors.brandTeal : null,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          _mfaEnabled
                              ? 'Tu cuenta está protegida con autenticación de dos factores.'
                              : 'Activa MFA para añadir una capa extra de seguridad.',
                          style: theme.textTheme.bodySmall?.copyWith(color: ZColors.neutral500),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            if (_success != null) ...[
              const SizedBox(height: ZSpacing.md),
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: ZColors.brandTeal.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: ZColors.brandTeal.withValues(alpha: 0.3)),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.check_circle, color: ZColors.brandTeal, size: 20),
                    const SizedBox(width: 8),
                    Expanded(child: Text(_success!, style: const TextStyle(color: ZColors.brandTeal, fontSize: 13))),
                  ],
                ),
              ),
            ],
            if (_error != null) ...[
              const SizedBox(height: ZSpacing.md),
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: theme.colorScheme.error.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: theme.colorScheme.error.withValues(alpha: 0.3)),
                ),
                child: Row(
                  children: [
                    Icon(Icons.error_outline, color: theme.colorScheme.error, size: 20),
                    const SizedBox(width: 8),
                    Expanded(child: Text(_error!, style: TextStyle(color: theme.colorScheme.error, fontSize: 13))),
                  ],
                ),
              ),
            ],
            const SizedBox(height: ZSpacing.xl),
            // Enable MFA flow
            if (!_mfaEnabled) ...[
              if (_qrUri == null) ...[
                ZButton(
                  text: 'ACTIVAR MFA',
                  onPressed: _onEnableMfa,
                  isLoading: _submitting,
                  gradient: ZColors.accentGradient,
                ),
              ] else ...[
                ZCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Escanea este QR con tu app de autenticación:',
                          style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                      const SizedBox(height: ZSpacing.md),
                      Center(child: QrCodeWidget(uri: _qrUri!)),
                      if (_secretKey != null) ...[
                        const SizedBox(height: ZSpacing.md),
                        Text('Clave secreta:', style: theme.textTheme.bodySmall),
                        const SizedBox(height: 4),
                        Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: SelectableText(_secretKey!, style: const TextStyle(fontFamily: 'monospace', fontSize: 14)),
                        ),
                      ],
                      const SizedBox(height: ZSpacing.lg),
                      ZTextField(controller: _codeController, label: 'Código de verificación', prefix: const Icon(Icons.pin_outlined), keyboardType: TextInputType.number),
                      const SizedBox(height: ZSpacing.md),
                      ZButton(text: 'CONFIRMAR Y ACTIVAR', onPressed: _onVerifyAndEnable, isLoading: _submitting),
                    ],
                  ),
                ),
              ],
            ],
            // Disable MFA flow
            if (_mfaEnabled) ...[
              ZCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Desactivar MFA', style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                    const SizedBox(height: ZSpacing.md),
                    ZTextField(controller: _passwordController, label: 'Contraseña actual', prefix: const Icon(Icons.lock_outline), obscureText: true),
                    const SizedBox(height: ZSpacing.md),
                    ZTextField(controller: _codeController, label: 'Código MFA actual', prefix: const Icon(Icons.pin_outlined), keyboardType: TextInputType.number),
                    const SizedBox(height: ZSpacing.md),
                    ZButton(text: 'DESACTIVAR MFA', onPressed: _onDisableMfa, isLoading: _submitting),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// QR code display — renders locally so the TOTP secret never leaves the device.
class QrCodeWidget extends StatelessWidget {
  final String uri;
  const QrCodeWidget({super.key, required this.uri});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 200,
      height: 200,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
      ),
      padding: const EdgeInsets.all(12),
      child: QrImageView(
        data: uri,
        version: QrVersions.auto,
        size: 176,
      ),
    );
  }
}
