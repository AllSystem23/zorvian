import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../providers/workshop_provider.dart';

class WorkshopListPage extends ConsumerStatefulWidget {
  const WorkshopListPage({super.key});
  @override
  ConsumerState<WorkshopListPage> createState() => _WorkshopListPageState();
}

class _WorkshopListPageState extends ConsumerState<WorkshopListPage> {
  final _searchCtrl = TextEditingController();
  String _search = '';

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(workshopProvider.notifier).load());
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final ws = ref.watch(workshopProvider);
    final filtered = _search.isEmpty
        ? ws.items
        : ws.items.where((w) =>
            w.name.toLowerCase().contains(_search.toLowerCase()) ||
            w.code.toLowerCase().contains(_search.toLowerCase()) ||
            (w.city?.toLowerCase().contains(_search.toLowerCase()) ?? false)).toList();

    return Scaffold(
      body: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              IconButton(
                icon: const Icon(Icons.add),
                tooltip: 'Nuevo taller',
                onPressed: () => context.push('/workshops/new'),
              ),
            ],
          ),
          Padding(
            padding: const EdgeInsets.all(ZSpacing.md),
            child: TextField(
              controller: _searchCtrl,
              decoration: InputDecoration(
                hintText: 'Buscar taller...',
                prefixIcon: const Icon(Icons.search),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(ZRadii.lg)),
                contentPadding: const EdgeInsets.symmetric(horizontal: ZSpacing.md),
              ),
              onChanged: (v) => setState(() => _search = v),
            ),
          ),
          Expanded(
            child: ws.loading
                ? const Center(child: CircularProgressIndicator(color: ZColors.brandPrimary))
                : ws.error != null
                    ? Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            const Icon(Icons.error_outline, size: 48, color: ZColors.danger),
                            const SizedBox(height: ZSpacing.md),
                            Text(ws.error!),
                            const SizedBox(height: ZSpacing.lg),
                            ZButton(text: 'Reintentar', icon: Icons.refresh, onPressed: () => ref.read(workshopProvider.notifier).load()),
                          ],
                        ),
                      )
                    : filtered.isEmpty
                        ? ZEmptyState.list(
                            itemType: 'talleres',
                            actionLabel: 'Crear taller',
                            onAction: () => context.push('/workshops/new'),
                          )
                        : RefreshIndicator(
                            onRefresh: () => ref.read(workshopProvider.notifier).load(),
                            child: ListView.separated(
                              padding: const EdgeInsets.all(ZSpacing.md),
                              itemCount: filtered.length,
                              separatorBuilder: (_, _) => const SizedBox(height: ZSpacing.sm),
                              itemBuilder: (_, i) {
                                final w = filtered[i];
                                return ZCard(
                                  padding: const EdgeInsets.all(ZSpacing.md),
                                  child: ListTile(
                                    contentPadding: EdgeInsets.zero,
                                    leading: CircleAvatar(
                                      backgroundColor: w.isActive
                                          ? ZColors.brandPrimary.withAlpha(25)
                                          : ZColors.neutral200,
                                      child: Icon(Icons.build, color: w.isActive ? ZColors.brandPrimary : ZColors.neutral400),
                                    ),
                                    title: Text(w.name, style: ZTypography.titleMedium),
                                    subtitle: Text(
                                      '${w.code} · ${w.city ?? '—'} · ${w.technicianCount} técnicos',
                                      style: ZTypography.bodySmall,
                                    ),
                                    trailing: Row(
                                      mainAxisSize: MainAxisSize.min,
                                      children: [
                                        if (w.rating > 0)
                                          Row(
                                            mainAxisSize: MainAxisSize.min,
                                            children: [
                                              const Icon(Icons.star, size: 16, color: ZColors.warning),
                                              const SizedBox(width: 2),
                                              Text(w.rating.toStringAsFixed(1), style: ZTypography.bodySmall),
                                            ],
                                          ),
                                        const SizedBox(width: ZSpacing.sm),
                                        ZBadge(
                                          text: w.isActive ? 'Activo' : 'Inactivo',
                                          type: w.isActive ? ZBadgeType.success : ZBadgeType.neutral,
                                        ),
                                        const SizedBox(width: ZSpacing.sm),
                                        const Icon(Icons.chevron_right),
                                      ],
                                    ),
                                    onTap: () => context.push('/workshops/${w.id}'),
                                  ),
                                );
                              },
                            ),
                          ),
          ),
        ],
      ),
    );
  }
}
