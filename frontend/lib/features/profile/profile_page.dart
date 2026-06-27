import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:dio/dio.dart';
import '../../auth/auth_provider.dart';
import '../../core/network/api_config.dart';
import '../../shared/ds/ds.dart';
import '../biometrics/providers/biometric_provider.dart';

final profileProvider = FutureProvider.autoDispose<Map<String, dynamic>?>((ref) async {
  final dio = ref.read(dioClientProvider);
  try {
    final r = await dio.get('employees/me');
    if (r.data == null || (r.data is Map && r.data.isEmpty)) return null;
    return r.data as Map<String, dynamic>;
  } catch (e) {
    if (e is DioException) {
      if (e.response?.statusCode == 401) return null;
    }
    rethrow;
  }
});

class ProfilePage extends ConsumerStatefulWidget {
  const ProfilePage({super.key});

  @override
  ConsumerState<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends ConsumerState<ProfilePage> {
  bool _editing = false;
  final _phoneCtrl = TextEditingController();
  final _photoUrlCtrl = TextEditingController();
  final _displayNameCtrl = TextEditingController();
  bool _saving = false;

  @override
  void dispose() {
    _phoneCtrl.dispose();
    _photoUrlCtrl.dispose();
    _displayNameCtrl.dispose();
    super.dispose();
  }

  Future<void> _saveProfile() async {
    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);

      if (_displayNameCtrl.text.isNotEmpty) {
        await dio.put('auth/me', data: {
          'displayName': _displayNameCtrl.text.trim(),
        });
        ref.read(authProvider.notifier).checkAuth();
      }

      await dio.put('employees/me', data: {
        'phone': _phoneCtrl.text,
        'photoUrl': _photoUrlCtrl.text,
      });

      ref.invalidate(profileProvider);
      if (mounted) {
        setState(() => _editing = false);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Perfil actualizado')),
        );
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error al guardar'), backgroundColor: ZColors.danger),
        );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final auth = ref.watch(authProvider);
    final profileAsync = ref.watch(profileProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Mi Perfil'),
        actions: [
          if (!_editing)
            IconButton(
              icon: const Icon(Icons.edit),
              onPressed: () {
                final p = profileAsync.asData?.value;
                _displayNameCtrl.text = auth.displayName ?? '';
                _phoneCtrl.text = p?['phone'] as String? ?? '';
                _photoUrlCtrl.text = p?['photoUrl'] as String? ?? '';
                setState(() => _editing = true);
              },
            )
          else
            IconButton(
              icon: _saving ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.check),
              onPressed: _saving ? null : _saveProfile,
            ),
        ],
      ),
      body: profileAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, stack) => Center(
          child: Padding(
            padding: const EdgeInsets.all(16.0),
            child: SelectableText('Error al cargar perfil:\n$e', style: const TextStyle(color: Colors.red)),
          ),
        ),
        data: (profile) {
          if (profile == null) return const Center(child: Text('Perfil no encontrado'));
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Center(
                child: Stack(
                  children: [
                    CircleAvatar(
                      radius: 48,
                      backgroundColor: theme.colorScheme.primary,
                      child: Text(
                        auth.displayName?.isNotEmpty == true ? auth.displayName![0] : '?',
                        style: const TextStyle(fontSize: 36, color: Colors.white),
                      ),
                    ),
                    if (_editing)
                      Positioned(
                        bottom: 0,
                        right: 0,
                        child: Container(
                          padding: const EdgeInsets.all(4),
                          decoration: const BoxDecoration(color: ZColors.brandAccent, shape: BoxShape.circle),
                          child: const Icon(Icons.camera_alt, size: 16, color: Colors.white),
                        ),
                      ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              Center(
                child: Text(profile['fullName'] as String? ?? '', style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold)),
              ),
              Center(
                child: Text(profile['employeeCode'] as String? ?? '', style: const TextStyle(color: Colors.grey)),
              ),
              const SizedBox(height: 24),

              _Section(title: 'Información Personal', children: [
                if (_editing)
                  _InfoRow(label: 'Nombre', child: ZTextField(controller: _displayNameCtrl, label: 'Nombre de usuario'))
                else
                  _InfoRow(label: 'Nombre', text: auth.displayName ?? ''),
                _InfoRow(label: 'Email', text: profile['email'] as String? ?? ''),
                _InfoRow(
                  label: 'Teléfono',
                  text: _editing ? null : (profile['phone'] as String? ?? '—'),
                  child: _editing ? ZTextField(controller: _phoneCtrl, label: 'Teléfono') : null,
                ),
                _InfoRow(label: 'Fecha Nacimiento', text: profile['dateOfBirth'] as String? ?? '—'),
                _InfoRow(label: 'Género', text: profile['gender'] as String? ?? '—'),
                _InfoRow(label: 'Identificación', text: profile['identificationNumber'] as String? ?? '—'),
                if (_editing)
                  _InfoRow(label: 'Foto URL', child: ZTextField(controller: _photoUrlCtrl, label: 'Foto URL')),
              ]),
              const SizedBox(height: 16),

              _Section(title: 'Laboral', children: [
                _InfoRow(label: 'Departamento', text: profile['departmentName'] as String? ?? '—'),
                _InfoRow(label: 'Puesto', text: profile['position'] as String? ?? '—'),
                _InfoRow(label: 'Fecha Contratación', text: profile['hireDate'] as String? ?? ''),
                _InfoRow(label: 'Estado', text: profile['status'] as String? ?? ''),
                const SizedBox(height: 8),
                FilledButton.icon(
                  icon: const Icon(Icons.download, size: 18),
                  label: const Text('Descargar Constancia'),
                  onPressed: () async {
                    final storage = ref.read(secureStorageProvider);
                    final token = await storage.getAccessToken();
                    final uri = Uri.parse(ApiConfig.resolve('employees/me/certificate')).replace(queryParameters: {'access_token': token ?? ''});
                    if (await canLaunchUrl(uri)) {
                      await launchUrl(uri, mode: LaunchMode.externalApplication);
                    }
                  },
                ),
              ]),
              const SizedBox(height: 16),

              const _EmailChangeSection(),
              const SizedBox(height: 16),
              const _PasswordChangeSection(),
              const SizedBox(height: 16),
              _BiometricSection(),
            ],
          );
        },
      ),
    );
  }
}

// ─── Email Change Section ───

class _EmailChangeSection extends ConsumerStatefulWidget {
  const _EmailChangeSection();

  @override
  ConsumerState<_EmailChangeSection> createState() => _EmailChangeSectionState();
}

class _EmailChangeSectionState extends ConsumerState<_EmailChangeSection> {
  bool _expanded = false;
  final _newEmailCtrl = TextEditingController();
  final _codeCtrl = TextEditingController();
  bool _loading = false;
  bool _codeSent = false;
  String? _error;

  @override
  void dispose() {
    _newEmailCtrl.dispose();
    _codeCtrl.dispose();
    super.dispose();
  }

  void _reset() {
    _newEmailCtrl.clear();
    _codeCtrl.clear();
    _codeSent = false;
    _error = null;
  }

  Future<void> _requestChange() async {
    if (_newEmailCtrl.text.isEmpty || !_newEmailCtrl.text.contains('@')) {
      setState(() => _error = 'Ingresa un email válido');
      return;
    }

    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('auth/request-email-change', data: {
        'newEmail': _newEmailCtrl.text.trim(),
      });
      if (mounted) {
        setState(() { _codeSent = true; _loading = false; });
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Código de verificación enviado al nuevo email')),
        );
      }
    } on DioException catch (e) {
      if (mounted) {
        setState(() {
          _error = e.response?.data?['error'] as String? ?? 'Error al solicitar cambio';
          _loading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() { _error = 'Error al solicitar cambio de email'; _loading = false; });
      }
    }
  }

  Future<void> _confirmChange() async {
    if (_codeCtrl.text.length != 6) {
      setState(() => _error = 'El código debe tener 6 dígitos');
      return;
    }

    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('auth/confirm-email-change', data: {
        'newEmail': _newEmailCtrl.text.trim(),
        'verificationCode': _codeCtrl.text.trim(),
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Email actualizado correctamente')),
        );
        _reset();
        setState(() => _expanded = false);
        ref.read(authProvider.notifier).checkAuth();
      }
    } on DioException catch (e) {
      if (mounted) {
        setState(() {
          _error = e.response?.data?['error'] as String? ?? 'Error al confirmar cambio';
          _loading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() { _error = 'Error al confirmar cambio de email'; _loading = false; });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = ref.watch(authProvider);

    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          InkWell(
            onTap: () => setState(() {
              _expanded = !_expanded;
              if (!_expanded) _reset();
            }),
            child: Row(
              children: [
                const Icon(Icons.email_outlined, size: 20, color: ZColors.brandPrimary),
                const SizedBox(width: 8),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Email', style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                      Text(auth.email ?? '', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
                    ],
                  ),
                ),
                Icon(_expanded ? Icons.expand_less : Icons.expand_more),
              ],
            ),
          ),
          if (_expanded) ...[
            const Divider(),
            const SizedBox(height: 12),
            if (_error != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: ZAlertCard(message: _error!, severity: 'high'),
              ),
            if (!_codeSent) ...[
              TextField(
                controller: _newEmailCtrl,
                keyboardType: TextInputType.emailAddress,
                decoration: const InputDecoration(
                  labelText: 'Nuevo email',
                  prefixIcon: Icon(Icons.email_outlined, size: 18),
                  border: OutlineInputBorder(),
                  isDense: true,
                  contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 12),
                ),
              ),
              const SizedBox(height: 16),
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: _loading ? null : _requestChange,
                  icon: _loading
                      ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                      : const Icon(Icons.send, size: 18),
                  label: const Text('Enviar Código de Verificación'),
                ),
              ),
            ] else ...[
              ZAlertCard(
                message: 'Se envió un código de 6 dígitos a ${_newEmailCtrl.text}',
                severity: 'info',
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _codeCtrl,
                keyboardType: TextInputType.number,
                maxLength: 6,
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 24, letterSpacing: 8, fontWeight: FontWeight.bold),
                decoration: const InputDecoration(
                  labelText: 'Código de verificación',
                  counterText: '',
                  prefixIcon: Icon(Icons.pin_outlined, size: 18),
                  border: OutlineInputBorder(),
                  isDense: true,
                  contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 12),
                ),
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _loading ? null : () { _reset(); },
                      child: const Text('Cancelar'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: FilledButton.icon(
                      onPressed: _loading ? null : _confirmChange,
                      icon: _loading
                          ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                          : const Icon(Icons.check, size: 18),
                      label: const Text('Confirmar'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              TextButton(
                onPressed: _loading ? null : _requestChange,
                child: const Text('Reenviar código'),
              ),
            ],
          ],
        ],
      ),
    );
  }
}

// ─── Password Change Section ───

class _PasswordChangeSection extends ConsumerStatefulWidget {
  const _PasswordChangeSection();

  @override
  ConsumerState<_PasswordChangeSection> createState() => _PasswordChangeSectionState();
}

class _PasswordChangeSectionState extends ConsumerState<_PasswordChangeSection> {
  bool _expanded = false;
  final _currentPasswordCtrl = TextEditingController();
  final _newPasswordCtrl = TextEditingController();
  final _confirmPasswordCtrl = TextEditingController();
  bool _loading = false;
  bool _obscureCurrent = true;
  bool _obscureNew = true;

  @override
  void dispose() {
    _currentPasswordCtrl.dispose();
    _newPasswordCtrl.dispose();
    _confirmPasswordCtrl.dispose();
    super.dispose();
  }

  Future<void> _changePassword() async {
    if (_currentPasswordCtrl.text.isEmpty || _newPasswordCtrl.text.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Completa todos los campos'), backgroundColor: ZColors.danger),
      );
      return;
    }
    if (_newPasswordCtrl.text != _confirmPasswordCtrl.text) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Las contraseñas nuevas no coinciden'), backgroundColor: ZColors.danger),
      );
      return;
    }
    if (_newPasswordCtrl.text.length < 6) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('La nueva contraseña debe tener al menos 6 caracteres'), backgroundColor: ZColors.danger),
      );
      return;
    }

    setState(() => _loading = true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('auth/change-password', data: {
        'currentPassword': _currentPasswordCtrl.text,
        'newPassword': _newPasswordCtrl.text,
        'confirmPassword': _confirmPasswordCtrl.text,
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Contraseña actualizada correctamente')),
        );
        _currentPasswordCtrl.clear();
        _newPasswordCtrl.clear();
        _confirmPasswordCtrl.clear();
        setState(() => _expanded = false);
      }
    } on DioException catch (e) {
      if (mounted) {
        final msg = e.response?.data?['error'] as String? ?? 'Error al cambiar contraseña';
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(msg), backgroundColor: ZColors.danger),
        );
      }
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error al cambiar contraseña'), backgroundColor: ZColors.danger),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          InkWell(
            onTap: () => setState(() => _expanded = !_expanded),
            child: Row(
              children: [
                const Icon(Icons.lock_outline, size: 20, color: ZColors.brandPrimary),
                const SizedBox(width: 8),
                Expanded(
                  child: Text('Seguridad', style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                ),
                Icon(_expanded ? Icons.expand_less : Icons.expand_more),
              ],
            ),
          ),
          if (_expanded) ...[
            const Divider(),
            const SizedBox(height: 12),
            TextField(
              controller: _currentPasswordCtrl,
              obscureText: _obscureCurrent,
              decoration: InputDecoration(
                labelText: 'Contraseña actual',
                prefixIcon: const Icon(Icons.lock_outline, size: 18),
                suffixIcon: IconButton(
                  icon: Icon(_obscureCurrent ? Icons.visibility_off : Icons.visibility, size: 18),
                  onPressed: () => setState(() => _obscureCurrent = !_obscureCurrent),
                ),
                border: const OutlineInputBorder(),
                isDense: true,
                contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _newPasswordCtrl,
              obscureText: _obscureNew,
              decoration: InputDecoration(
                labelText: 'Nueva contraseña',
                prefixIcon: const Icon(Icons.lock_reset, size: 18),
                suffixIcon: IconButton(
                  icon: Icon(_obscureNew ? Icons.visibility_off : Icons.visibility, size: 18),
                  onPressed: () => setState(() => _obscureNew = !_obscureNew),
                ),
                border: const OutlineInputBorder(),
                isDense: true,
                contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: _confirmPasswordCtrl,
              obscureText: true,
              decoration: const InputDecoration(
                labelText: 'Confirmar nueva contraseña',
                prefixIcon: Icon(Icons.lock_reset, size: 18),
                border: OutlineInputBorder(),
                isDense: true,
                contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 12),
              ),
            ),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: _loading ? null : _changePassword,
                icon: _loading
                    ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : const Icon(Icons.save, size: 18),
                label: const Text('Actualizar Contraseña'),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

// ─── Shared widgets ───

class _Section extends StatelessWidget {
  final String title;
  final List<Widget> children;
  const _Section({required this.title, required this.children});

  @override
  Widget build(BuildContext context) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
          const Divider(),
          ...children,
        ],
      ),
    );
  }
}

class _BiometricSection extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final bioState = ref.watch(biometricProvider);
    if (!bioState.isAvailable) return const SizedBox.shrink();
    return _Section(title: 'Biométrico', children: [
      SwitchListTile(
        title: const Text('Acceso biométrico'),
        subtitle: Text(bioState.isEnabled ? 'Usa huella/FaceID para desbloquear' : 'Activar para desbloquear con huella/FaceID'),
        value: bioState.isEnabled && !bioState.loading,
        onChanged: bioState.loading ? null : (_) => ref.read(biometricProvider.notifier).toggleEnabled(),
      ),
    ]);
  }
}

class _InfoRow extends StatelessWidget {
  final String label;
  final String? text;
  final Widget? child;
  const _InfoRow({required this.label, this.text, this.child});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(width: 140, child: Text(label, style: const TextStyle(color: Colors.grey))),
          Expanded(child: child ?? Text(text ?? '')),
        ],
      ),
    );
  }
}
