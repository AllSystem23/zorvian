import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/ds/ds.dart';
import '../providers/employee_provider.dart';

class EmployeeDetailPage extends ConsumerStatefulWidget {
  final String employeeId;
  const EmployeeDetailPage({super.key, required this.employeeId});

  @override
  ConsumerState<EmployeeDetailPage> createState() => _EmployeeDetailPageState();
}

class _EmployeeDetailPageState extends ConsumerState<EmployeeDetailPage> {
  Map<String, dynamic>? _employee;
  List<dynamic> _periods = const [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('employees/${widget.employeeId}');
      List<dynamic> periods = const [];
      try {
        final pr = await dio.get('termination/periods/${widget.employeeId}');
        if (pr.data is List) periods = pr.data as List<dynamic>;
      } catch (_) {
        // El historial es best-effort: si falla, la página sigue siendo útil.
      }
      setState(() { _employee = r.data; _periods = periods; _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar trabajador'; _loading = false; });
    }
  }

  /// Recontratación sobre el MISMO registro: abre un nuevo período de contrato
  /// con la nueva fecha de ingreso (la antigüedad reinicia — la indemnización
  /// del período anterior ya fue liquidada y pagada).
  Future<void> _rehire() async {
    final hireDate = await showDatePicker(
      context: context,
      initialDate: DateTime.now(),
      firstDate: DateTime.now().subtract(const Duration(days: 365)),
      lastDate: DateTime.now().add(const Duration(days: 365)),
      helpText: 'Nueva fecha de ingreso',
    );
    if (hireDate == null || !mounted) return;

    final salaryController = TextEditingController();
    final confirmed = await showZAdaptiveDialog<bool>(
      context,
      builder: (ctx) => ZResponsiveDialog(
        title: 'Recontratar trabajador',
        icon: Icons.person_add_alt_outlined,
        maxWidth: 500,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Nueva fecha de ingreso: ${ZFormatters.date(hireDate)}\n\n'
              'Se abrirá un nuevo período de contrato sobre el mismo registro '
              '(conserva código, documentos e historial). La antigüedad '
              'reinicia: la liquidación del período anterior ya fue pagada.',
            ),
            const SizedBox(height: 12),
            TextField(
              controller: salaryController,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(
                labelText: 'Salario mensual (opcional)',
                prefixText: 'C\$ ',
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Recontratar'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    try {
      final dio = ref.read(dioClientProvider);
      final salary = double.tryParse(salaryController.text.trim());
      await dio.post(
        'termination/rehire/${widget.employeeId}',
        data: {
          'hireDate': hireDate.toIso8601String().substring(0, 10),
          if (salary != null && salary > 0) 'salary': salary,
        },
      );
      if (!mounted) return;
      ZToast.success(context, 'Trabajador recontratado: nuevo período de contrato abierto');
      _load();
      ref.read(employeeProvider.notifier).load();
    } catch (ex) {
      if (!mounted) return;
      final msg = ex is DioException
          ? (ex.response?.data is Map && (ex.response!.data as Map)['message'] != null
              ? (ex.response!.data as Map)['message'].toString()
              : 'No se pudo recontratar al trabajador')
          : 'No se pudo recontratar al trabajador';
      ZToast.error(context, msg);
    }
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'active': return Colors.green;
      case 'inactive': return Colors.grey;
      case 'terminated': return Colors.red;
      default: return Colors.orange;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (_loading) return const Scaffold(body: Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(body: Center(child: Text(_error!)));
    final e = _employee!;

    return Scaffold(
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(child: Text('${e['firstName']} ${e['lastName']}', style: theme.textTheme.titleLarge)),
                IconButton(
                  icon: const Icon(Icons.bolt),
                  tooltip: 'Generar Documento (3 clics)',
                  onPressed: () => context.push('/documents/quick-generate', extra: {
                    'entityType': 'employee',
                    'entityId': widget.employeeId,
                    'entityDisplayName': '${e['firstName']} ${e['lastName']}',
                  }),
                ),
                IconButton(
                  icon: const Icon(Icons.edit),
                  onPressed: () async {
                    final result = await context.push<bool>('/employees/${widget.employeeId}/edit');
                    if (result == true) {
                      _load();
                      if (mounted) ref.read(employeeProvider.notifier).load();
                    }
                  },
                ),
                if ((e['status'] as String? ?? 'active') != 'active') ...[
                  const SizedBox(width: 4),
                  FilledButton.icon(
                    icon: const Icon(Icons.person_add_alt_1, size: 18),
                    label: const Text('Recontratar'),
                    onPressed: _rehire,
                  ),
                ],
              ],
            ),
            const SizedBox(height: 8),
            ZCard(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      CircleAvatar(
                        radius: 32,
                        backgroundColor: theme.colorScheme.primaryContainer,
                        child: Text(
                          '${(e['firstName'] as String? ?? '?')[0]}${(e['lastName'] as String? ?? '?')[0]}',
                          style: TextStyle(fontSize: 24, color: theme.colorScheme.onPrimaryContainer),
                        ),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('${e['firstName']} ${e['lastName']}', style: theme.textTheme.titleLarge),
                            const SizedBox(height: 4),
                            Text(e['position'] ?? '', style: theme.textTheme.bodyMedium),
                          ],
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                        decoration: BoxDecoration(
                          color: _statusColor(e['status'] as String? ?? '').withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text(
                          ZFormatters.statusLabel(e['status'] as String? ?? ''),
                          style: TextStyle(color: _statusColor(e['status'] as String? ?? '')),
                        ),
                      ),
                    ],
                  ),
                  const Divider(height: 32),
                  _infoRow(Icons.badge, 'Código', e['employeeCode']),
                  _infoRow(Icons.email, 'Correo', e['email']),
                  _infoRow(Icons.phone, 'Teléfono', _fmtPhone(e['phone'])),
                  _infoRow(Icons.business, 'Departamento', e['departmentName']),
                  _infoRow(Icons.work, 'Cargo', e['position']),
                  _infoRow(Icons.calendar_today, 'Fecha contratación', _fmtDate(e['hireDate'])),
                  _infoRow(Icons.attach_money, 'Salario', _fmtSalary(e['salary'])),
                ],
              ),
            ),
            const SizedBox(height: 16),
            ZCard(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Información adicional', style: theme.textTheme.titleMedium),
                  const Divider(),
                  _infoRow(Icons.badge, 'Cédula', e['identificationNumber']),
                  _infoRow(Icons.cake, 'Fecha de nacimiento', _fmtDate(e['dateOfBirth'])),
                  _infoRow(Icons.wc, 'Género', e['gender'] == 'M' ? 'Masculino' : e['gender'] == 'F' ? 'Femenino' : e['gender']),
                ],
              ),
            ),
            const SizedBox(height: 16),
            ZCard(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Historial de Contratos', style: theme.textTheme.titleMedium),
                  const Divider(),
                  if (_periods.isEmpty)
                    const Center(child: Text('Sin períodos registrados'))
                  else
                    ..._periods.map((p) => _buildPeriodTile(p)),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  String? _fmtDate(dynamic raw) {
    if (raw == null || (raw as String).isEmpty) return null;
    final dt = DateTime.tryParse(raw);
    return dt != null ? ZFormatters.date(dt) : raw;
  }

  String? _fmtSalary(dynamic raw) {
    if (raw == null) return null;
    final v = double.tryParse(raw.toString());
    return v != null ? ZFormatters.currency(v) : raw.toString();
  }

  String? _fmtPhone(dynamic raw) {
    if (raw == null || (raw as String).isEmpty) return null;
    return ZFormatters.phone(raw);
  }

  Widget _infoRow(IconData icon, String label, String? value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 18, color: Colors.grey),
          const SizedBox(width: 12),
          SizedBox(
            width: 150,
            child: Text(label, style: const TextStyle(color: Colors.grey)),
          ),
          const SizedBox(width: 8),
          Expanded(child: Text(value ?? '—')),
        ],
      ),
    );
  }

  /// Fila de un período de contrato del historial: nº, fechas de ingreso/salida
  /// y estado (vigente / liquidado con motivo).
  Widget _buildPeriodTile(dynamic p) {
    final map = p is Map<String, dynamic> ? p : Map<String, dynamic>.from(p as Map);
    final number = map['periodNumber'];
    final hire = _fmtDate(map['hireDate']);
    final end = _fmtDate(map['terminationDate']);
    final status = map['status'] as String? ?? 'open';
    final reason = map['terminationReasonLabel'] as String?;
    final isOpen = status == 'open';
    final color = isOpen ? Colors.green : Colors.red;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(isOpen ? Icons.play_arrow : Icons.stop, size: 18, color: color),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Período #$number — ${hire ?? '?'} → ${end ?? 'vigente'}',
                  style: const TextStyle(fontWeight: FontWeight.w500),
                ),
                if (!isOpen && reason != null)
                  Text(reason, style: const TextStyle(color: Colors.grey, fontSize: 12)),
              ],
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.15),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(
              isOpen ? 'Vigente' : 'Cerrado',
              style: TextStyle(color: color, fontSize: 12),
            ),
          ),
        ],
      ),
    );
  }
}
