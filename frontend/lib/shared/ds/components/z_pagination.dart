import 'package:flutter/material.dart';
import '../ds.dart';

/// Reusable pagination controls for data tables
class ZPagination extends StatelessWidget {
  final int currentPage;
  final int totalPages;
  final int totalItems;
  final int pageSize;
  final ValueChanged<int> onPageChanged;

  const ZPagination({
    super.key,
    required this.currentPage,
    required this.totalPages,
    required this.totalItems,
    required this.pageSize,
    required this.onPageChanged,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final startItem = (currentPage - 1) * pageSize + 1;
    final endItem = (currentPage * pageSize).clamp(1, totalItems);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.sm),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          // Info text
          Text(
            'Mostrando $startItem-$endItem de $totalItems',
            style: theme.textTheme.bodySmall?.copyWith(color: ZColors.neutral500),
          ),

          // Page controls
          Row(
            children: [
              // Previous
              IconButton(
                icon: const Icon(Icons.chevron_left, size: 20),
                onPressed: currentPage > 1 ? () => onPageChanged(currentPage - 1) : null,
                tooltip: 'Anterior',
              ),

              // Page numbers
              ..._buildPageNumbers(theme),

              // Next
              IconButton(
                icon: const Icon(Icons.chevron_right, size: 20),
                onPressed: currentPage < totalPages ? () => onPageChanged(currentPage + 1) : null,
                tooltip: 'Siguiente',
              ),
            ],
          ),
        ],
      ),
    );
  }

  List<Widget> _buildPageNumbers(ThemeData theme) {
    final pages = <int>{1, currentPage, totalPages};
    if (currentPage > 1) pages.add(currentPage - 1);
    if (currentPage < totalPages) pages.add(currentPage + 1);
    if (currentPage > 3) pages.add(0); // ellipsis
    if (currentPage < totalPages - 2) pages.add(totalPages + 1); // ellipsis

    final sorted = pages.where((p) => p >= 0 && p <= totalPages).toList()..sort();

    return sorted.map((page) {
      if (page == 0 || page == totalPages + 1) {
        return const Padding(
          padding: EdgeInsets.symmetric(horizontal: 4),
          child: Text('...', style: TextStyle(color: ZColors.neutral400)),
        );
      }

      final isSelected = page == currentPage;
      return Padding(
        padding: const EdgeInsets.symmetric(horizontal: 2),
        child: SizedBox(
          width: 32,
          height: 32,
          child: isSelected
              ? FilledButton(
                  onPressed: null,
                  style: FilledButton.styleFrom(
                    padding: EdgeInsets.zero,
                    minimumSize: Size.zero,
                  ),
                  child: Text('$page'),
                )
              : TextButton(
                  onPressed: () => onPageChanged(page),
                  style: TextButton.styleFrom(
                    padding: EdgeInsets.zero,
                    minimumSize: Size.zero,
                  ),
                  child: Text('$page'),
                ),
        ),
      );
    }).toList();
  }
}