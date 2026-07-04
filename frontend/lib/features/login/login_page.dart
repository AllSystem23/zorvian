import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../../core/widgets/particle_background.dart';
import '../../core/widgets/responsive_layout.dart';
import '../../shared/ds/ds.dart';

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: ZColors.darkBackground,
      body: Stack(
        children: [
          // ANIMATED PARTICLE BACKGROUND
          Positioned.fill(
            child: const ParticleBackground(
              particleCount: 50,
              speed: 0.6,
            ),
          ),
          // PREMIUM BACKGROUND GRADIENT (over particles)
          Positioned.fill(
            child: Container(
              decoration: BoxDecoration(
                gradient: RadialGradient(
                  center: Alignment(0.7, -0.6),
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
          // AMBIENT GLOWS
          Positioned(
            top: -100,
            right: -100,
            child: ClipRRect(
              child: Container(
                width: 400,
                height: 400,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: ZColors.brandAccent.withValues(alpha: 0.05),
                ),
                child: BackdropFilter(filter: ImageFilter.blur(sigmaX: 100, sigmaY: 100), child: Container()),
              ),
            ),
          ),
          Positioned(
            bottom: -50,
            left: -50,
            child: ClipRRect(
              child: Container(
                width: 300,
                height: 300,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: ZColors.brandTeal.withValues(alpha: 0.03),
                ),
                child: BackdropFilter(filter: ImageFilter.blur(sigmaX: 80, sigmaY: 80), child: Container()),
              ),
            ),
          ),
          
          SafeArea(
            child: ResponsiveBuilder(
              builder: (_, size) => size == ScreenSize.desktop
                  ? _buildDesktopLayout()
                  : _buildMobileLayout(),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDesktopLayout() {
    return Row(
      children: [
        Expanded(
          flex: 4,
          child: Padding(
            padding: EdgeInsets.symmetric(horizontal: MediaQuery.of(context).size.width > 1200 ? 80 : 40),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Hero(
                  tag: 'logo',
                  child: Image.asset(
                    'assets/Zorvian.png',
                    height: 140,
                    fit: BoxFit.contain,
                  ),
                ),
                const SizedBox(height: 48),
                Text(
                  'ZORVIAN ERP',
                  style: ZTypography.labelSmall.copyWith(
                    color: ZColors.brandAccent,
                    letterSpacing: 4.0,
                  ),
                ),
                const SizedBox(height: 16),
                Text(
                  'La Próxima Generación de\nGestión Empresarial.',
                  style: ZTypography.displayLarge.copyWith(
                    color: Colors.white,
                    fontSize: 56,
                  ),
                ),
                const SizedBox(height: 24),
                Container(
                  width: 60,
                  height: 4,
                  decoration: BoxDecoration(
                    color: ZColors.brandAccent,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
                const SizedBox(height: 32),
                Text(
                  'Sistemas inteligentes diseñados para la eficiencia operativa absoluta y la toma de decisiones basada en datos.',
                  style: ZTypography.bodyLarge.copyWith(
                    color: ZColors.neutral400,
                    fontSize: 18,
                  ),
                ),
              ],
            ),
          ),
        ),
        Expanded(
          flex: 3,
          child: Center(
            child: Container(
              constraints: const BoxConstraints(maxWidth: 480),
              margin: const EdgeInsets.only(right: 60),
              child: const LoginForm(),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildMobileLayout() {
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(32.0),
        child: Column(
          children: [
            Image.asset(
              'assets/Zorvian.png',
              height: 100,
              fit: BoxFit.contain,
            ),
            const SizedBox(height: 56),
            const LoginForm(),
          ],
        ),
      ),
    );
  }
}

class LoginForm extends ConsumerStatefulWidget {
  const LoginForm({super.key});

  @override
  ConsumerState<LoginForm> createState() => _LoginFormState();
}

class _LoginFormState extends ConsumerState<LoginForm> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _emailFocusNode = FocusNode();
  final _passwordFocusNode = FocusNode();
  final _formKey = GlobalKey<FormState>();
  String? _error;
  bool _loading = false;
  bool _obscurePassword = true;
  bool _rememberMe = false;

  @override
  void initState() {
    super.initState();
    _loadSavedCredentials();
  }

  Future<void> _loadSavedCredentials() async {
    final storage = ref.read(secureStorageProvider);
    final (email, password) = await storage.getRememberedCredentials();
    if (email != null && mounted) {
      _emailController.text = email;
      if (password != null) {
        _passwordController.text = password;
      }
      setState(() => _rememberMe = true);
    }
  }

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    _emailFocusNode.dispose();
    _passwordFocusNode.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final success = await ref.read(authProvider.notifier).loginWithPassword(
            _emailController.text.trim(),
            _passwordController.text,
          );
      if (success) {
        final storage = ref.read(secureStorageProvider);
        if (_rememberMe) {
          await storage.saveRememberedCredentials(
            _emailController.text.trim(),
            _passwordController.text,
          );
        } else {
          await storage.clearRememberedCredentials();
        }
      } else if (mounted) {
        setState(() => _error = 'Error al conectar con el servidor');
      }
    } catch (e) {
      if (mounted) {
        setState(() => _error = e is Exception ? e.toString().replaceFirst('Exception: ', '') : 'Error de conexión');
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ClipRRect(
      borderRadius: BorderRadius.circular(28),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 20, sigmaY: 20),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 40, vertical: 48),
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
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Iniciar Sesión',
                  style: ZTypography.displaySmall.copyWith(
                    color: Colors.white,
                    letterSpacing: -0.5,
                  ),
                ),
                const SizedBox(height: 12),
                Text(
                  'Gestión Empresarial de Alto Nivel',
                  style: ZTypography.bodyMedium.copyWith(
                    color: ZColors.neutral400,
                  ),
                ),
                const SizedBox(height: 40),
                if (_error != null) _buildErrorBanner(theme),
                _buildTextField(
                  controller: _emailController,
                  focusNode: _emailFocusNode,
                  label: 'ID Corporativo / Email',
                  icon: Icons.person_outline_rounded,
                ),
                const SizedBox(height: 24),
                _buildTextField(
                  controller: _passwordController,
                  focusNode: _passwordFocusNode,
                  label: 'Contraseña',
                  icon: Icons.lock_outline_rounded,
                  isPassword: true,
                ),
                const SizedBox(height: 16),
                Theme(
                  data: theme.copyWith(
                    unselectedWidgetColor: ZColors.neutral500,
                  ),
                  child: Material(
                    type: MaterialType.transparency,
                    child: CheckboxListTile(
                      value: _rememberMe,
                      onChanged: (v) => setState(() => _rememberMe = v ?? false),
                      title: Text(
                        'Recordar sesión en este dispositivo',
                        style: ZTypography.bodySmall.copyWith(color: ZColors.neutral300),
                      ),
                      controlAffinity: ListTileControlAffinity.leading,
                      contentPadding: EdgeInsets.zero,
                      dense: true,
                      activeColor: ZColors.brandAccent,
                      checkColor: ZColors.brandPrimary,
                      side: BorderSide(color: Colors.white.withValues(alpha: 0.2)),
                    ),
                  ),
                ),
                const SizedBox(height: 32),
                ZButton(
                  text: 'ACCEDER AL SISTEMA',
                  onPressed: _login,
                  isLoading: _loading,
                  gradient: ZColors.accentGradient,
                ),
                const SizedBox(height: 24),
                ZButton(
                  text: '¿No tienes cuenta? Solicita acceso',
                  onPressed: () => context.push('/register'),
                  type: ZButtonType.ghost,
                ),
                const SizedBox(height: 8),
                TextButton(
                  onPressed: () => context.push('/forgot-password'),
                  child: Text(
                    '¿Olvidaste tu contraseña?',
                    style: ZTypography.labelSmall.copyWith(
                      color: ZColors.neutral500,
                    ),
                    textAlign: TextAlign.center,
                  ),
                ),
                const SizedBox(height: 16),
                _buildCountryBadge(),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildTextField({
    Key? key,
    required TextEditingController controller,
    required String label,
    required IconData icon,
    bool isPassword = false,
    FocusNode? focusNode,
  }) {
    return ZTextField(
      key: key,
      controller: controller,
      focusNode: focusNode,
      label: label,
      obscureText: isPassword && _obscurePassword,
      prefix: Icon(icon),
      suffix: isPassword
          ? IconButton(
              icon: Icon(
                _obscurePassword ? Icons.visibility_off : Icons.visibility,
                semanticLabel: _obscurePassword ? 'Mostrar contraseña' : 'Ocultar contraseña',
              ),
              onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
              tooltip: _obscurePassword ? 'Mostrar contraseña' : 'Ocultar contraseña',
            )
          : null,
      validator: (v) => v == null || v.isEmpty ? 'Este campo es obligatorio' : null,
    );
  }

  Widget _buildCountryBadge() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.03),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: Colors.white.withValues(alpha: 0.06)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.language, size: 14, color: ZColors.neutral500),
          const SizedBox(width: 8),
          Text(
            'NI · CR · GT · HN · SV · PA',
            style: TextStyle(fontSize: 11, color: ZColors.neutral500, letterSpacing: 1.5),
          ),
        ],
      ),
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
            child: Text(
              _error!,
              style: TextStyle(color: theme.colorScheme.error, fontSize: 13),
            ),
          ),
        ],
      ),
    );
  }
}