import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/branch_provider.dart';

class BranchListPage extends ConsumerStatefulWidget {
  const BranchListPage({super.key});

  @override
  ConsumerState<BranchListPage> createState() => _BranchListPageState();
}

class _BranchListPageState extends ConsumerState<BranchListPage> {
  Future<void> _delete(Map<String, dynamic> b) async {
    final name = b['name'] ?? '';
    final confirm = await ZModal.confirm(
      context,
      title: 'Eliminar sucursal',
      message: '¿Eliminar "$name"? Solo es posible si no tiene productos activos, '
          'ventas, compras ni empleados asignados. Sus bodegas se darán de baja.',
    );
    if (!confirm) return;

    final branchId = b['id']?.toString();
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('branches/$branchId');

      // Si la sucursal eliminada era la activa en el cabecero, limpia la
      // selección (y la bodega asociada) para no mandar un X-Branch-Id muerto.
      final selected = ref.read(companyBranchProvider).branchId;
      if (branchId != null && selected == branchId) {
        ref.read(companyBranchProvider.notifier).clearBranch();
      }

      ref.invalidate(headerBranchListProvider);
      ref.invalidate(branchListProvider);
    } catch (e) {
      if (!mounted) return;
      final msg = e is DioException
          ? (e.response?.data is Map
              ? e.response?.data['message']?.toString()
              : null)
          : null;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(msg ?? 'No se pudo eliminar la sucursal'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final branchesAsync = ref.watch(branchListProvider);

    return Scaffold(
      body: branchesAsync.when(
        data: (branches) => branches.isEmpty
            ? const Center(child: Text('No hay sucursales configuradas'))
            : RefreshIndicator(
                onRefresh: () async => ref.invalidate(branchListProvider),
                child: ListView.builder(
                  itemCount: branches.length,
                  itemBuilder: (_, i) {
                    final b = branches[i];
                    return ZCard(
                      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                      child: ListTile(
                        leading: CircleAvatar(
                          backgroundColor: b['isActive'] == true
                              ? Colors.green.withValues(alpha: 0.15)
                              : Colors.grey.withValues(alpha: 0.15),
                          child: Icon(
                            Icons.store,
                            color: b['isActive'] == true ? Colors.green : Colors.grey,
                          ),
                        ),
                        title: Text(b['name'] ?? '', style: const TextStyle(fontWeight: FontWeight.w600)),
                        subtitle: Text([
                          if (b['code'] != null) b['code'],
                          if (b['phone'] != null) b['phone'],
                          if (b['address'] != null) b['address'],
                        ].join(' · ')),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            if (b['isActive'] != true)
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                decoration: BoxDecoration(
                                  color: Colors.grey.withValues(alpha: 0.15),
                                  borderRadius: BorderRadius.circular(12),
                                ),
                                child: const Text('Inactiva', style: TextStyle(fontSize: 12, color: Colors.grey)),
                              ),
                            const SizedBox(width: 4),
                            PopupMenuButton<String>(
                              padding: EdgeInsets.zero,
                              icon: const Icon(Icons.more_vert, size: 20),
                              onSelected: (value) {
                                if (value == 'edit') {
                                  context.push('/branches/${b['id']}');
                                } else if (value == 'delete') {
                                  _delete(b);
                                }
                              },
                              itemBuilder: (_) => const [
                                PopupMenuItem(value: 'edit', child: Text('Editar')),
                                PopupMenuItem(value: 'delete', child: Text('Eliminar')),
                              ],
                            ),
                          ],
                        ),
                        onTap: () => context.push('/branches/${b['id']}'),
                      ),
                    );
                  },
                ),
              ),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Error: $e')),
      ),
    );
  }
}
