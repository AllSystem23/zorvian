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
  bool _saving = false;

  @override
  void dispose() {
    _phoneCtrl.dispose();
    _photoUrlCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() => _saving = true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('employees/me', data: {
        'phone': _phoneCtrl.text,
        'photoUrl': _photoUrlCtrl.text,
      });
      ref.invalidate(profileProvider);
      setState(() => _editing = false);
    } catch (_) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Error al guardar')));
    } finally {
      setState(() => _saving = false);
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
                _phoneCtrl.text = p?['phone'] as String? ?? '';
                _photoUrlCtrl.text = p?['photoUrl'] as String? ?? '';
                setState(() => _editing = true);
              },
            )
          else
            IconButton(
              icon: _saving ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.check),
              onPressed: _saving ? null : _save,
            ),
        ],
      ),
      body: profileAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, stack) {
          return Center(
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: SelectableText(
                'Error al cargar perfil:\n$e\n\nStack:\n$stack',
                style: const TextStyle(color: Colors.red),
              ),
            ),
          );
        },
        data: (profile) {
          if (profile == null) return const Center(child: Text('Perfil no encontrado'));
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Center(
                child: CircleAvatar(
                  radius: 48,
                  backgroundColor: theme.colorScheme.primary,
                  child: Text(
                    _editing && _photoUrlCtrl.text.isNotEmpty
                        ? '...'
                        : auth.displayName?.isNotEmpty == true ? auth.displayName![0] : '?',
                    style: const TextStyle(fontSize: 36, color: Colors.white),
                  ),
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
                  _InfoRow(
                    label: 'Foto URL',
                    child: ZTextField(controller: _photoUrlCtrl, label: 'Foto URL'),
                  ),
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
              _BiometricSection(),
            ],
          );
        },
      ),
    );
  }
}

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
    return _Section(title: 'Seguridad', children: [
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
