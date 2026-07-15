import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

class InvitationHistoryPage extends ConsumerStatefulWidget {
  const InvitationHistoryPage({super.key});

  @override
  ConsumerState<InvitationHistoryPage> createState() =>
      _InvitationHistoryPageState();
}

class _InvitationHistoryPageState
    extends ConsumerState<InvitationHistoryPage> {
  bool _loading = true;
  String? _error;
  List<Map<String, dynamic>> _invitations = [];
  String _filterStatus = 'all'; // 'all', 'pending', 'used'
  final _searchCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    _loadInvitations();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadInvitations() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('invitations');
      setState(() {
        _invitations = List<Map<String, dynamic>>.from(response.data);
        _loading = false;
      });
    } catch (e) {
      setState(() {
        _error = 'Error al cargar invitaciones: $e';
        _loading = false;
      });
    }
  }

  String _formatDate(String? dateStr) {
    if (dateStr == null) return '—';
    try {
      final date = DateTime.parse(dateStr);
      return '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}/${date.year} ${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}';
    } catch (_) {
      return dateStr;
    }
  }

  DateTime? _safeParseDate(String? dateStr) {
    if (dateStr == null) return null;
    try {
      return DateTime.parse(dateStr);
    } catch (_) {
      return null;
    }
  }

  bool _isExpired(Map<String, dynamic> inv) {
    if (inv['isUsed'] == true) return false;
    final expiresAt = _safeParseDate(inv['expiresAt'] as String?);
    return expiresAt != null && expiresAt.isBefore(DateTime.now());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Padding(
        padding: const EdgeInsets.all(ZSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header
            Row(
              children: [
                const Expanded(
                  child: Text(
                    'Historial de Invitaciones',
                    style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                  ),
                ),
                ZButton(
                  text: 'Nueva Invitación',
                  icon: Icons.person_add_outlined,
                  fullWidth: false,
                  onPressed: () => context.push('/admin/invite'),
                ),
              ],
            ),
            const SizedBox(height: ZSpacing.md),
            // Filters
            Row(
              children: [
                Expanded(
                  child: ZTextField(
                    label: 'Buscar por email',
                    prefix: const Icon(Icons.search, size: 18),
                    controller: _searchCtrl,
                    onChanged: (value) => setState(() {}),
                  ),
                ),
                const SizedBox(width: ZSpacing.md),
                _buildFilterChip('Todos', 'all'),
                const SizedBox(width: 8),
                _buildFilterChip('Pendientes', 'pending'),
                const SizedBox(width: 8),
                _buildFilterChip('Usadas', 'used'),
                const SizedBox(width: 8),
                _buildFilterChip('Expiradas', 'expired'),
              ],
            ),
            const SizedBox(height: ZSpacing.md),
            // Content
            Expanded(
              child: _loading
                  ? const Center(child: CircularProgressIndicator())
                  : _error != null
                      ? _buildError()
                      : _invitations.isEmpty
                          ? _buildEmpty()
                          : _buildTable(),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildError() {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.error_outline, size: 48, color: ZColors.danger),
          const SizedBox(height: 16),
          Text(_error!, style: const TextStyle(color: ZColors.danger)),
          const SizedBox(height: 16),
          ZButton(
            text: 'Reintentar',
            fullWidth: false,
            onPressed: _loadInvitations,
          ),
        ],
      ),
    );
  }

  Widget _buildEmpty() {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.mail_outline, size: 64, color: ZColors.neutral400),
          const SizedBox(height: 16),
          const Text(
            'No hay invitaciones enviadas',
            style: TextStyle(fontSize: 16, color: ZColors.neutral500),
          ),
          const SizedBox(height: 8),
          const Text(
            'Crea tu primera invitación para comenzar a invitar usuarios',
            style: TextStyle(color: ZColors.neutral400),
          ),
          const SizedBox(height: 24),
          ZButton(
            text: 'Crear Invitación',
            icon: Icons.person_add_outlined,
            fullWidth: false,
            onPressed: () => context.push('/admin/invite'),
          ),
        ],
      ),
    );
  }

  List<Map<String, dynamic>> get _filteredInvitations {
    return _invitations.where((inv) {
      // Filter by status
      final invExpired = _isExpired(inv);
      if (_filterStatus == 'pending' && (inv['isUsed'] == true || invExpired)) return false;
      if (_filterStatus == 'used' && inv['isUsed'] != true) return false;
      if (_filterStatus == 'expired' && !invExpired) return false;
      // Filter by email search
      if (_searchCtrl.text.isNotEmpty) {
        final email = (inv['email'] as String? ?? '').toLowerCase();
        if (!email.contains(_searchCtrl.text.toLowerCase())) return false;
      }
      return true;
    }).toList();
  }

  Widget _buildTable() {
    final filtered = _filteredInvitations;
    if (filtered.isEmpty && (_searchCtrl.text.isNotEmpty || _filterStatus != 'all')) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.search_off, size: 48, color: ZColors.neutral400),
            const SizedBox(height: 16),
            const Text(
              'No se encontraron invitaciones con estos filtros',
              style: TextStyle(fontSize: 16, color: ZColors.neutral500),
            ),
            const SizedBox(height: 12),
            ZButton(
              text: 'Limpiar Filtros',
              fullWidth: false,
              type: ZButtonType.secondary,
              onPressed: () {
                setState(() {
                  _searchCtrl.clear();
                  _filterStatus = 'all';
                });
              },
            ),
          ],
        ),
      );
    }
    return ZDataTable<Map<String, dynamic>>(
      columns: const [
        ZColumn(id: 'code', label: 'Código', width: 100),
        ZColumn(id: 'email', label: 'Email'),
        ZColumn(id: 'role', label: 'Rol', width: 120),
        ZColumn(id: 'status', label: 'Estado', width: 120),
        ZColumn(id: 'created', label: 'Creada', width: 140),
        ZColumn(id: 'expires', label: 'Expira', width: 140),
        ZColumn(id: 'used', label: 'Uso', width: 140),
        ZColumn(id: 'actions', label: '', width: 80),
      ],
      rows: filtered,
      rowMapper: (item) {
        final isUsed = item['isUsed'] == true;
        final isExpired = _isExpired(item);
        return DataRow(cells: [
          DataCell(
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  item['code'] ?? '—',
                  style: const TextStyle(
                    fontFamily: 'monospace',
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(width: 4),
                InkWell(
                  onTap: () {
                    final code = item['code'] as String?;
                    if (code != null) {
                      Clipboard.setData(ClipboardData(text: code));
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text('Código $code copiado'),
                          duration: const Duration(seconds: 2),
                        ),
                      );
                    }
                  },
                  borderRadius: BorderRadius.circular(4),
                  child: const Padding(
                    padding: EdgeInsets.all(4),
                    child: Icon(Icons.copy_rounded, size: 14, color: ZColors.neutral400),
                  ),
                ),
              ],
            ),
          ),
          DataCell(Text(item['email'] ?? '—')),
          DataCell(_buildRoleBadge(item['role'] ?? 'Employee')),
          DataCell(_buildStatusBadge(isUsed, isExpired)),
          DataCell(Text(_formatDate(item['createdAt']))),
          DataCell(Text(_formatDate(item['expiresAt']))),
          DataCell(Text(_formatDate(item['usedAt']))),
          DataCell(
            isUsed
                ? const SizedBox.shrink()
                : IconButton(
                    icon: const Icon(Icons.refresh_rounded, size: 18, color: ZColors.brandAccent),
                    tooltip: 'Regenerar código',
                    onPressed: () => _regenerate(item['id']),
                  ),
          ),
        ]);
      },
    );
  }

  Widget _buildFilterChip(String label, String value) {
    final isSelected = _filterStatus == value;
    return FilterChip(
      label: Text(label, style: TextStyle(fontSize: 12, fontWeight: isSelected ? FontWeight.w600 : FontWeight.normal)),
      selected: isSelected,
      onSelected: (_) => setState(() => _filterStatus = value),
      selectedColor: ZColors.brandAccent.withValues(alpha: 0.15),
      checkmarkColor: ZColors.brandAccent,
      side: BorderSide(color: isSelected ? ZColors.brandAccent.withValues(alpha: 0.4) : ZColors.neutral300),
    );
  }

  Future<void> _regenerate(String? id) async {
    if (id == null) return;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        icon: const Icon(Icons.warning_amber_rounded, color: ZColors.warning, size: 36),
        title: const Text('Regenerar código'),
        content: const Text('Se generará un nuevo código. El anterior dejará de funcionar.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Regenerar')),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.post('invitations/$id/regenerate');
      final newCode = response.data['code'];
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Nuevo código: $newCode')),
        );
        _loadInvitations();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e'), backgroundColor: ZColors.danger),
        );
      }
    }
  }

  Widget _buildStatusBadge(bool isUsed, bool isExpired) {
    if (isUsed) {
      return const ZBadge(text: 'USADA', type: ZBadgeType.success);
    }
    if (isExpired) {
      return const ZBadge(text: 'EXPIRADA', type: ZBadgeType.danger);
    }
    return const ZBadge(text: 'PENDIENTE', type: ZBadgeType.warning);
  }

  Widget _buildRoleBadge(String role) {
    final colors = {
      'SuperAdmin': ZColors.danger,
      'CompanyAdmin': ZColors.brandAccent,
      'Rrhh': ZColors.moduleHr,
      'Supervisor': ZColors.moduleSales,
      'Employee': ZColors.neutral500,
    };
    final color = colors[role] ?? ZColors.neutral500;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(6),
        border: Border.all(color: color.withValues(alpha: 0.3)),
      ),
      child: Text(
        role,
        style: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.w600,
          color: color,
        ),
      ),
    );
  }
}
