import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/provider_state.dart';

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
                    label: 'Total Prestadores',
                    value: stats['total'].toString(),
                  ),
                ),
                const SizedBox(width: ZSpacing.md),
                Expanded(
                  child: ZStatCard(
                    label: 'Activos',
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
                  label: 'Nuevo Prestador',
                  onPressed: () {}, // TODO: Implementar formulario
                  icon: Icons.add,
                ),
              ],
            ),
            const SizedBox(height: ZSpacing.md),
            
            providersAsync.when(
              data: (providers) => providers.isEmpty
                  ? const ZEmptyState(
                      title: 'No hay prestadores',
                      message: 'Aún no has registrado ningún prestador de servicios.',
                    )
                  : ZDataTable(
                      columns: const ['Nombre/Razón Social', 'Categoría', 'Estado', 'Acciones'],
                      rows: providers.map((p) => {
                        'Nombre/Razón Social': Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(p.businessName, style: ZTypography.labelMedium.copyWith(fontWeight: FontWeight.bold)),
                            Text(p.specialization ?? '', style: ZTypography.bodySmall),
                          ],
                        ),
                        'Categoría': Text(p.serviceCategory),
                        'Estado': ZBadge(label: p.status, isSuccess: p.status == 'active'),
                        'Acciones': IconButton(
                          icon: const Icon(Icons.chevron_right),
                          onPressed: () => context.push('/providers/${p.id}'),
                        ),
                      }).toList(),
                    ),
              loading: () => const ZSkeleton(height: 400),
              error: (err, _) => ZAlertCard(
                title: 'Error',
                message: 'No se pudieron cargar los prestadores: $err',
                isError: true,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
