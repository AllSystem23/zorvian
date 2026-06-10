import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../auth/auth_provider.dart';
import '../../shared/ds/ds.dart';

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  @override
  Widget build(BuildContext context) {
    debugPrint('LoginPage build reconstruido');
    final isDesktop = MediaQuery.sizeOf(context).width > 600;

    return Scaffold(
      body: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [
              Color(0xFF0B1F3B),
              Color(0xFF1E3A5F),
              Color(0xFF0B1F3B),
            ],
          ),
        ),
        child: isDesktop ? _buildDesktopLayout() : _buildMobileLayout(),
      ),
    );
  }

  Widget _buildDesktopLayout() {
    return Row(
      children: [
        Expanded(
          flex: 3,
          child: Padding(
            padding: const EdgeInsets.all(64.0),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Image.asset(
                  'assets/Zorvian.png',
                  height: 120,
                  fit: BoxFit.contain,
                  excludeFromSemantics: true,
                ),
                const SizedBox(height: 32),
                const Text(
                  'Bienvenido a Zorvian ERP',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 48,
                    fontWeight: FontWeight.bold,
                    letterSpacing: -1.5,
                  ),
                ),
                const SizedBox(height: 16),
                const Text(
                  'Gestión inteligente de recursos humanos y operaciones empresariales en una sola plataforma.',
                  style: TextStyle(
                    color: Colors.white70,
                    fontSize: 20,
                    height: 1.5,
                  ),
                ),
              ],
            ),
          ),
        ),
        Expanded(
          flex: 2,
          child: Center(
            child: Container(
              constraints: const BoxConstraints(maxWidth: 450),
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
        child: Padding(
          padding: const EdgeInsets.all(24.0),
          child: Column(
            children: [
              Image.asset(
                'assets/Zorvian.png',
                height: 100,
                fit: BoxFit.contain,
                excludeFromSemantics: true,
              ),
              const SizedBox(height: 48),
              const LoginForm(),
            ],
          ),
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
    } catch (_) {
      if (mounted) setState(() => _error = 'Error de conexión');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    debugPrint('LoginForm build reconstruido');
    final theme = Theme.of(context);

    return ClipRRect(
      borderRadius: BorderRadius.circular(24),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 15, sigmaY: 15),
        child: Container(
          padding: const EdgeInsets.all(40),
          decoration: BoxDecoration(
            color: Colors.white.withValues(alpha: 0.1),
            borderRadius: BorderRadius.circular(24),
            border: Border.all(color: Colors.white.withValues(alpha: 0.2)),
          ),
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Text(
                  'Iniciar Sesión',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 28,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  'Ingresa tus credenciales para continuar',
                  style: TextStyle(color: Colors.white.withValues(alpha: 0.6)),
                ),
                const SizedBox(height: 32),
                if (_error != null) _buildErrorBanner(theme),
                _buildTextField(
                  controller: _emailController,
                  focusNode: _emailFocusNode,
                  label: 'Correo electrónico',
                  icon: Icons.email_outlined,
                ),
                const SizedBox(height: 20),
                _buildTextField(
                  controller: _passwordController,
                  focusNode: _passwordFocusNode,
                  label: 'Contraseña',
                  icon: Icons.lock_outlined,
                  isPassword: true,
                ),
                CheckboxListTile(
                  value: _rememberMe,
                  onChanged: (v) => setState(() => _rememberMe = v ?? false),
                  title: const Text('Recordar credenciales',
                      style: TextStyle(color: Colors.white, fontSize: 14)),
                  controlAffinity: ListTileControlAffinity.leading,
                  contentPadding: EdgeInsets.zero,
                  dense: true,
                  activeColor: Colors.white,
                  checkColor: const Color(0xFF0B1F3B),
                  side: const BorderSide(color: Colors.white54),
                ),
                const SizedBox(height: 24),
                ZButton(
                  text: 'Entrar al Sistema',
                  onPressed: _login,
                  isLoading: _loading,
                ),
                const SizedBox(height: 20),
                ZButton(
                  text: '¿No tienes cuenta? Solicita acceso',
                  onPressed: () => context.push('/register'),
                  type: ZButtonType.ghost,
                  fullWidth: false,
                ),
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