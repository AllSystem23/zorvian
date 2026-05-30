import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';

class CommandItem {
  final IconData icon;
  final String label;
  final String? route;
  final String? shortcut;
  final VoidCallback? onSelected;
  final List<String> keywords;

  const CommandItem({
    required this.icon,
    required this.label,
    this.route,
    this.shortcut,
    this.onSelected,
    this.keywords = const [],
  });
}

final List<CommandItem> _commands = [
  CommandItem(icon: Icons.dashboard, label: 'Dashboard', route: '/dashboard', shortcut: 'G D', keywords: ['inicio', 'home']),
  CommandItem(icon: Icons.people, label: 'Empleados', route: '/employees', shortcut: 'G E', keywords: ['lista', 'personal']),
  CommandItem(icon: Icons.business, label: 'Departamentos', route: '/departments', shortcut: 'G P', keywords: ['areas']),
  CommandItem(icon: Icons.beach_access, label: 'Vacaciones', route: '/vacations', shortcut: 'G V', keywords: ['dias', 'descanso']),
  CommandItem(icon: Icons.description, label: 'Permisos', route: '/permissions', shortcut: 'G R', keywords: ['ausencia', 'licencia']),
  CommandItem(icon: Icons.schedule, label: 'Asistencia', route: '/attendance', shortcut: 'G A', keywords: ['marcar', 'check', 'reloj']),
  CommandItem(icon: Icons.calendar_month, label: 'Calendario de Ausencias', route: '/absence-calendar', keywords: ['ausencias']),
  CommandItem(icon: Icons.assessment, label: 'Reportes', route: '/reports', shortcut: 'G R', keywords: ['informes', 'excel']),
  CommandItem(icon: Icons.history, label: 'Auditoría', route: '/audit-logs', keywords: ['audit', 'log', 'bitacora']),
  CommandItem(icon: Icons.person, label: 'Mi Perfil', route: '/profile', shortcut: 'G P', keywords: ['cuenta']),
  CommandItem(icon: Icons.admin_panel_settings, label: 'Admin / Usuarios', route: '/admin/users', keywords: ['usuarios', 'roles', 'admin']),
  CommandItem(icon: Icons.settings, label: 'Configuración', route: '/settings', keywords: ['compania', 'empresa']),
  CommandItem(icon: Icons.event_note, label: 'Tipos de Permiso', route: '/leave-types', keywords: ['tipos', 'licencias']),
  CommandItem(icon: Icons.qr_code_scanner, label: 'QR Check-in', route: '/attendance/qr', keywords: ['qr', 'checkin', 'marcar']),
];

class CommandPalette extends StatefulWidget {
  final BuildContext appContext;

  const CommandPalette({super.key, required this.appContext});

  static void show(BuildContext context) {
    showDialog(
      context: context,
      barrierDismissible: true,
      barrierColor: Colors.black54,
      builder: (_) => CommandPalette(appContext: context),
    );
  }

  @override
  State<CommandPalette> createState() => _CommandPaletteState();
}

class _CommandPaletteState extends State<CommandPalette> {
  final _controller = TextEditingController();
  final _focusNode = FocusNode();
  late List<CommandItem> _filtered;
  int _selectedIndex = 0;

  @override
  void initState() {
    super.initState();
    _filtered = _commands;
    _focusNode.requestFocus();
  }

  @override
  void dispose() {
    _controller.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  void _onSearchChanged(String query) {
    final q = query.toLowerCase();
    setState(() {
      _filtered = q.isEmpty
          ? _commands
          : _commands.where((c) =>
              c.label.toLowerCase().contains(q) ||
              c.keywords.any((k) => k.contains(q)) ||
              (c.route?.contains(q) ?? false))
          .toList();
      _selectedIndex = 0;
    });
  }

  void _select(int index) {
    if (index < 0 || index >= _filtered.length) return;
    final item = _filtered[index];
    Navigator.pop(context);
    if (item.onSelected != null) {
      item.onSelected!();
    } else if (item.route != null) {
      widget.appContext.push(item.route!);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return KeyboardListener(
      focusNode: FocusNode(),
      onKeyEvent: (event) {
        if (event is KeyDownEvent) {
          if (event.logicalKey == LogicalKeyboardKey.arrowDown) {
            setState(() => _selectedIndex = (_selectedIndex + 1) % _filtered.length);
          } else if (event.logicalKey == LogicalKeyboardKey.arrowUp) {
            setState(() => _selectedIndex = (_selectedIndex - 1 + _filtered.length) % _filtered.length);
          } else if (event.logicalKey == LogicalKeyboardKey.enter) {
            _select(_selectedIndex);
          } else if (event.logicalKey == LogicalKeyboardKey.escape) {
            Navigator.pop(context);
          }
        }
      },
      child: Center(
        child: Material(
          color: Colors.transparent,
          child: Container(
            constraints: const BoxConstraints(maxWidth: 560, maxHeight: 480),
            margin: const EdgeInsets.symmetric(horizontal: 24),
            decoration: BoxDecoration(
              color: isDark ? const Color(0xFF1E1E2E) : Colors.white,
              borderRadius: BorderRadius.circular(16),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.3),
                  blurRadius: 32,
                  offset: const Offset(0, 16),
                ),
              ],
            ),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(16),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Padding(
                    padding: const EdgeInsets.all(12),
                    child: TextField(
                      controller: _controller,
                      focusNode: _focusNode,
                      onChanged: _onSearchChanged,
                      style: theme.textTheme.bodyLarge,
                      decoration: InputDecoration(
                        hintText: 'Buscar páginas y acciones...',
                        prefixIcon: const Icon(Icons.search, size: 20),
                        suffixIcon: _controller.text.isNotEmpty
                            ? IconButton(
                                icon: const Icon(Icons.clear, size: 18),
                                onPressed: () {
                                  _controller.clear();
                                  _onSearchChanged('');
                                },
                              )
                            : null,
                        border: InputBorder.none,
                        filled: false,
                        contentPadding: const EdgeInsets.symmetric(vertical: 12),
                      ),
                    ),
                  ),
                  Divider(height: 1, color: theme.dividerColor),
                  if (_filtered.isEmpty)
                    Padding(
                      padding: const EdgeInsets.all(32),
                      child: Column(
                        children: [
                          Icon(Icons.search_off, size: 40, color: theme.colorScheme.outline),
                          const SizedBox(height: 8),
                          Text('Sin resultados', style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.outline)),
                        ],
                      ),
                    )
                  else
                    Flexible(
                      child: ListView.builder(
                        shrinkWrap: true,
                        itemCount: _filtered.length,
                        padding: const EdgeInsets.symmetric(vertical: 4),
                        itemBuilder: (_, i) {
                          final item = _filtered[i];
                          final selected = i == _selectedIndex;
                          return ListTile(
                            selected: selected,
                            selectedTileColor: theme.colorScheme.primaryContainer.withValues(alpha: 0.3),
                            leading: Icon(item.icon, size: 20, color: theme.colorScheme.primary),
                            title: Text(item.label, style: const TextStyle(fontSize: 14)),
                            trailing: item.shortcut != null
                                ? Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                    decoration: BoxDecoration(
                                      color: theme.colorScheme.surfaceContainerHighest,
                                      borderRadius: BorderRadius.circular(4),
                                    ),
                                    child: Text(
                                      item.shortcut!,
                                      style: TextStyle(fontSize: 11, color: theme.colorScheme.outline, letterSpacing: 1),
                                    ),
                                  )
                                : null,
                            onTap: () => _select(i),
                            dense: true,
                          );
                        },
                      ),
                    ),
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        _HintChip(label: '↑↓', desc: 'Navegar'),
                        const SizedBox(width: 12),
                        _HintChip(label: 'Enter', desc: 'Ir'),
                        const SizedBox(width: 12),
                        _HintChip(label: 'Esc', desc: 'Cerrar'),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _HintChip extends StatelessWidget {
  final String label;
  final String desc;

  const _HintChip({required this.label, required this.desc});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
          decoration: BoxDecoration(
            color: theme.colorScheme.surfaceContainerHighest,
            borderRadius: BorderRadius.circular(3),
            border: Border.all(color: theme.colorScheme.outlineVariant),
          ),
          child: Text(label, style: TextStyle(fontSize: 10, color: theme.colorScheme.outline)),
        ),
        const SizedBox(width: 4),
        Text(desc, style: TextStyle(fontSize: 11, color: theme.colorScheme.outline)),
      ],
    );
  }
}
