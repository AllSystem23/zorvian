import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../core/widgets/bi/bi.dart';
import '../../shared/ds/ds.dart';
import 'providers/role_dashboard_provider.dart';

class DashboardV2Page extends ConsumerStatefulWidget {
  const DashboardV2Page({super.key});

  @override
  ConsumerState<DashboardV2Page> createState() => _DashboardV2PageState();
}

class _DashboardV2PageState extends ConsumerState<DashboardV2Page> {
  final _mainContentFocus = FocusNode();

  @override
  void dispose() {
    _mainContentFocus.dispose();
    super.dispose();
  }

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(roleDashboardProvider.notifier).load());
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(roleDashboardProvider);

    return Scaffold(
      body: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            mainAxisSize: MainAxisSize.min,
            children: [
              IconButton(
                icon: const Icon(Icons.refresh),
                tooltip: 'Actualizar',
                onPressed: () => ref.read(roleDashboardProvider.notifier).load(),
              ),
            ],
          ),
          ZSkipLink(targetFocus: _mainContentFocus),
          Expanded(
            child: ZMainContent(
              focusNode: _mainContentFocus,
              child: ZAsyncRenderer(
                value: state,
                builder: (groups) => RefreshIndicator(
                  onRefresh: () => ref.read(roleDashboardProvider.notifier).load(),
                  child: ReorderableListView.builder(
                    buildDefaultDragHandles: false,
                    padding: const EdgeInsets.all(16),
                    itemCount: groups.length,
                    onReorderItem: (oldIdx, newIdx) =>
                        ref.read(roleDashboardProvider.notifier).reorderGroup(oldIdx, newIdx),
                    itemBuilder: (context, index) {
                      final group = groups[index];
                      return _buildSection(context, group, index, groups.length);
                    },
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSection(
      BuildContext context, DashboardRoleKpiGroup group, int index, int total) {
    return Padding(
      key: ValueKey('section_${group.sectionLabel}'),
      padding: const EdgeInsets.only(bottom: 24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Semantics(
                label: 'Reordenar ${group.sectionLabel}',
                button: true,
                child: ReorderableDragStartListener(
                  index: index,
                  child: const Icon(Icons.drag_indicator, color: Colors.grey),
                ),
              ),
              const SizedBox(width: 8),
              Text(group.sectionLabel,
                  style: Theme.of(context)
                      .textTheme
                      .titleMedium
                      ?.copyWith(fontWeight: FontWeight.bold)),
              const Spacer(),
              if (index > 0)
                IconButton(
                  icon: const Icon(Icons.keyboard_arrow_up, size: 20),
                  onPressed: () =>
                      ref.read(roleDashboardProvider.notifier).reorderGroup(index, index - 1),
                ),
              if (index < total - 1)
                IconButton(
                  icon: const Icon(Icons.keyboard_arrow_down, size: 20),
                  onPressed: () =>
                      ref.read(roleDashboardProvider.notifier).reorderGroup(index, index + 1),
                ),
            ],
          ),
          const SizedBox(height: 8),
          _buildKpiGrid(context, group.kpis),
        ],
      ),
    );
  }

  Widget _buildKpiGrid(BuildContext context, List<DashboardRoleKpi> kpis) {
    return FocusTraversalGroup(
      child: LayoutBuilder(
      builder: (context, constraints) {
        final width = constraints.maxWidth;
        final crossAxisCount = width > 800 ? 4 : width > 500 ? 3 : 2;
        final cardWidth = (width - (crossAxisCount - 1) * 8) / crossAxisCount;

        return Wrap(
          spacing: 8,
          runSpacing: 8,
          children: kpis.map((kpi) => SizedBox(
            width: cardWidth,
            child: BiKpiCard(
              label: kpi.label,
              value: kpi.value,
              changePercent: kpi.changePercent,
              icon: kpi.icon,
              color: kpi.color,
              sparklineData: kpi.sparklineData,
              onTap: kpi.drillDownRoute != null
                  ? () => context.push(kpi.drillDownRoute!)
                  : null,
            ),
          )).toList(),
        );
      },
    ),
    );
  }
}
