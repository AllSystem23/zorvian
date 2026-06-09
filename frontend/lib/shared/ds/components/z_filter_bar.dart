import 'package:flutter/material.dart';
import '../ds.dart';

/// Filter bar with chips, search and sort
class ZFilterBar extends StatelessWidget {
  final List<ZFilterChipData> filters;
  final String? activeFilter;
  final ValueChanged<String?> onFilterChanged;
  final VoidCallback? onClearAll;
  final Widget? trailing;
  final bool showSearch;
  final TextEditingController? searchController;
  final ValueChanged<String>? onSearchChanged;

  const ZFilterBar({
    super.key,
    required this.filters,
    required this.onFilterChanged,
    this.activeFilter,
    this.onClearAll,
    this.trailing,
    this.showSearch = true,
    this.searchController,
    this.onSearchChanged,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.sm),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        borderRadius: BorderRadius.circular(ZRadii.md),
        border: Border.all(color: theme.colorScheme.outlineVariant),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (showSearch) ...[
            Row(
              children: [
                Expanded(
                  child: ZSearchField(
                    controller: searchController,
                    onChanged: onSearchChanged ?? (_) {},
                    hintText: 'Buscar...',
                  ),
                ),
                if (trailing != null) ...[
                  const SizedBox(width: ZSpacing.sm),
                  trailing!,
                ],
              ],
            ),
            const SizedBox(height: ZSpacing.sm),
          ],
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: [
                _buildClearAllChip(),
                for (final filter in filters) ...[
                  const SizedBox(width: 6),
                  _buildFilterChip(filter),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFilterChip(ZFilterChipData filter) {
    final isActive = activeFilter == filter.value;
    return FilterChip(
      label: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(filter.label),
          if (filter.count != null) ...[
            const SizedBox(width: 4),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
              decoration: BoxDecoration(
                color: isActive ? Colors.white.withValues(alpha: 0.2) : ZColors.neutral200,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Text(
                '${filter.count}',
                style: TextStyle(fontSize: 10, color: isActive ? Colors.white : ZColors.neutral600, fontWeight: FontWeight.w600),
              ),
            ),
          ],
        ],
      ),
      selected: isActive,
      onSelected: (_) => onFilterChanged(isActive ? null : filter.value),
    );
  }

  Widget _buildClearAllChip() {
    final hasActive = activeFilter != null;
    if (!hasActive && onClearAll == null) return const SizedBox.shrink();
    return ActionChip(
      avatar: const Icon(Icons.close, size: 16),
      label: const Text('Limpiar'),
      onPressed: () => onFilterChanged(null),
    );
  }
}

class ZFilterChipData {
  final String label;
  final String value;
  final int? count;
  final IconData? icon;

  const ZFilterChipData({
    required this.label,
    required this.value,
    this.count,
    this.icon,
  });
}

/// Sort dropdown
class ZSortDropdown extends StatelessWidget {
  final String? currentSort;
  final List<ZSortOption> options;
  final ValueChanged<String> onChanged;

  const ZSortDropdown({
    super.key,
    required this.options,
    required this.onChanged,
    this.currentSort,
  });

  @override
  Widget build(BuildContext context) {
    return PopupMenuButton<String>(
      tooltip: 'Ordenar',
      onSelected: onChanged,
      itemBuilder: (_) => options.map((opt) {
        return PopupMenuItem(
          value: opt.value,
          child: Row(
            children: [
              Icon(
                currentSort == opt.value ? Icons.check : opt.icon,
                size: 18,
                color: currentSort == opt.value ? Theme.of(context).colorScheme.primary : ZColors.neutral500,
              ),
              const SizedBox(width: 8),
              Text(opt.label),
            ],
          ),
        );
      }).toList(),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        decoration: BoxDecoration(
          border: Border.all(color: Theme.of(context).colorScheme.outlineVariant),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.sort, size: 16),
            const SizedBox(width: 6),
            Text(
              options.firstWhere((o) => o.value == currentSort, orElse: () => options.first).label,
              style: const TextStyle(fontSize: 13),
            ),
          ],
        ),
      ),
    );
  }
}

class ZSortOption {
  final String label;
  final String value;
  final IconData icon;

  const ZSortOption({required this.label, required this.value, this.icon = Icons.sort});
}
