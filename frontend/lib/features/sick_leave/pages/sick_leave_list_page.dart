import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../core/widgets/empty_state.dart';
import '../providers/sick_leave_provider.dart';

class SickLeaveListPage extends ConsumerStatefulWidget {
  final String employeeId;
  const SickLeaveListPage({super.key, required this.employeeId});

  @override
  ConsumerState<SickLeaveListPage> createState() => _SickLeaveListPageState();
}

class _SickLeaveListPageState extends ConsumerState<SickLeaveListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(sickLeaveProvider.notifier).loadByEmployee(widget.employeeId));
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(sickLeaveProvider);

    return Scaffold(
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 40, 16, 0),
            child: Row(
              children: [
                const Expanded(child: Text('Incapacidades Médicas', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold))),
                IconButton(
                  icon: const Icon(Icons.add),
                  onPressed: () => context.push('/sick-leave/new?employeeId=${widget.employeeId}'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 8),
          Expanded(
            child: state.loading
                ? const Center(child: CircularProgressIndicator())
                : state.records.isEmpty
              ? EmptyState(
                  icon: Icons.medical_services_outlined,
                  title: 'Sin incapacidades',
                  subtitle: 'No hay registros de incapacidad para este empleado',
                  actionLabel: 'Nueva incapacidad',
                  onAction: () => context.push('/sick-leave/new?employeeId=${widget.employeeId}'),
                )
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: state.records.length,
                  itemBuilder: (_, i) {
                    final r = state.records[i];
                    final status = r['status']?.toString() ?? '';
                    final isPending = status == 'pending';
                    return ZCard(
                      padding: EdgeInsets.zero,
                      child: ListTile(
                        leading: CircleAvatar(
                          backgroundColor: isPending ? Colors.orange.shade100 : Colors.green.shade100,
                          child: Icon(
                            Icons.medical_services,
                            color: isPending ? Colors.orange : Colors.green,
                          ),
                        ),
                        title: Text('${r['startDate'] ?? ''} - ${r['endDate'] ?? ''}'),
                        subtitle: Text('${r['diagnosisCode'] ?? ''} · ${isPending ? "Pendiente" : "Aprobado"}'),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Chip(
                              label: Text(
                                isPending ? 'Pendiente' : 'Aprobado',
                                style: TextStyle(fontSize: 11, color: isPending ? Colors.orange : Colors.green),
                              ),
                              materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                            ),
                            if (isPending)
                              IconButton(
                                icon: const Icon(Icons.check_circle_outline, color: Colors.green),
                                onPressed: () async {
                                  await ref.read(sickLeaveProvider.notifier).approve(r['id']);
                                  ref.read(sickLeaveProvider.notifier).loadByEmployee(widget.employeeId);
                                },
                              ),
                          ],
                        ),
                      ),
                    );
                  },
                ),
          ),
        ],
      ),
    );
  }
}
