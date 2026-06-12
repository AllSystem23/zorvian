import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/provider_state.dart';
import '../../../core/entities/service_provider.dart';

class ProviderListPage extends ConsumerWidget {
  const ProviderListPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final providersAsync = ref.watch(serviceProvidersProvider);
    final stats = ref.watch(providerStatsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Prestadores de Servicios')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Estadísticas Rápidas
            Row(
              children: [
                Expanded(
                  child: ZStatCard(
                    title: 'Total Prestadores',
                    value: stats['total'].toString(),
                  ),
                ),
                const SizedBox(width: ZSpacing.md),
                Expanded(
                  child: ZStatCard(
                    title: 'Activos',
                    value: stats['active'].toString(),
                  ),
                ),
              ],
            ),
            const SizedBox(height: ZSpacing.xl),
            
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Lista de Colaboradores Externos', style: ZTypography.titleLarge),
                ZButton(
                  text: 'Nuevo Prestador',
                  onPressed: () {}, // TODO: Implementar formulario
                  icon: Icons.add,
                ),
              ],
            ),
            const SizedBox(height: ZSpacing.md),
            
            providersAsync.when(
              data: (providers) => providers.isEmpty
                  ? const ZEmptyState(
                      icon: Icons.person_off,
                      title: 'No hay prestadores',
                      subtitle: 'Aún no has registrado ningún prestador de servicios.',
                    )
                  : ZDataTable<ServiceProvider>(
                      columns: const [
                        ZColumn(id: 'name', label: 'Nombre/Razón Social'),
                        ZColumn(id: 'category', label: 'Categoría'),
                        ZColumn(id: 'status', label: 'Estado'),
                        ZColumn(id: 'actions', label: ''),
                      ],
                      rows: providers,
                      rowMapper: (p) => DataRow(cells: [
                        DataCell(Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(p.businessName, style: ZTypography.labelMedium.copyWith(fontWeight: FontWeight.bold)),
                            Text(p.specialization ?? '', style: ZTypography.bodySmall),
                          ],
                        )),
                        DataCell(Text(p.serviceCategory)),
                        DataCell(ZBadge(text: p.status, type: p.status == 'active' ? ZBadgeType.success : ZBadgeType.neutral)),
                        DataCell(IconButton(
                          icon: const Icon(Icons.chevron_right),
                          onPressed: () => context.push('/providers/${p.id}'),
                        )),
                      ]),
                    ),
              loading: () => const ZSkeleton(height: 400),
              error: (err, _) => ZAlertCard(
                message: 'No se pudieron cargar los prestadores: $err',
                severity: 'high',
              ),
            ),
          ],
        ),
      ),
    );
  }
}
