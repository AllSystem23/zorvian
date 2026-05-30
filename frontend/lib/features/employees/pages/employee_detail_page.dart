import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';

class EmployeeDetailPage extends ConsumerStatefulWidget {
  final String employeeId;
  const EmployeeDetailPage({super.key, required this.employeeId});

  @override
  ConsumerState<EmployeeDetailPage> createState() => _EmployeeDetailPageState();
}

class _EmployeeDetailPageState extends ConsumerState<EmployeeDetailPage> {
  Map<String, dynamic>? _employee;
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
      final r = await dio.get('/employees/${widget.employeeId}');
      setState(() { _employee = r.data; _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar empleado'; _loading = false; });
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

    if (_loading) return Scaffold(appBar: AppBar(title: const Text('Empleado')), body: const Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(appBar: AppBar(title: const Text('Empleado')), body: Center(child: Text(_error!)));
    final e = _employee!;

    return Scaffold(
      appBar: AppBar(
        title: Text('${e['firstName']} ${e['lastName']}'),
        actions: [
          IconButton(
            icon: const Icon(Icons.edit),
            onPressed: () => context.push('/employees/${widget.employeeId}/edit'),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Card(
              child: Padding(
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
                            e['status'] as String? ?? '',
                            style: TextStyle(color: _statusColor(e['status'] as String? ?? '')),
                          ),
                        ),
                      ],
                    ),
                    const Divider(height: 32),
                    _infoRow(Icons.badge, 'Código', e['employeeCode']),
                    _infoRow(Icons.email, 'Correo', e['email']),
                    _infoRow(Icons.phone, 'Teléfono', e['phone']),
                    _infoRow(Icons.business, 'Departamento', e['departmentName']),
                    _infoRow(Icons.work, 'Cargo', e['position']),
                    _infoRow(Icons.calendar_today, 'Fecha contratación', e['hireDate']),
                    _infoRow(Icons.attach_money, 'Salario', e['salary']?.toString()),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Información adicional', style: theme.textTheme.titleMedium),
                    const Divider(),
                    _infoRow(Icons.badge, 'Cédula', e['identificationNumber']),
                    _infoRow(Icons.cake, 'Fecha de nacimiento', e['dateOfBirth']),
                    _infoRow(Icons.wc, 'Género', e['gender'] == 'M' ? 'Masculino' : e['gender'] == 'F' ? 'Femenino' : e['gender']),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Historial', style: theme.textTheme.titleMedium),
                    const Divider(),
                    const Center(child: Text('Sin historial registrado')),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _infoRow(IconData icon, String label, String? value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Icon(icon, size: 18, color: Colors.grey),
          const SizedBox(width: 12),
          SizedBox(width: 120, child: Text(label, style: const TextStyle(color: Colors.grey))),
          Expanded(child: Text(value ?? '—')),
        ],
      ),
    );
  }
}
