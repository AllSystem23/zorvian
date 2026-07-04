import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';

class PermissionDetailPage extends ConsumerStatefulWidget {
  final String permissionId;
  const PermissionDetailPage({super.key, required this.permissionId});

  @override
  ConsumerState<PermissionDetailPage> createState() => _PermissionDetailPageState();
}

class _PermissionDetailPageState extends ConsumerState<PermissionDetailPage> {
  Map<String, dynamic>? _permission;
  bool _loading = true;
  String? _error;
  bool _actionLoading = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('permissions/${widget.permissionId}');
      setState(() => _permission = r.data as Map<String, dynamic>);
    } catch (e) {
      setState(() => _error = 'Error al cargar permiso');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _approve() async {
    final comment = await _askComment('¿Aprobar esta solicitud?');
    if (comment == null) return;

    setState(() => _actionLoading = true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('permissions/${widget.permissionId}/approve', data: {'comment': comment});
      _load();
    } catch (e) {
      _showError('Error al aprobar');
    } finally {
      if (mounted) setState(() => _actionLoading = false);
    }
  }

  Future<void> _reject() async {
    final reason = await _askComment('Motivo de rechazo');
    if (reason == null || reason.trim().isEmpty) {
      _showError('Debe indicar un motivo de rechazo');
      return;
    }

    setState(() => _actionLoading = true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('permissions/${widget.permissionId}/reject', data: {'comment': reason});
      _load();
    } catch (e) {
      _showError('Error al rechazar');
    } finally {
      if (mounted) setState(() => _actionLoading = false);
    }
  }

  Future<String?> _askComment(String title) {
    final ctrl = TextEditingController();
    return ZModal.show<String>(context,
      title: title,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ZTextField(
            controller: ctrl,
            label: '',
            hint: 'Comentario (opcional)',
            maxLines: 3,
          ),
          const SizedBox(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancelar')),
              const SizedBox(width: 8),
              ZButton(
                onPressed: () => Navigator.pop(context, ctrl.text),
                text: 'Confirmar',
              ),
            ],
          ),
        ],
      ),
    );
  }

  void _showError(String msg) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg), backgroundColor: Colors.red));
  }

  Color _statusColor(String s) {
    switch (s) {
      case 'approved': return Colors.green;
      case 'rejected': return Colors.red;
      case 'pending': return Colors.orange;
      default: return Colors.grey;
    }
  }

  String _statusLabel(String s) {
    switch (s) {
      case 'approved': return 'Aprobado';
      case 'rejected': return 'Rechazado';
      case 'pending': return 'Pendiente';
      default: return s;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: const Text('Detalle de permiso')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)))
              : _permission == null
                  ? const Center(child: Text('Permiso no encontrado'))
                  : _buildContent(theme),
    );
  }

  Widget _buildContent(ThemeData theme) {
    final p = _permission!;
    final status = p['status'] as String? ?? '';

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          ZCard(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(p['leaveTypeName'] as String? ?? '', style: theme.textTheme.titleLarge),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                      decoration: BoxDecoration(
                        color: _statusColor(status).withValues(alpha: 0.15),
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: Text(
                        _statusLabel(status),
                        style: TextStyle(color: _statusColor(status), fontWeight: FontWeight.w600),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                _infoRow('Trabajador', '${p['employeeName'] ?? ''} (${p['employeeCode'] ?? ''})'),
                _infoRow('Período', '${p['startDate'] ?? ''} → ${p['endDate'] ?? ''}'),
                _infoRow('Días totales', '${p['totalDays'] ?? 0}'),
                _infoRow('Días hábiles', '${p['businessDays'] ?? 0}'),
                _infoRow('Pagado', p['isPaid'] == true ? 'Sí' : 'No'),
                if (p['reason'] != null && (p['reason'] as String).isNotEmpty)
                  _infoRow('Motivo', p['reason'] as String),
                if (p['rejectionReason'] != null)
                  _infoRow('Motivo de rechazo', p['rejectionReason'] as String, color: Colors.red),
              ],
            ),
          ),
          if (p['supportingDocumentUrl'] != null && (p['supportingDocumentUrl'] as String).isNotEmpty)
            ZCard(
              child: ListTile(
                leading: const Icon(Icons.attach_file),
                title: Text(p['supportingDocumentFileName'] as String? ?? 'Documento'),
                trailing: const Icon(Icons.open_in_new),
                onTap: () async {
                  final url = p['supportingDocumentUrl'] as String?;
                  if (url != null && url.isNotEmpty) {
                    final uri = Uri.tryParse(url);
                    if (uri != null && await canLaunchUrl(uri)) {
                      await launchUrl(uri, mode: LaunchMode.externalApplication);
                    }
                  }
                },
              ),
            ),
          if (status == 'pending' && _actionLoading)
            const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            ),
          if (status == 'pending' && !_actionLoading)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 16),
              child: Row(
                children: [
                  Expanded(
                    child: ElevatedButton.icon(
                      onPressed: _reject,
                      icon: const Icon(Icons.close),
                      style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
                      label: const Text('Rechazar'),
                    ),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: ElevatedButton.icon(
                      onPressed: _approve,
                      icon: const Icon(Icons.check),
                      label: const Text('Aprobar'),
                    ),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }

  Widget _infoRow(String label, String value, {Color? color}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Flexible(flex: 2, child: Text(label, style: const TextStyle(fontWeight: FontWeight.w500, color: Colors.grey))),
          Expanded(child: Text(value, style: color != null ? TextStyle(color: color) : null)),
        ],
      ),
    );
  }
}
