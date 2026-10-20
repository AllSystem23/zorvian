import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/roles_admin_provider.dart';

/// Matriz de permisos granulares por rol de la compañía.
///
/// Quien tenga `role.manage` (CompanyAdmin lo trae sembrado) puede editar qué
/// permisos tiene cada rol. Los permisos de plataforma
/// (company.manage / role.manage / apikey.manage) solo los otorga o revoca
/// SuperAdmin: para los demás el chip aparece bloqueado con una nota, y muestra
/// siempre el estado REAL del rol (no uno inventado).
class RolesPermissionsPage extends ConsumerStatefulWidget {
  const RolesPermissionsPage({super.key});

  @override
  ConsumerState<RolesPermissionsPage> createState() =>
      _RolesPermissionsPageState();
}

class _RolesPermissionsPageState extends ConsumerState<RolesPermissionsPage> {
  final Map<String, Set<String>> _drafts = {};
  String? _savingRoleId;

  bool get _isSuperAdmin => ref.read(authProvider).role == 'SuperAdmin';

  Set<String> _current(RolePermissionModel role) =>
      _drafts[role.id] ?? role.permissions.toSet();

  void _toggle(RolePermissionModel role, String code, bool? value) {
    if (value == null) return;
    setState(() {
      final draft = _current(role).toSet();
      value ? draft.add(code) : draft.remove(code);
      _drafts[role.id] = draft;
    });
  }

  Future<void> _save(RolePermissionModel role) async {
    final permissions = _current(role).toList()..sort();
    setState(() => _savingRoleId = role.id);
    try {
      await ref
          .read(rolesAdminProvider.notifier)
          .updatePermissions(role.id, permissions);
      if (!mounted) return;
      setState(() => _drafts.remove(role.id));
      ZToast.show(
        context,
        'Permisos de "${role.displayName}" actualizados. Los usuarios verán '
        'los cambios al refrescar su sesión.',
        type: ZToastType.success,
      );
    } on DioException catch (e) {
      if (!mounted) return;
      ZToast.show(context, _messageFrom(e, 'No se pudieron guardar los permisos'),
          type: ZToastType.error);
    } catch (_) {
      if (!mounted) return;
      ZToast.show(context, 'No se pudieron guardar los permisos',
          type: ZToastType.error);
    } finally {
      if (mounted) setState(() => _savingRoleId = null);
    }
  }

  /// Mensaje legible del backend (`{error: "..."}`) con fallback.
  String _messageFrom(DioException e, String fallback) {
    final data = e.response?.data;
    if (data is Map && data['error'] != null) return '${data['error']}';
    return fallback;
  }

  Future<void> _createRole() async {
    final created = await showZAdaptiveDialog<bool>(
      context,
      builder: (_) => _RoleFormDialog(
        title: 'Nuevo rol',
        onSubmit: (name, description) async {
          try {
            await ref.read(rolesAdminProvider.notifier).createRole(
                  displayName: name,
                  description: description,
                );
            return null;
          } on DioException catch (e) {
            return _messageFrom(e, 'No se pudo crear el rol');
          } catch (_) {
            return 'No se pudo crear el rol';
          }
        },
      ),
    );
    if (created == true && mounted) {
      ZToast.show(
        context,
        'Rol creado. Asígnale permisos y usuarios.',
        type: ZToastType.success,
      );
    }
  }

  Future<void> _renameRole(RolePermissionModel role) async {
    final saved = await showZAdaptiveDialog<bool>(
      context,
      builder: (_) => _RoleFormDialog(
        title: 'Editar rol',
        initialName: role.displayName,
        initialDescription: role.description,
        onSubmit: (name, description) async {
          try {
            await ref.read(rolesAdminProvider.notifier).updateRole(
                  role.id,
                  displayName: name,
                  description: description ?? '',
                );
            return null;
          } on DioException catch (e) {
            return _messageFrom(e, 'No se pudo actualizar el rol');
          } catch (_) {
            return 'No se pudo actualizar el rol';
          }
        },
      ),
    );
    if (saved == true && mounted) {
      ZToast.show(context, 'Rol actualizado.', type: ZToastType.success);
    }
  }

  Future<void> _deleteRole(RolePermissionModel role) async {
    final confirmed = await ZConfirmDialog.show(
      context,
      title: 'Eliminar rol',
      message: '¿Eliminar el rol "${role.displayName}"? '
          'Esta acción no se puede deshacer.',
      confirmLabel: 'Eliminar',
      icon: Icons.delete_outline,
      isDestructive: true,
    );
    if (!confirmed) return;

    try {
      await ref.read(rolesAdminProvider.notifier).deleteRole(role.id);
      if (!mounted) return;
      setState(() => _drafts.remove(role.id));
      ZToast.show(context, 'Rol "${role.displayName}" eliminado.',
          type: ZToastType.success);
    } on DioException catch (e) {
      if (!mounted) return;
      ZToast.show(context, _messageFrom(e, 'No se pudo eliminar el rol'),
          type: ZToastType.error);
    } catch (_) {
      if (!mounted) return;
      ZToast.show(context, 'No se pudo eliminar el rol', type: ZToastType.error);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final adminAsync = ref.watch(rolesAdminProvider);

    return Scaffold(
      backgroundColor: isDark ? ZColors.darkBackground : ZColors.background,
      body: adminAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text('Error al cargar roles: $e', style: ZTypography.bodyMedium),
              const SizedBox(height: 12),
              ZButton(
                text: 'Reintentar',
                fullWidth: false,
                onPressed: () => ref.read(rolesAdminProvider.notifier).load(),
              ),
            ],
          ),
        ),
        data: (state) => _buildContent(state, isDark),
      ),
    );
  }

  Widget _buildContent(RolesAdminState state, bool isDark) {
    final grouped = state.groupedCatalog;
    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildHeader(),
          const SizedBox(height: 12),
          _buildNote(isDark),
          const SizedBox(height: 24),
          for (final role in state.roles)
            Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: _buildRoleCard(role, grouped, isDark),
            ),
        ],
      ),
    );
  }

  Widget _buildHeader() {
    return Row(
      children: [
        Container(
          width: 48,
          height: 48,
          decoration: BoxDecoration(
            gradient: const LinearGradient(colors: ZColors.accentGradient),
            borderRadius: BorderRadius.circular(12),
          ),
          child: const Icon(Icons.security_outlined,
              color: Colors.white, size: 24),
        ),
        const SizedBox(width: 16),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Roles y Permisos', style: ZTypography.headlineLarge),
              Text(
                'Define qué puede hacer cada rol en esta compañía',
                style:
                    ZTypography.bodyMedium.copyWith(color: ZColors.neutral500),
              ),
            ],
          ),
        ),
        ZButton(
          text: 'Nuevo rol',
          icon: Icons.add,
          fullWidth: false,
          onPressed: _createRole,
        ),
        const SizedBox(width: 8),
        IconButton(
          icon: const Icon(Icons.refresh),
          tooltip: 'Actualizar',
          onPressed: () => ref.read(rolesAdminProvider.notifier).load(),
        ),
      ],
    );
  }

  Widget _buildNote(bool isDark) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: ZColors.info.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: ZColors.info.withValues(alpha: 0.3)),
      ),
      child: Row(
        children: [
          const Icon(Icons.info_outline, size: 18, color: ZColors.info),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              'Los permisos viajan en el token JWT: los cambios aplican cuando '
              'el usuario vuelva a iniciar sesión o refresque su token '
              '(acceso 1h). El rol SuperAdmin siempre tiene todos los permisos y '
              'es el único que puede otorgar los de plataforma (compañías, '
              'roles y API keys), marcados con candado.',
              style: ZTypography.bodySmall.copyWith(
                  color: isDark ? ZColors.neutral200 : ZColors.neutral700),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildRoleCard(
    RolePermissionModel role,
    Map<String, List<PermissionCatalogEntry>> grouped,
    bool isDark,
  ) {
    final isSuperAdminRole = role.name == 'SuperAdmin';
    final selected = _current(role);
    final dirty = _drafts.containsKey(role.id);
    final saving = _savingRoleId == role.id;

    return ZCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(
                isSuperAdminRole
                    ? Icons.workspace_premium_outlined
                    : Icons.security_outlined,
                size: 22,
                color: isSuperAdminRole ? ZColors.warning : ZColors.brandAccent,
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(role.displayName, style: ZTypography.titleMedium),
                    if (role.description != null &&
                        role.description!.isNotEmpty)
                      Text(role.description!,
                          style: ZTypography.bodySmall
                              .copyWith(color: ZColors.neutral500)),
                  ],
                ),
              ),
              if (isSuperAdminRole)
                const ZBadge(text: 'Siempre activo', type: ZBadgeType.warning)
              else ...[
                if (dirty)
                  const ZBadge(
                      text: 'Sin guardar', type: ZBadgeType.warning),
                const SizedBox(width: 8),
                ZButton(
                  text: 'Guardar',
                  icon: Icons.save_outlined,
                  fullWidth: false,
                  isLoading: saving,
                  onPressed: saving ? () {} : () => _save(role),
                ),
                if (role.isCustom) ...[
                  const SizedBox(width: 4),
                  PopupMenuButton<String>(
                    tooltip: 'Acciones del rol',
                    icon: const Icon(Icons.more_vert, size: 20),
                    onSelected: (value) {
                      if (value == 'rename') _renameRole(role);
                      if (value == 'delete') _deleteRole(role);
                    },
                    itemBuilder: (_) => const [
                      PopupMenuItem(
                        value: 'rename',
                        child: Text('Renombrar / editar'),
                      ),
                      PopupMenuItem(
                        value: 'delete',
                        child: Text('Eliminar rol'),
                      ),
                    ],
                  ),
                ],
              ],
            ],
          ),
          const SizedBox(height: 16),
          if (isSuperAdminRole)
            Text(
              'El rol SuperAdmin tiene bypass total en el backend y no es '
              'editable.',
              style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500),
            )
          else
            ...grouped.entries.map(
              (group) => _buildPermissionGroup(
                role,
                group.key,
                group.value,
                selected,
                isDark,
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildPermissionGroup(
    RolePermissionModel role,
    String group,
    List<PermissionCatalogEntry> entries,
    Set<String> selected,
    bool isDark,
  ) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            group.toUpperCase(),
            style: ZTypography.labelSmall.copyWith(
              color: ZColors.neutral500,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.8,
            ),
          ),
          const SizedBox(height: 6),
          Wrap(
            spacing: 8,
            runSpacing: 4,
            children: entries
                .map((e) => _buildPermissionChip(role, e, selected))
                .toList(),
          ),
        ],
      ),
    );
  }

  Widget _buildPermissionChip(
    RolePermissionModel role,
    PermissionCatalogEntry entry,
    Set<String> selected,
  ) {
    // Los permisos de plataforma solo los otorga/quita SuperAdmin.
    final lockedForCaller = entry.isSuperAdminOnly && !_isSuperAdmin;
    // El chip SIEMPRE refleja el estado real del rol; que el permiso sea de
    // plataforma solo cambia quién puede tocarlo, no si está activo.
    final checked = selected.contains(entry.code);

    return Tooltip(
      message: lockedForCaller
          ? 'Solo SuperAdmin puede otorgar o revocar este permiso de plataforma (${entry.code})'
          : entry.code,
      child: FilterChip(
        label: Text(entry.label),
        avatar: entry.isSuperAdminOnly
            ? const Icon(Icons.lock_outline, size: 14)
            : null,
        selected: checked,
        showCheckmark: true,
        onSelected:
            lockedForCaller ? null : (v) => _toggle(role, entry.code, v),
      ),
    );
  }
}

/// Formulario de creación/edición de un rol PERSONALIZADO (nombre y
/// descripción; los permisos se asignan con los chips de la tarjeta).
///
/// [onSubmit] devuelve `null` si el guardado fue exitoso, o el mensaje de error
/// que debe mostrarse dentro del diálogo (validación del backend).
class _RoleFormDialog extends StatefulWidget {
  const _RoleFormDialog({
    required this.title,
    required this.onSubmit,
    this.initialName,
    this.initialDescription,
  });

  final String title;
  final String? initialName;
  final String? initialDescription;
  final Future<String?> Function(String name, String? description) onSubmit;

  @override
  State<_RoleFormDialog> createState() => _RoleFormDialogState();
}

class _RoleFormDialogState extends State<_RoleFormDialog> {
  late final TextEditingController _name =
      TextEditingController(text: widget.initialName ?? '');
  late final TextEditingController _description =
      TextEditingController(text: widget.initialDescription ?? '');
  String? _error;
  bool _saving = false;

  @override
  void dispose() {
    _name.dispose();
    _description.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final name = _name.text.trim();
    if (name.isEmpty) {
      setState(() => _error = 'El nombre del rol es obligatorio');
      return;
    }
    final description = _description.text.trim();
    setState(() {
      _saving = true;
      _error = null;
    });

    final error = await widget.onSubmit(
      name,
      description.isEmpty ? null : description,
    );

    if (!mounted) return;
    if (error != null) {
      setState(() {
        _saving = false;
        _error = error;
      });
      return;
    }
    Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) {
    return ZResponsiveDialog(
      title: widget.title,
      icon: Icons.admin_panel_settings_outlined,
      maxWidth: 460,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          TextField(
            controller: _name,
            autofocus: true,
            textInputAction: TextInputAction.next,
            decoration: const InputDecoration(
              labelText: 'Nombre del rol',
              hintText: 'p. ej. Auditor de Ventas',
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: _description,
            maxLines: 2,
            decoration: const InputDecoration(
              labelText: 'Descripción (opcional)',
            ),
          ),
          if (_error != null) ...[
            const SizedBox(height: 12),
            Text(
              _error!,
              style: ZTypography.bodySmall.copyWith(color: ZColors.danger),
            ),
          ],
          const SizedBox(height: 10),
          Text(
            'Los permisos se asignan después con los chips de la tarjeta '
            'del rol. Los roles se distinguen por su nombre.',
            style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500),
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: _saving ? null : () => Navigator.pop(context, false),
          child: const Text('Cancelar'),
        ),
        ZButton(
          text: 'Guardar',
          fullWidth: false,
          isLoading: _saving,
          onPressed: _saving ? () {} : _submit,
        ),
      ],
    );
  }
}
